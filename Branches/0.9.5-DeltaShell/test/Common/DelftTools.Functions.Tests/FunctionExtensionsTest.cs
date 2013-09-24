using System;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class FunctionExtensionsTest
    {
        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Function does not have a single argument and component.")]
        public void SetArgumentComponentValuesThrowsForInvalidNumberOfArguments()
        {
            var function = new Function();
            function.SetComponentArgumentValues(new[] { 1.0 }, new[] { 1.0 });
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Function component is not of type System.Double.")]
        public void SetArgumentComponentValuesThrowsForInvalidComponentValueType()
        {
            var function = FunctionHelper.Get1DFunction<int, int>();
            //set with double component values
            function.SetComponentArgumentValues(new[] { 1.0d}, new[] { 1 });
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "Function argument is not of type System.Double.")]
        public void SetArgumentComponentValuesThrowsForInvalidArgumentValueType()
        {
            var function = FunctionHelper.Get1DFunction<int, int>();
            //set with double arguemtn values
            function.SetComponentArgumentValues(new[] { 1 }, new[] { 1.0 });
        }

        [Test]
        public void SetArgumentComponentValues()
        {
            var function = FunctionHelper.Get1DFunction<int, int>();
            //set it!
            function.SetComponentArgumentValues(new[] {2}, new[] {1});

            Assert.AreEqual(new[] {1}, function.Arguments[0].Values);
            Assert.AreEqual(new[] {2}, function.Components[0].Values);
        }

        [Test]
        public void CopyToStore()
        {
            var function = FunctionHelper.Get1DFunction<double, double>();
            function[1.0d] = 2.0d;
            function[3.0d] = 3.3d;

            var targetStore = new MemoryFunctionStore();
            function.CopyToStore(targetStore);

            var copiedFunction = targetStore.Functions[0];
            Assert.AreEqual(function.Arguments.Count, copiedFunction.Arguments.Count);
            Assert.AreEqual(function.Components.Count, copiedFunction.Components.Count);
            Assert.AreEqual(function.Arguments[0].Values, copiedFunction.Arguments[0].Values);
            Assert.AreEqual(function.Components[0].Values, copiedFunction.Components[0].Values);
        }

        [Test]
        public void GetAllValuesOfFunctionWithTwoComponents()
        {
            IVariable x = new Variable<double>("x");
            IVariable l = new Variable<double>("left");
            IVariable r = new Variable<double>("right");

            IFunction f = new Function();
            f.Arguments.Add(x);
            f.Components.Add(l);
            f.Components.Add(r);

            f[0.0] = new[] { 10.0, 3.0 };
            f[1.0] = new[] { 20.0, 6.0 };
            
            // We need GetAllComponentValues function to retrieve ALL components, not just the first. TODO: FIX
            Assert.AreEqual( 10.0 , f[0.0]);
            Assert.AreEqual(new[] { 10.0, 3.0 }, f.GetAllComponentValues(0.0));
            Assert.AreEqual(new[] { 20.0, 6.0 }, f.GetAllComponentValues(1.0));
        }
    }
}