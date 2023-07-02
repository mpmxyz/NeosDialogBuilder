using BaseX;
using FrooxEngine;
using System.Collections.Generic;
using FrooxEngine.UIX;
using System.Reflection;
using System;
using static NeosDialogBuilder.NeosDialogBuilderMod;
using System.Linq;

namespace NeosDialogBuilder//TODO: fix .github
{
    /// <summary>
    /// Helper class to create an import dialog
    /// </summary>
    public partial class DialogBuilder<T> where T : IDialog
    {
        private readonly IList<IDialogOptionField<T>> options = new List<IDialogOptionField<T>>();
        public bool hasErrorOverflow = false;
        private readonly IList<DialogActionMethod<T>> actions = new List<DialogActionMethod<T>>();

        public DialogBuilder(bool autoAdd = true, bool hasErrorOverflow = false)
        {
            if (autoAdd)
            {
                AutoAddOptions();
                AutoAddActions();
            }

            this.hasErrorOverflow = hasErrorOverflow;
        }

        public DialogBuilder<T> AutoAddOptions()
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

        public DialogBuilder<T> AutoAddActions()
        {
            var converterType = typeof(T);

            foreach (var methodInfo in converterType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var attr in methodInfo.GetCustomAttributes(true))
                {
                    if (attr is DialogActionAttribute conf)
                    {
                        AddAction(conf, (dialog) => methodInfo.Invoke(dialog, new object[] { }));
                        break;
                    }
                }
            }
            return this;
        }

        public DialogBuilder<T> AddOption(DialogOptionAttribute conf, FieldInfo fieldInfo)
        {
            var genType = typeof(DialogOptionField<,>).MakeGenericType(typeof(T), fieldInfo.FieldType);
            var cons = genType.GetConstructor(
                    new Type[] {
                        typeof(DialogOptionAttribute),
                        typeof(FieldInfo)
                    }
                );
            var field = (IDialogOptionField<T>) cons.Invoke(new object[] { conf, fieldInfo });
            return AddOption(field);
        }

        public DialogBuilder<T> AddOption(IDialogOptionField<T> optionField)
        {
            options.Add(optionField);
            return this;
        }

        public DialogBuilder<T> AddAction(DialogActionAttribute conf, Action<T> action)
        {
            return AddAction(new DialogActionMethod<T>(conf, action));
        }

        public DialogBuilder<T> AddAction(DialogActionMethod<T> action)
        {
            actions.Add(action);
            return this;
        }

        public void BuildInPlace(UIBuilder uiBuilder, T dialog, bool privateEnvironment = false)
        {
            var errorSetters = new Dictionary<string, Action<string>>();
            var knownErrors = new Dictionary<string, string>();
            var validationSetters = new List<Action<IDictionary<string, string>>>();
            Action<IEnumerable<string>> setUnassignedErrors = null;

            void onChange()
            {
                var errors = dialog.Validate();
                var unassignedErrors = new List<string>();
                foreach (var error in errors)
                {
                    if (errorSetters.TryGetValue(error.Key, out Action<string> setter))
                    {
                        setter(error.Value);
                    }
                    else
                    {
                        unassignedErrors.Add(error.Value);
                    }
                }
                foreach (var previousError in knownErrors)
                {
                    if (
                        !errors.ContainsKey(previousError.Key)
                        && errorSetters.TryGetValue(previousError.Key, out Action<string> setter)
                    )
                    {
                        setter(null);
                    }
                }

                if (setUnassignedErrors != null)
                {
                    unassignedErrors.Sort();
                    setUnassignedErrors(unassignedErrors);
                }

                foreach (var validSetter in validationSetters)
                {
                    validSetter(errors);
                }
                knownErrors = new Dictionary<string, string>(errors);
            }

            foreach (var option in options)
            {
                (var key, var setError) = option.Build(uiBuilder, dialog, onChange, privateEnvironment);
                errorSetters.Add(key, setError);
            }

            if (hasErrorOverflow)
            {
                uiBuilder.Style.FlexibleHeight = 1f;
                //TODO: create place to put unassignedErrors
            }

            uiBuilder.Style.FlexibleHeight = -1;
            uiBuilder.Style.MinHeight = BUTTON_HEIGHT;
            uiBuilder.Style.PreferredHeight = BUTTON_HEIGHT;
            uiBuilder.Style.ForceExpandWidth = true;

            uiBuilder.HorizontalLayout(SPACING);
            uiBuilder.Style.FlexibleWidth = 1;

            foreach (var action in actions)
            {
                var setValidation = action.Build(uiBuilder, dialog);
                validationSetters.Add(setValidation);
            }
            //TODO: pop out of scroll area
            onChange();
        }

        public Slot BuildWindow(
            string title,
            World world,
            float3 position,
            floatQ rotation,
            float3 scale,
            T dialog,
            bool privateEnvironment = false)
        {
            var slot = world.AddSlot(title, false);
            slot.GlobalPosition = position;
            slot.GlobalRotation = rotation;
            slot.GlobalScale = scale;

            var panel = slot.AttachComponent<NeosCanvasPanel>();
            panel.Panel.Title = title;
            panel.Panel.AddCloseButton();
            panel.CanvasSize = CONFIG_CANVAS_SIZE; //TODO: auto-calculate canvas size, clamped to max-size
            panel.CanvasScale = CONFIG_PANEL_HEIGHT / panel.CanvasSize.y;

            var uiBuilder = new UIBuilder(panel.Canvas);

            uiBuilder.ScrollArea();
            uiBuilder.VerticalLayout(SPACING); //cannot measure size here
            var content = uiBuilder.VerticalLayout(SPACING).Slot; //solution: extra layer for content
            uiBuilder.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
            BuildInPlace(uiBuilder, dialog, privateEnvironment);

            var sizeDriver = content.AttachComponent<RectSizeDriver>();
            sizeDriver.TargetSize.Target = panel.Canvas.Size;

            return slot;
        }
    }
}