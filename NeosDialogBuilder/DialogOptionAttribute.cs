using System;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Attributes fields representing a dialog option
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DialogOptionAttribute : Attribute
    {
        public readonly string name;
        public readonly bool secret;
        public readonly bool showErrors;

        /// <summary>
        /// Creates an option line in the dialog
        /// </summary>
        /// <param name="name">Display name of the option</param>
        /// <param name="secret">True makes the user edit this in userspace</param>
        public DialogOptionAttribute(string name, bool secret = false, bool showErrors = true)
        {
            this.name = name;
            this.secret = secret;
            this.showErrors = showErrors;
        }
    }
}
