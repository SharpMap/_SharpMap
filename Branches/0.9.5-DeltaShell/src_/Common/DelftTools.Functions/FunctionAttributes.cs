namespace DelftTools.Functions
{
    /// <summary>
    /// A few string constants as used for attributes in netcdf variables
    /// </summary>
    public static class FunctionAttributes
    {
        // attributes used to enforce CF conventions
        public const string StandardName = "standard_name";
        public const string Units = "units";
        public const string Time = "time";

        public const string ConvertedType = "converted_type";
        public const string FunctionName = "function_name";
        public const string FunctionType = "function_type";
        public const string IsIndependend = "is_independend";
        public const string InterpolationType = "interpolation_type";
        public const string ExtrapolationType = "extrapolation_type";
    }
}