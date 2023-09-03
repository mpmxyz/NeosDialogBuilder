using BaseX;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Definition of a text output that displays validation errors.
    /// </summary>
    /// <typeparam name="T">type of the dialog object</typeparam>
    public class DialogErrorDisplayDefinition<T> : IDialogEntryDefinition<T> where T : IDialogState
    {
        private readonly bool onlyUnbound;
        private readonly int nLines;

        /// <summary>
        /// Creates an error display definition
        /// </summary>
        /// <param name="onlyUnbound">true to only display errors that are not already displayed at an dialog option</param>
        /// <param name="nLines">height of the display in lines</param>
        public DialogErrorDisplayDefinition(bool onlyUnbound, int nLines = 2)
        {
            this.onlyUnbound = onlyUnbound;
            this.nLines = nLines;
        }

        public IDialogElement
            Create(UIBuilder uiBuilder, T dialog, Func<(IDictionary<object, string>, IDictionary<object, string>)> onChange, bool inUserspace = false)
        {
            uiBuilder.PushStyle();
            uiBuilder.Style.PreferredHeight = NeosDialogBuilderMod.ERROR_HEIGHT * nLines;
            uiBuilder.Style.TextColor = color.Red;
            Text text = uiBuilder.Text("", alignment: Alignment.MiddleRight);
            uiBuilder.PopStyle();
            return (new List<string>(), (errors, unboundErrors) =>
                {
                    IDictionary<string, string> displayedErrors = onlyUnbound ? unboundErrors : errors;
                    text.Content.Value = $"<b>{string.Join("\n", displayedErrors.Values)}</b>";
                }
            );
        }
    }
}