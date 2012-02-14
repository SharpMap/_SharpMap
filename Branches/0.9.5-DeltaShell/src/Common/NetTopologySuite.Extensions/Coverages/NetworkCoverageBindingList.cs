using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    //Assumption is a network coverage as function. Network Coverage may be time dependent and contains at most one network location argument.
    public class NetworkCoverageBindingList : FunctionBindingList
    {
        public static string ColumnNameBranch = "Location_Branch";
        public static string ColumnNameOffset = "Location_Chainage";
        public static string DisplayNameBranch = "Branch";
        public static string DisplayNameOffset = "Chainage";

        private readonly bool baseDoesNotAllowNew;

        public INetworkCoverage NetworkCoverage 
        { 
            get { return (INetworkCoverage) Function; }
        }

        public NetworkCoverageBindingList(IFunction function) : base(function)
        {
            if (!(function is INetworkCoverage))
            {
                throw new ArgumentException("Expected function to be of type INetworkCoverage");
            }

            if (!AllowNew)
            {
                baseDoesNotAllowNew = true;
            }

            if (IsValidNetworkCoverage())
            {
                NetworkCoverage.Network.Branches.CollectionChanged += BranchesCollectionChanged;
            }
            UpdateAllowNew();
        }
        
        void BranchesCollectionChanged(object sender, DelftTools.Utils.Collections.NotifyCollectionChangingEventArgs e)
        {
            UpdateAllowNew();
        }

        /// <summary>
        /// We can only add new rows (network locations) if there is at least one branch.
        /// </summary>
        private void UpdateAllowNew()
        {
            if (baseDoesNotAllowNew)
            {
                return; //don't set AllowNew=true if our base didn't want it in the first place
            }

            AllowNew = IsValidNetworkCoverage() && NetworkCoverage.Network.Branches.Count > 0;
        }

        private bool IsValidNetworkCoverage()
        {
            return NetworkCoverage.Network != null && NetworkCoverage.Network.Branches != null;
        }

        public override void Dispose()
        {
            if (IsValidNetworkCoverage())
            {
                NetworkCoverage.Network.Branches.CollectionChanged -= BranchesCollectionChanged;
            }
            base.Dispose();
        }

        protected override FunctionBindingListRow CreateEmptyBindingListRow()
        {
            return new NetworkCoverageBindingListRow(this);
        }

        public override string[] ColumnNames
        {
            get
            {
                var list = base.ColumnNames.ToList();
                list.RemoveAt(GetIndexOfNetworkLocationArgument(this.Function));
                list.Insert(0, ColumnNameBranch);
                list.Insert(1, ColumnNameOffset);
                return list.ToArray();
            }
        }

        public override string[] DisplayNames
        {
            get
            {
                var list = base.DisplayNames.ToList();
                list.RemoveAt(GetIndexOfNetworkLocationArgument(this.Function));
                list.Insert(0, DisplayNameBranch);
                list.Insert(1, DisplayNameOffset);
                return list.ToArray();
            }
        }

        public override PropertyDescriptorCollection GetItemProperties(PropertyDescriptor[] listAccessors)
        {
            PropertyDescriptorCollection descriptorCollection = base.GetItemProperties(listAccessors);

            bool locationColumnsAdded = false;

            for (int i = 0; i < descriptorCollection.Count; i++ )
            {
                var descriptor = descriptorCollection[i];

                if (locationColumnsAdded)
                {
                    ((FunctionBindingListPropertyDescriptor) descriptor).index += 1;
                    continue;
                }

                if (descriptor.PropertyType == typeof(INetworkLocation))
                {
                    descriptorCollection.RemoveAt(i); //remove network location, add two entries instead
                    descriptorCollection.Insert(i, new FunctionBindingListPropertyDescriptor(ColumnNameBranch, DisplayNameBranch, typeof(IBranch), i));
                    i++;
                    descriptorCollection.Insert(i, new FunctionBindingListPropertyDescriptor(ColumnNameOffset, DisplayNameOffset, typeof(double), i));
                    locationColumnsAdded = true;
                }
            }

            return descriptorCollection;
        }

        public static int GetIndexOfNetworkLocationArgument(IFunction function)
        {
            return function.Arguments.ToList().FindIndex(arg => arg.ValueType == typeof(INetworkLocation));
        }
    }
}
