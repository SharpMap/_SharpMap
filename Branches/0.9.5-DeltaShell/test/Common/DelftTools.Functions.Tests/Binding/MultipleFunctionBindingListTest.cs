using System.Collections.Generic;
using DelftTools.Functions.Binding;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Binding
{
    [TestFixture]
    public class MultipleFunctionBindingListTest
    {
        [Test]
        public void MultipleFunctions()
        {
            IFunction function1 = new Function();

            function1.Arguments.Add(new Variable<int>("x"));
            function1.Components.Add(new Variable<double>("y1"));

            function1[0] = 1.0;
            function1[1] = 2.0;
            function1[2] = 3.0;

            IFunction function2 = new Function();

            function2.Arguments.Add(new Variable<int>("x"));
            function2.Components.Add(new Variable<double>("y2"));

            function2[0] = 1.0;
            function2[1] = 4.0;
            function2[2] = 9.0;

            List<IFunction> functions = new List<IFunction>() { function1, function2 };
            IFunctionBindingList functionBindingList = new MultipleFunctionBindingList(functions);

            Assert.AreEqual("x [-]", functionBindingList.DisplayNames[0]);
            Assert.AreEqual("y1 [-]", functionBindingList.DisplayNames[1]);
            Assert.AreEqual("y2 [-]", functionBindingList.DisplayNames[2]);

            MultipleFunctionBindingListRow firstRow = (MultipleFunctionBindingListRow)functionBindingList[0];
            Assert.AreEqual(0, firstRow["x [-]"]);
            Assert.AreEqual(1, firstRow["y1 [-]"]);
            Assert.AreEqual(1, firstRow["y2 [-]"]);

            MultipleFunctionBindingListRow secondRow = (MultipleFunctionBindingListRow)functionBindingList[1];
            Assert.AreEqual(1, secondRow["x [-]"]);
            Assert.AreEqual(2, secondRow["y1 [-]"]);
            Assert.AreEqual(4, secondRow["y2 [-]"]);
        }
    }
}
