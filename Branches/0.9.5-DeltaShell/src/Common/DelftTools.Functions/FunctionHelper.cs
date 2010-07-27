using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Xml;
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
            int i = values.Count - 1;
            while (i > -1  && (value.IsSmaller((IComparable)values[i])))
            {
                i--;
            }
            var itemIndex = i + 1;
            if (itemIndex == values.Count )
                return null;

            return (IComparable)values[itemIndex];
        }

        public static IComparable GetLastValueSmallerThan(IComparable value, IMultiDimensionalArray values)
        {
            int i = 0;
            while (i < values.Count &&((IComparable)values[i]).IsSmaller(value))
            {
                i++;
            }
            var itemIndex = i - 1;
            if (itemIndex < 0)
                return null;
            return (IComparable) values[itemIndex];
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
        public static IFunction GetSimpleFunction<TArg,TComp>(string functionName,string argumentName,string componentName) where TArg : IComparable where TComp : IComparable 
        {
            IFunction function = new Function {Name = functionName};
            function.Arguments.Add(new Variable<TArg>{Name = argumentName});
            function.Components.Add(new Variable<TComp> { Name = componentName});
            return function;
        }

        
        /// <summary>
        /// Add rows of a datatable to function. Assumes the first column is the argument and the second one the component.
        /// Also assument the table contains doubles
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="function"></param>
        public static void AddDataTableRowsToFunction(DataTable dataTable, IFunction function)
        {
            foreach (DataRow dataRow in dataTable.Rows)
            {
                double x = Convert.ToDouble(dataRow[0]);
                double y = Convert.ToDouble(dataRow[1]);
                function[x] = y;
            }
        }
    }
}