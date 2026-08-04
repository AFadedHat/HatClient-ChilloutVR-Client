using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Systems.GameEventSystem;
using BTKUILib.Features;
using BTKUILib.UIObjects;
using MelonLoader;

namespace BTKUILib
{
    internal static class BuildInfo
    {
        public const string Name = "HatClient | Internal";
        public const string Author = "TwoFadedHats Team";
        public const string Company = "TwoFadedHats";
        public const string Version = "1.0.1";
    }
    
    internal class BTKUILib : MelonMod
    {
        internal static MelonLogger.Instance Log;
        internal static BTKUILib Instance;
        
        internal Dictionary<string, Page> MLPrefsPages = new();

        private MelonPreferences_Entry<bool> _displayPrefsTab;
        private Page _mlPrefsPage;
        private bool _hookedQuickMenuAPI;
        private bool _hatClientInitialized;

        private MelonPreferences_Category _hatClientPrefs;

        public override void OnInitializeMelon()
        {
            Log = LoggerInstance;
            Instance = this;
            
            Log.Msg("HatClient is starting up!");

            try
            {
                System.Console.Title = "HatClient | Internal | Version: v1.0.1 | ChilloutVR";
            }
            catch (System.Exception ex)
            {
                Log.Warning($"Could not set console title: {ex.Message}");
            }
            
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.PrepareIcon("MelonLoader", "pencil", System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKUILib.Resources.Pencil.png"));
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.PrepareIcon("MelonLoader", "settings", System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKUILib.Resources.Settings.png"));
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.PrepareIcon("MelonLoader", "star", System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKUILib.Resources.Star.png"));
            
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.PrepareIcon("HatClient", "Star", System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKUILib.Resources.Star.png"));
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.PrepareIcon("HatClient", "Settings", System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKUILib.Resources.Settings.png"));
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.PrepareIcon("HatClient", "Pencil", System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("BTKUILib.Resources.Pencil.png"));

            _hatClientPrefs = MelonPreferences.CreateCategory("HatClient", "HatClient");
            _displayPrefsTab = _hatClientPrefs.CreateEntry("DisplayPrefsTab", false, "Display MelonPrefs Tab", "Sets if the MelonLoader Prefs tab should be displayed");
            _displayPrefsTab.OnEntryValueChanged.Subscribe((b1, _) =>
            {
                if (_mlPrefsPage != null)
                    _mlPrefsPage.HideTab = b1;
            });

         
            InitHatClientFeatures();
            
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OnMenuRegenerate += OnMenuRegenerate;
        }

      
        public override void OnUpdate()
        {
            KeybindModule.PollKeybinds();
            FlightModule.OnUpdate();
            StatsOverlayModule.OnUpdate();
            VisualsModule.OnUpdate();
            TeleportPropsModule.OnUpdate();
            MiscModule.OnUpdate();
            ExploitsModule.OnUpdate();
        }

  
        public override void OnGUI()
        {
            StatsOverlayModule.OnGUI();
        }

        private void InitHatClientFeatures()
        {
            Log.Msg("Initializing HatClient features...");

            KeybindModule.Init(_hatClientPrefs);
            FlightModule.Init(_hatClientPrefs);
            BookmarksModule.Init();
            StatsOverlayModule.Init(_hatClientPrefs);
            TeleportPropsModule.Init();
            VisualsModule.Init(_hatClientPrefs);
            MiscModule.Init(_hatClientPrefs);
            ExploitsModule.Init(_hatClientPrefs);

            Log.Msg("HatClient features initialized!");
        }

        private void GenerateHatClientTab()
        {
            if (_hatClientInitialized) return;
            _hatClientInitialized = true;

            Log.Msg("Generating HatClient Quick Menu tab...");

            var hatPage = Page.GetOrCreatePage("HatClient", "Main", true, "Star");
            hatPage.MenuTitle = "HatClient Internal | Version: v1.0.1";
            hatPage.MenuSubtitle = "Made By AFadedHat | Welcome!";


            var flightCat = hatPage.AddCategory("Movements");
            FlightModule.GenerateUI(flightCat);

            var visualsCat = hatPage.AddCategory("Visuals");
            VisualsModule.GenerateUI(visualsCat);

            var exploitsCat = hatPage.AddCategory("Exploits");
            ExploitsModule.GenerateUI(exploitsCat);

            var subPagesCat = hatPage.AddCategory("Misc");
            BookmarksModule.GenerateUI(subPagesCat);
            KeybindModule.GenerateUI(subPagesCat);
            MiscModule.GenerateUI(subPagesCat);
            StatsOverlayModule.GenerateUI(subPagesCat);

            TeleportPropsModule.GenerateUI();

            Log.Msg("HatClient Quick Menu tab generated!");
        }

        private void OnMenuRegenerate(CVR_MenuManager obj)
        {
            QuickMenuAPI.OnMenuRegenerate?.Invoke(obj);

            if (_hookedQuickMenuAPI) return;
            
            _hookedQuickMenuAPI = true;
            
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OnMenuGenerated += manager => QuickMenuAPI.OnMenuGenerated?.Invoke(manager); 
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OnBackAction += (s, s1) => QuickMenuAPI.OnBackAction?.Invoke(s, s1);
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OnOpenedPage +=  (s, s1) => QuickMenuAPI.OnOpenedPage?.Invoke(s, s1);
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OnPlayerEntitySelected += player => QuickMenuAPI.OnPlayerEntitySelected?.Invoke(new UIPlayerObject(player));
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OnPlayerSelected += (s, s1) =>
            {
                QuickMenuAPI.SelectedPlayerID = s1;
                QuickMenuAPI.SelectedPlayerName = s;
                QuickMenuAPI.OnPlayerSelected?.Invoke(s, s1);
            };
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OnTabChange += (s, s1) => QuickMenuAPI.OnTabChange?.Invoke(s, s1);
            ABI_RC.Systems.UI.UILib.QuickMenuAPI.OnWorldLeave += () => QuickMenuAPI.OnWorldLeave?.Invoke();
            CVRGameEventSystem.Player.OnJoinEntity.AddListener(player =>
            {
                QuickMenuAPI.UserJoin?.Invoke(player);
            });
            CVRGameEventSystem.Player.OnLeaveEntity.AddListener(player =>
            {
                QuickMenuAPI.UserLeave?.Invoke(player);
            });
            
            GenerateSettingsPage();
            GenerateMlPrefsTab();

            GenerateHatClientTab();
        }

        private void GenerateSettingsPage()
        {
            var uiSettingsMainCat = UIUtils.GetInternalSettingsPage();

            if (uiSettingsMainCat == null)
            {
                Log.Error("Unable to get internal UILib settings main category!");
                return;
            }
         

            var prefsTabDisplay = uiSettingsMainCat.AddToggle("Show ML Prefs Tab", "Displays the MelonLoader prefs tab", _displayPrefsTab.Value);
            prefsTabDisplay.OnValueUpdated += b =>
            {
                _displayPrefsTab.Value = b;
                MelonPreferences.Save();
            };
        }

        
        private void GenerateMlPrefsTab()
        {
            if(_mlPrefsPage != null) return;

            _mlPrefsPage = Page.GetOrCreatePage("MelonLoader", "Prefs", true, "Settings");
            _mlPrefsPage.MenuTitle = "MelonLoader Preferences";
            _mlPrefsPage.MenuSubtitle = "Control your MelonLoader Preferences from other mods!";
            _mlPrefsPage.SetProtected(true);
            _mlPrefsPage.HideTab = !_displayPrefsTab.Value;

            var prefCat = _mlPrefsPage.AddCategory("Categories");

            MLPrefsPages.Clear();

            foreach (var category in MelonPreferences.Categories.OrderBy(x => x.DisplayName))
            {
                var page = prefCat.AddPage(category.DisplayName, "Star", $"Opens the preferences category for {category.DisplayName}", "MelonLoader");
                MLPrefsPages.Add(category.Identifier, page);
                var pageCat = page.AddCategory("Preferences");

                foreach (var pref in category.Entries)
                {
                    if (pref.GetReflectedType() == typeof(bool))
                    {
                        var toggle = pageCat.AddToggle(pref.DisplayName, pref.Description, (bool)pref.BoxedValue);
                        toggle.OnValueUpdated += b =>
                        {
                            pref.BoxedValue = b;
                        };

                        if (pref.GetReflectedType() == typeof(string))
                        {
                            var button = pageCat.AddButton($"Edit {pref.DisplayName}", "Pencil", pref.Description);
                            button.OnPress += () =>
                            {
                                QuickMenuAPI.OpenKeyboard((string)pref.BoxedValue, s =>
                                {
                                    pref.BoxedValue = s;
                                });
                            };
                     
                        }
                      
                    }
                }
            }
        }
    }
}