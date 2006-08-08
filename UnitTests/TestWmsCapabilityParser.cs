using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;

namespace UnitTests
{
	[TestFixture]
	public class TestWmsCapabilityParser
	{
		[Test]
		public void Test()
		{
			SharpMap.Web.Wms.Client c = new SharpMap.Web.Wms.Client("http://wms.iter.dk/example_capabilities_1_3_0.xml");
			Assert.AreEqual(3, c.ServiceDescription.Keywords.Length);
		}
	}
}
