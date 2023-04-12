using Avalonia.Controls;
using Avalonia.Input;
using DiagramEditor.Models;
using DiagramEditor.Views;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;

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
        private AddShape? menu;

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

            Apply = ReactiveCommand.Create<Unit, Unit>(_ => { FuncApply(); return new Unit(); });
            Close = ReactiveCommand.Create<Unit, Unit>(_ => { FuncClose(); return new Unit(); });
            Clear = ReactiveCommand.Create<Unit, Unit>(_ => { FuncClear(); return new Unit(); });

            canv = mw.Find<Canvas>("canvas") ?? new Canvas();
            canv.Children.Add(map.Marker);
            canv.Children.Add(map.Marker2);
            var panel = (Panel?) canv.Parent;
            if (panel == null) return;

            panel.PointerPressed += (object? sender, PointerPressedEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.Press(@control, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerMoved += (object? sender, PointerEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.Move(@control, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerReleased += (object? sender, PointerReleasedEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) {
                    var pos = e.GetCurrentPoint(canv).Position;
                    bool tapped = map.Release(@control, pos);
                    if (tapped) {
                        menu = new AddShape { DataContext = this };
                        menu.ShowDialog(mw);
                    }
                }
            };
        }

        /*
         * Вкладка общих параметров
         */

        string name = "yeah";
        int stereo = 0; // -, static, abstract
        int access = 0; // private, public, protected, package

        public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }

        public bool Stereo_1 { get => stereo == 0; set => stereo = value ? 0 : -1; }
        public bool Stereo_2 { get => stereo == 1; set => stereo = value ? 1 : -1; }
        public bool Stereo_3 { get => stereo == 2; set => stereo = value ? 2 : -1; }

        public bool Access_1 { get => access == 0; set => access = value ? 0 : -1; }
        public bool Access_2 { get => access == 1; set => access = value ? 1 : -1; }
        public bool Access_3 { get => access == 2; set => access = value ? 2 : -1; }
        public bool Access_4 { get => access == 3; set => access = value ? 3 : -1; }

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

        // Ещё кнопочки ;'-}

        private static readonly string[] stereos = new string[] { "static", "abstract" };

        private void FuncApply() {
            StringBuilder sb = new();
            sb.Append($"{"-+#~"[access]} {name}");
            if (stereo != 0) {
                sb.Append(' ');
                sb.Append(stereos[stereo - 1]);
            }
            var head = new MeasuredText[] { new(sb.ToString()) };

            List<MeasuredText> arr = new(), arr2 = new();
            foreach (var attr in attributes) arr.Add(new(attr.ToString()));
            foreach (var meth in methods) arr2.Add(new(meth.ToString()));
            MeasuredText[] attrs = arr.ToArray();
            MeasuredText[] meths = arr2.ToArray();

            var pos = map.tap_pos;
            var item = new DiagramItem(head, attrs, meths) { Margin = new(pos.X - 75, pos.Y - 50, 0, 0) };
            canv.Children.Add(item);
            map.AddItem(item);

            FuncClose();
        }
        private void FuncClose() {
            if (menu == null) return;
            menu.Close();
            menu = null;
        }
        private void FuncClear() {
            Name = "yeah";
            stereo = 0;
            access = 0;
            attributes.Clear();
            methods.Clear();
            this.RaisePropertyChanged(nameof(Stereo_1));
            this.RaisePropertyChanged(nameof(Access_1));
        }

        public ReactiveCommand<Unit, Unit> Apply { get; }
        public ReactiveCommand<Unit, Unit> Close { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }
    }
}