using System.Collections.Generic;
using GeoAPI.Geometries;
using NetTopologySuite.Extensions.Geometries;
using SharpMap.Api;
using SharpMap.Api.Editors;
using SharpMap.Editors.FallOff;

namespace SharpMap.Editors.Interactors.Network
{
    public static class BranchToBranchFeatureService
    {
        public static IList<double> UpdateNewFractions(ILineString oldLineString, ILineString newLineString, IList<double> fractions, IList<int> handles, IFallOffPolicy fallOffPolicy)
        {
            if (fallOffPolicy.FallOffPolicy != FallOffType.None || handles.Count == 0)
            {
                // no handles is editing the entire linestring; fractions do not need updating.
                return fractions;
            }

            var handle = handles[0];
            var handleMinus1 = (handle > 0) ? handle - 1 : handle;
            var handlePlus1 = (handle < newLineString.Coordinates.Length - 1) ? handle + 1 : handle;

            var fraction = GeometryHelper.LineStringGetFraction(oldLineString, handle);
            var prevFraction = GeometryHelper.LineStringGetFraction(oldLineString, handleMinus1);
            var nextFraction = GeometryHelper.LineStringGetFraction(oldLineString, handlePlus1);

            var oldLength1 = GeometryHelper.LineStringGetDistance(oldLineString, handle) -
                                GeometryHelper.LineStringGetDistance(oldLineString, handleMinus1);

            var newLength1 = GeometryHelper.LineStringGetDistance(newLineString, handle) -
                                GeometryHelper.LineStringGetDistance(newLineString, handleMinus1);

            var forOffset = GeometryHelper.LineStringGetDistance(oldLineString, handleMinus1);

            var oldLength2 = GeometryHelper.LineStringGetDistance(oldLineString, handlePlus1) -
                                GeometryHelper.LineStringGetDistance(oldLineString, handle);

            var newLength2 = GeometryHelper.LineStringGetDistance(newLineString, handlePlus1) -
                                GeometryHelper.LineStringGetDistance(newLineString, handle);

            var afterFactor = newLength2 / oldLength2;
            var afterOffset = GeometryHelper.LineStringGetDistance(newLineString, handle);

            if (handles.Count != 1)
            {
                return fractions;
            }

            var newFractions = new List<double>();
            var forFactor = newLength1 / oldLength1;

            // during update features along the branch are updated following 5 different policies
            for (int i = 0; i < fractions.Count; i++)
            {
                if (fractions[i] >= 1.0)
                {
                    // 0: features at offset1 will remain at offset 1 (idem for 0)
                    newFractions.Add(1.0);
                }
                else if (fractions[i] <= prevFraction)
                {
                    // 1: features before moving segment. These features will retain their distance to 
                    //    the start of old branch.
                    newFractions.Add(GeometryHelper.LineStringGetFraction(newLineString,
                                                                          GeometryHelper.LineStringGetDistance(oldLineString, fractions[i])));
                }
                else if (fractions[i] < fraction)
                {
                    // 2: features at the moving segment but before the 'active' curvepoint will retain
                    //    their relative position in the first part of the segment
                    double oldDistance = GeometryHelper.LineStringGetDistance(oldLineString, fractions[i]);
                    double oldDelta = oldDistance - forOffset;
                    double newDelta = oldDelta * forFactor;
                    newFractions.Add(GeometryHelper.LineStringGetFraction(newLineString, forOffset + newDelta));
                }
                else if (fractions[i] < nextFraction)
                {
                    // 3: features at the moving segment but after the 'active' curvepoint will retain
                    //    their relative position in the second part of the segment
                    double oldDistance = GeometryHelper.LineStringGetDistance(oldLineString, fractions[i]);
                    double oldDelta = oldDistance - GeometryHelper.LineStringGetDistance(oldLineString, handle);
                    double newDelta = oldDelta * afterFactor;
                    newFractions.Add(GeometryHelper.LineStringGetFraction(newLineString, afterOffset + newDelta));
                }
                else // if (fractions[i] >= nextFraction)
                {
                    // 4: features after moving segment. These features will retain their distance to 
                    //    the end of the old branch.
                    double oldDistance = GeometryHelper.LineStringGetDistance(oldLineString, fractions[i]);
                    newFractions.Add(GeometryHelper.LineStringGetFraction(newLineString,
                                                                          newLineString.Length - (oldLineString.Length - oldDistance)));
                }
            }
            // else multiple handles can be selected by the user (CTRL key) or during a move linestring operation
            // newfractions count should equal fractions count
            return newFractions;
        }
    }
}