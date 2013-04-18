using DelftTools.Utils.Drawing;

namespace SharpMap.Styles.Shapes
{
    public class ShapeFactory : IShapeFactory
    {
        public IShape CreateShape()
        {
            return new Shape();
        }
    }
}