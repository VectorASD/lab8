using DiagramEditor.ViewModels;
using ReactiveUI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Text;

namespace DiagramEditor.Models {
    public class MethodItem {
        private static readonly string[] stereos = new string[] { "virtual", "static", "abstract", "«create»" };

        string name = "mn";
        string type = "mt";
        int access = 0; // private, public, protected, package
        int stereo = 0; // virtual, static, abstract, «required»

        readonly MainWindowViewModel parent;

        public MethodItem(MainWindowViewModel mwvm) {
            parent = mwvm;

            AddNextMethod = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddNextMethod(); return new Unit(); });
            RemoveMe = ReactiveCommand.Create<Unit, Unit>(_ => { FuncRemoveMe(); return new Unit(); });

            AddFirstProp = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddFirstProp(); return new Unit(); });
        }

        // Параметры

        public string Name { get => name; set => parent.RaiseAndSetIfChanged(ref name, value); }
        public string Type { get => type; set => parent.RaiseAndSetIfChanged(ref type, value); }
        public int Access { get => access; set => parent.RaiseAndSetIfChanged(ref access, value); }
        public int Stereo { get => stereo; set => parent.RaiseAndSetIfChanged(ref stereo, value); }

        // Кнопочки

        public void FuncAddNextMethod() => parent.FuncAddNextMethod(this);
        public void FuncRemoveMe() => parent.FuncRemoveMethod(this);
        public ReactiveCommand<Unit, Unit> AddNextMethod { get; }
        public ReactiveCommand<Unit, Unit> RemoveMe { get; }

        /*
         * Список параметров
         */

        readonly ObservableCollection<PropertyItem> props = new();
        public ObservableCollection<PropertyItem> Properties { get => props; }

        public ReactiveCommand<Unit, Unit> AddFirstProp { get; }

        private void FuncAddFirstProp() => props.Insert(0, new PropertyItem(this));
        public void FuncAddNextProp(PropertyItem item) => props.Insert(props.IndexOf(item) + 1, new PropertyItem(this));
        public void FuncRemoveProp(PropertyItem item) => props.Remove(item);

        // Цель/суть

        public override string ToString() {
            StringBuilder sb = new();
            sb.Append("-+#~"[access]);
            if (stereo != 0) {
                sb.Append(' ');
                sb.Append(stereos[stereo]);
            }
            sb.Append(' ');
            sb.Append(name);
            sb.Append(" (");
            sb.Append(string.Join(", ", props));
            sb.Append("): ");
            sb.Append(type);
            return sb.ToString();
        }

        public Dictionary<string, object> Export() {
            return new() {
                ["name"] = name,
                ["type"] = type,
                ["access"] = access,
                ["stereo"] = stereo,
                ["props"] = props.Select(x => x.Export()).ToList(),
            };
        }

        public MethodItem(MainWindowViewModel mwvm, object entity) : this(mwvm) { // Import
            if (entity is not Dictionary<string, object> @dict) { Log.Write("MethodItem: Ожидался словарь, вместо " + entity.GetType().Name); return; }

            @dict.TryGetValue("name", out var value);
            name = value is not string @str ? "mn" : @str;

            @dict.TryGetValue("type", out var value2);
            type = value2 is not string @str2 ? "mt" : @str2;

            @dict.TryGetValue("access", out var value3);
            access = value3 is not int @int ? 0 : @int;

            @dict.TryGetValue("stereo", out var value4);
            stereo = value4 is not int @int2 ? 0 : @int2;

            @dict.TryGetValue("props", out var value5);
            props.Clear();
            if (value5 is IEnumerable<object> @propz)
                foreach (var prop in @propz) props.Add(new PropertyItem(this, prop));
        }
    }
}
