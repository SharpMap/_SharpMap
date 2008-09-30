using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpMap.Rendering
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ViewRectangle2D : IViewMatrix, IComparable<ViewRectangle2D>
    {
        private double _left;
        private double _top;
        private double _right;
        private double _bottom;

        public ViewRectangle2D(double left, double right, double top, double bottom)
        {
            _left = left;
            _right = right;
            _top = top;
            _bottom = bottom;
        }

        public ViewRectangle2D(ViewPoint2D location, ViewSize2D size)
        {
            _left = location.X;
            _top = location.Y;
            _right = _left + size.Width;
            _bottom = _top + size.Height;
        }

        public double X
        {
            get { return _left; }
        }

        public double Y
        {
            get { return _top; }
        }

        public double Left
        {
            get { return _left; }
            private set { _left = value; }
        }

        public double Top
        {
            get { return _top; }
            private set { _top = value; }
        }

        public double Right
        {
            get { return _right; }
            private set { _right = value; }
        }

        public double Bottom
        {
            get { return _bottom; }
            private set { _bottom = value; }
        }

        public ViewPoint2D Center
        {
            get { return new ViewPoint2D(X + Width / 2, Y + Height / 2); }
        }

        public ViewPoint2D Location
        {
            get { return new ViewPoint2D(_left, _top); }
        }

        public ViewSize2D Size
        {
            get { return new ViewSize2D(Width, Height); }
        }

        public double Width
        {
            get { return _right - _left; }
        }

        public double Height
        {
            get { return _bottom - _top; }
        }

        public static bool operator ==(ViewRectangle2D rect1, ViewRectangle2D rect2)
        {
            return rect1.Left == rect2.Left &&
                rect1.Right == rect2.Right &&
                rect1.Top == rect2.Top &&
                rect1.Bottom == rect2.Bottom;
        }

        public static bool operator !=(ViewRectangle2D rect1, ViewRectangle2D rect2)
        {
            return !(rect1 == rect2);
        }

        public override bool Equals(object obj)
        {
            if (obj is IViewMatrix)
                return Equals(obj as IViewMatrix);

            if (!(obj is ViewRectangle2D))
                return false;

            ViewRectangle2D other = (ViewRectangle2D)obj;

            return other == this;
        }

        public override int GetHashCode()
        {
            return unchecked((int)Left ^ (int)Right ^ (int)Top ^ (int)Bottom);
        }

        public override string ToString()
        {
            return String.Format("ViewRectangle - Left: {0:N3}; Top: {1:N3}; Right: {2:N3}; Bottom: {3:N3}", Left, Top, Right, Top, Bottom);
        }

        /// <summary>
        /// Determines whether this <see cref="Rectangle"/> intersects another.
        /// </summary>
        /// <param name="rectangle"><see cref="Rectangle"/> to check intersection with.</param>
        /// <returns>True if there is intersection, false if not.</returns>
        public bool Intersects(ViewRectangle2D rectangle)
        {
            return !(rectangle.Left > Right ||
                     rectangle.Right < Left ||
                     rectangle.Bottom > Top ||
                     rectangle.Top < Bottom);
        }

        #region IComparable<Rectangle> Members

        /// <summary>
        /// Compares this <see cref="Rectangle"/> instance with another instance.
        /// </summary>
        /// <remarks>
        /// </remarks>
        /// <param name="other">Rectangle to perform intersection test with.</param>
        /// <returns>
        /// Returns 0 if the <see cref="Rectangle"/> instances intersect each other,
        /// 1 if this Rectangle is located to the right or down from the <paramref name="other"/>
        /// Rectange, and -1 if this Rectangle is located to the left or up from the other.
        /// </returns>
        public int CompareTo(ViewRectangle2D other)
        {
            if (this.Intersects(other))
                return 0;

            if (other.Left > Right || other.Top > Bottom)
                return -1;
            
            return 1;
        }

        #endregion

        internal static ViewRectangle2D FromLTRB(double p, double p_2, double p_3, double p_4)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #region IViewMatrix Members

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public void Invert()
        {
            throw new NotSupportedException();
        }

        public bool IsInvertible
        {
            get { return false; }
        }

        public double[,] Elements
        {
            get 
            {
                return new double[,] { { Left, Top }, { Right, Bottom } };
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                if (value.Rank != 2 || value.GetUpperBound(0) != 2)
                    throw new ArgumentException("Elements can be set only to a 2x2 array");

                Left = value[0, 0] < value[1, 0] ? value[0, 0] : value[1, 0];
                Top = value[0, 1] < value[1, 1] ? value[0, 1] : value[1, 1];
                Right = value[0, 0] < value[1, 0] ? value[1, 0] : value[0, 0];
                Bottom = value[0, 1] < value[1, 1] ? value[1, 1] : value[0, 1];
            }
        }

        public void Rotate(double degreesTheta)
        {
            throw new NotSupportedException();
        }

        public void RotateAt(double degreesTheta, IViewVector center)
        {
            throw new NotSupportedException();
        }

        public double GetOffset(int dimension)
        {
            throw new NotSupportedException();
        }

        public void Offset(IViewVector offsetVector)
        {
            Translate(offsetVector);
        }

        public void Multiply(IViewMatrix matrix)
        {
            throw new NotSupportedException();
        }

        public void Scale(double scaleAmount)
        {
            double newWidth = Width * scaleAmount;
            double newHeight = Height * scaleAmount;

            Right = Left + newWidth;
            Bottom = Top + newHeight;
        }

        public void Scale(IViewVector scaleVector)
        {
            if (scaleVector.Elements.Length != 2)
                throw new ArgumentOutOfRangeException("scaleVector", scaleVector, "Argument vector must have two elements");

            double newWidth = Width * scaleVector[0];
            double newHeight = Height * scaleVector[1];

            Right = Left + newWidth;
            Bottom = Top + newHeight;
        }

        public void Translate(double translationAmount)
        {
            Left += translationAmount;
            Top += translationAmount;
            Right += translationAmount;
            Bottom += translationAmount;
        }

        public void Translate(IViewVector translationVector)
        {
            if (translationVector.Elements.Length != 2)
                throw new ArgumentOutOfRangeException("translationVector", translationVector, "Argument vector must have two elements");

            double xDelta = translationVector[0];
            double yDelta = translationVector[1];

            Left += xDelta;
            Right += xDelta;

            Top += yDelta;
            Bottom += yDelta;
        }

        #endregion

        #region ICloneable Members

        public object Clone()
        {
            return new ViewRectangle2D(Left, Right, Top, Bottom);
        }

        #endregion

        #region IEquatable<IViewMatrix> Members

        public bool Equals(IViewMatrix other)
        {
            double[,] lhs = this.Elements, rhs = other.Elements;

            if (lhs.Length != rhs.Length)
                return false;

            if (lhs.Rank != rhs.Rank)
                return false;

            int rowCount = lhs.GetUpperBound(0);
            int colCount = lhs.GetUpperBound(1);

            if (rowCount != rhs.GetUpperBound(0) || 0 != rhs.GetLowerBound(0))
                return false;

            if (colCount != rhs.GetUpperBound(1) || 0 != rhs.GetLowerBound(1))
                return false;

            unchecked
            {
                for (int row = 0; row < rowCount; row++)
                    for (int col = 0; col < colCount; col++)
                        if (lhs[row, col] != rhs[row, col])
                            return false;
            }

            return true;
        }

        #endregion
    }
}
