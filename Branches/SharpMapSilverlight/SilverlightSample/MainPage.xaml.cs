using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SilverlightSample
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void MyGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WriteableBitmap bitmap = new WriteableBitmap(1024, 1024);
            bitmap.Render(grid, null);
            bitmap.Invalidate();
            image.Source = bitmap;
        }
    }
}
