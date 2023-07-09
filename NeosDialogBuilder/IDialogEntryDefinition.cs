using FrooxEngine.UIX;
using System;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    /// <summary>
    /// An object specifying a dialog entry
    /// </summary>
    /// <typeparam name="T">type of the dialog object</typeparam>
    public interface IDialogEntryDefinition<in T> where T : IDialog
    {
        /// <summary>
        /// Creates a dialog entry
        /// </summary>
        /// <param name="uiBuilder">ui builder to build entry with; on method exit it has to have the same nesting level as during method call</param>
        /// <param name="dialog">object this dialog is based on, may be used to target getters/setters/methods</param>
        /// <param name="onChange">can be triggered by the ui to signal reevaluation of visibility/validity</param>
        /// <param name="inUserspace">signals if dialog is created in userspace</param>
        /// <returns>displayed error keys and error setter<br/>
        /// The first argument of the error setter is the result of the <see cref="IDialog.UpdateAndValidate"/> method.<br/>
        /// The second argument of the error setter is the subset of the first that does not have any matching dialog entries.</returns>
        (IEnumerable<string>, Action<IDictionary<string, string>, IDictionary<string, string>>)
            Create(UIBuilder uiBuilder, T dialog, Func<(IDictionary<string, string>, IDictionary<string, string>)> onChange, bool inUserspace = false);
    }
}