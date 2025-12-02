using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImasClipManager.Helpers;
using ImasClipManager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ImasClipManager.ViewModels
{
    // コンボボックス等の選択肢用クラス
    public class SelectionItem<T> : ObservableObject
    {
        public string Label { get; set; } = "";
        public T Value { get; set; }
        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public partial class AdvancedSearchViewModel : ObservableObject
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty] private string _searchResultText = "検索結果: - 件";

        // --- 検索フィールド ---
        [ObservableProperty] private string _path = "";

        // 動画時間 (秒数または mm:ss)
        [ObservableProperty] private string _minDuration = "";
        [ObservableProperty] private string _maxDuration = "";

        [ObservableProperty] private string _clipName = "";
        [ObservableProperty] private string _songTitle = "";
        [ObservableProperty] private string _concertName = "";

        [ObservableProperty] private DateTime? _concertDateFrom;
        [ObservableProperty] private DateTime? _concertDateTo;

        [ObservableProperty] private string _lyrics = "";
        [ObservableProperty] private string _remarks = "";

        [ObservableProperty] private DateTime? _createdFrom;
        [ObservableProperty] private DateTime? _createdTo;
        [ObservableProperty] private DateTime? _updatedFrom;
        [ObservableProperty] private DateTime? _updatedTo;

        // リスト選択系
        public ObservableCollection<SelectionItem<BrandType>> BrandList { get; } = new();
        public ObservableCollection<SelectionItem<LiveType>> LiveTypeList { get; } = new();

        // 出演者 (ダイアログ選択結果)
        [ObservableProperty] private string _performersText = "";
        private List<Performer> _selectedPerformers = new();

        public AdvancedSearchViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            InitializeLists();

            // 初回計算
            CalculateCount();

            // 入力が変わるたびに件数更新したい場合は、各プロパティのsetterやPropertyChangedイベントで
            // CalculateCount()を呼ぶようにするとリアルタイム反映されますが、
            // 今回はシンプルに「適用」ボタンか、特定のタイミングで呼ぶ形にします。
            // (下記 ApplySearchCommand で反映)
        }

        private void InitializeLists()
        {
            // ブランド
            foreach (BrandType b in Enum.GetValues(typeof(BrandType)))
            {
                if (b == BrandType.None) continue;
                BrandList.Add(new SelectionItem<BrandType> { Label = b.ToDisplayString(), Value = b });
            }
            // 形式
            foreach (LiveType t in Enum.GetValues(typeof(LiveType)))
            {
                LiveTypeList.Add(new SelectionItem<LiveType> { Label = t.ToDisplayString(), Value = t });
            }
        }

        // クエリ文字列の生成
        public string GenerateQuery()
        {
            var sb = new StringBuilder();

            AppendText(sb, "?path", Path);
            AppendText(sb, "?clip", ClipName);
            AppendText(sb, "?song", SongTitle);
            AppendText(sb, "?concert", ConcertName);
            AppendText(sb, "?lyrics", Lyrics);
            AppendText(sb, "?remarks", Remarks);

            AppendRange(sb, "?duration", TimeStrSeconds(MinDuration), TimeStrSeconds(MaxDuration));
            AppendRange(sb, "?date", DateStr(ConcertDateFrom), DateStr(ConcertDateTo));
            AppendRange(sb, "?created", DateStr(CreatedFrom), DateStr(CreatedTo));
            AppendRange(sb, "?updated", DateStr(UpdatedFrom), DateStr(UpdatedTo));

            // 集合 (OR)
            AppendSelection(sb, "?brands", BrandList.Where(b => b.IsSelected).Select(b => b.Label));
            AppendSelection(sb, "?type", LiveTypeList.Where(t => t.IsSelected).Select(t => t.Label));

            if (_selectedPerformers.Any())
            {
                AppendSelection(sb, "?performers", _selectedPerformers.Select(p => p.Name));
            }

            return sb.ToString().Trim();
        }

        private void AppendText(StringBuilder sb, string key, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                // スペースを含む場合はクォート
                if (value.Contains(" ")) sb.Append($"{key}:\"{value}\" ");
                else sb.Append($"{key}:{value} ");
            }
        }

        private void AppendRange(StringBuilder sb, string key, string min, string max)
        {
            if (!string.IsNullOrEmpty(min) || !string.IsNullOrEmpty(max))
            {
                sb.Append($"{key}:{min}-{max} ");
            }
        }

        private void AppendSelection(StringBuilder sb, string key, IEnumerable<string> values)
        {
            var list = values.ToList();
            if (!list.Any()) return;

            // 常に ?key:(A OR B) の形式にする
            var joined = string.Join(" OR ", list.Select(v => $"\"{v}\""));
            sb.Append($"{key}:({joined}) ");
        }

        private string TimeStrSeconds(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            if (TimeHelper.TryParseTime(input, out long ms)) return (ms / 1000).ToString();
            // 数値変換できなければそのまま返す(パーサー側で処理)
            return input;
        }

        private string DateStr(DateTime? d) => d?.ToString("yyyy/MM/dd") ?? "";

        // --- コマンド ---

        [RelayCommand]
        public void ApplySearch()
        {
            var query = GenerateQuery();
            // MainViewModelのSearchTextを更新 -> 自動的に検索が走る
            _mainViewModel.SearchText = query;
            CalculateCount();
        }

        [RelayCommand]
        public void Reset()
        {
            Path = ""; MinDuration = ""; MaxDuration = "";
            ClipName = ""; SongTitle = ""; ConcertName = "";
            ConcertDateFrom = null; ConcertDateTo = null;
            Lyrics = ""; Remarks = "";
            CreatedFrom = null; CreatedTo = null;
            UpdatedFrom = null; UpdatedTo = null;

            foreach (var b in BrandList) b.IsSelected = false;
            foreach (var t in LiveTypeList) t.IsSelected = false;

            _selectedPerformers.Clear();
            PerformersText = "";

            ApplySearch();
        }

        [RelayCommand]
        public void SelectPerformers()
        {
            // 既存の選択画面VM/Viewを利用
            var vm = new PerformerSelectionViewModel(_selectedPerformers);
            var win = new Views.PerformerSelectionWindow(vm);
            // 本来はOwner設定が必要ですが、ViewModelからは直接触れないため省略
            // (Viewのコードビハインドで設定するか、Service経由で開くのが理想)

            if (win.ShowDialog() == true)
            {
                _selectedPerformers = vm.GetSelectedPerformers();
                PerformersText = string.Join(", ", _selectedPerformers.Select(p => p.Name));
            }
        }

        public void CalculateCount()
        {
            var query = GenerateQuery();
            // 一時的にパースして計算
            var predicate = SearchQueryParser.Parse(query, null); // 設定無視(明示指定のみのため)
            int count = _mainViewModel.Clips.Count(c => predicate(c));
            SearchResultText = $"検索結果: {count} 件";
        }
    }
}