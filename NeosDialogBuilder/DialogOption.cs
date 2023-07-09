﻿using FrooxEngine;
using FrooxEngine.UIX;
using System.Reflection;
using System;
using BaseX;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    public class DialogOption<T, V> : IDialogEntryDefinition<T> where T : IDialog
    {
        private readonly DialogOptionAttribute conf;

        private readonly FieldInfo fieldInfo;

        public DialogOption(DialogOptionAttribute conf, FieldInfo fieldInfo)
        {
            this.conf = conf;
            this.fieldInfo = fieldInfo;
        }

        public (IEnumerable<string>, Action<IDictionary<string, string>, IDictionary<string, string>>)
            Create(UIBuilder uiBuilder, T dialog, Func<(IDictionary<string, string>, IDictionary<string, string>)> onChange, bool inUserspace = false)
        {
            uiBuilder.VerticalLayout(spacing: NeosDialogBuilderMod.SPACING / 2);
            if (conf.secret && !inUserspace)
            {
                var secretDialog = new SecretDialog(this, dialog, onChange);
                StaticBuildFunctions.BuildSecretButton(fieldInfo.Name, uiBuilder, () => secretDialog.Open());
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

        private class SecretDialog
        {
            private readonly DialogBuilder<T> dialogBuilder;
            private readonly string title;
            private readonly T dialog;
            private Slot slot = null;

            public SecretDialog(DialogOption<T, V> option, T dialog, Func<(IDictionary<string, string>, IDictionary<string, string>)> onChangeSource)
            {
                this.dialogBuilder = new DialogBuilder<T>(addDefaults: false, customUpdateAndValidate: onChangeSource)
                        .AddEntry(option)
                        .AddEntry(new DialogAction<T>(
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