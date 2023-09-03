using FrooxEngine;
using System;
using System.Collections.Generic;

namespace NeosDialogBuilder
{
    public class DialogElementContainer : IDialogElement
    {
        private bool _ParentEnabled;
        private bool _Enabled;

        public Slot Root { get; }
        public IEnumerable<IDialogElement> Elements { get; }

        public object Key { get; }

        public IEnumerable<object> BoundErrorKeys
        {
            get
            {
                var allKeys = new HashSet<object>();
                foreach (var element in Elements)
                {
                    foreach (var key in element.BoundErrorKeys) {
                        allKeys.Add(key);
                    }
                }
                return allKeys;
            }
        }

        public bool Visible
        {
            get
            {
                return Root.ActiveSelf;
            }
            set
            { 
                Root.ActiveSelf = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return _Enabled;
            }
            set
            {
                _Enabled = value;
                PropagateEnabled();
            }
        }

        public bool ParentEnabled
        {
            private get
            {
                return _ParentEnabled;
            }
            set
            {
                _ParentEnabled = value;
                PropagateEnabled();
            }
        }

        private void PropagateEnabled()
        {
            foreach (var element in Elements)
            {
                element.ParentEnabled = ParentEnabled && Enabled;
            }
        }

        public void Reset()
        {
            Reset(null);
        }

        public void DisplayErrors(IDictionary<object, string> allErrors, IDictionary<object, string> unboundErrors)
        {
            foreach (var element in Elements)
            {
                element.DisplayErrors(allErrors, unboundErrors);
            }
        }

        public void Show()
        {
            Visible = true;
        }
        public void Hide()
        {
            Visible = false;
        }


        public void ShowAll()
        {
            Show(null);
        }
        public void HideAll()
        {
            Hide(null);
        }
        public void Show(IEnumerable<object> keys) {
            ForEachElement(keys, (element) => element.Visible = true);
        }
        public void Hide(IEnumerable<object> keys) {
            ForEachElement(keys, (element) => element.Visible = false);
        }
        public void SetVisible(IEnumerable<object> keys) {
            ForEachElement(keys, (element) => element.Visible = true, (element) => element.Visible = false);
        }

        public void EnableAll()
        {
            Enable(null);
        }
        public void DisableAll()
        {
            Disable(null);
        }
        public void Enable(IEnumerable<object> keys) {
            ForEachElement(keys, (element) => element.Enabled = true);
        }
        public void Disable(IEnumerable<object> keys) {
            ForEachElement(keys, (element) => element.Enabled = false);
        }
        public void SetEnabled(IEnumerable<object> keys) {
            ForEachElement(keys, (element) => element.Enabled = true, (element) => element.Enabled = false);
        }

        public void ResetAll()
        {
            Reset(null);
        }
        public void Reset(IEnumerable<object> keys)
        {
            ForEachElement(keys, (element) => element.Reset());
        }

        private void ForEachElement(IEnumerable<object> keys, Action<IDialogElement> thenAction, Action<IDialogElement> elseAction = null)
        {
            Func<object, bool> isAffected;
            if (keys == null)
            {
                isAffected = (_) => true;
            }
            else
            {
                isAffected = new HashSet<object>(keys).Contains;
            }
            foreach(IDialogElement element in Elements)
            {
                if (isAffected(element))
                {
                    thenAction(element);
                }
                else if (elseAction != null)
                {
                    elseAction(element);
                }
            }
        }

        internal DialogElementContainer(object key, Slot root, IEnumerable<IDialogElement> elements)
        {
            this.Key = key;
            this.Root = root;
            this.Elements = elements;
        }
    }
}
