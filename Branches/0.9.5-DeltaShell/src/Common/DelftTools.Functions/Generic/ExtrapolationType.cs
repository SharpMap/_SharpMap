namespace DelftTools.Functions.Generic
{
    public enum ExtrapolationType
    {
        /// <summary>
        /// Performs a piece-wise interpolation. On extrapolation nearest defined value is used.
        /// </summary>
        Constant,
        /// <summary>
        /// Provider linear inter- and extra-polation
        /// </summary>
        Linear,

        /// <summary>
        /// Repeats given values . For a example if values 1,2,3 are defined then the value 4 will be the same as value 2.
        /// </summary>
        Periodic,

        None
    }
}