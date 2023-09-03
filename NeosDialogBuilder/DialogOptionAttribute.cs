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
        public readonly Type toNeosMapper;

        /// <summary>
        /// Creates an option line in the dialog
        /// </summary>
        /// <param name="name">Display name of the option</param>
        /// <param name="secret">makes the user edit this in userspace</param>
        /// <param name="showErrors">causes some space below the input to be reserved for error messages.</param>
        /// <param name="toNeosMapper">allows editing custom types using an in-world representation using a non-custom type</param>
        public DialogOptionAttribute(string name, bool secret = false, bool showErrors = true, Type toNeosMapper = null)
        {
            this.name = name;
            this.secret = secret;
            this.showErrors = showErrors;
            this.toNeosMapper = toNeosMapper;
        }
    }
}
