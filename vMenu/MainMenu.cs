using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.menus;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient
{
    public class MainMenu : BaseScript
    {
        #region Variables

        public static bool PermissionsSetupComplete => ArePermissionsSetup;
        public static bool ConfigOptionsSetupComplete = false;

        public static string MenuToggleKey { get; private set; } = "M"; // Par défaut : M
        public static string NoClipKey { get; private set; } = "F2"; // Par défaut : F2
        public static Menu Menu { get; private set; }
        public static Menu PlayerSubmenu { get; private set; }
        public static Menu VehicleSubmenu { get; private set; }
        public static Menu WorldSubmenu { get; private set; }

        public static PlayerOptions PlayerOptionsMenu { get; private set; }
        public static OnlinePlayers OnlinePlayersMenu { get; private set; }
        public static BannedPlayers BannedPlayersMenu { get; private set; }
        public static SavedVehicles SavedVehiclesMenu { get; private set; }
        public static PersonalVehicle PersonalVehicleMenu { get; private set; }
        public static VehicleOptions VehicleOptionsMenu { get; private set; }
        public static VehicleSpawner VehicleSpawnerMenu { get; private set; }
        public static PlayerAppearance PlayerAppearanceMenu { get; private set; }
        public static MpPedCustomization MpPedCustomizationMenu { get; private set; }
        public static TimeOptions TimeOptionsMenu { get; private set; }
        public static WeatherOptions WeatherOptionsMenu { get; private set; }
        public static WeaponOptions WeaponOptionsMenu { get; private set; }
        public static WeaponLoadouts WeaponLoadoutsMenu { get; private set; }
        public static Recording RecordingMenu { get; private set; }
        public static MiscSettings MiscSettingsMenu { get; private set; }
        public static VoiceChat VoiceChatSettingsMenu { get; private set; }
        public static About AboutMenu { get; private set; }
        public static bool NoClipEnabled { get { return NoClip.IsNoclipActive(); } set { NoClip.SetNoclipActive(value); } }
        public static IPlayerList PlayersList;

        public static bool DebugMode = GetResourceMetadata(GetCurrentResourceName(), "client_debug_mode", 0) == "true";
        public static bool EnableExperimentalFeatures = (GetResourceMetadata(GetCurrentResourceName(), "experimental_features_enabled", 0) ?? "0") == "1";
        public static string Version { get { return GetResourceMetadata(GetCurrentResourceName(), "version", 0); } }

        public static bool DontOpenMenus { get { return MenuController.DontOpenAnyMenu; } set { MenuController.DontOpenAnyMenu = value; } }
        public static bool DisableControls { get { return MenuController.DisableMenuButtons; } set { MenuController.DisableMenuButtons = value; } }

        public static bool MenuEnabled { get; private set; } = true;

        private const int currentCleanupVersion = 2;
        #endregion

        /// <summary>
        /// Constructeur.
        /// </summary>
        public MainMenu()
        {
            PlayersList = new NativePlayerList(Players);

            #region Nettoyage des KVP inutilisés
            var tmp_kvp_handle = StartFindKvp("");
            var cleanupVersionChecked = false;
            var tmp_kvp_names = new List<string>();
            while (true)
            {
                var k = FindKvp(tmp_kvp_handle);
                if (string.IsNullOrEmpty(k))
                {
                    break;
                }
                if (k == "vmenu_cleanup_version")
                {
                    if (GetResourceKvpInt("vmenu_cleanup_version") >= currentCleanupVersion)
                    {
                        cleanupVersionChecked = true;
                    }
                }
                tmp_kvp_names.Add(k);
            }
            EndFindKvp(tmp_kvp_handle);

            if (!cleanupVersionChecked)
            {
                SetResourceKvpInt("vmenu_cleanup_version", currentCleanupVersion);
                foreach (var kvp in tmp_kvp_names)
                {
#pragma warning disable CS8793 // L'expression donnée correspond toujours au modèle fourni.
                    if (currentCleanupVersion is 1 or 2)
                    {
                        if (!kvp.StartsWith("settings_") && !kvp.StartsWith("vmenu") && !kvp.StartsWith("veh_") && !kvp.StartsWith("ped_") && !kvp.StartsWith("mp_ped_"))
                        {
                            DeleteResourceKvp(kvp);
                            Debug.WriteLine($"[vMenu] [nettoyage id: 1] KVP inutilisé supprimé : {kvp}.");
                        }
                    }
#pragma warning restore CS8793 // L'expression donnée correspond toujours au modèle fourni.
                    if (currentCleanupVersion == 2)
                    {
                        if (kvp.StartsWith("mp_char"))
                        {
                            DeleteResourceKvp(kvp);
                            Debug.WriteLine($"[vMenu] [nettoyage id: 2] KVP inutilisé supprimé : {kvp}.");
                        }
                    }
                }
                Debug.WriteLine("[vMenu] Nettoyage des KVP inutilisés terminé.");
            }
            #endregion

            #region Mapping des touches
            string KeyMappingID = String.IsNullOrWhiteSpace(GetSettingsString(Setting.vmenu_keymapping_id)) ? "Default" : GetSettingsString(Setting.vmenu_keymapping_id);
            RegisterCommand($"vMenu:{KeyMappingID}:NoClip", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {
                if (IsAllowed(Permission.NoClip))
                {
                    if (Game.PlayerPed.IsInVehicle())
                    {
                        var veh = GetVehicle();
                        if (veh != null && veh.Exists() && veh.Driver == Game.PlayerPed)
                        {
                            NoClipEnabled = !NoClipEnabled;
                        }
                        else
                        {
                            NoClipEnabled = false;
                            Notify.Error("Ce véhicule n'existe pas (d'une manière ou d'une autre) ou vous devez être le conducteur pour activer le noclip !");
                        }
                    }
                    else
                    {
                        NoClipEnabled = !NoClipEnabled;
                    }
                }
            }), false);
            RegisterCommand($"vMenu:{KeyMappingID}:MenuToggle", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {
                if (MenuEnabled)
                {
                    if (!MenuController.IsAnyMenuOpen())
                    {
                        Menu.OpenMenu();
                    }
                    else
                    {
                        MenuController.CloseAllMenus();
                    }
                }
            }), false);

            if (!(GetSettingsString(Setting.vmenu_noclip_toggle_key) == null))
            {
                NoClipKey = GetSettingsString(Setting.vmenu_noclip_toggle_key);
            }
            else
            {
                NoClipKey = "F2";
            }

            if (!(GetSettingsString(Setting.vmenu_menu_toggle_key) == null))
            {
                MenuToggleKey = GetSettingsString(Setting.vmenu_menu_toggle_key);
            }
            else
            {
                MenuToggleKey = "M";
            }
            MenuController.MenuToggleKey = (Control)(-1); // Désactive la touche de bascule du menu
            RegisterKeyMapping($"vMenu:{KeyMappingID}:NoClip", "Bouton de bascule NoClip de vMenu", "keyboard", NoClipKey);
            RegisterKeyMapping($"vMenu:{KeyMappingID}:MenuToggle", "Bouton de bascule de vMenu", "keyboard", MenuToggleKey);
            RegisterKeyMapping($"vMenu:{KeyMappingID}:MenuToggle", "Bouton de bascule de vMenu (manette)", "pad_digitalbuttonany", "start_index");
            #endregion

            if (EnableExperimentalFeatures)
            {
                RegisterCommand("testped", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    var data = Game.PlayerPed.GetHeadBlendData();
                    Debug.WriteLine(JsonConvert.SerializeObject(data, Formatting.Indented));
                }), false);

                RegisterCommand("tattoo", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    if (args != null && args[0] != null && args[1] != null)
                    {
                        Debug.WriteLine(args[0].ToString() + " " + args[1].ToString());
                        TattooCollectionData d = Game.GetTattooCollectionData(int.Parse(args[0].ToString()), int.Parse(args[1].ToString()));
                        Debug.WriteLine("check");
                        Debug.Write(JsonConvert.SerializeObject(d, Formatting.Indented) + "\n");
                    }
                }), false);

                RegisterCommand("clearfocus", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
                {
                    SetNuiFocus(false, false);
                }), false);
            }

            RegisterCommand("vmenuclient", new Action<dynamic, List<dynamic>, string>((dynamic source, List<dynamic> args, string rawCommand) =>
            {
                if (args != null)
                {
                    if (args.Count > 0)
                    {
                        if (args[0].ToString().ToLower() == "debug")
                        {
                            DebugMode = !DebugMode;
                            Notify.Custom($"Le mode debug est maintenant : {DebugMode}.");
                            // Définir la présence Discord riche une fois, permettant à d'autres ressources de la remplacer une fois chargées.
                            if (DebugMode)
                            {
                                SetRichPresence($"Débogage de vMenu {Version} !");
                            }
                            else
                            {
                                SetRichPresence($"Profitez de FiveM !");
                            }
                        }
                        else if (args[0].ToString().ToLower() == "gc")
                        {
                            GC.Collect();
                            Debug.Write("Mémoire libérée.\n");
                        }
                        else if (args[0].ToString().ToLower() == "dump")
                        {
                            Notify.Info("Un dump complet de la configuration sera effectué dans la console. Vérifiez le fichier de log. Cela peut causer du lag !");
                            Debug.WriteLine("\n\n\n########################### vMenu ###########################");
                            Debug.WriteLine($"Exécution de vMenu Version : {Version}, Fonctionnalités expérimentales : {EnableExperimentalFeatures}, Mode debug : {DebugMode}.");
                            Debug.WriteLine("\nDump de la liste de tous les KVP :");
                            var handle = StartFindKvp("");
                            var names = new List<string>();
                            while (true)
                            {
                                var k = FindKvp(handle);
                                if (string.IsNullOrEmpty(k))
                                {
                                    break;
                                }
                                names.Add(k);
                            }
                            EndFindKvp(handle);

                            var kvps = new Dictionary<string, dynamic>();
                            foreach (var kvp in names)
                            {
                                var type = 0; // 0 = string, 1 = float, 2 = int.
                                if (kvp.StartsWith("settings_"))
                                {
                                    if (kvp == "settings_voiceChatProximity") // float
                                    {
                                        type = 1;
                                    }
                                    else if (kvp == "settings_clothingAnimationType") // int
                                    {
                                        type = 2;
                                    }
                                    else if (kvp == "settings_miscLastTimeCycleModifierIndex") // int
                                    {
                                        type = 2;
                                    }
                                    else if (kvp == "settings_miscLastTimeCycleModifierStrength") // int
                                    {
                                        type = 2;
                                    }
                                }
                                else if (kvp == "vmenu_cleanup_version") // int
                                {
                                    type = 2;
                                }
                                switch (type)
                                {
                                    case 0:
                                        var s = GetResourceKvpString(kvp);
                                        if (s.StartsWith("{") || s.StartsWith("["))
                                        {
                                            kvps.Add(kvp, JsonConvert.DeserializeObject(s));
                                        }
                                        else
                                        {
                                            kvps.Add(kvp, GetResourceKvpString(kvp));
                                        }
                                        break;
                                    case 1:
                                        kvps.Add(kvp, GetResourceKvpFloat(kvp));
                                        break;
                                    case 2:
                                        kvps.Add(kvp, GetResourceKvpInt(kvp));
                                        break;
                                }
                            }
                            Debug.WriteLine(@JsonConvert.SerializeObject(kvps, Formatting.None) + "\n");

                            Debug.WriteLine("\n\nDump de la liste des permissions autorisées :");
                            Debug.WriteLine(@JsonConvert.SerializeObject(Permissions, Formatting.None));

                            Debug.WriteLine("\n\nDump des paramètres de configuration du serveur vmenu :");
                            var settings = new Dictionary<string, string>();
                            foreach (var a in Enum.GetValues(typeof(Setting)))
                            {
                                settings.Add(a.ToString(), GetSettingsString((Setting)a));
                            }
                            Debug.WriteLine(@JsonConvert.SerializeObject(settings, Formatting.None));
                            Debug.WriteLine("\nFin du dump de vMenu !");
                            Debug.WriteLine("\n########################### vMenu ###########################");
                        }
                    }
                    else
                    {
                        Notify.Custom($"vMenu est actuellement en version : {Version}.");
                    }
                }
            }), false);

            if (GetCurrentResourceName() != "vMenu")
            {
                MenuController.MainMenu = null;
                MenuController.DontOpenAnyMenu = true;
                MenuController.DisableMenuButtons = true;
                throw new Exception("\n[vMenu] ERREUR D'INSTALLATION !\nLe nom de la ressource n'est pas valide. Veuillez changer le nom du dossier de '" + GetCurrentResourceName() + "' en 'vMenu' (sensible à la casse) !\n");
            }
            else
            {
                Tick += OnTick;
            }

            // Effacer toutes les informations précédentes du menu pause au démarrage de la ressource.
            ClearBrief();

            // Demander les données de permissions au serveur.
            TriggerServerEvent("vMenu:RequestPermissions");

            // Demander l'état du serveur au serveur.
            TriggerServerEvent("vMenu:RequestServerState");
        }

        #region Bits Infinity
        [EventHandler("vMenu:SetServerState")]
        public void SetServerState(IDictionary<string, object> data)
        {
            if (data.TryGetValue("IsInfinity", out var isInfinity))
            {
                if (isInfinity is bool isInfinityBool)
                {
                    if (isInfinityBool)
                    {
                        PlayersList = new InfinityPlayerList(Players);
                    }
                }
            }
        }

        [EventHandler("vMenu:ReceivePlayerList")]
        public void ReceivedPlayerList(IList<object> players)
        {
            PlayersList?.ReceivedPlayerList(players);
        }

        public static async Task<Vector3> RequestPlayerCoordinates(int serverId)
        {
            var coords = Vector3.Zero;
            var completed = false;

            // TODO: remplacer par RPC client<->serveur une fois implémenté dans CitizenFX !
            Func<Vector3, bool> CallbackFunction = (data) =>
            {
                coords = data;
                completed = true;
                return true;
            };

            TriggerServerEvent("vMenu:GetPlayerCoords", serverId, CallbackFunction);

            while (!completed)
            {
                await Delay(0);
            }

            return coords;
        }
        #endregion

        #region Fonction de définition des permissions
        /// <summary>
        /// Définit les permissions pour ce client.
        /// </summary>
        /// <param name="dict"></param>
        public static async void SetPermissions(string permissionsList)
        {
            vMenuShared.PermissionsManager.SetPermissions(permissionsList);

            VehicleSpawner.allowedCategories = new List<bool>()
            {
                IsAllowed(Permission.VSCompacts, checkAnyway: true),
                IsAllowed(Permission.VSSedans, checkAnyway: true),
                IsAllowed(Permission.VSSUVs, checkAnyway: true),
                IsAllowed(Permission.VSCoupes, checkAnyway: true),
                IsAllowed(Permission.VSMuscle, checkAnyway: true),
                IsAllowed(Permission.VSSportsClassic, checkAnyway: true),
                IsAllowed(Permission.VSSports, checkAnyway: true),
                IsAllowed(Permission.VSSuper, checkAnyway: true),
                IsAllowed(Permission.VSMotorcycles, checkAnyway: true),
                IsAllowed(Permission.VSOffRoad, checkAnyway: true),
                IsAllowed(Permission.VSIndustrial, checkAnyway: true),
                IsAllowed(Permission.VSUtility, checkAnyway: true),
                IsAllowed(Permission.VSVans, checkAnyway: true),
                IsAllowed(Permission.VSCycles, checkAnyway: true),
                IsAllowed(Permission.VSBoats, checkAnyway: true),
                IsAllowed(Permission.VSHelicopters, checkAnyway: true),
                IsAllowed(Permission.VSPlanes, checkAnyway: true),
                IsAllowed(Permission.VSService, checkAnyway: true),
                IsAllowed(Permission.VSEmergency, checkAnyway: true),
                IsAllowed(Permission.VSMilitary, checkAnyway: true),
                IsAllowed(Permission.VSCommercial, checkAnyway: true),
                IsAllowed(Permission.VSTrains, checkAnyway: true),
                IsAllowed(Permission.VSOpenWheel, checkAnyway: true)
            };
            ArePermissionsSetup = true;
            while (!ConfigOptionsSetupComplete)
            {
                await Delay(100);
            }
            PostPermissionsSetup();
        }
        #endregion

        /// <summary>
        /// Configure les éléments dès que les permissions sont chargées.
        /// Déclenche la création des menus, la définition des drapeaux initiaux comme le PVP, les statistiques du joueur,
        /// et déclenche la création des fonctions Tick de la classe FunctionsController.
        /// </summary>
        private static void PostPermissionsSetup()
        {
            switch (GetSettingsInt(Setting.vmenu_pvp_mode))
            {
                case 1:
                    NetworkSetFriendlyFireOption(true);
                    SetCanAttackFriendly(Game.PlayerPed.Handle, true, false);
                    break;
                case 2:
                    NetworkSetFriendlyFireOption(false);
                    SetCanAttackFriendly(Game.PlayerPed.Handle, false, false);
                    break;
                case 0:
                default:
                    break;
            }

            static bool canUseMenu()
            {
                if (GetSettingsBool(Setting.vmenu_menu_staff_only) == false)
                {
                    return true;
                }
                else if (IsAllowed(Permission.Staff))
                {
                    return true;
                }

                return false;
            }

            if (!canUseMenu())
            {
                MenuController.MainMenu = null;
                MenuController.DisableMenuButtons = true;
                MenuController.DontOpenAnyMenu = true;
                MenuEnabled = false;
                return;
            }
            // Créer le menu principal.
            Menu = new Menu(Game.Player.Name, "Menu Principal");
            PlayerSubmenu = new Menu(Game.Player.Name, "Options relatives au joueur");
            VehicleSubmenu = new Menu(Game.Player.Name, "Options relatives aux véhicules");
            WorldSubmenu = new Menu(Game.Player.Name, "Options du monde");

            // Ajouter le menu principal au pool de menus.
            MenuController.AddMenu(Menu);
            MenuController.MainMenu = Menu;

            MenuController.AddSubmenu(Menu, PlayerSubmenu);
            MenuController.AddSubmenu(Menu, VehicleSubmenu);
            MenuController.AddSubmenu(Menu, WorldSubmenu);

            // Créer tous les (sous-)menus.
            CreateSubmenus();

            if (!GetSettingsBool(Setting.vmenu_disable_player_stats_setup))
            {
                // Gérer l'endurance
                if (PlayerOptionsMenu != null && PlayerOptionsMenu.PlayerStamina && IsAllowed(Permission.POUnlimitedStamina))
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 100, true);
                }
                else
                {
                    StatSetInt((uint)GetHashKey("MP0_STAMINA"), 0, true);
                }

                // Gérer d'autres statistiques, dans l'ordre d'apparition dans la page des statistiques du menu pause.
                StatSetInt((uint)GetHashKey("MP0_SHOOTING_ABILITY"), 100, true);        // Tir
                StatSetInt((uint)GetHashKey("MP0_STRENGTH"), 100, true);                // Force
                StatSetInt((uint)GetHashKey("MP0_STEALTH_ABILITY"), 100, true);         // Discrétion
                StatSetInt((uint)GetHashKey("MP0_FLYING_ABILITY"), 100, true);          // Pilotage
                StatSetInt((uint)GetHashKey("MP0_WHEELIE_ABILITY"), 100, true);         // Conduite
                StatSetInt((uint)GetHashKey("MP0_LUNG_CAPACITY"), 100, true);           // Capacité pulmonaire
                StatSetFloat((uint)GetHashKey("MP0_PLAYER_MENTAL_STATE"), 0f, true);    // État mental
            }

            TriggerEvent("vMenu:SetupTickFunctions");
        }

        /// <summary>
        /// La tâche principale OnTick s'exécute à chaque tick du jeu et gère tout ce qui concerne le menu.
        /// </summary>
        /// <returns></returns>
        private async Task OnTick()
        {
            // Si la configuration (permissions) est terminée et que ce n'est pas le premier tick, alors faire ceci :
            if (ConfigOptionsSetupComplete)
            {
                #region Gestion de l'ouverture/fermeture du menu.
                var tmpMenu = GetOpenMenu();
                if (MpPedCustomizationMenu != null)
                {
                    static bool IsOpen()
                    {
                        return
                            MpPedCustomizationMenu.appearanceMenu.Visible ||
                            MpPedCustomizationMenu.faceShapeMenu.Visible ||
                            MpPedCustomizationMenu.createCharacterMenu.Visible ||
                            MpPedCustomizationMenu.inheritanceMenu.Visible ||
                            MpPedCustomizationMenu.propsMenu.Visible ||
                            MpPedCustomizationMenu.clothesMenu.Visible ||
                            MpPedCustomizationMenu.tattoosMenu.Visible;
                    }

                    if (IsOpen())
                    {
                        if (tmpMenu == MpPedCustomizationMenu.createCharacterMenu)
                        {
                            MpPedCustomization.DisableBackButton = true;
                        }
                        else
                        {
                            MpPedCustomization.DisableBackButton = false;
                        }
                        MpPedCustomization.DontCloseMenus = true;
                    }
                    else
                    {
                        MpPedCustomization.DisableBackButton = false;
                        MpPedCustomization.DontCloseMenus = false;
                    }
                }

                if (Game.IsDisabledControlJustReleased(0, Control.PhoneCancel) && MpPedCustomization.DisableBackButton)
                {
                    await Delay(0);
                    Notify.Alert("Vous devez d'abord sauvegarder votre ped avant de quitter, ou cliquer sur le bouton ~r~Quitter sans sauvegarder~s~.");
                }

                #endregion

                // Bouton de bascule du menu.
                //Game.DisableControlThisFrame(0, MenuToggleKey);
            }
        }

        #region Fonction d'ajout de menu
        /// <summary>
        /// Ajoute le menu au pool de menus et le configure correctement.
        /// Ajoute et lie également les boutons du menu.
        /// </summary>
        /// <param name="submenu"></param>
        /// <param name="menuButton"></param>
        private static void AddMenu(Menu parentMenu, Menu submenu, MenuItem menuButton)
        {
            parentMenu.AddMenuItem(menuButton);
            MenuController.AddSubmenu(parentMenu, submenu);
            MenuController.BindMenuItem(parentMenu, submenu, menuButton);
            submenu.RefreshIndex();
        }
        #endregion

        #region Création des sous-menus
        /// <summary>
        /// Crée tous les sous-menus en fonction des permissions de l'utilisateur.
        /// </summary>
        private static void CreateSubmenus()
        {
            // Ajouter le menu des joueurs en ligne.
            if (IsAllowed(Permission.OPMenu))
            {
                OnlinePlayersMenu = new OnlinePlayers();
                var menu = OnlinePlayersMenu.GetMenu();
                var button = new MenuItem("Joueurs en ligne", "Tous les joueurs actuellement connectés.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
                Menu.OnItemSelect += async (sender, item, index) =>
                {
                    if (item == button)
                    {
                        PlayersList.RequestPlayerList();

                        await OnlinePlayersMenu.UpdatePlayerlist();
                        menu.RefreshIndex();
                    }
                };
            }
            if (IsAllowed(Permission.OPUnban) || IsAllowed(Permission.OPViewBannedPlayers))
            {
                BannedPlayersMenu = new BannedPlayers();
                var menu = BannedPlayersMenu.GetMenu();
                var button = new MenuItem("Joueurs bannis", "Voir et gérer tous les joueurs bannis dans ce menu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
                Menu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == button)
                    {
                        TriggerServerEvent("vMenu:RequestBanList", Game.Player.Handle);
                        menu.RefreshIndex();
                    }
                };
            }

            var playerSubmenuBtn = new MenuItem("Options relatives au joueur", "Ouvrir ce sous-menu pour les sous-catégories relatives au joueur.") { Label = "→→→" };
            Menu.AddMenuItem(playerSubmenuBtn);

            // Ajouter le menu des options du joueur.
            if (IsAllowed(Permission.POMenu))
            {
                PlayerOptionsMenu = new PlayerOptions();
                var menu = PlayerOptionsMenu.GetMenu();
                var button = new MenuItem("Options du joueur", "Les options courantes du joueur peuvent être configurées ici.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            var vehicleSubmenuBtn = new MenuItem("Options relatives aux véhicules", "Ouvrir ce sous-menu pour les sous-catégories relatives aux véhicules.") { Label = "→→→" };
            Menu.AddMenuItem(vehicleSubmenuBtn);
            // Ajouter le menu des options du véhicule.
            if (IsAllowed(Permission.VOMenu))
            {
                VehicleOptionsMenu = new VehicleOptions();
                var menu = VehicleOptionsMenu.GetMenu();
                var button = new MenuItem("Options du véhicule", "Ici, vous pouvez modifier les options courantes du véhicule, ainsi que tuner et styliser votre véhicule.")
                {
                    Label = "→→→"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Ajouter le menu de spawn des véhicules.
            if (IsAllowed(Permission.VSMenu))
            {
                VehicleSpawnerMenu = new VehicleSpawner();
                var menu = VehicleSpawnerMenu.GetMenu();
                var button = new MenuItem("Spawn de véhicules", "Faites apparaître un véhicule par nom ou choisissez-en un dans une catégorie spécifique.")
                {
                    Label = "→→→"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Ajouter le menu des véhicules sauvegardés.
            if (IsAllowed(Permission.SVMenu))
            {
                SavedVehiclesMenu = new SavedVehicles();
                var menu = SavedVehiclesMenu.GetTypeMenu();
                var button = new MenuItem("Véhicules sauvegardés", "Sauvegardez de nouveaux véhicules, ou faites apparaître ou supprimez des véhicules déjà sauvegardés.")
                {
                    Label = "→→→"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Ajouter le menu du véhicule personnel.
            if (IsAllowed(Permission.PVMenu))
            {
                PersonalVehicleMenu = new PersonalVehicle();
                var menu = PersonalVehicleMenu.GetMenu();
                var button = new MenuItem("Véhicule personnel", "Définissez un véhicule comme votre véhicule personnel et contrôlez certaines choses à propos de ce véhicule lorsque vous n'êtes pas à l'intérieur.")
                {
                    Label = "→→→"
                };
                AddMenu(VehicleSubmenu, menu, button);
            }

            // Ajouter le menu de l'apparence du joueur.
            if (IsAllowed(Permission.PAMenu))
            {
                PlayerAppearanceMenu = new PlayerAppearance();
                var menu = PlayerAppearanceMenu.GetMenu();
                var button = new MenuItem("Apparence du joueur", "Choisissez un modèle de ped, personnalisez-le et sauvegardez et chargez vos personnages personnalisés.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu, button);

                MpPedCustomizationMenu = new MpPedCustomization();
                var menu2 = MpPedCustomizationMenu.GetMenu();
                var button2 = new MenuItem("Personnalisation MP Ped", "Créez, modifiez, sauvegardez et chargez des peds multijoueurs. ~r~Note, vous ne pouvez sauvegarder que les peds créés dans ce sous-menu. vMenu ne peut PAS détecter les peds créés en dehors de ce sous-menu. Simplement à cause des limitations de GTA.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu2, button2);
            }

            var worldSubmenuBtn = new MenuItem("Options relatives au monde", "Ouvrir ce sous-menu pour les sous-catégories relatives au monde.") { Label = "→→→" };
            Menu.AddMenuItem(worldSubmenuBtn);

            // Ajouter le menu des options de temps.
            // Vérifier 'not true' pour s'assurer qu'il est _UNIQUEMENT_ désactivé si le propriétaire le _VEUT VRAIMENT_, pas s'il a accidentellement mal orthographié "false" ou autre.
            if (IsAllowed(Permission.TOMenu) && GetSettingsBool(Setting.vmenu_enable_time_sync))
            {
                TimeOptionsMenu = new TimeOptions();
                var menu = TimeOptionsMenu.GetMenu();
                var button = new MenuItem("Options de temps", "Changez l'heure et modifiez d'autres options relatives au temps.")
                {
                    Label = "→→→"
                };
                AddMenu(WorldSubmenu, menu, button);
            }

            // Ajouter le menu des options météo.
            // Vérifier 'not true' pour s'assurer qu'il est _UNIQUEMENT_ désactivé si le propriétaire le _VEUT VRAIMENT_, pas s'il a accidentellement mal orthographié "false" ou autre.
            if (IsAllowed(Permission.WOMenu) && GetSettingsBool(Setting.vmenu_enable_weather_sync))
            {
                WeatherOptionsMenu = new WeatherOptions();
                var menu = WeatherOptionsMenu.GetMenu();
                var button = new MenuItem("Options météo", "Changez toutes les options relatives à la météo ici.")
                {
                    Label = "→→→"
                };
                AddMenu(WorldSubmenu, menu, button);
            }

            // Ajouter le menu des armes.
            if (IsAllowed(Permission.WPMenu))
            {
                WeaponOptionsMenu = new WeaponOptions();
                var menu = WeaponOptionsMenu.GetMenu();
                var button = new MenuItem("Options des armes", "Ajoutez/supprimez des armes, modifiez les armes et définissez les options de munitions.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            // Ajouter le menu des loadouts d'armes.
            if (IsAllowed(Permission.WLMenu))
            {
                WeaponLoadoutsMenu = new WeaponLoadouts();
                var menu = WeaponLoadoutsMenu.GetMenu();
                var button = new MenuItem("Loadouts d'armes", "Gérez et faites apparaître des loadouts d'armes sauvegardés.")
                {
                    Label = "→→→"
                };
                AddMenu(PlayerSubmenu, menu, button);
            }

            if (IsAllowed(Permission.NoClip))
            {
                var toggleNoclip = new MenuItem("Activer/Désactiver NoClip", "Active ou désactive le NoClip.");
                PlayerSubmenu.AddMenuItem(toggleNoclip);
                PlayerSubmenu.OnItemSelect += (sender, item, index) =>
                {
                    if (item == toggleNoclip)
                    {
                        NoClipEnabled = !NoClipEnabled;
                    }
                };
            }

            // Ajouter le menu des paramètres de chat vocal.
            if (IsAllowed(Permission.VCMenu))
            {
                VoiceChatSettingsMenu = new VoiceChat();
                var menu = VoiceChatSettingsMenu.GetMenu();
                var button = new MenuItem("Paramètres du chat vocal", "Changez les options du chat vocal ici.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            {
                RecordingMenu = new Recording();
                var menu = RecordingMenu.GetMenu();
                var button = new MenuItem("Options d'enregistrement", "Options d'enregistrement en jeu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            // Ajouter le menu des paramètres divers.
            {
                MiscSettingsMenu = new MiscSettings();
                var menu = MiscSettingsMenu.GetMenu();
                var button = new MenuItem("Paramètres divers", "Les options/paramètres divers de vMenu peuvent être configurés ici. Vous pouvez également sauvegarder vos paramètres dans ce menu.")
                {
                    Label = "→→→"
                };
                AddMenu(Menu, menu, button);
            }

            // Ajouter le menu À propos.
            AboutMenu = new About();
            var sub = AboutMenu.GetMenu();
            var btn = new MenuItem("À propos de vMenu", "Informations sur vMenu.")
            {
                Label = "→→→"
            };
            AddMenu(Menu, sub, btn);

            // Tout rafraîchir.
            MenuController.Menus.ForEach((m) => m.RefreshIndex());

            if (!GetSettingsBool(Setting.vmenu_use_permissions))
            {
                Notify.Alert("vMenu est configuré pour ignorer les permissions, les permissions par défaut seront utilisées.");
            }

            if (PlayerSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, PlayerSubmenu, playerSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(playerSubmenuBtn);
            }

            if (VehicleSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, VehicleSubmenu, vehicleSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(vehicleSubmenuBtn);
            }

            if (WorldSubmenu.Size > 0)
            {
                MenuController.BindMenuItem(Menu, WorldSubmenu, worldSubmenuBtn);
            }
            else
            {
                Menu.RemoveMenuItem(worldSubmenuBtn);
            }

            if (MiscSettingsMenu != null)
            {
                MenuController.EnableMenuToggleKeyOnController = !MiscSettingsMenu.MiscDisableControllerSupport;
            }
        }
        #endregion
    }
}