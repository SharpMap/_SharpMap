using System;
using System.Drawing;
using SharpMap.Styles;

namespace SharpMap.Rendering.Thematics
{
    public interface IThemeItem : IComparable
    {
        string Range { get; }
        string Label { get; set; }
        Bitmap Symbol { get; set; }
        IStyle Style { get; set; }
    }
}