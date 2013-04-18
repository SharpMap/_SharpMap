using NUnit.Framework;
using Rhino.Mocks;

namespace DelftTools.Units.Tests
{
    [TestFixture]
    public class UnitTest
    {
        private readonly MockRepository mocks = new MockRepository();

        [Test]
        public void CloneUnitTest()
        {
            IDimension dimension = mocks.Stub<IDimension>();
            var unit = new Unit("test", "symbol", dimension);
            IUnit clonedUnit = (IUnit) unit.Clone();

            mocks.ReplayAll();

            Assert.AreEqual(unit.Name, clonedUnit.Name);
            Assert.AreEqual(unit.Symbol, clonedUnit.Symbol);

            mocks.VerifyAll();
        }
    }
}
