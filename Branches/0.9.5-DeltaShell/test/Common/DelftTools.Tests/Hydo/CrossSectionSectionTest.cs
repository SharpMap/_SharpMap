using DelftTools.Hydro.CrossSections;
using DelftTools.Utils;
using NUnit.Framework;

namespace DelftTools.Tests.Hydo
{
    [TestFixture]
    public class CrossSectionSectionTest
    {
        [Test]
        [Ignore("WIP")]
        public void UnsubscribeOldSectionType()
        {
            var section = new CrossSectionSection();
            var old = new CrossSectionSectionType { Name = "old" };
            section.SectionType = old;

            var newType = new CrossSectionSectionType { Name = "new" };
            section.SectionType = newType;

            ((INotifyPropertyChange)section).PropertyChanging += (s, e) => Assert.Fail("FAAAAAAAAAAAAAAIL!");
            old.Name = "hoi";
        }
    }
}