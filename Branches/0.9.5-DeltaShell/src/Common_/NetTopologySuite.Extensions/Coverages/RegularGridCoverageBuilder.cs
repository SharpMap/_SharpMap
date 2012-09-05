using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DelftTools.Functions;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Collections.Generic;
using GeoAPI.Extensions.Coverages;

namespace NetTopologySuite.Extensions.Coverages
{
    public class RegularGridCoverageBuilder:IFunctionBuilder
    {
        /// <summary>
        /// Checks if regular coverage can be created using given variables.
        /// 
        /// Regular grid coverage can be created when there is a variable available containing "coordinates" attribute
        /// and that variable at least has 2 arguments with "projection_x_coordinate" and "projection_x_coordinate" as their standard name
        /// </summary>
        /// <param name="variables"></param>
        /// <returns></returns>
        public bool CanBuildFunction(IEnumerable<IVariable> variables)
        {
            var gridComponent = variables.FirstOrDefault(f => f.Attributes.ContainsKey("coordinates"));

            IVariable x = null, y = null;
            if (gridComponent != null)
            {
                foreach (var argument in gridComponent.Arguments)
                {
                    if (!argument.Attributes.ContainsKey(FunctionAttributes.StandardName))
                    {
                        continue;
                    }

                    if (x == null && argument.Attributes[FunctionAttributes.StandardName].Equals("projection_x_coordinate"))
                    {
                        x = argument;
                    }
                    if (y == null && argument.Attributes[FunctionAttributes.StandardName].Equals("projection_y_coordinate"))
                    {
                        y = argument;
                    }
                }
            }

            return gridComponent != null && x != null && y != null;
        }

        public IFunction CreateFunction(IEnumerable<IVariable> variables)
        {
            IVariable gridComponent = variables.First(f => f.Attributes.ContainsKey("coordinates"));
            
            IVariable<double> x = null;
            IVariable<double> y = null;
            IVariable<DateTime> time = null;

            foreach (IVariable argument in gridComponent.Arguments)
            {
                if (!argument.Attributes.ContainsKey(FunctionAttributes.StandardName))
                {
                    continue;
                }

                if (argument.Attributes[FunctionAttributes.StandardName].Equals("projection_x_coordinate"))
                {
                    x = (IVariable<double>)argument;
                }
                if (argument.Attributes[FunctionAttributes.StandardName].Equals("projection_y_coordinate"))
                {
                    y = (IVariable<double>)argument;
                }

                if (argument.Attributes[FunctionAttributes.StandardName].Equals("time"))
                {
                    time = (IVariable<DateTime>)argument;
                }
            }

            var gridName = gridComponent.Name;

            if(gridComponent.Attributes.ContainsKey("function_name"))
            {
                gridName = gridComponent.Attributes[FunctionAttributes.FunctionName];
            }

            // create a new grid coverage and replace components / arguments
            IRegularGridCoverage gridCoverage = new RegularGridCoverage { Name = gridName };

            if (time != null)
            {
                gridCoverage.Arguments = new EventedList<IVariable>(new IVariable[] { time,  y, x });
            }
            else
            {
                gridCoverage.Arguments = new EventedList<IVariable>(new IVariable[] { y, x });
            }
            gridCoverage.Components = new EventedList<IVariable>(new[] { gridComponent });
            
            return gridCoverage;
        }
    }
}
