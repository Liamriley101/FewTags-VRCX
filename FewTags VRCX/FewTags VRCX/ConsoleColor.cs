using System.Text.RegularExpressions;

namespace FewTags
{
    internal class ColorConsole
    {
        static void Background(string HexColor)
        {
            ConsoleColor BackgroundColor = HexToConsoleColor(HexColor);
            Console.BackgroundColor = BackgroundColor;
            if (BackgroundColor == ConsoleColor.Black)
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Black;
            }
        }

        static ConsoleColor HexToConsoleColor(string Hex)
        {
            double MinimumDistance = double.MaxValue;
            ConsoleColor ClosestColor = ConsoleColor.Black;
            System.Drawing.Color HexColor = System.Drawing.ColorTranslator.FromHtml(Hex);
            foreach (ConsoleColor EnumColor in Enum.GetValues(typeof(ConsoleColor)))
            {
                System.Drawing.Color ConsoleColor = System.Drawing.Color.FromName(EnumColor.ToString());
                double Distance = Math.Sqrt(Math.Pow(HexColor.R - ConsoleColor.R, 2) + Math.Pow(HexColor.G - ConsoleColor.G, 2) + Math.Pow(HexColor.B - ConsoleColor.B, 2));
                if (Distance < MinimumDistance)
                {
                    MinimumDistance = Distance;
                    ClosestColor = EnumColor;
                }
            }
            return ClosestColor;
        }

        public static void Print(string Tag)
        {
            List<string> HexColors = new List<string>();
            MatchCollection ColorMatches = Regex.Matches(Tag, @"<color=#[0-9A-Fa-f]{6}>|</color>");
            foreach (Match Match in ColorMatches)
            {
                if (Match.Value.StartsWith("<color="))
                {
                    string HexColor = Match.Value.Replace("<color=", "").Replace(">", "");
                    HexColors.Add(HexColor);
                }
                else if (!Match.Value.StartsWith("<color="))
                {
                    Console.ResetColor();
                }
            }
            Tag = Regex.Replace(Tag, @"<\/?color=#[0-9A-Fa-f]{6}>", "");
            foreach (string HexColor in HexColors)
            {
                Background(HexColor);
            }
            Console.WriteLine($"[FewTags] {Tag}");
        }
    }
}