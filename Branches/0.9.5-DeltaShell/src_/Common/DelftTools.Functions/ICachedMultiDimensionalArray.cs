namespace DelftTools.Functions
{
    public interface ICachedMultiDimensionalArray : IMultiDimensionalArray
    {
        /// <summary>
        /// Retrieves values for view in a efficient way
        /// </summary>
        /// <param name="view"></param>
        void Cache(IMultiDimensionalArrayView view);

        /// <summary>
        /// Frees cache for the given view
        /// </summary>
        /// <param name="view"></param>
        void Free(IMultiDimensionalArrayView view);

        /// <summary>
        /// 
        /// </summary>
        //IList<IMultiDimensionalArrayView> CachedViews { get; }
    }
}