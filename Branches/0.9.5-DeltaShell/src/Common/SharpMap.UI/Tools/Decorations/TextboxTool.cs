using System.Drawing;

namespace SharpMap.UI.Tools
{
    public class TextboxTool : LayoutComponentTool
    {
        private static readonly Brush BgBrush = new SolidBrush(Color.FromArgb(64, Color.Blue));

        public override void Render(Graphics graphics, Map mapBox)
        {
            if (string.IsNullOrEmpty(Text))
                return;

            var measure = graphics.MeasureString(Text, SystemFonts.DefaultFont);

            var x = 25f;
            var y = (mapBox.Size.Height - measure.Height) - 20;

            var rectangle = new RectangleF(x, y, measure.Width, measure.Height); //25 from the bottom

            rectangle.Inflate(5, 3);
            
            graphics.FillRectangle(BgBrush, rectangle);
            //graphics.DrawRectangle(Pens.Black, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);

            graphics.DrawString(Text, SystemFonts.DefaultFont, Brushes.Black, x, y);
        }

        public override bool RendersInScreenCoordinates
        {
            get { return true; }
        }

        public override bool AlwaysActive
        {
            get { return true; }
        }

        public string Text { get; set; }
    }
}