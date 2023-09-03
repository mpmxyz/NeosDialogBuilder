using FrooxEngine.UIX;
using System;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Defines a line of horizontally arranged sub-elements
    /// </summary>
    /// <typeparam name="T">type of the dialog object</typeparam>
    public class DialogLineDefinition<T> : IDialogEntryDefinition<T> where T : IDialogState
    {
        private readonly IEnumerable<IDialogEntryDefinition<T>> elements;

        /// <summary>
        /// Creates a line of sub-elements
        /// </summary>
        /// <param name="elements"></param>
        public DialogLineDefinition(IEnumerable<IDialogEntryDefinition<T>> elements)
        {
            this.elements = elements;
        }

        public IDialogElement
            Create(UIBuilder uiBuilder, T dialog, Func<(IDictionary<object, string>, IDictionary<object, string>)> onChange, bool inUserspace = false)
        {
            var allErrorSetters = new List<Action<IDictionary<object, string>, IDictionary<object, string>>>();
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