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
	[Serializable]
	public class BoundingBox : IEquatable<BoundingBox>
	{
		/// <summary>
		/// Initializes a bounding box
		/// </summary>
		/// <remarks>
		/// In case min values are larger than max values, the parameters will be swapped to ensure correct min/max boundary
		/// </remarks>
		/// <param name="minX">left</param>
		/// <param name="minY">bottom</param>
		/// <param name="maxX">right</param>
		/// <param name="maxY">top</param>
		public BoundingBox(double minX, double minY, double maxX, double maxY)
		{
			_Min = new Point(minX, minY);
			_Max = new Point(maxX, maxY);
			CheckMinMax();
		}

		/// <summary>
		/// Initializes a bounding box
		/// </summary>
		/// <param name="lowerLeft">Lower left corner</param>
		/// <param name="upperRight">Upper right corner</param>
		public BoundingBox(Geometries.Point lowerLeft, Geometries.Point upperRight) : this(lowerLeft.X,lowerLeft.Y,upperRight.X,upperRight.Y)
		{
		}

		/// <summary>
		/// Initializes a new Bounding Box based on the bounds from a set of geometries
		/// </summary>
		/// <param name="objects">list of objects</param>
		public BoundingBox(List<SharpMap.Geometries.Geometry> objects)
		{
			if (objects == null || objects.Count == 0)
			{
				_Min = null;
				_Max = null;
				return;
			}
			_Min = objects[0].GetBoundingBox().Min.Clone();
			_Max = objects[0].GetBoundingBox().Max.Clone();
			CheckMinMax();
			for (int i = 1; i < objects.Count; i++)
			{
				BoundingBox box = objects[i].GetBoundingBox();
				_Min.X = Math.Min(box.Min.X, this.Min.X);
				_Min.Y = Math.Min(box.Min.Y, this.Min.Y);
				_Max.X = Math.Max(box.Max.X, this.Max.X);
				_Max.Y = Math.Max(box.Max.Y, this.Max.Y);
			}
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
				for (int i = 1; i < objects.Count; i++)
				{
					_Min.X = Math.Min(objects[i].Min.X, this.Min.X);
					_Min.Y = Math.Min(objects[i].Min.Y, this.Min.Y);
					_Max.X = Math.Max(objects[i].Max.X, this.Max.X);
					_Max.Y = Math.Max(objects[i].Max.Y, this.Max.Y);
				}
			}
		}

		private Point _Min;

		/// <summary>
		/// Gets or sets the lower left corner.
		/// </summary>
		public SharpMap.Geometries.Point Min
		{
			get { return _Min; }
			set { _Min = value; }
		}
		private SharpMap.Geometries.Point _Max;

		/// <summary>
		/// Gets or sets the upper right corner.
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
		/// Moves/translates the <see cref="BoundingBox"/> along the the specified vector
		/// </summary>
		/// <param name="vector">Offset vector</param>
		public void Offset(Point vector)
		{
			_Min += vector;
			_Max += vector;
		}

		/// <summary>
		/// Checks whether min values are actually smaller than max values and in that case swaps them.
		/// </summary>
		/// <returns>true if the bounding was changed</returns>
		public bool CheckMinMax()
		{
			bool wasSwapped = false;
			if (_Min.X > _Max.X)
			{
				double tmp = _Min.X;
				_Min.X = _Max.X;
				_Max.X = tmp;
				wasSwapped = true;
			}
			if (_Min.Y > _Max.Y)
			{
				double tmp = _Min.Y;
				_Min.Y = _Max.Y;
				_Max.Y = tmp;
				wasSwapped = true;
			}
			return wasSwapped;
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
				return new BoundingBox(Math.Min(this.Min.X , box.Min.X ), Math.Min(this.Min.Y , box.Min.Y ),
									   Math.Max(this.Max.X , box.Max.X ), Math.Max(this.Max.Y , box.Max.Y ));
		}
		/// <summary>
		/// Computes the joined boundingbox of two boundingboxes
		/// </summary>
		/// <param name="box1"></param>
		/// <param name="box2"></param>
		/// <returns></returns>
		public static BoundingBox Join(BoundingBox box1, BoundingBox box2)
		{
			if (box1 == null && box2==null)
				return null;
			else if (box1 == null)
				return box2.Clone();
			else
				return box1.Join(box2);
		}

		/// <summary>
		/// Increases the size of the boundingbox by the givent amount in all directions
		/// </summary>
		/// <param name="amount">Amount to grow in all directions</param>
		public BoundingBox Grow(double amount)
		{
			BoundingBox box = this.Clone();
			box.Min.X -= amount;
			box.Min.Y -= amount;
			box.Max.X += amount;
			box.Max.Y += amount;
			box.CheckMinMax();
			return box;
		}

		/// <summary>
		/// Increases the size of the boundingbox by the givent amount in horizontal and vertical directions
		/// </summary>
		/// <param name="amountInX">Amount to grow in horizontal direction</param>
		/// <param name="amountInY">Amount to grow in vertical direction</param>
		public BoundingBox Grow(double amountInX, double amountInY)
		{
			BoundingBox box = this.Clone();
			box.Min.X -= amountInX;
			box.Min.Y -= amountInY;
			box.Max.X += amountInX;
			box.Max.Y += amountInY;
			box.CheckMinMax();
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
		/// Computes the distance between this and another <see cref="BoundingBox"/>.
		/// The distance between overlapping bounding boxes is 0.  Otherwise, the
		/// distance is the Euclidean distance between the closest points.
		/// </summary>
		/// <param name="box">Box to calculate distance to</param>
		/// <returns>The distance between this and another <see cref="BoundingBox"/>.</returns>
		public virtual double Distance(BoundingBox box)
		{
			if (Intersects(box))
				return 0;

			double dx = 0;

			if (Right < box.Left)
				dx = box.Left - Right;
			if (Left > box.Right)
				dx = Left - box.Right;

			double dy = 0;

			if (Top < box.Bottom)
				dy = box.Bottom - Top;
			if (Bottom > box.Top)
				dy = Bottom - box.Top;

			if (dx == 0.0) return dy;
			if (dy == 0.0) return dx;

			return Math.Sqrt(Math.Pow(dx,2) + Math.Pow(dy,2));
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
			return String.Format(SharpMap.Map.numberFormat_EnUS, "{0},{1} {2},{3}", this.Min.X, this.Min.Y, this.Max.X, this.Max.Y);
		}

		#region IEquatable<BoundingBox> Members

		/// <summary>
		/// Indicates whether the current object is equal to another object of the same type.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			BoundingBox box = obj as BoundingBox;
			if (obj == null) return false;
			else return Equals(box);
		}

		/// <summary>
		/// Returns a hash code for the specified object
		/// </summary>
		/// <returns>A hash code for the specified object</returns>
		public override int GetHashCode()
		{
			return Min.GetHashCode() ^ Max.GetHashCode();
		}

		/// <summary>
		/// Checks whether the values of this instance is equal to the values of another instance.
		/// </summary>
		/// <param name="other"><see cref="BoundingBox"/> to compare to.</param>
		/// <returns>True if equal</returns>
		public bool Equals(BoundingBox other)
		{
			if(other==null) return false;
			return this.Left == other.Left && this.Right == other.Right && this.Top == other.Top && this.Bottom == other.Bottom;
		}

		#endregion
	}
}
