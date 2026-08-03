using System;
using System.Collections.Generic;
using BTKUILib.UIObjects;
using BTKUILib.UIObjects.Components;
using MelonLoader;
using UnityEngine;

namespace BTKUILib.Features
{
    internal static class KeybindModule
    {
        internal class KeybindEntry
        {
            public string Name;
            public KeyCode Key;
            public KeyCode Modifier;
            public Action OnPressed;
            public MelonPreferences_Entry<string> KeyPref;
            public MelonPreferences_Entry<string> ModifierPref;
            public Button UiButton;
        }

        private static readonly List<KeybindEntry> _keybinds = new();
        private static Page _keybindPage;
        private static Category _keybindCategory;
        private static MelonPreferences_Category _prefCategory;

        internal static void Init(MelonPreferences_Category prefCategory)
        {
            _prefCategory = prefCategory;
        }

        internal static void RegisterKeybind(string name, KeyCode defaultKey, KeyCode defaultModifier, Action onPressed)
        {
            var keyPref = _prefCategory.CreateEntry($"Keybind_{name}_Key", defaultKey.ToString(), $"{name} Key", $"Key for {name}");
            var modPref = _prefCategory.CreateEntry($"Keybind_{name}_Modifier", defaultModifier.ToString(), $"{name} Modifier", $"Modifier key for {name} (None for no modifier)");

            if (!Enum.TryParse<KeyCode>(keyPref.Value, out var parsedKey))
                parsedKey = defaultKey;
            if (!Enum.TryParse<KeyCode>(modPref.Value, out var parsedMod))
                parsedMod = defaultModifier;

            _keybinds.Add(new KeybindEntry
            {
                Name = name,
                Key = parsedKey,
                Modifier = parsedMod,
                OnPressed = onPressed,
                KeyPref = keyPref,
                ModifierPref = modPref
            });
        }

        internal static void PollKeybinds()
        {
            foreach (var kb in _keybinds)
            {
                if (!Input.GetKeyDown(kb.Key)) continue;

                if (kb.Modifier != KeyCode.None && !Input.GetKey(kb.Modifier))
                    continue;

                try
                {
                    kb.OnPressed?.Invoke();
                }
                catch (Exception ex)
                {
                    BTKUILib.Log.Error($"Error executing keybind '{kb.Name}': {ex}");
                }
            }
        }

        internal static void GenerateUI(Category parentCategory)
        {
            _keybindPage = parentCategory.AddPage("Keybinds", "Settings", "Configure your HatClient keybinds", "HatClient");
            _keybindPage.MenuTitle = "HatClient Keybinds";
            _keybindPage.MenuSubtitle = "Click a keybind to remap it";

            _keybindCategory = _keybindPage.AddCategory("Registered Keybinds");

            foreach (var kb in _keybinds)
            {
                var displayText = FormatKeybindDisplay(kb);
                var button = _keybindCategory.AddButton(displayText, "Settings", $"Click to remap {kb.Name}", ButtonStyle.TextOnly);
                kb.UiButton = button;

                var entry = kb;
                button.OnPress += () =>
                {
                    QuickMenuAPI.OpenKeyboard(entry.Key.ToString(), newKeyStr =>
                    {
                        if (Enum.TryParse<KeyCode>(newKeyStr, true, out var newKey))
                        {
                            entry.Key = newKey;
                            entry.KeyPref.Value = newKey.ToString();
                            MelonPreferences.Save();
                            entry.UiButton.ButtonText = FormatKeybindDisplay(entry);
                            QuickMenuAPI.ShowAlertToast($"{entry.Name} rebound to {FormatKeybindDisplay(entry)}", 3);
                        }
                        else
                        {
                            QuickMenuAPI.ShowAlertToast($"Invalid key: {newKeyStr}", 3);
                        }
                    });
                };
            }
        }

        private static string FormatKeybindDisplay(KeybindEntry kb)
        {
            if (kb.Modifier != KeyCode.None)
                return $"{kb.Name}: {kb.Modifier} + {kb.Key}";
            return $"{kb.Name}: {kb.Key}";
        }
    }
}
