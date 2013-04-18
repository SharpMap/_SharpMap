using System;
using GeoAPI.Geometries;
using System.Collections.Generic;
using NetTopologySuite.Extensions.Geometries;
using SharpMap.Topology;
using SharpMap.UI.Mapping;

namespace SharpMap.UI.FallOff
{
    public class NoFallOffPolicy : IFallOffPolicy
    {
        public virtual FallOffPolicyRule FallOffPolicy
        {
            get { return FallOffPolicyRule.None; }
        }
        public virtual void Move(IGeometry targetGeometry, IGeometry sourceGeometry, IList<IGeometry> geometries, IList<int> handleIndices, 
                         int mouseIndex, double deltaX, double deltaY)
        {
            if ((targetGeometry != null) && (sourceGeometry != null))
            {
                if (targetGeometry.Coordinates.Length != sourceGeometry.Coordinates.Length)
                {
                    throw new ArgumentException("Source and target geometries should have same number of coordinates.");
                }
            }
            for (int i = 0; i < handleIndices.Count; i++)
            {
                GeometryHelper.MoveCoordinate(targetGeometry, sourceGeometry, handleIndices[i], deltaX, deltaY);
                targetGeometry.GeometryChangedAction();

                if (null != geometries)
                {
                    IGeometry tracker = geometries[handleIndices[i]];
                    GeometryHelper.MoveCoordinate(tracker, 0, deltaX, deltaY);
                    tracker.GeometryChangedAction();
                }
            }
        }

        public virtual void Move(IGeometry geometry, IList<IGeometry> trackers, IList<int> handleIndices, int mouseIndex,
                  double deltaX, double deltaY)
        {
            Move(geometry, geometry, trackers, handleIndices, mouseIndex, deltaX, deltaY);
        }
        public virtual void Move(IGeometry targetGeometry, IGeometry sourceGeometry, int handleIndex, double deltaX, double deltaY)
        {
            Move(targetGeometry, sourceGeometry, null, new List<int>(new int[] { handleIndex }), handleIndex, deltaX, deltaY);
        }
        public virtual void Move(IGeometry geometry, int handleIndex, double deltaX, double deltaY)
        {
            Move(geometry, geometry, null, new List<int>(new int[] { handleIndex }), handleIndex, deltaX, deltaY);
        }

        public virtual void Reset()
        {
        }
    }
}