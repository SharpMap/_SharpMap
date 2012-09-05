using System.Linq;
using DelftTools.Hydro.CrossSections;
using DelftTools.Hydro.Helpers;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionDefinitionProxyTest
    {
        [Test]
        public void ProxyDoesLevelShiftYZ()
        {
            var innerDefinition = CrossSectionDefinitionYZ.CreateDefault();
            AssertLevelShiftsWork(innerDefinition);
        }

        [Test]
        public void ProxyDoesLevelShiftZW()
        {
            var innerDefinition = CrossSectionDefinitionZW.CreateDefault();
            AssertLevelShiftsWork(innerDefinition);
        }

        [Test]
        public void CloneOfProxyIsProxyToSameInnerDefinition()
        {
            var innerDefinition = CrossSectionDefinitionZW.CreateDefault();
            var proxyDefinition = new CrossSectionDefinitionProxy(innerDefinition);
            var clone = (CrossSectionDefinitionProxy)proxyDefinition.Clone();
            Assert.AreEqual(innerDefinition, clone.InnerDefinition);
        }

        [Test]
        public void CloneIncludesLevelShift()
        {
            var innerDefinition = CrossSectionDefinitionZW.CreateDefault();
            var proxyDefinition = new CrossSectionDefinitionProxy(innerDefinition);
            proxyDefinition.LevelShift = 33.0;
            var clone = (CrossSectionDefinitionProxy)proxyDefinition.Clone();
            Assert.AreEqual(proxyDefinition.LevelShift, clone.LevelShift);
        }

        [Test]
        public void CanHaveSummerDikeIfInnerDefinitionIsZw()
        {
            var innerDefinition = CrossSectionDefinitionZW.CreateDefault();
            var proxyDefinition = new CrossSectionDefinitionProxy(innerDefinition);
            Assert.IsTrue(proxyDefinition.CanHaveSummerDike);
            proxyDefinition.InnerDefinition = CrossSectionDefinitionYZ.CreateDefault(); 
            Assert.IsFalse(proxyDefinition.CanHaveSummerDike);
        }
        [Test]
        public void ChangeOfInnerDefinitionFiresDataChanged()
        {
            //functionality is needed because datachanged is used to invalidate the geometry of the cs.
            int callCount = 0;
            var innerDefinition = CrossSectionDefinitionZW.CreateDefault();
            var proxyDefinition = new CrossSectionDefinitionProxy(innerDefinition);
            proxyDefinition.DataChanged += (s, e) => { callCount++; };
            proxyDefinition.InnerDefinition = CrossSectionDefinitionYZ.CreateDefault();
            Assert.AreEqual(1,callCount);
        }
        private static void AssertLevelShiftsWork(ICrossSectionDefinition innerDefinition)
        {
            Assert.IsTrue(innerDefinition.Profile.Count()>0);

            var proxyDefinition = new CrossSectionDefinitionProxy(innerDefinition);
            CheckWithShift(innerDefinition, proxyDefinition, 3.3);
            CheckWithShift(innerDefinition, proxyDefinition, 1.1);
        }

        private static void CheckWithShift(ICrossSectionDefinition innerDefinition, CrossSectionDefinitionProxy proxyDefinition, double levelShift)
        {
            proxyDefinition.LevelShift = levelShift;
            Assert.AreEqual(innerDefinition.Profile.Select(c => new Coordinate(c.X, c.Y + levelShift)).ToList(),proxyDefinition.Profile.ToList());
            Assert.AreEqual(innerDefinition.FlowProfile.Select(c => new Coordinate(c.X, c.Y + levelShift)).ToList(), proxyDefinition.FlowProfile.ToList());
        }
    }
}