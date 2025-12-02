using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ImasClipManager.Models;

namespace ImasClipManager.Helpers
{
    public static class SearchQueryParser
    {
        // クエリを解析してフィルタ関数を返す
        public static Func<Clip, bool> Parse(string query, ViewModels.SettingsViewModel? settings)
        {
            if (string.IsNullOrWhiteSpace(query)) return _ => true;

            var tokens = Tokenize(query);
            var predicates = new List<Func<Clip, bool>>();

            foreach (var token in tokens)
            {
                var p = ParseToken(token, settings);
                if (p != null) predicates.Add(p);
            }

            // トップレベルは AND 結合
            return clip => predicates.All(p => p(clip));
        }

        private static List<string> Tokenize(string input)
        {
            var tokens = new List<string>();
            // 正規表現でトークン分割
            // 1. キー付きグループ: -?key:(...) または ?key:(...)
            // 2. キー付きクォート: -?key:"..." または ?key:"..."
            // 3. キー付き通常値:   -?key:value または ?key:value
            // 4. キーなしグループ: (...)  -> (-A OR B) など
            // 5. 除外クォート:     -"..."
            // 6. 除外通常値:       -value
            // 7. 通常クォート:     "..."
            // 8. 通常値:           value
            var pattern = @"(-?\?[a-zA-Z0-9_]+:\(.*?\))|(-?\?[a-zA-Z0-9_]+:""(.*?)"")|(-?\?[a-zA-Z0-9_]+:[^ ]+)|(\(.*?\))|(-"".*?"")|(-[^ ]+)|("".*?"")|([^ ]+)";

            foreach (Match m in Regex.Matches(input, pattern))
            {
                if (!string.IsNullOrWhiteSpace(m.Value))
                {
                    tokens.Add(m.Value);
                }
            }
            return tokens;
        }

        private static Func<Clip, bool>? ParseToken(string token, ViewModels.SettingsViewModel? settings)
        {
            bool isNegated = false;

            // 1. 否定チェック (-から始まる場合)
            if (token.StartsWith("-"))
            {
                isNegated = true;
                token = token.Substring(1); // 先頭の - を除去
            }

            Func<Clip, bool>? predicate = null;

            // 2. キー指定チェック (?key:...)
            if (token.StartsWith("?"))
            {
                var parts = token.Split(new[] { ':' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].ToLower();
                    var value = parts[1];

                    // グループ (A OR B)
                    if (value.StartsWith("(") && value.EndsWith(")"))
                    {
                        var content = value.Substring(1, value.Length - 2);
                        predicate = ParseGroup(key, content, settings);
                    }
                    else
                    {
                        value = value.Trim('"');
                        predicate = GetFieldPredicate(key, value);
                    }
                }
            }
            // 3. キーなしグループ ((A OR B)) -> グローバル検索でのOR
            else if (token.StartsWith("(") && token.EndsWith(")"))
            {
                var content = token.Substring(1, token.Length - 2);
                predicate = ParseGroup(null, content, settings);
            }
            // 4. 通常キーワード
            else
            {
                var val = token.Trim('"');
                predicate = clip => MatchGlobal(clip, val, settings);
            }

            if (predicate == null) return null;
            return isNegated ? c => !predicate(c) : predicate;
        }

        // グループ解析: (A OR -B OR "C D")
        private static Func<Clip, bool> ParseGroup(string? key, string content, ViewModels.SettingsViewModel? settings)
        {
            var parts = content.Split(new[] { " OR " }, StringSplitOptions.RemoveEmptyEntries);
            var groupPredicates = new List<Func<Clip, bool>>();

            foreach (var part in parts)
            {
                var term = part.Trim();
                bool isTermNegated = false;

                if (term.StartsWith("-"))
                {
                    isTermNegated = true;
                    term = term.Substring(1);
                }

                term = term.Trim('"');

                Func<Clip, bool> p;
                if (key != null)
                {
                    // キー指定あり: ?brands:(-765)
                    p = GetFieldPredicate(key, term);
                }
                else
                {
                    // キー指定なし: (-xxx OR yyy) -> グローバル検索
                    p = clip => MatchGlobal(clip, term, settings);
                }

                if (isTermNegated) groupPredicates.Add(c => !p(c));
                else groupPredicates.Add(p);
            }

            // OR結合
            return clip => groupPredicates.Any(p => p(clip));
        }

        private static Func<Clip, bool> GetFieldPredicate(string key, string value)
        {
            var comp = StringComparison.OrdinalIgnoreCase;

            switch (key)
            {
                case "?path": return c => c.FilePath?.Contains(value, comp) ?? false;
                case "?clip": return c => c.ClipName?.Contains(value, comp) ?? false;
                case "?song": return c => c.SongTitle?.Contains(value, comp) ?? false;
                case "?concert": return c => c.ConcertName?.Contains(value, comp) ?? false;
                case "?lyrics": return c => c.Lyrics?.Contains(value, comp) ?? false;
                case "?remarks": return c => c.Remarks?.Contains(value, comp) ?? false;

                case "?type":
                    return c => c.LiveType.ToDisplayString().Contains(value, comp);

                case "?brands":
                case "?brand":
                    return c => c.Brands.ToDisplayString().Contains(value, comp);

                case "?performers":
                case "?performer":
                    return c => c.Performers.Any(p => p.Name.Contains(value, comp) || p.Yomi.Contains(value, comp));

                case "?duration": return ParseRange(value, c => c.DurationMs / 1000.0);
                case "?date": return ParseDateRange(value, c => c.ConcertDate);
                case "?created": return ParseDateRange(value, c => c.CreatedAt);
                case "?updated": return ParseDateRange(value, c => c.UpdatedAt);

                default: return _ => false;
            }
        }

        // --- 修正された範囲判定ロジック ---

        // 数値範囲 (min-max)
        private static Func<Clip, bool> ParseRange(string rangeStr, Func<Clip, double> valueSelector)
        {
            // ハイフンがない場合は「完全一致（近似値）」
            if (!rangeStr.Contains("-"))
            {
                if (double.TryParse(rangeStr, out double exactVal))
                {
                    // 浮動小数点誤差を許容 (±0.5秒以内など)
                    return c => Math.Abs(valueSelector(c) - exactVal) < 0.5;
                }
                return _ => false;
            }

            var parts = rangeStr.Split('-');
            double? min = null, max = null;

            // "100-" -> parts[0]="100", parts[1]=""
            // "-100" -> parts[0]="", parts[1]="100"
            if (!string.IsNullOrEmpty(parts[0]) && double.TryParse(parts[0], out double v1)) min = v1;
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) && double.TryParse(parts[1], out double v2)) max = v2;

            return c =>
            {
                var val = valueSelector(c);
                if (min.HasValue && val < min.Value) return false;
                if (max.HasValue && val > max.Value) return false;
                return true;
            };
        }

        // 日付範囲 (yyyy/MM/dd-yyyy/MM/dd)
        private static Func<Clip, bool> ParseDateRange(string rangeStr, Func<Clip, DateTime?> dateSelector)
        {
            // ハイフンがない場合は「その日（1日分）」
            if (!rangeStr.Contains("-"))
            {
                if (DateTime.TryParse(rangeStr, out DateTime exactDate))
                {
                    var start = exactDate.Date;
                    var end = exactDate.Date.AddDays(1).AddTicks(-1);
                    return c =>
                    {
                        var val = dateSelector(c);
                        if (!val.HasValue) return false;
                        return val.Value >= start && val.Value <= end;
                    };
                }
                return _ => false;
            }

            var parts = rangeStr.Split('-');
            DateTime? min = null, max = null;

            if (!string.IsNullOrEmpty(parts[0]) && DateTime.TryParse(parts[0], out DateTime d1)) min = d1;
            if (parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) && DateTime.TryParse(parts[1], out DateTime d2))
                max = d2.AddDays(1).AddTicks(-1);

            return c =>
            {
                var val = dateSelector(c);
                if (!val.HasValue) return false;
                if (min.HasValue && val.Value < min.Value) return false;
                if (max.HasValue && val.Value > max.Value) return false;
                return true;
            };
        }

        // グローバル検索 (設定依存)
        private static bool MatchGlobal(Clip clip, string keyword, ViewModels.SettingsViewModel? settings)
        {
            var comp = StringComparison.OrdinalIgnoreCase;
            bool match = false;

            // Settingsがnull、または各フラグがtrueの場合に検索
            if (settings == null || settings.Search_FilePath) if (clip.FilePath?.Contains(keyword, comp) ?? false) match = true;
            if (!match && (settings == null || settings.Search_ClipName)) if (clip.ClipName?.Contains(keyword, comp) ?? false) match = true;
            if (!match && (settings == null || settings.Search_SongTitle)) if (clip.SongTitle?.Contains(keyword, comp) ?? false) match = true;
            if (!match && (settings == null || settings.Search_ConcertName)) if (clip.ConcertName?.Contains(keyword, comp) ?? false) match = true;
            if (!match && (settings == null || settings.Search_LiveType)) if (clip.LiveType.ToDisplayString().Contains(keyword, comp)) match = true;
            if (!match && (settings == null || settings.Search_Brands)) if (clip.Brands.ToDisplayString().Contains(keyword, comp)) match = true;
            if (!match && (settings == null || settings.Search_Lyrics)) if (clip.Lyrics?.Contains(keyword, comp) ?? false) match = true;
            if (!match && (settings == null || settings.Search_Remarks)) if (clip.Remarks?.Contains(keyword, comp) ?? false) match = true;

            if (!match && (settings == null || settings.Search_Performers))
            {
                if (clip.Performers.Any(p => p.Name.Contains(keyword, comp) || p.Yomi.Contains(keyword, comp))) match = true;
            }

            return match;
        }
    }
}