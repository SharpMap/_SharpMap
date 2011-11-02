namespace GeoAPI.Geometries
{
    /// <summary>
    /// Extensions methods to the coordinate class
    /// </summary>
    public static class CoordinateEx
    {
        /// <summary>
        /// Converts a coordinate to an array of <see cref="double"/> ordinates
        /// </summary>
        /// <param name="self">The coordinate to transfrom</param>
        /// <returns>The array of ordinates</returns>
        public static double[] ToDoubleArray(this Coordinate self)
        {
            if (self.Y != self.Y)
                return new[] { self.X, self.Y };
            return new[] { self.X, self.Y, self.Z };
        }

        /// <summary>
        /// Creates a <see cref="Coordinate"/> from an array of ordinates
        /// </summary>
        /// <param name="ordinates">the array of ordinates</param>
        /// <returns>The coordinate</returns>
        public static Coordinate FromDoubleArray(double[] ordinates)
        {
            var ret = new Coordinate(ordinates[0], ordinates[1]);
            if (ordinates.Length > 2) ret.Z = ordinates[2];
            return ret;
        }
    }

}
