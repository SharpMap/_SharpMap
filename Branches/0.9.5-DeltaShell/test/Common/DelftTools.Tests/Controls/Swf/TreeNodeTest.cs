using System.Collections.Generic;
using DelftTools.Controls;
using DelftTools.Controls.Swf;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class TreeNodeTest
    {
        private TreeView treeView;

        [SetUp]
        public void SetUp()
        {
            treeView = new TreeView();
        }

        [Test]
        public void GetParentOfLevel()
        {
            var node1 = treeView.NewNode();
            var node11 = treeView.NewNode();
            var node12 = treeView.NewNode();

            treeView.Nodes.Add(node1);
            node1.Nodes.Add(node11);
            node1.Nodes.Add(node12);

            var node = node12.GetParentOfLevel(0);

            node
                .Should().Be.EqualTo(node1);
        }

        [Test]
        public void RefreshChildNodes()
        {
            const string tagObject = "TagObject";
            const string childObject1 = "childObject1";
            const string childObject2 = "childObject2";

            var childObjects = new List<string> { childObject1 };
            
            var mocks = new MockRepository();

            var treeViewMock = mocks.StrictMock<TreeView>();
            var treeViewNodePresenterMock = mocks.Stub<ITreeNodePresenter>();

            var treeNode = new TreeNode(treeViewMock);
            var subNode1 = new TreeNode(treeViewMock);
            var subNode2 = new TreeNode(treeViewMock);

            Expect.Call(((ITreeView) treeViewMock).NewNode()).Return(subNode1).Repeat.Twice();
            Expect.Call(((ITreeView) treeViewMock).NewNode()).Return(subNode2).Repeat.Once();   
            Expect.Call(((ITreeView) treeViewMock).GetTreeViewNodePresenter(tagObject)).Return(treeViewNodePresenterMock).IgnoreArguments().Repeat.Any();
            Expect.Call(treeViewMock.CheckBoxes).Return(false).Repeat.Any();

            Expect.Call(() => treeViewMock.UpdateNode(treeNode, subNode1, childObject1)).WhenCalled(delegate { subNode1.Tag = childObject1; }).Repeat.Twice();
            Expect.Call(() => treeViewMock.UpdateNode(treeNode, subNode2, childObject2)).WhenCalled(delegate { subNode2.Tag = childObject2; }).Repeat.Once();
            Expect.Call(treeViewNodePresenterMock.GetChildNodeObjects(tagObject)).Return(childObjects).Repeat.Any();
                
            mocks.ReplayAll();

            treeNode.Tag = tagObject;

            Assert.AreEqual(1, treeNode.Nodes.Count);
            Assert.IsFalse(treeNode.Nodes[0].IsExpanded);

            childObjects.Add(childObject2);
            treeNode.Nodes[0].Expand();
            treeNode.RefreshChildNodes();

            Assert.AreEqual(2, treeNode.Nodes.Count);
            Assert.IsTrue(treeNode.Nodes[0].IsExpanded);
            Assert.IsFalse(treeNode.Nodes[1].IsExpanded);

            mocks.VerifyAll();
        }
    }
}