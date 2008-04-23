using System;
using System.Collections.Generic;
using System.Text;

namespace SharpMap.Styles
{
    public abstract class ImageMapStyleBase
           : IStyle
    {
        #region IStyle Members

        private double _minVisible;
        private double _maxVisible;
        private bool _enabled;


        public double MinVisible
        {
            get
            {
                return _minVisible;
            }
            set
            {
                _minVisible = value;
            }
        }

        public double MaxVisible
        {
            get
            {
                return _maxVisible;
            }
            set
            {
                _maxVisible = value; ;
            }
        }

        public bool Enabled
        {
            get
            {
                return _enabled;
            }
            set
            {
                _enabled = value;
            }
        }


        public ImageMapStyleBase(double minVisible, double maxVisible, bool enabled)
        {
            this._minVisible = minVisible;
            this._maxVisible = maxVisible;
            this._enabled = enabled;
        }




        #endregion
    }

    public class PolygonStyle : ImageMapStyleBase
    {
        public PolygonStyle(double minVis, double maxVis, bool enable)
            : base(minVis, maxVis, enable)
        {

        }

    }

    public class PointStyle
        : ImageMapStyleBase
    {
        private int _radius;
        public int Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }

        public PointStyle(int radius, double minVis, double maxVis, bool enable)
            : base(minVis, maxVis, enable)
        {
            _radius = radius;
        }
    }

    public class LineStyle : ImageMapStyleBase
    {
        private int _bufferWidth = 5;
        public int BufferWidth
        {
            get
            {
                return _bufferWidth;
            }
            set
            {
                _bufferWidth = value;
            }
        }
        public LineStyle(int bufferWidth, double minVis, double maxVis, bool enable)
            : base(minVis, maxVis, enable)
        {
            _bufferWidth = bufferWidth;
        }
    }

    public class ImageMapStyle : ImageMapStyleBase
    {
        private PointStyle _ps;
        private LineStyle _ls;
        private PolygonStyle _polyStyle;

        public PointStyle Point
        {
            get
            {
                return _ps;
            }
        }

        public LineStyle Line
        {
            get
            {
                return _ls;
            }
        }

        public PolygonStyle Polygon
        {
            get
            {
                return _polyStyle;
            }
        }

        public ImageMapStyle(double minVis, double maxVis, bool enabled)
            : base(minVis, maxVis, enabled)
        {
            this._polyStyle = new PolygonStyle(minVis, maxVis, enabled);
            this._ps = new PointStyle(5, minVis, maxVis, enabled);
            this._ls = new LineStyle(5, minVis, maxVis, enabled);
        }

    }
}
