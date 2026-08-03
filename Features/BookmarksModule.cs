using System;
using System.Collections.Generic;
using System.IO;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using ABI_RC.Core.Savior;
using ABI_RC.Core.Networking.IO.Instancing;
using MelonLoader;
using Newtonsoft.Json;
using UnityEngine;

namespace BTKUILib.Features
{
    internal static class BookmarksModule
    {
        private static readonly string BookmarkDir = Path.Combine("UserData", "HatClient");
        private static readonly string BookmarkFile = Path.Combine("UserData", "HatClient", "bookmarks.json");

        private static BookmarkData _data = new();
        private static Page _bookmarksPage;
        private static Category _bookmarkListCategory;

        internal static void Init()
        {
            if (!Directory.Exists(BookmarkDir))
                Directory.CreateDirectory(BookmarkDir);

            LoadBookmarks();

            KeybindModule.RegisterKeybind("Open Bookmarks", KeyCode.B, KeyCode.None, () =>
            {
                _bookmarksPage?.OpenPage(true);
            });
        }

        internal static void GenerateUI(Category parentCategory)
        {
            _bookmarksPage = parentCategory.AddPage("World Bookmarks", "Star", "Save and quick-join your favorite worlds", "HatClient");
            _bookmarksPage.MenuTitle = "World Bookmarks";
            _bookmarksPage.MenuSubtitle = "Save, manage, and join your bookmarked worlds";

            var actionsCat = _bookmarksPage.AddCategory("Actions");
            var saveBtn = actionsCat.AddButton("Bookmark Current World", "Star", "Save the current world to your bookmarks");
            saveBtn.OnPress += SaveCurrentWorld;

            _bookmarkListCategory = _bookmarksPage.AddCategory("Saved Bookmarks");

            RefreshBookmarkList();
        }

        private static void SaveCurrentWorld()
        {
            try
            {
                var worldId = Instances.CurrentWorldId;
                var instanceId = Instances.CurrentInstanceId;

                if (string.IsNullOrEmpty(worldId))
                {
                    QuickMenuAPI.ShowAlertToast("Not in a world!", 3);
                    return;
                }

                if (_data.Bookmarks.Exists(b => b.WorldId == worldId))
                {
                    QuickMenuAPI.ShowAlertToast("World already bookmarked!", 3);
                    return;
                }

                string worldName = worldId;
                try
                {
                    var metaPort = MetaPort.Instance;
                    if (metaPort != null)
                        worldName = metaPort.CurrentWorldName ?? worldId;
                }
                catch
                {

                }

                var entry = new BookmarkEntry
                {
                    WorldName = string.IsNullOrEmpty(worldName) ? worldId : worldName,
                    WorldId = worldId,
                    InstanceId = instanceId,
                    SavedAt = DateTime.Now
                };

                _data.Bookmarks.Add(entry);
                SaveBookmarks();
                RefreshBookmarkList();

                QuickMenuAPI.ShowAlertToast($"Bookmarked: {entry.WorldName}", 3);
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to save bookmark: {ex}");
                QuickMenuAPI.ShowAlertToast("Failed to save bookmark", 3);
            }
        }

        private static void JoinWorld(string worldId)
        {
            try
            {
                ABI_RC.Core.Networking.NetworkManager.TryCreateInstanceOfWorldToJoin(worldId, false);
                QuickMenuAPI.ShowAlertToast("Joining world...", 3);
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to join world: {ex}");
                QuickMenuAPI.ShowAlertToast("Failed to join world", 3);
            }
        }

        private static void RemoveBookmark(BookmarkEntry entry)
        {
            _data.Bookmarks.Remove(entry);
            SaveBookmarks();
            RefreshBookmarkList();
            QuickMenuAPI.ShowAlertToast($"Removed: {entry.WorldName}", 3);
        }

        private static void RefreshBookmarkList()
        {
            if (_bookmarkListCategory == null) return;

            _bookmarkListCategory.ClearChildren();

            if (_data.Bookmarks.Count == 0)
            {
                _bookmarkListCategory.AddButton("No bookmarks yet", "", "Save a world to see it here", ButtonStyle.TextOnly);
                return;
            }

            foreach (var bookmark in _data.Bookmarks)
            {
                var bm = bookmark;
                var btn = _bookmarkListCategory.AddButton(bm.ToString(), "Star", $"Click to join | Hold to delete\nWorld ID: {bm.WorldId}");
                btn.OnPress += () => JoinWorld(bm.WorldId);
                btn.OnHeld += () => RemoveBookmark(bm);
            }
        }

        private static void LoadBookmarks()
        {
            try
            {
                if (File.Exists(BookmarkFile))
                {
                    var json = File.ReadAllText(BookmarkFile);
                    _data = JsonConvert.DeserializeObject<BookmarkData>(json) ?? new BookmarkData();
                }
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to load bookmarks: {ex}");
                _data = new BookmarkData();
            }
        }

        private static void SaveBookmarks()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_data, Formatting.Indented);
                File.WriteAllText(BookmarkFile, json);
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to save bookmarks: {ex}");
            }
        }
    }
}
