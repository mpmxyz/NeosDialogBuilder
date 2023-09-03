using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Reflection;
using static NeosDialogBuilder.NeosDialogBuilderMod;

namespace NeosDialogBuilder
{
    internal class StaticBuildFunctions
    {
        private class MappedValue<I, O>
        {
            internal O OValue;
            internal I IValue
            {
                get
                {
                    if (mapper.TryUnmap(OValue, out var iVal))
                    {
                        return iVal;
                    }
                    else
                    {
                        return default;
                    }
                }
                set
                {
                    if (mapper.TryMap(value, out var oVal))
                    {
                        OValue = oVal;
                    }
                }
            }
            private readonly IReversibleMapper<I, O> mapper;

            public MappedValue(I value, IReversibleMapper<I, O> mapper)
            {
                this.mapper = mapper;
                this.IValue = value;
            }
        }

        internal static void BuildEditor(Slot ifieldSlot, object valueObj, FieldInfo prop, Action onChange, UIBuilder uiBuilder, DialogOptionAttribute conf)
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
            if (onChange == null)
            {
                throw new ArgumentNullException(nameof(onChange));
            }
            if (uiBuilder == null)
            {
                throw new ArgumentNullException(nameof(uiBuilder));
            }
            if (conf == null)
            {
                throw new ArgumentNullException(nameof(conf));
            }

            if (conf.toNeosMapper != null)
            {
                ApplyMapping(conf.toNeosMapper, ref valueObj, ref prop, ref onChange);
            }
            SyncMemberEditorBuilder.Build(
                BuildField(ifieldSlot, valueObj, prop, onChange),
                conf.name,
                prop,
                uiBuilder
            );
            if (conf.secret && uiBuilder.Current.ChildrenCount > 0)
            {
                Slot added = uiBuilder.Current;
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

        private static void ApplyMapping(Type reversibleMapper, ref object valueObj, ref FieldInfo prop, ref Action onChange)
        {
            //save original values to create adapters
            object originalObj = valueObj;
            FieldInfo originalProp = prop;
            Action originalOnChange = onChange;

            //create adapters
            Type[] mappingTypes = reversibleMapper.GetGenericArgumentsFromInterface(typeof(IReversibleMapper<,>));
            if (mappingTypes == null)
            {
                throw new ArgumentException("Expected implementation of " + typeof(IReversibleMapper<,>).Name, nameof(DialogOptionAttribute.toNeosMapper));
            }
            Type adaptedType = typeof(MappedValue<,>).MakeGenericType(mappingTypes);
            FieldInfo adaptedProp = adaptedType.GetField(nameof(MappedValue<object, object>.OValue), BindingFlags.NonPublic | BindingFlags.Instance);
            PropertyInfo iValueProp = adaptedType.GetProperty(nameof(MappedValue<object, object>.IValue), BindingFlags.NonPublic | BindingFlags.Instance);
            object mapperInstance = reversibleMapper.GetConstructor(new Type[0]).Invoke(new object[0]);
            object adaptedObj = adaptedType
                .GetConstructor(new Type[] { mappingTypes[0], typeof(IReversibleMapper<,>).MakeGenericType(mappingTypes) })
                .Invoke(new object[] { originalProp.GetValue(originalObj), mapperInstance});
            void adaptedOnChange()
            {
                object newValue = iValueProp.GetValue(adaptedObj);
                UniLog.Log(newValue);
                originalProp.SetValue(originalObj, newValue);
                originalOnChange();
            }

            //replace original values with adapters
            valueObj = adaptedObj;
            prop = adaptedProp;
            onChange = adaptedOnChange;
        }

        internal static void BuildSecretButton(string name, UIBuilder uiBuilder, Action onClick)
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
                onClick();
            };

            uiBuilder.NestOut();
            uiBuilder.PopStyle();
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
            var value = slot.AttachComponent<ReferenceField<V>>().Reference;
            value.Target = (V)prop.GetValue(obj);
            value.OnTargetChange += (x) =>
            {
                prop.SetValue(obj, x.Target);
                onChange();
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
