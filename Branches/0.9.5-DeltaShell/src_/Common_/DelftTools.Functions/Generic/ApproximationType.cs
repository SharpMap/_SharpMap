namespace DelftTools.Functions.Generic
{
    public enum ApproximationType
    {
        /// <summary>
        /// Performs a piece-wise interpolation. On extrapolation nearest defined value is used.
        /// </summary>
        Constant,
        /// <summary>
        /// Provider linear inter- and extra-polation
        /// </summary>
        Linear,

        None
    }
}