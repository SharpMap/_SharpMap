using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using DelftTools.Functions.Generic;
using NUnit.Framework;

namespace DelftTools.Functions.Tests
{
    [TestFixture]
    public class MemoryFunctionStoreHelperTest
    {
        [Test]
        public void GetArgumentDependentVariables()
        {
            IFunction f = new Function();
            IVariable c1 = new Variable<int>("c1");
            IVariable c2 = new Variable<int>("c2");
            IVariable x = new Variable<int>("x");
            
            f.Components.Add(c1);
            f.Components.Add(c2);

            IDictionary<IVariable, IEnumerable<IVariable>> dependentVariables = MemoryFunctionStoreHelper.GetDependentVariables(f.Store.Functions);
            
            //no argument dependencies yet
            Assert.AreEqual(0, dependentVariables.Count);
            
            f.Arguments.Add(x);
            
            dependentVariables = MemoryFunctionStoreHelper.GetDependentVariables(f.Store.Functions);

            Assert.AreEqual(1, dependentVariables.Count);
            Assert.AreEqual(2, dependentVariables[x].Count());
            Assert.IsTrue(dependentVariables[x].Contains(c2));
            Assert.IsTrue(dependentVariables[x].Contains(c1));
        }

        [Test]
        public void GetComponentDependentVariables()
        {
            IFunction f = new Function();
            IVariable c1 = new Variable<int>("c1");
            IVariable c2 = new Variable<int>("c2");
            
            f.Components.Add(c1);
            f.Components.Add(c2);


            IDictionary<IVariable, IList<IVariable>> dependentVariables = MemoryFunctionStoreHelper.GetComponentsDependencyTable(f.Store.Functions);

            Assert.AreEqual(2, dependentVariables.Count);
            Assert.AreEqual(1, dependentVariables[c1].Count);
            Assert.AreEqual(1, dependentVariables[c2].Count);
            Assert.IsTrue(dependentVariables[c1].Contains(c2));
            Assert.IsTrue(dependentVariables[c2].Contains(c1));
        }
    }
}