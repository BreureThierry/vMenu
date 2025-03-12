using System;
using System.Collections.Generic;
using System.Linq;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class MiscSettings
    {
        // Variables
        private Menu menu;
        private Menu teleportOptionsMenu;
        private Menu developerToolsMenu;
        private Menu entitySpawnerMenu;

        public bool ShowSpeedoKmh { get; private set; } = UserDefaults.MiscSpeedKmh;
        public bool ShowSpeedoMph { get; private set; } = UserDefaults.MiscSpeedMph;
        public bool ShowCoordinates { get; private set; } = false;
        public bool HideHud { get; private set; } = false;
        public bool HideRadar { get; private set; } = false;
        public bool ShowLocation { get; private set; } = UserDefaults.MiscShowLocation;
        public bool DeathNotifications { get; private set; } = UserDefaults.MiscDeathNotifications;
        public bool JoinQuitNotifications { get; private set; } = UserDefaults.MiscJoinQuitNotifications;
        public bool LockCameraX { get; private set; } = false;
        public bool LockCameraY { get; private set; } = false;
        public bool MPPedPreviews { get; private set; } = UserDefaults.MPPedPreviews;
        public bool ShowLocationBlips { get; private set; } = UserDefaults.MiscLocationBlips;
        public bool ShowPlayerBlips { get; private set; } = UserDefaults.MiscShowPlayerBlips;
        public bool MiscShowOverheadNames { get; private set; } = UserDefaults.MiscShowOverheadNames;
        public bool ShowVehicleModelDimensions { get; private set; } = false;
        public bool ShowPedModelDimensions { get; private set; } = false;
        public bool ShowPropModelDimensions { get; private set; } = false;
        public bool ShowEntityHandles { get; private set; } = false;
        public bool ShowEntityModels { get; private set; } = false;
        public bool ShowEntityNetOwners { get; private set; } = false;
        public bool MiscRespawnDefaultCharacter { get; private set; } = UserDefaults.MiscRespawnDefaultCharacter;
        public bool RestorePlayerAppearance { get; private set; } = UserDefaults.MiscRestorePlayerAppearance;
        public bool RestorePlayerWeapons { get; private set; } = UserDefaults.MiscRestorePlayerWeapons;
        public bool DrawTimeOnScreen { get; internal set; } = UserDefaults.MiscShowTime;
        public bool MiscRightAlignMenu { get; private set; } = UserDefaults.MiscRightAlignMenu;
        public bool MiscDisablePrivateMessages { get; private set; } = UserDefaults.MiscDisablePrivateMessages;
        public bool MiscDisableControllerSupport { get; private set; } = UserDefaults.MiscDisableControllerSupport;

        internal bool TimecycleEnabled { get; private set; } = false;
        internal int LastTimeCycleModifierIndex { get; private set; } = UserDefaults.MiscLastTimeCycleModifierIndex;
        internal int LastTimeCycleModifierStrength { get; private set; } = UserDefaults.MiscLastTimeCycleModifierStrength;


        // keybind states
        public bool KbTpToWaypoint { get; private set; } = UserDefaults.KbTpToWaypoint;
        public int KbTpToWaypointKey { get; } = vMenuShared.ConfigManager.GetSettingsInt(vMenuShared.ConfigManager.Setting.vmenu_teleport_to_wp_keybind_key) != -1
            ? vMenuShared.ConfigManager.GetSettingsInt(vMenuShared.ConfigManager.Setting.vmenu_teleport_to_wp_keybind_key)
            : 168; // 168 (F7 by default)
        public bool KbDriftMode { get; private set; } = UserDefaults.KbDriftMode;
        public bool KbRecordKeys { get; private set; } = UserDefaults.KbRecordKeys;
        public bool KbRadarKeys { get; private set; } = UserDefaults.KbRadarKeys;
        public bool KbPointKeys { get; private set; } = UserDefaults.KbPointKeys;

        internal static List<vMenuShared.ConfigManager.TeleportLocation> TpLocations = new();

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            MenuController.MenuAlignment = MiscRightAlignMenu ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left;
            if (MenuController.MenuAlignment != (MiscRightAlignMenu ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left))
            {
                Notify.Error(CommonErrors.RightAlignedNotSupported);

                // (re)set the default to left just in case so they don't get this error again in the future.
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Left;
                MiscRightAlignMenu = false;
                UserDefaults.MiscRightAlignMenu = false;
            }

            // Créer le menu.
            menu = new Menu(Game.Player.Name, "Paramètres divers");
            teleportOptionsMenu = new Menu(Game.Player.Name, "Options de téléportation");
            developerToolsMenu = new Menu(Game.Player.Name, "Outils de développement");
            entitySpawnerMenu = new Menu(Game.Player.Name, "Générateur d'entités");

            // Menu de téléportation
            var teleportMenu = new Menu(Game.Player.Name, "Lieux de téléportation");
            var teleportMenuBtn = new MenuItem("Lieux de téléportation", "Téléportez-vous vers des lieux préconfigurés, ajoutés par le propriétaire du serveur.");
            MenuController.AddSubmenu(menu, teleportMenu);
            MenuController.BindMenuItem(menu, teleportMenu, teleportMenuBtn);

            // Menu des paramètres des raccourcis clavier
            var keybindMenu = new Menu(Game.Player.Name, "Paramètres des raccourcis");
            var keybindMenuBtn = new MenuItem("Paramètres des raccourcis", "Active ou désactive les raccourcis pour certaines options.");
            MenuController.AddSubmenu(menu, keybindMenu);
            MenuController.BindMenuItem(menu, keybindMenu, keybindMenuBtn);

            // Éléments du menu des paramètres des raccourcis
            var kbTpToWaypoint = new MenuCheckboxItem("Téléportation vers le point de passage", "Téléportez-vous vers votre point de passage en appuyant sur le raccourci. Par défaut, ce raccourci est défini sur ~r~F7~s~, mais les propriétaires de serveurs peuvent le modifier, demandez-leur si vous ne le connaissez pas.", KbTpToWaypoint);
            var kbDriftMode = new MenuCheckboxItem("Mode drift", "Donne à votre véhicule presque aucune traction en maintenant la touche Maj gauche sur le clavier, ou X sur la manette.", KbDriftMode);
            var kbRecordKeys = new MenuCheckboxItem("Contrôles d'enregistrement", "Active ou désactive les raccourcis d'enregistrement (enregistrement de gameplay pour l'éditeur Rockstar) sur le clavier et la manette.", KbRecordKeys);
            var kbRadarKeys = new MenuCheckboxItem("Contrôles du radar", "Appuyez sur la touche Info Multijoueur (Z sur le clavier, flèche bas sur la manette) pour basculer entre le radar étendu et le radar normal.", KbRadarKeys);
            var kbPointKeysCheckbox = new MenuCheckboxItem("Contrôles du pointage du doigt", "Active la touche de pointage du doigt. Le mapping par défaut pour les claviers QWERTY est 'B', ou pour la manette, double-cliquez rapidement sur le stick analogique droit.", KbPointKeys);
            var backBtn = new MenuItem("Retour");

            // Créer les éléments du menu.
            var rightAlignMenu = new MenuCheckboxItem("Aligner le menu à droite", "Si vous souhaitez que vMenu apparaisse à gauche de votre écran, désactivez cette option. Cette option est enregistrée immédiatement. Vous n'avez pas besoin de cliquer sur sauvegarder les préférences.", MiscRightAlignMenu);
            var disablePms = new MenuCheckboxItem("Désactiver les messages privés", "Empêche les autres joueurs de vous envoyer un message privé via le menu des joueurs en ligne. Cela vous empêche également d'envoyer des messages à d'autres joueurs.", MiscDisablePrivateMessages);
            var disableControllerKey = new MenuCheckboxItem("Désactiver le support de la manette", "Désactive la touche de bascule du menu pour la manette. Cela ne désactive PAS les boutons de navigation.", MiscDisableControllerSupport);
            var speedKmh = new MenuCheckboxItem("Afficher la vitesse en KM/H", "Affiche un compteur de vitesse à l'écran indiquant votre vitesse en KM/h.", ShowSpeedoKmh);
            var speedMph = new MenuCheckboxItem("Afficher la vitesse en MPH", "Affiche un compteur de vitesse à l'écran indiquant votre vitesse en MPH.", ShowSpeedoMph);
            var coords = new MenuCheckboxItem("Afficher les coordonnées", "Affiche vos coordonnées actuelles en haut de votre écran.", ShowCoordinates);
            var hideRadar = new MenuCheckboxItem("Masquer le radar", "Masque le radar/minimap.", HideRadar);
            var hideHud = new MenuCheckboxItem("Masquer l'interface", "Masque tous les éléments de l'interface.", HideHud);
            var showLocation = new MenuCheckboxItem("Affichage de l'emplacement", "Affiche votre emplacement actuel et votre cap, ainsi que le carrefour le plus proche. Similaire à PLD. ~r~Attention : Cette fonctionnalité peut réduire jusqu'à -4,6 FPS à 60 Hz.", ShowLocation) { LeftIcon = MenuItem.Icon.WARNING };
            var drawTime = new MenuCheckboxItem("Afficher l'heure à l'écran", "Affiche l'heure actuelle à l'écran.", DrawTimeOnScreen);
            var saveSettings = new MenuItem("Sauvegarder les paramètres personnels", "Sauvegarde vos paramètres actuels. Tout est enregistré côté client, si vous réinstallez Windows, vous perdrez vos paramètres. Les paramètres sont partagés entre tous les serveurs utilisant vMenu.")
            {
                RightIcon = MenuItem.Icon.TICK
            };
            var exportData = new MenuItem("Exporter/Importer des données", "Bientôt disponible (TM) : la possibilité d'importer et d'exporter vos données sauvegardées.");
            var joinQuitNotifs = new MenuCheckboxItem("Notifications de connexion/déconnexion", "Recevez des notifications lorsqu'un joueur rejoint ou quitte le serveur.", JoinQuitNotifications);
            var deathNotifs = new MenuCheckboxItem("Notifications de décès", "Recevez des notifications lorsqu'un joueur meurt ou est tué.", DeathNotifications);
            var nightVision = new MenuCheckboxItem("Activer la vision nocturne", "Active ou désactive la vision nocturne.", false);
            var thermalVision = new MenuCheckboxItem("Activer la vision thermique", "Active ou désactive la vision thermique.", false);
            var vehModelDimensions = new MenuCheckboxItem("Afficher les dimensions des véhicules", "Dessine les contours des modèles pour chaque véhicule proche de vous.", ShowVehicleModelDimensions);
            var propModelDimensions = new MenuCheckboxItem("Afficher les dimensions des objets", "Dessine les contours des modèles pour chaque objet proche de vous.", ShowPropModelDimensions);
            var pedModelDimensions = new MenuCheckboxItem("Afficher les dimensions des peds", "Dessine les contours des modèles pour chaque ped proche de vous.", ShowPedModelDimensions);
            var showEntityHandles = new MenuCheckboxItem("Afficher les handles des entités", "Affiche les handles des entités pour toutes les entités proches (vous devez activer les fonctions de contour ci-dessus pour que cela fonctionne).", ShowEntityHandles);
            var showEntityModels = new MenuCheckboxItem("Afficher les modèles des entités", "Affiche les modèles des entités pour toutes les entités proches (vous devez activer les fonctions de contour ci-dessus pour que cela fonctionne).", ShowEntityModels);
            var showEntityNetOwners = new MenuCheckboxItem("Afficher les propriétaires réseau", "Affiche le propriétaire réseau des entités pour toutes les entités proches (vous devez activer les fonctions de contour ci-dessus pour que cela fonctionne).", ShowEntityNetOwners);
            var dimensionsDistanceSlider = new MenuSliderItem("Rayon d'affichage des dimensions", "Définit la portée d'affichage des modèles/handles/dimensions des entités.", 0, 20, 20, false);

            var clearArea = new MenuItem("Nettoyer la zone", "Nettoie la zone autour de votre joueur (100 mètres). Dégâts, saleté, peds, objets, véhicules, etc. Tout est nettoyé, réparé et réinitialisé à l'état par défaut du monde.");
            var lockCamX = new MenuCheckboxItem("Verrouiller la rotation horizontale de la caméra", "Verrouille la rotation horizontale de votre caméra. Peut être utile dans les hélicoptères, je suppose.", false);
            var lockCamY = new MenuCheckboxItem("Verrouiller la rotation verticale de la caméra", "Verrouille la rotation verticale de votre caméra. Peut être utile dans les hélicoptères, je suppose.", false);

            var mpPedPreview = new MenuCheckboxItem("Aperçu 3D des peds multijoueur", "Affiche un aperçu 3D des peds multijoueur lors de la visualisation des peds sauvegardés.", MPPedPreviews);

            // Générateur d'entités
            var spawnNewEntity = new MenuItem("Générer une nouvelle entité", "Génère une entité dans le monde et vous permet de définir sa position et sa rotation");
            var confirmEntityPosition = new MenuItem("Confirmer la position de l'entité", "Arrête de placer l'entité et la fixe à son emplacement actuel.");
            var cancelEntity = new MenuItem("Annuler", "Supprime l'entité actuelle et annule son placement");
            var confirmAndDuplicate = new MenuItem("Confirmer la position et dupliquer", "Arrête de placer l'entité, la fixe à son emplacement actuel et en crée une nouvelle à placer.");

            var connectionSubmenu = new Menu(Game.Player.Name, "Options de connexion");
            var connectionSubmenuBtn = new MenuItem("Options de connexion", "Options de connexion au serveur/quitter le jeu.");

            var quitSession = new MenuItem("Quitter la session", "Vous reste connecté au serveur, mais quitte la session réseau. ~r~Ne peut pas être utilisé si vous êtes l'hôte.");
            var rejoinSession = new MenuItem("Rejoindre la session", "Cela peut ne pas fonctionner dans tous les cas, mais vous pouvez essayer de l'utiliser si vous souhaitez rejoindre la session précédente après avoir cliqué sur 'Quitter la session'.");
            var quitGame = new MenuItem("Quitter le jeu", "Quitte le jeu après 5 secondes.");
            var disconnectFromServer = new MenuItem("Se déconnecter du serveur", "Vous déconnecte du serveur et vous renvoie à la liste des serveurs. ~r~Cette fonctionnalité n'est pas recommandée, quittez complètement le jeu et redémarrez-le pour une meilleure expérience.");
            connectionSubmenu.AddMenuItem(quitSession);
            connectionSubmenu.AddMenuItem(rejoinSession);
            connectionSubmenu.AddMenuItem(quitGame);
            connectionSubmenu.AddMenuItem(disconnectFromServer);

            var enableTimeCycle = new MenuCheckboxItem("Activer le modificateur de cycle temporel", "Active ou désactive le modificateur de cycle temporel dans la liste ci-dessous.", TimecycleEnabled);
            var timeCycleModifiersListData = TimeCycles.Timecycles.ToList();
            for (var i = 0; i < timeCycleModifiersListData.Count; i++)
            {
                timeCycleModifiersListData[i] += $" ({i + 1}/{timeCycleModifiersListData.Count})";
            }
            var timeCycles = new MenuListItem("TM", timeCycleModifiersListData, MathUtil.Clamp(LastTimeCycleModifierIndex, 0, Math.Max(0, timeCycleModifiersListData.Count - 1)), "Sélectionnez un modificateur de cycle temporel et activez la case à cocher ci-dessus.");
            var timeCycleIntensity = new MenuSliderItem("Intensité du modificateur de cycle temporel", "Définit l'intensité du modificateur de cycle temporel.", 0, 20, LastTimeCycleModifierStrength, true);

            var locationBlips = new MenuCheckboxItem("Points de repère", "Affiche des points de repère sur la carte pour certains lieux communs.", ShowLocationBlips);
            var playerBlips = new MenuCheckboxItem("Afficher les points des joueurs", "Affiche des points de repère sur la carte pour tous les joueurs. ~y~Note pour les serveurs utilisant OneSync Infinity : cela ne fonctionnera pas pour les joueurs trop éloignés.", ShowPlayerBlips);
            var playerNames = new MenuCheckboxItem("Afficher les noms des joueurs", "Active ou désactive l'affichage des noms des joueurs au-dessus de leur tête.", MiscShowOverheadNames);
            var respawnDefaultCharacter = new MenuCheckboxItem("Réapparaître en tant que personnage MP par défaut", "Si vous activez cette option, vous réapparaîtrez en tant que votre personnage MP sauvegardé par défaut. Notez que le propriétaire du serveur peut désactiver globalement cette option. Pour définir votre personnage par défaut, allez dans l'un de vos personnages MP sauvegardés et cliquez sur le bouton 'Définir comme personnage par défaut'.", MiscRespawnDefaultCharacter);
            var restorePlayerAppearance = new MenuCheckboxItem("Restaurer l'apparence du joueur", "Restore l'apparence de votre joueur chaque fois que vous réapparaissez après être mort. Rejoindre un serveur ne restaurera pas votre apparence précédente.", RestorePlayerAppearance);
            var restorePlayerWeapons = new MenuCheckboxItem("Restaurer les armes du joueur", "Restore vos armes chaque fois que vous réapparaissez après être mort. Rejoindre un serveur ne restaurera pas vos armes précédentes.", RestorePlayerWeapons);

            MenuController.AddSubmenu(menu, connectionSubmenu);
            MenuController.BindMenuItem(menu, connectionSubmenu, connectionSubmenuBtn);

            keybindMenu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == kbTpToWaypoint)
                {
                    KbTpToWaypoint = _checked;
                }
                else if (item == kbDriftMode)
                {
                    KbDriftMode = _checked;
                }
                else if (item == kbRecordKeys)
                {
                    KbRecordKeys = _checked;
                }
                else if (item == kbRadarKeys)
                {
                    KbRadarKeys = _checked;
                }
                else if (item == kbPointKeysCheckbox)
                {
                    KbPointKeys = _checked;
                }
            };
            keybindMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == backBtn)
                {
                    keybindMenu.GoBack();
                }
            };

            connectionSubmenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == quitGame)
                {
                    CommonFunctions.QuitGame();
                }
                else if (item == quitSession)
                {
                    if (NetworkIsSessionActive())
                    {
                        if (NetworkIsHost())
                        {
                            Notify.Error("Désolé, vous ne pouvez pas quitter la session lorsque vous êtes l'hôte. Cela empêcherait d'autres joueurs de rejoindre le serveur ou d'y rester.");
                        }
                        else
                        {
                            QuitSession();
                        }
                    }
                    else
                    {
                        Notify.Error("Vous n'êtes actuellement dans aucune session.");
                    }
                }
                else if (item == rejoinSession)
                {
                    if (NetworkIsSessionActive())
                    {
                        Notify.Error("Vous êtes déjà connecté à une session.");
                    }
                    else
                    {
                        Notify.Info("Tentative de réinscription à la session.");
                        NetworkSessionHost(-1, 32, false);
                    }
                }
                else if (item == disconnectFromServer)
                {

                    RegisterCommand("disconnect", new Action<dynamic, dynamic, dynamic>((a, b, c) => { }), false);
                    ExecuteCommand("disconnect");
                }
            };

            // Teleportation options
            if (IsAllowed(Permission.MSTeleportToWp) || IsAllowed(Permission.MSTeleportLocations) || IsAllowed(Permission.MSTeleportToCoord))
            {
                var teleportOptionsMenuBtn = new MenuItem("Options de téléportation", "Diverses options de téléportation.") { Label = "→→→" };
                menu.AddMenuItem(teleportOptionsMenuBtn);
                MenuController.BindMenuItem(menu, teleportOptionsMenu, teleportOptionsMenuBtn);

                var tptowp = new MenuItem("Téléportation vers le point de passage", "Téléportez-vous vers le point de passage sur votre carte.");
                var tpToCoord = new MenuItem("Téléportation vers les coordonnées", "Entrez les coordonnées x, y, z et vous serez téléporté à cet emplacement.");
                var saveLocationBtn = new MenuItem("Enregistrer un lieu de téléportation", "Ajoute votre position actuelle au menu des lieux de téléportation et l'enregistre sur le serveur.");
                teleportOptionsMenu.OnItemSelect += async (sender, item, index) =>
                {
                    // Teleport to waypoint.
                    if (item == tptowp)
                    {
                        TeleportToWp();
                    }
                    else if (item == tpToCoord)
                    {
                        var x = await GetUserInput("Coordonné X.");
                        if (string.IsNullOrEmpty(x))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                            return;
                        }
                        var y = await GetUserInput("Coordonné  Y");
                        if (string.IsNullOrEmpty(y))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                            return;
                        }
                        var z = await GetUserInput("Coordonné  Z");
                        if (string.IsNullOrEmpty(z))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                            return;
                        }


                        if (!float.TryParse(x, out var posX))
                        {
                            if (int.TryParse(x, out var intX))
                            {
                                posX = intX;
                            }
                            else
                            {
                                Notify.Error("Vous n'avez pas saisi de coordonnées X valides.");
                                return;
                            }
                        }
                        if (!float.TryParse(y, out var posY))
                        {
                            if (int.TryParse(y, out var intY))
                            {
                                posY = intY;
                            }
                            else
                            {
                                Notify.Error("Vous n'avez pas saisi de coordonnées Y valides.");
                                return;
                            }
                        }
                        if (!float.TryParse(z, out var posZ))
                        {
                            if (int.TryParse(z, out var intZ))
                            {
                                posZ = intZ;
                            }
                            else
                            {
                                Notify.Error("Vous n'avez pas saisi de coordonnées Z valides.");
                                return;
                            }
                        }

                        await TeleportToCoords(new Vector3(posX, posY, posZ), true);
                    }
                    else if (item == saveLocationBtn)
                    {
                        SavePlayerLocationToLocationsFile();
                    }
                };

                if (IsAllowed(Permission.MSTeleportToWp))
                {
                    teleportOptionsMenu.AddMenuItem(tptowp);
                    keybindMenu.AddMenuItem(kbTpToWaypoint);
                }
                if (IsAllowed(Permission.MSTeleportToCoord))
                {
                    teleportOptionsMenu.AddMenuItem(tpToCoord);
                }
                if (IsAllowed(Permission.MSTeleportLocations))
                {
                    teleportOptionsMenu.AddMenuItem(teleportMenuBtn);

                    MenuController.AddSubmenu(teleportOptionsMenu, teleportMenu);
                    MenuController.BindMenuItem(teleportOptionsMenu, teleportMenu, teleportMenuBtn);
                    teleportMenuBtn.Label = "→→→";

                    teleportMenu.OnMenuOpen += (sender) =>
                    {
                        if (teleportMenu.Size != TpLocations.Count())
                        {
                            teleportMenu.ClearMenuItems();
                            foreach (var location in TpLocations)
                            {
                                var x = Math.Round(location.coordinates.X, 2);
                                var y = Math.Round(location.coordinates.Y, 2);
                                var z = Math.Round(location.coordinates.Z, 2);
                                var heading = Math.Round(location.heading, 2);
                                var tpBtn = new MenuItem(location.name, $"Teleporté vers ~y~{location.name}~n~~s~x: ~y~{x}~n~~s~y: ~y~{y}~n~~s~z: ~y~{z}~n~~s~heading: ~y~{heading}") { ItemData = location };
                                teleportMenu.AddMenuItem(tpBtn);
                            }
                        }
                    };

                    teleportMenu.OnItemSelect += async (sender, item, index) =>
                    {
                        if (item.ItemData is vMenuShared.ConfigManager.TeleportLocation tl)
                        {
                            await TeleportToCoords(tl.coordinates, true);
                            SetEntityHeading(Game.PlayerPed.Handle, tl.heading);
                            SetGameplayCamRelativeHeading(0f);
                        }
                    };

                    if (IsAllowed(Permission.MSTeleportSaveLocation))
                    {
                        teleportOptionsMenu.AddMenuItem(saveLocationBtn);
                    }
                }

            }

            #region dev tools menu

            var devToolsBtn = new MenuItem("Outils du développeur", "Divers outils de développement/débogage.") { Label = "→→→" };
            menu.AddMenuItem(devToolsBtn);
            MenuController.AddSubmenu(menu, developerToolsMenu);
            MenuController.BindMenuItem(menu, developerToolsMenu, devToolsBtn);

            // clear area and coordinates
            if (IsAllowed(Permission.MSClearArea))
            {
                developerToolsMenu.AddMenuItem(clearArea);
            }
            if (IsAllowed(Permission.MSShowCoordinates))
            {
                developerToolsMenu.AddMenuItem(coords);
            }

            // model outlines
            if ((!vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.vmenu_disable_entity_outlines_tool)) && (IsAllowed(Permission.MSDevTools)))
            {
                developerToolsMenu.AddMenuItem(vehModelDimensions);
                developerToolsMenu.AddMenuItem(propModelDimensions);
                developerToolsMenu.AddMenuItem(pedModelDimensions);
                developerToolsMenu.AddMenuItem(showEntityHandles);
                developerToolsMenu.AddMenuItem(showEntityModels);
                developerToolsMenu.AddMenuItem(showEntityNetOwners);
                developerToolsMenu.AddMenuItem(dimensionsDistanceSlider);
            }


            // timecycle modifiers
            developerToolsMenu.AddMenuItem(timeCycles);
            developerToolsMenu.AddMenuItem(enableTimeCycle);
            developerToolsMenu.AddMenuItem(timeCycleIntensity);

            developerToolsMenu.OnSliderPositionChange += (sender, item, oldPos, newPos, itemIndex) =>
            {
                if (item == timeCycleIntensity)
                {
                    ClearTimecycleModifier();
                    if (TimecycleEnabled)
                    {
                        SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                        var intensity = newPos / 20f;
                        SetTimecycleModifierStrength(intensity);
                    }
                    UserDefaults.MiscLastTimeCycleModifierIndex = timeCycles.ListIndex;
                    UserDefaults.MiscLastTimeCycleModifierStrength = timeCycleIntensity.Position;
                }
                else if (item == dimensionsDistanceSlider)
                {
                    FunctionsController.entityRange = newPos / 20f * 2000f; // max radius = 2000f;
                }
            };

            developerToolsMenu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
            {
                if (item == timeCycles)
                {
                    ClearTimecycleModifier();
                    if (TimecycleEnabled)
                    {
                        SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                        var intensity = timeCycleIntensity.Position / 20f;
                        SetTimecycleModifierStrength(intensity);
                    }
                    UserDefaults.MiscLastTimeCycleModifierIndex = timeCycles.ListIndex;
                    UserDefaults.MiscLastTimeCycleModifierStrength = timeCycleIntensity.Position;
                }
            };

            developerToolsMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == clearArea)
                {
                    var pos = Game.PlayerPed.Position;
                    BaseScript.TriggerServerEvent("vMenu:ClearArea", pos.X, pos.Y, pos.Z);
                }
            };

            developerToolsMenu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == vehModelDimensions)
                {
                    ShowVehicleModelDimensions = _checked;
                }
                else if (item == propModelDimensions)
                {
                    ShowPropModelDimensions = _checked;
                }
                else if (item == pedModelDimensions)
                {
                    ShowPedModelDimensions = _checked;
                }
                else if (item == showEntityHandles)
                {
                    ShowEntityHandles = _checked;
                }
                else if (item == showEntityModels)
                {
                    ShowEntityModels = _checked;
                }
                else if (item == showEntityNetOwners)
                {
                    ShowEntityNetOwners = _checked;
                }
                else if (item == enableTimeCycle)
                {
                    TimecycleEnabled = _checked;
                    ClearTimecycleModifier();
                    if (TimecycleEnabled)
                    {
                        SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                        var intensity = timeCycleIntensity.Position / 20f;
                        SetTimecycleModifierStrength(intensity);
                    }
                }
                else if (item == coords)
                {
                    ShowCoordinates = _checked;
                }
            };

            if (IsAllowed(Permission.MSEntitySpawner))
            {
                var entSpawnerMenuBtn = new MenuItem("Entité Spawner", "Création et déplacement d'entités") { Label = "→→→" };
                developerToolsMenu.AddMenuItem(entSpawnerMenuBtn);
                MenuController.BindMenuItem(developerToolsMenu, entitySpawnerMenu, entSpawnerMenuBtn);

                entitySpawnerMenu.AddMenuItem(spawnNewEntity);
                entitySpawnerMenu.AddMenuItem(confirmEntityPosition);
                entitySpawnerMenu.AddMenuItem(confirmAndDuplicate);
                entitySpawnerMenu.AddMenuItem(cancelEntity);

                entitySpawnerMenu.OnItemSelect += async (sender, item, index) =>
                {
                    if (item == spawnNewEntity)
                    {
                        if (EntitySpawner.CurrentEntity != null || EntitySpawner.Active)
                        {
                            Notify.Error("Vous êtes déjà en train de placer une entité, définissez son emplacement ou annulez et réessayez !");
                            return;
                        }

                        var result = await GetUserInput(windowTitle: "Saisir le nom du modèle");

                        if (string.IsNullOrEmpty(result))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                        }

                        EntitySpawner.SpawnEntity(result, Game.PlayerPed.Position);
                    }
                    else if (item == confirmEntityPosition || item == confirmAndDuplicate)
                    {
                        if (EntitySpawner.CurrentEntity != null)
                        {
                            EntitySpawner.FinishPlacement(item == confirmAndDuplicate);
                        }
                        else
                        {
                            Notify.Error("Pas d'entité pour laquelle confirmer la position !");
                        }
                    }
                    else if (item == cancelEntity)
                    {
                        if (EntitySpawner.CurrentEntity != null)
                        {
                            EntitySpawner.CurrentEntity.Delete();
                        }
                        else
                        {
                            Notify.Error("Aucune entité à supprimer !");
                        }
                    }
                };
            }

            #endregion


            // Keybind options
            if (IsAllowed(Permission.MSDriftMode))
            {
                keybindMenu.AddMenuItem(kbDriftMode);
            }
            // always allowed keybind menu options
            keybindMenu.AddMenuItem(kbRecordKeys);
            keybindMenu.AddMenuItem(kbRadarKeys);
            keybindMenu.AddMenuItem(kbPointKeysCheckbox);
            keybindMenu.AddMenuItem(backBtn);

            // Always allowed
            menu.AddMenuItem(rightAlignMenu);
            menu.AddMenuItem(disablePms);
            menu.AddMenuItem(disableControllerKey);
            menu.AddMenuItem(speedKmh);
            menu.AddMenuItem(speedMph);
            menu.AddMenuItem(keybindMenuBtn);
            keybindMenuBtn.Label = "→→→";
            if (IsAllowed(Permission.MSConnectionMenu))
            {
                menu.AddMenuItem(connectionSubmenuBtn);
                connectionSubmenuBtn.Label = "→→→";
            }
            if (IsAllowed(Permission.MSShowLocation))
            {
                menu.AddMenuItem(showLocation);
            }
            menu.AddMenuItem(drawTime); // always allowed
            if (IsAllowed(Permission.MSJoinQuitNotifs))
            {
                menu.AddMenuItem(joinQuitNotifs);
            }
            if (IsAllowed(Permission.MSDeathNotifs))
            {
                menu.AddMenuItem(deathNotifs);
            }
            if (IsAllowed(Permission.MSNightVision))
            {
                menu.AddMenuItem(nightVision);
            }
            if (IsAllowed(Permission.MSThermalVision))
            {
                menu.AddMenuItem(thermalVision);
            }
            if (IsAllowed(Permission.MSLocationBlips))
            {
                menu.AddMenuItem(locationBlips);
                ToggleBlips(ShowLocationBlips);
            }
            if (IsAllowed(Permission.MSPlayerBlips))
            {
                menu.AddMenuItem(playerBlips);
            }
            if (IsAllowed(Permission.MSOverheadNames))
            {
                menu.AddMenuItem(playerNames);
            }
            // always allowed, it just won't do anything if the server owner disabled the feature, but players can still toggle it.
            menu.AddMenuItem(respawnDefaultCharacter);
            if (IsAllowed(Permission.MSRestoreAppearance))
            {
                menu.AddMenuItem(restorePlayerAppearance);
            }
            if (IsAllowed(Permission.MSRestoreWeapons))
            {
                menu.AddMenuItem(restorePlayerWeapons);
            }

            // Always allowed
            menu.AddMenuItem(hideRadar);
            menu.AddMenuItem(hideHud);
            menu.AddMenuItem(lockCamX);
            menu.AddMenuItem(lockCamY);

            // If disabled at a server level, don't show the option to players
            if (GetSettingsBool(Setting.vmenu_mp_ped_preview))
            {
                menu.AddMenuItem(mpPedPreview);
            }

            if (MainMenu.EnableExperimentalFeatures)
            {
                menu.AddMenuItem(exportData);
            }
            menu.AddMenuItem(saveSettings);

            // Handle checkbox changes.
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == rightAlignMenu)
                {

                    MenuController.MenuAlignment = _checked ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left;
                    MiscRightAlignMenu = _checked;
                    UserDefaults.MiscRightAlignMenu = MiscRightAlignMenu;

                    if (MenuController.MenuAlignment != (_checked ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left))
                    {
                        Notify.Error(CommonErrors.RightAlignedNotSupported);
                        // (re)set the default to left just in case so they don't get this error again in the future.
                        MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Left;
                        MiscRightAlignMenu = false;
                        UserDefaults.MiscRightAlignMenu = false;
                    }

                }
                else if (item == disablePms)
                {
                    MiscDisablePrivateMessages = _checked;
                }
                else if (item == disableControllerKey)
                {
                    MiscDisableControllerSupport = _checked;
                    MenuController.EnableMenuToggleKeyOnController = !_checked;
                }
                else if (item == speedKmh)
                {
                    ShowSpeedoKmh = _checked;
                }
                else if (item == speedMph)
                {
                    ShowSpeedoMph = _checked;
                }
                else if (item == hideHud)
                {
                    HideHud = _checked;
                    DisplayHud(!_checked);
                }
                else if (item == hideRadar)
                {
                    HideRadar = _checked;
                    if (!_checked)
                    {
                        DisplayRadar(true);
                    }
                }
                else if (item == showLocation)
                {
                    ShowLocation = _checked;
                }
                else if (item == drawTime)
                {
                    DrawTimeOnScreen = _checked;
                }
                else if (item == deathNotifs)
                {
                    DeathNotifications = _checked;
                }
                else if (item == joinQuitNotifs)
                {
                    JoinQuitNotifications = _checked;
                }
                else if (item == nightVision)
                {
                    SetNightvision(_checked);
                }
                else if (item == thermalVision)
                {
                    SetSeethrough(_checked);
                }
                else if (item == lockCamX)
                {
                    LockCameraX = _checked;
                }
                else if (item == lockCamY)
                {
                    LockCameraY = _checked;
                }
                else if (item == mpPedPreview)
                {
                    MPPedPreviews = _checked;
                }
                else if (item == locationBlips)
                {
                    ToggleBlips(_checked);
                    ShowLocationBlips = _checked;
                }
                else if (item == playerBlips)
                {
                    ShowPlayerBlips = _checked;
                }
                else if (item == playerNames)
                {
                    MiscShowOverheadNames = _checked;
                }
                else if (item == respawnDefaultCharacter)
                {
                    MiscRespawnDefaultCharacter = _checked;
                }
                else if (item == restorePlayerAppearance)
                {
                    RestorePlayerAppearance = _checked;
                }
                else if (item == restorePlayerWeapons)
                {
                    RestorePlayerWeapons = _checked;
                }

            };

            // Handle button presses.
            menu.OnItemSelect += (sender, item, index) =>
            {
                // export data
                if (item == exportData)
                {
                    MenuController.CloseAllMenus();
                    var vehicles = GetSavedVehicles();
                    var normalPeds = StorageManager.GetSavedPeds();
                    var mpPeds = StorageManager.GetSavedMpPeds();
                    var weaponLoadouts = WeaponLoadouts.GetSavedWeapons();
                    var data = JsonConvert.SerializeObject(new
                    {
                        saved_vehicles = vehicles,
                        normal_peds = normalPeds,
                        mp_characters = mpPeds,
                        weapon_loadouts = weaponLoadouts
                    });
                    SendNuiMessage(data);
                    SetNuiFocus(true, true);
                }
                // save settings
                else if (item == saveSettings)
                {
                    UserDefaults.SaveSettings();
                }
            };
        }


        /// <summary>
        /// Create the menu if it doesn't exist, and then returns it.
        /// </summary>
        /// <returns>The Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }
            return menu;
        }

        private readonly struct Blip
        {
            public readonly Vector3 Location;
            public readonly int Sprite;
            public readonly string Name;
            public readonly int Color;
            public readonly int blipID;

            public Blip(Vector3 Location, int Sprite, string Name, int Color, int blipID)
            {
                this.Location = Location;
                this.Sprite = Sprite;
                this.Name = Name;
                this.Color = Color;
                this.blipID = blipID;
            }
        }

        private readonly List<Blip> blips = new();

        /// <summary>
        /// Toggles blips on/off.
        /// </summary>
        /// <param name="enable"></param>
        private void ToggleBlips(bool enable)
        {
            if (enable)
            {
                try
                {
                    foreach (var bl in vMenuShared.ConfigManager.GetLocationBlipsData())
                    {
                        var blipID = AddBlipForCoord(bl.coordinates.X, bl.coordinates.Y, bl.coordinates.Z);
                        SetBlipSprite(blipID, bl.spriteID);
                        BeginTextCommandSetBlipName("STRING");
                        AddTextComponentSubstringPlayerName(bl.name);
                        EndTextCommandSetBlipName(blipID);
                        SetBlipColour(blipID, bl.color);
                        SetBlipAsShortRange(blipID, true);

                        var b = new Blip(bl.coordinates, bl.spriteID, bl.name, bl.color, blipID);
                        blips.Add(b);
                    }
                }
                catch (JsonReaderException ex)
                {
                    Debug.Write($"\n\n[vMenu] An error occurred while loading the locations.json file. Please contact the server owner to resolve this.\nWhen contacting the owner, provide the following error details:\n{ex.Message}.\n\n\n");
                }
            }
            else
            {
                if (blips.Count > 0)
                {
                    foreach (var blip in blips)
                    {
                        var id = blip.blipID;
                        if (DoesBlipExist(id))
                        {
                            RemoveBlip(ref id);
                        }
                    }
                }
                blips.Clear();
            }
        }

    }
}
