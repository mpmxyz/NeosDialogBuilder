using FrooxEngine.UIX;
using System;

namespace NeosDialogBuilder
{
    public interface IDialogOptionField<in T>
    {
        (string, Action<string>) Build(UIBuilder uiBuilder, T dialog, Action onChange, bool privateEnvironment = false);
    }
}