using System.Runtime.InteropServices;

namespace Funeral_PMT
{
    class HTCC
    {
        // DllImports \\
        const int STD_OUTPUT_HANDLE = -11;
        const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
        // End \\

        // Functions \\
        public static void DefaultConsoleColor()
        {
            Console.Write("\x1b[0m");
        }

        public static void EnableVirtualTerminal()
        {
            var Handle = GetStdHandle(STD_OUTPUT_HANDLE);
            GetConsoleMode(Handle, out uint Mode);
            SetConsoleMode(Handle, Mode | ENABLE_VIRTUAL_TERMINAL_PROCESSING);
        }

        public static void SetConsoleColor(string Foreground = null, string Background = null)
        {
            if (Foreground != null && TryParseHexColor(Foreground, out var FR, out var FG, out var FB))
            {
                Console.Write($"\x1b[38;2;{FR};{FG};{FB}m");
            }
            if (Background != null && TryParseHexColor(Background, out var BR, out var BF, out var BB))
            {
                Console.Write($"\x1b[48;2;{BR};{BF};{BB}m");
            }
        }

        static bool TryParseHexColor(string HexColor, out int R, out int G, out int B)
        {
            R = G = B = 0;

            if (string.IsNullOrWhiteSpace(HexColor))
            {
                return false;
            }
            HexColor = HexColor.TrimStart('#');
            if (HexColor.Length != 6)
            {
                return false;
            }

            try
            {
                R = Convert.ToInt32(HexColor.Substring(0, 2), 16);
                G = Convert.ToInt32(HexColor.Substring(2, 2), 16);
                B = Convert.ToInt32(HexColor.Substring(4, 2), 16);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // End \\
    }
}