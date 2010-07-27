using System.Drawing;
using System.Windows.Forms;
using DelftTools.Controls;

namespace DelftTools.Tests.Controls.Swf
{
    class MockUserControlView : ShellUserControl, IView
    {
        private readonly MockPresenter presenter;

        public MockUserControlView(MockPresenter presenter)
        {
            this.presenter = presenter;
        }
        
        public object Data { get; set; }
        public Image Image { get; set; }
    }

    public class ShellUserControl : UserControl
    {
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            ParentForm.FormClosing += ParentFormFormClosing;
        }

        static void ParentFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (MessageBox.Show("Would you like to close the parent form?", "Close parent form?",

                                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}