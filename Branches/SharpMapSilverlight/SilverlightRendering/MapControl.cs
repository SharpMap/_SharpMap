using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SharpMap;
using SilverlightRendering;
using System;

namespace SilverlightRendering
{
    public class MapControl : Canvas
    {
        View view;
        Map map;
        Buffer frontBuffer = new Buffer();
        Point previousPosition;
        MatrixTransform matrixTransform = new MatrixTransform();
        bool refresh = true;
        bool first = true;
        bool mouseDown = false;
        
        public Map Map
        {
            get { return map; }
            set {
                map = value;
                if (view == null) view = InitializeView();
           }
        }

        public MapControl()
        {
            this.Background = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            CompositionTarget.Rendering += new System.EventHandler(CompositionTarget_Rendering);

            this.SizeChanged += new SizeChangedEventHandler(MapControl_SizeChanged);
            this.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(MapControl_MouseDown);
            this.MouseMove += new MouseEventHandler(MapControl_MouseMove);
            this.MouseLeftButtonUp += new MouseButtonEventHandler(MapControl_MouseUp);
            this.MouseLeave += new MouseEventHandler(MapControl_MouseLeave);
            this.MouseWheel += new MouseWheelEventHandler(MapControl_MouseWheel);
            this.Loaded += new RoutedEventHandler(MapControl_Loaded);
        }

        void MapControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (first)
            {
                first = false;
                if (map == null) return;
                view.Width = this.ActualWidth;
                view.Height = this.ActualHeight;
                RedrawBuffer();
            }
        }

        void MapControl_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double newResolution = 1;

            if (e.Delta > 0)
            {
                newResolution = view.Resolution * 0.5;
            }
            else if (e.Delta < 0)
            {
                newResolution = view.Resolution * 2;
            }

            Point mousePosition = e.GetPosition(this);
            // When zooming we want the mouse position to stay above the same world coordinate.
            // We calcultate that in 3 steps.

            // 1) Temporarily center on the mouse position
            view.Center = view.ViewToWorld(new SharpMap.Geometries.Point(mousePosition.X, mousePosition.Y));

            // 2) Then zoom 
            this.view.Resolution = newResolution;

            // 3) Then move the temporary center of the map back to the mouse position
            this.view.Center = this.view.ViewToWorld(new SharpMap.Geometries.Point(
              this.view.Width - mousePosition.X,
              this.view.Height - mousePosition.Y));

            //!!!RedrawBuffer();
            refresh = true;
#if (!SILVERLIGHT)

            InvalidateVisual();
#endif
        }

        void CompositionTarget_Rendering(object sender, System.EventArgs e)
        {
            if (map == null) return;
            if (view == null) return;
            if (!refresh) return;
            refresh = false;
            matrixTransform.Matrix = BuildMatrix(view, frontBuffer.Extent);
            frontBuffer.Canvas.RenderTransform = matrixTransform;
        }

        private static Matrix BuildMatrix(View view, SharpMap.Geometries.BoundingBox bufferExtent)
        {
            Matrix matrix = new Matrix();

            var viewExtent = view.Extent;
            SharpMap.Geometries.Point centerView = viewExtent.GetCentroid();
            SharpMap.Geometries.Point centerBuffer = bufferExtent.GetCentroid();
            double scaleX = bufferExtent.Width / viewExtent.Width;
            double scaleY = bufferExtent.Height / viewExtent.Height;

#if !SILVERLIGHT
            matrix.ScaleAt(scaleX, scaleY, view.Width * 0.5, view.Height * 0.5);
            matrix.Translate((centerBuffer.X - centerView.X) * 1 / view.Resolution, (centerView.Y - centerBuffer.Y) * 1 / view.Resolution);
#endif
            return matrix;
        }

        private View InitializeView()
        {
            if ((this.ActualWidth == 0) && (this.ActualHeight == 0)) return null;
            if (map == null) return null;

            view = new View();
            view.Width = this.ActualWidth;
            view.Height = this.ActualHeight;
            view.Center = map.GetExtents().GetCentroid();
            view.Resolution = map.GetExtents().Width / view.Width;
            return view;
        }

        void MapControl_MouseLeave(object sender, MouseEventArgs e)
        {
            previousPosition = new Point();
        }

        void MapControl_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            previousPosition = e.GetPosition(this);
            mouseDown = true;
#if !SILVERLIGHT
            Mouse.Capture(this);
#endif
        }

        void MapControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (!mouseDown) return;

            if (previousPosition == new Point()) return; //Experience showed that sometimes MouseDown is called before state is pressed.

            Point currentPosition = e.GetPosition(this);

            ViewHelper.Pan(view, currentPosition.X, currentPosition.Y, previousPosition.X, previousPosition.Y);

            previousPosition = currentPosition;

            refresh = true;
#if !SILVERLIGHT

            InvalidateVisual();
#endif
        }

        void MapControl_MouseUp(object sender, MouseButtonEventArgs e)
        {
            previousPosition = new Point();
#if (!SILVERLIGHT)
            Mouse.Capture(null);
#endif
            mouseDown = false;

            RedrawBuffer();
        }

        private void RedrawBuffer()
        {
            //Should be called an a background thread
            RenderMap();
        }

        void MapControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (map == null) return;
            if (view == null) InitializeView();
            view.Width = this.ActualWidth;
            view.Height = this.ActualHeight;
            RedrawBuffer();
        }

        private void RenderMap()
        {
            var backBuffer = new Buffer();
            SilverlightRenderer renderer = new SilverlightRenderer(backBuffer.Canvas.Children);
            backBuffer.Extent = view.Extent.Clone();
            map.Render(renderer, view);

            this.Children.Remove(frontBuffer.Canvas);
            this.frontBuffer = backBuffer;
            this.Children.Add(frontBuffer.Canvas);

            refresh = true;
#if !SILVERLIGHT
            InvalidateVisual();
#endif
        }

    }
}
