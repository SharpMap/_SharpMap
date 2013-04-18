using System;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using ValidationAspects;
using ValidationAspects.PostSharp;

namespace NetTopologySuite.Extensions.Coverages
{
    public class DiscreteGridPointCoverageBuilder : IFunctionBuilder
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DiscreteGridPointCoverageBuilder));

        private IVariable Index1Variable { get; set; }
        private IVariable Index2Variable { get; set; }
        private IVariable<double> XVariable { get; set; }
        private IVariable<double> YVariable { get; set; }
        private IVariable ValuesVariable { get; set; }
        private IVariable TimeVariable { get; set; }

        public string Index1VariableName { get; set; }
        public string Index2VariableName { get; set; }
        public string XVariableName { get; set; }
        public string YVariableName { get; set; }
        public string ValuesVariableName { get; set; }
        public string TimeVariableName { get; set; }

        public bool CanBuildFunction(IEnumerable<IVariable> variables)
        {
            try
            {
                InitializeVariables(variables);
            } 
            catch
            {
                return false;
            }

            return true;
        }

        public IFunction CreateFunction(IEnumerable<IVariable> variables)
        {
            InitializeVariables(variables);

            var coverage = new DiscreteGridPointCoverage
                               {
                                   Arguments = new EventedList<IVariable>(ValuesVariable.Arguments),
                                   Components = new EventedList<IVariable>(ValuesVariable.Components),
                                   X = XVariable,
                                   Y = YVariable,
                                   Index1 = Index1Variable,
                                   Index2 = Index2Variable
                               };

            return coverage;
        }

        private void InitializeVariables(IEnumerable<IVariable> variables)
        {
            ValidateInputProperties();

            Index1Variable = variables.OfType<IVariable>().First(v => v.Name == Index1VariableName);
            Index2Variable = variables.OfType<IVariable>().First(v => v.Name == Index2VariableName);
            XVariable = variables.OfType<IVariable<double>>().First(v => v.Name == XVariableName);
            YVariable = variables.OfType<IVariable<double>>().First(v => v.Name == YVariableName);
            ValuesVariable = variables.First(v => v.Name == ValuesVariableName);

            if(!string.IsNullOrEmpty(TimeVariableName))
            {
                TimeVariable = variables.First(v => v.Name == TimeVariableName);
            }

/*
            if(!XVariable.Arguments.SequenceEqual(YVariable.Arguments))
            {
                throw new NotSupportedException("Arguments used in X variable must be the same as in Y variable");
            }

            if (!XVariable.Arguments.SequenceEqual(ValuesVariable.Arguments))
            {
                throw new NotSupportedException("Arguments used in Values variable must be the same as in X and Y variables");
            }
*/
        }

        private void ValidateInputProperties()
        {
            if (string.IsNullOrEmpty(Index1VariableName))
            {
                throw new InvalidOperationException("Index1VariableName is undefined");
            }

            if (string.IsNullOrEmpty(Index2VariableName))
            {
                throw new InvalidOperationException("Index2VariableName is undefined");
            }

            if (string.IsNullOrEmpty(XVariableName))
            {
                throw new InvalidOperationException("XVariableName is undefined");
            }

            if (string.IsNullOrEmpty(YVariableName))
            {
                throw new InvalidOperationException("YVariableName is undefined");
            }

            if (string.IsNullOrEmpty(ValuesVariableName))
            {
                throw new InvalidOperationException("ValuesVariableName is undefined");
            }
        }
    }
}