using System.Collections.Generic;

using CitizenFX.Core;

using MenuAPI;

using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class VoiceChat
    {
        // Variables
        private Menu menu;
        public bool EnableVoicechat = UserDefaults.VoiceChatEnabled;
        public bool ShowCurrentSpeaker = UserDefaults.ShowCurrentSpeaker;
        public bool ShowVoiceStatus = UserDefaults.ShowVoiceStatus;
        public float currentProximity = (GetSettingsFloat(Setting.vmenu_override_voicechat_default_range) != 0.0) ? GetSettingsFloat(Setting.vmenu_override_voicechat_default_range) : UserDefaults.VoiceChatProximity;
        public List<string> channels = new()
        {
            "Canal 1 (Defaut)",
            "Canal 2",
            "Canal 3",
            "Canal 4",
        };
        public string currentChannel;
        private readonly List<float> proximityRange = new()
        {
            5f, // 5m
            10f, // 10m
            15f, // 15m
            20f, // 20m
            100f, // 100m
            300f, // 300m
            1000f, // 1.000km
            2000f, // 2.000km
            0f, // global
        };


        private void CreateMenu()
        {
            currentChannel = channels[0];
            if (IsAllowed(Permission.VCStaffChannel))
            {
                channels.Add("Canal Staff");
            }

            // Create the menu.
            menu = new Menu(Game.Player.Name, "Paramètres du chat vocal");

            var voiceChatEnabled = new MenuCheckboxItem("Activer le chat vocal", "Activer ou désactiver le chat vocal.", EnableVoicechat);
            var showCurrentSpeaker = new MenuCheckboxItem("Afficher l'interlocuteur actuel", "Affiche qui est en train de parler.", ShowCurrentSpeaker);
            var showVoiceStatus = new MenuCheckboxItem("Afficher l'état du microphone", "Affiche si votre microphone est ouvert ou muet.", ShowVoiceStatus);

            var proximity = new List<string>()
            {
                "5 m",
                "10 m",
                "15 m",
                "20 m",
                "100 m",
                "300 m",
                "1 km",
                "2 km",
                "Global",
            };
            var voiceChatProximity = new MenuItem("Chat vocal Proximité (" + ConvertToMetric(currentProximity) + ")", "Définit la proximité de réception du chat vocal en mètres. La valeur 0 est utilisée pour la réception globale.");
            var voiceChatChannel = new MenuListItem("Canal de discussion vocale", channels, channels.IndexOf(currentChannel), "Définir le canal du chat vocal.");

            if (IsAllowed(Permission.VCEnable))
            {
                menu.AddMenuItem(voiceChatEnabled);

                // Nested permissions because without voice chat enabled, you wouldn't be able to use these settings anyway.
                if (IsAllowed(Permission.VCShowSpeaker))
                {
                    menu.AddMenuItem(showCurrentSpeaker);
                }

                menu.AddMenuItem(voiceChatProximity);
                menu.AddMenuItem(voiceChatChannel);
                menu.AddMenuItem(showVoiceStatus);
            }

            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == voiceChatEnabled)
                {
                    EnableVoicechat = _checked;
                }
                else if (item == showCurrentSpeaker)
                {
                    ShowCurrentSpeaker = _checked;
                }
                else if (item == showVoiceStatus)
                {
                    ShowVoiceStatus = _checked;
                }
            };

            menu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
            {
                if (item == voiceChatChannel)
                {
                    currentChannel = channels[newIndex];
                    Subtitle.Custom($"Nouveau canal de chat vocal réglé sur : ~b~{channels[newIndex]}~s~.");
                }
            };
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == voiceChatProximity)
                {
                    var result = await GetUserInput(windowTitle: $"Saisir la proximité en mètres. Actuel : ({ConvertToMetric(currentProximity)})", maxInputLength: 6);

                    if (float.TryParse(result, out var resultfloat))
                    {
                        currentProximity = resultfloat;
                        Subtitle.Custom($"Nouvelle proximité du chat vocal réglée sur : ~b~{ConvertToMetric(currentProximity)}~s~.");
                        voiceChatProximity.Text = ("Chat vocal Proximité (" + ConvertToMetric(currentProximity) + ")");
                    }
                }
            };

        }
        static string ConvertToMetric(float input)
        {
            string val = "0m";
            if (input < 1.0)
            {
                val = (input * 100) + "cm";
            }
            else if (input >= 1.0)
            {
                if (input < 1000)
                {
                    val = input + "m";
                }
                else
                {
                    val = (input / 1000) + "km";
                }
            }
            if (input == 0)
            {
                val = "global";
            }
            return val;
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
