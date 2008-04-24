using System;
using System.Drawing;

namespace SharpMap.Renderers.ImageMap.Impl
{


    internal class LineStringCoordList
        : ImageMapCoordList
    {

        private double length = 0;


        public override bool Add(Point p)
        {
            if (base.Add(p))
            {
                if (this.Count > 1)
                {
                    Point p2 = this[this.Count - 2];

                    length += Math.Abs(Math.Pow(p2.X - p.X, 2) + Math.Pow(p2.Y - p.Y, 2));
                }
                return true;
            }
            else
                return false;
        }

        public override double Weight
        {
            get { return length; }
        }
    }
}
