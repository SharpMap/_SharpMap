// Copyright 2008 - Paul den Dulk (Geodan)
// 
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using SharpMap.Geometries;

namespace SharpMap
{
    public class View : IView
    {
        #region Fields

        double resolution;
        Point center = new Point();
        float width;
        float height;
        BoundingBox extent;

        #endregion

        #region Public Methods

        public double Resolution
        {
            set
            {
                resolution = value;
                UpdateExtent();
            }
            get
            {
                return resolution;
            }
        }

        public Point Center
        {
            set
            {
                center = value;
                UpdateExtent();
            }
            get
            {
                return center;
            }
        }

        public float Width
        {
            set
            {
                width = value;
                UpdateExtent();
            }
            get { return width; }
        }

        public float Height
        {
            set
            {
                height = value;
                UpdateExtent();
            }
            get { return height; }
        }

        public BoundingBox Extent
        {
            get { return extent; }
        }

        public Point WorldToView(Point point)
        {
            return new SharpMap.Geometries.Point((point.X - extent.MinX) / resolution, (extent.MaxY - point.Y) / resolution);
        }

        public Point ViewToWorld(Point point)
        {
            return new SharpMap.Geometries.Point((extent.MinX + point.X * resolution), (extent.MaxY - (point.Y * resolution)));
        }

        #endregion

        #region Private Methods

        private void UpdateExtent()
        {
            if (center.IsEmpty()) return;

            float spanX = width * (float)resolution;
            float spanY = height * (float)resolution;
            extent = new BoundingBox(center.X - spanX * 0.5f, center.Y - spanY * 0.5f,
              center.X + spanX * 0.5f, center.Y + spanY * 0.5f);
        }

        #endregion
    }
}
