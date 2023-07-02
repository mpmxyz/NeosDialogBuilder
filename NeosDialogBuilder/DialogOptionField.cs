using FrooxEngine;
using FrooxEngine.UIX;
using System.Reflection;
using System;
using BaseX;

namespace NeosDialogBuilder
{
    public class DialogOptionField<T, V> : IDialogOptionField<T> where T : IDialog
    {
        private readonly DialogOptionAttribute conf;

        private readonly FieldInfo fieldInfo;

        public DialogOptionField(DialogOptionAttribute conf, FieldInfo fieldInfo)
        {
            this.conf = conf;
            this.fieldInfo = fieldInfo;
        }

        public (string, Action<string>) Build(UIBuilder uiBuilder, T dialog, Action onChange, bool privateEnvironment = false)
        {
            uiBuilder.VerticalLayout(spacing: NeosDialogBuilderMod.SPACING / 2);
            if (conf.secret && !privateEnvironment)
            {
                StaticBuildFunctions.BuildSecretButton(conf.name, uiBuilder, (uiBuilder2) =>
                {
                    var root = uiBuilder2.Root;
                    StaticBuildFunctions.BuildEditor(root, dialog, fieldInfo, onChange, uiBuilder2, conf, true);
                    var editor = root.GetComponentInChildren<TextEditor>();
                    editor?.Focus();
                });
            }
            else
            {
                StaticBuildFunctions.BuildEditor(uiBuilder.Root, dialog, fieldInfo, onChange, uiBuilder, conf);
            }
            if (conf.showErrors)
            {
                uiBuilder.PushStyle();
                uiBuilder.Style.PreferredHeight = NeosDialogBuilderMod.ERROR_HEIGHT;
                uiBuilder.Style.TextColor = color.Red;
                var errorText = uiBuilder.Text("", alignment: Alignment.TopRight);
                uiBuilder.PopStyle();
                uiBuilder.NestOut();
                return (fieldInfo.Name, (error) =>
                    {
                        errorText.World.RunSynchronously(() => errorText.Content.Value = $"<b>{error}</b>");
                    }
                );
            }
            else
            {
                return (fieldInfo.Name, (error) => { });
            }
        }
    }
}