using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using SharpMap;
using SilverlightRendering;
using System.IO;
using System;

namespace SilverlightSample
{
    public partial class MainPage : UserControl
    {
        bool refresh = true;
        Stream stream;

        public MainPage()
        {
            InitializeComponent();
            CompositionTarget.Rendering += new System.EventHandler(CompositionTarget_Rendering);
            var webClient = new System.Net.WebClient();
            webClient.OpenReadCompleted += new System.Net.OpenReadCompletedEventHandler(webClient_OpenReadCompleted);
            webClient.OpenReadAsync(new Uri("http://localhost:62297/Images/icon.png"));
        }

        void webClient_OpenReadCompleted(object sender, System.Net.OpenReadCompletedEventArgs e)
        {            
            stream = e.Result;
        }

        void CompositionTarget_Rendering(object sender, System.EventArgs e)
        {
            RenderMap(); 
        }

        private void MyGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            refresh = true;

            //WriteableBitmap bitmap = new WriteableBitmap(1024, 1024);
            //bitmap.Render(grid, null);
            //bitmap.Invalidate();
            //image.Source = bitmap;
        }

        Map map = null;

        private void RenderMap()
        {
            if (refresh == false) return;
            refresh = false;
            if (stream == null) return;
            
            if (map == null) map = MySample.InitializeMap(stream);

            SilverlightRenderer renderer = new SilverlightRenderer(canvas.Children);
            View transform = new View();

            transform.Center = map.GetExtents().GetCentroid();
            transform.Width = (float)canvas.ActualWidth;
            transform.Height = (float)canvas.ActualHeight;
            transform.Resolution = map.GetExtents().Width / transform.Width;

            canvas.Children.Clear();
            map.Render(renderer, transform);

            //!!!byte[] bytes = renderer.GetMapAsByteArray(transform, map);
            //!!!BitmapImage bitmapImage = new BitmapImage();
            //!!!bitmapImage.BeginInit();
            //!!!bitmapImage.StreamSource = new MemoryStream(bytes);
            //!!!bitmapImage.EndInit();
            //!!!image.Source = bitmapImage;
            //!!!this.InvalidateVisual();
        }
    }
}
