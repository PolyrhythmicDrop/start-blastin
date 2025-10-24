using System.Runtime.CompilerServices;
using Godot;

namespace Utility
{
    public class DebugLogger
    {
        public static void LogMessage(
            string message,
            bool trace = false,
            bool error = false,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = ""
        )
        {
            string log = message;

            if (trace)
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                log = $"{fileName}.{memberName}: {message}";
            }
            if (error)
            {
                GD.PrintErr(log);
            }
            else
            {
                GD.Print(log);
            }
        }
    }
}
