using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeosDialogBuilder
{
    public class DialogActionMethod<T> where T : IDialog
    {
        private readonly DialogActionAttribute conf;

        private readonly Action<T> action;

        public DialogActionMethod(DialogActionAttribute conf, Action<T> action)
        {
            this.conf = conf;
            this.action = action;
        }

        public Action<IDictionary<string, string>> Build(UIBuilder uiBuilder, T dialog)
        {
            Button button = uiBuilder.Button(conf.name);
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
                if (!conf.isValidated || isEnabled(dialog.Validate()))
                {
                    action(dialog);
                }
            };
            return (errors) => button.World?.RunSynchronously(() => button.Enabled = isEnabled(errors));
        }
    }
}