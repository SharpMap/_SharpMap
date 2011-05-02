using System.ComponentModel;
using System.Linq;
using DelftTools.Hydro.Structures;
using DelftTools.Hydro.Structures.WeirFormula;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using ValidationAspects;

namespace DelftTools.DataObjects.Tests.HydroNetwork.Structures
{
    [TestFixture]
    public class WeirTest
    {
        [Test]
        public void DefaultWeir()
        {
            IWeir weir = new Weir("test");
            Assert.IsTrue(weir.Validate().IsValid);
        }

        [Test]
        public void NegativeCrestWidth()
        {
            IWeir weir = new Weir("test") {CrestWidth = (-10)};
            var validationResult = weir.Validate();
            Assert.IsFalse(validationResult.IsValid);
            Assert.AreEqual(1, validationResult.Messages.Count());
            Assert.AreEqual(weir, validationResult.ValidationException.Context.Instance);
        }

        [Test]
        public void CloneWeir()
        {
            IWeir weir = new Weir
                             {
                                 Geometry = new Point(7, 0),
                                 OffsetY = 175,
                                 CrestWidth = 75,
                                 CrestLevel = -3,
                                 Name = "Weir one",
                                 AllowNegativeFlow = true,
                             };
            IWeir clonedWeir = (IWeir) weir.Clone();

            Assert.AreEqual(clonedWeir.Name, weir.Name);
            Assert.AreEqual(clonedWeir.OffsetY, weir.OffsetY);
            Assert.AreEqual(clonedWeir.Geometry, weir.Geometry);
            Assert.AreEqual(clonedWeir.CrestWidth, weir.CrestWidth);
            Assert.AreEqual(clonedWeir.CrestLevel, weir.CrestLevel);
        }

        [Test]
        public void IsGated()
        {
            IWeir simpleweir = new Weir("simple") { };
            IWeir gatedweir = new Weir("gated") { WeirFormula = new GatedWeirFormula()};

            Assert.IsFalse(simpleweir.IsGated);
            Assert.IsTrue(gatedweir.IsGated);

        }

        [Test]
        public void IsRectangle()
        {
            IWeir simpleweir = new Weir("simple") { };
            IWeir freeformweir = new Weir("freeform") { WeirFormula = new FreeFormWeirFormula() };

            Assert.IsTrue(simpleweir.IsRectangle);
            Assert.IsFalse(freeformweir.IsRectangle);
        }

        [Test]
        public void Allow()
        {
            IWeir simpleweir = new Weir("simple");

            simpleweir.AllowNegativeFlow = true;
            simpleweir.AllowPositiveFlow = true;

            Assert.IsTrue(simpleweir.AllowNegativeFlow);
            Assert.IsTrue(simpleweir.AllowPositiveFlow);

            simpleweir.AllowPositiveFlow = false;

            Assert.IsTrue(simpleweir.AllowNegativeFlow);
            Assert.IsFalse(simpleweir.AllowPositiveFlow);

            simpleweir.AllowNegativeFlow = false;

            Assert.IsFalse(simpleweir.AllowNegativeFlow);
            Assert.IsFalse(simpleweir.AllowPositiveFlow);

            simpleweir.AllowNegativeFlow = true;
            simpleweir.AllowPositiveFlow = true;

            Assert.IsTrue(simpleweir.AllowNegativeFlow);
            Assert.IsTrue(simpleweir.AllowPositiveFlow);

        }

        [ExpectedException]
        public void AllowFlowDirectionException()
        {
            IWeir riverweir = new Weir("river") { WeirFormula = new RiverWeirFormula() };
            riverweir.AllowNegativeFlow = true;
        }

        [ExpectedException]
        public void AllowFlowDirectionException2()
        {
            IWeir pierweir = new Weir("pier") { WeirFormula = new PierWeirFormula() };
            pierweir.AllowNegativeFlow = true;
        }

        [Test]
        public void BindingTest()
        {
            var  pierweir = new Weir("pier") { WeirFormula = new PierWeirFormula() };
            int callCount = 0;

            ((INotifyPropertyChanged)pierweir).PropertyChanged += (s, e) =>
            //pierweir.PropertyChanged += (s, e) =>
                                            {
                                                callCount++;
                                                Assert.AreEqual(pierweir, s);
                                                Assert.AreEqual("CrestShape", e.PropertyName);
                                            };
            pierweir.CrestShape = CrestShape.Triangular;
            Assert.AreEqual(1,callCount);
        }

        [Test]
        public void PropertyChangedForFormula()
        {
            //translates the event so view etc don't have to know about formula. 
            //requires a hack in weir that can be removed with new PS (hopefully)
            var formula = new GatedWeirFormula();
            var pierweir = new Weir("pier") { WeirFormula = formula };

            int callCount = 0;
            ((INotifyPropertyChanged) pierweir).PropertyChanged += (s, e) =>
                                                                       {
                                                                           Assert.AreEqual(new[]{"GateOpening","WeirFormula"}[callCount], e.PropertyName);
                                                                           Assert.AreEqual(new object[]{formula,pierweir}[callCount], s);
                                                                           callCount++;
                                                                       };

            formula.GateOpening = 22.0;
            Assert.AreEqual(2,callCount);
        }
    }
}
