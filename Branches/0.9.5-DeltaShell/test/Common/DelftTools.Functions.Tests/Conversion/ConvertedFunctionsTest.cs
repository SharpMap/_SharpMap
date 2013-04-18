using System;
using System.Linq;
using DelftTools.Functions.Conversion;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Conversion
{
    [TestFixture]
    public class ConvertedFunctionsTest
    {
        [Test]
        public void IndependVariable()
        {
            IVariable<int> x = new Variable<int>("x") { Values = { 10 } };
            IVariable s =  new ConvertedVariable<string,string, int>(x,x,Convert.ToInt32,Convert.ToString);
            
            Assert.AreEqual("10", s.Values[0]);
            s.Values[0] = "20";
            Assert.AreEqual(20,x.Values[0]);
            x.Values.Add(3);
            Assert.AreEqual(2,s.Values.Count);
        }

        [Test]
        public void ConvertedArgumentFunction()
        {
            IFunction func = new Function();
            IVariable<int> x = new Variable<int>("x");
            var fx = new Variable<int>();
            func.Arguments.Add(x);
            func.Components.Add(fx);
            func[10] = 4;

            IFunction convertedFunction = new ConvertedFunction<string, int>(func, x, Convert.ToInt32, Convert.ToString);
            Assert.AreEqual(1,convertedFunction.Arguments.Count);
            Assert.AreEqual(1, convertedFunction.Components[0].Arguments.Count);
            //notice both argument and component are converted
            Assert.IsTrue(convertedFunction.Arguments[0] is IVariable<string>);
            Assert.IsTrue(convertedFunction.Components[0] is IVariable<int>);
            //notice the argument has been converted to a string variable
            Assert.AreEqual(4 , convertedFunction["10"]);
            Assert.AreEqual(4 , convertedFunction.Components[0].Values[0]);
            //arguments of components are converted as well :)
            Assert.AreEqual(4, convertedFunction.Components[0]["10"]);
            convertedFunction["30"] = 10;
            IMultiDimensionalArray<string> strings = (IMultiDimensionalArray<string>) convertedFunction.Arguments[0].Values;
            Assert.IsTrue(new[] { "10", "30" }.SequenceEqual( strings));
        }

        [Test]
        public void ConvertOneArgumentOfTwoDimensionalFunction()
        {
            IFunction func = new Function();
            IVariable<int> x = new Variable<int>("x");
            IVariable<DateTime> t = new Variable<DateTime>("t");
            var fx = new Variable<int>();
            func.Arguments.Add(x);
            func.Arguments.Add(t);
            func.Components.Add(fx);
            DateTime t0 = DateTime.Now;
            func[10,t0] = 4;

            IFunction convertedFunction = new ConvertedFunction<string, int>(func, x, Convert.ToInt32, Convert.ToString);
            //notice both argument and component are converted
            Assert.IsTrue(convertedFunction.Arguments[0] is IVariable<string>);
            Assert.IsTrue(convertedFunction.Components[0] is IVariable<int>);
            //notice the argument has been converted to a string variable
            Assert.AreEqual(4, convertedFunction["10",t0]);
            Assert.AreEqual(4, convertedFunction.Components[0].Values[0,0]);
            //arguments of components are converted as well :)
            Assert.AreEqual(4, convertedFunction.Components[0]["10",t0]);
            convertedFunction["30",t0] = 10;
            IMultiDimensionalArray<string> strings = (IMultiDimensionalArray<string>)convertedFunction.Arguments[0].Values;
            Assert.IsTrue(new[] { "10", "30" }.SequenceEqual(strings));
            
        }

        [Test]
        [Ignore("work in progress")]
        public void ConvertedComponentFunction()
        {
            IFunction func = new Function();
            IVariable<int> x = new Variable<int>("x");
            var fx = new Variable<int>();
            func.Arguments.Add(x);
            func.Components.Add(fx);
            func[10] = 4;

            IFunction convertedFunction = new ConvertedFunction<string, int>(func, fx, Convert.ToInt32, Convert.ToString);
            Assert.AreEqual("4", convertedFunction[10]);
            convertedFunction[11] = "5";
            Assert.AreEqual(5,func[11]);
        }
    }
}