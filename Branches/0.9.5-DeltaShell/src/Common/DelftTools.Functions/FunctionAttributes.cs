namespace DelftTools.Functions
{
    /// <summary>
    /// A few string constants as used for attributes in netcdf variables
    /// TODO: check what names we can use from: http://cf-pcmdi.llnl.gov/documents/cf-standard-names/standard-name-table/19/cf-standard-name-table.html
    /// http://cf-pcmdi.llnl.gov/documents/cf-conventions/latest-cf-conventions-document-1
    /// TODO: check INSPIRE documents for recommended variable names
    /// </summary>
    public static class FunctionAttributes
    {
        public static string LocationType = "location";
        public static string Global = "global";
        public static string QBoundary = "QBoundary";
        public static string HBoundary = "HBoundary";
        public const string QhBoundary = "QhBoundary";
        public const string Qt = "Qt";
        public const string Ht = "Ht";


        // attributes used to enforce CF conventions
        public const string StandardName = "standard_name";
        public const string StandardFeatureName = "standard_feature_name";
        public const string Units = "units";
        public const string AggregationType = "aggregation_type";

        // TODO: move to DelftTools.Hydro.StandardNames and define here e.g. StandardNames repository/aggregate, etc.  
        public static class StandardNames
        {

            // Coordinate
            public const string Longitude_X = "longitude";
            public const string Latitude_Y = "latitude";

            //todo: In the future, make this hierarchical (for example: 'river_water_discharge', 'water_discharge')

            /// <summary>
            /// Q(h) Boundary
            /// </summary>
            public const string WaterLevelTable = "water_level_table";

            public const string WaterFlowArea = "water_flow_area";
            public const string FroudeNumber = "froude_number";
            public const string WaterLevelGradient = "water_level_gradient";
            public const string WaterConveyance = "water_conveyance";
            public const string WaterHydraulicRadius = "water_hydraulic_radius";
            public const string SaltDispersion = "salt_dispersion";
            public const string WaterSalinity = "water_salinity";
            public const string WaterSalinityTop = "water_salinity_top";
            public const string WaterDensity = "water_density";
            public const string WaterDischarge = "water_discharge";
            public const string WaterVelocity = "water_velocity";
            public const string WaterVolume = "water_volume";
            public const string WaterLevel = "water_level";
            public const string WaterDepth = "water_depth";
            public const string WaterSurfaceArea = "water_surface_area";
            public const string WindVelocity = "wind_velocity";
            public const string WindDirection = "wind_direction";
            public const string RtcTimeCondition = "rtc_timecondition";
            public const string RtcTimeRule = "rtc_timerule";
            public const string RtcPidRule = "rtc_pidrule";
            public const string RtcIntervalRule = "rtc_intervalrule";


            //property _of_ structure
            public const string StructureSetPoint = "structure_setpoint";
            public const string StructureValveOpening = "structure_valve_opening";
            public const string StructureGateOpeningHeight = "structure_gate_opening_height";
            public const string StructureGateLowerEdgeLevel = "structure_gate_lower_edge_level";
            public const string StructureCrestWidth = "structure_crest_width";
            public const string StructureCrestLevel = "structure_crest_level";

            //property _at_ structure [CF: 'surface': at_structure/at_crest_of_structure] ['component': downstream/upstream]
            public const string StructureWaterLevelAtCrest = "at_crest_of_structure_water_level";
            public const string StructurePressureDifference = "at_structure_water_pressure_difference";
            public const string StructureWaterHead = "at_structure_water_head";
            public const string StructureWaterLevelDownstream = "at_structure_downstream_water_level";
            public const string StructureWaterLevelUpstream = "at_structure_upstream_water_level";

            //DFlow-FM quantities
            public const string DamHeight = "dam_height";
            public static string AirPressure = "air_pressure";
            public static string RainFallRate = "rainfall_rate";
        }

        public static class StandardUnits
        {
            public const string Meters = "meters";
            public const string Long_X_Degr = "degrees_east";
            public const string Lat_Y_Degr = "degrees_north";
        }

        public static class StandardFeatureNames
        {
            public const string FiniteVolumeGridPoint = "finite_volume_grid_point";
            public const string ReachSegment = "reach_segment";
            public const string GridPoint = "grid_point";
            public const string Structure = "structure";
            public const string ObservationPoint = "observation_point";
            public const string LateralSource = "lateral_source";
            public const string Retention = "retention"; 
        }

        public static class AggregationTypes
        {
            public const string None = "none"; // no aggregation
            public const string Maximum = "maximum";
            public const string Minimum = "minimum";
            public const string Average = "average";
        }
    }
}