using System;
using System.Globalization;

namespace ImasClipManager.Helpers
{
    public static class TimeHelper
    {
        public static bool TryParseTime(string input, out long resultMs)
        {
            resultMs = 0;
            if (string.IsNullOrWhiteSpace(input)) return true; // 空文字は0扱い（成功）

            // 1. 数値のみの場合 (秒として扱う) -> "30" = 30秒
            if (double.TryParse(input, out double seconds))
            {
                resultMs = (long)(seconds * 1000);
                return true;
            }

            // 2. コロン区切りの処理
            var parts = input.Split(':');

            // mm:ss 形式
            if (parts.Length == 2)
            {
                if (double.TryParse(parts[0], out double mm) && double.TryParse(parts[1], out double ss))
                {
                    resultMs = (long)(TimeSpan.FromMinutes(mm) + TimeSpan.FromSeconds(ss)).TotalMilliseconds;
                    return true;
                }
            }
            // hh:mm:ss 形式
            else if (parts.Length == 3)
            {
                if (double.TryParse(parts[0], out double hh) &&
                    double.TryParse(parts[1], out double mm) &&
                    double.TryParse(parts[2], out double ss))
                {
                    resultMs = (long)(TimeSpan.FromHours(hh) + TimeSpan.FromMinutes(mm) + TimeSpan.FromSeconds(ss)).TotalMilliseconds;
                    return true;
                }
            }
            return false;
        }

        // ミリ秒を "hh:mm:ss" または "mm:ss" 形式に変換
        public static string FormatDuration(long durationMs)
        {
            if (durationMs < 0) durationMs = 0;
            var ts = TimeSpan.FromMilliseconds(durationMs);

            if (ts.TotalHours >= 1)
            {
                return ts.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return ts.ToString(@"mm\:ss");
            }
        }
    }
}