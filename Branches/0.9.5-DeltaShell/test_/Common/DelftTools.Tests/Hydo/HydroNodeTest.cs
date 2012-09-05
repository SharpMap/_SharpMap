using System.Collections.Generic;
using DelftTools.Hydro;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Networks;
using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class HydroNodeTest
    {
        private MockRepository mocks = new MockRepository();

        [Test]
        public void Clone()
        {
            var node = new HydroNode("naam") {LongName = "Long"};
            var clone = (HydroNode)node.Clone();

            //todo expand to cover functionality
            Assert.AreEqual(node.LongName,clone.LongName);
        }

        [Test]
        public void UnlinksOnBranchAdd()
        {
            var source = mocks.Stub<ILinkSource>();
            var branch = mocks.Stub<IBranch>();
            var branch2 = mocks.Stub<IBranch>();

            var outgoingLinks = new EventedList<ILinkDestination>();

            source.Stub(s => s.OutgoingLinks).Return(outgoingLinks).Repeat.Any();

            mocks.ReplayAll();

            var node = new HydroNode();
            node.OutgoingBranches.Add(branch);

            Assert.IsTrue(node.IsBoundaryNode);

            node.IncomingLinks.Add(source);
            source.OutgoingLinks.Add(node);
            
            node.IncomingBranches.Add(branch2);

            Assert.IsFalse(node.IsBoundaryNode);

            Assert.AreEqual(0, outgoingLinks.Count);
            Assert.AreEqual(0, node.IncomingLinks.Count);
        }
    }
}