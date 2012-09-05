using System.ComponentModel;

namespace DelftTools.Functions.Binding
{
    public interface IMultiDimensionalArrayBindingList : IBindingList
    {
        IMultiDimensionalArray Array { get; set; }

        int ColumnDimension { get; set; }

        int RowDimension { get; set; }

        string[] ColumnNames { get; set; }
    }
}