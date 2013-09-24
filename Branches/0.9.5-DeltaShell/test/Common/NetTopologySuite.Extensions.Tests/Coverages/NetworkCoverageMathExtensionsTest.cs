using System;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Coverages;
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
        public void SubstractTimeDependentCoverages()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0),
                                                               new Point(100, 100));
            var location = new NetworkLocation(network.Branches[0], 0);
            var coverageA = new NetworkCoverage {Network = network, IsTimeDependent = true};
            var coverageB = new NetworkCoverage {Network = network, IsTimeDependent = true};

            var dates = new[] {new DateTime(2000, 1, 1), new DateTime(2001, 1, 1)};

            //add a uniform coverage B to a 
            coverageB.DefaultValue = 100.0;
            coverageB.Time.SetValues(dates);

            coverageA[dates[0], location] = 40.0;
            coverageA[dates[1], location] = 10.0;
            coverageA.Substract(coverageB);

            Assert.AreEqual(-60.0, coverageA[dates[0], location]);
            Assert.AreEqual(-90.0, coverageA[dates[1], location]);
        }

        [Test]
        public void SubstractComplexCoverages()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0),
                                                               new Point(100, 100));
            
            var waterLevel = new NetworkCoverage { Network = network };
            var bedLevel = new NetworkCoverage { Network = network };
            
            var branchA = network.Branches[0];
            var branchB = network.Branches[1];

            var locationA1 = new NetworkLocation(branchA, 0);
            var locationA2 = new NetworkLocation(branchA, branchA.Length/2);
            var locationA3 = new NetworkLocation(branchA, branchA.Length);
            var locationB1 = new NetworkLocation(branchB, 0);
            var locationB2 = new NetworkLocation(branchB, branchB.Length/2);
            var locationB3 = new NetworkLocation(branchB, branchB.Length);

            //add a uniform coverage B to a 
            bedLevel.DefaultValue = 100.0;
            bedLevel[locationB1] = 100.0;
            bedLevel[locationB2] = 120.0;
            bedLevel[locationB3] = 80.0;

            waterLevel[locationA1] = 140.0;
            waterLevel[locationA2] = 110.0;
            waterLevel[locationA3] = 90.0;
            waterLevel[locationB1] = 120.0;
            waterLevel[locationB2] = 130.0;
            waterLevel[locationB3] = 140.0;
            
            waterLevel.Substract(bedLevel);

            var waterDepth = waterLevel;

            Assert.AreEqual(40.0, waterDepth[locationA1]);
            Assert.AreEqual(10.0, waterDepth[locationA2]);
            Assert.AreEqual(-10.0, waterDepth[locationA3]);
            Assert.AreEqual(20.0, waterDepth[locationB1]);
            Assert.AreEqual(10.0, waterDepth[locationB2]);
            Assert.AreEqual(60.0, waterDepth[locationB3]);
        }

        [Test]
        public void SubstractComplexCoveragesFromClonedNetworks()
        {
            var network = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(200, 0));
            var blNetwork = RouteHelperTest.GetSnakeNetwork(false, new Point(0, 0), new Point(100, 0), new Point(100, 100));

            var waterLevel = new NetworkCoverage { Network = network };
            var bedLevel = new NetworkCoverage { Network = blNetwork };

            var branchA = network.Branches[0];
            var branchB = network.Branches[1];
            var blBranchB = blNetwork.Branches[1];

            var locationA1 = new NetworkLocation(branchA, 0);
            var locationA2 = new NetworkLocation(branchA, branchA.Length / 2);
            var locationA3 = new NetworkLocation(branchA, branchA.Length);
            var locationB1 = new NetworkLocation(branchB, 0);
            var locationB2 = new NetworkLocation(branchB, branchB.Length / 2);
            var locationB3 = new NetworkLocation(branchB, branchB.Length);

            var blLocationB1 = new NetworkLocation(blBranchB, 0);
            var blLocationB2 = new NetworkLocation(blBranchB, blBranchB.Length / 2);
            var blLocationB3 = new NetworkLocation(blBranchB, blBranchB.Length);

            //add a uniform coverage B to a 
            bedLevel.DefaultValue = 100.0;
            bedLevel[blLocationB1] = 100.0;
            bedLevel[blLocationB2] = 120.0;
            bedLevel[blLocationB3] = 80.0;

            waterLevel[locationA1] = 140.0;
            waterLevel[locationA2] = 110.0;
            waterLevel[locationA3] = 90.0;
            waterLevel[locationB1] = 120.0;
            waterLevel[locationB2] = 130.0;
            waterLevel[locationB3] = 140.0;

            waterLevel.Substract(bedLevel);

            var waterDepth = waterLevel;

            Assert.AreEqual(40.0, waterDepth[locationA1]);
            Assert.AreEqual(10.0, waterDepth[locationA2]);
            Assert.AreEqual(-10.0, waterDepth[locationA3]);
            Assert.AreEqual(20.0, waterDepth[locationB1]);
            Assert.AreEqual(130.0, waterDepth[locationB2]);
            Assert.AreEqual(140.0, waterDepth[locationB3]);
        }
    }
}