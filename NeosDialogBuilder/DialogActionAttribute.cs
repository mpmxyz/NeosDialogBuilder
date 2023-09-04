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
        public readonly object[] onlyValidating;

        /// <summary>
        /// Creates an action button in the dialog
        /// </summary>
        /// <param name="name">Display name of the action</param>
        /// <param name="onlyValidating">explicitly selects keys to validate for</param>
        public DialogActionAttribute(string name, object[] onlyValidating = null)
        {
            this.name = name;
            this.onlyValidating = onlyValidating;
        }
    }
}
