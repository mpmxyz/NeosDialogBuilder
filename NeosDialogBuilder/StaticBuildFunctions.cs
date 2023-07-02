﻿using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Reflection;
using static NeosDialogBuilder.NeosDialogBuilderMod;

namespace NeosDialogBuilder
{
    internal class StaticBuildFunctions
    {

        internal static void BuildEditor(Slot ifieldSlot, object valueObj, FieldInfo prop, Action onChange, UIBuilder uiBuilder, DialogOptionAttribute conf, bool privateEnvironment = false)
        {
            if (ifieldSlot == null)
            {
                throw new ArgumentNullException(nameof(ifieldSlot));
            }
            if (valueObj == null)
            {
                throw new ArgumentNullException(nameof(valueObj));
            }
            if (prop == null)
            {
                throw new ArgumentNullException(nameof(prop));
            }

            SyncMemberEditorBuilder.Build(
                BuildField(ifieldSlot, valueObj, prop, onChange),
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
                            patternField.Value = SECRET_TEXT_PATTERN;
                        }
                    }
                );
            }
        }

        internal static void BuildSecretButton(string name, UIBuilder uiBuilder, Action<UIBuilder> createFields)
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
                Slot slot = Userspace.UserspaceWorld.AddSlot(TITLE_SECRET_EDIT_PANEL, persistent: false);
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

        private static IField BuildField(Slot ifieldSlot, object valueObj, FieldInfo prop, Action onChange)
        {
            return (IField)FieldBuilder(prop)
                ?.Invoke(null, new object[] { ifieldSlot, valueObj, prop, onChange });
        }

        private static MethodInfo FieldBuilder(FieldInfo prop)
        {
            const BindingFlags FLAGS = BindingFlags.NonPublic | BindingFlags.Static;
            if (typeof(IWorldElement).IsAssignableFrom(prop.FieldType))
            {
                return typeof(StaticBuildFunctions)
                    .GetGenericMethod(nameof(BuildReferenceField), FLAGS, prop.FieldType);
            }
            else
            {
                return typeof(StaticBuildFunctions)
                    .GetGenericMethod(nameof(BuildValueField), FLAGS, prop.FieldType);
            }
        }

        private static IField BuildReferenceField<V>(Slot slot, object obj, FieldInfo prop, Action onChange) where V : class, IWorldElement
        {
            UniLog.Log($"{typeof(V)} {slot?.Name} {obj} {prop?.Name}");
            var value = slot.AttachComponent<ReferenceField<V>>().Reference;
            UniLog.Log(value);
            value.Target = (V)prop.GetValue(obj);
            value.OnTargetChange += (x) =>
            {
                UniLog.Log($"RefChange {x.Target} {x.Value}");
                UniLog.Log($"Types {x.Target?.GetType()} {typeof(V)} {prop.FieldType}");
                prop.SetValue(obj, x.Target);
                UniLog.Log($"now: {prop.GetValue(obj)}");
                onChange();
                UniLog.Log($"Finished");
            };
            return value;
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


    }
}
