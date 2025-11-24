// ImasClipManager/Helpers/TimeHelper.cs
using System;

namespace ImasClipManager.Helpers
{
    public static class TimeHelper
    {
        public static bool TryParseTime(string input, out long resultMs)
        {
            resultMs = 0;
            if (string.IsNullOrWhiteSpace(input)) return true; // 空文字は0扱い（成功）

            // mm:ss 形式の独自対応
            var parts = input.Split(':');
            if (parts.Length == 2)
            {
                if (double.TryParse(parts[0], out double mm) && double.TryParse(parts[1], out double ss))
                {
                    resultMs = (long)(TimeSpan.FromMinutes(mm) + TimeSpan.FromSeconds(ss)).TotalMilliseconds;
                    return true;
                }
            }

            // 標準的な TimeSpan パース (hh:mm:ss など)
            if (TimeSpan.TryParse(input, out var ts))
            {
                resultMs = (long)ts.TotalMilliseconds;
                return true;
            }

            return false;
        }
    }
}