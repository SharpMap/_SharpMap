using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Hydro;
using DelftTools.Hydro.Helpers;
using DeltaShell.Plugins.NetworkEditor.Helpers;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;
using ValidationAspects;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class LateralSourceTest
    {
        [Test]
        public void DefaultLateralSource()
        {
            LateralSource lateralSource = new LateralSource();
            Assert.IsTrue(lateralSource.Validate().IsValid);
        }

        [Test]
        public void Clone()
        {
            var lateralSource = new LateralSource() { Name = "Neem" };
            var clone = (LateralSource)lateralSource.Clone();

            Assert.AreEqual(clone.Name, lateralSource.Name);
            Assert.AreEqual(lateralSource.IsDiffuse, clone.IsDiffuse);
        }

        /// <summary>
        /// HACK: MOVE THIS TEST OUT, HYDRO NETWORK EDITOR
        /// </summary>
        [Test]
        public void LateralSourceCheckGeometry()
        {
            var hydroNetwork = HydroNetworkHelper.GetSnakeHydroNetwork(new Point(0, 0), new Point(200, 0), new Point(200, 200));
            var branch1 = hydroNetwork.Branches[0];

            var lateralSource = new LateralSource
            {
                Name = "Source1",
                Offset = 10,
            };
            branch1.BranchFeatures.Add(lateralSource);
            lateralSource.Branch = branch1;
            lateralSource.IsDiffuse = true;
            
            //needed here; this is actually done via LateralSourceProperties
            HydroNetworkEditorHelper.UpdateBranchFeatureGeometry(lateralSource, 40);
            var geometry = lateralSource.Geometry;
            Assert.AreEqual(typeof(LineString),geometry.GetType());
            Assert.AreEqual(40.0, geometry.Length);

            lateralSource.IsDiffuse = false;
            HydroNetworkEditorHelper.UpdateBranchFeatureGeometry(lateralSource, 0);
            Assert.AreEqual(typeof (Point), lateralSource.Geometry.GetType());
        }
    }
}
