
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    public interface IDialogElement
    {
        object Key { get; }
        IEnumerable<object> BoundErrorKeys { get; }

        bool ParentEnabled { set; }
        bool Enabled { get; set; }
        bool Visible { get; set; }

        /*setting World state to dialog state*/
        void Reset(); //TODO: redesign to allow partial resets without O(n²)
        void DisplayErrors(IDictionary<object, string> allErrors, IDictionary<object, string> unboundErrors);
    }
}
