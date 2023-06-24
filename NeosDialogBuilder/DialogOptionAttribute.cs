using System;

namespace NeosDialogBuilder
{
    /// <summary>
    /// Attributes fields representing a configuration option
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class DialogOptionAttribute : Attribute
    {
        public readonly string name;
        public readonly bool secret;

        /// <summary>
        /// Creates a configuration line in the import configurator
        /// </summary>
        /// <param name="name">Display name of the option</param>
        /// <param name="type">Type of the option</param>
        public DialogOptionAttribute(string name, bool secret = false)
        {
            this.name = name;
            this.secret = secret;
        }
    }
}
