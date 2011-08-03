﻿// Copyright 2011 - Felix Obermaier (www.ivv-aachen.de)
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
//
// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using SharpMap.Geometries;
using SharpMap.Utilities;
using Point = SharpMap.Geometries.Point;

namespace SharpMap.Rendering.Symbolizer
{
    /// <summary>
    /// Base class for all possible Point symbolizers
    /// </summary>
    [Serializable]
    public abstract class PointSymbolizer : IPointSymbolizer
    {
        private float _scale = 1f;

        /// <summary>
        /// Offset of the point from the point
        /// </summary>
        public PointF Offset { get; set; }

        /// <summary>
        /// Rotation of the symbol
        /// </summary>
        public float Rotation { get; set; }

        /// <summary>
        /// Gets or sets the Size of the symbol
        /// <para>
        /// Implementations may ignore the setter, the getter must return a <see cref="Size"/> with positive width and height values.
        /// </para>
        /// </summary>
        public abstract Size Size
        {
            get; set;
        }

 
        /// <summary>
        /// Gets or sets the scale 
        /// </summary>
        public virtual float Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (value <= 0)
                    return;
                _scale = value;
            }
        }

        private SizeF GetOffset()
        {
            var size = Size;
            var result = new SizeF(Offset.X - Scale * (size.Width * 0.5f), Offset.Y - Scale * (size.Height * 0.5f));
            return result;
        }



        /// <summary>
        /// Function to render the symbol
        /// </summary>
        /// <param name="map">The map</param>
        /// <param name="point">The point to symbolize</param>
        /// <param name="g">The graphics object</param>
        protected void RenderPoint(Map map, Point point, Graphics g)
        {
            if (point == null)
                return;


            PointF pp = Transform.WorldtoMap(point, map);
            pp = PointF.Add(pp, GetOffset());

            if (Rotation != 0f && !Single.IsNaN(Rotation))
            {
                Matrix startingTransform = g.Transform.Clone();

                Matrix transform = g.Transform;
                PointF rotationCenter = pp;
                transform.RotateAt(Rotation, rotationCenter);

                g.Transform = transform;
                
                OnRenderInternal(pp, g);

                g.Transform = startingTransform;
            }
            else
            {
                OnRenderInternal(pp, g);
            }
        }

        /// <summary>
        /// Method to render the <see cref="MultiPoint"/> to the <see cref="Graphics"/> object.
        /// </summary>
        /// <param name="map">The map object</param>
        /// <param name="points">Locations where to render the Symbol</param>
        /// <param name="g">The graphics object to use.</param>
        [Obsolete]
        public void Render(Map map, MultiPoint points, Graphics g)
        {
            if (points == null)
                return;
            
            foreach (Point point in points)
                Render(map, point, g);
        }

        /// <summary>
        /// Function that does the actual rendering
        /// </summary>
        /// <param name="pt">The point</param>
        /// <param name="g">The graphics object</param>
        internal abstract void OnRenderInternal(PointF pt, Graphics g);

        /// <summary>
        /// Utility function to transform any <see cref="IPointSymbolizer"/> into an unscaled <see cref="RasterPointSymbolizer"/>. This may bring performance benefits.
        /// </summary>
        /// <returns></returns>
        public virtual IPointSymbolizer ToRasterPointSymbolizer()
        {
            var bitmap = new Bitmap(Size.Width, Size.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                OnRenderInternal(new PointF(Size.Width * 0.5f, Size.Height * 0.5f), g);
            }

            return new RasterPointSymbolizer
                       {
                           Offset = Offset,
                           Rotation = Rotation,
                           Scale = Scale,
                           ImageAttributes = new ImageAttributes(),
                           Symbol = bitmap
                       };
        }

        public void Render(Map map, IPuntal geometry, Graphics graphics)
        {
            var mp = geometry as MultiPoint;
            if (mp != null)
            {
                foreach (Point point in mp.Points)
                    RenderPoint(map, point, graphics);
                return;
            }
            RenderPoint(map, geometry as Point, graphics);

        }

        public void Begin(Graphics g, Map map, int aproximateNumberOfGeometries)
        {
            //Nothing to do
        }

        public void Symbolize(Graphics g, Map map)
        {
            //Nothing to do, all points have been rendered
            //in the Render function call
        }

        public void End(Graphics g, Map map)
        {
            //Nothing to do
        }
    }
}