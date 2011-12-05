using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Utils.Collections;
using NUnit.Framework;

namespace DelftTools.Tests.Utils.Collections
{
    [TestFixture]
    public class EnumerableExtensionsTest
    {
        [Test]
        public void HasExactOneValue()
        {
            IEnumerable one = Enumerable.Range(1, 1);
            Assert.IsTrue(one.HasExactlyOneValue());

            //has two
            IEnumerable two= Enumerable.Range(1, 2);
            
            two.HasExactlyOneValue()
                .Should().Be.False();
            
            //has none
            Enumerable.Empty<double>().HasExactlyOneValue()
                .Should().Be.False();
        }
        
        [Test]
        public void ForEach()
        {
            var items = new [] {1, 2, 3};
            
            var results = new List<int>();

            items.ForEach(results.Add);

            results
                .Should("elements should be equal").Have.SameSequenceAs(items);
        }

        [Test]
        public void ForEachWithIndex()
        {
            var items = new[] {1, 2, 3};

            var resultIndices = new List<int>();
            var resultElements = new List<int>();

            items.ForEach((o, i) => { resultElements.Add(o); resultIndices.Add(i); });

            resultElements.Should().Have.SameSequenceAs(items);
            resultIndices.Should().Have.SameSequenceAs(new []{0, 1, 2});
        }
    }
}