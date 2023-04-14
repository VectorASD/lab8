using Avalonia.Controls;
using Avalonia.Input;
using DiagramEditor.Models;
using DiagramEditor.Views;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        private DiagramItem? editable;

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
            ExportB = ReactiveCommand.Create<string, Unit>(n => { FuncExport(n); return new Unit(); });
            ImportB = ReactiveCommand.Create<string, Unit>(n => { FuncImport(n); return new Unit(); });

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
                    map.Release(@control, pos);
                    if (map.tap_mode == 1) {
                        editable = null;
                        menu = new AddShape { DataContext = this };
                        menu.ShowDialog(mw);
                    }

                    if (map.new_join != null) {
                        var newy = map.new_join.line;
                        canv.Children.Add(newy);
                        map.new_join = null;
                    }

                    if (map.tap_mode == 2 && map.tapped_item != null) {
                        editable = map.tapped_item;
                        Import(editable.entity);
                        menu = new AddShape { DataContext = this, Title = "Редактирование ноды диаграммы" };
                        menu.ShowDialog(mw);
                    }
                }
            };

            panel.PointerWheelChanged += (object? sender, PointerWheelEventArgs e) => {
                if (e.Source != null && e.Source is Control @control) map.WheelMove(@control, e.Delta.Y);
            };

            /* int x = 100, y = 100, w = 500, angle = 90; // Чисто оставил для вида, как прототип первой нормааааЪааЪаЪально вращаемой стрелочки
            Path p = new() {
                StrokeThickness = 3,
                Stroke = Brushes.Sienna,
                Data = Geometry.Parse($"M {x},{y} l {w},0 m -20,-20 l 20,20 m -20,20 l 20,-20"),
                RenderTransform = new RotateTransform(angle, (x - w) / 2, (y - 20) / 2)
            };
            canv.Children.Add(p); */

            panel.AddHandler(DragDrop.DragOverEvent, map.DragOver);
            panel.AddHandler(DragDrop.DragEnterEvent, (object? sender, DragEventArgs e) => DropboxVisible = true);
            panel.AddHandler(DragDrop.DropEvent, (object? sender, DragEventArgs e) => {
                DiagramItem[]? beginners = map.Drop(sender, e);
                if (beginners != null) UnpackImport(beginners);
                DropboxVisible = false;
            });
        }
        bool dropbox_visible = false;
        public bool DropboxVisible { get => dropbox_visible; set => this.RaiseAndSetIfChanged(ref dropbox_visible, value); }

        /*
         * Вкладка общих параметров
         */

        string name = "yeah";
        int stereo = 0; // -, «static», «abstract», «interface»
        int access = 0; // private, public, protected, package

        public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }

        public bool Stereo_1 { get => stereo == 0; set => stereo = value ? 0 : -1; }
        public bool Stereo_2 { get => stereo == 1; set => stereo = value ? 1 : -1; }
        public bool Stereo_3 { get => stereo == 2; set => stereo = value ? 2 : -1; }
        public bool Stereo_4 { get => stereo == 3; set => stereo = value ? 3 : -1; }

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

        /*
         *  Ещё кнопочки ;'-}
         */

        private static readonly string[] stereos = new string[] { "static", "abstract", "interface" };

        private Dictionary<string, object> Export() {
            Dictionary<string, object> res = new() {
                ["name"] = name,
                ["stereo"] = stereo,
                ["access"] = access,
                ["attributes"] = attributes.Select(x => x.Export()).ToList(),
                ["methods"] = methods.Select(x => x.Export()).ToList(),
            };
            // Log.Write(Utils.Obj2json(res));
            return res;
        }

        private void Import(object entity) {
            if (entity is not Dictionary<string, object> @dict) { Log.Write("General: Ожидался словарь, вместо " + entity.GetType().Name); return; }

            @dict.TryGetValue("name", out var value);
            name = value is not string @str ? "yeah" : @str;

            @dict.TryGetValue("stereo", out var value2);
            stereo = value2 is not int @int ? 0 : @int;

            @dict.TryGetValue("access", out var value3);
            access = value3 is not int @int2 ? 0 : @int2;

            @dict.TryGetValue("attributes", out var value4);
            attributes.Clear();
            if (value4 is IEnumerable<object> @attrs)
                foreach (var attr in @attrs) attributes.Add(new AttributeItem(this, attr));

            @dict.TryGetValue("methods", out var value5);
            methods.Clear();
            if (value5 is IEnumerable<object> @meths)
                foreach (var meth in @meths) methods.Add(new MethodItem(this, meth));
        }

        private void UnpackImport(DiagramItem[]? items) {
            if (items != null && map.new_joins != null) {
                foreach (var item in items) {
                    Import(item.entity);
                    editable = item;
                    FuncApply();
                    canv.Children.Add(item);
                }

                foreach (var join in map.new_joins) canv.Children.Add(join.line);
                map.new_joins = null;
            }
        }

        private void FuncExport(string type) => map.Export(type, canv);
        private void FuncImport(string type) {
            UnpackImport(map.Import(type));
        }

        private void FuncApply() {
            StringBuilder sb = new();
            sb.Append($"{"-+#~"[access]} {name}");
            var head = stereo != 0 ?
                new MeasuredText[] { new("«" + stereos[stereo - 1] + "»"), new(sb.ToString()) } :
                new MeasuredText[] { new(sb.ToString()) };

            List<MeasuredText> arr = new(), arr2 = new();
            foreach (var attr in attributes) arr.Add(new(attr.ToString()));
            foreach (var meth in methods) arr2.Add(new(meth.ToString()));
            MeasuredText[] attrs = arr.ToArray();
            MeasuredText[] meths = arr2.ToArray();

            var pos = map.tap_pos;
            if (editable == null) {
                var item = new DiagramItem(head, attrs, meths, Export()) { Margin = new(pos.X - 75, pos.Y - 50, 0, 0) };
                canv.Children.Add(item);
                map.AddItem(item);
            } else {
                editable.Change(head, attrs, meths, Export());
                editable = null;
            }

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

        public string ApplyText { get => editable is null ? "Добавить" : "Изменить"; }

        public ReactiveCommand<Unit, Unit> Apply { get; }
        public ReactiveCommand<Unit, Unit> Close { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }
        public ReactiveCommand<string, Unit> ExportB { get; }
        public ReactiveCommand<string, Unit> ImportB { get; }
    }
}