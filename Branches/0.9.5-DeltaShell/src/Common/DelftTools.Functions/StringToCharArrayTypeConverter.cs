using System;

namespace DelftTools.Functions
{
    public class StringToCharArrayTypeConverter : TypeConverterBase<string>
    {
        public override Type[] StoreTypes
        {
            get { return new[] { typeof(char[]) }; }
        }

        public override string[] VariableNames
        {
            get { return new[] { "char_array" }; }
        }

        public override string[] VariableStandardNames
        {
            get { return new string[] { null }; }
        }

        public override string[] VariableUnits
        {
            get { return new string[] { null }; }
        }

        public override string ConvertFromStore(object source)
        {
            var tuple = (object[])source;
            return new string((char[])tuple[0]).TrimEnd();
        }

        public override object[] ConvertToStore(string source)
        {
            return new[] {ConvertToCharArray(source)};
        }
    }
}
