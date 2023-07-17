using FrooxEngine.UIX;
using System;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Defines a line of horizontally arranged sub-elements
    /// </summary>
    /// <typeparam name="T">type of the dialog object</typeparam>
    public class DialogLine<T> : IDialogEntryDefinition<T> where T : IDialog
    {
        private readonly IEnumerable<IDialogEntryDefinition<T>> elements;

        /// <summary>
        /// Creates a line of sub-elements
        /// </summary>
        /// <param name="elements"></param>
        public DialogLine(IEnumerable<IDialogEntryDefinition<T>> elements)
        {
            this.elements = elements;
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
            foreach (var entry in elements)
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