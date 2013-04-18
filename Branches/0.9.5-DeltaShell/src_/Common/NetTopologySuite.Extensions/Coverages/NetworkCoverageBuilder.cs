using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Functions;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Networks;
using GisSharpBlog.NetTopologySuite.Geometries;

namespace NetTopologySuite.Extensions.Coverages
{
    public class NetworkCoverageBuilder : IFunctionBuilder
    {
        private readonly INetwork network;

        public NetworkCoverageBuilder(INetwork network)
        {
            this.network = network;
        }

        public bool CanBuildFunction(IEnumerable<IVariable> variables)
        {
            bool b = variables.Any(v => v.Attributes.ContainsKey(FunctionAttributes.FunctionType) &&
                     v.Attributes[FunctionAttributes.FunctionType] == typeof(NetworkCoverage).ToString());
            return b;
        }

        public IFunction CreateFunction(IEnumerable<IVariable> variables)
        {
            IVariable component = variables.First(f => !f.IsIndependent);

            //TODO: set name correctly
            INetworkCoverage networkCoverage = new NetworkCoverage
                                                   {
                                                       Name = "networkcoverage",
                                                       Arguments = new EventedList<IVariable>(component.Arguments),
                                                       Components = new EventedList<IVariable>(new[] {component}),
                                                       Network = network
                                                   };

            //component.Store.Functions.Add(networkCoverage);

            return networkCoverage;
        }
    }
}
