using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
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
        public DiagramItem() {
            InitializeComponent();

            List<Rectangle> rects = new();
            foreach (var ch in LogicalChildren[0].LogicalChildren)
                if (ch is Rectangle @rect) rects.Add(@rect);
            borders = rects.ToArray();
            // Log.Write("Len: " + borders.Length); 4? Всё нормально
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
            var min = dists.Min();
            return min == null ? new() : min.p;
        }
    }
}
