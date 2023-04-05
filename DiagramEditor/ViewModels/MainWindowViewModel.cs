using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using DiagramEditor.Models;
using DiagramEditor.Views;
using ReactiveUI;
using Splat;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;

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
        private readonly Mapper map;

        public string Logg { get => log; set => this.RaiseAndSetIfChanged(ref log, value); }

        /* Не работает подобная тема :/
         * Не получается получить недосформированное окно, что как раз занято созданием этого класса XD
         * Ну или как там дизайнер этот работает...
        
        public static Window? GetCurrentWindow() {
            var cur = Application.Current;
            if (cur == null) return null;

            var app = (IClassicDesktopStyleApplicationLifetime?) cur.ApplicationLifetime;
            if (app == null) return null;

            return app.MainWindow;
        }

        public MainWindowViewModel() : this(GetCurrentWindow()) {} // Для корректной работы предварительного просмотра
        */

        public MainWindowViewModel(Window mw) {
            Log.Mwvm = this;
            map = new Mapper();

            AddFirstAttr = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddFirstAttr(); return new Unit(); });
            AddFirstMethod = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddFirstMethod(); return new Unit(); });

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
                var window = new AddShape() { DataContext = this };
                window.ShowDialog(mw);
            };
        }

        /*
         * Вкладка атрибутов
         */

        readonly ObservableCollection<AttributeItem> attributes = new();
        public ObservableCollection<AttributeItem> Attributes { get => attributes; }

        public ReactiveCommand<Unit, Unit> AddFirstAttr { get; }

        private void FuncAddFirstAttr() => attributes.Insert(0, new AttributeItem(this));
        public void FuncAddNextAttr(AttributeItem item) => attributes.Insert(attributes.IndexOf(item) + 1, new AttributeItem(this));
        public void FuncRemoveAttr(AttributeItem item) => attributes.Remove(item);

        /*
         * Вкладка методов
         */

        readonly ObservableCollection<MethodItem> methods = new();
        public ObservableCollection<MethodItem> Methods { get => methods; }

        public ReactiveCommand<Unit, Unit> AddFirstMethod { get; }

        private void FuncAddFirstMethod() => methods.Insert(0, new MethodItem(this));
        public void FuncAddNextMethod(MethodItem item) => methods.Insert(methods.IndexOf(item) + 1, new MethodItem(this));
        public void FuncRemoveMethod(MethodItem item) => methods.Remove(item);
    }
}