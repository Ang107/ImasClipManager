using CommunityToolkit.Mvvm.ComponentModel;
using ImasClipManager.Data;
using ImasClipManager.Models;
using ImasClipManager.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ImasClipManager.ViewModels
{
    // ラッパークラス (表示ロジック削除)
    public partial class PerformerSelectable : ObservableObject
    {
        public Performer Model { get; }

        [ObservableProperty]
        private bool _isSelected;

        public PerformerSelectable(Performer performer, bool isSelected)
        {
            Model = performer;
            IsSelected = isSelected;
        }

        // そのまま表示するだけ
        public string DisplayName => Model.Name;
    }

    public partial class PerformerSelectionViewModel : ObservableObject
    {
        private List<PerformerSelectable> _allPerformers = new();
        public ObservableCollection<PerformerSelectable> DisplayPerformers { get; } = new();

        [ObservableProperty]
        private string _searchText = string.Empty;

        // --- ブランドフィルタ (変更なし) ---
        public class BrandOption
        {
            public string Label { get; set; } = "";
            public BrandType? Value { get; set; }
        }
        public List<BrandOption> BrandList { get; private set; }
        private BrandOption _selectedBrandOption;
        public BrandOption SelectedBrandOption
        {
            get => _selectedBrandOption;
            set { if (SetProperty(ref _selectedBrandOption, value)) ApplyFilter(); }
        }

        // --- ライブ形式フィルタ (変更なし) ---
        public class LiveTypeOption
        {
            public string Label { get; set; } = "";
            public LiveType? Value { get; set; }
        }
        public List<LiveTypeOption> LiveTypeList { get; private set; }
        private LiveTypeOption _selectedLiveTypeOption;
        public LiveTypeOption SelectedLiveTypeOption
        {
            get => _selectedLiveTypeOption;
            set { if (SetProperty(ref _selectedLiveTypeOption, value)) ApplyFilter(); }
        }

        public PerformerSelectionViewModel(List<Performer> currentSelection)
        {
            // 1. ブランドリスト生成
            var brandList = new List<BrandOption> { new BrandOption { Label = "すべて", Value = null } };
            foreach (BrandType brand in Enum.GetValues(typeof(BrandType)))
            {
                if (brand == BrandType.None) continue;
                brandList.Add(new BrandOption { Label = brand.ToDisplayString(), Value = brand });
            }
            BrandList = brandList;

            // 2. ライブ形式リスト生成
            var liveList = new List<LiveTypeOption> { new LiveTypeOption { Label = "指定なし", Value = null } }; // フィルタ用なので「指定なし」が必要
            foreach (LiveType type in Enum.GetValues(typeof(LiveType)))
            {
                liveList.Add(new LiveTypeOption { Label = type.ToDisplayString(), Value = type });
            }
            LiveTypeList = liveList;

            _selectedLiveTypeOption = LiveTypeList.First(); // 指定なし
            _selectedBrandOption = BrandList.First();       // すべて

            LoadData(currentSelection);
        }

        private void LoadData(List<Performer> currentSelection)
        {
            using (var db = new AppDbContext())
            {
                // 名前順などでソート
                var dbList = db.Performers.OrderBy(p => p.Brand).ThenBy(p => p.Yomi).ToList();

                var currentIds = currentSelection.Select(p => p.Id).ToHashSet();

                _allPerformers = dbList.Select(p =>
                    new PerformerSelectable(p, currentIds.Contains(p.Id))).ToList();
            }
            ApplyFilter();
        }

        partial void OnSearchTextChanged(string value) => ApplyFilter();

        private void ApplyFilter()
        {
            DisplayPerformers.Clear();
            IEnumerable<PerformerSelectable> query = _allPerformers;

            // ブランドフィルタ
            if (SelectedBrandOption != null && SelectedBrandOption.Value.HasValue)
            {
                query = query.Where(p => p.Model.Brand.HasFlag(SelectedBrandOption.Value.Value));
            }

            // ライブ形式フィルタ (表示切り替えではなく、単純な絞り込みとして機能)
            if (SelectedLiveTypeOption != null && SelectedLiveTypeOption.Value.HasValue)
            {
                query = query.Where(p => p.Model.LiveType == SelectedLiveTypeOption.Value.Value);
            }

            // テキスト検索 (名前 または 読み)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                // スペース区切りのAND検索に対応
                var keywords = SearchText.ToLower().Split(new[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var k in keywords)
                {
                    query = query.Where(p =>
                        p.Model.Name.ToLower().Contains(k) ||
                        p.Model.Yomi.Contains(k));
                }
            }

            foreach (var item in query)
            {
                DisplayPerformers.Add(item);
            }
        }

        public List<Performer> GetSelectedPerformers()
        {
            return _allPerformers.Where(p => p.IsSelected).Select(p => p.Model).ToList();
        }
    }
}