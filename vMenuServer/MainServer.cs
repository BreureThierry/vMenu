using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;

using Newtonsoft.Json;

using vMenuShared;

using static CitizenFX.Core.Native.API;
using static vMenuServer.DebugLog;
using static vMenuShared.ConfigManager;

namespace vMenuServer
{

    public static class DebugLog
    {
        public enum LogLevel
        {
            error = 1,
            success = 2,
            info = 4,
            warning = 3,
            none = 0
        }

        /// <summary>
        /// Global log data function, only logs when debugging is enabled.
        /// </summary>
        /// <param name="data"></param>
        public static void Log(dynamic data, LogLevel level = LogLevel.none)
        {
            if (MainServer.DebugMode || level == LogLevel.error || level == LogLevel.warning)
            {
                var prefix = "[vMenu] ";
                if (level == LogLevel.error)
                {
                    prefix = "^1[vMenu] [ERREUR]^7 ";
                }
                else if (level == LogLevel.info)
                {
                    prefix = "^5[vMenu] [INFO]^7 ";
                }
                else if (level == LogLevel.success)
                {
                    prefix = "^2[vMenu] [OK]^7 ";
                }
                else if (level == LogLevel.warning)
                {
                    prefix = "^3[vMenu] [WARNING]^7 ";
                }
                Debug.WriteLine($"{prefix}[DEBUG LOG] {data.ToString()}");
            }
        }
    }

    public class MainServer : BaseScript
    {
        #region vars
        // Debug shows more information when doing certain things. Leave it off to improve performance!
        public static bool DebugMode = GetResourceMetadata(GetCurrentResourceName(), "server_debug_mode", 0) == "true";

        public static string Version { get { return GetResourceMetadata(GetCurrentResourceName(), "version", 0); } }

        // Time
        private int CurrentHours
        {
            get { return MathUtil.Clamp(GetSettingsInt(Setting.vmenu_current_hour), 0, 23); }
            set { SetConvarReplicated(Setting.vmenu_current_hour.ToString(), MathUtil.Clamp(value, 0, 23).ToString()); }
        }
        private int CurrentMinutes
        {
            get { return MathUtil.Clamp(GetSettingsInt(Setting.vmenu_current_minute), 0, 59); }
            set { SetConvarReplicated(Setting.vmenu_current_minute.ToString(), MathUtil.Clamp(value, 0, 59).ToString()); }
        }
        private int MinuteClockSpeed
        {
            get
            {
                var value = GetSettingsInt(Setting.vmenu_ingame_minute_duration);
                if (value < 100)
                {
                    value = 2000;
                }

                return value;
            }
        }
        private bool FreezeTime
        {
            get { return GetSettingsBool(Setting.vmenu_freeze_time); }
            set { SetConvarReplicated(Setting.vmenu_freeze_time.ToString(), value.ToString().ToLower()); }
        }
        private bool IsServerTimeSynced { get { return GetSettingsBool(Setting.vmenu_sync_to_machine_time); } }


        // Weather
        private string CurrentWeather
        {
            get
            {
                var value = GetSettingsString(Setting.vmenu_current_weather, "CLEAR");
                if (!WeatherTypes.Contains(value.ToUpper()))
                {
                    return "CLEAR";
                }
                return value;
            }
            set
            {
                if (string.IsNullOrEmpty(value) || !WeatherTypes.Contains(value.ToUpper()))
                {
                    SetConvarReplicated(Setting.vmenu_current_weather.ToString(), "CLEAR");
                }
                SetConvarReplicated(Setting.vmenu_current_weather.ToString(), value.ToUpper());
            }
        }
        private bool DynamicWeatherEnabled
        {
            get { return GetSettingsBool(Setting.vmenu_enable_dynamic_weather); }
            set { SetConvarReplicated(Setting.vmenu_enable_dynamic_weather.ToString(), value.ToString().ToLower()); }
        }
        private bool ManualSnowEnabled
        {
            get { return GetSettingsBool(Setting.vmenu_enable_snow); }
            set { SetConvarReplicated(Setting.vmenu_enable_snow.ToString(), value.ToString().ToLower()); }
        }
        private bool BlackoutEnabled
        {
            get { return GetSettingsBool(Setting.vmenu_blackout_enabled); }
            set { SetConvarReplicated(Setting.vmenu_blackout_enabled.ToString(), value.ToString().ToLower()); }
        }
        private int DynamicWeatherMinutes
        {
            get { return Math.Max(GetSettingsInt(Setting.vmenu_dynamic_weather_timer), 1); }
        }
        private long lastWeatherChange = 0;

        private readonly List<string> CloudTypes = new()
        {
            "Cloudy 01",
            "RAIN",
            "horizonband1",
            "horizonband2",
            "Puffs",
            "Wispy",
            "Horizon",
            "Stormy 01",
            "Clear 01",
            "Snowy 01",
            "Contrails",
            "altostratus",
            "Nimbus",
            "Cirrus",
            "cirrocumulus",
            "stratoscumulus",
            "horizonband3",
            "Stripey",
            "horsey",
            "shower",
        };
        private readonly List<string> WeatherTypes = new()
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
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainServer()
        {
            // name check
            if (GetCurrentResourceName() != "vMenu")
            {
                var InvalidNameException = new Exception("\r\n\r\n^1[vMenu] INSTALLATION ERROR!\r\nLe nom de la ressource n'est pas valide. " +
                    "Veuillez changer le nom du dossier de '^3" + GetCurrentResourceName() + "^1' vers '^2vMenu^1' (sensible à la casse) à la place !\r\n\r\n\r\n^7");
                try
                {
                    throw InvalidNameException;
                }
                catch (Exception e)
                {
                    Debug.Write(e.Message);
                }
            }
            else
            {
                // Add event handlers.
                EventHandlers.Add("vMenu:GetPlayerIdentifiers", new Action<int, NetworkCallbackDelegate>((TargetPlayer, CallbackFunction) =>
                {
                    var data = new List<string>();
                    Players[TargetPlayer].Identifiers.ToList().ForEach(e =>
                    {
                        if (!e.Contains("ip:"))
                        {
                            data.Add(e);
                        }
                    });
                    CallbackFunction(JsonConvert.SerializeObject(data));
                }));
                EventHandlers.Add("vMenu:RequestPermissions", new Action<Player>(PermissionsManager.SetPermissionsForPlayer));
                EventHandlers.Add("vMenu:RequestServerState", new Action<Player>(RequestServerStateFromPlayer));

                // check addons file for errors
                var addons = LoadResourceFile(GetCurrentResourceName(), "config/addons.json") ?? "{}";
                try
                {
                    JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(addons);
                    // If the above crashes, then the json is invalid and it'll throw warnings in the console.
                }
                catch (JsonReaderException ex)
                {
                    Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Votre fichier addons.json contient un problème ! Détails de l'erreur : {ex.Message}\n\n");
                }

                // check extras file for errors
                string extras = LoadResourceFile(GetCurrentResourceName(), "config/extras.json") ?? "{}";
                try
                {
                    JsonConvert.DeserializeObject<Dictionary<string, Dictionary<int, string>>>(extras);
                    // If the above crashes, then the json is invalid and it'll throw warnings in the console.
                }
                catch (JsonReaderException ex)
                {
                    Debug.WriteLine($"\n\n^1[vMenu] [ERROR] ^7Votre fichier extras.json contient un problème ! Détails de l'erreur : {ex.Message}\n\n");
                }

                // check if permissions are setup (correctly)
                if (!GetSettingsBool(Setting.vmenu_use_permissions))
                {
                    Debug.WriteLine("^3[vMenu] [WARNING] vMenu est configuré pour ignorer les autorisations !\nSi vous l'avez fait exprès, vous pouvez ignorer cet avertissement.\nSi vous ne l'avez pas fait exprès, c'est que vous avez commis une erreur lors de la configuration de vMenu.\nVeuillez lire la documentation de vMenu (^5https://docs.vespura.com/vmenu^3).\nIl est très probable que vous n'exécutiez pas (correctement) le fichier permissions.cfg.^7");
                }

                Tick += PlayersFirstTick;

                // Start the loops
                if (GetSettingsBool(Setting.vmenu_enable_weather_sync))
                {
                    Tick += WeatherLoop;
                }

                if (GetSettingsBool(Setting.vmenu_enable_time_sync))
                {
                    Tick += TimeLoop;
                }
            }
        }
        #endregion

        #region command handler
        [Command("vmenuserver", Restricted = true)]
        internal void ServerCommandHandler(int source, List<object> args, string _)
        {
            if (args != null)
            {
                if (args.Count > 0)
                {
                    if (args[0].ToString().ToLower() == "debug")
                    {
                        DebugMode = !DebugMode;
                        if (source < 1)
                        {
                            Debug.WriteLine($"Debug mode est maintenant réglé sur : {DebugMode}.");
                        }
                        else
                        {
                            Players[source].TriggerEvent("chatMessage", $"vMenu Le mode debug est maintenant réglé sur : {DebugMode}.");
                        }
                        return;
                    }
                    else if (args[0].ToString().ToLower() == "unban" && (source < 1))
                    {
                        if (args.Count() > 1 && !string.IsNullOrEmpty(args[1].ToString()))
                        {
                            var uuid = args[1].ToString().Trim();
                            var bans = BanManager.GetBanList();
                            var banRecord = bans.Find(b => { return b.uuid.ToString() == uuid; });
                            if (banRecord != null)
                            {
                                BanManager.RemoveBan(banRecord);
                                Debug.WriteLine("Le joueur a été déban avec succès.");
                            }
                            else
                            {
                                Debug.WriteLine($"Impossible de trouver un joueur banni avec l'uuid fourni. '{uuid}'.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Vous n'avez pas spécifié de joueur à déban, vous devez entrer le nom de joueur COMPLET. Utilisation : vmenuserver unban \"playername\"");
                        }
                        return;
                    }
                    else if (args[0].ToString().ToLower() == "weather")
                    {
                        if (args.Count < 2 || string.IsNullOrEmpty(args[1].ToString()))
                        {
                            Debug.WriteLine("[vMenu] Syntaxe de commande invalide. Utiliser 'vmenuserver weather <weatherType>' instead.");
                        }
                        else
                        {
                            var wtype = args[1].ToString().ToUpper();
                            if (WeatherTypes.Contains(wtype))
                            {
                                TriggerEvent("vMenu:UpdateServerWeather", wtype, BlackoutEnabled, DynamicWeatherEnabled, ManualSnowEnabled);
                                Debug.WriteLine($"[vMenu] La météo est maintenant réglée sur : {wtype}");
                            }
                            else if (wtype.ToLower() == "dynamic")
                            {
                                if (args.Count == 3 && !string.IsNullOrEmpty(args[2].ToString()))
                                {
                                    if ((args[2].ToString().ToLower() ?? $"{DynamicWeatherEnabled}") == "true")
                                    {
                                        TriggerEvent("vMenu:UpdateServerWeather", CurrentWeather, BlackoutEnabled, true, ManualSnowEnabled);
                                        Debug.WriteLine("[vMenu] La météo dynamique est maintenant activée.");
                                    }
                                    else if ((args[2].ToString().ToLower() ?? $"{DynamicWeatherEnabled}") == "false")
                                    {
                                        TriggerEvent("vMenu:UpdateServerWeather", CurrentWeather, BlackoutEnabled, false, ManualSnowEnabled);
                                        Debug.WriteLine("[vMenu] La météo dynamique est désormais désactivée.");
                                    }
                                    else
                                    {
                                        Debug.WriteLine("[vMenu] Utilisation incorrecte de la commande. Syntaxe correcte : vmenuserver weather dynamic <true|false>");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("[vMenu] Utilisation incorrecte de la commande. Syntaxe correcte : vmenuserver weather dynamic <true|false>");
                                }

                            }
                            else
                            {
                                Debug.WriteLine("[vMenu] Ce type de temps n'est pas valide !");
                            }
                        }
                    }
                    else if (args[0].ToString().ToLower() == "time")
                    {
                        if (args.Count == 2)
                        {
                            if (args[1].ToString().ToLower() == "freeze")
                            {
                                TriggerEvent("vMenu:UpdateServerTime", CurrentHours, CurrentMinutes, !FreezeTime);
                                Debug.WriteLine($"Time is now {(FreezeTime ? "frozen" : "not frozen")}.");
                            }
                            else
                            {
                                Debug.WriteLine("Syntaxe non valide. Utiliser : ^5vmenuserver time <freeze|<hour> <minute>>^7 instead.");
                            }
                        }
                        else if (args.Count > 2)
                        {
                            if (int.TryParse(args[1].ToString(), out var hour))
                            {
                                if (int.TryParse(args[2].ToString(), out var minute))
                                {
                                    if (hour is >= 0 and < 24)
                                    {
                                        if (minute is >= 0 and < 60)
                                        {
                                            TriggerEvent("vMenu:UpdateServerTime", hour, minute, FreezeTime);
                                            Debug.WriteLine($"Time is now {(hour < 10 ? ("0" + hour.ToString()) : hour.ToString())}:{(minute < 10 ? ("0" + minute.ToString()) : minute.ToString())}.");
                                        }
                                        else
                                        {
                                            Debug.WriteLine("Invalid minute provided. Value must be between 0 and 59.");
                                        }
                                    }
                                    else
                                    {
                                        Debug.WriteLine("Invalid hour provided. Value must be between 0 and 23.");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("Syntaxe non valide. Utiliser : ^5vmenuserver time <freeze|<hour> <minute>>^7 instead.");
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Syntaxe non valide. Utiliser : ^5vmenuserver time <freeze|<hour> <minute>>^7 instead.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Syntaxe non valide. Utiliser : ^5vmenuserver time <freeze|<hour> <minute>>^7 instead.");
                        }
                    }
                    else if (args[0].ToString().ToLower() == "ban" && source < 1)  // only do this via server console (server id < 1)
                    {
                        if (args.Count > 3)
                        {
                            Player p = null;

                            var findByServerId = args[1].ToString().ToLower() == "id";
                            var identifier = args[2].ToString().ToLower();

                            if (findByServerId)
                            {
                                if (Players.Any(player => player.Handle == identifier))
                                {
                                    p = Players.Single(pl => pl.Handle == identifier);
                                }
                                else
                                {
                                    Debug.WriteLine("[vMenu] Ce joueur n'a pas été trouvé, assurez-vous qu'il est en ligne.");
                                    return;
                                }
                            }
                            else
                            {
                                if (Players.Any(player => player.Name.ToLower() == identifier.ToLower()))
                                {
                                    p = Players.Single(pl => pl.Name.ToLower() == identifier.ToLower());
                                }
                                else
                                {
                                    Debug.WriteLine("[vMenu] Ce joueur n'a pas été trouvé, assurez-vous qu'il est en ligne.");
                                    return;
                                }
                            }

                            var reason = "Ban par le staff pour :";
                            args.GetRange(3, args.Count - 3).ForEach(arg => reason += " " + arg);

                            if (p != null)
                            {
                                var ban = new BanManager.BanRecord(
                                    BanManager.GetSafePlayerName(p.Name),
                                    p.Identifiers.ToList(),
                                    new DateTime(3000, 1, 1),
                                    reason,
                                    "Console du serveur",
                                    new Guid()
                                );

                                BanManager.AddBan(ban);
                                BanManager.BanLog($"[vMenu] Player {p.Name}^7 a été ban par la Console du serveur pour [{reason}].");
                                TriggerEvent("vMenu:BanSuccessful", JsonConvert.SerializeObject(ban).ToString());
                                var timeRemaining = BanManager.GetRemainingTimeMessage(ban.bannedUntil.Subtract(DateTime.Now));
                                p.Drop($"Vous êtes ban de ce serveur. Temps de ban restant : {timeRemaining}. Ban par: {ban.bannedBy}. Ban raison : {ban.banReason}. Information complémentaire : {vMenuShared.ConfigManager.GetSettingsString(vMenuShared.ConfigManager.Setting.vmenu_default_ban_message_information)}.");
                            }
                            else
                            {
                                Debug.WriteLine("[vMenu] Le joueur n'a pas été trouvé, il n'a pas été possible de le bannir.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("[vMenu] Manque d'arguments, syntaxe: ^5vmenuserver ban <id|name> <server id|username> <reason>^7.");
                        }
                    }
                    else if (args[0].ToString().ToLower() == "help")
                    {
                        Debug.WriteLine("Available commands:");
                        Debug.WriteLine("(server console only): vmenuserver ban <id|name> <server id|username> <reason> (le joueur doit être en ligne !)");
                        Debug.WriteLine("(server console only): vmenuserver unban <uuid>");
                        Debug.WriteLine("vmenuserver weather <new weather type | dynamic <true | false>>");
                        Debug.WriteLine("vmenuserver time <freeze|<hour> <minute>>");
                        Debug.WriteLine("vmenuserver migrate (Ceci copie tous les joueurs bannis dans le fichier bans.json vers le nouveau système de bannissement dans vMenu v3.3.0, vous ne devez le faire qu'une seule fois)");
                    }
                    else if (args[0].ToString().ToLower() == "migrate" && source < 1)
                    {
                        var file = LoadResourceFile(GetCurrentResourceName(), "bans.json");
                        if (string.IsNullOrEmpty(file) || file == "[]")
                        {
                            Debug.WriteLine("&1[vMenu] [ERREUR]^7 Aucun fichier bans.json n'a été trouvé ou il est vide.");
                            return;
                        }
                        Debug.WriteLine("^5[vMenu] [INFO]^7 Importation de tous les enregistrements de bannissement du fichier bans.json dans le nouveau système de stockage. ^3Cela peut prendre un certain temps...^7");
                        var bans = JsonConvert.DeserializeObject<List<BanManager.BanRecord>>(file);
                        bans.ForEach((br) =>
                        {
                            var record = new BanManager.BanRecord(br.playerName, br.identifiers, br.bannedUntil, br.banReason, br.bannedBy, Guid.NewGuid());
                            BanManager.AddBan(record);
                        });
                        Debug.WriteLine("^2[vMenu] [OK]^7 Tous les enregistrements d'interdiction ont été importés. Vous n'avez plus besoin du fichier bans.json.");
                    }
                    else
                    {
                        Debug.WriteLine($"vMenu - version actuelle : {Version}. Essayez ^5vmenuserver help^7 pour plus d'informations.");
                    }
                }
                else
                {
                    Debug.WriteLine($"vMenu - version actuelle : {Version}. Essayez ^5vmenuserver help^7 pour plus d'informations.");
                }
            }
            else
            {
                Debug.WriteLine($"vMenu - version actuelle : {Version}. Essayez ^5vmenuserver help^7 pour plus d'informations.");
            }
        }
        #endregion

        #region kick players from personal vehicle
        /// <summary>
        /// Makes the player leave the personal vehicle.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="vehicleNetId"></param>
        /// <param name="playerOwner"></param>
        [EventHandler("vMenu:GetOutOfCar")]
        internal void GetOutOfCar([FromSource] Player source, int vehicleNetId, int playerOwner)
        {
            if (source != null)
            {
                if (vMenuShared.PermissionsManager.GetPermissionAndParentPermissions(vMenuShared.PermissionsManager.Permission.PVKickPassengers).Any(perm => vMenuShared.PermissionsManager.IsAllowed(perm, source)))
                {
                    TriggerClientEvent("vMenu:GetOutOfCar", vehicleNetId, playerOwner);
                    source.TriggerEvent("vMenu:Notify", "Tous les passagers seront expulsés dès que le véhicule s'arrêtera de rouler, ou après 10 secondes s'ils refusent d'arrêter le véhicule.");
                }
            }
        }
        #endregion

        #region clear area near pos
        /// <summary>
        /// Clear the area near this point for all players.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        [EventHandler("vMenu:ClearArea")]
        internal void ClearAreaNearPos(float x, float y, float z)
        {
            TriggerClientEvent("vMenu:ClearArea", x, y, z);
        }
        #endregion

        #region Manage weather and time changes.
        /// <summary>
        /// Loop used for syncing and keeping track of the time in-game.
        /// </summary>
        /// <returns></returns>
        private async Task TimeLoop()
        {
            if (IsServerTimeSynced)
            {
                var currentTime = DateTime.Now;
                CurrentMinutes = currentTime.Minute;
                CurrentHours = currentTime.Hour;

                // Update this once every 60 seconds.
                await Delay(60000);
            }
            else
            {
                if (!FreezeTime)
                {
                    if ((CurrentMinutes + 1) > 59)
                    {
                        CurrentMinutes = 0;
                        if ((CurrentHours + 1) > 23)
                        {
                            CurrentHours = 0;
                        }
                        else
                        {
                            CurrentHours++;
                        }
                    }
                    else
                    {
                        CurrentMinutes++;
                    }
                }
                await Delay(MinuteClockSpeed);
            }
        }

        /// <summary>
        /// Task used for syncing and changing weather dynamically.
        /// </summary>
        /// <returns></returns>
        private async Task WeatherLoop()
        {
            if (DynamicWeatherEnabled)
            {
                await Delay(DynamicWeatherMinutes * 60000);

                if (GetSettingsBool(Setting.vmenu_enable_weather_sync))
                {
                    // Manage dynamic weather changes.

                    {
                        // Disable dynamic weather because these weather types shouldn't randomly change.
                        if (CurrentWeather is "XMAS" or "HALLOWEEN" or "NEUTRAL")
                        {
                            DynamicWeatherEnabled = false;
                            return;
                        }

                        // Is it time to generate a new weather type?
                        if (GetGameTimer() - lastWeatherChange > (DynamicWeatherMinutes * 60000))
                        {
                            // Choose a new semi-random weather type.
                            RefreshWeather();

                            // Log if debug mode is on how long the change has taken and what the new weather type will be.
                            if (DebugMode)
                            {
                                Log($"Changing weather, new weather: {CurrentWeather}");
                            }
                        }
                    }
                }
            }
            else
            {
                await Delay(5000);
            }
        }

        /// <summary>
        /// Select a new random weather type, based on the current weather and some patterns.
        /// </summary>
        private void RefreshWeather()
        {
            var random = new Random().Next(20);
            if (CurrentWeather is "RAIN" or "THUNDER")
            {
                CurrentWeather = "CLEARING";
            }
            else if (CurrentWeather == "CLEARING")
            {
                CurrentWeather = "CLOUDS";
            }
            else
            {
                CurrentWeather = random switch
                {
                    0 or 1 or 2 or 3 or 4 or 5 => CurrentWeather == "EXTRASUNNY" ? "CLEAR" : "EXTRASUNNY",
                    6 or 7 or 8 => CurrentWeather == "SMOG" ? "FOGGY" : "SMOG",
                    9 or 10 or 11 => CurrentWeather == "CLOUDS" ? "OVERCAST" : "CLOUDS",
                    12 or 13 or 14 => CurrentWeather == "CLOUDS" ? "OVERCAST" : "CLOUDS",
                    15 => CurrentWeather == "OVERCAST" ? "THUNDER" : "OVERCAST",
                    16 => CurrentWeather == "CLOUDS" ? "EXTRASUNNY" : "RAIN",
                    _ => CurrentWeather == "FOGGY" ? "SMOG" : "FOGGY",
                };
            }

        }
        #endregion

        #region Sync weather & time with clients
        /// <summary>
        /// Update the weather for all clients.
        /// </summary>
        /// <param name="newWeather"></param>
        /// <param name="blackoutNew"></param>
        /// <param name="dynamicWeatherNew"></param>
        [EventHandler("vMenu:UpdateServerWeather")]
        internal void UpdateWeather(string newWeather, bool blackoutNew, bool dynamicWeatherNew, bool enableSnow)
        {

            // Automatically enable snow effects whenever one of the snow weather types is selected.
            if (newWeather is "XMAS" or "SNOWLIGHT" or "SNOW" or "BLIZZARD")
            {
                enableSnow = true;
            }

            // Update the new weather related variables.
            CurrentWeather = newWeather;
            BlackoutEnabled = blackoutNew;
            DynamicWeatherEnabled = dynamicWeatherNew;
            ManualSnowEnabled = enableSnow;

            // Reset the dynamic weather loop timer to another (default) 10 mintues.
            lastWeatherChange = GetGameTimer();
        }

        /// <summary>
        /// Set a new random clouds type and opacity for all clients.
        /// </summary>
        /// <param name="removeClouds"></param>
        [EventHandler("vMenu:UpdateServerWeatherCloudsType")]
        internal void UpdateWeatherCloudsType(bool removeClouds)
        {
            if (removeClouds)
            {
                TriggerClientEvent("vMenu:SetClouds", 0f, "removed");
            }
            else
            {
                var opacity = float.Parse(new Random().NextDouble().ToString());
                var type = CloudTypes[new Random().Next(0, CloudTypes.Count)];
                TriggerClientEvent("vMenu:SetClouds", opacity, type);
            }
        }

        /// <summary>
        /// Set and sync the time to all clients.
        /// </summary>
        /// <param name="newHours"></param>
        /// <param name="newMinutes"></param>
        /// <param name="freezeTimeNew"></param>
        [EventHandler("vMenu:UpdateServerTime")]
        internal void UpdateTime(int newHours, int newMinutes, bool freezeTimeNew)
        {
            CurrentHours = newHours;
            CurrentMinutes = newMinutes;
            FreezeTime = freezeTimeNew;
        }
        #endregion

        #region Online Players Menu Actions
        /// <summary>
        /// Kick a specific player.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="kickReason"></param>
        [EventHandler("vMenu:KickPlayer")]
        internal void KickPlayer([FromSource] Player source, int target, string kickReason = "Vous avez été kick du serveur.")
        {
            if (IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.Kick") || IsPlayerAceAllowed(source.Handle, "vMenu.Everything") ||
                IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.All"))
            {
                // If the player is allowed to be kicked.
                var targetPlayer = Players[target];
                if (targetPlayer != null)
                {
                    if (!IsPlayerAceAllowed(targetPlayer.Handle, "vMenu.DontKickMe"))
                    {
                        TriggerEvent("vMenu:KickSuccessful", source.Name, kickReason, targetPlayer.Name);

                        KickLog($"Player: {source.Name} has kicked: {targetPlayer.Name} for: {kickReason}.");
                        TriggerClientEvent(player: source, eventName: "vMenu:Notify", args: $"Le joueur ciblé (<C>{targetPlayer.Name}</C>) a été mis à kick.");

                        // Kick the player from the server using the specified reason.
                        DropPlayer(targetPlayer.Handle, kickReason);
                        return;
                    }
                    // Trigger the client event on the source player to let them know that kicking this player is not allowed.
                    TriggerClientEvent(player: source, eventName: "vMenu:Notify", args: "Désolé, ce joueur ~r~ne peut pas~s~ être kické.");
                    return;
                }
                TriggerClientEvent(player: source, eventName: "vMenu:Notify", args: "Une erreur inconnue s'est produite. Signalez-la ici : vespura.com/vmenu");
            }
            else
            {
                BanManager.BanCheater(source);
            }
        }

        /// <summary>
        /// Kill a specific player.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        [EventHandler("vMenu:KillPlayer")]
        internal void KillPlayer([FromSource] Player source, int target)
        {
            if (IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.Kill") || IsPlayerAceAllowed(source.Handle, "vMenu.Everything") ||
                IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.All"))
            {
                var targetPlayer = Players[target];
                if (targetPlayer != null)
                {
                    // Trigger the client event on the target player to make them kill themselves. R.I.P.
                    TriggerClientEvent(player: targetPlayer, eventName: "vMenu:KillMe", args: source.Name);
                    return;
                }
                TriggerClientEvent(player: source, eventName: "vMenu:Notify", args: "Une erreur inconnue s'est produite. Signalez-la ici : vespura.com/vmenu");
            }
            else
            {
                BanManager.BanCheater(source);
            }
        }

        /// <summary>
        /// Teleport a specific player to another player.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        [EventHandler("vMenu:SummonPlayer")]
        internal void SummonPlayer([FromSource] Player source, int target)
        {
            if (IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.Summon") || IsPlayerAceAllowed(source.Handle, "vMenu.Everything") ||
                IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.All"))
            {
                // Trigger the client event on the target player to make them teleport to the source player.
                var targetPlayer = Players[target];
                if (targetPlayer != null)
                {
                    TriggerClientEvent(player: targetPlayer, eventName: "vMenu:GoToPlayer", args: source.Handle);
                    return;
                }
                TriggerClientEvent(player: source, eventName: "vMenu:Notify", args: "Une erreur inconnue s'est produite. Signalez-la ici : vespura.com/vmenu");
            }
            else
            {
                BanManager.BanCheater(source);
            }
        }

        [EventHandler("vMenu:SendMessageToPlayer")]
        internal void SendPrivateMessage([FromSource] Player source, int targetServerId, string message)
        {
            var targetPlayer = Players[targetServerId];
            if (targetPlayer != null)
            {
                targetPlayer.TriggerEvent("vMenu:PrivateMessage", source.Handle, message);

                foreach (var p in Players)
                {
                    if (p != source && p != targetPlayer)
                    {
                        if (vMenuShared.PermissionsManager.IsAllowed(vMenuShared.PermissionsManager.Permission.OPSeePrivateMessages, p))
                        {
                            p.TriggerEvent("vMenu:Notify", $"[vMenu Staff Log] <C>{source.Name}</C>~s~ sent a PM to <C>{targetPlayer.Name}</C>~s~: {message}");
                        }
                    }
                }
            }
        }

        [EventHandler("vMenu:PmsDisabled")]
        internal void NotifySenderThatDmsAreDisabled([FromSource] Player source, string senderServerId)
        {
            var p = Players[int.Parse(senderServerId)];
            p?.TriggerEvent("vMenu:Notify", $"Désolé, votre message privé à <C>{source.Name}</C>~s~ n'a pas pu être délivré parce qu'ils ont désactivé les messages privés.");
        }
        #endregion

        #region logging and update checks notifications
        /// <summary>
        /// If enabled using convars, will log all kick actions to the server console as well as an external file.
        /// </summary>
        /// <param name="kickLogMesage"></param>
        private static void KickLog(string kickLogMesage)
        {
            //if (GetConvar("vMenuLogKickActions", "true") == "true")
            if (GetSettingsBool(Setting.vmenu_log_kick_actions))
            {
                var file = LoadResourceFile(GetCurrentResourceName(), "vmenu.log") ?? "";
                var date = DateTime.Now;
                var formattedDate = (date.Day < 10 ? "0" : "") + date.Day + "-" +
                    (date.Month < 10 ? "0" : "") + date.Month + "-" +
                    (date.Year < 10 ? "0" : "") + date.Year + " " +
                    (date.Hour < 10 ? "0" : "") + date.Hour + ":" +
                    (date.Minute < 10 ? "0" : "") + date.Minute + ":" +
                    (date.Second < 10 ? "0" : "") + date.Second;
                var outputFile = file + $"[\t{formattedDate}\t] [KICK ACTION] {kickLogMesage}\n";
                SaveResourceFile(GetCurrentResourceName(), "vmenu.log", outputFile, -1);
                Debug.WriteLine("^3[vMenu] [KICK]^7 " + kickLogMesage + "\n");
            }
        }

        #endregion

        #region Add teleport location
        [EventHandler("vMenu:SaveTeleportLocation")]
        internal void AddTeleportLocation([FromSource] Player _, string locationJson)
        {
            var location = JsonConvert.DeserializeObject<TeleportLocation>(locationJson);
            if (GetTeleportLocationsData().Any(loc => loc.name == location.name))
            {
                Log("Un lieu de téléportation portant ce nom existe déjà, le lieu n'a pas été enregistré.", LogLevel.error);
                return;
            }
            var locs = GetLocations();
            locs.teleports.Add(location);
            if (!SaveResourceFile(GetCurrentResourceName(), "config/locations.json", JsonConvert.SerializeObject(locs, Formatting.Indented), -1))
            {
                Log("Impossible d'enregistrer le fichier locations.json, raison inconnue.", LogLevel.error);
            }
            TriggerClientEvent("vMenu:UpdateTeleportLocations", JsonConvert.SerializeObject(locs.teleports));
        }
        #endregion

        #region Infinity bits
        private void RequestServerStateFromPlayer([FromSource] Player player)
        {
            player.TriggerEvent("vMenu:SetServerState", new
            {
                IsInfinity = GetConvar("onesync_enableInfinity", "false") == "true"
            });
        }

        [EventHandler("vMenu:RequestPlayerList")]
        internal void RequestPlayerListFromPlayer([FromSource] Player player)
        {
            player.TriggerEvent("vMenu:ReceivePlayerList", Players.Select(p => new
            {
                n = p.Name,
                s = int.Parse(p.Handle),
            }));
        }

        [EventHandler("vMenu:GetPlayerCoords")]
        internal void GetPlayerCoords([FromSource] Player source, int playerId, NetworkCallbackDelegate callback)
        {
            if (IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.Teleport") || IsPlayerAceAllowed(source.Handle, "vMenu.Everything") ||
                IsPlayerAceAllowed(source.Handle, "vMenu.OnlinePlayers.All"))
            {
                var coords = Players[playerId]?.Character?.Position ?? Vector3.Zero;

                _ = callback(coords);

                return;
            }

            _ = callback(Vector3.Zero);
        }
        #endregion

        #region Player join/quit
        private readonly HashSet<string> joinedPlayers = new();

        private Task PlayersFirstTick()
        {
            Tick -= PlayersFirstTick;

            foreach (var player in Players)
            {
                joinedPlayers.Add(player.Handle);
            }

            return Task.FromResult(0);
        }

        [EventHandler("playerJoining")]
        internal void OnPlayerJoining([FromSource] Player sourcePlayer)
        {
            joinedPlayers.Add(sourcePlayer.Handle);

            foreach (var player in Players)
            {
                if (IsPlayerAceAllowed(player.Handle, "vMenu.MiscSettings.JoinQuitNotifs") ||
                    IsPlayerAceAllowed(player.Handle, "vMenu.MiscSettings.All"))
                {
                    player.TriggerEvent("vMenu:PlayerJoinQuit", sourcePlayer.Name, null);
                }
            }
        }

        [EventHandler("playerDropped")]
        internal void OnPlayerDropped([FromSource] Player sourcePlayer, string reason)
        {
            if (!joinedPlayers.Contains(sourcePlayer.Handle))
            {
                return;
            }

            joinedPlayers.Remove(sourcePlayer.Handle);

            foreach (var player in Players)
            {
                if (IsPlayerAceAllowed(player.Handle, "vMenu.MiscSettings.JoinQuitNotifs") ||
                    IsPlayerAceAllowed(player.Handle, "vMenu.MiscSettings.All"))
                {
                    player.TriggerEvent("vMenu:PlayerJoinQuit", sourcePlayer.Name, reason);
                }
            }
        }
        #endregion
    }
}
