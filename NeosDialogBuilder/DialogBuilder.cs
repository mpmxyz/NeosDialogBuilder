using BaseX;
using FrooxEngine;
using System.Collections.Generic;
using FrooxEngine.UIX;
using System.Reflection;
using System;
using static NeosDialogBuilder.NeosDialogBuilderMod;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Helper class to create an import dialog
    /// </summary>
    public partial class DialogBuilder<T> where T : IDialog
    {
        private readonly IList<IDialogEntryDefinition<T>> entries = new List<IDialogEntryDefinition<T>>();
        private readonly Func<(IDictionary<string, string>, IDictionary<string, string>)> customUpdateAndValidate;

        public DialogBuilder(bool addDefaults = true, Func<(IDictionary<string, string>, IDictionary<string, string>)> customUpdateAndValidate = null)
        {
            if (addDefaults)
            {
                AddAllOptions();
                AddUnboundErrorDisplay();
                AddAllActions();
            }

            this.customUpdateAndValidate = customUpdateAndValidate;
        }

        public DialogBuilder<T> AddEntry(IDialogEntryDefinition<T> optionField)
        {
            entries.Add(optionField);
            return this;
        }

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
                        actions.Add(new DialogAction<T>(conf, (dialog) => methodInfo.Invoke(dialog, new object[] { })));
                        break;
                    }
                }
            }
            AddActionLine(actions);

            return this;
        }

        public DialogBuilder<T> AddOption(DialogOptionAttribute conf, FieldInfo fieldInfo)
        {
            var genType = typeof(DialogOption<,>).MakeGenericType(typeof(T), fieldInfo.FieldType);
            var cons = genType.GetConstructor(
                    new Type[] {
                        typeof(DialogOptionAttribute),
                        typeof(FieldInfo)
                    }
                );
            var field = (IDialogEntryDefinition<T>) cons.Invoke(new object[] { conf, fieldInfo });
            return AddEntry(field);
        }

        public DialogBuilder<T> AddActionLine(IEnumerable<DialogAction<T>> actions)
        {
            AddEntry(new DialogLine<T>(actions));
            return this;
        }

        public DialogBuilder<T> AddUnboundErrorDisplay()
        {
            AddEntry(new DialogErrorDisplay<T>(onlyUnbound: true));
            return this;
        }

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
                if (customUpdateAndValidate != null)
                {
                    (errors, unboundErrors) = customUpdateAndValidate();
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