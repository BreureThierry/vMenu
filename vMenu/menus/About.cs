using MenuAPI;

namespace vMenuClient.menus
{
    public class About
    {
        // Variables
        private Menu menu;

        private void CreateMenu()
        {
            // Create the menu.
            menu = new Menu("vMenu", "A propos du vMenu");

            // Create menu items.
            var version = new MenuItem("vMenu Version", $"vMenu ~b~~h~{MainMenu.Version}~h~~s~ Traduis par ~p~TusBluX.~s~")
            {
                Label = $"~h~{MainMenu.Version}~h~"
            };
            var credits = new MenuItem("A propos du vMenu / Credits", "vMenu est conçu par ~b~Vespura~s~. Pour plus d'informations, consultez ~b~www.vespura.com/vmenu~s~. Merci à : Deltanic, Brigliar, IllusiveTea, Shayan Doust, zr0iq et Golden pour leurs contributions !");

            var serverInfoMessage = vMenuShared.ConfigManager.GetSettingsString(vMenuShared.ConfigManager.Setting.vmenu_server_info_message);
            if (!string.IsNullOrEmpty(serverInfoMessage))
            {
                var serverInfo = new MenuItem("Serveur Info", serverInfoMessage);
                var siteUrl = vMenuShared.ConfigManager.GetSettingsString(vMenuShared.ConfigManager.Setting.vmenu_server_info_website_url);
                if (!string.IsNullOrEmpty(siteUrl))
                {
                    serverInfo.Label = $"{siteUrl}";
                }
                menu.AddMenuItem(serverInfo);
            }
            menu.AddMenuItem(version);
            menu.AddMenuItem(credits);
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
