using System;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using ABI_RC.Core.Player;
using MelonLoader;
using UnityEngine;

namespace BTKUILib.Features
{

    internal static class StatsOverlayModule
    {
        private static bool _overlayVisible;
        private static MelonPreferences_Entry<bool> _visiblePref;
        private static MelonPreferences_Entry<int> _fontSizePref;

        private static ToggleButton _overlayToggleUI;

       
        private static float _deltaTime;
        private static float _fps;
        private static float _updateInterval = 0.5f;
        private static float _timeSinceLastUpdate;
        private static int _frameCount;

      
        private static GUIStyle _labelStyle;
        private static GUIStyle _shadowStyle;

        internal static void Init(MelonPreferences_Category prefCategory)
        {
            _visiblePref = prefCategory.CreateEntry("StatsOverlayVisible", false, "Stats Overlay", "Show FPS/performance stats overlay");
            _fontSizePref = prefCategory.CreateEntry("StatsOverlayFontSize", 16, "Stats Font Size", "Font size for the stats overlay");

            _overlayVisible = _visiblePref.Value;

           
            KeybindModule.RegisterKeybind("Toggle Stats Overlay", KeyCode.F3, KeyCode.None, ToggleOverlay);
        }

        internal static void GenerateUI(Category parentCategory)
        {
            _overlayToggleUI = parentCategory.AddToggle("Stats Overlay", "Toggle FPS/performance overlay (F3)", _overlayVisible);
            
            var fontSlider = parentCategory.AddSlider("Stats Font Size", "Adjust the font size of the stats overlay", _fontSizePref.Value, 10f, 32f, 0, 16f, true);
            fontSlider.Hidden = !_overlayVisible;
            
            _overlayToggleUI.OnValueUpdated += b =>
            {
                _overlayVisible = b;
                fontSlider.Hidden = !b;
                _visiblePref.Value = b;
                MelonPreferences.Save();
            };

            fontSlider.OnValueUpdated += f =>
            {
                _fontSizePref.Value = (int)f;
                _labelStyle = null; 
                _shadowStyle = null;
                MelonPreferences.Save();
            };
        }

       
        internal static void OnUpdate()
        {
            _deltaTime = Time.unscaledDeltaTime;
            _frameCount++;
            _timeSinceLastUpdate += _deltaTime;

            if (_timeSinceLastUpdate >= _updateInterval)
            {
                _fps = _frameCount / _timeSinceLastUpdate;
                _frameCount = 0;
                _timeSinceLastUpdate = 0f;
            }
        }

       
        internal static void OnGUI()
        {
            if (!_overlayVisible) return;

            EnsureStyles();

            var frameTimeMs = _deltaTime * 1000f;

           
            var playerCount = GetPlayerCount();

            var text = $"FPS: {_fps:F1}\n" +
                       $"Frame: {frameTimeMs:F1}ms\n" +
                       $"Players: {playerCount}";

            var x = 10f;
            var y = 10f;

            
            GUI.Label(new Rect(x + 1, y + 1, 300, 100), text, _shadowStyle);
         
            GUI.Label(new Rect(x, y, 300, 100), text, _labelStyle);
        }

        private static void ToggleOverlay()
        {
            _overlayVisible = !_overlayVisible;
            _visiblePref.Value = _overlayVisible;
            MelonPreferences.Save();

            if (_overlayToggleUI != null)
                _overlayToggleUI.ToggleValue = _overlayVisible;

            QuickMenuAPI.ShowAlertToast(_overlayVisible ? "Stats Overlay ON" : "Stats Overlay OFF", 2);
        }

        private static int GetPlayerCount()
        {
            try
            {
                var playerManager = CVRPlayerManager.Instance;
                if (playerManager == null) return -1;

                var networkPlayers = playerManager.NetworkPlayers;
                return (networkPlayers?.Count ?? 0) + 1; 
            }
            catch
            {
                return -1;
            }
        }

        private static void EnsureStyles()
        {
            if (_labelStyle != null) return;

            var fontSize = _fontSizePref?.Value ?? 16;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.1f, 1f, 0.4f, 0.95f) },
                alignment = TextAnchor.UpperLeft
            };

            _shadowStyle = new GUIStyle(_labelStyle)
            {
                normal = { textColor = new Color(0f, 0f, 0f, 0.7f) }
            };
        }
    }
}
