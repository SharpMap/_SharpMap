using System.Linq;
using DelftTools.Hydro.Structures;
using NUnit.Framework;
using ValidationAspects;

namespace DelftTools.DataObjects.Tests.HydroNetwork.Structures
{
    [TestFixture]
    public class CompositeStructureTest
    {
        [Test]
        public void EmptyCompositeStructure()
        {
            var compositeBranchStructure = new CompositeBranchStructure();
            var validationResult = compositeBranchStructure.Validate();
            Assert.AreEqual(false, validationResult.IsValid);

            Assert.AreEqual(1, validationResult.Messages.Count());
            Assert.AreEqual(compositeBranchStructure, validationResult.ValidationException.Context.Instance);
        }
        [Test]
        public void CompositeStructure1SimpleWeir()
        {
            var compositeBranchStructure = new CompositeBranchStructure();
            IWeir weir = new Weir("test");
            compositeBranchStructure.Structures.Add(weir);

            var validationResult = compositeBranchStructure.Validate();
            Assert.AreEqual(true, validationResult.IsValid);
        }

        [Test]
        public void CompositeStructure1InvalidWeir()
        {
            var compositeBranchStructure = new CompositeBranchStructure();
            IWeir weir = new Weir("test") {CrestWidth = (-1)};
            compositeBranchStructure.Structures.Add(weir);

            var validationResult = compositeBranchStructure.Validate();
            Assert.AreEqual(false, validationResult.IsValid);
            Assert.AreEqual(compositeBranchStructure, validationResult.ValidationException.Context.Instance);
        }

        [Test]
        public void Composite2IdenticalWeirs()
        {
            // 2 identical and thus overlapping weirs
            var compositeBranchStructure = new CompositeBranchStructure();
            IWeir weir1 = new Weir("test");
            compositeBranchStructure.Structures.Add(weir1);
            IWeir weir2 = new Weir("test");
            compositeBranchStructure.Structures.Add(weir2);

            var validationResult = compositeBranchStructure.Validate();
            // offsetY is representing property only and should not generate validation error
            Assert.IsTrue(validationResult.IsValid);
        }

        [Test]
        public void Composite2NeightbourWeirs()
        {
            // 2 identical and thus overlapping weirs
            var compositeBranchStructure = new CompositeBranchStructure();
            IWeir weir1 = new Weir("test") {OffsetY = 0, CrestWidth = 100};
            compositeBranchStructure.Structures.Add(weir1);
            IWeir weir2 = new Weir("test") {OffsetY = 100, CrestWidth = 100};
            compositeBranchStructure.Structures.Add(weir2);

            var validationResult = compositeBranchStructure.Validate();
            Assert.AreEqual(true, validationResult.IsValid);
        }
    }
}
