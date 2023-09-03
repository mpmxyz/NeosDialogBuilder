using System.Collections.Generic;

namespace NeosDialogBuilder
{

    /// <summary>
    /// Defines the behaviour of a dialog
    /// </summary>
    public interface IDialogState
    {
        /// <summary>
        /// The dialog this state has been bound to (only required to be assignable once)
        /// </summary>
        Dialog Dialog {set; }

        /// <summary>
        /// Updates internal state and checks for errors
        /// </summary>
        /// <returns>a mapping from field name to the associated error,
        /// disables the validated buttons if non-empty</returns>
        IDictionary<object, string> UpdateAndValidate();

        /// <summary>
        /// Called when the dialog is in the process of being destroyed (e.g. via the X button)
        /// </summary>
        void OnDestroy();
    }
}
