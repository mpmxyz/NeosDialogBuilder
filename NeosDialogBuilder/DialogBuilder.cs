using BaseX;
using FrooxEngine;
using System.Collections.Generic;
using FrooxEngine.UIX;
using System.Reflection;
using CodeX;
using System;
using System.Collections;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Helper class to create an import dialog
    /// </summary>
    public class DialogBuilder<T> where T : IDialog
    {
        private readonly IList<IDialogOptionField> options;

        private interface IDialogOptionField
        {
            (string, Action<string>) Build(UIBuilder uiBuilder, T dialog, Action onChange, bool privateEnvironment = false);
        }

        private class DialogOptionField<V> : IDialogOptionField
        {
            private readonly DialogOptionAttribute option;

            private readonly FieldInfo fieldInfo;

            public DialogOptionField(DialogOptionAttribute option, FieldInfo fieldInfo)
            {
                this.option = option;
                this.fieldInfo = fieldInfo;
            }

            public (string, Action<string>) Build(UIBuilder uiBuilder, T dialog, Action onChange, bool privateEnvironment = false)
            {
                if (option.secret && !privateEnvironment)
                {
                    BuildSecretButton(option.name, uiBuilder, (uiBuilder2) =>
                    {
                        var root = uiBuilder2.Root;
                        BuildEditor(dialog, root, fieldInfo, onChange, uiBuilder2, option, true);
                        var editor = root.GetComponentInChildren<TextEditor>();
                        editor?.Focus();
                    });
                }
                else
                {
                    BuildEditor(dialog, uiBuilder.Current, fieldInfo, onChange, uiBuilder, option);
                }
                return (fieldInfo.Name, (error) => { });
            }
        }

        public DialogBuilder()
        {
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

        public DialogBuilder<T> AddOption(DialogOptionAttribute conf, FieldInfo fieldInfo)
        {
            var v = fieldInfo.FieldType;
            options.Add((IDialogOptionField)typeof(DialogOptionField<>)
                .MakeGenericType(v)
                .GetConstructor(
                    BindingFlags.Public,
                        null,
                        new Type[] {
                        typeof(DialogOptionAttribute),
                        typeof(FieldInfo)
                    },
                    new ParameterModifier[0]
                )
                .Invoke(new object[] { conf, fieldInfo }));
            return this;
        }

        public void BuildInPlace(UIBuilder uiBuilder, T dialog, bool privateEnvironment = false)
        {
            var errorSetters = new Dictionary<string, Action<string>>();
            var knownErrors = new Dictionary<string, string>();
            void onChange()
            {
                var errors = dialog.Validate();
                foreach (var error in errors)
                {
                    Action<string> setter;
                    if (errorSetters.TryGetValue(error.Key, out setter)
                    || errorSetters.TryGetValue(DEFAULT_KEY, out setter)
                    //TODO: allow combining multiple errors into default setter
                    //TODO: make default setter an argument?
                )
                    {
                        setter(error.Value);
                    }
                }
                foreach (var error in knownErrors)
                {
                    Action<string> setter;
                    if (
                        !errors.ContainsKey(error.Key)
                        && (errorSetters.TryGetValue(error.Key, out setter)
                            || errorSetters.TryGetValue(DEFAULT_KEY, out setter))
                    )
                    {
                        setter(null);
                    }
                }
            }
            foreach (var option in options)
            {
                (var key, var setError) = option.Build(uiBuilder, dialog, onChange, privateEnvironment);
            }
        }

        public Slot BuildWindow(/*TODO*/)
        {
            //...
            //BuildInPlace(...)
            //...
        }

        private const string SLOT_NAME_IMPORT = "Document Import Configurator";
        private const string SLOT_NAME_USERSPACE_DIALOG = "Userspace Dialog";
        private const string TITLE_IMPORT_PANEL = "Document Import";
        private const string TITLE_SECRET_EDIT_PANEL = "Edit...";
        private const string LABEL_TEXT_IMPORT = "Import";
        private const string LABEL_TEXT_SKIP = "Skip conversion";
        private const string LABEL_SECRET_EDIT = "Edit";
        private const string LABEL_USERSPACE_DIALOG_CLOSE = "OK";
        private const float CONFIG_PANEL_HEIGHT = 0.25f;
        private const float USERSPACE_PANEL_HEIGHT = 0.15f;
        private static readonly float2 CONFIG_CANVAS_SIZE = new float2(200f, 108f);
        private static readonly float2 USERSPACE_CANVAS_SIZE = new float2(200f, 52f);
        private const string DEFAULT_KEY = "";
        private const float SPACING = 4f;
        private const float BUTTON_HEIGHT = 24f;

        internal static void Spawn(
            AssetClass assetClass,
            IEnumerable<string> files,
            World world,
            float3 position,
            floatQ rotation,
            float3 scale,
            IDialog converter)
        {

            var slot = world.AddSlot(SLOT_NAME_IMPORT, false);
            slot.GlobalPosition = position;
            slot.GlobalRotation = rotation;
            slot.GlobalScale = scale;

            var panel = slot.AttachComponent<NeosCanvasPanel>();
            panel.Panel.Title = TITLE_IMPORT_PANEL;
            panel.Panel.AddCloseButton();
            panel.CanvasSize = CONFIG_CANVAS_SIZE;
            panel.CanvasScale = CONFIG_PANEL_HEIGHT / panel.CanvasSize.y;

            var uiBuilder = new UIBuilder(panel.Canvas);
            uiBuilder.ScrollArea();
            uiBuilder.VerticalLayout(SPACING);

            Button trigger = null;
            BuildConfigFields(converter, slot, uiBuilder, () => UpdateButton(converter, trigger));

            uiBuilder.Style.FlexibleHeight = -1;
            uiBuilder.Style.MinHeight = BUTTON_HEIGHT;
            uiBuilder.Style.PreferredHeight = BUTTON_HEIGHT;
            uiBuilder.Style.ForceExpandWidth = true;

            uiBuilder.HorizontalLayout(SPACING);
            uiBuilder.Style.FlexibleWidth = 1;

            trigger = uiBuilder.Button();
            trigger.LocalPressed += (button, data) =>
            {
                if (converter.ValidateConfig(out var ignored))
                {
                    Conversion.Start(files, converter, world, slot.GlobalPosition, slot.GlobalRotation);
                    slot.Destroy();
                }
            };

            var rawImportTrigger = uiBuilder.Button((LocaleString)LABEL_TEXT_SKIP);
            rawImportTrigger.LocalPressed += (button, data) =>
            {
                NeosDocumentImportMod.skipNext = true;
                UniversalImporter.Import(assetClass, files, world, position, rotation);
                slot.Destroy();
            };

            UpdateButton(converter, trigger);
        }

        private static void UpdateButton(IDialog converter, Button trigger)
        {
            if (trigger != null)
            {
                if (!converter.ValidateConfig(out var msg))
                {
                    trigger.Enabled = false;
                    trigger.Label.Color.Value = color.Red;
                    trigger.LabelText = $"<b>{msg}</b>";
                }
                else
                {
                    trigger.Enabled = true;
                    trigger.Label.Color.Value = color.Black;
                    trigger.LabelText = LABEL_TEXT_IMPORT;
                }
            }
        }

        private static void BuildConfigFields(IDialog converter, Slot slot, UIBuilder uiBuilder, Action onChange)
        {
            var converterType = converter.GetType();

            foreach (var prop in converterType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                foreach (var attr in prop.GetCustomAttributes(true))
                {
                    if (attr is DialogOptionAttribute conf)
                    {
                        if (conf.secret)
                        {
                            BuildSecretButton(prop.Name, uiBuilder, (uiBuilder2) =>
                            {
                                var root = uiBuilder2.Root;
                                BuildEditor(converter, root, prop, onChange, uiBuilder2, conf);
                                root.GetComponentInChildren<TextEditor>()?.Focus();
                            });
                        }
                        else
                        {
                            BuildEditor(converter, slot, prop, onChange, uiBuilder, conf);
                        }
                        break;
                    }
                }
            }
        }

        private static void BuildSecretButton(string name, UIBuilder uiBuilder, Action<UIBuilder> createFields)
        {
            uiBuilder.PushStyle();
            uiBuilder.Style.MinHeight = 24f;

            uiBuilder.Panel();

            Text text2 = uiBuilder.Text(name + ":", bestFit: true, Alignment.MiddleLeft, parseRTF: false);
            text2.Color.Value = color.Black;
            uiBuilder.CurrentRect.AnchorMax.Value = new float2(0.25f, 1f);

            Button button = uiBuilder.Button(LABEL_SECRET_EDIT);
            uiBuilder.CurrentRect.AnchorMin.Value = new float2(0.25f);
            button.LocalPressed += (b, d) =>
            {
                CreateUserSpacePopup(TITLE_SECRET_EDIT_PANEL, createFields);
            };

            uiBuilder.NestOut();
            uiBuilder.PopStyle();
        }

        private static void CreateUserSpacePopup(string title, Action<UIBuilder> createFields)
        {
            Userspace.UserspaceWorld.RunSynchronously(() =>
            {
                Slot slot = Userspace.UserspaceWorld.AddSlot(SLOT_NAME_USERSPACE_DIALOG, persistent: false);
                var panel = slot.AttachComponent<NeosCanvasPanel>();
                panel.Panel.Title = title;
                panel.CanvasSize = USERSPACE_CANVAS_SIZE;
                panel.CanvasScale = USERSPACE_PANEL_HEIGHT / panel.CanvasSize.y;

                var uiBuilder = new UIBuilder(panel.Canvas);
                uiBuilder.ScrollArea();
                uiBuilder.VerticalLayout(SPACING);

                createFields(uiBuilder);

                uiBuilder.Style.FlexibleHeight = -1;
                uiBuilder.Style.MinHeight = BUTTON_HEIGHT;
                uiBuilder.Style.PreferredHeight = BUTTON_HEIGHT;

                Button trigger = uiBuilder.Button(LABEL_USERSPACE_DIALOG_CLOSE);
                trigger.LocalPressed += (button, data) =>
                {
                    slot.Destroy();
                };

                slot.PositionInFrontOfUser(float3.Backward);
            });
        }

        private static void BuildEditor(object valueObj, Slot ifieldSlot, FieldInfo prop, Action onChange, UIBuilder uiBuilder, DialogOptionAttribute conf, bool privateEnvironment = false)
        {
            SyncMemberEditorBuilder.Build(
                BuildField(valueObj, ifieldSlot, prop, onChange),
                conf.name,
                prop,
                uiBuilder
            );
            if (conf.secret && !privateEnvironment && uiBuilder.Current.ChildrenCount > 0)
            {
                Slot added = uiBuilder.Current[uiBuilder.Current.ChildrenCount - 1];
                added.ForeachComponentInChildren<TextField>(textField =>
                    {
                        var patternField = textField.Text?.MaskPattern;
                        if (patternField != null)
                        {
                            patternField.Value = "*";
                        }
                    }
                );
            }
        }


        private static IField BuildField(object valueObj, Slot ifieldSlot, FieldInfo prop, Action onChange)
        {
            return (IField)FieldBuilder(prop)
                ?.Invoke(null, new object[] { ifieldSlot, valueObj, prop, onChange });
        }

        private static MethodInfo FieldBuilder(FieldInfo prop)
        {
            const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Static;
            if (typeof(IWorldElement).IsAssignableFrom(prop.FieldType))
            {
                return typeof(DialogBuilder<T>)
                    .GetGenericMethod(nameof(BuildReferenceField), FLAGS, prop.FieldType);
            }
            else
            {
                return typeof(DialogBuilder<T>)
                    .GetGenericMethod(nameof(BuildValueField), FLAGS, prop.FieldType);
            }
        }

        private static IField BuildValueField<V>(Slot slot, object obj, FieldInfo prop, Action onChange)
        {
            var value = slot.AttachComponent<ValueField<V>>().Value;
            value.Value = (V)prop.GetValue(obj);
            value.OnValueChange += (x) =>
            {
                prop.SetValue(obj, x.Value);
                onChange();
            };
            return value;
        }

        private static IField BuildReferenceField<V>(Slot slot, object obj, FieldInfo prop, Action onChange) where V : class, IWorldElement
        {
            var value = slot.AttachComponent<ReferenceField<V>>().Reference;
            value.Target = (V)prop.GetValue(obj);
            value.OnReferenceChange += (x) =>
            {
                prop.SetValue(obj, x.Target);
                onChange();
            };
            return value;
        }
    }
}