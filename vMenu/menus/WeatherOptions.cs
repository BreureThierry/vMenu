using System.Collections.Generic;

using CitizenFX.Core;

using MenuAPI;

using vMenuShared;

using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class WeatherOptions
    {
        // Variables
        private Menu menu;
        public MenuCheckboxItem dynamicWeatherEnabled;
        public MenuCheckboxItem blackout;
        public MenuCheckboxItem snowEnabled;
        public static readonly List<string> weatherTypes = new()
        {
            "EXTRASUNNY",
            "CLEAR",
            "NEUTRAL",
            "SMOG",
            "FOGGY",
            "CLOUDS",
            "OVERCAST",
            "CLEARING",
            "RAIN",
            "THUNDER",
            "BLIZZARD",
            "SNOW",
            "SNOWLIGHT",
            "XMAS",
            "HALLOWEEN"
        };

        private void CreateMenu()
        {
            // Créer le menu.
            menu = new Menu(Game.Player.Name, "Options météorologiques");

            dynamicWeatherEnabled = new MenuCheckboxItem("Activer/Désactiver la météo dynamique", "Active ou désactive les changements météorologiques dynamiques.", EventManager.DynamicWeatherEnabled);
            blackout = new MenuCheckboxItem("Activer/Désactiver le blackout", "Cela désactive ou active toutes les lumières sur la carte.", EventManager.IsBlackoutEnabled);
            snowEnabled = new MenuCheckboxItem("Activer les effets de neige", "Cela forcera la neige à apparaître au sol et activera les effets de particules de neige pour les peds et les véhicules. Combinez avec la météo X-MAS ou Neige légère pour de meilleurs résultats.", ConfigManager.GetSettingsBool(ConfigManager.Setting.vmenu_enable_snow));
            var extrasunny = new MenuItem("Très ensoleillé", "Réglez la météo sur ~y~très ensoleillé~s~ !") { ItemData = "EXTRASUNNY" };
            var clear = new MenuItem("Dégagé", "Réglez la météo sur ~y~dégagé~s~ !") { ItemData = "CLEAR" };
            var neutral = new MenuItem("Neutre", "Réglez la météo sur ~y~neutre~s~ !") { ItemData = "NEUTRAL" };
            var smog = new MenuItem("Smog", "Réglez la météo sur ~y~smog~s~ !") { ItemData = "SMOG" };
            var foggy = new MenuItem("Brumeux", "Réglez la météo sur ~y~brumeux~s~ !") { ItemData = "FOGGY" };
            var clouds = new MenuItem("Nuageux", "Réglez la météo sur ~y~nuageux~s~ !") { ItemData = "CLOUDS" };
            var overcast = new MenuItem("Couvert", "Réglez la météo sur ~y~couvert~s~ !") { ItemData = "OVERCAST" };
            var clearing = new MenuItem("Éclaircies", "Réglez la météo sur ~y~éclaircies~s~ !") { ItemData = "CLEARING" };
            var rain = new MenuItem("Pluvieux", "Réglez la météo sur ~y~pluie~s~ !") { ItemData = "RAIN" };
            var thunder = new MenuItem("Orage", "Réglez la météo sur ~y~orage~s~ !") { ItemData = "THUNDER" };
            var blizzard = new MenuItem("Blizzard", "Réglez la météo sur ~y~blizzard~s~ !") { ItemData = "BLIZZARD" };
            var snow = new MenuItem("Neige", "Réglez la météo sur ~y~neige~s~ !") { ItemData = "SNOW" };
            var snowlight = new MenuItem("Neige légère", "Réglez la météo sur ~y~neige légère~s~ !") { ItemData = "SNOWLIGHT" };
            var xmas = new MenuItem("Neige de Noël", "Réglez la météo sur ~y~neige de Noël~s~ !") { ItemData = "XMAS" };
            var halloween = new MenuItem("Halloween", "Réglez la météo sur ~y~halloween~s~ !") { ItemData = "HALLOWEEN" };
            var removeclouds = new MenuItem("Supprimer tous les nuages", "Supprime tous les nuages du ciel !");
            var randomizeclouds = new MenuItem("Nuages aléatoires", "Ajoute des nuages aléatoires dans le ciel !");

            if (IsAllowed(Permission.WODynamic))
            {
                menu.AddMenuItem(dynamicWeatherEnabled);
            }
            if (IsAllowed(Permission.WOBlackout))
            {
                menu.AddMenuItem(blackout);
            }
            if (IsAllowed(Permission.WOSetWeather))
            {
                menu.AddMenuItem(snowEnabled);
                menu.AddMenuItem(extrasunny);
                menu.AddMenuItem(clear);
                menu.AddMenuItem(neutral);
                menu.AddMenuItem(smog);
                menu.AddMenuItem(foggy);
                menu.AddMenuItem(clouds);
                menu.AddMenuItem(overcast);
                menu.AddMenuItem(clearing);
                menu.AddMenuItem(rain);
                menu.AddMenuItem(thunder);
                menu.AddMenuItem(blizzard);
                menu.AddMenuItem(snow);
                menu.AddMenuItem(snowlight);
                menu.AddMenuItem(xmas);
                menu.AddMenuItem(halloween);
            }
            if (IsAllowed(Permission.WORandomizeClouds))
            {
                menu.AddMenuItem(randomizeclouds);
            }

            if (IsAllowed(Permission.WORemoveClouds))
            {
                menu.AddMenuItem(removeclouds);
            }

            menu.OnItemSelect += (sender, item, index2) =>
            {
                if (item == removeclouds)
                {
                    ModifyClouds(true); // Supprimer tous les nuages
                }
                else if (item == randomizeclouds)
                {
                    ModifyClouds(false); // Ajouter des nuages aléatoires
                }
                else if (item.ItemData is string weatherType)
                {
                    Notify.Custom($"La météo va être changée en ~y~{item.Text}~s~. Cela prendra {EventManager.WeatherChangeTime} secondes.");
                    UpdateServerWeather(weatherType, EventManager.IsBlackoutEnabled, EventManager.DynamicWeatherEnabled, EventManager.IsSnowEnabled);
                }
            };

            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == dynamicWeatherEnabled)
                {
                    Notify.Custom($"Les changements météorologiques dynamiques sont maintenant {(_checked ? "~g~activés" : "~r~désactivés")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, EventManager.IsBlackoutEnabled, _checked, EventManager.IsSnowEnabled);
                }
                else if (item == blackout)
                {
                    Notify.Custom($"Le mode blackout est maintenant {(_checked ? "~g~activé" : "~r~désactivé")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, _checked, EventManager.DynamicWeatherEnabled, EventManager.IsSnowEnabled);
                }
                else if (item == snowEnabled)
                {
                    Notify.Custom($"Les effets de neige seront maintenant {(_checked ? "~g~activés" : "~r~désactivés")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, EventManager.IsBlackoutEnabled, EventManager.DynamicWeatherEnabled, _checked);
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
    }
}
