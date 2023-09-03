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
            uiBuilder.VerticalLayout(spacing: NeosDialogBuilderMod.SPACING / 2);
            if (conf.secret && !inUserspace)
            {
                var secretDialog = new SecretDialog(this, dialog, onChange);
                StaticBuildFunctions.BuildSecretButton(conf.name, uiBuilder, () => secretDialog.Open());
            }
            else
            {
                StaticBuildFunctions.BuildEditor(uiBuilder.Root, dialog, fieldInfo, () => onChange(), uiBuilder, conf);
            }
            if (conf.showErrors)
            {
                uiBuilder.PushStyle();
                uiBuilder.Style.PreferredHeight = NeosDialogBuilderMod.ERROR_HEIGHT;
                uiBuilder.Style.TextColor = color.Red;
                var errorText = uiBuilder.Text("", alignment: Alignment.TopRight);
                uiBuilder.PopStyle();
                uiBuilder.NestOut();
                var key = fieldInfo.Name;
                return (new List<string>() { key }, (allErrors, unboundErrors) =>
                    {
                        errorText.Content.Value = allErrors.TryGetValue(key, out var error)
                                ? $"<b>{error}</b>"
                                : "";
                    }
                );
            }
            else
            {
                return (new List<string>(), (allErrors, unboundErrors) => { });
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
                this.dialogBuilder = new DialogBuilder<T>(addDefaults: false, overrideUpdateAndValidate: (_) => onChangeSource())
                        .AddEntry(option)
                        .AddEntry(new DialogActionDefinition<T>(
                            new DialogActionAttribute(NeosDialogBuilderMod.LABEL_USERSPACE_DIALOG_CLOSE, isValidated: false),
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
        }
    }
}