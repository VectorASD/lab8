using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.LogicalTree;
using DiagramEditor.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiagramEditor.Views {
    class Distantor: IComparable {
        readonly int dist;
        public readonly Point p;

        public Distantor(double dist, double x, double y) {
            this.dist = (int) dist;
            p = new Point(x, y);
        }

        public int CompareTo(object? obj) {
            if (obj is not Distantor @R) throw new ArgumentException("Ожидался Distantor", nameof(obj));
            return dist - @R.dist;
        }
    }

    public partial class DiagramItem: UserControl {
        readonly Rectangle[] borders;
        readonly StackPanel[] panels;
        MeasuredText[] head, attrs, meths;
        
        public DiagramItem(): this(Array.Empty<MeasuredText>(), Array.Empty<MeasuredText>(), Array.Empty<MeasuredText>()) {} // Чтобы дизайнер не глючил
        
        public DiagramItem(MeasuredText[] head, MeasuredText[] attrs, MeasuredText[] meths) {
            this.head = head; this.attrs = attrs; this.meths = meths;

            InitializeComponent();
            DataContext = this;

            List<Rectangle> rects = new();
            foreach (var ch in LogicalChildren[0].LogicalChildren)
                if (ch is Rectangle @rect) rects.Add(@rect);
            borders = rects.ToArray();
            // Log.Write("Len: " + borders.Length); 4? Всё нормально

            var grid = this.Find<Grid>("sp_grid") ?? throw new Exception("Чё!?"); // Можно, конечно, указать полный логический путь, но из-за его большой длины это уже небезопасно ;'-}
            List<StackPanel> sps = new();
            foreach (var ch in grid.GetLogicalChildren())
                if (ch is StackPanel @sp) sps.Add(@sp);
            panels = sps.ToArray();
            // Log.Write("Len: " + panels.Length); 3? Всё оки

            foreach (var item in head) panels[0].Children.Add(new TextBlock() { Text = item.Text, Tag = "item" });
            foreach (var item in attrs) panels[1].Children.Add(new TextBlock() { Text = item.Text, Tag = "item" });
            foreach (var item in meths) panels[2].Children.Add(new TextBlock() { Text = item.Text, Tag = "item" });

            RecalcSizes();
        }

        public void RecalcSizes() { // Максимально-сверх-скоростной перерассчитыватель размеров шрифта текста
            double w = Width - 10, h = Height - 10, sum_h = 0;
            List<double> sizes_A = new(), sizes_B = new(), sizes_C = new();

            foreach (var item in head) {
                var m = item.Find(w);
                sizes_A.Add(m.Size);
                sum_h += m.Height;
            }
            foreach (var item in attrs) {
                var m = item.Find(w);
                sizes_B.Add(m.Size);
                sum_h += m.Height;
            }
            foreach (var item in meths) {
                var m = item.Find(w);
                sizes_C.Add(m.Size);
                sum_h += m.Height;
            }

            double mul = sum_h > h ? h / sum_h : 1; // Подавляет размеры всех TextBlock, если они не влезают по высоте... Конечно можно было прикрутить более умный механизм с пирамидальной динамической сортировкой всех элементов по высоте и уменьшению самых высоких элементов, пока sum_h не упадёт до h, но это долго+лень делать :D

            for (int i = 0; i < head.Length; i++) ((TextBlock) panels[0].Children[i]).FontSize = (sizes_A[i] * mul).Normalize(8, 32);
            for (int i = 0; i < attrs.Length; i++) ((TextBlock) panels[1].Children[i]).FontSize = (sizes_B[i] * mul).Normalize(8, 32);
            for (int i = 0; i < meths.Length; i++) ((TextBlock) panels[2].Children[i]).FontSize = (sizes_C[i] * mul).Normalize(8, 32);
        }

        public void Resize(double width, double height) {
            Width = width;
            Height = height;
            borders[0].Width = borders[3].Width = width;
            borders[1].Height = borders[2].Height = height;
            var border = (Border) LogicalChildren[0].LogicalChildren[0];
            border.Width = width - 8;
            border.Height = height - 8;
            RecalcSizes();
        }

        private Distantor GetDist(int num, Point pos) {
            var b = borders[num];
            double w = b.Width / 2, h = b.Height / 2;
            double X = Margin.Left + (num == 2 ? Width - w : w), Y = Margin.Top + (num == 3 ? Height - h : h);
            if (w > h) {
                double x_left = Margin.Left + h, x_right = Margin.Left + b.Width - h;
                return new Distantor(Math.Abs(Y - pos.Y), pos.X.Normalize(x_left, x_right), Y);
            }
            double y_top = Margin.Top + w, y_bottom = Margin.Top + b.Height - w;
            return new Distantor(Math.Abs(X - pos.X), X, pos.Y.Normalize(y_top, y_bottom));
        }
        public Point GetPos(Point pos) {
            var dists = new[] { GetDist(0, pos), GetDist(1, pos), GetDist(2, pos), GetDist(3, pos) };
            var min = dists.Min() ?? throw new Exception("Чё!?");
            return min.p; // Вообще это всё вмещается в одну строку, но мы же не в питоне...)
        }
    }
}
