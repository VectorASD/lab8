using Avalonia.Controls;
using DiagramEditor.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Rectangle = Avalonia.Controls.Shapes.Rectangle;
using Brushes = Avalonia.Media.Brushes;

namespace DiagramEditor.Models {
    public class MeasuredText {
        static readonly int max_size = 64;
        static readonly Font[] fonts;
        static readonly Graphics graph = Graphics.FromHwndInternal(IntPtr.Zero /*screen*/);
        static MeasuredText() {
            // SystemFonts.MessageBoxFont.Name -> Segoe UI
            List<Font> arr = new();
            for (int i = 0; i <= max_size; i++) arr.Add(new("", i == 0 ? 1 : i));
            fonts = arr.ToArray();
        }



        readonly string text;
        readonly SizeF[] measures;
        public string Text { get => text; }

        public MeasuredText(string text) {
            this.text = text;
            measures = fonts.Select(x => graph.MeasureString(text, x)).ToArray();
        }

        public void Print() {
            for (int i = 0; i <= max_size; i++)
                Log.Write(i + ".) " + float.Round(measures[i].Width, 3) + "x" + float.Round(measures[i].Height, 3));
        }

        public double Find(double max_width) {
            int L = 0, R = max_size;
            int limit = 10;
            while (L < R) {
                if (limit == 0) break;
                limit -= 1;
                int M = (L + R) / 2;
                double w = measures[M].Width;
                // Log.Write("LMR: " + L + " " + M + " " + R + " | " + w + " " + max_width);
                if (w < max_width) L = M + 1;
                else R = M;
            }
            return (L - 1.2) * 1.35; // Подкруточка (вроде бы идеально)
        }



        public static void Test(Canvas canv) {
            var m = new MeasuredText("Некоторый текст текст");
            m.Print();
            for (int i = 0; i < 20; i++) {
                int w = 100 + i * 1;
                var r = new Rectangle() { Width = w, Height = 30, StrokeThickness = 2, Margin = new(300, i * 40, 0, 0), Fill = Brushes.Yellow, Stroke = Brushes.Blue };
                canv.Children.Add(r);
                var f = m.Find(w);
                var t = new TextBlock() { Text = m.Text, FontSize = f, Margin = new(300, i * 40, 0, 0) };
                canv.Children.Add(t);
            }
        }
    }
}
