using System.ComponentModel;

namespace DelftTools.Functions
{
    /// <summary>
    /// Specifies how the Multidimensionalarray is changed
    /// </summary>
    public class MultiDimensionalArrayDimensionChangeAction
    {
        //TODO: provides equals on array change action
        public CollectionChangeAction ChangeAction { get; set; }

        public int Dimension { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
    }
}