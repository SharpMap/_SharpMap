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

namespace SharpMap.Rendering
{

	/// <summary>
	/// Defines an axis-aligned box around a label, used for collision detection
	/// </summary>
	public class LabelBox : IComparable<LabelBox>
	{
		/// <summary>
		/// Initializes a new LabelBox instance
		/// </summary>
		/// <param name="left">Left side of box</param>
		/// <param name="top">Top of box</param>
		/// <param name="width">Width of the box</param>
		/// <param name="height">Height of the box</param>
		public LabelBox(float left, float top, float width, float height)
		{
			_left = left;
			_top = top;
			_width = width;
			_height = height;
		}

		/// <summary>
		/// Initializes a new LabelBox instance based on a rectangle
		/// </summary>
		/// <param name="rectangle"></param>
		public LabelBox(System.Drawing.RectangleF rectangle)
		{
			_left = rectangle.X;
			_top = rectangle.Y;
			_width = rectangle.Width;
			_height = rectangle.Height;
		}

		private float _left;

		/// <summary>
		/// The Left tie-point for the Label
		/// </summary>
		public float Left
		{
			get { return _left; }
			set { _left = value; }
		}
		private float _top;

		/// <summary>
		/// The Top tie-point for the label
		/// </summary>
		public float Top
		{
			get { return _top; }
			set { _top = value; }
		}
		private float _width;

		/// <summary>
		/// Width of the box
		/// </summary>
		public float Width
		{
			get { return _width; }
			set { _width = value; }
		}

		private float _height;

		/// <summary>
		/// Height of the box
		/// </summary>
		public float Height
		{
			get { return _height; }
			set { _height = value; }
		}

		/// <summary>
		/// Right side of the box
		/// </summary>
		public float Right
		{
			get { return _left + _width; }
		}

		/// <summary>
		/// Bottom of th ebox
		/// </summary>
		public float Bottom
		{
			get { return _top - _height; }
		}

		/// <summary>
		/// Determines whether the boundingbox intersects another boundingbox
		/// </summary>
		/// <param name="box"></param>
		/// <returns></returns>
		public bool Intersects(LabelBox box)
		{
			return !(box.Left > this.Left+this.Width ||
					 box.Left+box.Width < this.Left ||
					 box.Top-box.Height > this.Top ||
					 box.Top < this.Top-this.Height);
		}

		#region IComparable<LabelBox> Members

		/// <summary>
		/// Returns 0 if the boxes intersects each other
		/// </summary>
		/// <param name="other">labelbox to perform intersectiontest with</param>
		/// <returns>0 if the intersect</returns>
		public int CompareTo(LabelBox other)
		{
			if (this.Intersects(other))
				return 0;
			else if (other.Left > this.Left+this.Width ||
				other.Top - other.Height > this.Top)
				return 1;
			else
				return -1;
		}

		#endregion
	}

	/// <summary>
	/// Class for storing a label instance
	/// </summary>
	public class Label : IComparable<Label>, IComparer<Label>
	{
		/// <summary>
		/// Initializes a new Label instance
		/// </summary>
		/// <param name="text">Text to write</param>
		/// <param name="labelpoint">Position of label</param>
		/// <param name="rotation">Rotation</param>
		/// <param name="priority">Label priority used for collision detection</param>
		/// <param name="collisionbox">Box around label for collision detection</param>
		/// <param name="style">The style of the label</param>
		public Label(string text, System.Drawing.PointF labelpoint, float rotation, int priority, LabelBox collisionbox, SharpMap.Styles.LabelStyle style)
		{
			_Text = text;
			_LabelPoint = labelpoint;
			_Rotation = rotation;
			_Priority = priority;
			_box = collisionbox;
			_Style = style;
		}

		private string _Text;

		/// <summary>
		/// The text of the label
		/// </summary>
		public string Text
		{
			get { return _Text; }
			set { _Text = value; }
		}

		private System.Drawing.PointF _LabelPoint;

		/// <summary>
		/// Label position
		/// </summary>
		public System.Drawing.PointF LabelPoint
		{
			get { return _LabelPoint; }
			set { _LabelPoint = value; }
		}

		private System.Drawing.Font _Font;

		/// <summary>
		/// Label font
		/// </summary>
		public System.Drawing.Font Font
		{
			get { return _Font; }
			set { _Font = value; }
		}

		private float _Rotation;

		/// <summary>
		/// Label rotation
		/// </summary>
		public float Rotation
		{
			get { return _Rotation; }
			set { _Rotation = value; }
		}
		private int _Priority;

		/// <summary>
		/// Text rotation in radians
		/// </summary>
		public int Priority
		{
			get { return _Priority; }
			set { _Priority = value; }
		}

		private LabelBox _box;

		/// <summary>
		/// Label box
		/// </summary>
		public LabelBox Box
		{
			get { return _box; }
			set { _box = value; }
		}

		private SharpMap.Styles.LabelStyle _Style;

		/// <summary>
		/// Gets or sets the <see cref="SharpMap.Styles.LabelStyle"/> of this label
		/// </summary>
		public SharpMap.Styles.LabelStyle Style
		{
			get { return _Style; }
			set { _Style = value; }
		}
	

		#region IComparable<Label> Members

		/// <summary>
		/// Tests if two label boxes intersects
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(Label other)
		{
			if (this == other)
				return 0;
			else if (_box == null)
				return -1;
			else if (other.Box == null)
				return 1;
			else
				return _box.CompareTo(other.Box);
		}

		#endregion

		
		#region IComparer<Label> Members

		/// <summary>
		/// Checks if two labels intersect
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public int Compare(Label x, Label y)
		{
			return x.CompareTo(y);
		}

		#endregion
	}
}
