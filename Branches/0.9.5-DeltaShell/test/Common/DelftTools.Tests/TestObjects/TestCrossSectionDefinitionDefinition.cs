using System;
using System.Collections.Generic;
using System.Data;
using DelftTools.Hydro.CrossSections;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;

namespace DelftTools.Tests.Hydo
{
    /// <summary>
    /// Provides a dummy subclass of crossection
    /// </summary>
    public class TestCrossSectionDefinition:CrossSectionDefinition
    {
        public TestCrossSectionDefinition()
        {
            
        }
        public TestCrossSectionDefinition(string name, double offset) : base(name)
        {
        }
        
        public override bool GeometryBased
        {
            get { throw new NotImplementedException(); }
        }

        public override IEnumerable<ICoordinate> FlowProfile
        {
            get { throw new NotImplementedException(); }
        }

        
        public override CrossSectionType CrossSectionType
        {
            get { throw new NotImplementedException(); }
        }

        public override DataTable RawData
        {
            get { throw new NotImplementedException(); }
        }

        public override IEnumerable<ICoordinate> Profile
        {
            get { throw new NotImplementedException(); }
        }

        public override void ShiftLevel(double delta)
        {
            throw new NotImplementedException();
        }

        public override IGeometry GetGeometry(IBranch branch, double mapChainage)
        {
            throw new NotImplementedException();
        }

        public override int GetRawDataTableIndex(int profileIndex)
        {
            throw new NotImplementedException();
        }
        
    }
}