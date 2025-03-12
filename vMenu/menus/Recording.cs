using CitizenFX.Core;
using MenuAPI;
using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.ConfigManager;

namespace vMenuClient.menus
{
    public class Recording
    {
        // Variables
        private Menu menu;

        private void CreateMenu()
        {
            AddTextEntryByHash(0x86F10CE6, "Uploader sur le forum Cfx.re"); // Remplacer le bouton "Uploader sur Social Club" dans la galerie
            AddTextEntry("ERROR_UPLOAD", "Êtes-vous sûr de vouloir uploader cette photo sur le forum Cfx.re ?"); // Remplacer le texte d'avertissement pour l'upload

            // Créer le menu.
            menu = new Menu("Enregistrement", "Options d'enregistrement");

            var takePic = new MenuItem("Prendre une photo", "Prend une photo et l'enregistre dans la galerie du menu pause.");
            var openPmGallery = new MenuItem("Ouvrir la galerie", "Ouvre la galerie du menu pause.");
            var startRec = new MenuItem("Commencer l'enregistrement", "Commence un nouvel enregistrement de jeu en utilisant l'enregistrement intégré de GTA V.");
            var stopRec = new MenuItem("Arrêter l'enregistrement", "Arrête et sauvegarde votre enregistrement en cours.");
            var openEditor = new MenuItem("Éditeur Rockstar", "Ouvre l'éditeur Rockstar, notez qu'il est recommandé de quitter la session avant de le faire pour éviter certains problèmes.");

            menu.AddMenuItem(takePic);
            menu.AddMenuItem(openPmGallery);
            menu.AddMenuItem(startRec);
            menu.AddMenuItem(stopRec);
            menu.AddMenuItem(openEditor);

            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == startRec)
                {
                    if (IsRecording())
                    {
                        Notify.Alert("Vous êtes déjà en train d'enregistrer un clip, vous devez d'abord arrêter l'enregistrement avant de pouvoir en recommencer un nouveau !");
                    }
                    else
                    {
                        StartRecording(1);
                    }
                }
                else if (item == openPmGallery)
                {
                    ActivateFrontendMenu((uint)GetHashKey("FE_MENU_VERSION_MP_PAUSE"), true, 3);
                }
                else if (item == takePic)
                {
                    BeginTakeHighQualityPhoto();
                    SaveHighQualityPhoto(-1);
                    FreeMemoryForHighQualityPhoto();
                }
                else if (item == stopRec)
                {
                    if (!IsRecording())
                    {
                        Notify.Alert("Vous n'êtes actuellement PAS en train d'enregistrer un clip, vous devez d'abord commencer un enregistrement avant de pouvoir l'arrêter et le sauvegarder.");
                    }
                    else
                    {
                        StopRecordingAndSaveClip();
                    }
                }
                else if (item == openEditor)
                {
                    if (GetSettingsBool(Setting.vmenu_quit_session_in_rockstar_editor))
                    {
                        QuitSession();
                    }
                    ActivateRockstarEditor();
                    // Attendre que l'éditeur soit fermé.
                    while (IsPauseMenuActive())
                    {
                        await BaseScript.Delay(0);
                    }
                    // Puis effectuer un fondu entrant à l'écran.
                    DoScreenFadeIn(1);
                    Notify.Alert("Vous avez quitté votre session précédente avant d'entrer dans l'éditeur Rockstar. Redémarrez le jeu pour pouvoir rejoindre la session principale du serveur.", true, true);
                }
            };
        }

        /// <summary>
        /// Crée le menu s'il n'existe pas, puis le retourne.
        /// </summary>
        /// <returns>Le Menu</returns>
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