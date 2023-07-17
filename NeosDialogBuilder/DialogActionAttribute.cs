using System;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Attributes methods representing a dialog action (no arguments)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DialogActionAttribute : Attribute
    {
        public readonly string name;
        public readonly bool isValidated;
        public readonly string[] onlyValidating;

        /// <summary>
        /// Creates an action button in the dialog
        /// </summary>
        /// <param name="name">Display name of the action</param>
        /// <param name="isValidated">Disables this button when validation fails</param>
        /// <param name="onlyValidating">explicitly selects keys to validate for</param>
        public DialogActionAttribute(string name, bool isValidated = true, string[] onlyValidating = null)
        {
            this.name = name;
            this.isValidated = isValidated;
            this.onlyValidating = onlyValidating;
        }
    }
}
