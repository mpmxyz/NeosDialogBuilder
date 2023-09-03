using FrooxEngine;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    public class Dialog : DialogElementContainer
    {
        private readonly IDialogState state;

        internal Dialog(IDialogState state, object key, Slot root, IEnumerable<IDialogElement> elements) : base(key, root, elements)
        {
            this.state = state;
            state.Dialog = this;
        }


    }
}
