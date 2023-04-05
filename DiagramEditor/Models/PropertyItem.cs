using DiagramEditor.ViewModels;
using ReactiveUI;
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
    }
}
