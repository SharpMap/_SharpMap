using DelftTools.Controls;
using DelftTools.Shell.Gui;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Controls.Swf
{
    [TestFixture]
    public class PresenterTest
    {       
        private static readonly MockRepository mocks = new MockRepository();
        
        [TestFixtureSetUp]
        public void FixtureSetup()
        {            
        }

        [Test]
        public void OnViewSetActionWillBeCalledWhenViewIsSet()
        {
            var mockPresenter = new MockPresenter();
            mockPresenter.View = mocks.Stub<IView>();
            //assert onviewset is called.
            Assert.AreEqual(1,mockPresenter.OnViewSetCallCount);
        }

        [Test]
        public void OnViewReadyActionWillBeCalledWhenViewIsLoaded()
        {
            
            var presenter = new MockPresenter();
            presenter.ViewReady();
            Assert.AreEqual(1, presenter.OnViewReadyCallCount);
        }

        [Test]
        public void OnViewCloseActionWillBeCalledWhenViewIsClosed()
        {

            var presenter = new MockPresenter();
            
            presenter.View = mocks.Stub<IView>();
            //do we really want this?? seems strange
            presenter.Dispose();
            Assert.AreEqual(1, presenter.OnViewClosedCallCount);
        }

        [Test]
        public void CanCloseViewEvaluationWorks()
        {
//            bool canCloseView = false;
//            var presenter = new MockPresenter();
//            presenter.SetCanCloseFunction(v => canCloseView );
//            var view = new MockFormView(presenter);            
//            presenter.View = view;
//            Assert.IsTrue(presenter.CanCloseView());
//            canCloseView = true;
//            Assert.IsFalse(presenter.CanCloseView());
        }

        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
        }
    }

    /// <summary>
    /// TODO: use RhinoMock
    /// </summary>
    class MockPresenter : Presenter<IView>
    {
        public int OnViewSetCallCount;

        public int OnViewReadyCallCount;
        public int OnViewClosedCallCount;

        protected override void OnViewSet()
        {
            base.OnViewSet();
            OnViewSetCallCount++;
        }
        protected override void OnViewReady()
        {
            base.OnViewReady();
            OnViewReadyCallCount++;
        }
        protected override void OnCloseView()
        {
            base.OnCloseView();
            OnViewClosedCallCount++;
        }
    }
}