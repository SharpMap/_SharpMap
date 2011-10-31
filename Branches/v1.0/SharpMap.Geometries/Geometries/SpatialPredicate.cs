namespace SharpMap.Geometries
{
    /// <summary>
    /// An enumeration of spatial predicates
    /// </summary>
    public enum SpatialPredicate
    {
        /// <summary>
        /// Predicate that checks two geometries for equality.
        /// </summary>
        Equals,

        /// <summary>
        /// Predicate that checks two geometries for any intersection.
        /// </summary>
        Intersects,

        /// <summary>
        /// Predicate that checks if the intersection of two geometries results in an object of the same dimension as the
        /// input geometries and the intersection geometry is not equal to either geometry.
        /// </summary>
        Overlaps,
        
        /// <summary>
        /// Predicate that checks if the second geometry is wholly contained within the first geometry. This is the same as
        /// reversing the primary and comparison shapes of the <see cref="Within"/> predicate.
        /// </summary>
        Contains,

        /// <summary>
        /// Predicate that checks if the primary geometry is wholly contained within the comparison geometry.
        /// </summary>
        Within,
        
        /// <summary>
        /// 
        /// </summary>
        Covers,

        CoveredBy,

        /// <summary>
        /// Predicate that checks if the intersection of two geometries results in a geometry whose dimension is less than
        /// the maximum dimension of the two geometries and the intersection geometry is not equal to either.
        /// geometry.
        /// </summary>
        Crosses,

        /// <summary>
        /// Predicate that checks if two gemetries do not have anything in common.
        /// </summary>
        Disjoint,

        /// <summary>
        /// Predicate that checks if one geometry touches another geometry.
        /// </summary>
        Touches,

    }
}