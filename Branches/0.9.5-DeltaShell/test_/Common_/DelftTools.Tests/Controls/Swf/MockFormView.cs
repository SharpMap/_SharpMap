using System.Drawing;
using System.Windows.Forms;
using DelftTools.Controls;

namespace DelftTools.Tests.Controls.Swf
{
    class MockFormView : Form, IView
    {
        private readonly MockPresenter presenter;

        public MockFormView(MockPresenter presenter)
        {
            this.presenter = presenter;
            Load += delegate { presenter.ViewReady(); };
        }

        public object Data { get; set; }
        public Image Image { get; set; }
    }
}