using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    }
}
