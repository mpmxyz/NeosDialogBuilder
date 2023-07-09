using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeosDialogBuilder
{
    public class DialogAction<T> : IDialogEntryDefinition<T> where T : IDialog
    {
        private readonly DialogActionAttribute conf;

        private readonly Action<T> action;

        public DialogAction(DialogActionAttribute conf, Action<T> action)
        {
            this.conf = conf;
            this.action = action;
        }

        public (IEnumerable<string>, Action<IDictionary<string, string>, IDictionary<string, string>>)
            Create(UIBuilder uiBuilder, T dialog, Func<(IDictionary<string, string>, IDictionary<string, string>)> onChange, bool inUserspace = false)
        {
            uiBuilder.PushStyle();
            uiBuilder.Style.PreferredHeight = NeosDialogBuilderMod.BUTTON_HEIGHT;
            Button button = uiBuilder.Button(conf.name);
            uiBuilder.PopStyle();
            bool isEnabled(IDictionary<string, string> errors)
            {
                if (conf.isValidated)
                {
                    if (conf.onlyValidating == null)
                    {
                        return !errors.Any();
                    }
                    else
                    {
                        return !conf.onlyValidating.Any(errors.ContainsKey);
                    }
                }
                else
                {
                    return true;
                }
            }
            button.LocalPressed += (IButton b, ButtonEventData bed) =>
            {
                //"unnecessary" conf check to avoid running Validate
                if (!conf.isValidated || isEnabled(dialog.UpdateAndValidate()))
                {
                    action(dialog);
                }
            };
            return (new List<string>(), (errors, unboundErrors) => button.Enabled = isEnabled(errors));
        }
    }
}