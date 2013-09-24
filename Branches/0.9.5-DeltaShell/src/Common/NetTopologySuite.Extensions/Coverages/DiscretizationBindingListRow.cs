using System;
using DelftTools.Functions.Binding;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    public class DiscretizationBindingListRow : FunctionBindingListRow
    {
        private new DiscretizationBindingList owner
        {
            get { return (DiscretizationBindingList) base.owner; }
        }

        public DiscretizationBindingListRow(DiscretizationBindingList owner) : base(owner)
        {
        }

        protected override int GetColumnIndex(string columnName)
        {
            int firstIndex = DiscretizationBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnName.Equals(DiscretizationBindingList.ColumnNameBranch))
            {
                return firstIndex;
            }
            if (columnName.Equals(DiscretizationBindingList.ColumnNameChainage))
            {
                return firstIndex + 1;
            }
            if (columnName.Equals(DiscretizationBindingList.ColumnNameLocationName))
            {
                return firstIndex + 2;
            }
            return BaseIndexToColumnIndex(base.GetColumnIndex(columnName));
        }

        public override object this[int columnIndex]
        {
            get
            {
                int baseIndex = ColumnIndexToBaseIndex(columnIndex);

                var networkLocation = base[baseIndex] as INetworkLocation;
                if (networkLocation != null)
                {
                    if (ColumnIsBranchColumn(columnIndex))
                    {
                        return networkLocation.Branch;
                    }
                    if (ColumnIsChainageColumn(columnIndex))
                    {
                        return networkLocation.Chainage;
                    }
                    if (ColumnIsLocationNameColumn(columnIndex))
                    {
                        return networkLocation.Name;
                    }
                }
                return base[baseIndex];
            }
            set
            {
                int baseIndex = ColumnIndexToBaseIndex(columnIndex);

                var oldLocation = (base[baseIndex] as INetworkLocation) ?? new NetworkLocation(null, 0);

                if (ColumnIsBranchColumn(columnIndex))
                {
                    var newLocation = new NetworkLocation((IBranch)value, oldLocation.Chainage);
                    base[baseIndex] = newLocation;
                }
                else if (ColumnIsChainageColumn(columnIndex))
                {
                    var newLocation = new NetworkLocation(oldLocation.Branch, Convert.ToDouble(value));
                    base[baseIndex] = newLocation;
                }
                else if (ColumnIsLocationNameColumn(columnIndex))
                {
                    var newLocation = new NetworkLocation(oldLocation.Branch, oldLocation.Chainage) { Name = Convert.ToString(value) };
                    base[baseIndex] = newLocation;
                }
                else
                {
                    base[baseIndex] = value;
                }
            }
        }
        
        public override object this[string columnName]
        {
            get
            {
                return this[GetColumnIndex(columnName)];
            }
            set
            {
                this[GetColumnIndex(columnName)] = value;
            }
        }

        #region IndexHelpers

        private bool ColumnIsBranchColumn(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = DiscretizationBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex == firstIndexOfNetworkLocationArgument)
                return true;

            return false;
        }

        private bool ColumnIsChainageColumn(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = DiscretizationBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex == firstIndexOfNetworkLocationArgument + 1)
                return true;

            return false;
        }

        private bool ColumnIsLocationNameColumn(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = DiscretizationBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex == firstIndexOfNetworkLocationArgument + 2)
                return true;

            return false;
        }

        private int ColumnIndexToBaseIndex(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = DiscretizationBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex > firstIndexOfNetworkLocationArgument + 1)
                return columnIndex - 2;

            if (columnIndex > firstIndexOfNetworkLocationArgument)
                return columnIndex - 1;

            return columnIndex;
        }

        private int BaseIndexToColumnIndex(int baseColumnIndex)
        {
            int firstIndexOfNetworkLocationArgument = DiscretizationBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (baseColumnIndex > firstIndexOfNetworkLocationArgument + 1)
                return baseColumnIndex + 2;

            if (baseColumnIndex > firstIndexOfNetworkLocationArgument)
                return baseColumnIndex + 1;

            return baseColumnIndex;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        #endregion

        public INetworkLocation GetNetworkLocation()
        {
            int indexOfNetworkLocationInBase = DiscretizationBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            return (INetworkLocation)base[indexOfNetworkLocationInBase];
        }
    }
}
