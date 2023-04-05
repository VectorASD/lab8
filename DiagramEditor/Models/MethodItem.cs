using DiagramEditor.ViewModels;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;

namespace DiagramEditor.Models {
    public class MethodItem {
        private static readonly string[] stereos = new string[] { "virtual", "static", "abstract", "«create»" };

        string name = "mn";
        string type = "mt";
        int access = 0; // private, public, protected, package
        int stereo = 0; // virtual, static, abstract, "required"

        MainWindowViewModel parent;

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
    }
}
