using System;
using System.Diagnostics;
using System.Linq;

namespace BMG.State
{
    public static class AMGState
    {
        public class DrawerState
        {
            public void ResetCursor()
            {
                cursor.x = 0;
                cursor.y = 0;
            }

            public Vector2 cursor = new Vector2();

            public bool rowDrawn = false;

            public static int _tilesDrawn = 0;
            public int tilesDrawn => _tilesDrawn;
            public void DrawnTile() => _tilesDrawn++;

            public static int _mapsDrawn = 0;
            public int mapsDrawn => _mapsDrawn;
            public void DrawnMap() => _tilesDrawn++;
        }


        public class MapState
        {
            public Rectangle size => new Rectangle(data.Length, data[0].Length);

            public string name;
            public string[] data;

            private static int _index = -1;
            public int index => _index;

            public bool valid = true;
            public bool drawn = false;

            public MapState() { }
            public MapState(string[] data)
            {
                this.data = data;
                _index++;
            }
            public MapState(IMap map)
            {
                name = map.GetName();
                //data = map.Data;

                if (map.IsEmpty)
                {
                    Logger.LogWarning($"DATA is empty!\n  [Object] DATA of MAP {map.GetName()} is empty.", 4);
                    valid = false;
                }

                _index++;
            }
        }


        public class Version
        {
            public int major;
            public int minor;
            public int patch;
            public string access;

            public Version(int major, int minor, int patch, string access)
            {
                this.major = major;
                this.minor = minor;
                this.patch = patch;
                this.access = access;
            }

            public override string ToString()
            {
                return string.Format("v{0}.{1}.{2} {3}", major, minor, patch, access);
            }
        }


        private static Stopwatch _stopwatch = new Stopwatch();

        public static DrawerState drawer = new DrawerState();
        public static MapState map = new MapState();
        public static Version version = new Version(2, 0, 0, "Dev");


        public static void NewMap(IMap map)
        {
            drawer = new DrawerState();
            AMGState.map = new MapState(map);
        }


        public static void ResetState()
        {
            drawer.ResetCursor();
            map.drawn = false;
            drawer.rowDrawn = false;
        }

        public static void ResetRowState()
        {
            drawer.cursor.x = 0;
            drawer.rowDrawn = false;
        }

        public static void MoveCursor()
        {
            drawer.cursor.x++;
            if (drawer.cursor.x >= map.size.width)
            {
                drawer.rowDrawn = true;

                drawer.cursor.x = 0;
                drawer.cursor.y++;

                if (drawer.cursor.y >= map.size.height)
                {
                    map.drawn = true;

                    drawer.cursor.y = 0;
                }
            }
        }

        public static void MoveHorCursor()
        {
            drawer.cursor.x++;
            if (drawer.cursor.x >= map.size.width)
            {
                drawer.rowDrawn = true;

                drawer.cursor.x = 0;
            }
        }

        public static void MoveVerCursor()
        {
            drawer.cursor.y++;
            if (drawer.cursor.y >= map.size.height)
            {
                map.drawn = true;

                drawer.cursor.y = 0;
            }
        }

        public static char ReadAtCursor()
        {
            return map.data[drawer.cursor.y][drawer.cursor.x];
        }


        public static void StartTimer() => _stopwatch.Start();
        public static void StopTimer() => _stopwatch.Stop();

        public static long time => _stopwatch.ElapsedMilliseconds;


        public static float GetNumber(string name)
        {
            switch (name)
            {
                case "MAP->INDEX":
                    return map.index;

                case "MAP->SIZE->WIDTH":
                    return map.size.width;
                case "MAP->SIZE->HEIGHT":
                    return map.size.height;

                case "MAP->SIZE->MIDDLE->X":
                    return map.size.middle.x;
                case "MAP->SIZE->MIDDLE->Y":
                    return map.size.middle.y;

                case "DRAWER->CURSOR->X":
                    return drawer.cursor.x;
                case "DRAWER->CURSOR->Y":
                    return drawer.cursor.y;

                default:
                    throw new ApplicationException("Number of key '" + name + "' does not exist");
            }
        }
    }
}
