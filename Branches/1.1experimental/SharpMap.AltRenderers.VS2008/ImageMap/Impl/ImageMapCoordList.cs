using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace SharpMap.Renderers.ImageMap.Impl
{
    internal abstract class ImageMapCoordList
        : IEnumerable<Point>
    {
        public int Count
        {
            get
            {
                return _internal.Count;
            }
        }

        public Point this[int index]
        {
            get
            {
                return _internal[index];
            }
        }

        private readonly List<Point> _internal = new List<Point>();
        public virtual bool Add(System.Drawing.Point p)
        {
            if (this.Count == 0)
            {
                _internal.Add(p);
                return true;
            }
            else if (Hypotenuse(p, this[Count - 1]) > 1)
            {
                _internal.Add(p);
                return true;
            }
            return false;
        }

        private static double Hypotenuse(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        public void Add(PointF pf)
        {
            Point p = new Point((int)pf.X, (int)pf.Y);
            this.Add(p);
        }

        //public void Add(SharpMap.Geometries.Point p)
        //{
        //    this.Add(new Point((int)p.X, (int)p.Y));
        //}

        public string GetCoordString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.Count; i++)
            {
                sb.AppendFormat("{0},{1}", this[i].X, this[i].Y);
            }

            string s = sb.ToString();
            if (s.Length > 0)
            {
                s = s.Substring(0, s.Length - 1);
            }
            return s;
        }


        public abstract double Weight
        {
            get;
        }



        #region IEnumerable<Point> Members

        public IEnumerator<Point> GetEnumerator()
        {
            return _internal.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }

}
