using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils;
using DelftTools.Utils.Reflection;

namespace DelftTools.Functions
{
    public static class FunctionHelper
    {
        public static string ToXml(Function function)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode functionNode = doc.CreateElement("function");
            var nameAttribute = doc.CreateAttribute("name");
            nameAttribute.Value = function.Name;
            functionNode.Attributes.Append(nameAttribute);
            doc.AppendChild(functionNode);

            XmlNode argumentsNode = doc.CreateElement("arguments");
            functionNode.AppendChild(argumentsNode);
            foreach (IVariable argument in function.Arguments)
            {
                argumentsNode.InnerXml += argument.ToXml();
            }

            XmlNode componentsNode = doc.CreateElement("components");
            functionNode.AppendChild(componentsNode);
            foreach (IVariable component in function.Components)
            {
                componentsNode.InnerXml += component.ToXml();
            }

            return doc.InnerXml;
        }

        /// <summary>
        /// Divides a single enumerable into multiple one's. This is used when setting multiple values like
        /// f[0] = {1,2} for a two component function. The values are divived among the components by order.
        /// This could be slow. For fast access set component values directly
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="variables"></param>
        /// <returns></returns>
        public static IList<IEnumerable> SplitEnumerable(IEnumerable enumerable, IList<Type> types)
        {
            IList<IEnumerable> lists = new List<IEnumerable>();

            if (types.Count == 1) //do something faster here, this case happens alot
            {
                lists.Add(enumerable);
                return lists;
            }

            //create a bunch of lists for each variable one.
            foreach (Type t in types)
            {
                lists.Add(TypeUtils.GetTypedList(t));
            }
            
            //divide the values...cannot split the enum like stride because it is not single-typed
            int i = 0;
            foreach (var obj in enumerable)
            {
                //try to do some conversion if we need it
                if (!(obj.GetType()==types[i%types.Count]))
                {
                    //cannot move value into add strangely...
                    object value = Convert.ChangeType(obj,types[i%types.Count]);
                    ((IList)lists[i++ % lists.Count]).Add(value);
                }
                else
                {
                    ((IList)lists[i++ % lists.Count]).Add(obj);    
                }
            }
            //number of values is not divideable among types
            if(( i % types.Count) != 0)
            {
                throw new ArgumentException("Invalid number of component values. Expected a tuple of "+types.Count);
            }
            return lists;
        }

        public static IComparable GetFirstValueBiggerThan(IComparable value, IList values)
        {
            ArrayList adapterList = ArrayList.Adapter(values);
            int itemIndex = adapterList.BinarySearch(value);

            itemIndex = itemIndex < 0 ? ~itemIndex : itemIndex + 1; //interpreting binary search results

            if (itemIndex >= values.Count )
                return null;
    
            return (IComparable)values[itemIndex];
        }

        public static IComparable GetLastValueSmallerThan(IComparable value, IMultiDimensionalArray values)
        {
            ArrayList adapterList = ArrayList.Adapter(values);
            int itemIndex = adapterList.BinarySearch(value);

            //index of first bigger element:
            itemIndex = itemIndex < 0 ? ~itemIndex : itemIndex + 1; //interpreting binary search results
            
            for(int i = itemIndex - 1; i >= 0; i--) //find first smaller element
            {
                if (((IComparable)values[i]).IsSmaller(value))
                    return (IComparable)values[i];
            }

            return null;
        }

        public static IFunction Get1DFunction<TArg,TComp>() where TArg : IComparable where TComp : IComparable
        {
            return Get1DFunction<TArg, TComp>("F", "x", "y");
        }
        /// <summary>
        /// Returns a function with one argument and one component
        /// </summary>
        /// <typeparam name="TArg">Argument type</typeparam>
        /// <typeparam name="TComp">Component type</typeparam>
        /// <param name="functionName"></param>
        /// <param name="argumentName"></param>
        /// <param name="componentName"></param>
        /// <returns></returns>
        public static IFunction Get1DFunction<TArg,TComp>(string functionName,string argumentName,string componentName) where TArg : IComparable where TComp : IComparable 
        {
            IFunction function = new Function {Name = functionName};
            function.Arguments.Add(new Variable<TArg> {Name = argumentName});
            function.Components.Add(new Variable<TComp> { Name = componentName});
            return function;
        }

        ///// <summary>
        ///// Add rows of a datatable to function. Assumes the first column is the argument and the second one the component.
        ///// Also assument the table contains doubles
        ///// </summary>
        ///// <param name="dataTable"></param>
        ///// <param name="function"></param>
        //public static void AddDataTableRowsToFunction(DataTable dataTable, IFunction function)
        //{
        //    foreach (DataRow dataRow in dataTable.Rows)
        //    {
        //        double x = Convert.ToDouble(dataRow[0]);
        //        double y = Convert.ToDouble(dataRow[1]);
        //        function[x] = y;
        //    }
        //}
        /// <summary>
        /// Add rows of a datatable to function. Assumes the first column is the argument next ones the components.
        /// Order of columns should match order of components.
        /// If number of columns - 1 is less than number of components these components are not set
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="function"></param>
        public static void AddDataTableRowsToFunction(DataTable dataTable, IFunction function)
        {
            if (dataTable.Columns.Count > function.Arguments.Count + function.Components.Count)
            {
                throw new ArgumentException(
                    "Too many columns in datatable", "dataTable");
            }
            if (1 != function.Arguments.Count)
            {
                throw new ArgumentException(
                    "Only function with 1 argument are supported", "function");
            }
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var x = Convert.ChangeType(dataRow[0], function.Arguments[0].ValueType);
                object[] values = new object[dataTable.Columns.Count - 1];
                for (int i = 1; i < dataTable.Columns.Count; i++)
                {
                    values[i - 1] = Convert.ChangeType(dataRow[i], function.Components[i - 1].ValueType);
                }
                function[x] = values;
            }
        }

        public static void CopyValuesFrom(IFunction source, IFunction target)
        {
            if (source.Arguments.Count != target.Arguments.Count)
            {
                throw new ArgumentException("Number of arguments in source and target do not match.", "source");
            }
            if (source.Components.Count != target.Components.Count)
            {
                throw new ArgumentException("Number of components in source and target do not match.", "source");
            }
            if (source.Arguments.Count != 1)
            {
                throw new ArgumentException("only support for 1 argument.", "source");
            }
            target.Clear();
            target.Arguments[0].SetValues(source.Arguments[0].Values);
            var values = source.Arguments[0].Values;
            foreach (var value in values)
            {
                // How to do this more efficient?
                for (int i = 0; i < source.Components.Count; i++ )
                {
                    target.Components[i][value] = source.Components[i][value];
                }
            }
        }

        public static IFunction CreateSimpleFunction(IFunctionStore store)
        {
            var function = new Function("test");

            store.Functions.Add(function);

            // initialize schema
            IVariable x = new Variable<double>("x", 3);
            IVariable y = new Variable<double>("y", 2);
            IVariable f1 = new Variable<double>("f1");

            function.Arguments.Add(x);
            function.Arguments.Add(y);
            function.Components.Add(f1);


            // write some data
            var xValues = new double[] {0, 1, 2};
            var yValues = new double[] {0, 1};
            var fValues = new double[] {100, 101, 102, 103, 104, 105};

            function.SetValues(fValues,
                               new VariableValueFilter<double>(x, xValues),
                               new VariableValueFilter<double>(y, yValues),
                               new ComponentFilter(f1));
            return function;
        }
    }
}