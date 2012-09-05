using System;
using DelftTools.Functions.Conversion;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests.Conversion
{
    [TestFixture]
    public class ConvertedVariableTest
    {
        [Test]
        public void IndependVariable()
        {
            IVariable<int> x = new Variable<int>("x") { Values = { 10 } };
            IVariable s = new ConvertedVariable<string, string, int>(x, x, Convert.ToInt32, Convert.ToString);
            
            Assert.AreEqual("10", s.Values[0]);
            s.Values[0] = "20";
            Assert.AreEqual(20, x.Values[0]);
            x.Values.Add(3);
            Assert.AreEqual(2, s.Values.Count);

            //convert it back to int does not because the converted variable loses type :(
            //IVariable intVariable = new ConvertedVariable<int, string>(s, (IVariable<string>) s, Convert.ToString, Convert.ToInt32);
        }

        [Test,Ignore("Work in progress")]
        public void DependendVariable()
        {
            IVariable<int> x = new Variable<int>("x");
            IVariable<int> y= new Variable<int>("y");
            y.Arguments.Add(x);

            IVariable<int> convertedY = new ConvertedVariable<int, string, int>(y, x, Convert.ToInt32, Convert.ToString);

            convertedY["20"] = 20;
            Assert.AreEqual(20, y[20]);
        }

        [Test]
        public void ConvertTwice()
        {
            //source==>strings==>ints
            IVariable<int> source = new Variable<int>();
            IVariable<string> strings = new ConvertedVariable<string, string, int>(source, source, Convert.ToInt32,
                                                                                   Convert.ToString);
            IVariable<int> ints = new ConvertedVariable<int, int, string>(strings, strings, Convert.ToString,
                                                                          Convert.ToInt32);
            ints.Values.Add(1);
            //assert all variables are updated.
            Assert.AreEqual(1,source.Values[0]);
            Assert.AreEqual("1", strings.Values[0]);
            Assert.AreEqual(1, ints.Values[0]);

            //this also works when adding to source
            source.Values.Add(2);
            Assert.AreEqual(2, source.Values[1]);
            Assert.AreEqual("2", strings.Values[1]);
            Assert.AreEqual(2, ints.Values[1]);
        }
    }
}