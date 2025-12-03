using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImasClipManager.Helpers;
using ImasClipManager.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ImasClipManager.ViewModels
{
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
        private CancellationTokenSource? _debounceCts;

        [ObservableProperty] private string _searchResultText = "検索結果: - 件";

        // --- エラーメッセージプロパティ (各項目の下に表示) ---
        [ObservableProperty] private string _durationError = "";
        [ObservableProperty] private string _concertDateError = "";
        [ObservableProperty] private string _createdDateError = "";
        [ObservableProperty] private string _updatedDateError = "";

        // --- 検索フィールド ---
        [ObservableProperty] private string _path = "";
        partial void OnPathChanged(string value) => SearchWithDebounce();

        // 時間 (バリデーション対象)
        [ObservableProperty] private string _minDuration = "";
        partial void OnMinDurationChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private string _maxDuration = "";
        partial void OnMaxDurationChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private string _clipName = "";
        partial void OnClipNameChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private string _songTitle = "";
        partial void OnSongTitleChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private string _concertName = "";
        partial void OnConcertNameChanged(string value) => SearchWithDebounce();

        // 日付 (バリデーション対象)
        [ObservableProperty] private DateTime? _concertDateFrom;
        partial void OnConcertDateFromChanged(DateTime? value) => ApplySearch();

        [ObservableProperty] private DateTime? _concertDateTo;
        partial void OnConcertDateToChanged(DateTime? value) => ApplySearch();

        [ObservableProperty] private string _lyrics = "";
        partial void OnLyricsChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private string _remarks = "";
        partial void OnRemarksChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private DateTime? _createdFrom;
        partial void OnCreatedFromChanged(DateTime? value) => ApplySearch();

        [ObservableProperty] private DateTime? _createdTo;
        partial void OnCreatedToChanged(DateTime? value) => ApplySearch();

        [ObservableProperty] private DateTime? _updatedFrom;
        partial void OnUpdatedFromChanged(DateTime? value) => ApplySearch();

        [ObservableProperty] private DateTime? _updatedTo;
        partial void OnUpdatedToChanged(DateTime? value) => ApplySearch();

        public ObservableCollection<SelectionItem<BrandType>> BrandList { get; } = new();
        public ObservableCollection<SelectionItem<LiveType>> LiveTypeList { get; } = new();

        [ObservableProperty] private string _performersText = "";
        private List<Performer> _selectedPerformers = new();

        public AdvancedSearchViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            InitializeLists();
            CalculateCount();
        }

        private void InitializeLists()
        {
            foreach (BrandType b in Enum.GetValues(typeof(BrandType)))
            {
                if (b == BrandType.None) continue;
                var item = new SelectionItem<BrandType> { Label = b.ToDisplayString(), Value = b };
                item.PropertyChanged += OnItemPropertyChanged;
                BrandList.Add(item);
            }
            foreach (LiveType t in Enum.GetValues(typeof(LiveType)))
            {
                var item = new SelectionItem<LiveType> { Label = t.ToDisplayString(), Value = t };
                item.PropertyChanged += OnItemPropertyChanged;
                LiveTypeList.Add(item);
            }
        }

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectionItem<int>.IsSelected)) ApplySearch();
        }

        // --- クエリ生成 ---
        public string GenerateQuery()
        {
            var sb = new StringBuilder();

            // エラーリセット
            DurationError = "";
            ConcertDateError = "";
            CreatedDateError = "";
            UpdatedDateError = "";

            // 文字列系 (これらはそのまま)
            AppendText(sb, "?path", Path);
            AppendText(sb, "?clip", ClipName);
            AppendText(sb, "?song", SongTitle);
            AppendText(sb, "?concert", ConcertName);
            AppendText(sb, "?lyrics", Lyrics);
            AppendText(sb, "?remarks", Remarks);

            // 1. 動画時間のバリデーション
            bool isMinValid = TimeHelper.TryParseTime(MinDuration, out long minMs);
            bool isMaxValid = TimeHelper.TryParseTime(MaxDuration, out long maxMs);

            if (!isMinValid && !string.IsNullOrEmpty(MinDuration))
            {
                DurationError = "下限の形式が不正です";
            }
            else if (!isMaxValid && !string.IsNullOrEmpty(MaxDuration))
            {
                DurationError = "上限の形式が不正です";
            }
            else
            {
                // 空の場合は比較対象にしない
                bool hasMin = !string.IsNullOrEmpty(MinDuration);
                bool hasMax = !string.IsNullOrEmpty(MaxDuration);

                if (hasMin && hasMax && minMs > maxMs)
                {
                    DurationError = "下限が上限を超えています";
                }
                else
                {
                    // 正常または片方のみ
                    string minStr = hasMin ? (minMs / 1000.0).ToString() : "";
                    string maxStr = hasMax ? (maxMs / 1000.0).ToString() : "";
                    AppendRange(sb, "?duration", minStr, maxStr);
                }
            }

            // 2. 公演日のバリデーション
            if (ConcertDateFrom != null && ConcertDateTo != null && ConcertDateFrom > ConcertDateTo)
            {
                ConcertDateError = "開始日が終了日より未来です";
            }
            else
            {
                AppendRange(sb, "?date", DateStr(ConcertDateFrom), DateStr(ConcertDateTo));
            }

            // 3. 登録日のバリデーション
            if (CreatedFrom != null && CreatedTo != null && CreatedFrom > CreatedTo)
            {
                CreatedDateError = "開始日が終了日より未来です";
            }
            else
            {
                AppendRange(sb, "?created", DateStr(CreatedFrom), DateStr(CreatedTo));
            }

            // 4. 更新日のバリデーション
            if (UpdatedFrom != null && UpdatedTo != null && UpdatedFrom > UpdatedTo)
            {
                UpdatedDateError = "開始日が終了日より未来です";
            }
            else
            {
                AppendRange(sb, "?updated", DateStr(UpdatedFrom), DateStr(UpdatedTo));
            }

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
            var joined = string.Join(" OR ", list.Select(v => $"\"{v}\""));
            sb.Append($"{key}:({joined}) ");
        }

        private string DateStr(DateTime? d) => d?.ToString("yyyy/MM/dd") ?? "";

        // --- コマンド ---

        public void ApplySearch()
        {
            var query = GenerateQuery();
            // 不正な項目はクエリに含まれないため、正しい条件のみで検索される
            // (前回検索結果を保持したい場合はエラー判定を追加してここでreturnするが、
            //  今回は「不正な欄の更新を行わない」=「その条件を除外して検索」と解釈し、常に検索を実行する)
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
            var vm = new PerformerSelectionViewModel(_selectedPerformers);
            var win = new Views.PerformerSelectionWindow(vm);

            if (win.ShowDialog() == true)
            {
                _selectedPerformers = vm.GetSelectedPerformers();
                PerformersText = string.Join(", ", _selectedPerformers.Select(p => p.Name));
                ApplySearch();
            }
        }

        private async void SearchWithDebounce()
        {
            try
            {
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;
                await Task.Delay(300, token);
                Application.Current.Dispatcher.Invoke(ApplySearch);
            }
            catch (TaskCanceledException) { }
        }

        public void CalculateCount()
        {
            var query = GenerateQuery();
            var predicate = SearchQueryParser.Parse(query, null);
            int count = _mainViewModel.Clips.Count(c => predicate(c));
            SearchResultText = $"検索結果: {count} 件";
        }
    }
}