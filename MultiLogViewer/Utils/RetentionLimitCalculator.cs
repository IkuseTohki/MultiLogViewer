using MultiLogViewer.Services;
using System;
using System.Text.RegularExpressions;

namespace MultiLogViewer.Utils
{
    /// <summary>
    /// ログ読み込み制限日時の計算を担当するクラス。
    /// </summary>
    public class RetentionLimitCalculator
    {
        private readonly ITimeProvider _timeProvider;

        public RetentionLimitCalculator(ITimeProvider timeProvider)
        {
            _timeProvider = timeProvider;
        }

        /// <summary>
        /// 設定文字列から具体的な開始日時を計算します。
        /// </summary>
        /// <param name="input">設定文字列 (today, -1d, 2023-01-01 など)</param>
        /// <returns>計算された日時。制限なしの場合は null。</returns>
        public DateTime? Calculate(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var trimmedInput = input.Trim().ToLower();

            // 特殊キーワード
            if (trimmedInput == "today" || trimmedInput == "本日")
            {
                return _timeProvider.Today;
            }

            // 相対指定のパース (-Nd, -Nw, -Nm)
            var relativeMatch = Regex.Match(trimmedInput, @"^-(?<value>\d+)(?<unit>d|w|m|日前|週間前|ヶ月前)$");
            if (relativeMatch.Success)
            {
                int value = int.Parse(relativeMatch.Groups["value"].Value);
                string unit = relativeMatch.Groups["unit"].Value;

                return unit switch
                {
                    "d" => _timeProvider.Today.AddDays(-value),
                    "日前" => _timeProvider.Today.AddDays(-value),
                    "w" => _timeProvider.Today.AddDays(-value * 7),
                    "週間前" => _timeProvider.Today.AddDays(-value * 7),
                    "m" => _timeProvider.Today.AddMonths(-value),
                    "ヶ月前" => _timeProvider.Today.AddMonths(-value),
                    _ => _timeProvider.Today
                };
            }

            // 絶対指定のパース
            var absoluteDate = DateTimeParser.TryParse(input);
            if (absoluteDate.HasValue)
            {
                return absoluteDate.Value;
            }

            // 解析できない場合はデフォルト（当日）
            return _timeProvider.Today;
        }
    }
}
