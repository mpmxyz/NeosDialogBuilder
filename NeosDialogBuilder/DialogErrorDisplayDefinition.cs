using BaseX;
using FrooxEngine;
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
    {//TODO: add filter/binding
        private readonly object key;
        private readonly bool onlyUnbound;
        private readonly int nLines;

        /// <summary>
        /// Creates an error display definition
        /// </summary>
        /// <param name="onlyUnbound">true to only display errors that are not already displayed at an dialog option</param>
        /// <param name="nLines">height of the display in lines</param>
        public DialogErrorDisplayDefinition(object key, bool onlyUnbound, int nLines = 2)
        {
            this.key = key;
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
            return new Element(key, text.Slot, text, onlyUnbound);
        }

        private class Element : DialogElementBase
        {
            private readonly object _Key;
            private readonly Slot _Slot;
            private readonly Text _Text;
            private readonly bool _OnlyUnbound;

            public Element(object key, Slot slot, Text text, bool onlyUnbound)
            {
                _Key = key;
                _Slot = slot;
                _Text = text;
                _OnlyUnbound = onlyUnbound;
            }

            public override object Key => _Key;

            public override IEnumerable<object> BoundErrorKeys => new List<object>();

            public override bool Visible
            {
                get => _Slot.ActiveSelf;
                set => _Slot.ActiveSelf = value;
            }
            internal override bool EffectivelyEnabled {
                set { }
            }

            public override void DisplayErrors(IDictionary<object, string> allErrors, IDictionary<object, string> unboundErrors)
            {
                IDictionary<object, string> displayedErrors = _OnlyUnbound ? unboundErrors : allErrors;
                _Text.Content.Value = $"<b>{string.Join("\n", displayedErrors.Values)}</b>";
            }

            public override void Reset()
            {
                
            }
        }
    }
}