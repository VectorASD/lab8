using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using DiagramEditor.Models;
using DiagramEditor.ViewModels;
using System.Collections.Generic;

namespace DiagramEditor.Views {
    public partial class MainWindow: Window {
        private readonly MainWindowViewModel mwvm;

        public MainWindow() {
            mwvm = new MainWindowViewModel();
            DataContext = mwvm;
            map = mwvm.GetMap;

            mwvm.CloseMenu = CloseMenu;
            mwvm.ExportF = ExportF;
            mwvm.ImportF = ImportF;
            mwvm.ApplyF = ApplyF;
            mwvm.IsEditable = IsEditable;

            map.Marker_SetState = Marker_SetState;
            map.Marker2_SetState = Marker2_SetState;
            map.SaveMarkerVisibles = SaveMarkerVisibles;
            map.RestoreMarkerVisibles = RestoreMarkerVisibles;

            InitializeComponent();
            AddWindow();
        }

        /*
         * Проводники между этим классом и ViewModel. Не MVVM, их бы не было XD
         */

        private AddShape? menu;
        private Canvas canv = new();
        public DiagramItem? editable;
        private readonly Mapper map;

        private void CloseMenu() {
            if (menu == null) return;
            menu.Close();
            menu = null;
        }

        private void ExportF(string type) => map.Export(type, canv);

        private void Import(DiagramItem[]? items) {
            if (items != null && map.new_joins != null) {
                foreach (var item in items) {
                    mwvm.Import(item.entity);
                    editable = item;
                    mwvm.FuncApply();
                    canv.Children.Add(item);
                }

                foreach (var join in map.new_joins) canv.Children.Add(join.line);
                map.new_joins = null;
            }

        }
        private void ImportF(string type) => Import(map.Import(type));

        private void ApplyF(MeasuredText[] head, MeasuredText[] attrs, MeasuredText[] meths, object entity) {
            var pos = map.tap_pos;
            if (editable == null) {
                var item = new DiagramItem(head, attrs, meths, entity) { Margin = new(pos.X - 75, pos.Y - 50, 0, 0) };
                canv.Children.Add(item);
                map.AddItem(item);
            } else {
                editable.Change(head, attrs, meths, entity);
                editable = null;
            }
        }

        private bool IsEditable() => editable is not null;

        /*
         * Проводники между этим классом и Mapper. Не MVVM, их бы не было XD
         */

        private void FixItem(ref Control res, Point pos, IEnumerable<ILogical> items) {
            foreach (var logic in items) {
                // if (item.IsPointerOver) { } Гениальная вещь! ;'-} Хотя не, всё равно блокируется после Press и до Release, чего я впринципе хочу избежать ;'-}
                var item = (Control) logic;
                var tb = item.TransformedBounds;
                if (tb != null && new Rect(tb.Value.Clip.TopLeft, new Size()).Sum(item.Bounds).Contains(pos) && (string?) item.Tag != "arrow") res = item; // Гениально! ;'-} НАКОНЕЦ-ТО ЗАРАБОТАЛО!
                FixItem(ref res, pos, item.GetLogicalChildren());
            }
        }
        private Control FixItem(Control item, Point pos) {
            var mode = map.GetMode;
            // К сожалению, во время протягивания проводки, marker Ellipse застревает, как кость в горле, так что и соответствующий багофикс, ибо то, что попало в Press, фиксируется на всё время (Move/до конца Release) ;'-}
            if (mode == 3 || mode == 6) {
                item = new Canvas() { Tag = "scene" };
                FixItem(ref item, pos + new Point(0, 32), canv.Children); // doc_panel height = 32
                // Log.Write("Item: " + item + " " + item.Tag);
            }
            return item;
        }

        /*
         * Маркеры
         */

        readonly Ellipse marker = new() { Tag = "marker", Stroke = Brushes.Orange, Fill = Brushes.Yellow, StrokeThickness = 2, Width = 12, Height = 12, ZIndex = 2, IsVisible = false };
        readonly ArrowFactory marker2 = new() { Tag = "marker2", ZIndex = 2, IsVisible = false };
        void Marker_SetState(bool? vis, Thickness? margin) {
            if (vis != null) marker.IsVisible = (bool) vis;
            if (margin != null) marker.Margin = (Thickness) margin;
        }
        void Marker2_SetState(bool? vis, Point? start, Point? end, int? type) {
            if (vis != null) marker2.IsVisible = (bool) vis;
            if (start != null) marker2.StartPoint = (Point) start;
            if (end != null) marker2.EndPoint = (Point) end;
            if (type != null) marker2.Type = (int) type;
        }
        bool mvs = false, m2vs = false;
        void SaveMarkerVisibles() {
            mvs = marker.IsVisible;
            m2vs = marker2.IsVisible;
            marker.IsVisible = marker2.IsVisible = false;
        }
        void RestoreMarkerVisibles() {
            marker.IsVisible = mvs;
            marker2.IsVisible = m2vs;
        }

        /*
         * Основная мясорубка
         */

        void AddWindow() {
            canv = this.Find<Canvas>("canvas") ?? new Canvas();
            canv.Children.Add(marker);
            canv.Children.Add(marker2);
            var panel = (Panel?) canv.Parent;
            if (panel == null) return;

            panel.PointerPressed += (sender, e) => {
                if (e.Source != null && e.Source is Control @control) map.Press(@control, e.GetCurrentPoint(canv).Position);
            };
            panel.PointerMoved += (sender, e) => {
                if (e.Source != null && e.Source is Control @control) {
                    var pos = e.GetCurrentPoint(canv).Position;
                    map.Move(FixItem(@control, pos), pos);
                }
            };
            panel.PointerReleased += (sender, e) => {
                if (e.Source != null && e.Source is Control @control) {
                    var pos = e.GetCurrentPoint(canv).Position;
                    map.Release(FixItem(@control, pos), pos);

                    if (map.tap_mode == 1) {
                        editable = null;
                        menu = new AddShape { DataContext = mwvm };
                        menu.ShowDialog(this);
                    }

                    if (map.new_join != null) {
                        var newy = map.new_join.line;
                        canv.Children.Add(newy);
                        map.new_join = null;
                    }

                    if (map.tap_mode == 2 && map.tapped_item != null) {
                        editable = map.tapped_item;
                        mwvm.Import(editable.entity);
                        menu = new AddShape { DataContext = mwvm, Title = "Редактирование ноды диаграммы" };
                        menu.ShowDialog(this);
                    }
                }
            };

            panel.PointerWheelChanged += (sender, e) => {
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
            panel.AddHandler(DragDrop.DragEnterEvent, (sender, e) => mwvm.DropboxVisible = true);
            panel.AddHandler(DragDrop.DropEvent, (sender, e) => {
                DiagramItem[]? beginners = map.Drop(sender, e);
                if (beginners != null) Import(beginners);
                mwvm.DropboxVisible = false;
            });
        }
    }
}