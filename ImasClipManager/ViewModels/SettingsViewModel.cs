using CommunityToolkit.Mvvm.ComponentModel;
using ImasClipManager.Data;
using ImasClipManager.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImasClipManager.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly Func<AppDbContext> _dbFactory;
        private Dictionary<string, DisplayState> _cache = new();

        public SettingsViewModel(Func<AppDbContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task InitializeAsync()
        {
            using (var db = _dbFactory())
            {
                var allStates = await db.DisplayStates.ToListAsync();

                var defaults = new List<(DisplayContext, string)>
                {
                    // --- Table (表) ---
                    (DisplayContext.Table, "Thumbnail"),
                    (DisplayContext.Table, "FilePath"),
                    (DisplayContext.Table, "StartTime"),
                    (DisplayContext.Table, "EndTime"),
                    (DisplayContext.Table, "ClipName"),
                    (DisplayContext.Table, "SongTitle"),
                    (DisplayContext.Table, "ConcertName"),
                    (DisplayContext.Table, "ConcertDate"),
                    (DisplayContext.Table, "LiveType"),
                    (DisplayContext.Table, "Brands"),
                    (DisplayContext.Table, "Performers"),
                    (DisplayContext.Table, "Lyrics"),
                    (DisplayContext.Table, "Remarks"),
                    (DisplayContext.Table, "PlayCount"),
                    (DisplayContext.Table, "CreatedAt"),
                    (DisplayContext.Table, "UpdatedAt"),
                    
                    // --- Editor (編集・詳細) ---
                    (DisplayContext.Editor, "Thumbnail"),
                    (DisplayContext.Editor, "FilePath"),
                    (DisplayContext.Editor, "StartTime"),
                    (DisplayContext.Editor, "EndTime"),
                    (DisplayContext.Editor, "ClipName"),
                    (DisplayContext.Editor, "SongTitle"),
                    (DisplayContext.Editor, "ConcertName"),
                    (DisplayContext.Editor, "ConcertDate"),
                    (DisplayContext.Editor, "LiveType"),
                    (DisplayContext.Editor, "Brands"),
                    (DisplayContext.Editor, "Performers"),
                    (DisplayContext.Editor, "Lyrics"),
                    (DisplayContext.Editor, "Remarks"),
                    (DisplayContext.Editor, "PlayCount"),
                    (DisplayContext.Editor, "CreatedAt"),
                    (DisplayContext.Editor, "UpdatedAt")
                };

                // 表でデフォルトOFFにする項目
                var defaultOffKeys = new HashSet<string>
                {
                    "Table_FilePath",
                    "Table_StartTime",
                    "Table_EndTime",
                    "Table_ClipName"
                };

                bool needSave = false;
                foreach (var (ctx, key) in defaults)
                {
                    var dictKey = $"{ctx}_{key}";
                    var existing = allStates.FirstOrDefault(s => s.Context == ctx && s.PropertyKey == key);

                    if (existing == null)
                    {
                        // DBになければ作成。デフォルトOFFリストに含まれていれば false
                        bool initVal = !defaultOffKeys.Contains(dictKey);

                        existing = new DisplayState { Context = ctx, PropertyKey = key, IsVisible = initVal };
                        db.DisplayStates.Add(existing);
                        needSave = true;
                    }
                    _cache[dictKey] = existing;
                }

                if (needSave) await db.SaveChangesAsync();
            }
            OnPropertyChanged(string.Empty);
        }

        private bool GetValue(DisplayContext ctx, string key)
        {
            var dictKey = $"{ctx}_{key}";
            return _cache.ContainsKey(dictKey) && _cache[dictKey].IsVisible;
        }

        private void SetValue(DisplayContext ctx, string key, bool value)
        {
            var dictKey = $"{ctx}_{key}";
            if (_cache.ContainsKey(dictKey))
            {
                if (_cache[dictKey].IsVisible != value)
                {
                    _cache[dictKey].IsVisible = value;
                    OnPropertyChanged($"{ctx}_{key}");
                    _ = SaveStateAsync(_cache[dictKey]);
                }
            }
        }

        private async Task SaveStateAsync(DisplayState state)
        {
            using (var db = _dbFactory())
            {
                db.DisplayStates.Update(state);
                await db.SaveChangesAsync();
            }
        }

        // --- プロパティ (Table) ---
        public bool Table_Thumbnail { get => GetValue(DisplayContext.Table, "Thumbnail"); set => SetValue(DisplayContext.Table, "Thumbnail", value); }
        public bool Table_FilePath { get => GetValue(DisplayContext.Table, "FilePath"); set => SetValue(DisplayContext.Table, "FilePath", value); }
        public bool Table_StartTime { get => GetValue(DisplayContext.Table, "StartTime"); set => SetValue(DisplayContext.Table, "StartTime", value); }
        public bool Table_EndTime { get => GetValue(DisplayContext.Table, "EndTime"); set => SetValue(DisplayContext.Table, "EndTime", value); }
        public bool Table_ClipName { get => GetValue(DisplayContext.Table, "ClipName"); set => SetValue(DisplayContext.Table, "ClipName", value); }
        public bool Table_SongTitle { get => GetValue(DisplayContext.Table, "SongTitle"); set => SetValue(DisplayContext.Table, "SongTitle", value); }
        public bool Table_ConcertName { get => GetValue(DisplayContext.Table, "ConcertName"); set => SetValue(DisplayContext.Table, "ConcertName", value); }
        public bool Table_ConcertDate { get => GetValue(DisplayContext.Table, "ConcertDate"); set => SetValue(DisplayContext.Table, "ConcertDate", value); }
        public bool Table_LiveType { get => GetValue(DisplayContext.Table, "LiveType"); set => SetValue(DisplayContext.Table, "LiveType", value); }
        public bool Table_Brands { get => GetValue(DisplayContext.Table, "Brands"); set => SetValue(DisplayContext.Table, "Brands", value); }
        public bool Table_Performers { get => GetValue(DisplayContext.Table, "Performers"); set => SetValue(DisplayContext.Table, "Performers", value); }
        public bool Table_Lyrics { get => GetValue(DisplayContext.Table, "Lyrics"); set => SetValue(DisplayContext.Table, "Lyrics", value); }
        public bool Table_Remarks { get => GetValue(DisplayContext.Table, "Remarks"); set => SetValue(DisplayContext.Table, "Remarks", value); }
        public bool Table_PlayCount { get => GetValue(DisplayContext.Table, "PlayCount"); set => SetValue(DisplayContext.Table, "PlayCount", value); }
        public bool Table_CreatedAt { get => GetValue(DisplayContext.Table, "CreatedAt"); set => SetValue(DisplayContext.Table, "CreatedAt", value); }
        public bool Table_UpdatedAt { get => GetValue(DisplayContext.Table, "UpdatedAt"); set => SetValue(DisplayContext.Table, "UpdatedAt", value); }

        // --- プロパティ (Editor) ---
        public bool Editor_Thumbnail { get => GetValue(DisplayContext.Editor, "Thumbnail"); set => SetValue(DisplayContext.Editor, "Thumbnail", value); }
        public bool Editor_FilePath { get => GetValue(DisplayContext.Editor, "FilePath"); set => SetValue(DisplayContext.Editor, "FilePath", value); }
        public bool Editor_StartTime { get => GetValue(DisplayContext.Editor, "StartTime"); set => SetValue(DisplayContext.Editor, "StartTime", value); }
        public bool Editor_EndTime { get => GetValue(DisplayContext.Editor, "EndTime"); set => SetValue(DisplayContext.Editor, "EndTime", value); }
        public bool Editor_ClipName { get => GetValue(DisplayContext.Editor, "ClipName"); set => SetValue(DisplayContext.Editor, "ClipName", value); }
        public bool Editor_SongTitle { get => GetValue(DisplayContext.Editor, "SongTitle"); set => SetValue(DisplayContext.Editor, "SongTitle", value); }
        public bool Editor_ConcertName { get => GetValue(DisplayContext.Editor, "ConcertName"); set => SetValue(DisplayContext.Editor, "ConcertName", value); }
        public bool Editor_ConcertDate { get => GetValue(DisplayContext.Editor, "ConcertDate"); set => SetValue(DisplayContext.Editor, "ConcertDate", value); }
        public bool Editor_LiveType { get => GetValue(DisplayContext.Editor, "LiveType"); set => SetValue(DisplayContext.Editor, "LiveType", value); }
        public bool Editor_Brands { get => GetValue(DisplayContext.Editor, "Brands"); set => SetValue(DisplayContext.Editor, "Brands", value); }
        public bool Editor_Performers { get => GetValue(DisplayContext.Editor, "Performers"); set => SetValue(DisplayContext.Editor, "Performers", value); }
        public bool Editor_Lyrics { get => GetValue(DisplayContext.Editor, "Lyrics"); set => SetValue(DisplayContext.Editor, "Lyrics", value); }
        public bool Editor_Remarks { get => GetValue(DisplayContext.Editor, "Remarks"); set => SetValue(DisplayContext.Editor, "Remarks", value); }
        public bool Editor_PlayCount { get => GetValue(DisplayContext.Editor, "PlayCount"); set => SetValue(DisplayContext.Editor, "PlayCount", value); }
        public bool Editor_CreatedAt { get => GetValue(DisplayContext.Editor, "CreatedAt"); set => SetValue(DisplayContext.Editor, "CreatedAt", value); }
        public bool Editor_UpdatedAt { get => GetValue(DisplayContext.Editor, "UpdatedAt"); set => SetValue(DisplayContext.Editor, "UpdatedAt", value); }
    }
}