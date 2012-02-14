using System;
using System.Collections.Generic;
using DelftTools.Controls;
using DelftTools.Controls.Swf;
using DelftTools.Tests.TestObjects;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class TreeNodePresenterResolverTest
    {

        [Test]
        public void ResolveNodePresenterForDataWalksUpClassHierarchy()
        {
            var baseClassNodePresenter = new BaseClassNodePresenter();
            var nodePresenters = new List<ITreeNodePresenter> { baseClassNodePresenter };
            var subClassNodePresenter = new SubClassNodePresenter();
            nodePresenters.Add(subClassNodePresenter);

            Assert.AreEqual(subClassNodePresenter, TreeNodePresenterResolver.ResolveNodePresenterForData(new SubClass(), nodePresenters));
        }

        [Test]
        public void ResolveNodePresenterForDataReturnsNullIfNotFound()
        {
            Assert.IsNull(TreeNodePresenterResolver.ResolveNodePresenterForData(new SubClass(), new List<ITreeNodePresenter>()));
        }

        [Test]
        [ExpectedException(ExceptionType = typeof(InvalidOperationException), ExpectedMessage = "Unable to resolve nodepresenter for null data")]
        public void ResolveNodePresenterForDataThrowsIfItemIsNull()
        {
            TreeNodePresenterResolver.ResolveNodePresenterForData(null, new List<ITreeNodePresenter>());
        }
        
        [Test]
        public void ResolveNodePresenterCanMatchOnInterface()
        {
            var interfaceNodePresenter = new SomeInterfaceNodePresenter();
            var interfaceImplementor = new SubClass();
            Assert.AreEqual(interfaceNodePresenter,
                TreeNodePresenterResolver.ResolveNodePresenterForData(interfaceImplementor, new []{interfaceNodePresenter}));
        }
    }
}