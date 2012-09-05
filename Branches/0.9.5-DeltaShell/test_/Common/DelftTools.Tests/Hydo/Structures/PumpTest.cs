using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions;
using DelftTools.Hydro.Structures;
using NUnit.Framework;
using ValidationAspects;

namespace DelftTools.DataObjects.Tests.HydroNetwork.Structures
{
    [TestFixture]
    public class PumpTest
    {
        [Test]
        public void DefaultPump()
        {
            Pump pump = new Pump("testPump");
            Assert.IsTrue(pump.Validate().IsValid);
        }

        [Test]
        public void Clone()
        {
            var pump = new Pump("Kees") {LongName = "Long"};
            var clone = (Pump) pump.Clone();

            Assert.AreEqual(clone.LongName, pump.LongName);
        }

        [Test]
        public void CopyFrom()
        {
            var targetPump = new Pump();
            var sourcePump = new Pump
                                 {
                                     Name = "target",
                                     Capacity = 42.0,
                                     StartDelivery = 1,
                                     StopDelivery = 0,
                                     StartSuction = 4.0,
                                     StopSuction = 3.0,
                                     DirectionIsPositive = false,
                                     ControlDirection = PumpControlDirection.DeliverySideControl,
                                     ReductionTable =
                                         FunctionHelper.Get1DFunction<double, double>("reduction", "differrence",
                                                                                          "factor")
                                 };
            targetPump.CopyFrom(sourcePump);
            Assert.AreEqual(sourcePump.Attributes, targetPump.Attributes);
            Assert.AreEqual(sourcePump.Capacity, targetPump.Capacity);
            Assert.AreEqual(sourcePump.StopDelivery, targetPump.StopDelivery);
            Assert.AreEqual(sourcePump.StartDelivery, targetPump.StartDelivery);
            Assert.AreEqual(sourcePump.StartSuction, targetPump.StartSuction);
            Assert.AreEqual(sourcePump.StopSuction, targetPump.StopSuction);
            Assert.AreEqual(sourcePump.ControlDirection, targetPump.ControlDirection);
            Assert.AreEqual(sourcePump.OffsetY, targetPump.OffsetY);
            Assert.AreEqual(sourcePump.DirectionIsPositive, targetPump.DirectionIsPositive);
            for (int i = 0; i < sourcePump.ReductionTable.Components[0].Values.Count; i++)
            {
                Assert.AreEqual(sourcePump.ReductionTable.Components[0].Values[i], targetPump.ReductionTable.Components[0].Values[i]);
            }
            Assert.AreNotEqual(sourcePump.Name, targetPump.Name);
            
        }
    }
}