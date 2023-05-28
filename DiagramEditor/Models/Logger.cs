using System.IO;

namespace DiagramEditor.Models {
    public class Log {
        static readonly string path = "../../../Log.txt";
        static bool first = true;

        static readonly bool use_file = false;

        public delegate void LogHandler(string message);
        public static event LogHandler? NewLine;

        public static void Write(string message, bool without_update = false) {
            if (!without_update) NewLine?.Invoke(message);

            if (use_file) {
                if (first) File.WriteAllText(path, message + "\n");
                else File.AppendAllText(path, message + "\n");
                first = false;
            }
        }
    }
}
