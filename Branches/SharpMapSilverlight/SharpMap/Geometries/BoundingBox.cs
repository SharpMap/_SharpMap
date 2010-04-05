// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
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

using System;
using System.Collections.Generic;
using System.Text;

namespace SharpMap.Geometries
{
	/// <summary>
	/// Bounding box type with double precision
	/// </summary>
	/// <remarks>
	/// The Bounding Box represents a box whose sides are parallel to the two axes of the coordinate system.
    /// </remarks>
    public class BoundingBox
    {
        #region Fields

        private Point _Min;
        private Point _Max;

        #endregion
        /// <summary>
		/// Initializes a bounding box
		/// </summary>
		/// <param name="minX">left</param>
		/// <param name="minY">bottom</param>
		/// <param name="maxX">right</param>
		/// <param name="maxY">top</param>
		public BoundingBox(double minX, double minY, double maxX, double maxY)
		{
			Min = new Point(minX, minY);
			Max = new Point(maxX, maxY);
		}

		/// <summary>
		/// Initializes a bounding box
		/// </summary>
		/// <param name="lowerLeft">Lower left corner</param>
		/// <param name="upperRight">Upper right corner</param>
		public BoundingBox(Geometries.Point lowerLeft, Geometries.Point upperRight)
		{
			Min = new Point(lowerLeft.X,lowerLeft.Y);
			Max = new Point(upperRight.X,upperRight.Y);
		}

		/// <summary>
		/// Initializes a new Bounding Box based on the bounds from a set of geometries
		/// </summary>
		/// <param name="objects">list of objects</param>
		public BoundingBox(List<SharpMap.Geometries.Geometry> objects)
		{
			Min = objects[0].GetBoundingBox().Min;
			Max = objects[0].GetBoundingBox().Max;
			for (int i = 0; i < objects.Count; i++)
				this.Join(objects[i].GetBoundingBox());
		}

		/// <summary>
		/// Initializes a new Bounding Box based on the bounds from a set of bounding boxes
		/// </summary>
		/// <param name="objects">list of objects</param>
		public BoundingBox(List<SharpMap.Geometries.BoundingBox> objects)
		{
			if (objects.Count == 0) { _Max = null; _Min = null; }
			else
			{
				_Min = objects[0].Min.Clone();
				_Max = objects[0].Max.Clone();
				for (int i = 0; i < objects.Count; i++)
					this.Join(objects[i]);
			}
		}

        public double MinX { get { return _Min.X; } }
        public double MinY { get { return _Min.Y; } }
        public double MaxX { get { return _Max.X; } }
        public double MaxY { get { return _Max.Y; } }

		/// <summary>
		/// Lower left corner
		/// </summary>
		public Point Min
		{
			get { return _Min; }
			set { _Min = value; }
		}

		/// <summary>
		/// Upper right corner
		/// </summary>
		public SharpMap.Geometries.Point Max
		{
			get { return _Max; }
			set { _Max = value; }
		}

		/// <summary>
		/// Gets the left boundary
		/// </summary>
		public Double Left
		{
			get { return _Min.X; }
		}

		/// <summary>
		/// Gets the right boundary
		/// </summary>
		public Double Right
		{
			get { return _Max.X; }
		}

		/// <summary>
		/// Gets the top boundary
		/// </summary>
		public Double Top
		{
			get { return _Max.Y; }
		}

        /// <summary>
		/// Gets the bottom boundary
		/// </summary>
		public Double Bottom
		{
			get { return _Min.Y; }
		}

		/// <summary>
		/// Returns the width of the bounding box
		/// </summary>
		/// <returns>Width of boundingbox</returns>
		public double Width
		{
			get { return Math.Abs(_Max.X - _Min.X); }
		}
		/// <summary>
		/// Returns the height of the bounding box
		/// </summary>
		/// <returns>Height of boundingbox</returns>
		public double Height
		{
			get { return Math.Abs(_Max.Y - _Min.Y); }
		}
		/// <summary>
		/// Determines whether the boundingbox intersects another boundingbox
		/// </summary>
		/// <param name="box"></param>
		/// <returns></returns>
		public bool Intersects(BoundingBox box)
		{
			return !(box.Min.X > this.Max.X ||
					 box.Max.X < this.Min.X ||
					 box.Min.Y > this.Max.Y ||
					 box.Max.Y < this.Min.Y);
		}

		/// <summary>
		/// Computes the joined boundingbox of this instance and another boundingbox
		/// </summary>
		/// <param name="box">Boundingbox to join with</param>
		/// <returns>Boundingbox containing both boundingboxes</returns>
		public BoundingBox Join(BoundingBox box)
		{
			if (box == null)
				return this.Clone();
			else
				return new BoundingBox(Math.Min(this.Min.X , box.Min.X ), Math.Min(this.Min.Y , box.Min.Y ), Math.Max(this.Max.X , box.Max.X ), Math.Max(this.Max.Y , box.Max.Y ));
		}
		/// <summary>
		/// Computes the joined boundingbox of two boundingboxes
		/// </summary>
		/// <param name="box1"></param>
		/// <param name="box2"></param>
		/// <returns></returns>
		public static BoundingBox Join(BoundingBox box1, BoundingBox box2)
		{
			if (box1 == null)
				return box2;
			else
				return box1.Join(box2);
		}

		/// <summary>
		/// Increases the size of the boundingbox by the givent amount in all directions
		/// </summary>
		/// <param name="amount"></param>
		public BoundingBox Grow(double amount)
		{
			BoundingBox box = this.Clone();
			box.Min.X -= amount;
			box.Min.Y -= amount;
			box.Max.X += amount;
			box.Max.Y += amount;
			return box;
		}

		/// <summary>
		/// Checks whether a point lies within the bounding box
		/// </summary>
		/// <param name="p">Point</param>
		/// <returns>true if point is within</returns>
		public bool Contains(Point p)
		{
			if (this.Max.X < p.X)
				return false;
			if (this.Min.X > p.X)
				return false;
			if (this.Max.Y < p.Y)
				return false;
			if (this.Min.Y > p.Y)
				return false;
			return true;
		}

		/// <summary>
		/// Distance squared to a point from the box (Arvo's algorithm)
		/// </summary>
		/// <param name="point"></param>
		/// <returns></returns>
		public double DistSqrd(SharpMap.Geometries.Point point)
		{
			//for each component, find the point's relative position and the distance contribution
			double dst = 0;
			for (uint ii = 0; ii < 3; ii++)
				dst += (point[ii] < this.Min[ii]) ? Math.Sqrt(point[ii] - this.Min[ii]) :
					(point[ii] > this.Max[ii]) ? Math.Sqrt(point[ii] - this.Max[ii]) : 0;
			return dst;
		}

		/// <summary>
		/// Intersection scalar (used for weighting in building the tree) 
		/// </summary>
		public uint LongestAxis
		{
			get
			{
				SharpMap.Geometries.Point boxdim = this.Max - this.Min;
				uint la = 0; // longest axis
				double lav = 0; // longest axis length
				// for each dimension  
				for (uint ii = 0; ii < 2; ii++)
				{
					// check if its longer
					if (boxdim[ii] > lav)
					{
						// store it if it is
						la = ii;
						lav = boxdim[ii];
					}
				}
				return la;
			}
		}

		/// <summary>
		/// Returns the center of the bounding box
		/// </summary>
		public Point GetCentroid()
		{
			return (this._Min + this._Max) * .5f;
		}

		/// <summary>
		/// Creates a copy of the BoundingBox
		/// </summary>
		/// <returns></returns>
		public BoundingBox Clone()
		{
			return new BoundingBox(this._Min.X, this._Min.Y, this._Max.X, this._Max.Y);
		}

		/// <summary>
		/// Returns a string representation of the boundingbox as LowerLeft + UpperRight formatted as "MinX,MinY MaxX,MaxY"
		/// </summary>
		/// <returns>MinX,MinY MaxX,MaxY</returns>
		public override string ToString()
		{
			return this.Min.X.ToString() + "," + this.Min.Y.ToString() + " " + this.Max.X.ToString() + "," + this.Max.Y.ToString();
		}

	}
}
