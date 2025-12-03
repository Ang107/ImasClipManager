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

        // --- エラーメッセージプロパティ ---
        [ObservableProperty] private string _durationError = "";
        [ObservableProperty] private string _concertDateError = "";
        [ObservableProperty] private string _createdDateError = "";
        [ObservableProperty] private string _updatedDateError = "";

        // --- 検索フィールド ---
        [ObservableProperty] private string _path = "";
        partial void OnPathChanged(string value) => SearchWithDebounce();

        // ★修正: MinDuration (手動プロパティ)
        private string _minDuration = "";
        public string MinDuration
        {
            get => _minDuration;
            set
            {
                // 空文字は許可
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (SetProperty(ref _minDuration, value)) ValidateAndApplySearch();
                    return;
                }

                // 単体バリデーション: 時刻として解析できるか
                if (TimeHelper.TryParseTime(value, out long ms))
                {
                    // 成功: hh:mm:ss 形式に整形してセット
                    string formatted = TimeHelper.FormatDuration(ms);
                    // 値が変わっていればセット、変わらなくても再フォーマット扱いなどで更新通知
                    if (SetProperty(ref _minDuration, formatted))
                    {
                        ValidateAndApplySearch();
                    }
                    else
                    {
                        // SetPropertyがfalseでも、フォーマット統一のためにView更新を促す
                        OnPropertyChanged(nameof(MinDuration));
                        ValidateAndApplySearch();
                    }
                }
                else
                {
                    // 失敗: 更新を拒否してViewを元の値に戻す
                    OnPropertyChanged(nameof(MinDuration));
                }
            }
        }

        // ★修正: MaxDuration (手動プロパティ)
        private string _maxDuration = "";
        public string MaxDuration
        {
            get => _maxDuration;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    if (SetProperty(ref _maxDuration, value)) ValidateAndApplySearch();
                    return;
                }

                if (TimeHelper.TryParseTime(value, out long ms))
                {
                    string formatted = TimeHelper.FormatDuration(ms);
                    if (SetProperty(ref _maxDuration, formatted))
                    {
                        ValidateAndApplySearch();
                    }
                    else
                    {
                        OnPropertyChanged(nameof(MaxDuration));
                        ValidateAndApplySearch();
                    }
                }
                else
                {
                    OnPropertyChanged(nameof(MaxDuration));
                }
            }
        }

        [ObservableProperty] private string _clipName = "";
        partial void OnClipNameChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private string _songTitle = "";
        partial void OnSongTitleChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private string _concertName = "";
        partial void OnConcertNameChanged(string value) => SearchWithDebounce();

        // 日付系は変更時即反映
        [ObservableProperty] private DateTime? _concertDateFrom;
        partial void OnConcertDateFromChanged(DateTime? value) => ValidateAndApplySearch();

        [ObservableProperty] private DateTime? _concertDateTo;
        partial void OnConcertDateToChanged(DateTime? value) => ValidateAndApplySearch();

        [ObservableProperty] private string _lyrics = "";
        partial void OnLyricsChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private string _remarks = "";
        partial void OnRemarksChanged(string value) => SearchWithDebounce();

        [ObservableProperty] private DateTime? _createdFrom;
        partial void OnCreatedFromChanged(DateTime? value) => ValidateAndApplySearch();

        [ObservableProperty] private DateTime? _createdTo;
        partial void OnCreatedToChanged(DateTime? value) => ValidateAndApplySearch();

        [ObservableProperty] private DateTime? _updatedFrom;
        partial void OnUpdatedFromChanged(DateTime? value) => ValidateAndApplySearch();

        [ObservableProperty] private DateTime? _updatedTo;
        partial void OnUpdatedToChanged(DateTime? value) => ValidateAndApplySearch();

        public ObservableCollection<SelectionItem<BrandType>> BrandList { get; } = new();
        public ObservableCollection<SelectionItem<LiveType>> LiveTypeList { get; } = new();

        [ObservableProperty] private string _performersText = "";
        private List<Performer> _selectedPerformers = new();

        public AdvancedSearchViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            InitializeLists();
            // 初期状態はエラーなし・全件検索
            ValidateAndApplySearch();
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
            if (e.PropertyName == nameof(SelectionItem<int>.IsSelected)) ValidateAndApplySearch();
        }

        // --- ★重要: バリデーションと検索適用の制御 ---
        private void ValidateAndApplySearch()
        {
            // デバウンス経由で呼び出す（入力のたびに重い処理が走らないように）
            SearchWithDebounce();
        }

        private async void SearchWithDebounce()
        {
            try
            {
                _debounceCts?.Cancel();
                _debounceCts = new CancellationTokenSource();
                var token = _debounceCts.Token;

                // バリデーション自体は即時行っても良いが、エラー表示のチラつきを防ぐためデバウンスに入れる
                await Task.Delay(300, token);

                // UIスレッドで実行
                Application.Current.Dispatcher.Invoke(() =>
                {
                    PerformValidationAndSearch();
                });
            }
            catch (TaskCanceledException) { }
        }

        private void PerformValidationAndSearch()
        {
            bool hasError = false;

            // 1. 動画時間の大小チェック
            DurationError = "";
            if (TimeHelper.TryParseTime(MinDuration, out long minMs) &&
                TimeHelper.TryParseTime(MaxDuration, out long maxMs))
            {
                // 両方入力されている場合のみチェック
                if (!string.IsNullOrEmpty(MinDuration) && !string.IsNullOrEmpty(MaxDuration))
                {
                    if (minMs > maxMs)
                    {
                        DurationError = "下限が上限を超えています";
                        hasError = true;
                    }
                }
            }

            // 2. 公演日の大小チェック
            ConcertDateError = "";
            if (ConcertDateFrom != null && ConcertDateTo != null && ConcertDateFrom > ConcertDateTo)
            {
                ConcertDateError = "開始日が終了日より未来です";
                hasError = true;
            }

            // 3. 登録日の大小チェック
            CreatedDateError = "";
            if (CreatedFrom != null && CreatedTo != null && CreatedFrom > CreatedTo)
            {
                CreatedDateError = "開始日が終了日より未来です";
                hasError = true;
            }

            // 4. 更新日の大小チェック
            UpdatedDateError = "";
            if (UpdatedFrom != null && UpdatedTo != null && UpdatedFrom > UpdatedTo)
            {
                UpdatedDateError = "開始日が終了日より未来です";
                hasError = true;
            }

            // ★エラーがある場合は検索クエリを更新しない
            if (hasError)
            {
                // ここで return することでメインウィンドウの検索は更新されない
                // ただし、高度な検索ウィンドウ上のエラーメッセージは更新される
                return;
            }

            // エラーがない場合のみ検索実行
            var query = GenerateQuery();
            _mainViewModel.SearchText = query;
            CalculateCount();
        }

        // --- クエリ生成 (バリデーションは通過済み前提) ---
        public string GenerateQuery()
        {
            var sb = new StringBuilder();

            AppendText(sb, "?path", Path);
            AppendText(sb, "?clip", ClipName);
            AppendText(sb, "?song", SongTitle);
            AppendText(sb, "?concert", ConcertName);
            AppendText(sb, "?lyrics", Lyrics);
            AppendText(sb, "?remarks", Remarks);

            // Duration
            // バリデーション済みなので単純に変換
            TimeHelper.TryParseTime(MinDuration, out long minMs);
            TimeHelper.TryParseTime(MaxDuration, out long maxMs);
            bool hasMin = !string.IsNullOrEmpty(MinDuration);
            bool hasMax = !string.IsNullOrEmpty(MaxDuration);
            string minStr = hasMin ? (minMs / 1000.0).ToString() : "";
            string maxStr = hasMax ? (maxMs / 1000.0).ToString() : "";
            AppendRange(sb, "?duration", minStr, maxStr);

            // Dates
            AppendRange(sb, "?date", DateStr(ConcertDateFrom), DateStr(ConcertDateTo));
            AppendRange(sb, "?created", DateStr(CreatedFrom), DateStr(CreatedTo));
            AppendRange(sb, "?updated", DateStr(UpdatedFrom), DateStr(UpdatedTo));

            // Selections
            AppendSelection(sb, "?brands", BrandList.Where(b => b.IsSelected).Select(b => b.Label));
            AppendSelection(sb, "?type", LiveTypeList.Where(t => t.IsSelected).Select(t => t.Label));

            if (_selectedPerformers.Any())
            {
                AppendSelection(sb, "?performers", _selectedPerformers.Select(p => p.Name));
            }

            return sb.ToString().Trim();
        }

        // ... (Append系ヘルパーメソッドは変更なしのため省略) ...
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

        public void CalculateCount()
        {
            var query = GenerateQuery();
            var predicate = SearchQueryParser.Parse(query, null);
            int count = _mainViewModel.Clips.Count(c => predicate(c));
            SearchResultText = $"検索結果: {count} 件";
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

            ValidateAndApplySearch();
        }

        [RelayCommand]
        public void SelectPerformers()
        {
            var vm = new PerformerSelectionViewModel(_selectedPerformers);
            var win = new Views.PerformerSelectionWindow(vm);

            if (win.ShowDialog() == true)
            {
                _selectedPerformers = vm.GetSelectedPerformers();
                // ★修正: 改行区切り
                PerformersText = string.Join(Environment.NewLine, _selectedPerformers.Select(p => p.Name));
                ValidateAndApplySearch();
            }
        }
    }
}