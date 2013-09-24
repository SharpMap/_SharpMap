using System;
using DelftTools.Functions;
using DelftTools.Functions.Filters;
using DelftTools.Functions.Generic;
using DelftTools.Utils.Aop;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

using log4net;

namespace NetTopologySuite.Extensions.Coverages
{
    [Entity(FireOnCollectionChange = false)]
    public abstract class Coverage : Function, ICoverage
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Coverage));

        private IFeatureAttributeCollection attributes;
        private IGeometry geometry;
        
        public virtual IGeometry Geometry
        {
            get { return geometry; }
            set { geometry = value; }
        }

        IFeatureAttributeCollection IFeature.Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        protected override bool IsArgumentObligatoryForType(IVariable variable)
        {
            if (variable.ValueType == typeof(DateTime)) //reducing time argument still makes this a coverage
            {
                return false;
            }
            return true; //assume true
        }

        public abstract object Evaluate(ICoordinate coordinate);

        /// <summary>
        /// Evaluates value of coverage at coordinate.
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public abstract T Evaluate<T>(ICoordinate coordinate);

        /// <summary>
        /// Evaluates value of coverage at coordinate.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public abstract T Evaluate<T>(double x, double y);

        public abstract object Evaluate(ICoordinate coordinate, DateTime? time);

        public virtual IVariable<DateTime> Time
        {
            get
            {
                if(Arguments.Count > 0)
                {
                    return (Arguments[0] as IVariable<DateTime>);
                }

                return null;
            }
            set
            {
                if (Time == null)
                {
                    Arguments.Insert(0, value);
                }
            }
        }

        public virtual bool IsTimeDependent
        {
            get { return Time != null; }
            set { OnTimeDependenceChanged(value); }
        }

        [EditAction]
        private void OnTimeDependenceChanged(bool value)
        {
            if (value) //make time dependend
            {
                if (Time == null)
                {
                    var time = new Variable<DateTime>("time") {InterpolationType = InterpolationType.Constant};
                    //time.Attributes["standard_name"] = StandardNameTime;
                    Time = time;
                }
            }
            else //make independend
            {
                if (Time != null)
                {
                    Arguments.Remove(Time);
                }
            }
        }

        public virtual IFunction GetTimeSeries(ICoordinate coordinate)
        {
            throw new NotImplementedException();
        }

        public virtual IFunction GetTimeSeries(IFeature feature)
        {
            if (feature == null)
            {
                throw new NotImplementedException();
            }
            return GetTimeSeries(feature.Geometry.Centroid.Coordinate);
        }

        public override object Clone()
        {
            return Clone(true);
        }

        public override object Clone(bool copyValues)
        {
            var clone = (ICoverage)base.Clone(copyValues);

            if (Geometry != null)
            {
                clone.Geometry = (IGeometry)Geometry.Clone();
            }

            return clone;
        }

        /// <summary>
        /// Returns a time filtered version of the coverage
        /// </summary>
        /// <param name="time"></param>
        public virtual ICoverage FilterTime(DateTime time)
        {
            if (!IsTimeDependent)
                throw new InvalidOperationException("Coverage is not time dependend and cannot be filtered in time");
            return (ICoverage)Filter(new VariableValueFilter<DateTime>(Time, time));
        }
    }
}