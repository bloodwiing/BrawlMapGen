namespace BMG
{
    public struct Rectangle
    {
        public int height;
        public int width;

        public Vector2 middle;

        public Rectangle(int height, int width)
        {
            this.height = height;
            this.width = width;

            middle = new Vector2(
                width / 2 - (width + 1) % 2,
                height / 2 - (height + 1) % 2
                );
        }
    }
}
