using DiagramEditor.Models;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;

namespace DiagramEditor.ViewModels {
    public class MainWindowViewModel: ViewModelBase {
        private string log = "";
        public string Logg { get => log; set => this.RaiseAndSetIfChanged(ref log, value); }

        static readonly List<string> logs = new();
        private void LogHandler(string message) {
            foreach (var mess in message.Split('\n')) logs.Add(mess);
            while (logs.Count > 45) logs.RemoveAt(0);
            Logg = string.Join('\n', logs);
        }



        private readonly Mapper map = new();
        public Mapper GetMap => map;

        public MainWindowViewModel() {
            Log.NewLine += LogHandler;

            AddFirstAttr = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddFirstAttr(); return new Unit(); });
            AddFirstMethod = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddFirstMethod(); return new Unit(); });

            Apply = ReactiveCommand.Create<Unit, Unit>(_ => { FuncApply(); return new Unit(); });
            Close = ReactiveCommand.Create<Unit, Unit>(_ => { FuncClose(); return new Unit(); });
            Clear = ReactiveCommand.Create<Unit, Unit>(_ => { FuncClear(); return new Unit(); });
            ExportB = ReactiveCommand.Create<string, Unit>(n => { FuncExport(n); return new Unit(); });
            ImportB = ReactiveCommand.Create<string, Unit>(n => { FuncImport(n); return new Unit(); });
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

        public void Import(object entity) {
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


        public Action<string>? ExportF;
        public Action<string>? ImportF;
        public Action<MeasuredText[], MeasuredText[], MeasuredText[], object>? ApplyF;
        public Func<bool>? IsEditable;

        private void FuncExport(string type) => ExportF?.Invoke(type);
        private void FuncImport(string type) => ImportF?.Invoke(type);

        public void FuncApply() {
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

            var entity = Export();
            ApplyF?.Invoke(head, attrs, meths, entity);

            FuncClose();
        }
        public Action? CloseMenu;
        private void FuncClose() => CloseMenu?.Invoke();
        private void FuncClear() {
            Name = "yeah";
            stereo = 0;
            access = 0;
            attributes.Clear();
            methods.Clear();
            this.RaisePropertyChanged(nameof(Stereo_1));
            this.RaisePropertyChanged(nameof(Access_1));
        }

        public string ApplyText { get => (IsEditable?.Invoke() ?? false) ? "Изменить" : "Добавить"; }

        public ReactiveCommand<Unit, Unit> Apply { get; }
        public ReactiveCommand<Unit, Unit> Close { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }
        public ReactiveCommand<string, Unit> ExportB { get; }
        public ReactiveCommand<string, Unit> ImportB { get; }
    }
}