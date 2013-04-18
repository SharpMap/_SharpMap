using System;
using System.Collections;
using System.ComponentModel;

namespace DelftTools.Utils
{
    /// <summary>
    /// Helps to display properties in a specific order in the property grid.
    /// </summary>
    public class PropertySorter : ExpandableObjectConverter
    {
        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value,
                                                                   Attribute[] attributes)
        {
            //
            // This override returns a list of properties in order
            //
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(value, attributes);
            var orderedProperties = new ArrayList();
            foreach (PropertyDescriptor pd in pdc)
            {
                Attribute attribute = pd.Attributes[typeof (PropertyOrderAttribute)];
                if (attribute != null)
                {
                    //
                    // If the attribute is found, then create an pair object to hold it
                    //
                    var orderAttribute = (PropertyOrderAttribute) attribute;
                    orderedProperties.Add(new PropertyOrderPair(pd.Name, orderAttribute.Order));
                }
                else
                {
                    //
                    // If no order attribute is specifed then given it an order of 0
                    //
                    orderedProperties.Add(new PropertyOrderPair(pd.Name, 0));
                }
            }
            //
            // Perform the actual order using the value PropertyOrderPair classes
            // implementation of IComparable to sort
            //
            orderedProperties.Sort();
            //
            // Build a string list of the ordered names
            //
            var propertyNames = new ArrayList();
            foreach (PropertyOrderPair pop in orderedProperties)
            {
                propertyNames.Add(pop.Name);
            }
            //
            // Pass in the ordered list for the PropertyDescriptorCollection to sort by
            //
            return pdc.Sort((string[]) propertyNames.ToArray(typeof (string)));
        }

        public class PropertyOrderPair : IComparable
        {
            private readonly string name;
            private readonly int order;

            public PropertyOrderPair(string name, int order)
            {
                this.order = order;
                this.name = name;
            }

            public string Name
            {
                get { return name; }
            }

            #region IComparable Members

            public int CompareTo(object obj)
            {
                //
                // Sort the pair objects by ordering by order value
                // Equal values get the same rank
                //
                int otherOrder = ((PropertyOrderPair)obj).order;
                if (otherOrder == order)
                {
                    //
                    // If order not specified, sort by name
                    //
                    string otherName = ((PropertyOrderPair)obj).name;
                    return string.Compare(name, otherName);
                }
                if (otherOrder > order)
                {
                    return -1;
                }
                return 1;
            }

            #endregion
        }
    }
}