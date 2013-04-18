using System;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
using NetTopologySuite.Extensions.Networks;
using NUnit.Framework;

namespace NetTopologySuite.Extensions.Tests.Coverages
{
    [TestFixture]
    public class NetworkCoverageMathExtensionsTest
    {
        [Test]
        public void AddCoverages()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0),
                                                               new Point(100, 100));
            var location = new NetworkLocation(network.Branches[0], 0);
            var coverageA = new NetworkCoverage { Network = network };
            var coverageB = new NetworkCoverage { Network = network };

            //add a uniform coverage B to a 
            coverageB.DefaultValue = 100.0;

            coverageA[location] = 40.0;
            coverageA.Add(coverageB);

            Assert.AreEqual(140.0,coverageA[location]);

            //define a value for B so it no longer uses default value
            coverageB[location] = -20.0;

            //should substract the -20 now
            coverageA.Add(coverageB);
            Assert.AreEqual(120.0,coverageA[location]);
        }
        
        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AddCoveragesThrowsExceptionIfNetworksDontMatch()
        {
            var coverageA = new NetworkCoverage { Network = new Network() };
            var coverageB = new NetworkCoverage { Network = new Network() };

            coverageA.Add(coverageB);
        }

        [Test]
        public void SubstractCoverages()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0),
                                                               new Point(100, 100));
            var location = new NetworkLocation(network.Branches[0], 0);
            var coverageA = new NetworkCoverage { Network = network };
            var coverageB = new NetworkCoverage { Network = network };

            //add a uniform coverage B to a 
            coverageB.DefaultValue = 100.0;

            coverageA[location] = 40.0;
            coverageA.Substract(coverageB);
            
            Assert.AreEqual(-60.0, coverageA[location]);
        }


        [Test]
        public void SubstractComplexCoverages()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0),
                                                               new Point(100, 100));
            
            var waterLevel = new NetworkCoverage { Network = network };
            var bottomLevel = new NetworkCoverage { Network = network };
            
            var branchA = network.Branches[0];
            var branchB = network.Branches[1];

            var locationA1 = new NetworkLocation(branchA, 0);
            var locationA2 = new NetworkLocation(branchA, branchA.Length/2);
            var locationA3 = new NetworkLocation(branchA, branchA.Length);
            var locationB1 = new NetworkLocation(branchB, 0);
            var locationB2 = new NetworkLocation(branchB, branchB.Length/2);
            var locationB3 = new NetworkLocation(branchB, branchB.Length);

            //add a uniform coverage B to a 
            bottomLevel.DefaultValue = 100.0;
            bottomLevel[locationB1] = 100.0;
            bottomLevel[locationB2] = 120.0;
            bottomLevel[locationB3] = 80.0;

            waterLevel[locationA1] = 140.0;
            waterLevel[locationA2] = 110.0;
            waterLevel[locationA3] = 90.0;
            waterLevel[locationB1] = 120.0;
            waterLevel[locationB2] = 130.0;
            waterLevel[locationB3] = 140.0;
            
            waterLevel.Substract(bottomLevel);

            var waterDepth = waterLevel;

            Assert.AreEqual(40.0, waterDepth[locationA1]);
            Assert.AreEqual(10.0, waterDepth[locationA2]);
            Assert.AreEqual(-10.0, waterDepth[locationA3]);
            Assert.AreEqual(20.0, waterDepth[locationB1]);
            Assert.AreEqual(10.0, waterDepth[locationB2]);
            Assert.AreEqual(60.0, waterDepth[locationB3]);
        }
    }
}