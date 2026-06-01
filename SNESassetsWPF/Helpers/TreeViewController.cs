using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SNESassetsWPF.Helpers
{
    public class TreeViewController
    {
        public TreeView Tree { get; }
        public TreeViewStateManager State { get; }

        public List<string> SavedExpanded { get; private set; }
        public string SavedSelected { get; private set; }

        public TreeViewController(TreeView tree)
        {
            Tree = tree;
            State = new TreeViewStateManager( tree );
        }

        public void SaveState()
        {
            SavedExpanded = State.SaveExpanded();
            SavedSelected = State.SaveSelected();
        }

        public void RestoreState()
        {
            if ( SavedExpanded == null && SavedSelected == null )
                return;

            // Let WPF finish regenerating containers, then restore
            Tree.Dispatcher.BeginInvoke(
                DispatcherPriority.Background ,
                new Action( () =>
                {
                    State.RestoreState( SavedExpanded , SavedSelected );
                } ) );
        }
    }
}
