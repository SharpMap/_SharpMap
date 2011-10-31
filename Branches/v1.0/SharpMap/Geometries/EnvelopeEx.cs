namespace SharpMap.Geometries
{
    using System;
    using System.IO;

    using GeoAPI.Geometries;

    /// <summary>
    /// Extensions methods on <see cref="Envelope"/>
    /// </summary>
    public static class EnvelopeEx
    {
        internal static Envelope Read(BinaryReader br)
        {
            var bytes = new byte[32];
            br.Read(bytes, 0, 32);

            var doubles = new double[4];
            Buffer.BlockCopy(bytes, 0, doubles, 0, 32);
            
            return new Envelope( new Coordinate(doubles[0], doubles[1]),
                                 new Coordinate(doubles[2], doubles[3]));
        }

        /// <summary>
        /// This bounding box only contains valid ordinates
        /// </summary>
        public static bool IsValid(this Envelope self)
        {
                return (!double.IsNaN(self.MinX) && !double.IsNaN(self.MaxX) &&
                        !double.IsNaN(self.MinY) && !double.IsNaN(self.MaxY));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static Envelope Join(this Envelope self, Envelope other)
        {
            if (other == null)
                return self.Clone();

            return new Envelope(Math.Min(self.MinX, other.MinX), Math.Min(self.MinY, other.MinY),
                                Math.Max(self.MaxX, other.MaxX), Math.Max(self.MaxY, other.MaxY));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static double Left(this Envelope self)
        {
            return self.MinX;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static double Right(this Envelope self)
        {
            return self.MaxX;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static double Bottom(this Envelope self)
        {
            return self.MinY;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static double Top(this Envelope self)
        {
            return self.MaxY;
        }

        public static Coordinate BottomLeft(this Envelope self)
        {
            return new Coordinate(self.MinX, self.MinX);
        }

        public static Coordinate TopLeft(this Envelope self)
        {
            return new Coordinate(self.MinX, self.MaxX);
        }

        public static Coordinate BottomRight(this Envelope self)
        {
            return new Coordinate(self.MaxX, self.MinY);
        }

        public static Coordinate TopRight(this Envelope self)
        {
            return new Coordinate(self.MaxX, self.MaxY);
        }

        public static IPoint GetCentroid(this Envelope self)
        {
            return GeometryServices.Instance.GeometryFactory.CreatePoint(new Coordinate(self.MinX + self.Width * 0.5d,  self.MinY + self.Height * 0.5d));
        }

        public static Ordinate LongestAxis(this Envelope self)
        {
            return (Ordinate)((self.Width >= self.Height) ? 0 : 1);
        }

        public static double GetArea(this Envelope self)
        {
            return self.Area;
        }

        /// <summary>
        /// Computes the minimum distance between this <see cref="Envelope"/> and a <see cref="Coordinate"/>
        /// </summary>
        /// <param name="p"><see cref="IPoint"/> to calculate distance to.</param>
        /// <returns>Minimum distance.</returns>
        public static double Distance(this Envelope self, Coordinate p)
        {
            var ret = 0.0;

            if (p.X < self.MinX)
                ret += Math.Pow(self.MinX - p.X, 2d);
            else if (p.X > self.MaxX)
                ret += Math.Pow(p.X - self.MaxX, 2d);

            if (p.Y < self.MinY)
                ret += Math.Pow(self.MinY - p.Y, 2d);
            else if (p.Y > self.MaxY)
                ret += Math.Pow(p.Y - self.MaxY, 2d);

            /*
            for (uint cIndex = 0; cIndex < 2; cIndex++)
            {
                if (p[cIndex] < Min[cIndex]) 
                    ret += Math.Pow(Min[cIndex] - p[cIndex], 2.0);
                else if (p[cIndex] > Max[cIndex]) 
                    ret += Math.Pow(p[cIndex] - Max[cIndex], 2.0);
            }
            */
            return Math.Sqrt(ret);
        }

        /// <summary>
        /// Returns true if this instance touches the <see cref="Coordinate"/>
        /// </summary>
        /// <param name="p">Geometry</param>
        /// <returns>True if touches</returns>
        public static bool Touches(this Envelope self, Coordinate p)
        {
            if ((self.MinX > p.X && self.MinX < p.X) ||
                (self.MaxX > p.X && self.MaxX < p.X))
                return true;

            if ((self.MinY > p.Y && self.MinY < p.X) ||
                (self.MaxY > p.Y && self.MaxY < p.X))
                return true;

            /*
            for (uint cIndex = 0; cIndex < 2; cIndex++)
            {
                if ((Min[cIndex] > p[cIndex] && Min[cIndex] < p[cIndex]) ||
                    (Max[cIndex] > p[cIndex] && Max[cIndex] < p[cIndex]))
                    return true;
            }
             */
            return false;
        }


    }
}