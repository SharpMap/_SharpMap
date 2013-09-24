using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Networks
{
    [TestFixture]
    public class BranchFeatureTest
    {
        [Test]
        public void SnapChainageTest()
        {
            const double epsilon = 1.0e-7;
            Assert.AreEqual(epsilon, BranchFeature.Epsilon, 1.0e-8);

            const double length = 100.0;

            // Values near 0.0
            const double smallCorrectableNegativeOffset = -1.0e-8;
            const double smallNotCorrectableNegativeOffset = -1.0e-6;
            Assert.AreEqual(0.0, BranchFeature.SnapChainage(length, smallCorrectableNegativeOffset));
            Assert.AreEqual(0.0, BranchFeature.SnapChainage(length, smallNotCorrectableNegativeOffset));

            const double smallCorrectablePositiveOffset = 1.0e-8;
            const double smallNotCorrectablePositiveOffset = 1.0e-6;
            Assert.AreEqual(0.0, BranchFeature.SnapChainage(length, smallCorrectablePositiveOffset));
            Assert.AreNotEqual(0.0, BranchFeature.SnapChainage(length, smallNotCorrectablePositiveOffset));

            // Values near 100.0
            const double correctableOffsetSmallerThanLength = 100.0 - 1.0e-8;
            const double notCorrectableOffsetSmallerThanLength = 100.0 - 1.0e-6;
            Assert.AreEqual(100.0, BranchFeature.SnapChainage(length, correctableOffsetSmallerThanLength));
            Assert.AreNotEqual(100.0, BranchFeature.SnapChainage(length, notCorrectableOffsetSmallerThanLength));

            const double correctableOffsetLargerThanLength = 100.0 + 1.0e-8;
            const double notCorrectableOffsetLargerThanLength = 100.0 + 1.0e-6;
            Assert.AreEqual(100.0, BranchFeature.SnapChainage(length, correctableOffsetLargerThanLength));
            Assert.AreEqual(100.0, BranchFeature.SnapChainage(length, notCorrectableOffsetLargerThanLength));

            // other case, should return input offset
            const double offsetValue1 = 50.0 + 1.0e-8;
            const double offsetValue2 = 50.0 + 1.0e-6;
            Assert.AreEqual(offsetValue1, BranchFeature.SnapChainage(length, offsetValue1));
            Assert.AreEqual(offsetValue2, BranchFeature.SnapChainage(length, offsetValue2));
        }
    }
}
