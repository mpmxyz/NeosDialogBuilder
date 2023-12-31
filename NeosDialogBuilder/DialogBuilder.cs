﻿using BaseX;
using FrooxEngine;
using System.Collections.Generic;
using FrooxEngine.UIX;
using System.Reflection;
using System;
using static NeosDialogBuilder.NeosDialogBuilderMod;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Helper class to create an import dialog <br/>
    /// To be exact this class starts its life as a builder where you can add defaults or custom elements to define a dialog.
    /// Then you can use it as a factory to produce new UIX windows from objects representing your custom dialog.
    /// </summary>
    /// <typeparam name="T">expected dialog object type</typeparam>
    public partial class DialogBuilder<T> where T : IDialog
    {
        private readonly IList<IDialogEntryDefinition<T>> entries = new List<IDialogEntryDefinition<T>>();
        private readonly Func<T, (IDictionary<string, string>, IDictionary<string, string>)> overrideUpdateAndValidate;

        /// <summary>
        /// Creates a dialog builder that can be configured to create dialog windows.
        /// </summary>
        /// <param name="addDefaults">creates a list of options, an output for errors and a line with buttons based on <typeparamref name="T"/>'s attributes</param>
        /// <param name="overrideUpdateAndValidate">replaces the default validation if not null</param>
        public DialogBuilder(bool addDefaults = true, Func<T, (IDictionary<string, string>, IDictionary<string, string>)> overrideUpdateAndValidate = null)
        {
            if (addDefaults)
            {
                AddAllOptions();
                AddUnboundErrorDisplay();
                AddAllActions();
            }

            this.overrideUpdateAndValidate = overrideUpdateAndValidate;
        }

        /// <summary>
        /// Adds a line to the dialog configuration
        /// </summary>
        /// <param name="optionField">object that will generate UI for an instance of <typeparamref name="T"/></param>
        /// <returns>this</returns>
        public DialogBuilder<T> AddEntry(IDialogEntryDefinition<T> optionField)
        {
            entries.Add(optionField);
            return this;
        }

        /// <summary>
        /// Adds a line for each of <typeparamref name="T"/>'s attributes annotated with <see cref="DialogOptionAttribute"/>
        /// </summary>
        /// <returns>this</returns>
        public DialogBuilder<T> AddAllOptions()
        {
            var converterType = typeof(T);

            foreach (var fieldInfo in converterType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var attr in fieldInfo.GetCustomAttributes(true))
                {
                    if (attr is DialogOptionAttribute conf)
                    {
                        AddOption(conf, fieldInfo);
                        break;
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Adds a line with one button for each of <typeparamref name="T"/>'s attributes annotated with <see cref="DialogActionAttribute"/>
        /// </summary>
        /// <returns>this</returns>
        /// <exception cref="InvalidOperationException">If an annotated method has arguments.</exception>
        public DialogBuilder<T> AddAllActions()
        {
            var converterType = typeof(T);
            var actions = new List<DialogAction<T>>();
            foreach (var methodInfo in converterType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var attr in methodInfo.GetCustomAttributes(true))
                {
                    if (attr is DialogActionAttribute conf)
                    {
                        if (methodInfo.GetParameters().Length != 0)
                        {
                            throw new InvalidOperationException($"DialogAction '{methodInfo.Name}' must have no arguments!");
                        }
                        actions.Add(new DialogAction<T>(conf, (dialog) => methodInfo.Invoke(dialog, new object[] { })));
                        break;
                    }
                }
            }
            AddLine(actions);

            return this;
        }

        /// <summary>
        /// Adds a line with an editable value
        /// </summary>
        /// <param name="conf">displayed name, secrecy and error output options</param>
        /// <param name="fieldInfo">field of <typeparamref name="T"/> which will be edited</param>
        /// <returns>this</returns>
        public DialogBuilder<T> AddOption(DialogOptionAttribute conf, FieldInfo fieldInfo)
        {
            var genType = typeof(DialogOption<,>).MakeGenericType(typeof(T), fieldInfo.FieldType);
            var cons = genType.GetConstructor(
                    new Type[] {
                        typeof(DialogOptionAttribute),
                        typeof(FieldInfo)
                    }
                );
            var field = (IDialogEntryDefinition<T>)cons.Invoke(new object[] { conf, fieldInfo });
            return AddEntry(field);
        }

        /// <summary>
        /// Adds a line with multiple sub-elements
        /// </summary>
        /// <param name="elements">Elements that will be placed in a single line.</param>
        /// <returns>this</returns>
        public DialogBuilder<T> AddLine(IEnumerable<IDialogEntryDefinition<T>> elements)
        {
            AddEntry(new DialogLine<T>(elements));
            return this;
        }

        /// <summary>
        /// Creates a text output that shows a list of all errors that are not displayed directly on the problematic input.
        /// </summary>
        /// <returns>this</returns>
        public DialogBuilder<T> AddUnboundErrorDisplay()
        {
            AddEntry(new DialogErrorDisplay<T>(onlyUnbound: true));
            return this;
        }

        /// <summary>
        /// Adds the dialog UI to whereever the <paramref name="uiBuilder"/> is currently at
        /// </summary>
        /// <param name="uiBuilder">used to build the UI</param>
        /// <param name="dialog">object that will be configured by the UI</param>
        public void BuildInPlace(UIBuilder uiBuilder, T dialog)
        {
            var errorSetters = new List<Action<IDictionary<string, string>, IDictionary<string, string>>>();
            var boundErrorKeys = new HashSet<string>();
            var world = uiBuilder.World;
            var inUserspace = world.IsUserspace();

            uiBuilder.Root.OnPrepareDestroy += (slot) => dialog.OnDestroy();

            (IDictionary<string, string>, IDictionary<string, string>) onChange()
            {
                IDictionary<string, string> errors, unboundErrors;
                if (overrideUpdateAndValidate != null)
                {
                    (errors, unboundErrors) = overrideUpdateAndValidate(dialog);
                }
                else
                {
                    errors = dialog.UpdateAndValidate();
                    unboundErrors = new Dictionary<string, string>(errors);
                    foreach (var errorKey in boundErrorKeys)
                    {
                        unboundErrors.Remove(errorKey);
                    }
                }

                world.RunSynchronously(() =>
                {
                    foreach (var setErrors in errorSetters)
                    {
                        setErrors(errors, unboundErrors);
                    }
                });

                return (errors, unboundErrors);
            }

            foreach (var option in entries)
            {
                (var keys, var setErrors) = option.Create(uiBuilder, dialog, onChange, inUserspace);
                errorSetters.Add(setErrors);
                foreach (var key in keys)
                {
                    boundErrorKeys.Add(key);
                }
            }

            onChange();
        }

        /// <summary>
        /// Creates a dialog window and positions it in front of the user
        /// </summary>
        /// <param name="title">title text of the window</param>
        /// <param name="world">world to place the window in, userspace will directly editing secret options</param>
        /// <param name="dialog">dialog object</param>
        /// <returns>The root of the created window</returns>
        public Slot BuildWindow(string title, World world, T dialog)
        {
            var slot = world.AddSlot(title, persistent: false);

            var panel = slot.AttachComponent<NeosCanvasPanel>();
            panel.Panel.Title = title;
            panel.Panel.AddCloseButton();
            panel.CanvasSize = CONFIG_CANVAS_SIZE;
            panel.CanvasScale = CONFIG_PANEL_HEIGHT / panel.CanvasSize.y;

            var uiBuilder = new UIBuilder(panel.Canvas);

            uiBuilder.ScrollArea();
            uiBuilder.VerticalLayout(SPACING);                      //problem: cannot measure size here
            var content = uiBuilder.VerticalLayout(SPACING).Slot;   //solution: extra layer for content
            uiBuilder.FitContent(SizeFit.Disabled, SizeFit.PreferredSize); //TODO: clamp to max-size
            BuildInPlace(uiBuilder, dialog);

            var sizeDriver = content.AttachComponent<RectSizeDriver>();
            sizeDriver.TargetSize.Target = panel.Canvas.Size;

            slot.PositionInFrontOfUser(float3.Backward);

            return slot;
        }
    }
}