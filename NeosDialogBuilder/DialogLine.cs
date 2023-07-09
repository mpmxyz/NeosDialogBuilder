using FrooxEngine.UIX;
using System;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    public class DialogLine<T> : IDialogEntryDefinition<T> where T : IDialog
    {
        private readonly IEnumerable<IDialogEntryDefinition<T>> entries;

        public DialogLine(IEnumerable<IDialogEntryDefinition<T>> entries)
        {
            this.entries = entries;
        }

        public (IEnumerable<string>, Action<IDictionary<string, string>, IDictionary<string, string>>)
            Create(UIBuilder uiBuilder, T dialog, Func<(IDictionary<string, string>, IDictionary<string, string>)> onChange, bool inUserspace = false)
        {
            var allErrorSetters = new List<Action<IDictionary<string, string>, IDictionary<string, string>>>();
            var allErrors = new HashSet<string>();

            uiBuilder.PushStyle();
            uiBuilder.HorizontalLayout(spacing: NeosDialogBuilderMod.SPACING);
            uiBuilder.Style.FlexibleWidth = 1;
            uiBuilder.Style.ForceExpandWidth = true;
            foreach (var entry in entries)
            {
                (var errors, var errorSetter) = entry.Create(uiBuilder, dialog, onChange, inUserspace);
                allErrorSetters.Add(errorSetter);
                foreach (var error in errors)
                {
                    allErrors.Add(error);
                }
            }
            uiBuilder.NestOut();
            uiBuilder.PopStyle();
            return (allErrors, (errors, unboundErrors) =>
            {
                foreach (var errorSetter in allErrorSetters)
                {
                    errorSetter(errors, unboundErrors);
                }
            }
            );
        }
    }
}