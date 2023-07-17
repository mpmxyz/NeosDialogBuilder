using BaseX;
using FrooxEngine;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Defines a simple button with an action
    /// </summary>
    /// <typeparam name="T">type of the expected dialog object</typeparam>
    public class DialogAction<T> : IDialogEntryDefinition<T> where T : IDialog
    {
        private readonly DialogActionAttribute conf;

        private readonly Action<T> action;

        /// <summary>
        /// Creates an action definition
        /// </summary>
        /// <param name="conf">displayed text and validation behaviour</param>
        /// <param name="action">Action that is triggered when pressing the button</param>
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