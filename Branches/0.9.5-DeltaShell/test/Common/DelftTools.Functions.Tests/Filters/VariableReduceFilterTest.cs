using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Filters
{
    [TestFixture]
    public class VariableReduceFilterTest
    {
        [Test]
        public void SimpleReduce()
        {
            IVariable<int> x = new Variable<int>("x");
            IVariable<int> y = new Variable<int>("y");
            IVariable<int> fx = new Variable<int>("fx");
            fx.Arguments.Add(x);
            fx.Arguments.Add(y);
            
            x.SetValues(new[] {1, 2, 3});
            y.SetValues(new[] {1});
            fx[3, 1] = 20;
            
            var reducedFunction = fx.Filter(new VariableValueFilter<int>(y, 1), new VariableReduceFilter(y));
            Assert.AreEqual(1, reducedFunction.Arguments.Count);
            Assert.AreEqual(3, reducedFunction.Values.Count);
            Assert.AreEqual(20, reducedFunction.Values[2]);
        }

        [Test]
        public void ReduceFitlterShouldPassIsEditableProperty()
        {
            IVariable<int> x = new Variable<int>("x"){IsEditable = false};
            IVariable<int> y = new Variable<int>("y");
            IVariable<int> fx = new Variable<int>("fx");
            fx.Arguments.Add(x);
            fx.Arguments.Add(y);

            x.SetValues(new[] { 1, 2, 3 });
            y.SetValues(new[] { 1 });
            fx[3, 1] = 20;

            var reducedFunction = fx.Filter(new VariableReduceFilter(y));
            Assert.AreEqual(1, reducedFunction.Arguments.Count);
            Assert.AreEqual("x", reducedFunction.Arguments[0].Name);
            Assert.IsFalse(reducedFunction.Arguments[0].IsEditable);
        }

    }
}