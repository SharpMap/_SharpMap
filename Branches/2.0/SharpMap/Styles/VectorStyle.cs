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
using System.Collections.Generic;
using System.IO;
using System.Text;

using SharpMap.Rendering;

namespace SharpMap.Styles
{
	/// <summary>
	/// Defines a style used for rendering vector data
	/// </summary>
	public class VectorStyle : Style
	{
		#region Private members
        private StylePen _lineStyle;
        private StylePen _highlightLineStyle;
        private StylePen _selectLineStyle;
        private bool _outline;
        private StylePen _outlineStyle;
        private StylePen _highlightOutlineStyle;
        private StylePen _selectOutlineStyle;
        private StyleBrush _fillStyle;
        private StyleBrush _highlightFillStyle;
        private StyleBrush _selectionFillStyle;
        private Symbol2D _symbol;
        private Symbol2D _highlightSymbol;
        private Symbol2D _selectSymbol;
        private StyleRenderingMode _smoothingMode;
        private StyleTextRenderingHint _textRenderingHint;
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
		public VectorStyle()
		{
			this.Outline = new StylePen(StyleColor.Black, 1);
			this.Line = new StylePen(StyleColor.Black, 1);
            this.Fill = new SolidStyleBrush(StyleColor.Black);
			this.EnableOutline = false;
		}

		#region Properties

		/// <summary>
		/// Linestyle for line geometries
		/// </summary>
		public StylePen Line
		{
			get { return _lineStyle; }
			set { _lineStyle = value; }
        }

        /// <summary>
        /// Highlighted line style for line geometries
        /// </summary>
        public StylePen HighlightLine
        {
            get { return _highlightLineStyle; }
            set { _highlightLineStyle = value; }
        }

        /// <summary>
        /// Selected line style for line geometries
        /// </summary>
        public StylePen SelectLine
        {
            get { return _selectLineStyle; }
            set { _selectLineStyle = value; }
        }

        /// <summary>
        /// Specified whether the objects are rendered with or without outlining
        /// </summary>
        public bool EnableOutline
        {
            get { return _outline; }
            set { _outline = value; }
        }

		/// <summary>
		/// Normal outline style for line and polygon geometries
		/// </summary>
		public StylePen Outline
		{
			get { return _outlineStyle; }
			set { _outlineStyle = value; }
        }

        /// <summary>
        /// Highlighted outline style for line and polygon geometries
        /// </summary>
        public StylePen HighlightOutline
        {
            get { return _highlightOutlineStyle; }
            set { _highlightOutlineStyle = value; }
        }

        /// <summary>
        /// Selected outline style for line and polygon geometries.
        /// </summary>
        public StylePen SelectOutline
        {
            get { return _selectOutlineStyle; }
            set { _selectOutlineStyle = value; }
        }

		/// <summary>
		/// Fillstyle for closed geometries.
		/// </summary>
		public StyleBrush Fill
		{
			get { return _fillStyle; }
			set { _fillStyle = value; }
		}

        public StyleBrush SelectFill
        {
            get { return _selectionFillStyle; }
            set { _selectionFillStyle = value; }
        }

        public StyleBrush HighlightFill
        {
            get { return _highlightFillStyle; }
            set { _highlightFillStyle = value; }
        }
		
		/// <summary>
		/// Symbol used for rendering points.
		/// </summary>
		public Symbol2D Symbol
		{
			get { return _symbol; }
			set { _symbol = value; }
        }

        /// <summary>
        /// Symbol used for rendering points.
        /// </summary>
        public Symbol2D HighlightSymbol
        {
            get { return _highlightSymbol; }
            set { _highlightSymbol = value; }
        }

        /// <summary>
        /// Symbol used for rendering points.
        /// </summary>
        public Symbol2D SelectSymbol
        {
            get { return _selectSymbol; }
            set { _selectSymbol = value; }
        }

        /// <summary>
        /// Render whether smoothing (antialiasing) is applied to lines 
        /// and curves and the edges of filled areas
        /// </summary>
        public StyleRenderingMode SmoothingMode
        {
            get { return _smoothingMode; }
            set { _smoothingMode = value; }
        }

        /// <summary>
        /// Specifies the quality of text rendering
        /// </summary>
        public StyleTextRenderingHint TextRenderingHint
        {
            get { return _textRenderingHint; }
            set { _textRenderingHint = value; }
        }
		#endregion
	}
}
