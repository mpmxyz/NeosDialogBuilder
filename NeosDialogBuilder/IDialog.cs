using System.Collections.Generic;

namespace NeosDialogBuilder
{

    /// <summary>
    /// Defines the behaviour of a dialog
    /// </summary>
    public interface IDialog
    {
        /// <summary>
        /// Checks if Accept can be called
        /// </summary>
        /// <returns>a mapping from field name to the associated error,
        /// disables the accept button if non-empty</returns>
        IDictionary<string, string> Validate();

        /// <summary>
        /// Called when the dialog has been closed (e.g. via the X button)
        /// </summary>
        void OnClose();//TODO: put on Slot.onDestroy or similar
    }
}
