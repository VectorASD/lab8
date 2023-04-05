using Avalonia.Controls;
using DiagramEditor.ViewModels;

namespace DiagramEditor.Views {
    public partial class MainWindow: Window {
        public MainWindow() {
            InitializeComponent();
            DataContext = new MainWindowViewModel(this);
        }
    }
}