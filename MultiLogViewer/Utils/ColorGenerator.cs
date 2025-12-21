using System;
using System.Windows.Media;

namespace MultiLogViewer.Utils
{
    public static class ColorGenerator
    {
        public static SolidColorBrush GenerateFromString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Brushes.Transparent;
            }

            int hash = input.GetHashCode();
            // Hue: 0-360
            double h = Math.Abs(hash) % 360;
            // Saturation: 65-85% (適度に鮮やか)
            double s = 0.65 + (((Math.Abs(hash) / 360) % 20) / 100.0);
            // Lightness: 80-90% (明るいパステルカラー)
            double l = 0.80 + (((Math.Abs(hash) / 100) % 15) / 100.0);

            var color = HslToRgb(h, s, l);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static SolidColorBrush GetForegroundForBackground(SolidColorBrush background)
        {
            if (background == null || background.Color.A == 0) return Brushes.Black;

            var color = background.Color;
            // 輝度計算 (Luma)
            // Y = 0.2126 R + 0.7152 G + 0.0722 B
            double luma = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255.0;

            // 輝度が高い（明るい）場合は黒文字、低い（暗い）場合は白文字
            return luma > 0.6 ? Brushes.Black : Brushes.White;
        }

        private static Color HslToRgb(double h, double s, double l)
        {
            double r, g, b;

            if (s == 0)
            {
                r = g = b = l; // achromatic
            }
            else
            {
                var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                var p = 2 * l - q;
                r = HueToRgb(p, q, h / 360.0 + 1.0 / 3.0);
                g = HueToRgb(p, q, h / 360.0);
                b = HueToRgb(p, q, h / 360.0 - 1.0 / 3.0);
            }

            return Color.FromRgb(
                (byte)(r * 255),
                (byte)(g * 255),
                (byte)(b * 255));
        }

        private static double HueToRgb(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
            if (t < 1.0 / 2.0) return q;
            if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
            return p;
        }
    }
}
