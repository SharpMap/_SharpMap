using System;
using DelftTools.Functions.Binding;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    public class NetworkCoverageBindingListRow : FunctionBindingListRow
    {
        private new NetworkCoverageBindingList owner
        {
            get { return (NetworkCoverageBindingList) base.owner; }
        }

        public NetworkCoverageBindingListRow(NetworkCoverageBindingList owner) : base(owner)
        {
        }

        protected override int GetColumnIndex(string columnName)
        {
            int firstIndex = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnName.Equals(NetworkCoverageBindingList.ColumnNameBranch))
            {
                return firstIndex;
            }
            if (columnName.Equals(NetworkCoverageBindingList.ColumnNameChainage))
            {
                return firstIndex + 1;
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
                }
                return base[baseIndex];
            }
            set
            {
                int baseIndex = ColumnIndexToBaseIndex(columnIndex);

                var oldLocation = (base[baseIndex] as INetworkLocation) ?? new NetworkLocation(null, 0);

                if (ColumnIsBranchColumn(columnIndex))
                {
                    var newLocation = new NetworkLocation((IBranch) value, oldLocation.Chainage);
                    base[baseIndex] = newLocation;
                }
                else if (ColumnIsChainageColumn(columnIndex))
                {
                    var newLocation = new NetworkLocation(oldLocation.Branch, Convert.ToDouble(value));
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
            int firstIndexOfNetworkLocationArgument = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex == firstIndexOfNetworkLocationArgument)
                return true;

            return false;
        }

        private bool ColumnIsChainageColumn(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex == firstIndexOfNetworkLocationArgument+1)
                return true;

            return false;
        }

        private int ColumnIndexToBaseIndex(int columnIndex)
        {
            int firstIndexOfNetworkLocationArgument = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            if (columnIndex > firstIndexOfNetworkLocationArgument)
                return columnIndex - 1;

            return columnIndex;
        }

        private int BaseIndexToColumnIndex(int baseColumnIndex)
        {
            int firstIndexOfNetworkLocationArgument = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

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
            int indexOfNetworkLocationInBase = NetworkCoverageBindingList.GetIndexOfNetworkLocationArgument(owner.Function);

            return (INetworkLocation)base[indexOfNetworkLocationInBase];
        }
    }
}
