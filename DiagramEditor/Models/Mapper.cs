using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using DiagramEditor.ViewModels;
using System;
using System.Collections.Generic;

namespace DiagramEditor.Models {
    public class Mapper {
        readonly Ellipse marker = new() { Fill = Brushes.Yellow, Width = 8, Height = 8, ZIndex = 2, IsVisible = false };
        readonly Line marker2 = new() { Stroke = Brushes.Blue, StrokeThickness = 3, ZIndex = 2, IsVisible = false };
        public Ellipse Marker { get => marker; }
        public Line Marker2 { get => marker2; }

        readonly List<Control> items = new();
        Point camera_pos;
        public void AddItem(Control item) {
            items.Add(item);
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
        */
        private void Calc_mode(Control item) {
            if (item is Ellipse) { Log.Write("LOL"); mode = 3; return; }
            var c = (string?) item.Tag;
            mode = c switch {
                "scene" => 1,
                "item" => 2,
                "field" => 3,
                "resizer" => 4,
                _ => 0,
            };
        }
        private static bool IsMode2(Control item) => (string?) item.Tag == "item";
        private static bool IsMode3(Control item) => (string?) item.Tag == "field" || item is Ellipse;

        private static Control GetItemRoot(Control item) {
            while (item.Parent != null && IsMode2(item) && item is not UserControl)
                item = (Control) item.Parent;
            return item;
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
            item = GetItemRoot(item);
            // Log.Write("PointerPressed: " + item.GetType().Name + " pos: " + pos);

            Calc_mode(item);
            Log.Write("new_mode: " + mode);

            var m = item.Margin;
            moved_item = item;
            moved_pos = pos;
            tapped = true;

            switch (mode) {
            case 1:
                item_old_pos = camera_pos;
                SaveItemsPos();
                break;
            case 2:
                item_old_pos = new Point(m.Left, m.Top) - camera_pos;
                break;
            case 3:
                item_old_pos = pos;
                marker2.StartPoint = pos;
                marker2.EndPoint = pos;
                marker2.IsVisible = true;
                break;
            }

            Move(item, pos);
        }

        public void Move(Control item, Point pos) {
            item = GetItemRoot(item);
            // Log.Write("PointerMoved: " + item.GetType().Name + " pos: " + pos);

            if (mode == 0 && IsMode3(item)) {
                marker.Margin = new(pos.X - 4, pos.Y - 4, 0, 0);
                marker.IsVisible = true;
            } else marker.IsVisible = false;

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
                var new_pos = item_old_pos + delta + camera_pos;
                item.Margin = new(new_pos.X, new_pos.Y, 0, 0);
                break;
            case 3:
                marker2.EndPoint = pos;
                break;
            }
        }

        public bool Release(Control item, Point pos) {
            item = GetItemRoot(item);
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
        }
    }
}
