using System.Collections;
//using System.ComponentModel;
using System.ComponentModel;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Controls;
using DelftTools.Controls.Swf;
using DelftTools.Tests.Controls.Swf.UndoRedoTestClasses;
using DelftTools.TestUtils;
using DelftTools.Utils.UndoRedo;
using NUnit.Framework;
using IEditableObject = DelftTools.Utils.IEditableObject;


namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class UndoRedoHistoryControlTest
    {
        [Test]
        [NUnit.Framework.Category(TestCategory.WindowsForms)]
        public void Show()
        {
            LogHelper.ConfigureLogging();

            var parent = new Parent { Name = "original name" };

            // setup undo/redo for parent
            var undoRedoManager = new UndoRedoManager(parent);

            // create undo/redo control
            var undoRedoHistoryControl = new UndoRedoHistoryControl { UndoRedoManager = undoRedoManager };

            // show parent using tree view
            var parentTreeView = new TreeView();
            var parentNodePresenter = new ParentTreeViewNodePresenter();
            var childNodePresenter = new ChildTreeViewNodePresenter();
            parentTreeView.NodePresenters.Add(parentNodePresenter);
            parentTreeView.NodePresenters.Add(childNodePresenter);
            parentTreeView.Data = parent;
            WindowsFormsTestHelper.Show(parentTreeView);

            // do some actions
            parent.Name = "name1";
            parent.Name = "name2";
            parent.Children.Add(new Child { Name = "child1" });

            var editable = (IEditableObject)parent;
            editable.BeginEdit("Add new child and change name");
            parent.Children.Add(new Child {Name = "child2"});
            parent.Name = "name3";
            editable.EndEdit();

            parent.Children.RemoveAt(0);

            WindowsFormsTestHelper.ShowModal(undoRedoHistoryControl);
        }

        public class ParentTreeViewNodePresenter : TreeViewNodePresenterBase<Parent>
        {
            public override void UpdateNode(ITreeNode parentNode, ITreeNode node, Parent nodeData)
            {
                node.Text = nodeData.Name;
            }

            public override IEnumerable GetChildNodeObjects(Parent parentNodeData)
            {
                return parentNodeData.Children.Cast<object>();
            }

            public override bool IsParentOf(Parent parentNodeData, object childNodeData)
            {
                var child = childNodeData as Child;
                
                return child != null && parentNodeData.Children.Contains(child);
            }

            public override void OnPropertyChanged(Parent item, PropertyChangedEventArgs e)
            {
                var node = TreeView.GetNodeByTag(item);
                if(e.PropertyName == "Name")
                {
                    node.Text = item.Name;
                }
            }
        }

        public class ChildTreeViewNodePresenter : TreeViewNodePresenterBase<Child>
        {
            public override void UpdateNode(ITreeNode parentNode, ITreeNode node, Child nodeData)
            {
                node.Text = nodeData.Name;
            }

            public override void OnPropertyChanged(Child item, PropertyChangedEventArgs e)
            {
                var node = TreeView.GetNodeByTag(item);
                if (e.PropertyName == "Name")
                {
                    node.Text = item.Name;
                }
            }
        }

        [Test]
        public void TrackTwoUndoActions()
        {
            var parent = new Parent { Name = "original name" };

            var undoRedoManager = new UndoRedoManager(parent);

            var undoRedoHistoryControl = new UndoRedoHistoryControl { UndoRedoManager = undoRedoManager };

            parent.Name = "name1";

            parent.Name = "name2";

            undoRedoHistoryControl.TreeView.Nodes.Count
                .Should().Be.EqualTo(3);

            undoRedoHistoryControl.TreeView.Nodes[0].Text 
                .Should("empty node").Be.EqualTo("");

            undoRedoHistoryControl.TreeView.Nodes[1].Text
                .Should().Contain("name1");

            undoRedoHistoryControl.TreeView.Nodes[2].Text
                .Should().Contain("name2");
        }

        [Test]
        public void UndoShouldChangeSelectedNodeToThePreviousNode()
        {
            var parent = new Parent { Name = "original name" };

            var undoRedoManager = new UndoRedoManager(parent);

            var undoRedoHistoryControl = new UndoRedoHistoryControl { UndoRedoManager = undoRedoManager };

            parent.Name = "name1";

            parent.Name = "name2";

            undoRedoManager.Undo();

            undoRedoHistoryControl.TreeView.Nodes.IndexOf(undoRedoHistoryControl.TreeView.SelectedNode)
                .Should().Be.EqualTo(1);
        }

        [Test]
        public void RedoShouldChangeSelectedNodeToTheNextNode()
        {
            var parent = new Parent { Name = "original name" };

            var undoRedoManager = new UndoRedoManager(parent);
            var undoRedoHistoryControl = new UndoRedoHistoryControl { UndoRedoManager = undoRedoManager };

            parent.Name = "name1";

            parent.Name = "name2";

            undoRedoManager.Undo();
            undoRedoManager.Undo();
            undoRedoManager.Redo();

            undoRedoHistoryControl.TreeView.Nodes.IndexOf(undoRedoHistoryControl.TreeView.SelectedNode)
                .Should().Be.EqualTo(1);
        }
    }
}