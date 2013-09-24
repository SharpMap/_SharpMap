namespace GeoAPI.Extensions.Coverages
{
    public enum SegmentGenerationMethod
    {
        /// <summary>
        ///                b1
        /// n1---------------------------------n2
        /// 
        /// *----------*---------*----------*--    - network locations
        /// [----][--------][--------][--------]   - segments
        /// </summary>
        SegmentPerLocation,


        /// <summary>
        ///                b1
        /// n1---------------------------------n2
        /// 
        /// A----------C---------B----------D--    - network locations
        /// [------------------->]                 - segments
        ///            [<--------]                 - 
        ///            [------------------->]      - 
        /// </summary>
        RouteBetweenLocations,


        /// <summary>
        ///                b1
        /// n1---------------------------------n2
        /// 
        /// A----------C---------B----------D--    - network locations
        /// [---------][--------][----------]      - segments
        /// </summary>
        SegmentBetweenLocations,


        /// <summary>
        ///                b1
        /// n3-------------n1---------------------------------n2----------n4
        /// 
        ///                -----A----------C---------B--------D--            - user defined network locations
        /// x              x x                                  x         x  - automatically added location
        /// [--------------][--][----------][-------][--------][----------]  - segments
        /// </summary>
        SegmentBetweenLocationsFullyCovered,


        /// <summary>
        ///                b1
        /// n1---------------------------------n2
        /// 
        /// A----------C---------B----------D--    - network locations
        ///                                        - segments
        /// </summary>
        None
    };
}