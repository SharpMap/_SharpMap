using System;

namespace SharpMap.Renderers.ImageMap.Impl
{
    internal class PolygonCoordList
          : ImageMapCoordList
    {
        private System.Drawing.Point min = new System.Drawing.Point(int.MaxValue, int.MaxValue);
        private System.Drawing.Point max = new System.Drawing.Point(int.MinValue, int.MinValue);


        public override bool Add(System.Drawing.Point p)
        {
            if (base.Add(p))
            {
                if (this.Count == 0)
                {
                    min = p;
                    max = p;
                }
                else
                {
                    min = new System.Drawing.Point(Math.Min(p.X, min.X), Math.Min(p.Y, min.Y));
                    max = new System.Drawing.Point(Math.Max(p.X, max.X), Math.Max(p.Y, max.Y));
                }
                return true;
            }
            else
                return false;
        }

        public override double Weight
        {
            get
            {
                return Math.Abs(max.X - min.X) * Math.Abs(max.Y - min.Y);

            }
        }
    }

}
