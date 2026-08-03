using System;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using ABI_RC.Core.Player;
using MelonLoader;
using UnityEngine;

namespace BTKUILib.Features
{

    internal static class FlightModule
    {
        private static bool _flightEnabled;
        private static bool _noclipEnabled;
        private static bool _speedhackEnabled;
        private static float _speedMultiplier = 2f;

        private static MelonPreferences_Entry<float> _speedPref;
        private static MelonPreferences_Entry<bool> _noclipPref;

        private static ToggleButton _flightToggleUI;
        private static ToggleButton _noclipToggleUI;
        private static ToggleButton _speedhackToggleUI;

        private static float _baseWalkSpeed = -1f;
        private static float _baseFlySpeed = -1f;

        internal static void Init(MelonPreferences_Category prefCategory)
        {
            _speedPref = prefCategory.CreateEntry("SpeedMultiplier", 2f, "Speed Multiplier", "Multiplier for walk/fly speed");
            _noclipPref = prefCategory.CreateEntry("NoclipEnabled", false, "Noclip", "Disable collisions while flying");

            _speedMultiplier = _speedPref.Value;
            _noclipEnabled = _noclipPref.Value;

            KeybindModule.RegisterKeybind("Toggle Flight", KeyCode.F, KeyCode.LeftControl, ToggleFlight);
        }

        internal static void GenerateUI(Category parentCategory)
        {
            _flightToggleUI = parentCategory.AddToggle("Flight Mode", "Toggle free-flight movement (LCtrl+F)", _flightEnabled);
            _flightToggleUI.OnValueUpdated += b =>
            {
                if (b) EnableFlight();
                else DisableFlight();
            };

            _noclipToggleUI = parentCategory.AddToggle("Noclip", "Disable collisions while flying", _noclipPref.Value);
            _noclipToggleUI.OnValueUpdated += b =>
            {
                _noclipEnabled = b;
                _noclipPref.Value = b;
                MelonPreferences.Save();

                if (_flightEnabled)
                    ApplyNoclip(_noclipEnabled);
            };

            var speedSlider = parentCategory.AddSlider("Speed Multiplier", "Adjust speed multiplier", _speedPref.Value, 1f, 10f, 1, 2f, true);
            speedSlider.Hidden = !_speedhackEnabled;

            _speedhackToggleUI = parentCategory.AddToggle("Speedhack", "Multiply walk and flight speed", _speedhackEnabled);
            _speedhackToggleUI.OnValueUpdated += b =>
            {
                _speedhackEnabled = b;
                speedSlider.Hidden = !b;
                ApplySpeedhack(b);
            };

            speedSlider.OnValueUpdated += f =>
            {
                _speedMultiplier = f;
                _speedPref.Value = f;
                MelonPreferences.Save();

                if (_speedhackEnabled)
                    ApplySpeedhack(true);
            };
        }

        internal static void OnUpdate()
        {

        }

        private static void ToggleFlight()
        {
            if (_flightEnabled)
                DisableFlight();
            else
                EnableFlight();
        }

        private static void EnableFlight()
        {
            try
            {
                var playerSetup = PlayerSetup.Instance;
                if (playerSetup == null || playerSetup.CharacterController == null) return;

                _flightEnabled = true;

      
                playerSetup.CharacterController.ChangeFlight(true, true);

                if (_noclipEnabled)
                    ApplyNoclip(true);

                if (_flightToggleUI != null)
                    _flightToggleUI.ToggleValue = true;

                QuickMenuAPI.ShowAlertToast("Flight Enabled", 2);
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to enable flight: {ex}");
            }
        }

        private static void DisableFlight()
        {
            _flightEnabled = false;

            try
            {
                var playerSetup = PlayerSetup.Instance;
                if (playerSetup == null || playerSetup.CharacterController == null) return;

                playerSetup.CharacterController.ChangeFlight(false, true);

                if (_noclipEnabled)
                    ApplyNoclip(false);

                if (_flightToggleUI != null)
                    _flightToggleUI.ToggleValue = false;

                QuickMenuAPI.ShowAlertToast("Flight Disabled", 2);
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to disable flight: {ex}");
            }
        }

        private static void ApplyNoclip(bool enable)
        {
            var playerSetup = PlayerSetup.Instance;
            if (playerSetup == null || playerSetup.CharacterController == null) return;

            if (enable != playerSetup.CharacterController.IsFlyingWithNoClip())
            {
                playerSetup.CharacterController.ToggleFlightNoClip();
            }
        }

        private static void ApplySpeedhack(bool enable)
        {
            var playerSetup = PlayerSetup.Instance;
            if (playerSetup == null || playerSetup.CharacterController == null) return;

            var cc = playerSetup.CharacterController;

            if (enable)
            {
                if (_baseWalkSpeed < 0)
                {
                    _baseWalkSpeed = cc.maxWalkSpeed;
                    _baseFlySpeed = cc.maxFlySpeed;
                }

                cc.maxWalkSpeed = _baseWalkSpeed * _speedMultiplier;
                cc.maxFlySpeed = _baseFlySpeed * _speedMultiplier;
            }
            else
            {
                if (_baseWalkSpeed > 0)
                {
                    cc.maxWalkSpeed = _baseWalkSpeed;
                    cc.maxFlySpeed = _baseFlySpeed;
                }
            }
        }
    }
}
