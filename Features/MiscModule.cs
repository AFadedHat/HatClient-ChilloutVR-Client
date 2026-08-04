using System;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using ABI_RC.Core.Player;
using ABI_RC.Core.PropManagement;
using ABI_RC.Core.InteractionSystem;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;
using ABI.CCK.Components;

namespace BTKUILib.Features
{
    internal static class MiscModule
    {
        private static MelonPreferences_Entry<bool> _nightVisionPref;
        private static MelonPreferences_Entry<bool> _flashlightPref;
        private static MelonPreferences_Entry<bool> _mirrorPref;
        private static MelonPreferences_Entry<bool> _antiPortalPref;
        private static MelonPreferences_Entry<bool> _notificationsPref;
        private static bool _nightVisionEnabled;
        private static bool _flashlightEnabled;
        private static bool _mirrorEnabled;
        private static bool _antiPortalEnabled;
        private static bool _notificationsEnabled;
        private static ToggleButton _nightVisionToggleUI;
        private static ToggleButton _flashlightToggleUI;
        private static ToggleButton _mirrorToggleUI;
        private static ToggleButton _antiPortalToggleUI;
        private static ToggleButton _notificationsToggleUI;
        private static Color _originalAmbientLight;
        private static AmbientMode _originalAmbientMode;
        private static bool _isNightVisionActive = false;
        private static GameObject _flashlightObj;
        private static float _nextPortalCheckTime;

        internal static void Init(MelonPreferences_Category prefCategory)
        {
            _nightVisionPref = prefCategory.CreateEntry("NightVisionEnabled", false, "Night Vision", "Forces ambient lighting to be bright");
            _flashlightPref = prefCategory.CreateEntry("FlashlightEnabled", false, "Flashlight", "Spawns a personal flashlight on your head");
            _mirrorPref = prefCategory.CreateEntry("MirrorEnabled", false, "Portable Mirror", "Toggles the built-in portable mirror");
            _antiPortalPref = prefCategory.CreateEntry("AntiPortalEnabled", false, "Anti-Portal", "Hides dropped portals");
            _notificationsPref = prefCategory.CreateEntry("NotificationsEnabled", false, "Join/Leave Notifications", "Toast notifications when players join/leave");

            _nightVisionEnabled = _nightVisionPref.Value;
            _flashlightEnabled = _flashlightPref.Value;
            _mirrorEnabled = _mirrorPref.Value;
            _antiPortalEnabled = _antiPortalPref.Value;
            _notificationsEnabled = _notificationsPref.Value;

            QuickMenuAPI.UserJoin += OnUserJoin;
            QuickMenuAPI.UserLeave += OnUserLeave;
            
            if (_flashlightEnabled) ToggleFlashlight(true);
        }

        internal static void GenerateUI(Category parentCategory)
        {
            _nightVisionToggleUI = parentCategory.AddToggle("Night Vision", "Forces ambient lighting to be extremely bright", _nightVisionEnabled);
            _nightVisionToggleUI.OnValueUpdated += b =>
            {
                _nightVisionEnabled = b;
                _nightVisionPref.Value = b;
                MelonPreferences.Save();
                ToggleNightVision(b);
            };

            _flashlightToggleUI = parentCategory.AddToggle("Personal Flashlight", "Spawns a hidden flashlight attached to your camera", _flashlightEnabled);
            _flashlightToggleUI.OnValueUpdated += b =>
            {
                _flashlightEnabled = b;
                _flashlightPref.Value = b;
                MelonPreferences.Save();
                ToggleFlashlight(b);
            };

            _mirrorToggleUI = parentCategory.AddToggle("Portable Mirror", "Toggles the CVR portable mirror", _mirrorEnabled);
            _mirrorToggleUI.OnValueUpdated += b =>
            {
                _mirrorEnabled = b;
                _mirrorPref.Value = b;
                MelonPreferences.Save();
                ToggleMirror(b);
            };

            _antiPortalToggleUI = parentCategory.AddToggle("Anti-Portal Grief", "Automatically hides portals dropped by other players", _antiPortalEnabled);
            _antiPortalToggleUI.OnValueUpdated += b =>
            {
                _antiPortalEnabled = b;
                _antiPortalPref.Value = b;
                MelonPreferences.Save();
            };

            _notificationsToggleUI = parentCategory.AddToggle("Join/Leave Alerts", "Show toast notifications when players join or leave", _notificationsEnabled);
            _notificationsToggleUI.OnValueUpdated += b =>
            {
                _notificationsEnabled = b;
                _notificationsPref.Value = b;
                MelonPreferences.Save();
            };
        }

        internal static void OnUpdate()
        {
            if (_nightVisionEnabled)
            {
                if (!_isNightVisionActive)
                {
                    _originalAmbientLight = RenderSettings.ambientLight;
                    _originalAmbientMode = RenderSettings.ambientMode;
                    _isNightVisionActive = true;
                }
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = Color.white;
            }
            else if (_isNightVisionActive)
            {
                RenderSettings.ambientLight = _originalAmbientLight;
                RenderSettings.ambientMode = _originalAmbientMode;
                _isNightVisionActive = false;
            }

            if (_antiPortalEnabled && Time.time >= _nextPortalCheckTime)
            {
                _nextPortalCheckTime = Time.time + 1.0f;
                try
                {
                    var portals = UnityEngine.Object.FindObjectsOfType<CVRPortalMarker>();
                    foreach (var portal in portals)
                    {
                        if (portal != null && portal.gameObject.activeInHierarchy)
                        {
                            portal.gameObject.SetActive(false);
                        }
                    }
                }
                catch { }
            }
        }

        private static void ToggleFlashlight(bool enable)
        {
            if (enable)
            {
                if (_flashlightObj == null)
                {
                    _flashlightObj = new GameObject("HatClient_Flashlight");
                    var light = _flashlightObj.AddComponent<Light>();
                    light.type = LightType.Spot;
                    light.range = 50f;
                    light.spotAngle = 60f;
                    light.intensity = 1.5f;
                    light.color = Color.white;
                    
                    var cam = Camera.main;
                    if (cam != null)
                    {
                        _flashlightObj.transform.SetParent(cam.transform);
                        _flashlightObj.transform.localPosition = Vector3.zero;
                        _flashlightObj.transform.localRotation = Quaternion.identity;
                    }
                }
                _flashlightObj.SetActive(true);
            }
            else
            {
                if (_flashlightObj != null)
                {
                    _flashlightObj.SetActive(false);
                }
            }
        }

        private static void ToggleNightVision(bool enable)
        {
            if (!enable && _isNightVisionActive)
            {
                RenderSettings.ambientLight = _originalAmbientLight;
                RenderSettings.ambientMode = _originalAmbientMode;
                _isNightVisionActive = false;
            }
        }

        private static void ToggleMirror(bool enable)
        {
            try
            {
                var menuManager = CVR_MenuManager.Instance;
                if (menuManager != null)
                {
                    var field = typeof(CVR_MenuManager).GetField("_portableMirrorInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var mirrorInstance = field.GetValue(menuManager) as MonoBehaviour;
                        if (mirrorInstance != null)
                        {
                            mirrorInstance.gameObject.SetActive(enable);
                            if (enable)
                            {
                                var playerSetup = PlayerSetup.Instance;
                                if (playerSetup != null)
                                {
                                    mirrorInstance.transform.position = playerSetup.transform.position + playerSetup.transform.forward * 2f + Vector3.up * 1f;
                                    mirrorInstance.transform.rotation = playerSetup.transform.rotation;
                                    mirrorInstance.transform.Rotate(0, 180, 0);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to toggle mirror: {ex.Message}");
            }
        }

        private static void OnUserJoin(CVRPlayerEntity player)
        {
            if (_notificationsEnabled && player != null)
            {
                QuickMenuAPI.ShowAlertToast($"{player.Username} joined the world", 3);
            }
        }

        private static void OnUserLeave(CVRPlayerEntity player)
        {
            if (_notificationsEnabled && player != null)
            {
                QuickMenuAPI.ShowAlertToast($"{player.Username} left the world", 3);
            }
        }
    }
}

