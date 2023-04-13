using DiagramEditor.ViewModels;
using ReactiveUI;
using System.Collections.Generic;
using System.Reactive;

namespace DiagramEditor.Models {
    public class PropertyItem {
        string name = "pn";
        string type = "pt";
        string _default = "pd";

        readonly MethodItem parent;

        public PropertyItem(MethodItem item) {
            parent = item;

            AddNextProp = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddNextProp(); return new Unit(); });
            RemoveMe = ReactiveCommand.Create<Unit, Unit>(_ => { FuncRemoveMe(); return new Unit(); });
        }

        // Параметры

        public string Name { get => name; set => name = value; }
        public string Type { get => type; set => type = value; }
        public string Default { get => _default; set => _default = value; }

        // Кнопочки

        public void FuncAddNextProp() => parent.FuncAddNextProp(this);
        public void FuncRemoveMe() => parent.FuncRemoveProp(this);
        public ReactiveCommand<Unit, Unit> AddNextProp { get; }
        public ReactiveCommand<Unit, Unit> RemoveMe { get; }

        // Цель/суть

        public override string ToString() => $"{name} : {type}" + (_default == "" ? "" : " = " + _default);

        public Dictionary<string, object> Export() => new() { ["name"] = name, ["type"] = type, ["default"] = _default };

        public PropertyItem(MethodItem item, object entity) : this(item) { // Import
            if (entity is not Dictionary<string, object> @dict) { Log.Write("MethodItem: Ожидался словарь, вместо " + entity.GetType().Name); return; }

            @dict.TryGetValue("name", out var value);
            name = value is not string @str ? "pn" : @str;

            @dict.TryGetValue("type", out var value2);
            type = value2 is not string @str2 ? "pt" : @str2;

            @dict.TryGetValue("default", out var value3);
            _default = value3 is not string @str3 ? "pd" : @str3;
        }
    }
}
