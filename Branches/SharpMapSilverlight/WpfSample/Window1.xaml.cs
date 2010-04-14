using System;
using System.Windows;
using System.Windows.Media;
using SharpMap;
using SharpMap.Samples;
using SilverlightRendering;
using SharpMap.Data;

namespace WpfSample
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        MapTransform transform = new MapTransform();
        bool refresh = false;
        int soep = 100;

        public Window1()
        {
            CompositionTarget.Rendering += new System.EventHandler(CompositionTarget_Rendering);
        }


        void CompositionTarget_Rendering(object sender, System.EventArgs e)
        {
            RenderMap();    
        }

        private void RenderMap()
        {
            if (refresh == false) return;
            refresh = false;

            Map map = GradiantThemeSample.InitializeMap();

            SilverlightRenderer renderer = new SilverlightRenderer(canvas.Children);
            MapTransform transform = new MapTransform();

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
            this.InvalidateVisual();
        }

        private void grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            canvas.Width = grid.ActualWidth;
            canvas.Height = grid.ActualHeight;
            refresh = true;
        }
    }
}
