using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia;
using Avalonia.Media;
using System.Text.Json;
using DiagramEditor.ViewModels;
using System.Collections;
using System.Diagnostics;

namespace DiagramEditor.Models {
    public static class Utils {
        // By VectorASD         Всё это моих рук дела! ;'-}

        /*
         * Base64 абилка
         */

        public static string Base64Encode(string plainText) {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
        public static string Base64Decode(string base64EncodedData) {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }

        /*
         * JSON абилка
         */

        public static string JsonEscape(string str) {
            StringBuilder sb = new();
            foreach (char i in str) {
                sb.Append(i switch {
                    '"' => "\\\"",
                    '\\' => "\\\\",
                    '$' => "{$", // Чисто по моей части ;'-}
                     _ => i
                });
            }
            return sb.ToString();
        }
        public static string Obj2json(object? obj) { // Велосипед ради поддержки своей сериализации классов по типу Point, SolidColorBrush и т.д.
            switch (obj) {
            case null: return "null";
            case string @str: return '"' + JsonEscape(str) + '"';
            case bool @bool: return @bool ? "true" : "false";
            case short @short: return @short.ToString();
            case int @int: return @int.ToString();
            case long @long: return @long.ToString();
            case float @float: return @float.ToString();
            case double @double: return @double.ToString();

            case Point @point: return "\"$p$" + (int) @point.X + "," + (int) @point.Y + '"';
            case Points @points: return "\"$P$" + string.Join("|", @points.Select(p => (int) p.X + "," + (int) p.Y)) + '"';
            case SolidColorBrush @color: return "\"$C$" + @color.Color + '"';
            case Thickness @thickness: return "\"$T$" + @thickness.Left + "," + @thickness.Top + "," + @thickness.Right + "," + @thickness.Bottom + '"';

            case Dictionary<string, object?> @dict: {
                StringBuilder sb = new();
                sb.Append('{');
                foreach (var entry in @dict) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(Obj2json(entry.Key));
                    sb.Append(": ");
                    sb.Append(Obj2json(entry.Value));
                }
                sb.Append('}');
                return sb.ToString();
            }
            case IEnumerable @list: {
                StringBuilder sb = new();
                sb.Append('[');
                foreach (object? item in @list) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(Obj2json(item));
                }
                sb.Append(']');
                return sb.ToString();
            }
            default: return "(" + obj.GetType() + " ???)";
            }
        }

        private static object JsonHandler(string str) {
            if (str.Length < 3 || str[0] != '$' || str[2] != '$') return str.Replace("{$", "$");
            string data = str[3..];
            string[] thick = str[1] == 'T' ? data.Split(',') : System.Array.Empty<string>();
            return str[1] switch {
                // 'p' => new SafePoint(data).Point,
                // 'P' => new SafePoints(data.Replace('|', ' ')).Points,
                'C' => new SolidColorBrush(Color.Parse(data)),
                'T' => new Thickness(double.Parse(thick[0]), double.Parse(thick[1]), double.Parse(thick[2]), double.Parse(thick[3])),
                _ => str,
            };
        }
        private static object? JsonHandler(object? obj) {
            if (obj == null) return null;

            if (obj is List<object?> @list) return @list.Select(JsonHandler).ToList();
            if (obj is Dictionary<string, object?> @dict) return @dict.Select(pair => new KeyValuePair<string, object?>(pair.Key, JsonHandler(pair.Value)));
            if (obj is JsonElement @item) {
                switch (@item.ValueKind) {
                case JsonValueKind.Undefined: return null;
                case JsonValueKind.Object:
                    Dictionary<string, object?> res = new();
                    foreach (var el in @item.EnumerateObject()) res[el.Name] = JsonHandler(el.Value);
                    return res;
                case JsonValueKind.Array: // Неиспытано ещё
                    List<string> res2 = new();
                    foreach (var el in @item.EnumerateObject()) _ = res2.Append(JsonHandler(el.Value));
                    return res2;
                case JsonValueKind.String:
                    var s = JsonHandler(@item.GetString() ?? "");
                    // Log.Write("JS: '" + @item.GetString() + "' -> '" + s + "'");
                    return s;
                case JsonValueKind.Number:
                    if (@item.ToString().Contains('.')) return @item.GetDouble();
                    // Иначе это целое число
                    long  a = @item.GetInt64();
                    int   b = @item.GetInt32();
                    short c = @item.GetInt16();
                    if (a != b) return a;
                    if (b != c) return b;
                    return c;
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Null: return null;
                }
            }
            Log.Write("JT: " + obj.GetType());

            return obj;
        }
        public static object? Json2obj(string json) {
            json = json.Trim();
            if (json.Length == 0) return null;

            object? data;
            if (json[0] == '[') data = JsonSerializer.Deserialize<List<object?>>(json);
            else if (json[0] == '{') data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            else return null;

            return JsonHandler(data);
        }

        /*
         * XML абилка
         */

        public static string XMLEscape(string str) {
            StringBuilder sb = new();
            foreach (char i in str) {
                sb.Append(i switch {
                    '"' => "&quot;",
                    '\'' => "&apos;",
                    '>' => "&gt;",
                    '<' => "&lt;",
                    '&' => "&amp;",
                    _ => i
                });
            }
            return sb.ToString();
        }

        private static bool IsComposite(object? obj) {
            if (obj == null) return false;
            if (obj is List<object?> || obj is Dictionary<string, object?> || obj is not JsonElement @item) return true;
            var T = @item.ValueKind;
            return T == JsonValueKind.Object || T == JsonValueKind.Array;
        }
        private static string Dict2XML(Dictionary<string, object?> dict, string level) {
            StringBuilder attrs = new();
            StringBuilder items = new();
            foreach (var entry in dict)
                if (IsComposite(entry.Value))
                    items.Append(level + "\t<" + entry.Key + ">" + ToXMLHandler(entry.Value, level + "\t\t") + level + "\t</" + entry.Key + ">");
                else attrs.Append(" " + entry.Key + "=\"" + ToXMLHandler(entry.Value, "{err}") + "\"");

            if (items.Length == 0) return level + "<Dict" + attrs.ToString() + "/>";
            return level + "<Dict" + attrs.ToString() + ">" + items.ToString() + level + "</Dict>";
        }
        private static string List2XML(List<object?> list, string level) {
            StringBuilder attrs = new();
            StringBuilder items = new();
            foreach (var entry in list)
                if (IsComposite(entry)) items.Append(ToXMLHandler(entry, level + "\t"));
                else attrs.Append(" " + ToXMLHandler(entry, "{err}") + "=''");

            if (items.Length == 0) return level + "<List" + attrs.ToString() + "/>";
            return level + "<List" + attrs.ToString() + ">" + items.ToString() + level + "</List>";
        }

        private static string ToXMLHandler(object? obj, string level) {
            if (obj == null) return "null";

            if (obj is List<object?> @list) return List2XML(@list, level);
            if (obj is Dictionary<string, object?> @dict) return Dict2XML(@dict, level);
            if (obj is JsonElement @item) {
                switch (@item.ValueKind) {
                case JsonValueKind.Undefined: return "undefined";
                case JsonValueKind.Object:
                    return Dict2XML(new Dictionary<string, object?>(@item.EnumerateObject().Select(pair => new KeyValuePair<string, object?>(pair.Name, pair.Value))), level);
                case JsonValueKind.Array: // Неиспытано ещё
                    return List2XML(@item.EnumerateObject().Select(item => (object?) item.Value).ToList(), level);
                case JsonValueKind.String:
                    var s = XMLEscape(@item.GetString() ?? "null");
                    // Log.Write("XS: '" + @item.GetString() + "' -> '" + s + "'");
                    return s;
                case JsonValueKind.Number: return "$" + @item.ToString(); // escape NUM
                case JsonValueKind.True: return "yeah";
                case JsonValueKind.False: return "nop";
                case JsonValueKind.Null: return "null";
                }
            }
            Log.Write("XT: " + obj.GetType());

            return "<UnknowType>" + obj.GetType() + "</UnknowType>";
        }
        public static string? Json2xml(string json) {
            json = json.Trim();
            if (json.Length == 0) return null;

            object? data;
            if (json[0] == '[') data = JsonSerializer.Deserialize<List<object?>>(json);
            else if (json[0] == '{') data = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            else return null;

            return "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + ToXMLHandler(data, "\n");
        }

        private static string ToJSONHandler(string str) {
            if (str.Length > 1 && str[0] == '$' && str[1] <= '9' && str[1] >= '0') return str[1..]; // unescape NUM
            return str switch {
                "null" => "null",
                "undefined" => "undefined",
                "yeah" => "true",
                "false" => "false",
                _ => '"' + str + '"',
            };
        }
        private static string ToJSONHandler(XElement xml) {
            var name = xml.Name.LocalName;
            StringBuilder sb = new();
            if (name == "Dict") {
                sb.Append('{');
                foreach (var attr in xml.Attributes()) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(ToJSONHandler(attr.Name.LocalName));
                    sb.Append(": ");
                    sb.Append(ToJSONHandler(attr.Value));
                }
                foreach (var el in xml.Elements()) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(ToJSONHandler(el.Name.LocalName));
                    sb.Append(": ");
                    sb.Append(ToJSONHandler(el.Elements().ToArray()[0]));
                }
                sb.Append('}');
            } else if (name == "List") {
                sb.Append('[');
                foreach (var attr in xml.Attributes()) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(ToJSONHandler(attr.Name.LocalName));
                }
                foreach (var el in xml.Elements()) {
                    if (sb.Length > 1) sb.Append(", ");
                    sb.Append(ToJSONHandler(el));
                }
                sb.Append(']');
            } else sb.Append("Type??" + name);
            return sb.ToString();
        }
        public static string Xml2json(string xml) => ToJSONHandler(XElement.Parse(xml));

        /*
         * Misc
         */

        public static string? Obj2xml(object? obj) => Json2xml(Obj2json(obj)); // Чёт припомнилось свойство транзитивности с дискретной матеши...
        public static object? Xml2obj(string xml) => Json2obj(Xml2json(xml));

        public static void RenderToFile(Control tar, string path) {
            var target = (Control?) tar.Parent;
            if (target == null) return;

            double w = target.Bounds.Width, h = target.Bounds.Height;
            var pixelSize = new PixelSize((int) w, (int) h);
            var size = new Size(w, h);
            using RenderTargetBitmap bitmap = new(pixelSize);
            target.Measure(size);
            target.Arrange(new Rect(size));
            bitmap.Render(target);
            bitmap.Save(path);
        }

        public static string TrimAll(this string str) { // Помимо пробелов по бокам, убирает повторы пробелов внутри
            StringBuilder sb = new();
            for (int i = 0; i < str.Length; i++) {
                if (i > 0 && str[i] == ' ' && str[i - 1] == ' ') continue;
                sb.Append(str[i]);
            }
            return sb.ToString().Trim();
        }

        // Странно, почему оригинальный Split() работает, как обычный Split(' '),
        // ведь во всех языках (по крайней мере в тех, которые я видел до C#) он
        // игнорирует добавления в ответ пустых строк.
        public static string[] NormSplit(this string str) => str.TrimAll().Split(' ');

        public static string GetStackInfo() {
            var st = new StackTrace();
            var sb = new StringBuilder();
            for (int i = 1; i < 11; i++) {
                var frame = st.GetFrame(i);
                if (frame == null) continue;

                var method = frame.GetMethod();
                if (method == null || method.ReflectedType == null) continue;

                sb.Append(method.ReflectedType.Name + " " + method.Name + " | ");
                if (i == 5) sb.Append("\n    ");
            }
            return sb.ToString();
        }
    }
}
