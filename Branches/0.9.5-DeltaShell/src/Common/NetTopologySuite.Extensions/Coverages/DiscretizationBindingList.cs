using System;
using System.ComponentModel;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Binding;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;

namespace NetTopologySuite.Extensions.Coverages
{
    // TODO: merge with FunctionBindingList, Function API allows everything what we need here?
    //Assumption is a network coverage as function. Network Coverage may be time dependent and contains at most one network location argument.
    public class DiscretizationBindingList : FunctionBindingList
    {
        public static string ColumnNameBranch = "Location_Branch";
        public static string ColumnNameChainage = "Location_Chainage";
        public static string ColumnNameLocationName = "Location_Name";
        public static string DisplayNameBranch = "Branch";
        public static string DisplayNameChainage = "Chainage";
        public static string DisplayNameLocationName = "Name";

        private readonly bool baseDoesNotAllowNew;

        public IDiscretization Discretization 
        { 
            get { return (IDiscretization) Function; }
        }

        public DiscretizationBindingList(IFunction function) : base(function)
        {
            if (!(function is IDiscretization))
            {
                throw new ArgumentException("Expected function to be of type IDiscretization");
            }

            if (!AllowNew)
            {
                baseDoesNotAllowNew = true;
            }

            if (IsValidDiscretization())
            {
                Discretization.Network.Branches.CollectionChanged += BranchesCollectionChanged;
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

            AllowNew = IsValidDiscretization() && Discretization.Network.Branches.Count > 0;
        }

        private bool IsValidDiscretization()
        {
            return Discretization.Network != null && Discretization.Network.Branches != null;
        }

        public override void Dispose()
        {
            if (IsValidDiscretization())
            {
                Discretization.Network.Branches.CollectionChanged -= BranchesCollectionChanged;
            }
            base.Dispose();
        }

        protected override FunctionBindingListRow CreateEmptyBindingListRow()
        {
            return new DiscretizationBindingListRow(this);
        }

        public override string[] ColumnNames
        {
            get
            {
                var list = base.ColumnNames.ToList();
                list.RemoveAt(GetIndexOfNetworkLocationArgument(this.Function));
                list.Insert(0, ColumnNameBranch);
                list.Insert(1, ColumnNameChainage);
                list.Insert(2, ColumnNameLocationName);
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
                list.Insert(1, DisplayNameChainage);
                list.Insert(2, DisplayNameLocationName);
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
                    ((FunctionBindingListPropertyDescriptor) descriptor).index += 2;
                    continue;
                }

                if (descriptor.PropertyType == typeof(INetworkLocation))
                {
                    descriptorCollection.RemoveAt(i); //remove network location, add three entries instead
                    descriptorCollection.Insert(i, new FunctionBindingListPropertyDescriptor(ColumnNameBranch, DisplayNameBranch, typeof(IBranch), i));
                    i++;
                    descriptorCollection.Insert(i, new FunctionBindingListPropertyDescriptor(ColumnNameChainage, DisplayNameChainage, typeof(double), i));
                    i++;
                    descriptorCollection.Insert(i, new FunctionBindingListPropertyDescriptor(ColumnNameLocationName, DisplayNameLocationName, typeof(string), i,true));
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
