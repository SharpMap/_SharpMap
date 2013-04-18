using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Controls;
using DelftTools.Controls.Swf;
using DelftTools.Tests.TestObjects;
using NUnit.Framework;
using Rhino.Mocks;
using TreeNode = DelftTools.Controls.Swf.TreeNode;
using TreeView = DelftTools.Controls.Swf.TreeView;

namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class TreeViewTests
    {
        readonly MockRepository mockRepository=new MockRepository();


        /// <summary>
        /// Assure the correct node is returned containing a specific tag
        /// </summary>
        [Test]
        public void GetNodeByTag()
        {
            var o1 = new object();

            var treeView = new TreeView();
            ITreeNode node = treeView.NewNode();
            node.Tag = o1;
            treeView.Nodes.Add(node);
            ITreeNode node1 = treeView.GetNodeByTag(o1);
            Assert.AreEqual(node, node1);
        }

        /// <summary>
        /// Assure a nodepresenter is returned that corresponds to the given datatype
        /// </summary>
        [Test]
        public void GetNodePresenterForDataType()
        {
            var presenter= mockRepository.Stub<ITreeNodePresenter>();
            Expect.Call(presenter.NodeTagType).Return(typeof (object));
            var treeView = new TreeView();

            //treeview is assigned to presenter when it's added to the list of nodepresenters
            treeView.NodePresenters.Add(presenter);
            Assert.AreEqual(treeView, presenter.TreeView);

            mockRepository.ReplayAll();
            Assert.AreEqual(presenter, treeView.GetTreeViewNodePresenter(new object()));
            mockRepository.VerifyAll();
        }

        [Test]
        public void HiseSelectionIsFalseByDefault()
        {
            new TreeView().HideSelection.Should().Be.False();
        }

        [Test]
        public void RefreshShouldNotRefreshNodesWhichAreNotLoaded()
        {
            var treeView = new TreeView();

            var parent = new Parent {Name = "parent1"};
            var child = new Child();
            parent.Children.Add(child);

            var parentNodePresenter = new ParentNodePresenter();
            var childNodePresenter = new ChildNodePresenter();
            
            treeView.NodePresenters.Add(parentNodePresenter);
            treeView.NodePresenters.Add(childNodePresenter);

            childNodePresenter.AfterUpdate += delegate { Assert.Fail("Child nodes which are not loaded should not be updated"); };

            treeView.Refresh();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ExceptionWhenTwoNodePresentersUseTheSameNodeTagType()
        {
            var treeView = new TreeView();

            var parentNodePresenter1 = new ParentNodePresenter();
            var parentNodePresenter2 = new ParentNodePresenter();

            treeView.NodePresenters.Add(parentNodePresenter1);
            treeView.NodePresenters.Add(parentNodePresenter2);
        }

        [Test]
        public void GetAllLoadedNodes()
        {
            /* 
             * RootNode
               |-LoadedChild
               |-NotLoadedChild
               |-LoadedChild2
                    |-LoadedGrandChild
               |-NotLoadedChild2
                    |-LoadedGrandChild2
             */
            var treeView = new TreeView();

            ITreeNode rootNode = new MockTestNode(treeView, true) { Text = "RootNode" };
            var loadedChild = new MockTestNode(treeView, true) { Text = "LoadedChild" };
            rootNode.Nodes.Add(loadedChild);
            var notLoadedChild = new MockTestNode(treeView, false) { Text = "NotLoadedChild" };
            rootNode.Nodes.Add(notLoadedChild);

            var loadedChild2 = new MockTestNode(treeView, true) { Text = "LoadedChild2" };
            rootNode.Nodes.Add(loadedChild2);
            var loadedGrandChild = new MockTestNode(treeView, true) { Text = "LoadedGrandChild" };
            loadedChild2.Nodes.Add(loadedGrandChild);

            var notLoadedChild2 = new MockTestNode(treeView, false) { Text = "NotLoadedChild2" };
            rootNode.Nodes.Add(notLoadedChild2);
            notLoadedChild2.Nodes.Add(new MockTestNode(treeView, true) { Text = "LoadedGrandChild2" });
            //reset the loaded flag. It was set set to true by the previous call
            notLoadedChild2.SetLoaded(false);

            treeView.Nodes.Add(rootNode);

            Assert.AreEqual(new[] { rootNode, loadedChild, notLoadedChild, loadedChild2, loadedGrandChild, notLoadedChild2 }, treeView.AllLoadedNodes.ToArray());
        }

    }
}