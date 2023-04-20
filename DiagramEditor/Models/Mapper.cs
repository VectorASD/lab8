using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using DiagramEditor.ViewModels;
using DiagramEditor.Views;
using DynamicData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DiagramEditor.Models
{
    public class Mapper {
        readonly Ellipse marker = new() { Tag = "marker", Stroke = Brushes.Orange, Fill = Brushes.Yellow, StrokeThickness = 2, Width = 12, Height = 12, ZIndex = 2, IsVisible = false };
        readonly ArrowFactory marker2 = new() { Tag = "marker2", ZIndex = 2, IsVisible = false };
        private DiagramItem? marker_parent;
        public Ellipse Marker { get => marker; }
        public ArrowFactory Marker2 { get => marker2; }

        readonly List<DiagramItem> items = new();
        Point camera_pos;
        public void AddItem(DiagramItem item) {
            items.Add(item);
        }
        private void RemoveItem(DiagramItem item) {
            items.Remove(item);
            var canv = (Canvas) (item.Parent ?? throw new Exception("Чё?!"));
            canv.Children.Remove(item);
            item.ClearJoins();
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
                item.Move(new_pos, true);
                n += 1;
            }
        }

        /*
         * Определение режима перемещения
         */

        int mode = 0;
        /*
         * Решил реализовать многорежимную систему перемещений:
         * 0 - ничего не делает
         * 1 - двигаем камеру
         * 2 - двигаем элемент
         * 3 - тянем проводку
         * 4 - растягиваем элемент
         * 5 - удаляем элемент
         * 6 - перемещаем уже готовую стрелочку (колесом меняем её тип)
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
                "arrow" => 6,
                _ => 0,
            };
        }
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

        Point moved_pos;
        Point item_old_pos;
        bool tapped = false;
        Distantor? marker_pos;
        Distantor? start_dist;

        public Point tap_pos;
        bool arrow_start;
        ArrowFactory? old_arrow;
        bool delete_arrow = false;

        public void Press(Control item, Point pos) {
            // Log.Write("PointerPressed: " + item.GetType().Name + " pos: " + pos);

            Calc_mode(item);
            // Log.Write("new_mode: " + mode);

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
                if (marker_pos == null) { mode = 0; break; }
                item_old_pos = pos;
                marker2.StartPoint = marker2.EndPoint = marker_pos.p;
                marker2.IsVisible = true;
                marker_parent = GetItemRoot(item);
                start_dist = marker_pos;
                break;
            case 4:
                d_item = GetItemRoot(item);
                if (d_item == null) break;
                item_old_pos = new(d_item.Width, d_item.Height);
                break;
            case 6:
                if (item is not ArrowFactory @arrow) break;
                var dist_a = @arrow.StartPoint.Hypot(pos);
                var dist_b = @arrow.EndPoint.Hypot(pos);
                arrow_start = dist_a > dist_b;
                old_arrow = @arrow;

                marker2.StartPoint = arrow_start ? @arrow.StartPoint : pos;
                marker2.EndPoint = arrow_start ? pos : @arrow.EndPoint;
                marker2.IsVisible = true;
                marker2.Type = arrow.Type;
                @arrow.IsVisible = false;
                break;
            }

            Move(item, pos);
        }

        public void FixItem(ref Control res, Point pos, IEnumerable<ILogical> items) {
            foreach (var logic in items) {
                // if (item.IsPointerOver) { } Гениальная вещь! ;'-} Хотя не, всё равно блокируется после Press и до Release, чего я впринципе хочу избежать ;'-}
                var item = (Control) logic;
                var tb = item.TransformedBounds;
                if (tb != null && new Rect(tb.Value.Clip.TopLeft, new Size()).Sum(item.Bounds).Contains(pos) && (string?) item.Tag != "arrow") res = item; // Гениально! ;'-} НАКОНЕЦ-ТО ЗАРАБОТАЛО!
                FixItem(ref res, pos, item.GetLogicalChildren());
            }
        }
        public void Move(Control item, Point pos) {
            // Log.Write("PointerMoved: " + item.GetType().Name + " pos: " + pos);

            // К сожалению, во время протягивания проводки, marker Ellipse застревает, как кость в горле, так что и соответствующий багофикс, ибо то, что попало в Press, фиксируется на всё время (Move/до конца Release) ;'-}
            if (mode == 3 || mode == 6) {
                item = new Canvas() { Tag = "scene" };
                FixItem(ref item, pos + new Point(0, 32), items); // doc_panel height = 32
                // Log.Write("Item: " + item + " " + item.Tag);
            }

            string[] mods = new[] { "field", "marker" };
            if (IsMode(item, mods)) {
                if (marker_parent == null) {
                    var d_item = GetItemRoot(item);
                    if (d_item != null) marker_parent = d_item;
                    marker.IsVisible = true;
                }
                if (marker_parent != null) {
                    var m_pos = marker_parent.GetPos(pos);
                    marker.Margin = new(m_pos.p.X - 6, m_pos.p.Y - 6, 0, 0);
                    marker_pos = m_pos;
                }
            } else {
                marker.IsVisible = false;
                marker_parent = null;
                marker_pos = null;
            }

            if (mode == 6) delete_arrow = (string?) item.Tag == "deleter";



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
                d_item.Move(new_pos, false);
                break;
            case 3:
                marker2.EndPoint = marker_pos == null ? pos : marker_pos.p;
                break;
            case 4:
                d_item = GetItemRoot(item);
                if (d_item == null) break;
                new_pos = item_old_pos + delta;
                d_item.Resize(new_pos.X, new_pos.Y);
                break;
            case 6:
                if (old_arrow == null) break;
                var p = marker_pos == null ? pos : marker_pos.p;
                if (arrow_start) marker2.EndPoint = p;
                else marker2.StartPoint = p;
                break;
            }
        }

        public JoinedItems? new_join; // Обрабатывается после Release
        public int tap_mode; // Обрабатывается после Release
        public DiagramItem? tapped_item; // Обрабатывается после Release

        public void Release(Control item, Point pos) {
            Move(item, pos);
            // Log.Write("PointerReleased: " + item.GetType().Name + " pos: " + pos);

            if (mode == 3) {
                if (start_dist != null && marker_pos != null) {
                    var join = new JoinedItems(start_dist, marker_pos);
                    new_join = join;
                }
                marker2.IsVisible = false;
            }

            if (mode == 6 && old_arrow != null) {
                JoinedItems.arrow_to_join.TryGetValue(old_arrow, out var @join);
                if (marker_pos != null && @join != null) {
                    if (arrow_start) @join.B = marker_pos;
                    else @join.A = marker_pos;
                    @join.Update();
                }
                old_arrow.IsVisible = true;
                marker2.IsVisible = false;
                old_arrow = null;

                if (delete_arrow && @join != null) @join.Delete();
                delete_arrow = false;
            }

            if (tapped) {
                Tapped(item, pos);
                tap_mode = mode;
                if (mode == 2) tapped_item = GetItemRoot(item);
            } else tap_mode = -1;
            mode = 0;
        }

        private void Tapped(Control item, Point pos) {
            // Log.Write("Tapped: " + item.GetType().Name + " pos: " + pos);
            tap_pos = pos;
            
            if (mode == 5) {
                var d_item = GetItemRoot(item);
                if (d_item != null) RemoveItem(d_item);
            }
        }

        public void WheelMove(Control item, double move) {
            // Log.Write("WheelMoved: " + item.GetType().Name + " delta: " + (move > 0 ? 1 : -1));
            if ((string?) item.Tag == "arrow" && item is ArrowFactory @arrow)
                @arrow.Type = (@arrow.Type + (move > 0 ? 1 : 5)) % 6;
        }

        /*
         * Система импортов и экспортов
         */

        public void Export(string type, Control canv) {
            if (type == "PNG") {
                bool a = marker.IsVisible, b = marker2.IsVisible;
                marker.IsVisible = marker2.IsVisible = false;

                try { Utils.RenderToFile(canv, "../../../Export.png"); } catch (Exception e) { Log.Write("Ошибка экспорта PNG: " + e); }

                marker.IsVisible = a; marker2.IsVisible = b;
                return;
            }

            List<object> entities = new();
            Dictionary<DiagramItem, int> di_to_num = new();
            int num = 0;
            foreach (var item in items) {
                var m = item.Margin;
                var d = new object[] { item.entity, (int) m.Left, (int) m.Top, (int) item.Width, (int) item.Height };
                entities.Add(d);
                di_to_num[item] = num++;
            }
            List<object[]> joins = new();
            foreach (var item in items) joins.Add(item.ExportJoins(di_to_num));

            Dictionary<string, object> data = new() {
                ["items"] = entities.ToList(),
                ["joins"] = joins,
            };

            switch (type) {
            case "JSON":
                var json = Utils.Obj2json(data);
                if (json == null) { Log.Write("Не удалось экспортировать в Export.json :/"); return; }
                // Log.Write("J: " + json);
                File.WriteAllText("../../../Export.json", json);
                break;
            case "XML":
                var xml = Utils.Obj2xml(data);
                if (xml == null) { Log.Write("Не удалось экспортировать в Export.xml :/"); return; }
                // Log.Write("X: " + xml);
                File.WriteAllText("../../../Export.xml", xml);
                break;
            case "YAML":
                var yaml = Utils.Obj2yaml(data);
                if (yaml == null) { Log.Write("Не удалось экспортировать в Export.yaml :/"); return; }
                // Log.Write("Y: " + yaml);
                File.WriteAllText("../../../Export.yaml", yaml);
                break;
            }
        }

        public JoinedItems[]? new_joins; // Обрабатывается после Import

        public DiagramItem[]? Import(string type, object? content = null) {
            string name = type switch {
                "JSON" => "Export.json",
                "XML" => "Export.xml",
                "YAML" => "Export.yaml",
                _ => throw new Exception("ЧЁ?!"),
            };
            if (content == null) {
                if (!File.Exists("../../../" + name)) { Log.Write(name + " не обнаружен"); return null; }

                var data = File.ReadAllText("../../../" + name);
                // Log.Write("data: " + (type == "XML" ? Utils.Xml2json(data) : data));

                content = type switch {
                    "JSON" => Utils.Json2obj(data),
                    "XML" => Utils.Xml2obj(data),
                    "YAML" => Utils.Yaml2obj(data),
                    _ => throw new Exception("ЧЁ?!"),
                };
            }
            // Log.Write("data: " + Utils.Obj2json(content));

            if (content is not Dictionary<string, object> @dict) { Log.Write("В начале " + name + " не словарь"); return null; }
            if (!@dict.TryGetValue("items", out var value)) { Log.Write("В корне необнаружен items"); return null; }
            if (value is not List<object> @itemz) { Log.Write("items не того типа"); return null; }
            if (!@dict.TryGetValue("joins", out var value2)) { Log.Write("В корне необнаружен joins"); return null; }
            if (value2 is not List<object> @joins) { Log.Write("joins не того типа"); return null; }

            if (items.Count > 0) {
                var canv = (Canvas) (items[0].Parent ?? throw new Exception("Чё?!"));
                foreach (var child in canv.Children.Cast<Control>().ToList()) {
                    var tag = (string?) child.Tag;
                    if (tag != "marker" && tag != "marker2") canv.Children.Remove(child);
                }
                items.Clear();
            }

            var empty = Array.Empty<MeasuredText>();
            foreach (var obj in itemz) {
                if (obj is not List<object> @item) { Log.Write("Один из элементов не того типа"); continue; }
                if (@item.Count != 5 ||
                    @item[1] is not int @x || @item[2] is not int @y ||
                    @item[3] is not int @w || @item[4] is not int @h) { Log.Write("Содержимое списка элемента ошибочно"); continue; }

                var newy = new DiagramItem(empty, empty, empty, item[0]);
                newy.Move(new(@x, @y), false);
                newy.Resize(@w, @h);
                items.Add(newy);
            }
            var items_arr = items.ToArray();

            var joinz = new List<JoinedItems>();
            foreach (var obj in @joins) {
                if (obj is not List<object> @join) { Log.Write("Одно из соединений не того типа"); continue; }
                if (@join.Count != 7 ||
                    @join[0] is not int @num_a || @join[1] is not int @bord_a || @join[2].ToDouble() is not double @d_a ||
                    @join[3] is not int @num_b || @join[4] is not int @bord_b || @join[5].ToDouble() is not double @d_b ||
                    @join[6] is not int @t) { Log.Write("Содержимое списка соединения ошибочно"); continue; }

                var newy = new JoinedItems(new(items_arr[@num_a], 0, 0, 0, @bord_a, @d_a), new(items_arr[@num_b], 0, 0, 0, @bord_b, @d_b));
                newy.line.Type = @t;
                joinz.Add(newy);
            }

            new_joins = joinz.ToArray();
            return items_arr;
        }

        public void DragOver(object? sender, DragEventArgs e) {
            // Log.Write("DragOver " + e.DragEffects);
            // Only allow Copy or Link as Drop Operations.
            e.DragEffects &= DragDropEffects.Copy | DragDropEffects.Link;

            // Only allow if the dragged data contains text or filenames.
            if (!e.Data.Contains(DataFormats.Text) && !e.Data.Contains(DataFormats.FileNames)) e.DragEffects = DragDropEffects.None;
        }

        private DiagramItem[]? GrandImport(string data) {
            object? content = null;

            try { content = Utils.Json2obj(data); } catch { }
            if (content != null) return Import("JSON", content);

            try { content = Utils.Xml2obj(data); } catch { }
            if (content != null) return Import("XML", content);

            Log.Write("Не получилось разпознать тип данных :/ Нужен JSON, либо XML, либо YAML");
            return null;
        }

        public DiagramItem[]? Drop(object? sender, DragEventArgs e) {
            // Log.Write("Drop");
            if (e.Data.Contains(DataFormats.Text)) {
                var data = e.Data.GetText();
                if (data != null) return GrandImport(data);
            }

            if (e.Data.Contains(DataFormats.FileNames)) {
                var list = e.Data.GetFileNames();
                if (list == null) return null;

                var files = list.ToArray();
                if (files.Length == 0) return null;

                return GrandImport(File.ReadAllText(files[0]));
            }

            return null;
        }
    }
}
