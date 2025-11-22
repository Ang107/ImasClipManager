using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using ImasClipManager.Helpers;
using ImasClipManager.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ImasClipManager.Services
{
    // 1. マッピング定義クラス
    public class PerformerMap : ClassMap<Performer>
    {
        public PerformerMap()
        {
            // IDは自動採番なので出力しない、読み込み時も無視する設定
            Map(m => m.Id).Ignore();

            Map(m => m.Name).Index(0).Name("表示名");
            Map(m => m.Yomi).Index(1).Name("読み");

            // 日本語変換コンバーターを適用
            Map(m => m.LiveType).Index(2).Name("形式").TypeConverter<LiveTypeConverter>();
            Map(m => m.Brand).Index(3).Name("ブランド").TypeConverter<BrandTypeConverter>();
        }
    }

    // 2. 形式(LiveType)用のコンバーター
    public class LiveTypeConverter : DefaultTypeConverter
    {
        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is LiveType type)
            {
                return type.ToDisplayString(); // 拡張メソッドで日本語化
            }
            return base.ConvertToString(value, row, memberMapData);
        }

        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return LiveType.Seiyuu; // デフォルト

            // 定義されているEnum値を総当りして、日本語名が一致するものを探す
            foreach (LiveType type in Enum.GetValues(typeof(LiveType)))
            {
                if (type.ToDisplayString() == text)
                {
                    return type;
                }
            }

            // マッチしなければデフォルトを返すか、エラーにする
            return LiveType.Seiyuu;
        }
    }

    // 3. ブランド(BrandType)用のコンバーター
    public class BrandTypeConverter : DefaultTypeConverter
    {
        public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value is BrandType brand)
            {
                return brand.ToDisplayString(); // 拡張メソッドで日本語化
            }
            return base.ConvertToString(value, row, memberMapData);
        }

        public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text)) return BrandType.None;

            // 複数選択(カンマ区切り)に対応するため、Flagsとして合成する
            var parts = text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = BrandType.None;

            foreach (var part in parts)
            {
                bool found = false;
                foreach (BrandType brand in Enum.GetValues(typeof(BrandType)))
                {
                    // Noneはスキップ
                    if (brand == BrandType.None) continue;

                    // 日本語名(ToDisplayString) または Enum名(ToString) でマッチング
                    if (brand.ToDisplayString() == part || brand.ToString() == part)
                    {
                        result |= brand;
                        found = true;
                    }
                }
            }
            return result;
        }
    }

    // 4. サービス本体
    public class CsvDataService
    {
        public void ExportPerformers(string filePath, IEnumerable<Performer> performers)
        {
            using (var writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                // マッピングを登録
                csv.Context.RegisterClassMap<PerformerMap>();
                csv.WriteRecords(performers);
            }
        }

        public List<Performer> ImportPerformers(string filePath)
        {
            using (var reader = new StreamReader(filePath, Encoding.UTF8))
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                // マッピングを登録
                csv.Context.RegisterClassMap<PerformerMap>();
                return csv.GetRecords<Performer>().ToList();
            }
        }
    }
}