using FrooxEngine;
using FrooxEngine.UIX;
using System.Reflection;
using System;
using BaseX;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Defines a configuration option in the dialog window
    /// </summary>
    /// <typeparam name="T">type of the dialog object</typeparam>
    /// <typeparam name="V">type of the edited value</typeparam>
    public class DialogOptionDefinition<T, V> : IDialogEntryDefinition<T> where T : IDialogState
    {
        private readonly DialogOptionAttribute conf;

        private readonly FieldInfo fieldInfo;

        /// <summary>
        /// Creates a configuration option
        /// </summary>
        /// <param name="conf">displayed name, secrecy and error output options</param>
        /// <param name="fieldInfo">field of type <typeparamref name="T"/> which will be edited</param>
        public DialogOptionDefinition(DialogOptionAttribute conf, FieldInfo fieldInfo)
        {
            this.conf = conf;
            this.fieldInfo = fieldInfo;
        }

        public IDialogElement
            Create(UIBuilder uiBuilder, T dialog, Func<(IDictionary<object, string>, IDictionary<object, string>)> onChange, bool inUserspace = false)
        {
            var slot = uiBuilder.VerticalLayout(spacing: NeosDialogBuilderMod.SPACING / 2).Slot;
            IValue<string> errorTextContent;
            Action reset;

            if (conf.secret && !inUserspace)
            {
                //TODO: ensure proper reset functionality
                var secretDialog = new SecretDialog(this, dialog, onChange);
                reset = secretDialog.Reset;
                StaticBuildFunctions.BuildSecretButton(conf.name, uiBuilder, () => secretDialog.Open());
            }
            else
            {
                reset = StaticBuildFunctions.BuildEditor(uiBuilder.Root, dialog, fieldInfo, () => onChange(), uiBuilder, conf);
            }
            var key = fieldInfo.Name;
            if (conf.showErrors)
            {
                uiBuilder.PushStyle();
                uiBuilder.Style.PreferredHeight = NeosDialogBuilderMod.ERROR_HEIGHT;
                uiBuilder.Style.TextColor = color.Red;
                var errorText = uiBuilder.Text("", alignment: Alignment.TopRight);
                uiBuilder.PopStyle();
                uiBuilder.NestOut();
                errorTextContent = errorText.Content;
            }
            else
            {
                errorTextContent = null;
            }
            return new Element(key, slot, errorTextContent, reset);
        }

        private class Element : DialogElementBase
        {
            private readonly object _Key;
            private readonly Slot _Slot;
            private readonly IValue<string> _ErrorField;
            private readonly Action _Reset;

            public Element(object key, Slot slot, IValue<string> errorField, Action reset)
            {
                _Key = key;
                _Slot = slot;
                _ErrorField = errorField;
                _Reset = reset;
            }

            public override object Key => _Key;

            public override IEnumerable<object> BoundErrorKeys => new List<object>(new object[] { _Key });

            public override bool Visible
            { 
                get => _Slot.ActiveSelf; 
                set => _Slot.ActiveSelf = value;
            }
            internal override bool EffectivelyEnabled
            {
                set => _Slot.GetComponentsInChildren<InteractionElement>().ForEach(it => it.Enabled = value);
            }

            public override void DisplayErrors(IDictionary<object, string> allErrors, IDictionary<object, string> unboundErrors)
            {
                if (_ErrorField != null)
                {
                    _ErrorField.Value = allErrors.TryGetValue(_Key, out var error)
                                    ? $"<b>{error}</b>"
                                    : "";
                }
            }

            public override void Reset()
            {
                _Reset();
            }
        }

        /// <summary>
        /// represents a dialog that edits a single value in userspace
        /// </summary>
        private class SecretDialog
        {
            private readonly DialogBuilder<T> dialogBuilder;
            private readonly string title;
            private readonly T dialog;
            private Slot slot = null;

            public SecretDialog(DialogOptionDefinition<T, V> option, T dialog, Func<(IDictionary<object, string>, IDictionary<object, string>)> onChangeSource)
            {
                //TODO: Dialog binding to IDialogState must be adjusted to cater for popups like this (potential target: condition/config in builder)
                this.dialogBuilder = new DialogBuilder<T>(addDefaults: false, overrideUpdateAndValidate: (_) => onChangeSource())
                        .AddEntry(option)
                        .AddEntry(new DialogActionDefinition<T>(
                            null,
                            new DialogActionAttribute(NeosDialogBuilderMod.LABEL_USERSPACE_DIALOG_CLOSE, onlyValidating: new object[0]),
                            (x) => Close()
                            ));
                this.title = option.conf.name;
                this.dialog = dialog;
            }

            public void Open()
            {
                Userspace.UserspaceWorld.RunSynchronously(() =>
                {
                    slot?.Destroy();
                    slot = dialogBuilder.BuildWindow(title, Userspace.UserspaceWorld, dialog);
                    var editor = slot.GetComponentInChildren<TextEditor>();
                    editor?.Focus();
                });
            }

            public void Close()
            {
                Userspace.UserspaceWorld.RunSynchronously(() =>
                {
                    slot?.Destroy();
                    slot = null;
                });
            }

            public void Reset()
            {

            }
        }
    }
}