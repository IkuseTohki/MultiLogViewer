using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using MultiLogViewer.Models;
using MultiLogViewer.Utils;
using System.Text.RegularExpressions;
using System.Windows;

namespace MultiLogViewer.ViewModels.Converters
{
    public class CellStyleConverter : IValueConverter
    {
        public ColumnStyleConfig? StyleConfig { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string param = parameter as string ?? "Background"; // "Background" or "Foreground" or "FontWeight"

            // デフォルト値
            object defaultValue = param switch
            {
                "Background" => Brushes.Transparent,
                "Foreground" => Brushes.Black, // テーマによっては要調整だが、一旦黒
                "FontWeight" => FontWeights.Normal,
                _ => null!
            };

            if (StyleConfig == null || value == null)
            {
                return defaultValue;
            }

            string text = value.ToString() ?? string.Empty;

            // 1. ルールベースの評価
            if (StyleConfig.Rules != null)
            {
                foreach (var rule in StyleConfig.Rules)
                {
                    if (!string.IsNullOrEmpty(rule.Pattern) && Regex.IsMatch(text, rule.Pattern))
                    {
                        if (param == "Background" && !string.IsNullOrEmpty(rule.Background))
                        {
                            try { return new BrushConverter().ConvertFromString(rule.Background) ?? defaultValue; } catch { }
                        }
                        else if (param == "Foreground" && !string.IsNullOrEmpty(rule.Foreground))
                        {
                            try { return new BrushConverter().ConvertFromString(rule.Foreground) ?? defaultValue; } catch { }
                        }
                        else if (param == "FontWeight" && !string.IsNullOrEmpty(rule.FontWeight))
                        {
                            if (rule.FontWeight.Equals("Bold", StringComparison.OrdinalIgnoreCase)) return FontWeights.Bold;
                            // 必要なら他のWeightも追加
                        }

                        // マッチしたらループを抜ける（最初のルール優先）
                        // ただし、Backgroundだけ設定されていてForegroundが設定されていない場合などはどうするか？
                        // 仕様では「マッチしたら適用」なので、ここで決めるのが単純。
                        // 部分的な適用（背景は赤、文字はデフォルトなど）を許容するため、
                        // ここで return するロジックだと、「背景だけマッチしたルール」でリターンすると、
                        // 後続のルールの「文字色」が評価されなくなる。
                        // 今回の仕様では「マッチしたルール」が全てを決定する（＝CSS的なカスケードはしない）で良いはず。
                        // プロパティが指定されていなければデフォルトを返す必要がある。

                        // ここで return してしまうと、例えば Background だけ指定されたルールにマッチした場合、
                        // param="Foreground" で呼ばれたときにもここで止まり、if文に入らずループを抜け、
                        // 下のセマンティックカラーリングに行ってしまうか、ループを抜けてデフォルトを返すか。

                        // 正しいロジック:
                        // ルールにマッチしたら、そのルールで完結する。
                        // そのルールにプロパティ設定があればそれを返し、なければデフォルトを返す（セマンティックには行かない）。

                        if (param == "Background")
                        {
                            if (!string.IsNullOrEmpty(rule.Background))
                                try { return new BrushConverter().ConvertFromString(rule.Background) ?? defaultValue; } catch { }
                            return defaultValue;
                        }
                        else if (param == "Foreground")
                        {
                            if (!string.IsNullOrEmpty(rule.Foreground))
                                try { return new BrushConverter().ConvertFromString(rule.Foreground) ?? defaultValue; } catch { }
                            return defaultValue;
                        }
                        else if (param == "FontWeight")
                        {
                            if (!string.IsNullOrEmpty(rule.FontWeight))
                                if (rule.FontWeight.Equals("Bold", StringComparison.OrdinalIgnoreCase)) return FontWeights.Bold;
                            return defaultValue;
                        }
                    }
                }
            }

            // 2. セマンティック・カラーリングの評価
            if (StyleConfig.SemanticColoring)
            {
                if (param == "Background")
                {
                    return ColorGenerator.GenerateFromString(text);
                }
                else if (param == "Foreground")
                {
                    // 背景色に応じた文字色を計算
                    var bgBrush = ColorGenerator.GenerateFromString(text);
                    return ColorGenerator.GetForegroundForBackground(bgBrush);
                }
            }

            return defaultValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
