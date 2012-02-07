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

using System.Drawing;
using System.Drawing.Drawing2D;
using SharpMap.Rendering.Thematics;

namespace SharpMap.Styles
{
    /// <summary>
    /// Defines a style used for rendering labels
    /// </summary>
    public class LabelStyle : Style
    {
        #region HorizontalAlignmentEnum enum

        /// <summary>
        /// Label text alignment
        /// </summary>
        public enum HorizontalAlignmentEnum : short
        {
            /// <summary>
            /// Left oriented
            /// </summary>
            Left = 0,
            /// <summary>
            /// Right oriented
            /// </summary>
            Right = 2,
            /// <summary>
            /// Centered
            /// </summary>
            Center = 1
        }

        #endregion

        #region VerticalAlignmentEnum enum

        /// <summary>
        /// Label text alignment
        /// </summary>
        public enum VerticalAlignmentEnum : short
        {
            /// <summary>
            /// Left oriented
            /// </summary>
            Bottom = 0,
            /// <summary>
            /// Right oriented
            /// </summary>
            Top = 2,
            /// <summary>
            /// Centered
            /// </summary>
            Middle = 1
        }

        #endregion

        private Brush _BackColor;
        private SizeF _CollisionBuffer;
        private bool _CollisionDetection;

        private Font _Font;

        private Color _ForeColor;
        private Pen _Halo;
        private HorizontalAlignmentEnum _HorisontalAlignment;
        private PointF _Offset;
        private VerticalAlignmentEnum _VerticalAlignment;
        private float _rotation;
        private bool _ignoreLength;

        /// <summary>
        /// Initializes a new LabelStyle
        /// </summary>
        public LabelStyle()
        {
            _Font = new Font("Times New Roman", 12f);
            _Offset = new PointF(0, 0);
            _CollisionDetection = false;
            _CollisionBuffer = new Size(0, 0);
            _ForeColor = Color.Black;
            _HorisontalAlignment = HorizontalAlignmentEnum.Center;
            _VerticalAlignment = VerticalAlignmentEnum.Middle;
        }

        /// <summary>
        /// Function to get a <see cref="StringFormat"/> with <see cref="StringFormat.Alignment"/> and <see cref="StringFormat.LineAlignment"/> properties according to 
        /// <see cref="HorizontalAlignment"/> and <see cref="VerticalAlignment"/>
        /// </summary>
        /// <returns>A <see cref="StringFormat"/></returns>
        internal StringFormat GetStringFormat()
        {
            var r = (StringFormat)StringFormat.GenericTypographic.Clone();
            switch (HorizontalAlignment)
            {
                case HorizontalAlignmentEnum.Center:
                    r.Alignment = StringAlignment.Center;
                    break;
                case HorizontalAlignmentEnum.Left:
                    r.Alignment = StringAlignment.Near;
                    break;
                case HorizontalAlignmentEnum.Right:
                    r.Alignment = StringAlignment.Far;
                    break;
            }
            switch (VerticalAlignment)
            {
                case VerticalAlignmentEnum.Middle:
                    r.LineAlignment = StringAlignment.Center;
                    break;
                case VerticalAlignmentEnum.Top:
                    r.LineAlignment = StringAlignment.Near;
                    break;
                case VerticalAlignmentEnum.Bottom:
                    r.LineAlignment = StringAlignment.Far;
                    break;
            }
            return r;
        }

        /// <summary>
        /// Label Font
        /// </summary>
        public Font Font
        {
            get { return _Font; }
            set { _Font = value; }
        }

        /// <summary>
        /// Font color
        /// </summary>
        public Color ForeColor
        {
            get { return _ForeColor; }
            set { _ForeColor = value; }
        }

        /// <summary>
        /// The background color of the label. Set to transparent brush or null if background isn't needed
        /// </summary>
        public Brush BackColor
        {
            get { return _BackColor; }
            set { _BackColor = value; }
        }

        /// <summary>
        /// Creates a halo around the text
        /// </summary>
        [System.ComponentModel.Category("Uneditable")]
        [System.ComponentModel.Editor()]
        public Pen Halo
        {
            get { return _Halo; }
            set
            {
                _Halo = value;
                if (_Halo != null)
                    _Halo.LineJoin = LineJoin.Round;
            }
        }


        /// <summary>
        /// Specifies relative position of labels with respect to objects label point
        /// </summary>
        [System.ComponentModel.Category("Uneditable")]
        public PointF Offset
        {
            get { return _Offset; }
            set { _Offset = value; }
        }

        /// <summary>
        /// Gets or sets whether Collision Detection is enabled for the labels.
        /// If set to true, label collision will be tested.
        /// </summary>
        /// <remarks>Just setting this property in a <see cref="ITheme.GetStyle"/> method does not lead to the desired result. You must set it to for the whole layer using the default Style.</remarks>
        [System.ComponentModel.Category("Collision Detection")]
        public bool CollisionDetection
        {
            get { return _CollisionDetection; }
            set { _CollisionDetection = value; }
        }

        /// <summary>
        /// Distance around label where collision buffer is active
        /// </summary>
        [System.ComponentModel.Category("Collision Detection")]
        public SizeF CollisionBuffer
        {
            get { return _CollisionBuffer; }
            set { _CollisionBuffer = value; }
        }

        /// <summary>
        /// The horisontal alignment of the text in relation to the labelpoint
        /// </summary>
        [System.ComponentModel.Category("Alignment")]
        public HorizontalAlignmentEnum HorizontalAlignment
        {
            get { return _HorisontalAlignment; }
            set { _HorisontalAlignment = value; }
        }

        /// <summary>
        /// The horizontal alignment of the text in relation to the labelpoint
        /// </summary>
        [System.ComponentModel.Category("Alignment")]
        public VerticalAlignmentEnum VerticalAlignment
        {
            get { return _VerticalAlignment; }
            set { _VerticalAlignment = value; }
        }

        /// <summary>
        /// The Rotation of the text
        /// </summary>
        [System.ComponentModel.Category("Alignment")]
        public float Rotation
        {
            get { return _rotation; }
            set { _rotation = value % 360f; }
        }

        /// <summary>
        /// Gets or sets if length of linestring should be ignored
        /// </summary>
        [System.ComponentModel.Category("Alignment")]
        public bool IgnoreLength
        {
            get { return _ignoreLength; }
            set { _ignoreLength = value; }
        }

    }
}