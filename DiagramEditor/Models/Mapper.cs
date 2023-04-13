using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using DiagramEditor.ViewModels;
using DiagramEditor.Views;
using DynamicData;
using System;
using System.Collections.Generic;

namespace DiagramEditor.Models {
    public class Mapper {
        readonly Ellipse marker = new() { Tag = "marker", Stroke = Brushes.Orange, Fill = Brushes.Yellow, StrokeThickness = 2, Width = 12, Height = 12, ZIndex = 2, IsVisible = false };
        readonly Line marker2 = new() { Tag = "marker", Stroke = Brushes.Blue, StrokeThickness = 3, ZIndex = 2, IsVisible = false };
        private DiagramItem? marker_parent;
        public Ellipse Marker { get => marker; }
        public Line Marker2 { get => marker2; }

        readonly List<DiagramItem> items = new();
        Point camera_pos;
        public void AddItem(DiagramItem item) {
            items.Add(item);
        }
        private void RemoveItem(DiagramItem item) {
            items.Remove(item);
            var canv = (Canvas) (item.Parent ?? throw new Exception("Чё?!"));
            canv.Children.Remove(item);
        }

        readonly Points saved_poses = new();
        private void SaveItemsPos() {
            saved_poses.Clear();
            foreach (var item in items) {
                var m = item.Margin;
                saved_poses.Add(new(m.Left, m.Top));
            }
        }
        private void MoveItems(Point delta) {
            int n = 0;
            foreach (var item in items) {
                var pos = saved_poses[n];
                var new_pos = pos + delta;
                item.Margin = new(new_pos.X, new_pos.Y, 0, 0);
                n += 1;
            }
        }

        /*
         * Определение режима перемещения
         */

        int mode = 0;
        /*
         * Решил реализовать многорежимную систему перемещения:
         * 0 - ничего не делает
         * 1 - двигаем камеру
         * 2 - двигаем элемент
         * 3 - тянем проводку
         * 4 - растягиваем элемент
         * 5 - удаляем элемент
        */
        private void Calc_mode(Control item) {
            var c = (string?) item.Tag;
            mode = c switch {
                "scene" => 1,
                "item" => 2,
                "field" => 3,
                "marker" => 3,
                "resizer" => 4,
                "deleter" => 5,
                _ => 0,
            };
        }
        private static bool IsMode3(Control item) => (string?) item.Tag == "field" || (string?) item.Tag == "marker";
        private static bool IsMode(Control item, string[] mods) {
            var name = (string?) item.Tag;
            if (name == null) return false;
            return mods.IndexOf(name) != -1;
        }

        private DiagramItem? GetItemRoot(Control item) {
            if ((string?) item.Tag == "marker" && marker_parent != null) return marker_parent;
            string[] mods = new[] { "item", "field", "resizer", "deleter" };
            while (item.Parent != null && IsMode(item, mods)) {
                if (item is DiagramItem @DI) return @DI;
                item = (Control) item.Parent;
            }
            return null;
        }

        /*
         * Обработка мыши
         */

        Control? moved_item;
        Point moved_pos;
        Point item_old_pos;
        bool tapped = false;

        public Point tap_pos;

        public void Press(Control item, Point pos) {
            // Log.Write("PointerPressed: " + item.GetType().Name + " pos: " + pos);

            Calc_mode(item);
            Log.Write("new_mode: " + mode);

            moved_item = item;
            moved_pos = pos;
            tapped = true;

            switch (mode) {
            case 1:
                item_old_pos = camera_pos;
                SaveItemsPos();
                break;
            case 2:
                var d_item = GetItemRoot(item);
                if (d_item == null) break;
                var m = d_item.Margin;
                item_old_pos = new Point(m.Left, m.Top) - camera_pos;
                break;
            case 3:
                item_old_pos = pos;
                marker2.StartPoint = pos;
                marker2.EndPoint = pos;
                marker2.IsVisible = true;
                marker_parent = GetItemRoot(item);
                break;
            case 4:
                d_item = GetItemRoot(item);
                if (d_item == null) break;
                item_old_pos = new(d_item.Width, d_item.Height);
                break;
            }

            Move(item, pos);
        }

        public void Move(Control item, Point pos) {
            // Log.Write("PointerMoved: " + item.GetType().Name + " pos: " + pos);

            if (mode == 0 && IsMode3(item)) {
                if (marker_parent == null) {
                    var d_item = GetItemRoot(item);
                    if (d_item != null) marker_parent = d_item;
                }
                if (marker_parent != null) {
                    var m_pos = marker_parent.GetPos(pos);
                    marker.Margin = new(m_pos.X - 6, m_pos.Y - 6, 0, 0);
                    marker.IsVisible = true;
                }
            } else {
                marker.IsVisible = false;
                marker_parent = null;
            }

            if (moved_item != item) return;

            var delta = pos - moved_pos;
            if (delta.X == 0 && delta.Y == 0) return;

            if (Math.Pow(delta.X, 2) + Math.Pow(delta.Y, 2) > 9) tapped = false;

            switch (mode) {
            case 1:
                camera_pos = item_old_pos + delta;
                MoveItems(delta);
                break;
            case 2:
                var d_item = GetItemRoot(item);
                if (d_item == null) break;
                var new_pos = item_old_pos + delta + camera_pos;
                d_item.Margin = new(new_pos.X, new_pos.Y, 0, 0);
                break;
            case 3:
                marker2.EndPoint = pos;
                break;
            case 4:
                d_item = GetItemRoot(item);
                if (d_item == null) break;
                new_pos = item_old_pos + delta;
                d_item.Resize(new_pos.X, new_pos.Y);
                break;
            }
        }

        public bool Release(Control item, Point pos) {
            Move(item, pos);
            // Log.Write("PointerReleased: " + item.GetType().Name + " pos: " + pos);

            if (mode == 3) marker2.IsVisible = false;

            if (tapped) {
                Tapped(item, pos);
                bool res = mode == 1;
                mode = 0;
                return res;
            }
            mode = 0;
            return false;
        }

        private void Tapped(Control item, Point pos) {
            Log.Write("Tapped: " + item.GetType().Name + " pos: " + pos);
            tap_pos = pos;
            
            if (mode == 5) {
                var d_item = GetItemRoot(item);
                Log.Write("remove " + d_item);
                if (d_item != null) RemoveItem(d_item);
            }
        }
    }
}
