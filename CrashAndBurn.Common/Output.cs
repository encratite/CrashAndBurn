using System;

namespace CrashAndBurn.Common
{
    public static class Output
    {
        public static void WriteLine(string text, ConsoleColor? color = null)
        {
            WithColor(color, () =>
            {
                Console.WriteLine(text);
            });
        }

        public static void Write(string text, ConsoleColor? color = null)
        {
            WithColor(color, () =>
            {
                Console.Write(text);
            });
        }

        private static void WithColor(ConsoleColor? color, Action action)
        {
            if (color.HasValue)
            {
                var originalForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                try
                {
                    action();
                }
                finally
                {
                    Console.ForegroundColor = originalForegroundColor;
                }
            }
            else
            {
                action();
            }
        }
    }
}
