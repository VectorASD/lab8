using Avalonia.Controls;
using Avalonia.Input;
using DiagramEditor.Views;
using ReactiveUI;
using Splat;
using System.Collections.Generic;

namespace DiagramEditor.ViewModels {
    public class Log {
        static readonly List<string> logs = new();
        // static readonly string path = "../../../Log.txt";
        // static bool first = true;

        public static MainWindowViewModel? Mwvm { private get; set; }
        public static void Write(string message, bool without_update = false) {
            if (!without_update) {
                foreach (var mess in message.Split('\n')) logs.Add(mess);
                while (logs.Count > 50) logs.RemoveAt(0);

                if (Mwvm != null) Mwvm.Logg = string.Join('\n', logs);
            }

            // if (first) File.WriteAllText(path, message + "\n");
            // else File.AppendAllText(path, message + "\n");
            // first = false;
        }
    }

    public class MainWindowViewModel: ViewModelBase {
        private string log = "";
        private readonly Canvas canv;

        public string Logg { get => log; set => this.RaiseAndSetIfChanged(ref log, value); }

        public MainWindowViewModel(MainWindow mw) {
            Log.Mwvm = this;
            canv = mw.Find<Canvas>("canvas") ?? new Canvas();
            var panel = (Panel?) canv.Parent;
            if (panel == null) return;

            Log.Write("yeah!");
            panel.PointerPressed += (object? sender, PointerPressedEventArgs e) => {
                Log.Write("PointerPressed: " + (e.Source == null ? "null" : e.Source.GetType().Name) + " pos: " + e.GetCurrentPoint(canv).Position);
                // if (e.Source != null && e.Source is Shape @shape) map.PressShape(@shape, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerMoved += (object? sender, PointerEventArgs e) => {
                Log.Write("PointerMoved: " + (e.Source == null ? "null" : e.Source.GetType().Name) + " pos: " + e.GetCurrentPoint(canv).Position);
                // if (e.Source != null && e.Source is Shape @shape) map.MoveShape(@shape, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerReleased += (object? sender, PointerReleasedEventArgs e) => {
                Log.Write("PointerReleased: " + (e.Source == null ? "null" : e.Source.GetType().Name) + " pos: " + e.GetCurrentPoint(canv).Position);
                /* if (e.Source != null && e.Source is Shape @shape) {
                    var item = map.ReleaseShape(@shape, e.GetCurrentPoint(canv).Position);
                    this.RaiseAndSetIfChanged(ref cur_shape, item, nameof(SelectedShape));
                }*/
            };
        }
    }
}