using System;
using System.Collections.Generic;
using System.Linq;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using ABI_RC.Core.Player;
using UnityEngine;

namespace BTKUILib.Features
{

    internal static class TeleportPropsModule
    {
        private static bool _orbitEnabled;
        private static string _orbitTargetId;
        private static ToggleButton _orbitToggleBtn;
          
        private static List<Transform> _cachedProps = new List<Transform>();
        private static float _orbitRadius = 2f;
        private static float _orbitSpeed = 1f;
        private static float _orbitHeight = 1.5f;

        internal static void Init()
        {
            QuickMenuAPI.UserLeave += OnUserLeave;
        }

        internal static void GenerateUI()
        {
            try
            {
                var playerPage = QuickMenuAPI.PlayerSelectPage;
                if (playerPage == null)
                {
                    BTKUILib.Log.Warning("PlayerSelectPage is null, TeleportProps UI not created");
                    return;
                }

                var hatCat = playerPage.AddCategory("HatClient", "HatClient");

                var teleportBtn = hatCat.AddButton("Teleport Props", "Star",
                    "Teleport all spawned props to this player's position");
                teleportBtn.OnPress += TeleportAllPropsToSelectedPlayer;

                var teleportToBtn = hatCat.AddButton("Teleport to Player", "Star",
                    "Teleport yourself to this player's position");
                teleportToBtn.OnPress += TeleportToSelectedPlayer;

                _orbitToggleBtn = hatCat.AddToggle("Orbit Props", "Make all props spin around this player", false);
                
                var radiusSlider = hatCat.AddSlider("Orbit Radius", "Adjust the radius of the prop orbit", 2f, 0.5f, 10f, 1, 2f, true);
                radiusSlider.Hidden = true;
                
                radiusSlider.OnValueUpdated += f =>
                {
                    _orbitRadius = f;
                };

                var heightSlider = hatCat.AddSlider("Orbit Height", "Adjust the height of the prop orbit", 1.5f, -2f, 5f, 1, 1.5f, true);
                heightSlider.Hidden = true;

                heightSlider.OnValueUpdated += f =>
                {
                    _orbitHeight = f;
                };

                _orbitToggleBtn.OnValueUpdated += b =>
                {
                    _orbitEnabled = b;
                    radiusSlider.Hidden = !b;
                    heightSlider.Hidden = !b;
                    
                    if (_orbitEnabled)
                    {
                        _orbitTargetId = QuickMenuAPI.SelectedPlayerID;
                        RefreshCachedProps();
                        QuickMenuAPI.ShowAlertToast($"Orbiting props around {QuickMenuAPI.SelectedPlayerName}", 2);
                    }
                    else
                    {
                        _orbitTargetId = null;
                        _cachedProps.Clear();
                        QuickMenuAPI.ShowAlertToast("Prop orbit stopped", 2);
                    }
                };

                QuickMenuAPI.OnPlayerSelected += (name, id) =>
                {
                    if (_orbitToggleBtn != null)
                    {
                        bool isOrbitingThisPlayer = (_orbitEnabled && _orbitTargetId == id);
                        _orbitToggleBtn.ToggleValue = isOrbitingThisPlayer;
                        radiusSlider.Hidden = !isOrbitingThisPlayer;
                        heightSlider.Hidden = !isOrbitingThisPlayer;
                    }
                };
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to create TeleportProps UI: {ex}");
            }
        }

        internal static void OnUpdate()
        {
            if (!_orbitEnabled || string.IsNullOrEmpty(_orbitTargetId)) return;

            try
            {
                var targetPos = GetPlayerPosition(_orbitTargetId);
                if (!targetPos.HasValue) return;

                _cachedProps.RemoveAll(p => p == null);
                
                if (_cachedProps.Count == 0) return;

                float time = Time.time * _orbitSpeed;
                float angleStep = (Mathf.PI * 2f) / _cachedProps.Count;

                for (int i = 0; i < _cachedProps.Count; i++)
                {
                    var prop = _cachedProps[i];
                    if (prop == null) continue;

                    float currentAngle = time + (i * angleStep);
                    float x = Mathf.Sin(currentAngle) * _orbitRadius;
                    float z = Mathf.Cos(currentAngle) * _orbitRadius;

                    prop.position = targetPos.Value + new Vector3(x, _orbitHeight, z);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private static void OnUserLeave(CVRPlayerEntity player)
        {
            if (_orbitEnabled && player != null && player.Uuid == _orbitTargetId)
            {
                _orbitEnabled = false;
                _orbitTargetId = null;
                _cachedProps.Clear();
                if (_orbitToggleBtn != null)
                    _orbitToggleBtn.ToggleValue = false;
            }
        }

        private static void RefreshCachedProps()
        {
            _cachedProps.Clear();
            
            try
            {
                var spawnableType = Type.GetType("ABI_RC.Core.Savior.CVRSpawnable, Assembly-CSharp")
                                    ?? Type.GetType("ABI_RC.Core.CVRSpawnable, Assembly-CSharp");

                if (spawnableType != null)
                {
                    var spawnables = UnityEngine.Object.FindObjectsOfType(spawnableType);
                    foreach (var spawnable in spawnables)
                    {
                        if (spawnable is Component comp)
                            _cachedProps.Add(comp.transform);
                    }
                }

                if (_cachedProps.Count == 0)
                {
                    var allObjects = UnityEngine.Object.FindObjectsOfType<Transform>();
                    foreach (var t in allObjects)
                    {
                        if (t.name.Contains("CVRSpawnable") || t.name.Contains("Spawnable"))
                        {
                            _cachedProps.Add(t);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Error caching props for orbit: {ex}");
            }
        }

        private static void TeleportAllPropsToSelectedPlayer()
        {
            try
            {
                var targetPlayerName = QuickMenuAPI.SelectedPlayerName;
                var targetPlayerId = QuickMenuAPI.SelectedPlayerID;

                if (string.IsNullOrEmpty(targetPlayerId))
                {
                    QuickMenuAPI.ShowAlertToast("No player selected!", 3);
                    return;
                }

                var targetPos = GetPlayerPosition(targetPlayerId);
                if (!targetPos.HasValue)
                {
                    QuickMenuAPI.ShowAlertToast($"Could not find player position for {targetPlayerName}", 3);
                    return;
                }

                RefreshCachedProps();
                
                foreach (var prop in _cachedProps)
                {
                    if (prop != null)
                        prop.position = targetPos.Value;
                }

                if (_cachedProps.Count > 0)
                    QuickMenuAPI.ShowAlertToast($"Teleported {_cachedProps.Count} prop(s) to {targetPlayerName}", 3);
                else
                    QuickMenuAPI.ShowAlertToast("No props found in scene", 3);
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to teleport props: {ex}");
                QuickMenuAPI.ShowAlertToast("Error teleporting props", 3);
            }
        }

        private static void TeleportToSelectedPlayer()
        {
            try
            {
                var targetPlayerName = QuickMenuAPI.SelectedPlayerName;
                var targetPlayerId = QuickMenuAPI.SelectedPlayerID;

                if (string.IsNullOrEmpty(targetPlayerId))
                {
                    QuickMenuAPI.ShowAlertToast("No player selected!", 3);
                    return;
                }

                var targetPos = GetPlayerPosition(targetPlayerId);
                if (!targetPos.HasValue)
                {
                    QuickMenuAPI.ShowAlertToast($"Could not find player position for {targetPlayerName}", 3);
                    return;
                }

                var playerSetup = PlayerSetup.Instance;
                if (playerSetup != null && playerSetup.CharacterController != null)
                {
                    playerSetup.CharacterController.TeleportPosition(targetPos.Value);
                    QuickMenuAPI.ShowAlertToast($"Teleported to {targetPlayerName}", 3);
                }
                else
                {
                    QuickMenuAPI.ShowAlertToast("PlayerSetup not found", 3);
                }
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to teleport to player: {ex}");
                QuickMenuAPI.ShowAlertToast("Error teleporting to player", 3);
            }
        }

        private static Vector3? GetPlayerPosition(string playerId)
        {
            try
            {
                var playerManager = CVRPlayerManager.Instance;
                if (playerManager == null) return null;

                if (playerManager.UserIdToPlayerEntity != null &&
                    playerManager.UserIdToPlayerEntity.TryGetValue(playerId, out var entity))
                {
                    if (entity?.PlayerObject != null)
                        return entity.PlayerObject.transform.position;
                }

                var networkPlayers = playerManager.NetworkPlayers;
                if (networkPlayers == null) return null;

                foreach (var player in networkPlayers)
                {
                    if (player == null) continue;
                    if (player.Uuid == playerId && player.PlayerObject != null)
                        return player.PlayerObject.transform.position;
                }

                return null;
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Error finding player position: {ex}");
                return null;
            }
        }
    }
}
