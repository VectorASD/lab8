using DiagramEditor.ViewModels;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace DiagramEditor.Models {
    public class AttributeItem {
        private static readonly string[] stereos = new string[] { "event", "property", "required" };

        string name = "an";
        string type = "at";
        int access = 0; // private, public, protected, package
        bool _readonly = false;
        bool _static = false;
        int stereo = 0; // -, «event», «property», «required»
        string _default = "ad";

        readonly MainWindowViewModel parent;

        public AttributeItem(MainWindowViewModel mwvm) {
            parent = mwvm;

            AddNextAttr = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddNextAttr(); return new Unit(); });
            RemoveMe = ReactiveCommand.Create<Unit, Unit>(_ => { FuncRemoveMe(); return new Unit(); });
        }

        // Параметры

        public string Name { get => name; set => parent.RaiseAndSetIfChanged(ref name, value); }
        public string Type { get => type; set => parent.RaiseAndSetIfChanged(ref type, value); }
        public int Access { get => access; set => parent.RaiseAndSetIfChanged(ref access, value); }
        public bool IsReadonly { get => _readonly; set => parent.RaiseAndSetIfChanged(ref _readonly, value); }
        public bool IsStatic { get => _static; set => parent.RaiseAndSetIfChanged(ref _static, value); }
        public int Stereo { get => stereo; set => parent.RaiseAndSetIfChanged(ref stereo, value); }
        public string Default { get => _default; set => parent.RaiseAndSetIfChanged(ref _default, value); }

        // Кнопочки

        public void FuncAddNextAttr() => parent.FuncAddNextAttr(this);
        public void FuncRemoveMe() => parent.FuncRemoveAttr(this);
        public ReactiveCommand<Unit, Unit> AddNextAttr { get; }
        public ReactiveCommand<Unit, Unit> RemoveMe { get; }

        // Цель/суть

        public override string ToString() {
            StringBuilder sb = new();
            sb.Append("-+#~"[access]);
            if (stereo != 0) {
                sb.Append(" «");
                sb.Append(stereos[stereo - 1]);
                sb.Append('»');
            }
            if (_static) sb.Append(" static");
            if (_readonly) sb.Append(" readonly");
            sb.Append(' ');
            sb.Append(name);
            sb.Append(" : ");
            sb.Append(type);
            if (_default != "") {
                sb.Append(" = ");
                sb.Append(_default);
            }
            return sb.ToString();
        }

        public Dictionary<string, object> Export() {
            return new() {
                ["name"] = name,
                ["type"] = type,
                ["access"] = access,
                ["readonly"] = _readonly,
                ["static"] = _static,
                ["stereo"] = stereo,
                ["default"] = _default,
            };
        }

        public AttributeItem(MainWindowViewModel mwvm, object entity) : this(mwvm) { // Import
            if (entity is not Dictionary<string, object> @dict) { Log.Write("AttributeItem: Ожидался словарь, вместо " + entity.GetType().Name); return; }

            @dict.TryGetValue("name", out var value);
            name = value is not string @str ? "an" : @str;

            @dict.TryGetValue("type", out var value2);
            type = value2 is not string @str2 ? "at" : @str2;

            @dict.TryGetValue("access", out var value3);
            access = value3 is not int @int ? 0 : @int;

            @dict.TryGetValue("readonly", out var value4);
            _readonly = value4 is bool @bool && @bool;

            @dict.TryGetValue("static", out var value5);
            _static = value5 is bool @bool2 && @bool2;

            @dict.TryGetValue("stereo", out var value6);
            stereo = value6 is not int @int2 ? 0 : @int2;

            @dict.TryGetValue("default", out var value7);
            _default = value7 is not string @str3 ? "ad" : @str3;
        }
    }
}
