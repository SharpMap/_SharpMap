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
using System.Drawing;
using System.Drawing.Drawing2D;
using DelftTools.Utils.Aop.NotifyPropertyChanged;
using DelftTools.Utils.Drawing;
using GeoAPI.Geometries;
using SharpMap.Styles.Shapes;

namespace SharpMap.Styles
{
	/// <summary>
	/// Defines a style used for rendering vector data
	/// </summary>
	[NotifyPropertyChanged(EnableLogging = false)]
	public class VectorStyle : Style, IDisposable
	{
		#region Privates
		private Pen _LineStyle;
		private Pen _OutlineStyle;
		private bool _EnableOutline;
		private Brush _FillStyle;
		private Bitmap _Symbol;
        private Bitmap _LegendSymbol;
	    private Type _geometryType;
        private int _ShapeSize = 18;
	    private bool _CustomSymbol;
		#endregion

		/// <summary>
		/// Initializes a new VectorStyle and sets the default values
		/// </summary>
		/// <remarks>
		/// Default style values when initialized:<br/>
		/// *LineStyle: 1px solid black<br/>
		/// *FillStyle: Solid black<br/>
		/// *Outline: No Outline
		/// *Symbol: null-reference
		/// </remarks>
		public VectorStyle() : 
            this(
                new SolidBrush(Color.AntiqueWhite), 
                            (Pen) Pens.Black.Clone(), 
                            true, 
                            (Pen) Pens.BlueViolet.Clone(), 
                            1f, 
                            typeof (ILineString),
                            ShapeType.Diamond,
                            18)
            {
		}

        /// <summary>
        /// Non default constructor to enable fast creation of correct vectorStyle without first generating invalid
        /// default style and symbol.
        /// </summary>
        /// <param name="fillStyle"></param>
        /// <param name="outLineStyle"></param>
        /// <param name="enableOutline"></param>
        /// <param name="lineStyle"></param>
        /// <param name="symbolScale"></param>
        /// <param name="geometryType"></param>
        /// <param name="shapeType"></param>
        /// <param name="shapeSize"></param>
        public VectorStyle(Brush fillStyle, Pen outLineStyle, bool enableOutline, Pen lineStyle, float symbolScale,
            Type geometryType, ShapeType shapeType, int shapeSize)
        {
            _FillStyle = fillStyle;
            _OutlineStyle = outLineStyle;
            _EnableOutline = enableOutline;
            _LineStyle = lineStyle;
		    _SymbolScale = symbolScale;
    	    _geometryType = geometryType;
            _ShapeType = shapeType;
            _ShapeSize = shapeSize;
            UpdateSymbols();
        }

	    #region Properties

		/// <summary>
		/// Linestyle for line geometries
		/// </summary>
		public virtual Pen Line
		{
			get { return _LineStyle; }
            set 
            { 
                _LineStyle = value;
                
                if (! _CustomSymbol)
                    UpdateSymbols(); 
            }
		}

		/// <summary>
		/// Outline style for line and polygon geometries
		/// </summary>
		public virtual Pen Outline
		{
			get { return _OutlineStyle; }
			set 
            { 
                _OutlineStyle = value;

                if (!_CustomSymbol)
                    UpdateSymbols();
            }
		}

		/// <summary>
		/// Specified whether the objects are rendered with or without outlining
		/// </summary>
		public virtual bool EnableOutline
		{
			get { return _EnableOutline; }
			set
			{
			    _EnableOutline = value;

                if (!_CustomSymbol) 
                    UpdateSymbols();
			}
		}

		/// <summary>
		/// Fillstyle for Polygon geometries
		/// </summary>
        public virtual Brush Fill
        {
            get { return _FillStyle; }
            set
            {
                _FillStyle = value;

                if (!_CustomSymbol) 
                    UpdateSymbols();
            }
        }


        public virtual Bitmap LegendSymbol
        {
            get     
            {
                return _LegendSymbol;
            }
        }

        public virtual Type GeometryType
        {
            get
            {
                return _geometryType;
            }
            set
            {
                _geometryType = value;

                if (!_CustomSymbol)
                    UpdateSymbols();
            }
        }


        /// <summary>
		/// Symbol used for rendering points
		/// </summary>
		public virtual Bitmap Symbol
		{
			get { return _Symbol; }
			set
			{
			    _Symbol = value;
			    _CustomSymbol = true;

                if (value != null)
                {
                    //set the legendSymbol with the custom image
                    Bitmap legendSymbolBitmap = new Bitmap(16, 16);
                    Graphics g = Graphics.FromImage(legendSymbolBitmap);
                    g.Clear(Color.Transparent);

                    g.CompositingMode = CompositingMode.SourceOver;
                    g.DrawImage(Symbol, 0, 0, legendSymbolBitmap.Width, legendSymbolBitmap.Height);
                    _LegendSymbol = legendSymbolBitmap;
                }
			}
		}
		private float _SymbolScale;

		/// <summary>
		/// Scale of the symbol (defaults to 1)
		/// </summary>
		/// <remarks>
		/// Setting the symbolscale to '2.0' doubles the size of the symbol, where a scale of 0.5 makes the scale half the size of the original image
		/// </remarks>
		public virtual float SymbolScale
		{
			get { return _SymbolScale; }
			set { _SymbolScale = value; }
		}

		private PointF _SymbolOffset;

		/// <summary>
		/// Gets or sets the offset in pixels of the symbol.
		/// </summary>
		/// <remarks>
		/// The symbol offset is scaled with the <see cref="SymbolScale"/> property and refers to the offset af <see cref="SymbolScale"/>=1.0.
		/// </remarks>
		public virtual PointF SymbolOffset
		{
			get { return _SymbolOffset; }
			set { _SymbolOffset = value; }
		}

		private float _SymbolRotation;

        ///<summary>
        /// Defines the shapesize for symbol
        ///</summary>
        public virtual int ShapeSize
        {
            get { return _ShapeSize; }
            set
            {
                _ShapeSize = value;

                if (!_CustomSymbol)
                    UpdateSymbols();
            }
        }

        private ShapeType _ShapeType = ShapeType.Diamond; // default

	    ///<summary>
	    /// Defines shape for symbol
	    ///</summary>
	    public ShapeType Shape
	    {
	        get { return _ShapeType; }
	        set
	        {
	            _ShapeType = value;
                UpdateSymbols();
	        }
	    }

	    public static IShapeFactory ShapeFactory = new ShapeFactory();

	    /// <summary>
		/// Gets or sets the rotation of the symbol in degrees (clockwise is positive)
		/// </summary>
		public virtual float SymbolRotation
		{
			get { return _SymbolRotation; }
			set { _SymbolRotation = value; }
		}

		#endregion
        
        /// <summary>
        /// This function updates the _Symbol property with a bitmap generated by using the shape type, size, fillcolor, bordercolor etc.
        /// </summary>
        private void UpdateSymbols()
        {
            // TODO: remove dependency from Swf.Controls
            var shape = ShapeFactory.CreateShape();
            shape.Width = _ShapeSize;
            shape.Height = _ShapeSize;
            shape.ColorFillSolid = ((SolidBrush)_FillStyle).Color;
            shape.BorderWidth = _OutlineStyle.Width;
            shape.BorderColor = _OutlineStyle.Color;
            shape.ShapeType = Shape;
            
            Bitmap bitmap = new Bitmap(_ShapeSize, _ShapeSize);
            Graphics g = Graphics.FromImage(bitmap);
            
            g.Clear(Color.Transparent);
            shape.Paint(g);
            _Symbol = bitmap;

            //update LegendSymbol
            Bitmap legendSymbolBitmap = new Bitmap(16, 16);
            g = Graphics.FromImage(legendSymbolBitmap);
            g.Clear(Color.Transparent);
            if (GeometryType == typeof(IPoint))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.DrawImage(Symbol, 0, 0, legendSymbolBitmap.Width, legendSymbolBitmap.Height);
            }
            else if ((GeometryType == typeof(IPolygon)) || (GeometryType == typeof(IMultiPolygon)))
            {
                g.FillRectangle(Fill, 2, 3, 12, 10);
                g.DrawRectangle(Outline, 2, 3, 12, 10);
            }
            else if ((GeometryType == typeof(ILineString)) || (GeometryType == typeof(IMultiLineString)))
            {
                g.DrawLine(Outline, 2, 8, 14, 8);
                g.DrawLine(Line, 2, 8, 14, 8);
            }
            else
            {
                g.FillRectangle(Fill, 2, 3, 12, 10);
                g.DrawRectangle(Outline, 2, 3, 12, 10);
            }

            _LegendSymbol = legendSymbolBitmap;
            _CustomSymbol = false;
        }

	    public override object Clone()
	    {
	        VectorStyle vectorStyle = new VectorStyle();

            vectorStyle.Shape = Shape;
            vectorStyle.ShapeSize = ShapeSize;
            if ((_CustomSymbol) && (null != Symbol))
                vectorStyle.Symbol = (Bitmap)Symbol.Clone();

            vectorStyle.Line = Line == null ? null : (Pen) Line.Clone();
            vectorStyle.Outline = Outline == null ? null : (Pen)Outline.Clone();
            vectorStyle.EnableOutline = EnableOutline;
            vectorStyle.Fill = Fill == null ? null : (Brush)Fill.Clone();
            vectorStyle.GeometryType = GeometryType;
        
	        return vectorStyle;
	    }

	    public void Dispose()
	    {
            if (Line != null) Line.Dispose();
            if (Outline != null) Outline.Dispose();
            if (Fill != null) Fill.Dispose();
            if (Symbol != null) Symbol.Dispose();
        }

        /// <summary>
        /// In order to support proper serialization the outside world needs to know if the Symbol was set
        /// by an external source.
        /// </summary>
        /// <returns></returns>
        public bool HasCustomSymbol
	    {
            get { return _CustomSymbol;}
	    }
	}
}
