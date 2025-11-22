using System;
using System.Collections.Generic;
using System.Linq;
using ImasClipManager.Models;

namespace ImasClipManager.Helpers
{
    public static class EnumExtensions
    {
        public static string ToDisplayString(this LiveType type)
        {
            return type switch
            {
                LiveType.Seiyuu => "声優ライブ",
                LiveType.MR => "MRライブ",
                LiveType.Other => "その他",
                _ => type.ToString(),
            };
        }

        public static string ToDisplayString(this BrandType brand)
        {
            // Noneの場合は空文字
            if (brand == BrandType.None) return "";

            var names = new List<string>();

            // フラグ判定（BrandTypeは[Flags]なので複数選択があり得る）
            if (brand.HasFlag(BrandType.Original)) names.Add("765PRO AS");
            if (brand.HasFlag(BrandType.DS)) names.Add("ディアリースターズ");
            if (brand.HasFlag(BrandType.Cinderella)) names.Add("シンデレラガールズ");
            if (brand.HasFlag(BrandType.Million)) names.Add("ミリオンライブ！");
            if (brand.HasFlag(BrandType.SideM)) names.Add("SideM");
            if (brand.HasFlag(BrandType.Shiny)) names.Add("シャイニーカラーズ");
            if (brand.HasFlag(BrandType.Valiv)) names.Add("ヴイアライヴ");
            if (brand.HasFlag(BrandType.Gakuen)) names.Add("学園アイドルマスター");
            if (brand.HasFlag(BrandType.Goudou)) names.Add("合同ライブ");
            if (brand.HasFlag(BrandType.Other)) names.Add("その他");

            return string.Join(", ", names);
        }
    }
}