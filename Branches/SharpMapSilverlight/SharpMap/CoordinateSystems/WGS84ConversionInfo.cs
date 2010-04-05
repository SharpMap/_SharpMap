// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;

namespace SharpMap.CoordinateSystems
{ 
	/// <summary>
	/// Parameters for a geographic transformation into WGS84. The Bursa Wolf parameters should be applied 
	/// to geocentric coordinates, where the X axis points towards the Greenwich Prime Meridian, the Y axis
	/// points East, and the Z axis points North.
	/// </summary>
	/// <remarks>
	/// <para>These parameters can be used to approximate a transformation from the horizontal datum to the
	/// WGS84 datum using a Bursa Wolf transformation. However, it must be remembered that this transformation
	/// is only an approximation. For a given horizontal datum, different Bursa Wolf transformations can be
	/// used to minimize the errors over different regions.</para>
	/// <para>If the DATUM clause contains a TOWGS84 clause, then this should be its “preferred” transformation,
	/// which will often be the transformation which gives a broad approximation over the whole area of interest
	/// (e.g. the area of interest in the containing geographic coordinate system).</para>
	/// <para>Sometimes, only the first three or six parameters are defined. In this case the remaining
	/// parameters must be zero. If only three parameters are defined, then they can still be plugged into the
	/// Bursa Wolf formulas, or you can take a short cut. The Bursa Wolf transformation works on geocentric
	/// coordinates, so you cannot apply it onto geographic coordinates directly. If there are only three
	/// parameters then you can use the Molodenski or abridged Molodenski formulas.</para>
	/// <para>If a datums ToWgs84Parameters parameter values are zero, then the receiving
	/// application can assume that the writing application believed that the datum is approximately equal to
	/// WGS84.</para>
	/// </remarks>
	public class Wgs84ConversionInfo
	{
		/// <summary>
		/// Initializes an instance of Wgs84ConversionInfo with default parameters (all values = 0)
		/// </summary>
		public Wgs84ConversionInfo() : this(0,0,0,0,0,0,0,String.Empty)
		{
		}
		/// <summary>
		/// Initializes an instance of Wgs84ConversionInfo
		/// </summary>
		/// <param name="dx">Bursa Wolf shift in meters.</param>
		/// <param name="dy">Bursa Wolf shift in meters.</param>
		/// <param name="dz">Bursa Wolf shift in meters.</param>
		/// <param name="ex">Bursa Wolf rotation in arc seconds.</param>
		/// <param name="ey">Bursa Wolf rotation in arc seconds.</param>
		/// <param name="ez">Bursa Wolf rotation in arc seconds.</param>
		/// <param name="ppm">Bursa Wolf scaling in parts per million.</param>
		public Wgs84ConversionInfo(double dx, double dy, double dz, double ex, double ey, double ez, double ppm)
			:
			this(dx, dy, dz, ex, ey, ez, ppm, String.Empty)
		{
		}
		/// <summary>
		/// Initializes an instance of Wgs84ConversionInfo
		/// </summary>
		/// <param name="dx">Bursa Wolf shift in meters.</param>
		/// <param name="dy">Bursa Wolf shift in meters.</param>
		/// <param name="dz">Bursa Wolf shift in meters.</param>
		/// <param name="ex">Bursa Wolf rotation in arc seconds.</param>
		/// <param name="ey">Bursa Wolf rotation in arc seconds.</param>
		/// <param name="ez">Bursa Wolf rotation in arc seconds.</param>
		/// <param name="ppm">Bursa Wolf scaling in parts per million.</param>
		/// <param name="areaOfUse">Area of use for this transformation</param>
		public Wgs84ConversionInfo(double dx, double dy, double dz, double ex, double ey, double ez, double ppm, string areaOfUse)
		{
			Dx = dx; Dy = dy; Dz = dz;
			Ex = ex; Ey = ey; Ez = ez;
			Ppm = ppm;
			AreaOfUse = areaOfUse;
		}

		/// <summary>
		/// Bursa Wolf shift in meters.
		/// </summary>
		public double Dx;
		/// <summary>
		/// Bursa Wolf shift in meters.
		/// </summary>
		public double Dy;
		/// <summary>
		/// Bursa Wolf shift in meters.
		/// </summary>
		public double Dz;
		/// <summary>
		/// Bursa Wolf rotation in arc seconds.
		/// </summary>
		public double Ex;
		/// <summary>
		/// Bursa Wolf rotation in arc seconds.
		/// </summary>
		public double Ey;
		/// <summary>
		/// Bursa Wolf rotation in arc seconds.
		/// </summary>
		public double Ez;
		/// <summary>
		/// Bursa Wolf scaling in parts per million.
		/// </summary>
		public double Ppm;
		/// <summary>
		/// Human readable text describing intended region of transformation.
		/// </summary>
		public string AreaOfUse;

		/// <summary>
		/// Affine Bursa-Wolf matrix transformation
		/// </summary>
		/// <remarks>
		/// <para>Transformation of coordinates from one geographic coordinate system into another 
		/// (also colloquially known as a “datum transformation”) is usually carried out as an 
		/// implicit concatenation of three transformations:</para>
		/// <para>[geographical to geocentric >> geocentric to geocentric >> geocentric to geographic</para>
		/// <para>
		/// The middle part of the concatenated transformation, from geocentric to geocentric, is usually 
		/// described as a simplified 7-parameter Helmert transformation, expressed in matrix form with 7 
		/// parameters, in what is known as the "Bursa-Wolf" formula:<br/>
		/// <code>
		///  S = 1 + Ppm/1000000
		///  [ Xt ]    [     S   -Ez*S   +Ey*S   Dx ]  [ Xs ]
		///  [ Yt ]  = [ +Ez*S       S   -Ex*S   Dy ]  [ Ys ]
		///  [ Zt ]    [ -Ey*S   +Ex*S       S   Dz ]  [ Zs ]
		///  [ 1  ]    [     0       0       0    1 ]  [ 1  ]
		/// </code><br/>
		/// The parameters are commonly referred to defining the transformation "from source coordinate system 
		/// to target coordinate system", whereby (XS, YS, ZS) are the coordinates of the point in the source 
		/// geocentric coordinate system and (XT, YT, ZT) are the coordinates of the point in the target 
		/// geocentric coordinate system. But that does not define the parameters uniquely; neither is the
		/// definition of the parameters implied in the formula, as is often believed. However, the 
		/// following definition, which is consistent with the "Position Vector Transformation" convention, 
		/// is common E&amp;P survey practice: 
		/// </para>	
		/// <para>(dX, dY, dZ): Translation vector, to be added to the point's position vector in the source 
		/// coordinate system in order to transform from source system to target system; also: the coordinates 
		/// of the origin of source coordinate system in the target coordinate system </para>
		/// <para>(RX, RY, RZ): Rotations to be applied to the point's vector. The sign convention is such that 
		/// a positive rotation about an axis is defined as a clockwise rotation of the position vector when 
		/// viewed from the origin of the Cartesian coordinate system in the positive direction of that axis;
		/// e.g. a positive rotation about the Z-axis only from source system to target system will result in a
		/// larger longitude value for the point in the target system. Although rotation angles may be quoted in
		/// any angular unit of measure, the formula as given here requires the angles to be provided in radians.</para>
		/// <para>: The scale correction to be made to the position vector in the source coordinate system in order 
		/// to obtain the correct scale in the target coordinate system. M = (1 + dS*10-6), whereby dS is the scale
		/// correction expressed in parts per million.</para>
		/// <para><see href="http://www.posc.org/Epicentre.2_2/DataModel/ExamplesofUsage/eu_cs35.html"/> for an explanation of the Bursa-Wolf transformation</para>
		/// </remarks>
		/// <returns></returns>
		public double[,] GetAffineTransform()
		{
			double S = 1 + Ppm * 0.000001;
			double RS = (Math.PI / (180 * 3600)) * S;
			return new double[4,4] {
				{ RS,		-Ez*RS,		+Ey*RS,	Dx} ,
				{ Ez*RS,	RS,			-Ex*RS,	Dy} ,
				{ -Ey*RS,	Ex*RS,		RS,		Dz} ,
                { 0,		0,			0,		1}
			};

		}
		/// <summary>
		/// Returns the Well Known Text (WKT) for this object.
		/// </summary>
		/// <remarks>The WKT format of this object is: <code>TOWGS84[dx, dy, dz, ex, ey, ez, ppm]</code></remarks>
		/// <returns>WKT representaion</returns>
		public string WKT
		{
			get
			{
				return String.Format("TOWGS84[{0}, {1}, {2}, {3}, {4}, {5}, {6}]", Dx, Dy, Dz, Ex, Ey, Ez, Ppm);
			}
		}

		/// <summary>
		/// Returns the Well Known Text (WKT) for this object.
		/// </summary>
		/// <remarks>The WKT format of this object is: <code>TOWGS84[dx, dy, dz, ex, ey, ez, ppm]</code></remarks>
		/// <returns>WKT representaion</returns>
		public override string ToString()
		{
			return WKT;
		}

	}
}

