using DiagramEditor.ViewModels;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;

namespace DiagramEditor.Models {
    public class MethodItem {
        string name = "name";
        string type = "type";
        int access = 0; // private, public, internal
        int stereo = 0; // virtual, static, abstract, "required"

        MainWindowViewModel parent;

        public MethodItem(MainWindowViewModel mwvm) {
            parent = mwvm;

            AddNextMethod = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddNextMethod(); return new Unit(); });
            RemoveMe = ReactiveCommand.Create<Unit, Unit>(_ => { FuncRemoveMe(); return new Unit(); });

            AddFirstProp = ReactiveCommand.Create<Unit, Unit>(_ => { FuncAddFirstProp(); return new Unit(); });
        }

        // Параметры

        public string Name { get => name; set { name = value; } }
        public string Type { get => type; set { type = value; } }
        public int Access { get => access; set { access = value; } }
        public int Stereo { get => stereo; set { stereo = value; } }

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
    }
}
