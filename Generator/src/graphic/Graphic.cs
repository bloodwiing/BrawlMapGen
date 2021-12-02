namespace BMG
{
    public class Graphic
    {
        public GraphicLayer[] Layers { get; private set; }

        public int ZIndex { get; private set; }
        public int HIndex { get; private set; }

        public Vector2? Position { get; private set; } = null;
    }

    public class GraphicLayer
    {
        public string Asset { get; private set; }

        public Vector2 Offset { get; private set; }
    }
}
