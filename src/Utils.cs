using BMG.State;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BMG
{
    class Utils
    {
        public static string StringVariables(string original, Dictionary<string, object> replacements)
        {
            return Regex.Replace(
                original,
                @"(?<!\{)\{([^}]+)\}(?!\})",
                match => replacements.TryGetValue(match.Groups[1].Value, out var value) ? value.ToString() : "???");
        }


        public static Vector2 ParsePosition(string position)
        {
            // SETUP

            Rectangle size = AMGState.map.size;


            // SPLIT KEYWORDS

            string xsLoc = position.Split(',')[0].Trim().ToLower();
            string ysLoc = position.Split(',')[1].Trim().ToLower();


            // PARSE

            if (!int.TryParse(xsLoc, out int x))
            {
                if (xsLoc == "left" || xsLoc == "l") 
                    x = 0;

                else if (xsLoc == "mid" || xsLoc == "m")
                    x = (size.width - 1) / 2;

                else if (xsLoc == "right" || xsLoc == "r") 
                    x = size.width - 1;
            }

            if (!int.TryParse(ysLoc, out int y))
            {
                if (ysLoc == "top" || ysLoc == "t")
                    y = 0;

                else if (ysLoc == "mid" || ysLoc == "m") 
                    y = (size.height - 1) / 2;

                else if (ysLoc == "bottom" || ysLoc == "bot" || ysLoc == "b")
                    y = size.height - 1;
            }


            // NEGATIVE OFFSET

            if (x < 0)
                x = size.width - (1 - x);

            if (y < 0)
                y = size.height - (1 - y);


            return new Vector2(x, y);
        }


        public static string ReadFileString(string file)
        {
            using (StreamReader reader = new StreamReader(file))
                return reader.ReadToEnd();
        }
    }
}
