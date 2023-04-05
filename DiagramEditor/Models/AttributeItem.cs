using DiagramEditor.ViewModels;
using ReactiveUI;
using System.Reactive;

namespace DiagramEditor.Models {
    public class AttributeItem {
        string name = "name";
        string type = "type";
        int access = 0; // private, public, internal
        bool _readonly = false;
        bool _static = false;
        int stereo = 0; // -, "event", "property", "required"

        MainWindowViewModel parent;

        public AttributeItem(MainWindowViewModel mwvm) {
            parent = mwvm;

            AddNextAttr = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddNextAttr(); return new Unit(); });
            RemoveMe = ReactiveCommand.Create<Unit, Unit>(_ => { FuncRemoveMe(); return new Unit(); });
        }

        // Параметры

        public string Name { get => name; set { name = value; } }
        public string Type { get => type; set { type = value; } }
        public int Access { get => access; set { access = value; } }
        public bool IsReadonly { get => _readonly; set { _readonly = value; } }
        public bool IsStatic { get => _static; set { _static = value; } }
        public int Stereo { get => stereo; set { stereo = value; } }

        // Кнопочки

        public void FuncAddNextAttr() => parent.FuncAddNextAttr(this);
        public void FuncRemoveMe() => parent.FuncRemoveAttr(this);
        public ReactiveCommand<Unit, Unit> AddNextAttr { get; }
        public ReactiveCommand<Unit, Unit> RemoveMe { get; }
    }
}
