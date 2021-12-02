using Idle.Serialization;
using System;
using System.Text.RegularExpressions;

namespace BMG.Preset.New
{
    [Serializable]
    public class IdlePoint
    {
        [IdleProperty("X")]
        private object _x { get => null; set => X = new Component(ComponentType.X, value); }
        public Component X { get; private set; }

        [IdleProperty("Y")]
        private object _y { get => null; set => Y = new Component(ComponentType.Y, value); }
        public Component Y { get; private set; }

        public Vector2 ToVector(Renderer renderer)
        {
            return new Vector2(
                X.Solve(renderer),
                Y.Solve(renderer));
        }
    }

    public class Component
    {
        public ComponentType ComponentType { get; private set; }

        public int Offset { get; private set; }

        public int Multiplier { get; private set; }
        public SolveType SolveType { get; private set; }

        public Component(ComponentType componentType, object input)
        {
            ComponentType = componentType;

            switch (input)
            {
                case int i:
                    SolveType = SolveType.NONE;
                    Multiplier = 0;
                    Offset = i;
                    break;

                case string s:
                    ReadString(s);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void ReadString(string input)
        {
            var match = Regex.Match(input, @"(\d*)?([A-Z]+)([+-]\d+)?", RegexOptions.IgnoreCase);

            // Fallback number
            if (!match.Success)
            {
                if (!int.TryParse(input, out int offset))
                    throw new Exception($"XY Point failed to be interpreted: '{input}'");
                Offset = offset;
            }

            var groups = match.Groups;

            if (groups[1].Value != "")
                Multiplier = int.Parse(groups[1].Value);
            else
                Multiplier = 1;

            if (groups[3].Value != "")
                Offset = int.Parse(groups[3].Value);

            switch (groups[2].Value.ToLower()[0])
            {
                case 'm':
                    SolveType = SolveType.MIDDLE_OF_MAP;
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        public int Solve(Renderer renderer)
        {
            int value;

            switch (SolveType)
            {
                case SolveType.MIDDLE_OF_MAP:

                    if (ComponentType == ComponentType.X)
                        value = renderer.map.Size.width;
                    else
                        value = renderer.map.Size.height;

                    break;

                default:
                    value = 0;
                    break;
            }

            return value * Multiplier + Offset;
        }
    }

    public enum SolveType
    {
        NONE,
        MIDDLE_OF_MAP
    }

    public enum ComponentType
    {
        X, Y
    }
}
