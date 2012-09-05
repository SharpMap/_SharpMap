using System;
using NetTopologySuite.Extensions.Networks;

namespace NetTopologySuite.Extensions.Tests.Features
{
    public class SimpleBranchFeature : BranchFeature
    {
        public override object Clone()
        {
            return base.Clone();
        }
    }
}
