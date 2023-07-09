using BaseX;
using FrooxEngine.UIX;
using System;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    public class DialogErrorDisplay<T> : IDialogEntryDefinition<T> where T : IDialog
    {
        private readonly bool onlyUnbound;
        private readonly int nLines;

        public DialogErrorDisplay(bool onlyUnbound, int nLines = 2)
        {
            this.onlyUnbound = onlyUnbound;
            this.nLines = nLines;
        }

        public (IEnumerable<string>, Action<IDictionary<string, string>, IDictionary<string, string>>)
            Create(UIBuilder uiBuilder, T dialog, Func<(IDictionary<string, string>, IDictionary<string, string>)> onChange, bool inUserspace = false)
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