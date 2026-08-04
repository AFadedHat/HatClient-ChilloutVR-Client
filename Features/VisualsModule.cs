using System;
using System.Collections.Generic;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using ABI_RC.Core.Player;
using MelonLoader;
using UnityEngine;
using UnityEngine.Rendering;

namespace BTKUILib.Features
{
    internal static class VisualsModule
    {
        private static bool _espEnabled;
        private static bool _nameplatesEnabled;
        private static MelonPreferences_Entry<bool> _espPref;
        private static MelonPreferences_Entry<bool> _nameplatesPref;
        
        private static ToggleButton _espToggleUI;
        private static ToggleButton _nameplatesToggleUI;
        
        private static Material _espMaterial;
        
        private static Dictionary<string, GameObject> _activeCapsules = new Dictionary<string, GameObject>();

        internal static void Init(MelonPreferences_Category prefCategory)
        {
            _espPref = prefCategory.CreateEntry("ESPEnabled", false, "ESP", "See players through walls");
            _nameplatesPref = prefCategory.CreateEntry("ESPNameplatesEnabled", false, "ESP Nameplates", "Show player names above ESP");
            
            _espEnabled = _espPref.Value;
            _nameplatesEnabled = _nameplatesPref.Value;

            try
            {
                var shader = Shader.Find("Hidden/Internal-Colored");
                if (shader != null)
                {
                    _espMaterial = new Material(shader);
                    _espMaterial.hideFlags = HideFlags.HideAndDontSave;
                    _espMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    _espMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    _espMaterial.SetInt("_Cull", (int)CullMode.Off);
                    _espMaterial.SetInt("_ZWrite", 0);
                    _espMaterial.SetInt("_ZTest", (int)CompareFunction.Always);
                    _espMaterial.color = Color.magenta;
                }
                else
                {
                    BTKUILib.Log.Warning("Hidden/Internal-Colored shader not found. ESP may not render through walls.");
                    _espMaterial = new Material(Shader.Find("Standard"));
                    _espMaterial.color = Color.magenta;
                }
            }
            catch (Exception ex)
            {
                BTKUILib.Log.Error($"Failed to initialize ESP material: {ex}");
            }

            QuickMenuAPI.UserLeave += OnUserLeave;
        }

        internal static void GenerateUI(Category parentCategory)
        {
            _espToggleUI = parentCategory.AddToggle("Capsule ESP", "Draws a capsule around players visible through walls", _espEnabled);
            _espToggleUI.OnValueUpdated += b =>
            {
                _espEnabled = b;
                _espPref.Value = b;
                MelonPreferences.Save();

                if (!_espEnabled && !_nameplatesEnabled)
                {
                    ClearAllCapsules();
                }
            };

            _nameplatesToggleUI = parentCategory.AddToggle("ESP Nameplates", "Shows player names above their ESP capsules", _nameplatesEnabled);
            _nameplatesToggleUI.OnValueUpdated += b =>
            {
                _nameplatesEnabled = b;
                _nameplatesPref.Value = b;
                MelonPreferences.Save();

                if (!_espEnabled && !_nameplatesEnabled)
                {
                    ClearAllCapsules();
                }
            };
        }

        internal static void OnUpdate()
        {
            if (!_espEnabled && !_nameplatesEnabled) return;

            try
            {
                var playerManager = CVRPlayerManager.Instance;
                if (playerManager == null || playerManager.NetworkPlayers == null) return;

                var validIds = new HashSet<string>();

                foreach (var player in playerManager.NetworkPlayers)
                {
                    if (player == null || player.PlayerObject == null || string.IsNullOrEmpty(player.Uuid)) continue;
                    
                    validIds.Add(player.Uuid);

                    if (!_activeCapsules.ContainsKey(player.Uuid))
                    {
                        var cap = new GameObject($"HatClient_ESP_{player.Uuid}");
                        
                        // LineRenderer Capsule
                        var lr = cap.AddComponent<LineRenderer>();
                        if (_espMaterial != null)
                            lr.material = _espMaterial;
                            
                        lr.useWorldSpace = false;
                        lr.loop = true;
                        lr.startWidth = 0.04f;
                        lr.endWidth = 0.04f;
                        lr.positionCount = 32;

                        Vector3[] points = new Vector3[32];
                        float radius = 0.5f;
                        float halfHeight = 0.5f;

                        for (int i = 0; i < 16; i++)
                        {
                            float angle = Mathf.PI * (i / 15f); // 0 to PI
                            points[i] = new Vector3(Mathf.Cos(angle) * radius, halfHeight + Mathf.Sin(angle) * radius, 0);
                        }
                        for (int i = 0; i < 16; i++)
                        {
                            float angle = Mathf.PI + Mathf.PI * (i / 15f); // PI to 2PI
                            points[i + 16] = new Vector3(Mathf.Cos(angle) * radius, -halfHeight + Mathf.Sin(angle) * radius, 0);
                        }
                        lr.SetPositions(points);

                        // TextMesh Nameplate
                        var textObj = new GameObject("Nameplate");
                        textObj.transform.SetParent(cap.transform);
                        textObj.transform.localPosition = new Vector3(0, 1.2f, 0); // Hover above capsule
                        
                        var tm = textObj.AddComponent<TextMesh>();
                        tm.text = player.Username;
                        tm.characterSize = 0.05f;
                        tm.fontSize = 64;
                        tm.anchor = TextAnchor.MiddleCenter;
                        tm.alignment = TextAlignment.Center;
                        tm.color = Color.magenta;

                        var textRenderer = textObj.GetComponent<MeshRenderer>();
                        if (textRenderer != null && textRenderer.sharedMaterial != null)
                        {
                            var fontMat = new Material(textRenderer.sharedMaterial);
                            fontMat.SetInt("_ZTest", (int)CompareFunction.Always);
                            textRenderer.sharedMaterial = fontMat;
                        }
                        
                        _activeCapsules[player.Uuid] = cap;
                    }

                    var capObj = _activeCapsules[player.Uuid];
                    capObj.transform.position = player.PlayerObject.transform.position + new Vector3(0, 1f, 0);

                    var lineRenderer = capObj.GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                        lineRenderer.enabled = _espEnabled;

                    var nameplateObj = capObj.transform.Find("Nameplate");
                    if (nameplateObj != null)
                    {
                        var nameplateTm = nameplateObj.GetComponent<TextMesh>();
                        if (nameplateTm != null)
                        {
                            nameplateObj.gameObject.SetActive(_nameplatesEnabled);
                            if (_nameplatesEnabled)
                                nameplateTm.text = player.Username;
                        }
                    }
                    
                    var camera = Camera.main;
                    if (camera != null)
                    {
                        capObj.transform.rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
                    }
                }

                var toRemove = new List<string>();
                foreach (var kvp in _activeCapsules)
                {
                    if (!validIds.Contains(kvp.Key))
                    {
                        if (kvp.Value != null)
                            UnityEngine.Object.Destroy(kvp.Value);
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (var id in toRemove)
                {
                    _activeCapsules.Remove(id);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private static void OnUserLeave(CVRPlayerEntity player)
        {
            if (player != null && !string.IsNullOrEmpty(player.Uuid))
            {
                if (_activeCapsules.TryGetValue(player.Uuid, out var cap))
                {
                    if (cap != null)
                        UnityEngine.Object.Destroy(cap);
                    _activeCapsules.Remove(player.Uuid);
                }
            }
        }

        private static void ClearAllCapsules()
        {
            foreach (var cap in _activeCapsules.Values)
            {
                if (cap != null)
                    UnityEngine.Object.Destroy(cap);
            }
            _activeCapsules.Clear();
        }
    }
}
