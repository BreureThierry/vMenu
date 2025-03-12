using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuClient.MpPedDataManager;
using static vMenuShared.ConfigManager;

namespace vMenuClient.menus
{
    public class MpPedCustomization
    {
        // Variables
        private Menu menu;
        public Menu createCharacterMenu = new("Créer un personnage", "Créer un nouveau personnage");
        public Menu savedCharactersMenu = new("vMenu", "Gérer les personnages sauvegardés");
        public Menu savedCharactersCategoryMenu = new("Catégorie", "Je suis mis à jour en temps réel !");
        public Menu inheritanceMenu = new("vMenu", "Options d'héritage du personnage");
        public Menu appearanceMenu = new("vMenu", "Options d'apparence du personnage");
        public Menu faceShapeMenu = new("vMenu", "Options de forme du visage du personnage");
        public Menu tattoosMenu = new("vMenu", "Options de tatouages du personnage");
        public Menu clothesMenu = new("vMenu", "Options de vêtements du personnage");
        public Menu propsMenu = new("vMenu", "Options d'accessoires du personnage");
        private readonly Menu manageSavedCharacterMenu = new("vMenu", "Gérer le personnage MP");

        // Nécessite de pouvoir désactiver/activer ces boutons depuis une autre classe.
        internal MenuItem createMaleBtn = new("Créer un personnage masculin", "Créer un nouveau personnage masculin.") { Label = "→→→" };
        internal MenuItem createFemaleBtn = new("Créer un personnage féminin", "Créer un nouveau personnage féminin.") { Label = "→→→" };
        internal MenuItem editPedBtn = new("Modifier un personnage sauvegardé", "Cela vous permet de modifier tout sur votre personnage sauvegardé. Les modifications seront enregistrées dans le fichier de sauvegarde de ce personnage une fois que vous appuyez sur le bouton de sauvegarde.");

        // Nécessite d'être modifiable depuis d'autres fonctions
        private readonly MenuListItem setCategoryBtn = new("Définir la catégorie du personnage", new List<string> { }, 0, "Définit la catégorie de ce personnage. Sélectionnez pour enregistrer.");
        private readonly MenuListItem categoryBtn = new("Catégorie du personnage", new List<string> { }, 0, "Définit la catégorie de ce personnage.");

        public static bool DontCloseMenus { get { return MenuController.PreventExitingMenu; } set { MenuController.PreventExitingMenu = value; } }
        public static bool DisableBackButton { get { return MenuController.DisableBackButton; } set { MenuController.DisableBackButton = value; } }
        string selectedSavedCharacterManageName = "";
        private bool isEdidtingPed = false;
        private readonly List<string> facial_expressions = new() { "mood_Normal_1", "mood_Happy_1", "mood_Angry_1", "mood_Aiming_1", "mood_Injured_1", "mood_stressed_1", "mood_smug_1", "mood_sulk_1", };

        private readonly List<string> parents = [];
        private readonly List<float> mixValues = [0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.0f];
        private readonly List<float> faceFeaturesValuesList =
        [
            -1.0f,    // 0
            -0.9f,    // 1
            -0.8f,    // 2
            -0.7f,    // 3
            -0.6f,    // 4
            -0.5f,    // 5
            -0.4f,    // 6
            -0.3f,    // 7
            -0.2f,    // 8
            -0.1f,    // 9
            0.0f,    // 10
            0.1f,    // 11
            0.2f,    // 12
            0.3f,    // 13
            0.4f,    // 14
            0.5f,    // 15
            0.6f,    // 16
            0.7f,    // 17
            0.8f,    // 18
            0.9f,    // 19
            1.0f     // 20
        ];
        private readonly Dictionary<int, KeyValuePair<string, string>> hairOverlays = new Dictionary<int, KeyValuePair<string, string>>()
        {
            { 0, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a") },
            { 1, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 2, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 3, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_003_a") },
            { 4, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 5, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 6, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 7, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 8, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_008_a") },
            { 9, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 10, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 11, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 12, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 13, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 14, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_long_a") },
            { 15, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_long_a") },
            { 16, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_z") },
            { 17, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a") },
            { 18, new KeyValuePair<string, string>("mpbusiness_overlays", "FM_Bus_M_Hair_000_a") },
            { 19, new KeyValuePair<string, string>("mpbusiness_overlays", "FM_Bus_M_Hair_001_a") },
            { 20, new KeyValuePair<string, string>("mphipster_overlays", "FM_Hip_M_Hair_000_a") },
            { 21, new KeyValuePair<string, string>("mphipster_overlays", "FM_Hip_M_Hair_001_a") },
            { 22, new KeyValuePair<string, string>("multiplayer_overlays", "FM_M_Hair_001_a") },
        };
        private readonly List<string> overlayColorsList = [];
        private readonly List<string> blemishesStyleList = [];
        private readonly List<string> beardStylesList = [];
        private readonly List<string> eyebrowsStyleList = [];
        private readonly List<string> ageingStyleList = [];
        private readonly List<string> makeupStyleList = [];
        private readonly List<string> blushStyleList = [];
        private readonly List<string> complexionStyleList = [];
        private readonly List<string> sunDamageStyleList = [];
        private readonly List<string> lipstickStyleList = [];
        private readonly List<string> molesFrecklesStyleList = [];
        private readonly List<string> chestHairStyleList = [];
        private readonly List<string> bodyBlemishesList = [];


        private readonly Random _random = new Random();
        private int _dadSelection;
        private int _mumSelection;
        private float _shapeMixValue;
        private float _skinMixValue;
        private readonly Dictionary<int, int> shapeFaceValues = [];
        private readonly Dictionary<int, Tuple<int, int, float>> apperanceValues = [];
        private int _hairSelection;
        private int _hairColorSelection;
        private int _hairHighlightColorSelection;
        private int _eyeColorSelection;
        private int _facialExpressionSelection;

        private MultiplayerPedData currentCharacter = new();
        private MpCharacterCategory currentCategory = new();

        private Ped _clone;

        /// <summary>
        /// Makes or updates the character creator menu. Also has an option to load data from the <see cref="currentCharacter"/> data, to allow for editing an existing ped.
        /// </summary>
        /// <param name="male"></param>
        /// <param name="editPed"></param>
        private void MakeCreateCharacterMenu(bool male, bool editPed = false)
        {
            isEdidtingPed = editPed;
            if (!editPed)
            {
                currentCharacter = new MultiplayerPedData();
                currentCharacter.DrawableVariations.clothes = new Dictionary<int, KeyValuePair<int, int>>();
                currentCharacter.PropVariations.props = new Dictionary<int, KeyValuePair<int, int>>();
                currentCharacter.PedHeadBlendData = Game.PlayerPed.GetHeadBlendData();
                currentCharacter.Version = 1;
                currentCharacter.ModelHash = male ? (uint)GetHashKey("mp_m_freemode_01") : (uint)GetHashKey("mp_f_freemode_01");
                currentCharacter.IsMale = male;

                SetPlayerClothing();
            }
            currentCharacter.DrawableVariations.clothes ??= new Dictionary<int, KeyValuePair<int, int>>();
            currentCharacter.PropVariations.props ??= new Dictionary<int, KeyValuePair<int, int>>();

            // Set the facial expression to default in case it doesn't exist yet, or keep the current one if it does.
            currentCharacter.FacialExpression ??= facial_expressions[0];

            // Set the facial expression on the ped itself.
            SetFacialIdleAnimOverride(Game.PlayerPed.Handle, currentCharacter.FacialExpression ?? facial_expressions[0], null);

            // Set the facial expression item list to the correct saved index.
            if (createCharacterMenu.GetMenuItems().ElementAt(6) is MenuListItem li)
            {
                var index = facial_expressions.IndexOf(currentCharacter.FacialExpression ?? facial_expressions[0]);
                if (index < 0)
                {
                    index = 0;
                }
                li.ListIndex = index;
            }

            appearanceMenu.ClearMenuItems();
            tattoosMenu.ClearMenuItems();
            clothesMenu.ClearMenuItems();
            propsMenu.ClearMenuItems();

            #region appearance menu.
            var opacity = new List<string>() { "0%", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "100%" };

            var maxHairStyles = GetNumberOfPedDrawableVariations(Game.PlayerPed.Handle, 2);
            //if (currentCharacter.ModelHash == (uint)PedHash.FreemodeFemale01)
            //{
            //    maxHairStyles /= 2;
            //}
            var hairStylesList = new List<string>();
            for (var i = 0; i < maxHairStyles; i++)
            {
                hairStylesList.Add($"Style #{i + 1}");
            }
            hairStylesList.Add($"Style #{maxHairStyles + 1}");

            var eyeColorList = new List<string>();
            for (var i = 0; i < 32; i++)
            {
                eyeColorList.Add($"Couleur des yeux #{i + 1}");
            }

            /*

            0               Blemishes             0 - 23,   255  
            1               Facial Hair           0 - 28,   255  
            2               Eyebrows              0 - 33,   255  
            3               Ageing                0 - 14,   255  
            4               Makeup                0 - 74,   255  
            5               Blush                 0 - 6,    255  
            6               Complexion            0 - 11,   255  
            7               Sun Damage            0 - 10,   255  
            8               Lipstick              0 - 9,    255  
            9               Moles/Freckles        0 - 17,   255  
            10              Chest Hair            0 - 16,   255  
            11              Body Blemishes        0 - 11,   255  
            12              Add Body Blemishes    0 - 1,    255  
            
            */


            // hair
            var currentHairStyle = editPed ? currentCharacter.PedAppearance.hairStyle : GetPedDrawableVariation(Game.PlayerPed.Handle, 2);
            var currentHairColor = editPed ? currentCharacter.PedAppearance.hairColor : 0;
            var currentHairHighlightColor = editPed ? currentCharacter.PedAppearance.hairHighlightColor : 0;

            // 0 blemishes
            var currentBlemishesStyle = editPed ? currentCharacter.PedAppearance.blemishesStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 0) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 0) : 0;
            var currentBlemishesOpacity = editPed ? currentCharacter.PedAppearance.blemishesOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 0, currentBlemishesStyle, currentBlemishesOpacity);

            // 1 beard
            var currentBeardStyle = editPed ? currentCharacter.PedAppearance.beardStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 1) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 1) : 0;
            var currentBeardOpacity = editPed ? currentCharacter.PedAppearance.beardOpacity : 0f;
            var currentBeardColor = editPed ? currentCharacter.PedAppearance.beardColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 1, currentBeardStyle, currentBeardOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 1, 1, currentBeardColor, currentBeardColor);

            // 2 eyebrows
            var currentEyebrowStyle = editPed ? currentCharacter.PedAppearance.eyebrowsStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 2) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 2) : 0;
            var currentEyebrowOpacity = editPed ? currentCharacter.PedAppearance.eyebrowsOpacity : 0f;
            var currentEyebrowColor = editPed ? currentCharacter.PedAppearance.eyebrowsColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 2, currentEyebrowStyle, currentEyebrowOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 2, 1, currentEyebrowColor, currentEyebrowColor);

            // 3 ageing
            var currentAgeingStyle = editPed ? currentCharacter.PedAppearance.ageingStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 3) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 3) : 0;
            var currentAgeingOpacity = editPed ? currentCharacter.PedAppearance.ageingOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 3, currentAgeingStyle, currentAgeingOpacity);

            // 4 makeup
            var currentMakeupStyle = editPed ? currentCharacter.PedAppearance.makeupStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 4) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 4) : 0;
            var currentMakeupOpacity = editPed ? currentCharacter.PedAppearance.makeupOpacity : 0f;
            var currentMakeupColor = editPed ? currentCharacter.PedAppearance.makeupColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 4, currentMakeupStyle, currentMakeupOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 4, 2, currentMakeupColor, currentMakeupColor);

            // 5 blush
            var currentBlushStyle = editPed ? currentCharacter.PedAppearance.blushStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 5) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 5) : 0;
            var currentBlushOpacity = editPed ? currentCharacter.PedAppearance.blushOpacity : 0f;
            var currentBlushColor = editPed ? currentCharacter.PedAppearance.blushColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 5, currentBlushStyle, currentBlushOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 5, 2, currentBlushColor, currentBlushColor);

            // 6 complexion
            var currentComplexionStyle = editPed ? currentCharacter.PedAppearance.complexionStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 6) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 6) : 0;
            var currentComplexionOpacity = editPed ? currentCharacter.PedAppearance.complexionOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 6, currentComplexionStyle, currentComplexionOpacity);

            // 7 sun damage
            var currentSunDamageStyle = editPed ? currentCharacter.PedAppearance.sunDamageStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 7) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 7) : 0;
            var currentSunDamageOpacity = editPed ? currentCharacter.PedAppearance.sunDamageOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 7, currentSunDamageStyle, currentSunDamageOpacity);

            // 8 lipstick
            var currentLipstickStyle = editPed ? currentCharacter.PedAppearance.lipstickStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 8) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 8) : 0;
            var currentLipstickOpacity = editPed ? currentCharacter.PedAppearance.lipstickOpacity : 0f;
            var currentLipstickColor = editPed ? currentCharacter.PedAppearance.lipstickColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 8, currentLipstickStyle, currentLipstickOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 8, 2, currentLipstickColor, currentLipstickColor);

            // 9 moles/freckles
            var currentMolesFrecklesStyle = editPed ? currentCharacter.PedAppearance.molesFrecklesStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 9) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 9) : 0;
            var currentMolesFrecklesOpacity = editPed ? currentCharacter.PedAppearance.molesFrecklesOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 9, currentMolesFrecklesStyle, currentMolesFrecklesOpacity);

            // 10 chest hair
            var currentChesthairStyle = editPed ? currentCharacter.PedAppearance.chestHairStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 10) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 10) : 0;
            var currentChesthairOpacity = editPed ? currentCharacter.PedAppearance.chestHairOpacity : 0f;
            var currentChesthairColor = editPed ? currentCharacter.PedAppearance.chestHairColor : 0;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 10, currentChesthairStyle, currentChesthairOpacity);
            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 10, 1, currentChesthairColor, currentChesthairColor);

            // 11 body blemishes
            var currentBodyBlemishesStyle = editPed ? currentCharacter.PedAppearance.bodyBlemishesStyle : GetPedHeadOverlayValue(Game.PlayerPed.Handle, 11) != 255 ? GetPedHeadOverlayValue(Game.PlayerPed.Handle, 11) : 0;
            var currentBodyBlemishesOpacity = editPed ? currentCharacter.PedAppearance.bodyBlemishesOpacity : 0f;
            SetPedHeadOverlay(Game.PlayerPed.Handle, 11, currentBodyBlemishesStyle, currentBodyBlemishesOpacity);

            var currentEyeColor = editPed ? currentCharacter.PedAppearance.eyeColor : 0;
            SetPedEyeColor(Game.PlayerPed.Handle, currentEyeColor);

            var hairStyles = new MenuListItem("Style de cheveux", hairStylesList, currentHairStyle, "Sélectionnez un style de cheveux.");
            var hairColors = new MenuListItem("Couleur des cheveux", overlayColorsList, currentHairColor, "Sélectionnez une couleur de cheveux.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };
            var hairHighlightColors = new MenuListItem("Couleur des reflets des cheveux", overlayColorsList, currentHairHighlightColor, "Sélectionnez une couleur de reflets pour les cheveux.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };

            var blemishesStyle = new MenuListItem("Style des imperfections", blemishesStyleList, currentBlemishesStyle, "Sélectionnez un style d'imperfections.");
            var blemishesOpacity = new MenuListItem("Opacité des imperfections", opacity, (int)(currentBlemishesOpacity * 10f), "Sélectionnez l'opacité des imperfections.") { ShowOpacityPanel = true };

            var beardStyles = new MenuListItem("Style de barbe", beardStylesList, currentBeardStyle, "Sélectionnez un style de barbe/pilosité faciale.");
            var beardOpacity = new MenuListItem("Opacité de la barbe", opacity, (int)(currentBeardOpacity * 10f), "Sélectionnez l'opacité de votre barbe/pilosité faciale.") { ShowOpacityPanel = true };
            var beardColor = new MenuListItem("Couleur de la barbe", overlayColorsList, currentBeardColor, "Sélectionnez une couleur de barbe.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };

            var eyebrowStyle = new MenuListItem("Style des sourcils", eyebrowsStyleList, currentEyebrowStyle, "Sélectionnez un style de sourcils.");
            var eyebrowOpacity = new MenuListItem("Opacité des sourcils", opacity, (int)(currentEyebrowOpacity * 10f), "Sélectionnez l'opacité de vos sourcils.") { ShowOpacityPanel = true };
            var eyebrowColor = new MenuListItem("Couleur des sourcils", overlayColorsList, currentEyebrowColor, "Sélectionnez une couleur de sourcils.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };

            var ageingStyle = new MenuListItem("Style de vieillissement", ageingStyleList, currentAgeingStyle, "Sélectionnez un style de vieillissement.");
            var ageingOpacity = new MenuListItem("Opacité du vieillissement", opacity, (int)(currentAgeingOpacity * 10f), "Sélectionnez l'opacité du vieillissement.") { ShowOpacityPanel = true };

            var makeupStyle = new MenuListItem("Style de maquillage", makeupStyleList, currentMakeupStyle, "Sélectionnez un style de maquillage.");
            var makeupOpacity = new MenuListItem("Opacité du maquillage", opacity, (int)(currentMakeupOpacity * 10f), "Sélectionnez l'opacité du maquillage.") { ShowOpacityPanel = true };
            var makeupColor = new MenuListItem("Couleur du maquillage", overlayColorsList, currentMakeupColor, "Sélectionnez une couleur de maquillage.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Makeup };

            var blushStyle = new MenuListItem("Style de fard à joues", blushStyleList, currentBlushStyle, "Sélectionnez un style de fard à joues.");
            var blushOpacity = new MenuListItem("Opacité du fard à joues", opacity, (int)(currentBlushOpacity * 10f), "Sélectionnez l'opacité du fard à joues.") { ShowOpacityPanel = true };
            var blushColor = new MenuListItem("Couleur du fard à joues", overlayColorsList, currentBlushColor, "Sélectionnez une couleur de fard à joues.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Makeup };

            var complexionStyle = new MenuListItem("Style de teint", complexionStyleList, currentComplexionStyle, "Sélectionnez un style de teint.");
            var complexionOpacity = new MenuListItem("Opacité du teint", opacity, (int)(currentComplexionOpacity * 10f), "Sélectionnez l'opacité du teint.") { ShowOpacityPanel = true };

            var sunDamageStyle = new MenuListItem("Style des dommages du soleil", sunDamageStyleList, currentSunDamageStyle, "Sélectionnez un style de dommages causés par le soleil.");
            var sunDamageOpacity = new MenuListItem("Opacité des dommages du soleil", opacity, (int)(currentSunDamageOpacity * 10f), "Sélectionnez l'opacité des dommages causés par le soleil.") { ShowOpacityPanel = true };

            var lipstickStyle = new MenuListItem("Style de rouge à lèvres", lipstickStyleList, currentLipstickStyle, "Sélectionnez un style de rouge à lèvres.");
            var lipstickOpacity = new MenuListItem("Opacité du rouge à lèvres", opacity, (int)(currentLipstickOpacity * 10f), "Sélectionnez l'opacité du rouge à lèvres.") { ShowOpacityPanel = true };
            var lipstickColor = new MenuListItem("Couleur du rouge à lèvres", overlayColorsList, currentLipstickColor, "Sélectionnez une couleur de rouge à lèvres.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Makeup };

            var molesFrecklesStyle = new MenuListItem("Style des grains de beauté et taches de rousseur", molesFrecklesStyleList, currentMolesFrecklesStyle, "Sélectionnez un style de grains de beauté et taches de rousseur.");
            var molesFrecklesOpacity = new MenuListItem("Opacité des grains de beauté et taches de rousseur", opacity, (int)(currentMolesFrecklesOpacity * 10f), "Sélectionnez l'opacité des grains de beauté et taches de rousseur.") { ShowOpacityPanel = true };

            var chestHairStyle = new MenuListItem("Style des poils de torse", chestHairStyleList, currentChesthairStyle, "Sélectionnez un style de poils de torse.");
            var chestHairOpacity = new MenuListItem("Opacité des poils de torse", opacity, (int)(currentChesthairOpacity * 10f), "Sélectionnez l'opacité des poils de torse.") { ShowOpacityPanel = true };
            var chestHairColor = new MenuListItem("Couleur des poils de torse", overlayColorsList, currentChesthairColor, "Sélectionnez une couleur de poils de torse.") { ShowColorPanel = true, ColorPanelColorType = MenuListItem.ColorPanelType.Hair };

            var bodyBlemishesStyle = new MenuListItem("Style des imperfections corporelles", bodyBlemishesList, currentBodyBlemishesStyle, "Sélectionnez un style d'imperfections corporelles.");
            var bodyBlemishesOpacity = new MenuListItem("Opacité des imperfections corporelles", opacity, (int)(currentBodyBlemishesOpacity * 10f), "Sélectionnez l'opacité des imperfections corporelles.") { ShowOpacityPanel = true };

            var eyeColor = new MenuListItem("Couleur des yeux", eyeColorList, currentEyeColor, "Sélectionnez une couleur d'yeux/verres de contact.");

            appearanceMenu.AddMenuItem(hairStyles);
            appearanceMenu.AddMenuItem(hairColors);
            appearanceMenu.AddMenuItem(hairHighlightColors);

            appearanceMenu.AddMenuItem(blemishesStyle);
            appearanceMenu.AddMenuItem(blemishesOpacity);

            appearanceMenu.AddMenuItem(beardStyles);
            appearanceMenu.AddMenuItem(beardOpacity);
            appearanceMenu.AddMenuItem(beardColor);

            appearanceMenu.AddMenuItem(eyebrowStyle);
            appearanceMenu.AddMenuItem(eyebrowOpacity);
            appearanceMenu.AddMenuItem(eyebrowColor);

            appearanceMenu.AddMenuItem(ageingStyle);
            appearanceMenu.AddMenuItem(ageingOpacity);

            appearanceMenu.AddMenuItem(makeupStyle);
            appearanceMenu.AddMenuItem(makeupOpacity);
            appearanceMenu.AddMenuItem(makeupColor);

            appearanceMenu.AddMenuItem(blushStyle);
            appearanceMenu.AddMenuItem(blushOpacity);
            appearanceMenu.AddMenuItem(blushColor);

            appearanceMenu.AddMenuItem(complexionStyle);
            appearanceMenu.AddMenuItem(complexionOpacity);

            appearanceMenu.AddMenuItem(sunDamageStyle);
            appearanceMenu.AddMenuItem(sunDamageOpacity);

            appearanceMenu.AddMenuItem(lipstickStyle);
            appearanceMenu.AddMenuItem(lipstickOpacity);
            appearanceMenu.AddMenuItem(lipstickColor);

            appearanceMenu.AddMenuItem(molesFrecklesStyle);
            appearanceMenu.AddMenuItem(molesFrecklesOpacity);

            appearanceMenu.AddMenuItem(chestHairStyle);
            appearanceMenu.AddMenuItem(chestHairOpacity);
            appearanceMenu.AddMenuItem(chestHairColor);

            appearanceMenu.AddMenuItem(bodyBlemishesStyle);
            appearanceMenu.AddMenuItem(bodyBlemishesOpacity);

            appearanceMenu.AddMenuItem(eyeColor);

            if (male)
            {
                // There are weird people out there that wanted makeup for male characters
                // so yeah.... here you go I suppose... strange...

                /*
                makeupStyle.Enabled = false;
                makeupStyle.LeftIcon = MenuItem.Icon.LOCK;
                makeupStyle.Description = "This is not available for male characters.";

                makeupOpacity.Enabled = false;
                makeupOpacity.LeftIcon = MenuItem.Icon.LOCK;
                makeupOpacity.Description = "This is not available for male characters.";

                makeupColor.Enabled = false;
                makeupColor.LeftIcon = MenuItem.Icon.LOCK;
                makeupColor.Description = "This is not available for male characters.";


                blushStyle.Enabled = false;
                blushStyle.LeftIcon = MenuItem.Icon.LOCK;
                blushStyle.Description = "This is not available for male characters.";

                blushOpacity.Enabled = false;
                blushOpacity.LeftIcon = MenuItem.Icon.LOCK;
                blushOpacity.Description = "This is not available for male characters.";

                blushColor.Enabled = false;
                blushColor.LeftIcon = MenuItem.Icon.LOCK;
                blushColor.Description = "This is not available for male characters.";


                lipstickStyle.Enabled = false;
                lipstickStyle.LeftIcon = MenuItem.Icon.LOCK;
                lipstickStyle.Description = "This is not available for male characters.";

                lipstickOpacity.Enabled = false;
                lipstickOpacity.LeftIcon = MenuItem.Icon.LOCK;
                lipstickOpacity.Description = "This is not available for male characters.";

                lipstickColor.Enabled = false;
                lipstickColor.LeftIcon = MenuItem.Icon.LOCK;
                lipstickColor.Description = "This is not available for male characters.";
                */
            }
            else
            {
                beardStyles.Enabled = false;
                beardStyles.LeftIcon = MenuItem.Icon.LOCK;
                beardStyles.Description = "Cette option n'est pas disponible pour les personnages féminins.";

                beardOpacity.Enabled = false;
                beardOpacity.LeftIcon = MenuItem.Icon.LOCK;
                beardOpacity.Description = "Cette option n'est pas disponible pour les personnages féminins.";

                beardColor.Enabled = false;
                beardColor.LeftIcon = MenuItem.Icon.LOCK;
                beardColor.Description = "Cette option n'est pas disponible pour les personnages féminins.";


                chestHairStyle.Enabled = false;
                chestHairStyle.LeftIcon = MenuItem.Icon.LOCK;
                chestHairStyle.Description = "Cette option n'est pas disponible pour les personnages féminins.";

                chestHairOpacity.Enabled = false;
                chestHairOpacity.LeftIcon = MenuItem.Icon.LOCK;
                chestHairOpacity.Description = "Cette option n'est pas disponible pour les personnages féminins.";

                chestHairColor.Enabled = false;
                chestHairColor.LeftIcon = MenuItem.Icon.LOCK;
                chestHairColor.Description = "Cette option n'est pas disponible pour les personnages féminins.";
            }

            #endregion

            #region clothing options menu
            var clothingCategoryNames = new string[12] { "Inutilisé (tête)", "Masques", "Inutilisé (cheveux)", "Haut du corps", "Bas du corps", "Sacs et parachutes", "Chaussures", "Écharpes et chaînes", "Chemises et accessoires", "Armures corporelles et accessoires 2", "Badges et logos", "Chemises superposées et vestes" };
            for (var i = 0; i < 12; i++)
            {
                if (i is not 0 and not 2)
                {
                    var currentVariationIndex = editPed && currentCharacter.DrawableVariations.clothes.ContainsKey(i) ? currentCharacter.DrawableVariations.clothes[i].Key : GetPedDrawableVariation(Game.PlayerPed.Handle, i);
                    var currentVariationTextureIndex = editPed && currentCharacter.DrawableVariations.clothes.ContainsKey(i) ? currentCharacter.DrawableVariations.clothes[i].Value : GetPedTextureVariation(Game.PlayerPed.Handle, i);

                    var maxDrawables = GetNumberOfPedDrawableVariations(Game.PlayerPed.Handle, i);

                    var items = new List<string>();
                    for (var x = 0; x < maxDrawables; x++)
                    {
                        items.Add($"Texture #{x} (sur {maxDrawables})");
                    }

                    var maxTextures = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, i, currentVariationIndex);

                    var listItem = new MenuListItem(clothingCategoryNames[i], items, currentVariationIndex, $"Select a drawable using the arrow keys and press ~o~enter~s~ to cycle through all available textures. Texture actuellement sélectionnée : #{currentVariationTextureIndex + 1} (of {maxTextures}).");
                    clothesMenu.AddMenuItem(listItem);
                }
            }
            #endregion

            #region props options menu
            var propNames = new string[5] { "Chapeaux & Casques", "Lunettes", "Accessoires divers", "Montres", "Bracelets" };
            for (var x = 0; x < 5; x++)
            {
                var propId = x;
                if (x > 2)
                {
                    propId += 3;
                }

                var currentProp = editPed && currentCharacter.PropVariations.props.ContainsKey(propId) ? currentCharacter.PropVariations.props[propId].Key : GetPedPropIndex(Game.PlayerPed.Handle, propId);
                var currentPropTexture = editPed && currentCharacter.PropVariations.props.ContainsKey(propId) ? currentCharacter.PropVariations.props[propId].Value : GetPedPropTextureIndex(Game.PlayerPed.Handle, propId);

                var propsList = new List<string>();
                for (var i = 0; i < GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, propId); i++)
                {
                    propsList.Add($"Accessoire #{i} (sur {GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, propId)})");
                }
                propsList.Add("Pas d'accessoire");


                if (GetPedPropIndex(Game.PlayerPed.Handle, propId) != -1)
                {
                    var maxPropTextures = GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, propId, currentProp);
                    var propListItem = new MenuListItem($"{propNames[x]}", propsList, currentProp, $"Sélectionnez un accessoire à l'aide des touches fléchées et appuyez sur ~o~Entrée~s~ pour parcourir toutes les textures disponibles. Texture actuellement sélectionnée : #{currentPropTexture + 1} (sur {maxPropTextures}).");
                    propsMenu.AddMenuItem(propListItem);
                }
                else
                {
                    var propListItem = new MenuListItem($"{propNames[x]}", propsList, currentProp, "Sélectionnez un accessoire à l'aide des touches fléchées et appuyez sur ~o~Entrée~s~ pour parcourir toutes les textures disponibles.");
                    propsMenu.AddMenuItem(propListItem);
                }


            }
            #endregion

            #region face features menu
            foreach (MenuSliderItem item in faceShapeMenu.GetMenuItems())
            {
                if (editPed)
                {
                    if (currentCharacter.FaceShapeFeatures.features == null)
                    {
                        currentCharacter.FaceShapeFeatures.features = new Dictionary<int, float>();
                    }
                    else
                    {
                        if (currentCharacter.FaceShapeFeatures.features.ContainsKey(item.Index))
                        {
                            item.Position = (int)(currentCharacter.FaceShapeFeatures.features[item.Index] * 10f) + 10;
                            SetPedFaceFeature(Game.PlayerPed.Handle, item.Index, currentCharacter.FaceShapeFeatures.features[item.Index]);
                        }
                        else
                        {
                            item.Position = 10;
                            SetPedFaceFeature(Game.PlayerPed.Handle, item.Index, 0f);
                        }
                    }
                }
                else
                {
                    item.Position = 10;
                    SetPedFaceFeature(Game.PlayerPed.Handle, item.Index, 0f);
                }
            }
            #endregion

            #region Tattoos menu
            var headTattoosList = new List<string>();
            var torsoTattoosList = new List<string>();
            var leftArmTattoosList = new List<string>();
            var rightArmTattoosList = new List<string>();
            var leftLegTattoosList = new List<string>();
            var rightLegTattoosList = new List<string>();
            var badgeTattoosList = new List<string>();

            TattoosData.GenerateTattoosData();
            if (male)
            {
                var counter = 1;
                foreach (var tattoo in MaleTattoosCollection.HEAD)
                {
                    headTattoosList.Add($"Tatouage #{counter} (sur {MaleTattoosCollection.HEAD.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.TORSO)
                {
                    torsoTattoosList.Add($"Tatouage #{counter} (sur {MaleTattoosCollection.TORSO.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.LEFT_ARM)
                {
                    leftArmTattoosList.Add($"Tatouage #{counter} (sur {MaleTattoosCollection.LEFT_ARM.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.RIGHT_ARM)
                {
                    rightArmTattoosList.Add($"Tatouage #{counter} (sur {MaleTattoosCollection.RIGHT_ARM.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.LEFT_LEG)
                {
                    leftLegTattoosList.Add($"Tatouage #{counter} (sur {MaleTattoosCollection.LEFT_LEG.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.RIGHT_LEG)
                {
                    rightLegTattoosList.Add($"Tatouage #{counter} (sur {MaleTattoosCollection.RIGHT_LEG.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in MaleTattoosCollection.BADGES)
                {
                    badgeTattoosList.Add($"Badge #{counter} (sur {MaleTattoosCollection.BADGES.Count})");
                    counter++;
                }
            }
            else
            {
                var counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.HEAD)
                {
                    headTattoosList.Add($"Tatouage #{counter} (sur {FemaleTattoosCollection.HEAD.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.TORSO)
                {
                    torsoTattoosList.Add($"Tatouage #{counter} (sur {FemaleTattoosCollection.TORSO.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.LEFT_ARM)
                {
                    leftArmTattoosList.Add($"Tatouage #{counter} (sur {FemaleTattoosCollection.LEFT_ARM.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.RIGHT_ARM)
                {
                    rightArmTattoosList.Add($"Tatouage #{counter} (sur {FemaleTattoosCollection.RIGHT_ARM.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.LEFT_LEG)
                {
                    leftLegTattoosList.Add($"Tatouage #{counter} (sur {FemaleTattoosCollection.LEFT_LEG.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.RIGHT_LEG)
                {
                    rightLegTattoosList.Add($"Tatouage #{counter} (sur {FemaleTattoosCollection.RIGHT_LEG.Count})");
                    counter++;
                }
                counter = 1;
                foreach (var tattoo in FemaleTattoosCollection.BADGES)
                {
                    badgeTattoosList.Add($"Badge #{counter} (sur {FemaleTattoosCollection.BADGES.Count})");
                    counter++;
                }
            }

            const string tatDesc = "Parcourez la liste pour prévisualiser les tatouages. Si vous en aimez un, appuyez sur Entrée pour le sélectionner. La sélection ajoutera le tatouage si vous ne l'avez pas déjà. Si vous avez déjà ce tatouage, il sera supprimé.";
            var headTatts = new MenuListItem("Tatouages de la tête", headTattoosList, 0, tatDesc);
            var torsoTatts = new MenuListItem("Tatouages du torse", torsoTattoosList, 0, tatDesc);
            var leftArmTatts = new MenuListItem("Tatouages du bras gauche", leftArmTattoosList, 0, tatDesc);
            var rightArmTatts = new MenuListItem("Tatouages du bras droit", rightArmTattoosList, 0, tatDesc);
            var leftLegTatts = new MenuListItem("Tatouages de la jambe gauche", leftLegTattoosList, 0, tatDesc);
            var rightLegTatts = new MenuListItem("Tatouages de la jambe droite", rightLegTattoosList, 0, tatDesc);
            var badgeTatts = new MenuListItem("Superpositions de badges", badgeTattoosList, 0, tatDesc);

            tattoosMenu.AddMenuItem(headTatts);
            tattoosMenu.AddMenuItem(torsoTatts);
            tattoosMenu.AddMenuItem(leftArmTatts);
            tattoosMenu.AddMenuItem(rightArmTatts);
            tattoosMenu.AddMenuItem(leftLegTatts);
            tattoosMenu.AddMenuItem(rightLegTatts);
            tattoosMenu.AddMenuItem(badgeTatts);
            tattoosMenu.AddMenuItem(new MenuItem("Enlever tous les tatouages", "Cliquez sur ce bouton si vous souhaitez supprimer tous les tatouages et recommencer à zéro."));
            #endregion

            List<string> categoryNames = GetAllCategoryNames();

            categoryNames.RemoveAt(0);

            List<MenuItem.Icon> categoryIcons = GetCategoryIcons(categoryNames);

            categoryBtn.ItemData = new Tuple<List<string>, List<MenuItem.Icon>>(categoryNames, categoryIcons);
            categoryBtn.ListItems = categoryNames;
            categoryBtn.ListIndex = 0;
            categoryBtn.RightIcon = categoryIcons[categoryBtn.ListIndex];

            createCharacterMenu.RefreshIndex();
            appearanceMenu.RefreshIndex();
            inheritanceMenu.RefreshIndex();
            tattoosMenu.RefreshIndex();
        }

        /// <summary>
        /// Saves the mp character and quits the editor if successful.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> SavePed()
        {
            currentCharacter.PedHeadBlendData = Game.PlayerPed.GetHeadBlendData();
            if (isEdidtingPed)
            {
                var json = JsonConvert.SerializeObject(currentCharacter);
                if (StorageManager.SaveJsonData(currentCharacter.SaveName, json, true))
                {
                    Notify.Success("Votre personnage a été sauvegardé avec succès.");
                    return true;
                }
                else
                {
                    Notify.Error("Votre personnage n'a pas pu être sauvegardé. Raison inconnue. :(");
                    return false;
                }
            }
            else
            {
                var name = await GetUserInput(windowTitle: "Saisissez un nom de sauvegarde.", maxInputLength: 30);
                if (string.IsNullOrEmpty(name))
                {
                    Notify.Error(CommonErrors.InvalidInput);
                    return false;
                }
                else
                {
                    currentCharacter.SaveName = "mp_ped_" + name;
                    var json = JsonConvert.SerializeObject(currentCharacter);

                    if (StorageManager.SaveJsonData("mp_ped_" + name, json, false))
                    {
                        Notify.Success($"Votre personnage (~g~<C>{name}</C>~s~) a été sauvegardé.");
                        Log($"Personnage sauvegardé {name}. Data: {json}");
                        return true;
                    }
                    else
                    {
                        Notify.Error($"La sauvegarde a échoué, très probablement parce que ce nom (~y~<C>{name}</C>~s~) est déjà utilisé.");
                        return false;
                    }
                }
            }

        }

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            for (int i = 0; i < 46; i++)
            {
                parents.Add($"#{i}");
            }

            for (int i = 0; i < GetNumHairColors(); i++)
            {
                overlayColorsList.Add($"Couleur #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(0); i++)
            {
                blemishesStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(1); i++)
            {
                beardStylesList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(2); i++)
            {
                eyebrowsStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(3); i++)
            {
                ageingStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(4); i++)
            {
                makeupStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(5); i++)
            {
                blushStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(6); i++)
            {
                complexionStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(7); i++)
            {
                sunDamageStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(8); i++)
            {
                lipstickStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(9); i++)
            {
                molesFrecklesStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(10); i++)
            {
                chestHairStyleList.Add($"Style #{i + 1}");
            }

            for (int i = 0; i < GetNumHeadOverlayValues(11); i++)
            {
                bodyBlemishesList.Add($"Style #{i + 1}");
            }

            // Create the menu.
            menu = new Menu(Game.Player.Name, "Personnalisation Ped MP");

            var savedCharacters = new MenuItem("Personnages sauvegardés", "Créez, modifiez ou supprimez vos personnages multijoueurs sauvegardés.")
            {
                Label = "→→→"
            };

            MenuController.AddMenu(createCharacterMenu);
            MenuController.AddMenu(savedCharactersMenu);
            MenuController.AddMenu(savedCharactersCategoryMenu);
            MenuController.AddMenu(inheritanceMenu);
            MenuController.AddMenu(appearanceMenu);
            MenuController.AddMenu(faceShapeMenu);
            MenuController.AddMenu(tattoosMenu);
            MenuController.AddMenu(clothesMenu);
            MenuController.AddMenu(propsMenu);

            CreateSavedPedsMenu();

            menu.AddMenuItem(createMaleBtn);
            MenuController.BindMenuItem(menu, createCharacterMenu, createMaleBtn);
            menu.AddMenuItem(createFemaleBtn);
            MenuController.BindMenuItem(menu, createCharacterMenu, createFemaleBtn);
            menu.AddMenuItem(savedCharacters);
            MenuController.BindMenuItem(menu, savedCharactersMenu, savedCharacters);

            menu.RefreshIndex();

            createCharacterMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            inheritanceMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            appearanceMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            faceShapeMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            tattoosMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            clothesMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");
            propsMenu.InstructionalButtons.Add(Control.MoveLeftRight, "Tourner la tête");

            createCharacterMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            inheritanceMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            appearanceMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            faceShapeMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            tattoosMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            clothesMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");
            propsMenu.InstructionalButtons.Add(Control.PhoneExtraOption, "Tourner le personnage");

            createCharacterMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            inheritanceMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            appearanceMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            faceShapeMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            tattoosMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            clothesMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");
            propsMenu.InstructionalButtons.Add(Control.ParachuteBrakeRight, "Tourner la caméra à droite");

            createCharacterMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            inheritanceMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            appearanceMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            faceShapeMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            tattoosMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            clothesMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");
            propsMenu.InstructionalButtons.Add(Control.ParachuteBrakeLeft, "Tourner la caméra à gauche");

            var randomizeButton = new MenuItem("Personnalisation aléatoire", "Personnalise aléatoirement l'apparence du personnage.");
            var inheritanceButton = new MenuItem("Héritage du personnage", "Options d'héritage du personnage.");
            var appearanceButton = new MenuItem("Apparence du personnage", "Options d'apparence du personnage.");
            var faceButton = new MenuItem("Options de la forme du visage", "Options de la forme du visage du personnage.");
            var tattoosButton = new MenuItem("Options des tatouages", "Options des tatouages du personnage.");
            var clothesButton = new MenuItem("Vêtements du personnage", "Vêtements du personnage.");
            var propsButton = new MenuItem("Accessoires du personnage", "Accessoires du personnage.");
            var saveButton = new MenuItem("Sauvegarder le personnage", "Sauvegarde votre personnage.");
            var exitNoSave = new MenuItem("Quitter sans sauvegarder", "Êtes-vous sûr ? Tout travail non sauvegardé sera perdu.");
            var faceExpressionList = new MenuListItem("Expression faciale", new List<string> { "Normal", "Heureux", "En colère", "En visée", "Blessé", "Stressé", "Suffisant", "Boudeur" }, 0, "Définissez une expression faciale qui sera utilisée lorsque votre personnage est inactif.");

            inheritanceButton.Label = "→→→";
            appearanceButton.Label = "→→→";
            faceButton.Label = "→→→";
            tattoosButton.Label = "→→→";
            clothesButton.Label = "→→→";
            propsButton.Label = "→→→";

            createCharacterMenu.AddMenuItem(randomizeButton);
            createCharacterMenu.AddMenuItem(inheritanceButton);
            createCharacterMenu.AddMenuItem(appearanceButton);
            createCharacterMenu.AddMenuItem(faceButton);
            createCharacterMenu.AddMenuItem(tattoosButton);
            createCharacterMenu.AddMenuItem(clothesButton);
            createCharacterMenu.AddMenuItem(propsButton);
            createCharacterMenu.AddMenuItem(faceExpressionList);
            createCharacterMenu.AddMenuItem(categoryBtn);
            createCharacterMenu.AddMenuItem(saveButton);
            createCharacterMenu.AddMenuItem(exitNoSave);

            MenuController.BindMenuItem(createCharacterMenu, inheritanceMenu, inheritanceButton);
            MenuController.BindMenuItem(createCharacterMenu, appearanceMenu, appearanceButton);
            MenuController.BindMenuItem(createCharacterMenu, faceShapeMenu, faceButton);
            MenuController.BindMenuItem(createCharacterMenu, tattoosMenu, tattoosButton);
            MenuController.BindMenuItem(createCharacterMenu, clothesMenu, clothesButton);
            MenuController.BindMenuItem(createCharacterMenu, propsMenu, propsButton);

            #region inheritance
            var dads = new Dictionary<string, int>();
            var moms = new Dictionary<string, int>();

            void AddInheritance(Dictionary<string, int> dict, int listId, string textPrefix)
            {
                var baseIdx = dict.Count;
                var basePed = GetPedHeadBlendFirstIndex(listId);

                // list 0/2 are male, list 1/3 are female
                var suffix = $" ({(listId % 2 == 0 ? "Male" : "Female")})";

                for (var i = 0; i < GetNumParentPedsOfType(listId); i++)
                {
                    // get the actual parent name, or the index if none
                    var label = GetLabelText($"{textPrefix}{i}");
                    if (string.IsNullOrWhiteSpace(label) || label == "NULL")
                    {
                        label = $"{baseIdx + i}";
                    }

                    // append the gender of the list
                    label += suffix;
                    dict[label] = basePed + i;
                }
            }

            int GetInheritance(Dictionary<string, int> list, MenuListItem listItem)
            {
                if (listItem.ListIndex < listItem.ListItems.Count)
                {
                    if (list.TryGetValue(listItem.ListItems[listItem.ListIndex], out var idx))
                    {
                        return idx;
                    }
                }

                return 0;
            }

            var listIdx = 0;
            foreach (var list in new[] { dads, moms })
            {
                void AddDads()
                {
                    AddInheritance(list, 0, "Male_");
                    AddInheritance(list, 2, "Special_Male_");
                }

                void AddMoms()
                {
                    AddInheritance(list, 1, "Female_");
                    AddInheritance(list, 3, "Special_Female_");
                }

                if (listIdx == 0)
                {
                    AddDads();
                    AddMoms();
                }
                else
                {
                    AddMoms();
                    AddDads();
                }

                listIdx++;
            }

            var inheritanceDads = new MenuListItem("Père", dads.Keys.ToList(), 0, "Sélectionnez un père.");
            var inheritanceMoms = new MenuListItem("Mère", moms.Keys.ToList(), 0, "Sélectionnez une mère.");
            var inheritanceShapeMix = new MenuSliderItem("Mélange de tête", "Choisissez la part de la forme de votre tête qui doit être héritée de votre père ou de votre mère. Tout à gauche, c'est votre père, tout à droite, c'est votre mère.", 0, 10, 5, true) { SliderLeftIcon = MenuItem.Icon.MALE, SliderRightIcon = MenuItem.Icon.FEMALE };
            var inheritanceSkinMix = new MenuSliderItem("Mélange de peaux", "Choisissez la proportion de votre teint qui doit être héritée de votre père ou de votre mère. Tout à gauche, c'est votre père, tout à droite, c'est votre mère.", 0, 10, 5, true) { SliderLeftIcon = MenuItem.Icon.MALE, SliderRightIcon = MenuItem.Icon.FEMALE };

            inheritanceMenu.AddMenuItem(inheritanceDads);
            inheritanceMenu.AddMenuItem(inheritanceMoms);
            inheritanceMenu.AddMenuItem(inheritanceShapeMix);
            inheritanceMenu.AddMenuItem(inheritanceSkinMix);

            // formula from maintransition.#sc
            float GetMinimum()
            {
                return currentCharacter.IsMale ? 0.05f : 0.3f;
            }

            float GetMaximum()
            {
                return currentCharacter.IsMale ? 0.7f : 0.95f;
            }

            float ClampMix(int value)
            {
                var sliderFraction = mixValues[value];
                var min = GetMinimum();
                var max = GetMaximum();

                return min + (sliderFraction * (max - min));
            }

            int UnclampMix(float value)
            {
                var min = GetMinimum();
                var max = GetMaximum();

                var origFraction = (value - min) / (max - min);
                return Math.Max(Math.Min((int)(origFraction * 10), 10), 0);
            }

            inheritanceMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, itemIndex) =>
            {
                _dadSelection = inheritanceDads.ListIndex;
                _mumSelection = inheritanceMoms.ListIndex;

                SetHeadBlend();
            };

            inheritanceMenu.OnSliderPositionChange += (sender, item, oldPosition, newPosition, itemIndex) =>
            {
                _shapeMixValue = inheritanceShapeMix.Position;
                _skinMixValue = inheritanceSkinMix.Position;

                SetHeadBlend();
            };
            #endregion

            #region appearance
            // manage the list changes for appearance items.
            appearanceMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, itemIndex) =>
            {
                if (itemIndex == 0) // hair style
                {
                    ChangePlayerHair(newSelectionIndex);
                }
                else if (itemIndex is 1 or 2) // hair colors
                {
                    var tmp = (MenuListItem)_menu.GetMenuItems()[1];
                    var hairColor = tmp.ListIndex;
                    tmp = (MenuListItem)_menu.GetMenuItems()[2];
                    var hairHighlightColor = tmp.ListIndex;

                    ChangePlayerHairColor(hairColor, hairHighlightColor);

                    currentCharacter.PedAppearance.hairColor = hairColor;
                    currentCharacter.PedAppearance.hairHighlightColor = hairHighlightColor;
                }
                else if (itemIndex == 33) // eye color
                {
                    var selection = ((MenuListItem)_menu.GetMenuItems()[itemIndex]).ListIndex;
                    ChangePlayerEyeColor(selection);
                    currentCharacter.PedAppearance.eyeColor = selection;
                }
                else
                {
                    var selection = ((MenuListItem)_menu.GetMenuItems()[itemIndex]).ListIndex;
                    var opacity = 0f;
                    if (_menu.GetMenuItems()[itemIndex + 1] is MenuListItem item2)
                    {
                        opacity = (((float)item2.ListIndex + 1) / 10f) - 0.1f;
                    }
                    else if (_menu.GetMenuItems()[itemIndex - 1] is MenuListItem item1)
                    {
                        opacity = (((float)item1.ListIndex + 1) / 10f) - 0.1f;
                    }
                    else if (_menu.GetMenuItems()[itemIndex] is MenuListItem item)
                    {
                        opacity = (((float)item.ListIndex + 1) / 10f) - 0.1f;
                    }
                    else
                    {
                        opacity = 1f;
                    }

                    switch (itemIndex)
                    {
                        case 3: // blemishes
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 0, selection, opacity);
                            currentCharacter.PedAppearance.blemishesStyle = selection;
                            currentCharacter.PedAppearance.blemishesOpacity = opacity;
                            break;
                        case 5: // beards
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 1, selection, opacity);
                            currentCharacter.PedAppearance.beardStyle = selection;
                            currentCharacter.PedAppearance.beardOpacity = opacity;
                            break;
                        case 7: // beards color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 1, 1, selection, selection);
                            currentCharacter.PedAppearance.beardColor = selection;
                            break;
                        case 8: // eyebrows
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 2, selection, opacity);
                            currentCharacter.PedAppearance.eyebrowsStyle = selection;
                            currentCharacter.PedAppearance.eyebrowsOpacity = opacity;
                            break;
                        case 10: // eyebrows color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 2, 1, selection, selection);
                            currentCharacter.PedAppearance.eyebrowsColor = selection;
                            break;
                        case 11: // ageing
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 3, selection, opacity);
                            currentCharacter.PedAppearance.ageingStyle = selection;
                            currentCharacter.PedAppearance.ageingOpacity = opacity;
                            break;
                        case 13: // makeup
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 4, selection, opacity);
                            currentCharacter.PedAppearance.makeupStyle = selection;
                            currentCharacter.PedAppearance.makeupOpacity = opacity;
                            break;
                        case 15: // makeup color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 4, 2, selection, selection);
                            currentCharacter.PedAppearance.makeupColor = selection;
                            break;
                        case 16: // blush style
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 5, selection, opacity);
                            currentCharacter.PedAppearance.blushStyle = selection;
                            currentCharacter.PedAppearance.blushOpacity = opacity;
                            break;
                        case 18: // blush color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 5, 2, selection, selection);
                            currentCharacter.PedAppearance.blushColor = selection;
                            break;
                        case 19: // complexion
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 6, selection, opacity);
                            currentCharacter.PedAppearance.complexionStyle = selection;
                            currentCharacter.PedAppearance.complexionOpacity = opacity;
                            break;
                        case 21: // sun damage
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 7, selection, opacity);
                            currentCharacter.PedAppearance.sunDamageStyle = selection;
                            currentCharacter.PedAppearance.sunDamageOpacity = opacity;
                            break;
                        case 23: // lipstick
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 8, selection, opacity);
                            currentCharacter.PedAppearance.lipstickStyle = selection;
                            currentCharacter.PedAppearance.lipstickOpacity = opacity;
                            break;
                        case 25: // lipstick color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 8, 2, selection, selection);
                            currentCharacter.PedAppearance.lipstickColor = selection;
                            break;
                        case 26: // moles and freckles
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 9, selection, opacity);
                            currentCharacter.PedAppearance.molesFrecklesStyle = selection;
                            currentCharacter.PedAppearance.molesFrecklesOpacity = opacity;
                            break;
                        case 28: // chest hair
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 10, selection, opacity);
                            currentCharacter.PedAppearance.chestHairStyle = selection;
                            currentCharacter.PedAppearance.chestHairOpacity = opacity;
                            break;
                        case 30: // chest hair color
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, 10, 1, selection, selection);
                            currentCharacter.PedAppearance.chestHairColor = selection;
                            break;
                        case 31: // body blemishes
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 11, selection, opacity);
                            currentCharacter.PedAppearance.bodyBlemishesStyle = selection;
                            currentCharacter.PedAppearance.bodyBlemishesOpacity = opacity;
                            break;
                    }
                }
            };

            // manage the slider changes for opacity on the appearance items.
            appearanceMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, itemIndex) =>
            {
                if (itemIndex is > 2 and < 33)
                {

                    var selection = ((MenuListItem)_menu.GetMenuItems()[itemIndex - 1]).ListIndex;
                    var opacity = 0f;
                    if (_menu.GetMenuItems()[itemIndex] is MenuListItem item2)
                    {
                        opacity = (((float)item2.ListIndex + 1) / 10f) - 0.1f;
                    }
                    else if (_menu.GetMenuItems()[itemIndex + 1] is MenuListItem item1)
                    {
                        opacity = (((float)item1.ListIndex + 1) / 10f) - 0.1f;
                    }
                    else if (_menu.GetMenuItems()[itemIndex - 1] is MenuListItem item)
                    {
                        opacity = (((float)item.ListIndex + 1) / 10f) - 0.1f;
                    }
                    else
                    {
                        opacity = 1f;
                    }

                    switch (itemIndex)
                    {
                        case 4: // blemishes
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 0, selection, opacity);
                            currentCharacter.PedAppearance.blemishesStyle = selection;
                            currentCharacter.PedAppearance.blemishesOpacity = opacity;
                            break;
                        case 6: // beards
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 1, selection, opacity);
                            currentCharacter.PedAppearance.beardStyle = selection;
                            currentCharacter.PedAppearance.beardOpacity = opacity;
                            break;
                        case 9: // eyebrows
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 2, selection, opacity);
                            currentCharacter.PedAppearance.eyebrowsStyle = selection;
                            currentCharacter.PedAppearance.eyebrowsOpacity = opacity;
                            break;
                        case 12: // ageing
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 3, selection, opacity);
                            currentCharacter.PedAppearance.ageingStyle = selection;
                            currentCharacter.PedAppearance.ageingOpacity = opacity;
                            break;
                        case 14: // makeup
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 4, selection, opacity);
                            currentCharacter.PedAppearance.makeupStyle = selection;
                            currentCharacter.PedAppearance.makeupOpacity = opacity;
                            break;
                        case 17: // blush style
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 5, selection, opacity);
                            currentCharacter.PedAppearance.blushStyle = selection;
                            currentCharacter.PedAppearance.blushOpacity = opacity;
                            break;
                        case 20: // complexion
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 6, selection, opacity);
                            currentCharacter.PedAppearance.complexionStyle = selection;
                            currentCharacter.PedAppearance.complexionOpacity = opacity;
                            break;
                        case 22: // sun damage
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 7, selection, opacity);
                            currentCharacter.PedAppearance.sunDamageStyle = selection;
                            currentCharacter.PedAppearance.sunDamageOpacity = opacity;
                            break;
                        case 24: // lipstick
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 8, selection, opacity);
                            currentCharacter.PedAppearance.lipstickStyle = selection;
                            currentCharacter.PedAppearance.lipstickOpacity = opacity;
                            break;
                        case 27: // moles and freckles
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 9, selection, opacity);
                            currentCharacter.PedAppearance.molesFrecklesStyle = selection;
                            currentCharacter.PedAppearance.molesFrecklesOpacity = opacity;
                            break;
                        case 29: // chest hair
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 10, selection, opacity);
                            currentCharacter.PedAppearance.chestHairStyle = selection;
                            currentCharacter.PedAppearance.chestHairOpacity = opacity;
                            break;
                        case 32: // body blemishes
                            SetPedHeadOverlay(Game.PlayerPed.Handle, 11, selection, opacity);
                            currentCharacter.PedAppearance.bodyBlemishesStyle = selection;
                            currentCharacter.PedAppearance.bodyBlemishesOpacity = opacity;
                            break;
                    }
                }
            };
            #endregion

            #region clothes
            clothesMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, realIndex) =>
            {
                var componentIndex = realIndex + 1;
                if (realIndex > 0)
                {
                    componentIndex += 1;
                }

                var textureIndex = GetPedTextureVariation(Game.PlayerPed.Handle, componentIndex);
                var newTextureIndex = 0;
                SetPedComponentVariation(Game.PlayerPed.Handle, componentIndex, newSelectionIndex, newTextureIndex, 0);
                currentCharacter.DrawableVariations.clothes ??= new Dictionary<int, KeyValuePair<int, int>>();

                var maxTextures = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, componentIndex, newSelectionIndex);

                currentCharacter.DrawableVariations.clothes[componentIndex] = new KeyValuePair<int, int>(newSelectionIndex, newTextureIndex);
                listItem.Description = $"Select a drawable using the arrow keys and press ~o~enter~s~ to cycle through all available textures. Texture actuellement sélectionnée : #{newTextureIndex + 1} (of {maxTextures}).";
            };

            clothesMenu.OnListItemSelect += (sender, listItem, listIndex, realIndex) =>
            {
                var componentIndex = realIndex + 1; // skip face options as that fucks up with inheritance faces
                if (realIndex > 0) // skip hair features as that is done in the appeareance menu
                {
                    componentIndex += 1;
                }

                var textureIndex = GetPedTextureVariation(Game.PlayerPed.Handle, componentIndex);
                var newTextureIndex = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, componentIndex, listIndex) - 1 < textureIndex + 1 ? 0 : textureIndex + 1;
                SetPedComponentVariation(Game.PlayerPed.Handle, componentIndex, listIndex, newTextureIndex, 0);
                currentCharacter.DrawableVariations.clothes ??= new Dictionary<int, KeyValuePair<int, int>>();

                var maxTextures = GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, componentIndex, listIndex);

                currentCharacter.DrawableVariations.clothes[componentIndex] = new KeyValuePair<int, int>(listIndex, newTextureIndex);
                listItem.Description = $"Sélectionnez une texture à l'aide des touches fléchées et appuyez sur ~o~entrer~s~. Texture actuellement sélectionnée : #{newTextureIndex + 1} (sur {maxTextures}).";
            };
            #endregion

            #region props
            propsMenu.OnListIndexChange += (_menu, listItem, oldSelectionIndex, newSelectionIndex, realIndex) =>
            {
                var propIndex = realIndex;
                if (realIndex == 3)
                {
                    propIndex = 6;
                }
                if (realIndex == 4)
                {
                    propIndex = 7;
                }

                var textureIndex = 0;
                if (newSelectionIndex >= GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, propIndex))
                {
                    SetPedPropIndex(Game.PlayerPed.Handle, propIndex, -1, -1, false);
                    ClearPedProp(Game.PlayerPed.Handle, propIndex);
                    currentCharacter.PropVariations.props ??= new Dictionary<int, KeyValuePair<int, int>>();
                    currentCharacter.PropVariations.props[propIndex] = new KeyValuePair<int, int>(-1, -1);
                    listItem.Description = $"Sélectionnez un accessoire à l'aide des touches fléchées et appuyez sur ~o~enter~s~";
                }
                else
                {
                    SetPedPropIndex(Game.PlayerPed.Handle, propIndex, newSelectionIndex, textureIndex, true);
                    currentCharacter.PropVariations.props ??= new Dictionary<int, KeyValuePair<int, int>>();
                    currentCharacter.PropVariations.props[propIndex] = new KeyValuePair<int, int>(newSelectionIndex, textureIndex);
                    if (GetPedPropIndex(Game.PlayerPed.Handle, propIndex) == -1)
                    {
                        listItem.Description = $"Sélectionnez un accessoire à l'aide des touches fléchées et appuyez sur ~o~enter~s~";
                    }
                    else
                    {
                        var maxPropTextures = GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, propIndex, newSelectionIndex);
                        listItem.Description = $"Sélectionnez un accessoire à l'aide des touches fléchées et appuyez sur ~o~enter~s~. Texture actuellement sélectionnée : #{textureIndex + 1} (sur {maxPropTextures}).";
                    }
                }
            };

            propsMenu.OnListItemSelect += (sender, listItem, listIndex, realIndex) =>
            {
                var propIndex = realIndex;
                if (realIndex == 3)
                {
                    propIndex = 6;
                }
                if (realIndex == 4)
                {
                    propIndex = 7;
                }

                var textureIndex = GetPedPropTextureIndex(Game.PlayerPed.Handle, propIndex);
                var newTextureIndex = GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, propIndex, listIndex) - 1 < textureIndex + 1 ? 0 : textureIndex + 1;
                if (textureIndex >= GetNumberOfPedPropDrawableVariations(Game.PlayerPed.Handle, propIndex))
                {
                    SetPedPropIndex(Game.PlayerPed.Handle, propIndex, -1, -1, false);
                    ClearPedProp(Game.PlayerPed.Handle, propIndex);
                    currentCharacter.PropVariations.props ??= new Dictionary<int, KeyValuePair<int, int>>();
                    currentCharacter.PropVariations.props[propIndex] = new KeyValuePair<int, int>(-1, -1);
                    listItem.Description = $"Sélectionnez un accessoire à l'aide des touches fléchées et appuyez sur ~o~enter~s~";
                }
                else
                {
                    SetPedPropIndex(Game.PlayerPed.Handle, propIndex, listIndex, newTextureIndex, true);
                    currentCharacter.PropVariations.props ??= new Dictionary<int, KeyValuePair<int, int>>();
                    currentCharacter.PropVariations.props[propIndex] = new KeyValuePair<int, int>(listIndex, newTextureIndex);
                    if (GetPedPropIndex(Game.PlayerPed.Handle, propIndex) == -1)
                    {
                        listItem.Description = $"Sélectionnez un accessoire à l'aide des touches fléchées et appuyez sur ~o~enter~s~";
                    }
                    else
                    {
                        var maxPropTextures = GetNumberOfPedPropTextureVariations(Game.PlayerPed.Handle, propIndex, listIndex);
                        listItem.Description = $"Sélectionnez un accessoire à l'aide des touches fléchées et appuyez sur ~o~enter~s~. Texture actuellement sélectionnée : #{newTextureIndex + 1} (of {maxPropTextures}).";
                    }
                }
                //propsMenu.UpdateScaleform();
            };
            #endregion

            #region face shape data
            /*
            Nose_Width  
            Nose_Peak_Hight  
            Nose_Peak_Lenght  
            Nose_Bone_High  
            Nose_Peak_Lowering  
            Nose_Bone_Twist  
            EyeBrown_High  
            EyeBrown_Forward  
            Cheeks_Bone_High  
            Cheeks_Bone_Width  
            Cheeks_Width  
            Eyes_Openning  
            Lips_Thickness  
            Jaw_Bone_Width 'Bone size to sides  
            Jaw_Bone_Back_Lenght 'Bone size to back  
            Chimp_Bone_Lowering 'Go Down  
            Chimp_Bone_Lenght 'Go forward  
            Chimp_Bone_Width  
            Chimp_Hole  
            Neck_Thikness  
            */

            var faceFeaturesNamesList = new string[20]
            {
                "Largeur du nez",           
                "Hauteur de la pointe du nez",          
                "Longueur de la pointe du nez",         
                "Hauteur de l'os du nez",           
                "Abaissement de la pointe du nez",          
                "Torsion de l'os du nez",           
                "Hauteur des sourcils",         
                "Profondeur des sourcils",          
                "Hauteur des pommettes",            
                "Largeur des pommettes",            
                "Largeur des joues",           
                "Ouverture des yeux",          
                "Épaisseur des lèvres",            
                "Largeur de l'os de la mâchoire",          
                "Profondeur/longueur de l'os de la mâchoire",          
                "Hauteur du menton",           
                "Profondeur/longueur du menton",           
                "Largeur du menton",           
                "Taille du trou du menton",            
                "Épaisseur du coup"
            };

            for (var i = 0; i < 20; i++)
            {
                var faceFeature = new MenuSliderItem(faceFeaturesNamesList[i], $"Définir le/la {faceFeaturesNamesList[i]}.", 0, 20, 10, true);
                faceShapeMenu.AddMenuItem(faceFeature);
            }

            faceShapeMenu.OnSliderPositionChange += (sender, sliderItem, oldPosition, newPosition, itemIndex) =>
            {
                currentCharacter.FaceShapeFeatures.features ??= new Dictionary<int, float>();
                var value = faceFeaturesValuesList[newPosition];
                currentCharacter.FaceShapeFeatures.features[itemIndex] = value;
                SetPedFaceFeature(Game.PlayerPed.Handle, itemIndex, value);
            };

            #endregion

            #region tattoos
            void CreateListsIfNull()
            {
                currentCharacter.PedTatttoos.HeadTattoos ??= new List<KeyValuePair<string, string>>();
                currentCharacter.PedTatttoos.TorsoTattoos ??= new List<KeyValuePair<string, string>>();
                currentCharacter.PedTatttoos.LeftArmTattoos ??= new List<KeyValuePair<string, string>>();
                currentCharacter.PedTatttoos.RightArmTattoos ??= new List<KeyValuePair<string, string>>();
                currentCharacter.PedTatttoos.LeftLegTattoos ??= new List<KeyValuePair<string, string>>();
                currentCharacter.PedTatttoos.RightLegTattoos ??= new List<KeyValuePair<string, string>>();
                currentCharacter.PedTatttoos.BadgeTattoos ??= new List<KeyValuePair<string, string>>();
            }

            void ApplySavedTattoos()
            {
                // remove all decorations, and then manually re-add them all. what a retarded way of doing this R*....
                ClearPedDecorations(Game.PlayerPed.Handle);

                foreach (var tattoo in currentCharacter.PedTatttoos.HeadTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.TorsoTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.LeftArmTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.RightArmTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.LeftLegTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.RightLegTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }
                foreach (var tattoo in currentCharacter.PedTatttoos.BadgeTattoos)
                {
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
                }

                if (!string.IsNullOrEmpty(currentCharacter.PedAppearance.HairOverlay.Key) && !string.IsNullOrEmpty(currentCharacter.PedAppearance.HairOverlay.Value))
                {
                    // reset hair value
                    SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(currentCharacter.PedAppearance.HairOverlay.Key), (uint)GetHashKey(currentCharacter.PedAppearance.HairOverlay.Value));
                }
            }

            tattoosMenu.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) =>
            {
                CreateListsIfNull();
                ApplySavedTattoos();
            };

            #region tattoos menu list select events
            tattoosMenu.OnListIndexChange += (sender, item, oldIndex, tattooIndex, menuIndex) =>
            {
                CreateListsIfNull();
                ApplySavedTattoos();
                if (menuIndex == 0) // head
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.HEAD.ElementAt(tattooIndex) : FemaleTattoosCollection.HEAD.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.HeadTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 1) // torso
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.TORSO.ElementAt(tattooIndex) : FemaleTattoosCollection.TORSO.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.TorsoTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 2) // left arm
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.LEFT_ARM.ElementAt(tattooIndex) : FemaleTattoosCollection.LEFT_ARM.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.LeftArmTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 3) // right arm
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.RIGHT_ARM.ElementAt(tattooIndex) : FemaleTattoosCollection.RIGHT_ARM.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.RightArmTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 4) // left leg
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.LEFT_LEG.ElementAt(tattooIndex) : FemaleTattoosCollection.LEFT_LEG.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.LeftLegTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 5) // right leg
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.RIGHT_LEG.ElementAt(tattooIndex) : FemaleTattoosCollection.RIGHT_LEG.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.RightLegTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
                else if (menuIndex == 6) // badges
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.BADGES.ElementAt(tattooIndex) : FemaleTattoosCollection.BADGES.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (!currentCharacter.PedTatttoos.BadgeTattoos.Contains(tat))
                    {
                        SetPedDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(tat.Key), (uint)GetHashKey(tat.Value));
                    }
                }
            };

            tattoosMenu.OnListItemSelect += (sender, item, tattooIndex, menuIndex) =>
            {
                CreateListsIfNull();

                if (menuIndex == 0) // head
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.HEAD.ElementAt(tattooIndex) : FemaleTattoosCollection.HEAD.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.HeadTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~r~retiré~s~.");
                        currentCharacter.PedTatttoos.HeadTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~g~ajouté~s~.");
                        currentCharacter.PedTatttoos.HeadTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 1) // torso
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.TORSO.ElementAt(tattooIndex) : FemaleTattoosCollection.TORSO.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.TorsoTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~r~retiré~s~.");
                        currentCharacter.PedTatttoos.TorsoTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~g~ajouté~s~.");
                        currentCharacter.PedTatttoos.TorsoTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 2) // left arm
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.LEFT_ARM.ElementAt(tattooIndex) : FemaleTattoosCollection.LEFT_ARM.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.LeftArmTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~r~retiré~s~.");
                        currentCharacter.PedTatttoos.LeftArmTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~g~ajouté~s~.");
                        currentCharacter.PedTatttoos.LeftArmTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 3) // right arm
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.RIGHT_ARM.ElementAt(tattooIndex) : FemaleTattoosCollection.RIGHT_ARM.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.RightArmTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~r~retiré~s~.");
                        currentCharacter.PedTatttoos.RightArmTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~g~ajouté~s~.");
                        currentCharacter.PedTatttoos.RightArmTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 4) // left leg
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.LEFT_LEG.ElementAt(tattooIndex) : FemaleTattoosCollection.LEFT_LEG.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.LeftLegTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~r~retiré~s~.");
                        currentCharacter.PedTatttoos.LeftLegTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~g~ajouté~s~.");
                        currentCharacter.PedTatttoos.LeftLegTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 5) // right leg
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.RIGHT_LEG.ElementAt(tattooIndex) : FemaleTattoosCollection.RIGHT_LEG.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.RightLegTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~r~retiré~s~.");
                        currentCharacter.PedTatttoos.RightLegTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Tattoo #{tattooIndex + 1} a été ~g~ajouté~s~.");
                        currentCharacter.PedTatttoos.RightLegTattoos.Add(tat);
                    }
                }
                else if (menuIndex == 6) // badges
                {
                    var Tattoo = currentCharacter.IsMale ? MaleTattoosCollection.BADGES.ElementAt(tattooIndex) : FemaleTattoosCollection.BADGES.ElementAt(tattooIndex);
                    var tat = new KeyValuePair<string, string>(Tattoo.collectionName, Tattoo.name);
                    if (currentCharacter.PedTatttoos.BadgeTattoos.Contains(tat))
                    {
                        Subtitle.Custom($"Badge #{tattooIndex + 1} a été ~r~retiré~s~.");
                        currentCharacter.PedTatttoos.BadgeTattoos.Remove(tat);
                    }
                    else
                    {
                        Subtitle.Custom($"Badge #{tattooIndex + 1} a été ~g~ajouté~s~.");
                        currentCharacter.PedTatttoos.BadgeTattoos.Add(tat);
                    }
                }

                ApplySavedTattoos();

            };

            // eventhandler for when a tattoo is selected.
            tattoosMenu.OnItemSelect += (sender, item, index) =>
            {
                Notify.Success("Tous les tatouages ont été enlevés.");
                currentCharacter.PedTatttoos.HeadTattoos.Clear();
                currentCharacter.PedTatttoos.TorsoTattoos.Clear();
                currentCharacter.PedTatttoos.LeftArmTattoos.Clear();
                currentCharacter.PedTatttoos.RightArmTattoos.Clear();
                currentCharacter.PedTatttoos.LeftLegTattoos.Clear();
                currentCharacter.PedTatttoos.RightLegTattoos.Clear();
                currentCharacter.PedTatttoos.BadgeTattoos.Clear();
                ClearPedDecorations(Game.PlayerPed.Handle);
            };

            #endregion
            #endregion


            // handle list changes in the character creator menu.
            createCharacterMenu.OnListIndexChange += (sender, item, oldListIndex, newListIndex, itemIndex) =>
            {
                if (item == faceExpressionList)
                {
                    currentCharacter.FacialExpression = facial_expressions[newListIndex];
                    SetFacialIdleAnimOverride(Game.PlayerPed.Handle, currentCharacter.FacialExpression ?? facial_expressions[0], null);
                }
                else if (item == categoryBtn)
                {
                    List<string> categoryNames = categoryBtn.ItemData.Item1;
                    List<MenuItem.Icon> categoryIcons = categoryBtn.ItemData.Item2;
                    currentCharacter.Category = categoryNames[newListIndex];
                    categoryBtn.RightIcon = categoryIcons[newListIndex];
                }
            };

            // handle button presses for the createCharacter menu.
            createCharacterMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == randomizeButton)
                {
                    _dadSelection = _random.Next(parents.Count);
                    _mumSelection = _random.Next(parents.Count);
                    _skinMixValue = _random.Next(2, 8);
                    _shapeMixValue = _random.Next(2, 8);

                    SetHeadBlend();

                    if (currentCharacter.FaceShapeFeatures.features == null)
                    {
                        currentCharacter.FaceShapeFeatures.features = [];
                    }

                    for (int i = 0; i < 20; i++)
                    {
                        shapeFaceValues[i] = _random.Next(5, 15);
                        SetPedFaceFeature(Game.PlayerPed.Handle, i, faceFeaturesValuesList[shapeFaceValues[i]]);
                        currentCharacter.FaceShapeFeatures.features[i] = faceFeaturesValuesList[shapeFaceValues[i]];
                    }

                    int bodyHair = _random.Next(31);

                    ChangePlayerHair(_random.Next(0, GetNumberOfPedDrawableVariations(Game.PlayerPed.Handle, 2)));
                    ChangePlayerHairColor(bodyHair, _random.Next(31));
                    ChangePlayerEyeColor(_random.Next(0, 9));

                    for (int i = 0; i < 12; i++)
                    {
                        int value;
                        int colorIndex = 0;
                        bool colorRequired = false;

                        int color = i == 1 || i == 2 || i == 10 ? bodyHair : _random.Next(17);
                        float opacity = (float)_random.NextDouble();

                        switch (i)
                        {
                            case 0:
                                value = _random.Next(blemishesStyleList.Count);

                                currentCharacter.PedAppearance.blemishesStyle = value;
                                currentCharacter.PedAppearance.blemishesOpacity = opacity;
                                break;

                            case 1:
                                if (!currentCharacter.IsMale)
                                {
                                    continue;
                                }

                                value = _random.Next(beardStylesList.Count);
                                colorRequired = true;
                                colorIndex = 1;

                                currentCharacter.PedAppearance.beardStyle = value;
                                currentCharacter.PedAppearance.beardColor = color;
                                currentCharacter.PedAppearance.beardOpacity = opacity;
                                break;

                            case 2:
                                value = _random.Next(eyebrowsStyleList.Count);
                                colorRequired = true;
                                colorIndex = 1;

                                currentCharacter.PedAppearance.eyebrowsColor = value;
                                currentCharacter.PedAppearance.eyebrowsStyle = color;
                                currentCharacter.PedAppearance.eyebrowsOpacity = opacity;
                                break;

                            case 3:
                                value = _random.Next(ageingStyleList.Count);

                                currentCharacter.PedAppearance.ageingStyle = value;
                                currentCharacter.PedAppearance.ageingOpacity = opacity;
                                break;

                            case 8:
                                if (currentCharacter.IsMale)
                                {
                                    continue;
                                }

                                value = _random.Next(6);
                                colorRequired = true;
                                colorIndex = 2;

                                currentCharacter.PedAppearance.lipstickStyle = value;
                                currentCharacter.PedAppearance.lipstickColor = color;
                                currentCharacter.PedAppearance.lipstickOpacity = opacity;
                                break;

                            case 9:
                                value = _random.Next(molesFrecklesStyleList.Count);

                                currentCharacter.PedAppearance.molesFrecklesStyle = value;
                                currentCharacter.PedAppearance.molesFrecklesOpacity = opacity;
                                break;

                            case 10:
                                if (!currentCharacter.IsMale)
                                {
                                    continue;
                                }

                                value = _random.Next(8);
                                colorRequired = true;
                                colorIndex = 1;

                                currentCharacter.PedAppearance.chestHairStyle = value;
                                currentCharacter.PedAppearance.chestHairColor = color;
                                currentCharacter.PedAppearance.chestHairOpacity = opacity;
                                break;

                            case 11:
                                value = _random.Next(bodyBlemishesList.Count);

                                currentCharacter.PedAppearance.bodyBlemishesStyle = value;
                                currentCharacter.PedAppearance.bodyBlemishesOpacity = opacity;
                                break;

                            default:
                                apperanceValues[i] = new Tuple<int, int, float>(0, 0, 0);
                                continue;
                        }

                        apperanceValues[i] = new Tuple<int, int, float>(value, color, opacity);
                        SetPedHeadOverlay(Game.PlayerPed.Handle, i, apperanceValues[i].Item1, apperanceValues[i].Item3);

                        if (colorRequired)
                        {
                            SetPedHeadOverlayColor(Game.PlayerPed.Handle, i, colorIndex, apperanceValues[i].Item2, apperanceValues[i].Item2);
                        }
                    }

                    _facialExpressionSelection = _random.Next(facial_expressions.Count);

                    SetFacialIdleAnimOverride(Game.PlayerPed.Handle, facial_expressions[_facialExpressionSelection], null);

                    currentCharacter.FacialExpression = facial_expressions[_facialExpressionSelection];

                    ((MenuListItem)createCharacterMenu.GetMenuItems()[7]).ListIndex = _facialExpressionSelection;

                    SetPlayerClothing();
                }
                else if (item == saveButton) // save ped
                {
                    if (await SavePed())
                    {
                        while (!MenuController.IsAnyMenuOpen())
                        {
                            await BaseScript.Delay(0);
                        }

                        while (IsControlPressed(2, 201) || IsControlPressed(2, 217) || IsDisabledControlPressed(2, 201) || IsDisabledControlPressed(2, 217))
                        {
                            await BaseScript.Delay(0);
                        }

                        await BaseScript.Delay(100);

                        createCharacterMenu.GoBack();
                    }
                }
                else if (item == exitNoSave) // exit without saving
                {
                    var confirm = false;
                    AddTextEntry("vmenu_warning_message_first_line", "Êtes-vous sûr de vouloir quitter le créateur de personnage ?");
                    AddTextEntry("vmenu_warning_message_second_line", "Vous perdrez toutes les personnalisations (non sauvegardées) !");
                    createCharacterMenu.CloseMenu();

                    // wait for confirmation or cancel input.
                    while (true)
                    {
                        await BaseScript.Delay(0);
                        var unk = 1;
                        var unk2 = 1;
                        SetWarningMessage("vmenu_warning_message_first_line", 20, "vmenu_warning_message_second_line", true, 0, ref unk, ref unk2, true, 0);
                        if (IsControlJustPressed(2, 201) || IsControlJustPressed(2, 217)) // continue/accept
                        {
                            confirm = true;
                            break;
                        }
                        else if (IsControlJustPressed(2, 202)) // cancel
                        {
                            break;
                        }
                    }

                    // if confirmed to discard changes quit the editor.
                    if (confirm)
                    {
                        while (IsControlPressed(2, 201) || IsControlPressed(2, 217) || IsDisabledControlPressed(2, 201) || IsDisabledControlPressed(2, 217))
                        {
                            await BaseScript.Delay(0);
                        }

                        await BaseScript.Delay(100);
                        menu.OpenMenu();
                    }
                    else // otherwise cancel and go back to the editor.
                    {
                        createCharacterMenu.OpenMenu();
                    }
                }
                else if (item == inheritanceButton) // update the inheritance menu anytime it's opened to prevent some weird glitch where old data is used.
                {
                    inheritanceDads.ListIndex = _dadSelection;
                    inheritanceMoms.ListIndex = _mumSelection;
                    inheritanceShapeMix.Position = (int)_shapeMixValue;
                    inheritanceSkinMix.Position = (int)_skinMixValue;
                    inheritanceMenu.RefreshIndex();
                }
                else if (item == faceButton)
                {
                    List<MenuItem> items = faceShapeMenu.GetMenuItems();

                    for (int i = 0; i < 20; i++)
                    {
                        if (items[i] is MenuSliderItem sliderItem)
                        {
                            sliderItem.Position = shapeFaceValues[i];
                        }
                    }

                    faceShapeMenu.RefreshIndex();
                }
                else if (item == appearanceButton)
                {
                    List<MenuItem> items = appearanceMenu.GetMenuItems();

                    ((MenuListItem)items[0]).ListIndex = _hairSelection;
                    ((MenuListItem)items[1]).ListIndex = _hairColorSelection;
                    ((MenuListItem)items[2]).ListIndex = _hairHighlightColorSelection;
                    ((MenuListItem)items[33]).ListIndex = _eyeColorSelection;

                    ((MenuListItem)items[3]).ListIndex = apperanceValues[0].Item1;
                    ((MenuListItem)items[4]).ListIndex = (int)(apperanceValues[0].Item3 * 10);

                    ((MenuListItem)items[5]).ListIndex = apperanceValues[1].Item1;
                    ((MenuListItem)items[6]).ListIndex = (int)(apperanceValues[1].Item3 * 10);
                    ((MenuListItem)items[7]).ListIndex = apperanceValues[1].Item1;

                    ((MenuListItem)items[8]).ListIndex = apperanceValues[2].Item1;
                    ((MenuListItem)items[9]).ListIndex = (int)(apperanceValues[2].Item3 * 10);
                    ((MenuListItem)items[10]).ListIndex = apperanceValues[2].Item1;

                    ((MenuListItem)items[11]).ListIndex = apperanceValues[3].Item1;
                    ((MenuListItem)items[12]).ListIndex = (int)(apperanceValues[3].Item3 * 10);

                    ((MenuListItem)items[13]).ListIndex = apperanceValues[4].Item1;
                    ((MenuListItem)items[14]).ListIndex = (int)(apperanceValues[4].Item3 * 10);
                    ((MenuListItem)items[15]).ListIndex = apperanceValues[4].Item1;

                    ((MenuListItem)items[16]).ListIndex = apperanceValues[5].Item1;
                    ((MenuListItem)items[17]).ListIndex = (int)(apperanceValues[5].Item3 * 10);
                    ((MenuListItem)items[18]).ListIndex = apperanceValues[5].Item1;

                    ((MenuListItem)items[19]).ListIndex = apperanceValues[6].Item1;
                    ((MenuListItem)items[20]).ListIndex = (int)(apperanceValues[6].Item3 * 10);

                    ((MenuListItem)items[21]).ListIndex = apperanceValues[7].Item1;
                    ((MenuListItem)items[22]).ListIndex = (int)(apperanceValues[7].Item3 * 10);

                    ((MenuListItem)items[23]).ListIndex = apperanceValues[8].Item1;
                    ((MenuListItem)items[24]).ListIndex = (int)(apperanceValues[8].Item3 * 10);
                    ((MenuListItem)items[25]).ListIndex = apperanceValues[8].Item1;

                    ((MenuListItem)items[26]).ListIndex = apperanceValues[9].Item1;
                    ((MenuListItem)items[27]).ListIndex = (int)(apperanceValues[9].Item3 * 10);

                    ((MenuListItem)items[28]).ListIndex = apperanceValues[10].Item1;
                    ((MenuListItem)items[29]).ListIndex = (int)(apperanceValues[10].Item3 * 10);
                    ((MenuListItem)items[30]).ListIndex = apperanceValues[10].Item1;

                    ((MenuListItem)items[31]).ListIndex = apperanceValues[11].Item1;
                    ((MenuListItem)items[32]).ListIndex = (int)(apperanceValues[11].Item3 * 10);

                    appearanceMenu.RefreshIndex();
                }
            };

            // eventhandler for whenever a menu item is selected in the main mp characters menu.
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == createMaleBtn)
                {
                    var model = (uint)GetHashKey("mp_m_freemode_01");

                    if (!HasModelLoaded(model))
                    {
                        RequestModel(model);
                        while (!HasModelLoaded(model))
                        {
                            await BaseScript.Delay(0);
                        }
                    }

                    var maxHealth = Game.PlayerPed.MaxHealth;
                    var maxArmour = Game.Player.MaxArmor;
                    var health = Game.PlayerPed.Health;
                    var armour = Game.PlayerPed.Armor;

                    SaveWeaponLoadout("vmenu_temp_weapons_loadout_before_respawn");
                    SetPlayerModel(Game.Player.Handle, model);
                    await SpawnWeaponLoadoutAsync("vmenu_temp_weapons_loadout_before_respawn", false, true, true);

                    Game.Player.MaxArmor = maxArmour;
                    Game.PlayerPed.MaxHealth = maxHealth;
                    Game.PlayerPed.Health = health;
                    Game.PlayerPed.Armor = armour;

                    ClearPedDecorations(Game.PlayerPed.Handle);
                    ClearPedFacialDecorations(Game.PlayerPed.Handle);
                    SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                    SetPedHairColor(Game.PlayerPed.Handle, 0, 0);
                    SetPedEyeColor(Game.PlayerPed.Handle, 0);
                    ClearAllPedProps(Game.PlayerPed.Handle);

                    MakeCreateCharacterMenu(male: true);
                }
                else if (item == createFemaleBtn)
                {
                    var model = (uint)GetHashKey("mp_f_freemode_01");

                    if (!HasModelLoaded(model))
                    {
                        RequestModel(model);
                        while (!HasModelLoaded(model))
                        {
                            await BaseScript.Delay(0);
                        }
                    }

                    var maxHealth = Game.PlayerPed.MaxHealth;
                    var maxArmour = Game.Player.MaxArmor;
                    var health = Game.PlayerPed.Health;
                    var armour = Game.PlayerPed.Armor;

                    SaveWeaponLoadout("vmenu_temp_weapons_loadout_before_respawn");
                    SetPlayerModel(Game.Player.Handle, model);
                    await SpawnWeaponLoadoutAsync("vmenu_temp_weapons_loadout_before_respawn", false, true, true);

                    Game.Player.MaxArmor = maxArmour;
                    Game.PlayerPed.MaxHealth = maxHealth;
                    Game.PlayerPed.Health = health;
                    Game.PlayerPed.Armor = armour;

                    ClearPedDecorations(Game.PlayerPed.Handle);
                    ClearPedFacialDecorations(Game.PlayerPed.Handle);
                    SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                    SetPedHairColor(Game.PlayerPed.Handle, 0, 0);
                    SetPedEyeColor(Game.PlayerPed.Handle, 0);
                    ClearAllPedProps(Game.PlayerPed.Handle);

                    MakeCreateCharacterMenu(male: false);
                }
                else if (item == savedCharacters)
                {
                    UpdateSavedPedsMenu();
                }
            };
        }

        /// <summary>
        /// Spawns this saved ped.
        /// </summary>
        /// <param name="name"></param>
        internal async Task SpawnThisCharacter(string name, bool restoreWeapons)
        {
            currentCharacter = StorageManager.GetSavedMpCharacterData(name);
            await SpawnSavedPed(restoreWeapons);
        }

        /// <summary>
        /// Spawns the ped from the data inside <see cref="currentCharacter"/>.
        /// Character data MUST be set BEFORE calling this function.
        /// </summary>
        /// <returns></returns>
        private async Task SpawnSavedPed(bool restoreWeapons)
        {
            if (currentCharacter.Version < 1)
            {
                return;
            }
            if (IsModelInCdimage(currentCharacter.ModelHash))
            {
                if (!HasModelLoaded(currentCharacter.ModelHash))
                {
                    RequestModel(currentCharacter.ModelHash);
                    while (!HasModelLoaded(currentCharacter.ModelHash))
                    {
                        await BaseScript.Delay(0);
                    }
                }
                var maxHealth = Game.PlayerPed.MaxHealth;
                var maxArmour = Game.Player.MaxArmor;
                var health = Game.PlayerPed.Health;
                var armour = Game.PlayerPed.Armor;

                SaveWeaponLoadout("vmenu_temp_weapons_loadout_before_respawn");
                SetPlayerModel(Game.Player.Handle, currentCharacter.ModelHash);
                await SpawnWeaponLoadoutAsync("vmenu_temp_weapons_loadout_before_respawn", false, true, true);

                Game.Player.MaxArmor = maxArmour;
                Game.PlayerPed.MaxHealth = maxHealth;
                Game.PlayerPed.Health = health;
                Game.PlayerPed.Armor = armour;

                ClearPedDecorations(Game.PlayerPed.Handle);
                ClearPedFacialDecorations(Game.PlayerPed.Handle);
                SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                SetPedHairColor(Game.PlayerPed.Handle, 0, 0);
                SetPedEyeColor(Game.PlayerPed.Handle, 0);
                ClearAllPedProps(Game.PlayerPed.Handle);

                await AppySavedDataToPed(currentCharacter, Game.PlayerPed.Handle);
            }

            // Set the facial expression, or set it to 'normal' if it wasn't saved/set before.
            SetFacialIdleAnimOverride(Game.PlayerPed.Handle, currentCharacter.FacialExpression ?? facial_expressions[0], null);
        }

        /// <summary>
        /// Creates the saved mp characters menu.
        /// </summary>
        private void CreateSavedPedsMenu()
        {
            UpdateSavedPedsMenu();

            MenuController.AddMenu(manageSavedCharacterMenu);

            var spawnPed = new MenuItem("Faire apparaître le personnage sauvegardé", "Fait apparaître le personnage sauvegardé sélectionné.");
            editPedBtn = new MenuItem("Modifier le personnage sauvegardé", "Cela vous permet de modifier tout ce qui concerne votre personnage sauvegardé. Les modifications seront enregistrées dans le fichier de sauvegarde de ce personnage une fois que vous aurez cliqué sur le bouton de sauvegarde.");
            var clonePed = new MenuItem("Cloner le personnage sauvegardé", "Cela créera un clone de votre personnage sauvegardé. Il vous sera demandé de fournir un nom pour ce personnage. Si ce nom est déjà pris, l'action sera annulée.");
            var setAsDefaultPed = new MenuItem("Définir comme personnage par défaut", "Si vous définissez ce personnage comme votre personnage par défaut et que vous activez l'option 'Réapparaître en tant que personnage MP par défaut' dans le menu des paramètres divers, vous serez défini comme ce personnage chaque fois que vous (re)apparaîtrez.");
            var renameCharacter = new MenuItem("Renommer le personnage sauvegardé", "Vous pouvez renommer ce personnage sauvegardé. Si le nom est déjà pris, l'action sera annulée.");
            var delPed = new MenuItem("Supprimer le personnage sauvegardé", "Supprime le personnage sauvegardé sélectionné. Cela ne peut pas être annulé !")
            {
                LeftIcon = MenuItem.Icon.WARNING
            };
            manageSavedCharacterMenu.AddMenuItem(spawnPed);
            manageSavedCharacterMenu.AddMenuItem(editPedBtn);
            manageSavedCharacterMenu.AddMenuItem(clonePed);
            manageSavedCharacterMenu.AddMenuItem(setCategoryBtn);
            manageSavedCharacterMenu.AddMenuItem(setAsDefaultPed);
            manageSavedCharacterMenu.AddMenuItem(renameCharacter);
            manageSavedCharacterMenu.AddMenuItem(delPed);

            MenuController.BindMenuItem(manageSavedCharacterMenu, createCharacterMenu, editPedBtn);

            manageSavedCharacterMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == editPedBtn)
                {
                    currentCharacter = StorageManager.GetSavedMpCharacterData(selectedSavedCharacterManageName);

                    await SpawnSavedPed(true);

                    MakeCreateCharacterMenu(male: currentCharacter.IsMale, editPed: true);
                }
                else if (item == spawnPed)
                {
                    currentCharacter = StorageManager.GetSavedMpCharacterData(selectedSavedCharacterManageName);

                    await SpawnSavedPed(true);
                }
                else if (item == clonePed)
                {
                    var tmpCharacter = StorageManager.GetSavedMpCharacterData("mp_ped_" + selectedSavedCharacterManageName);
                    var name = await GetUserInput(windowTitle: "Saisir un nom pour le personnage cloné", defaultText: tmpCharacter.SaveName.Substring(7), maxInputLength: 30);
                    if (string.IsNullOrEmpty(name))
                    {
                        Notify.Error(CommonErrors.InvalidSaveName);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(GetResourceKvpString("mp_ped_" + name)))
                        {
                            Notify.Error(CommonErrors.SaveNameAlreadyExists);
                        }
                        else
                        {
                            tmpCharacter.SaveName = "mp_ped_" + name;
                            if (StorageManager.SaveJsonData("mp_ped_" + name, JsonConvert.SerializeObject(tmpCharacter), false))
                            {
                                Notify.Success($"Votre personnage a été cloné. Le nom du personnage cloné est : ~g~<C>{name}</C>~s~.");
                                MenuController.CloseAllMenus();
                                UpdateSavedPedsMenu();
                                savedCharactersMenu.OpenMenu();
                            }
                            else
                            {
                                Notify.Error("Le clone n'a pas pu être créé, raison inconnue. Existe-t-il déjà un personnage portant ce nom ? :(");
                            }
                        }
                    }
                }
                else if (item == renameCharacter)
                {
                    var tmpCharacter = StorageManager.GetSavedMpCharacterData("mp_ped_" + selectedSavedCharacterManageName);
                    var name = await GetUserInput(windowTitle: "Saisir un nouveau nom de caractère", defaultText: tmpCharacter.SaveName.Substring(7), maxInputLength: 30);
                    if (string.IsNullOrEmpty(name))
                    {
                        Notify.Error(CommonErrors.InvalidInput);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(GetResourceKvpString("mp_ped_" + name)))
                        {
                            Notify.Error(CommonErrors.SaveNameAlreadyExists);
                        }
                        else
                        {
                            tmpCharacter.SaveName = "mp_ped_" + name;
                            if (StorageManager.SaveJsonData("mp_ped_" + name, JsonConvert.SerializeObject(tmpCharacter), false))
                            {
                                StorageManager.DeleteSavedStorageItem("mp_ped_" + selectedSavedCharacterManageName);
                                Notify.Success($"Votre personnage a été renommé en ~g~<C>{name}</C>~s~.");
                                UpdateSavedPedsMenu();
                                while (!MenuController.IsAnyMenuOpen())
                                {
                                    await BaseScript.Delay(0);
                                }
                                manageSavedCharacterMenu.GoBack();
                            }
                            else
                            {
                                Notify.Error("Quelque chose s'est mal passé lors du renommage de votre personnage, votre ancien personnage ne sera PAS supprimé pour autant.");
                            }
                        }
                    }
                }
                else if (item == delPed)
                {
                    if (delPed.Label == "Êtes-vous sûr ?")
                    {
                        delPed.Label = "";
                        DeleteResourceKvp("mp_ped_" + selectedSavedCharacterManageName);
                        Notify.Success("Votre personnage sauvegardé a été supprimé.");
                        manageSavedCharacterMenu.GoBack();
                        UpdateSavedPedsMenu();
                        manageSavedCharacterMenu.RefreshIndex();
                    }
                    else
                    {
                        delPed.Label = "Êtes-vous sûr ?";
                    }
                }
                else if (item == setAsDefaultPed)
                {
                    Notify.Success($"Votre personnage <C>{selectedSavedCharacterManageName}</C> sera désormais utilisé comme personnage par défaut à chaque (re)spawn.");
                    SetResourceKvp("vmenu_default_character", "mp_ped_" + selectedSavedCharacterManageName);
                }

                if (item != delPed)
                {
                    if (delPed.Label == "Êtes-vous sûr ?")
                    {
                        delPed.Label = "";
                    }
                }
            };

            // Update category preview icon
            manageSavedCharacterMenu.OnListIndexChange += (_, listItem, _, newSelectionIndex, _) => listItem.RightIcon = listItem.ItemData[newSelectionIndex];

            // Update character's category
            manageSavedCharacterMenu.OnListItemSelect += async (_, listItem, listIndex, _) =>
            {
                var tmpCharacter = StorageManager.GetSavedMpCharacterData("mp_ped_" + selectedSavedCharacterManageName);

                string name = listItem.ListItems[listIndex];

                if (name == "Create New")
                {
                    var newName = await GetUserInput(windowTitle: "Saisir un nom de catégorie.", maxInputLength: 30);
                    if (string.IsNullOrEmpty(newName) || newName.ToLower() == "uncategorized" || newName.ToLower() == "create new")
                    {
                        Notify.Error(CommonErrors.InvalidInput);
                        return;
                    }
                    else
                    {
                        var description = await GetUserInput(windowTitle: "Saisissez une description de la catégorie (facultatif).", maxInputLength: 120);
                        var newCategory = new MpCharacterCategory
                        {
                            Name = newName,
                            Description = description
                        };

                        if (StorageManager.SaveJsonData("mp_character_category_" + newName, JsonConvert.SerializeObject(newCategory), false))
                        {
                            Notify.Success($"Votre catégorie (~g~<C>{newName}</C>~s~) a été sauvegardé.");
                            Log($"Catégorie sauvegardée {newName}.");
                            MenuController.CloseAllMenus();
                            UpdateSavedPedsMenu();
                            savedCharactersCategoryMenu.OpenMenu();

                            currentCategory = newCategory;
                            name = newName;
                        }
                        else
                        {
                            Notify.Error($"La sauvegarde a échoué, très probablement parce que ce nom (~y~<C>{newName}</C>~s~) est déjà utilisé.");
                            return;
                        }
                    }
                }

                tmpCharacter.Category = name;

                var json = JsonConvert.SerializeObject(tmpCharacter);
                if (StorageManager.SaveJsonData(tmpCharacter.SaveName, json, true))
                {
                    Notify.Success("Votre personnage a été sauvegardé avec succès.");
                }
                else
                {
                    Notify.Error("Votre personnage n'a pas pu être sauvegardé. Raison inconnue. :(");
                }

                MenuController.CloseAllMenus();
                UpdateSavedPedsMenu();
                savedCharactersMenu.OpenMenu();
            };

            // reset the "are you sure" state.
            manageSavedCharacterMenu.OnMenuClose += (sender) =>
            {
                manageSavedCharacterMenu.GetMenuItems().Last().Label = "";
            };

            // Load selected category
            savedCharactersMenu.OnItemSelect += async (sender, item, index) =>
            {
                // Create new category
                if (item.ItemData is not MpCharacterCategory)
                {
                    var name = await GetUserInput(windowTitle: "Saisir un nom de catégorie.", maxInputLength: 30);
                    if (string.IsNullOrEmpty(name) || name.ToLower() == "uncategorized" || name.ToLower() == "create new")
                    {
                        Notify.Error(CommonErrors.InvalidInput);
                        return;
                    }
                    else
                    {
                        var description = await GetUserInput(windowTitle: "Saisissez une description de la catégorie (facultatif).", maxInputLength: 120);
                        var newCategory = new MpCharacterCategory
                        {
                            Name = name,
                            Description = description
                        };

                        if (StorageManager.SaveJsonData("mp_character_category_" + name, JsonConvert.SerializeObject(newCategory), false))
                        {
                            Notify.Success($"Votre catégorie (~g~<C>{name}</C>~s~) a été sauvegardé.");
                            Log($"Catégorie sauvegardée {name}.");
                            MenuController.CloseAllMenus();
                            UpdateSavedPedsMenu();
                            savedCharactersCategoryMenu.OpenMenu();

                            currentCategory = newCategory;
                        }
                        else
                        {
                            Notify.Error($"La sauvegarde a échoué, très probablement parce que ce nom (~y~<C>{name}</C>~s~) est déjà utilisé.");
                            return;
                        }
                    }
                }
                // Select an old category
                else
                {
                    currentCategory = item.ItemData;
                }

                bool isUncategorized = currentCategory.Name == "Non classé";

                savedCharactersCategoryMenu.MenuTitle = currentCategory.Name;
                savedCharactersCategoryMenu.MenuSubtitle = $"~s~Catégorie: ~y~{currentCategory.Name}";
                savedCharactersCategoryMenu.ClearMenuItems();

                var iconNames = Enum.GetNames(typeof(MenuItem.Icon)).ToList();

                string ChangeCallback(MenuDynamicListItem item, bool left)
                {
                    int currentIndex = iconNames.IndexOf(item.CurrentItem);
                    int newIndex = left ? currentIndex - 1 : currentIndex + 1;

                    // If going past the start or end of the list
                    if (iconNames.ElementAtOrDefault(newIndex) == default)
                    {
                        if (left)
                        {
                            newIndex = iconNames.Count - 1;
                        }
                        else
                        {
                            newIndex = 0;
                        }
                    }

                    item.RightIcon = (MenuItem.Icon)newIndex;

                    return iconNames[newIndex];
                }

                var renameBtn = new MenuItem("Renommer la catégorie", "Renommez cette catégorie.")
                {
                    Enabled = !isUncategorized
                };
                var descriptionBtn = new MenuItem("Modifier la description de la catégorie", "Modifiez la description de cette catégorie.")
                {
                    Enabled = !isUncategorized
                };
                var iconBtn = new MenuDynamicListItem("Changer l'icône de la catégorie", iconNames[(int)currentCategory.Icon], new MenuDynamicListItem.ChangeItemCallback(ChangeCallback), "Changez l'icône de cette catégorie. Sélectionnez pour sauvegarder.")
                {
                    Enabled = !isUncategorized,
                    RightIcon = currentCategory.Icon
                };
                var deleteBtn = new MenuItem("Supprimer la catégorie", "Supprimez cette catégorie. Cette action ne peut pas être annulée !")
                {
                    RightIcon = MenuItem.Icon.WARNING,
                    Enabled = !isUncategorized
                };
                var deleteCharsBtn = new MenuCheckboxItem("Supprimer tous les personnages", "Si coché, lorsque \"Supprimer la catégorie\" est pressé, tous les personnages sauvegardés dans cette catégorie seront également supprimés. Si non coché, les personnages sauvegardés seront déplacés vers \"Non classé\".")
                {
                    Enabled = !isUncategorized
                };

                savedCharactersCategoryMenu.AddMenuItem(renameBtn);
                savedCharactersCategoryMenu.AddMenuItem(descriptionBtn);
                savedCharactersCategoryMenu.AddMenuItem(iconBtn);
                savedCharactersCategoryMenu.AddMenuItem(deleteBtn);
                savedCharactersCategoryMenu.AddMenuItem(deleteCharsBtn);

                var spacer = GetSpacerMenuItem("↓ Personnages ↓");
                savedCharactersCategoryMenu.AddMenuItem(spacer);

                List<string> names = GetAllMpCharacterNames();

                if (names.Count > 0)
                {
                    var defaultChar = GetResourceKvpString("vmenu_default_character") ?? "";

                    names.Sort((a, b) => a.ToLower().CompareTo(b.ToLower()));
                    foreach (var name in names)
                    {
                        var tmpData = StorageManager.GetSavedMpCharacterData("mp_ped_" + name);

                        if (string.IsNullOrEmpty(tmpData.Category))
                        {
                            if (!isUncategorized)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (tmpData.Category != currentCategory.Name)
                            {
                                continue;
                            }
                        }

                        var btn = new MenuItem(name, "Cliquez sur ce bouton pour créer, modifier, cloner, renommer ou supprimer ce personnage sauvegardé.")
                        {
                            Label = "→→→",
                            LeftIcon = tmpData.IsMale ? MenuItem.Icon.MALE : MenuItem.Icon.FEMALE,
                            ItemData = tmpData.IsMale
                        };
                        if (defaultChar == "mp_ped_" + name)
                        {
                            btn.LeftIcon = MenuItem.Icon.TICK;
                            btn.Description += " ~g~Ce caractère est actuellement défini comme votre caractère par défaut et sera utilisé chaque (re)spawn.";
                        }
                        savedCharactersCategoryMenu.AddMenuItem(btn);
                        MenuController.BindMenuItem(savedCharactersCategoryMenu, manageSavedCharacterMenu, btn);
                    }
                }
            };

            savedCharactersCategoryMenu.OnIndexChange += async (menu, oldItem, newItem, oldIndex, newIndex) =>
            {
                if (!GetSettingsBool(Setting.vmenu_mp_ped_preview) || !MainMenu.MiscSettingsMenu.MPPedPreviews)
                {
                    return;
                }

                if (Entity.Exists(_clone))
                {
                    _clone.Delete();
                }

                // Only show preview for ped items, not menu items
                if (newItem.ItemData == null)
                {
                    return;
                }

                MultiplayerPedData character = StorageManager.GetSavedMpCharacterData(newItem.Text);

                if (!HasModelLoaded(character.ModelHash))
                {
                    RequestModel(character.ModelHash);
                    while (!HasModelLoaded(character.ModelHash))
                    {
                        await Delay(0);
                    }
                }

                ///
                /// Credit to whbl (https://forum.cfx.re/u/whbl) for the inspiration for this feature.
                /// https://forum.cfx.re/t/free-standalone-virtual-ped/5052458
                ///

                Ped playerPed = Game.PlayerPed;
                Vector3 clientPedPosition = playerPed.Position;

                _clone = new Ped(CreatePed(26, character.ModelHash, clientPedPosition.X, clientPedPosition.Y, clientPedPosition.Z - 3f, playerPed.Heading, false, false))
                {
                    IsCollisionEnabled = false,
                    IsInvincible = true,
                    BlockPermanentEvents = true,
                    IsPositionFrozen = true
                };

                int cloneHandle = _clone.Handle;

                await AppySavedDataToPed(character, cloneHandle);

                SetEntityCanBeDamaged(cloneHandle, false);
                SetPedAoBlobRendering(cloneHandle, false);

                while (Entity.Exists(_clone))
                {
                    Vector3 worldCoord = Vector3.Zero;
                    Vector3 normal = Vector3.Zero;

                    GetWorldCoordFromScreenCoord(0.6f, 0.8f, ref worldCoord, ref normal);

                    Vector3 cameraRotation = GameplayCamera.Rotation;

                    _clone.Position = worldCoord + (normal * 3.5f);
                    _clone.Rotation = new Vector3(cameraRotation.X * -1, 0f, cameraRotation.Z + 180);
                    _clone.Heading = cameraRotation.Z + 180;

                    GameplayCamera.ClampPitch(0f, 0f);

                    await Delay(0);
                }
            };


            savedCharactersCategoryMenu.OnItemSelect += async (sender, item, index) =>
            {
                switch (index)
                {
                    // Renommer la catégorie
                    case 0:
                        var name = await GetUserInput(windowTitle: "Saisir un nouveau nom de catégorie", defaultText: currentCategory.Name, maxInputLength: 30);

                        if (string.IsNullOrEmpty(name) || name.ToLower() == "uncategorized" || name.ToLower() == "create new")
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                            return;
                        }
                        else if (GetAllCategoryNames().Contains(name) || !string.IsNullOrEmpty(GetResourceKvpString("mp_character_category_" + name)))
                        {
                            Notify.Error(CommonErrors.SaveNameAlreadyExists);
                            return;
                        }

                        string oldName = currentCategory.Name;

                        currentCategory.Name = name;

                        if (StorageManager.SaveJsonData("mp_character_category_" + name, JsonConvert.SerializeObject(currentCategory), false))
                        {
                            StorageManager.DeleteSavedStorageItem("mp_character_category_" + oldName);

                            int totalCount = 0;
                            int updatedCount = 0;
                            List<string> characterNames = GetAllMpCharacterNames();

                            if (characterNames.Count > 0)
                            {
                                foreach (var characterName in characterNames)
                                {
                                    var tmpData = StorageManager.GetSavedMpCharacterData("mp_ped_" + characterName);

                                    if (string.IsNullOrEmpty(tmpData.Category))
                                    {
                                        continue;
                                    }

                                    if (tmpData.Category != oldName)
                                    {
                                        continue;
                                    }

                                    totalCount++;

                                    tmpData.Category = name;

                                    if (StorageManager.SaveJsonData(tmpData.SaveName, JsonConvert.SerializeObject(tmpData), true))
                                    {
                                        updatedCount++;
                                        Log($"Catégorie mise à jour pour \"{tmpData.SaveName}\"");
                                    }
                                    else
                                    {
                                        Log($"Un problème s'est produit lors de la mise à jour de la catégorie pour \"{tmpData.SaveName}\"");
                                    }
                                }
                            }

                            Notify.Success($"Votre catégorie a été renommée en ~g~<C>{name}</C>~s~. {updatedCount}/{totalCount} personnages mis à jour.");
                            MenuController.CloseAllMenus();
                            UpdateSavedPedsMenu();
                            savedCharactersMenu.OpenMenu();
                        }
                        else
                        {
                            Notify.Error("Un problème s'est produit lors du changement de nom de votre catégorie. Votre ancienne catégorie ne sera PAS supprimée pour cette raison.");
                        }
                        break;

                    // Change Category Description
                    case 1:
                        var description = await GetUserInput(windowTitle: "Saisir une nouvelle description de la catégorie", defaultText: currentCategory.Description, maxInputLength: 120);

                        currentCategory.Description = description;

                        if (StorageManager.SaveJsonData("mp_character_category_" + currentCategory.Name, JsonConvert.SerializeObject(currentCategory), true))
                        {
                            Notify.Success($"La description de votre catégorie a été modifiée.");
                            MenuController.CloseAllMenus();
                            UpdateSavedPedsMenu();
                            savedCharactersMenu.OpenMenu();
                        }
                        else
                        {
                            Notify.Error("Un problème s'est produit lors de la mise à jour de la catégorie pour");
                        }
                        break;

                    // Delete Category
                    case 3:
                        if (item.Label == "Êtes-vous sûr ?")
                        {
                            bool deletePeds = (sender.GetMenuItems().ElementAt(4) as MenuCheckboxItem).Checked;

                            item.Label = "";
                            DeleteResourceKvp("mp_character_category_" + currentCategory.Name);

                            int totalCount = 0;
                            int updatedCount = 0;

                            List<string> characterNames = GetAllMpCharacterNames();

                            if (characterNames.Count > 0)
                            {
                                foreach (var characterName in characterNames)
                                {
                                    var tmpData = StorageManager.GetSavedMpCharacterData("mp_ped_" + characterName);

                                    if (string.IsNullOrEmpty(tmpData.Category))
                                    {
                                        continue;
                                    }

                                    if (tmpData.Category != currentCategory.Name)
                                    {
                                        continue;
                                    }

                                    totalCount++;

                                    if (deletePeds)
                                    {
                                        updatedCount++;

                                        DeleteResourceKvp("mp_ped_" + tmpData.SaveName);
                                    }
                                    else
                                    {
                                        tmpData.Category = "Uncategorized";

                                        if (StorageManager.SaveJsonData(tmpData.SaveName, JsonConvert.SerializeObject(tmpData), true))
                                        {
                                            updatedCount++;
                                            Log($"Catégorie mise à jour pour \"{tmpData.SaveName}\"");
                                        }
                                        else
                                        {
                                            Log($"Un problème s'est produit lors de la mise à jour de la catégorie pour \"{tmpData.SaveName}\"");
                                        }
                                    }
                                }
                            }

                            Notify.Success($"Catégorie sauvegardée. {updatedCount}/{totalCount} personnages {(deletePeds ? "deleted" : "updated")}.");
                            MenuController.CloseAllMenus();
                            UpdateSavedPedsMenu();
                            savedCharactersMenu.OpenMenu();
                        }
                        else
                        {
                            item.Label = "Êtes-vous sûr ?";
                        }
                        break;

                    // Load saved character menu
                    default:
                        List<string> categoryNames = GetAllCategoryNames();
                        List<MenuItem.Icon> categoryIcons = GetCategoryIcons(categoryNames);
                        int nameIndex = categoryNames.IndexOf(currentCategory.Name);

                        setCategoryBtn.ItemData = categoryIcons;
                        setCategoryBtn.ListItems = categoryNames;
                        setCategoryBtn.ListIndex = nameIndex == 1 ? 0 : nameIndex;
                        setCategoryBtn.RightIcon = categoryIcons[setCategoryBtn.ListIndex];
                        selectedSavedCharacterManageName = item.Text;
                        manageSavedCharacterMenu.MenuSubtitle = item.Text;
                        manageSavedCharacterMenu.CounterPreText = $"{(item.LeftIcon == MenuItem.Icon.MALE ? "(Male)" : "(Female)")} ";
                        manageSavedCharacterMenu.RefreshIndex();
                        break;
                }
            };

            // Change Category Icon
            savedCharactersCategoryMenu.OnDynamicListItemSelect += (_, _, currentItem) =>
            {
                var iconNames = Enum.GetNames(typeof(MenuItem.Icon)).ToList();
                int iconIndex = iconNames.IndexOf(currentItem);

                currentCategory.Icon = (MenuItem.Icon)iconIndex;

                if (StorageManager.SaveJsonData("mp_character_category_" + currentCategory.Name, JsonConvert.SerializeObject(currentCategory), true))
                {
                    Notify.Success($"L'icône de votre catégorie a été changée en ~g~<C>{iconNames[iconIndex]}</C>~s~.");
                    UpdateSavedPedsMenu();
                }
                else
                {
                    Notify.Error("Un problème s'est produit lors de la modification de l'icône de Votre catégorie.");
                }
            };

            savedCharactersCategoryMenu.OnMenuClose += (_) =>
            {
                if (Entity.Exists(_clone))
                {
                    _clone.Delete();
                }
            };

        }

        /// <summary>
        /// Updates the saved peds menu.
        /// </summary>
        private void UpdateSavedPedsMenu()
        {
            var categories = GetAllCategoryNames();

            savedCharactersMenu.ClearMenuItems();

            var createCategoryBtn = new MenuItem("Créer une catégorie", "Créer une nouvelle catégorie de personnage.")
            {
                Label = "→→→"
            };
            savedCharactersMenu.AddMenuItem(createCategoryBtn);

            var spacer = GetSpacerMenuItem("↓ Catégories de personnage ↓");
            savedCharactersMenu.AddMenuItem(spacer);

            var uncategorized = new MpCharacterCategory
            {
                Name = "Uncategorized",
                Description = "Tous les caractères MP enregistrés qui n'ont pas été affectés à une catégorie."
            };
            var uncategorizedBtn = new MenuItem(uncategorized.Name, uncategorized.Description)
            {
                Label = "→→→",
                ItemData = uncategorized
            };
            savedCharactersMenu.AddMenuItem(uncategorizedBtn);
            MenuController.BindMenuItem(savedCharactersMenu, savedCharactersCategoryMenu, uncategorizedBtn);

            // Remove "Create New" and "Uncategorized"
            categories.RemoveRange(0, 2);

            if (categories.Count > 0)
            {
                categories.Sort((a, b) => a.ToLower().CompareTo(b.ToLower()));
                foreach (var item in categories)
                {
                    MpCharacterCategory category = StorageManager.GetSavedMpCharacterCategoryData("mp_character_category_" + item);

                    var btn = new MenuItem(category.Name, category.Description)
                    {
                        Label = "→→→",
                        LeftIcon = category.Icon,
                        ItemData = category
                    };
                    savedCharactersMenu.AddMenuItem(btn);
                    MenuController.BindMenuItem(savedCharactersMenu, savedCharactersCategoryMenu, btn);
                }
            }

            savedCharactersMenu.RefreshIndex();
        }

        private List<string> GetAllCategoryNames()
        {
            var categories = new List<string>();
            var handle = StartFindKvp("mp_character_category_");
            while (true)
            {
                var foundCategory = FindKvp(handle);
                if (string.IsNullOrEmpty(foundCategory))
                {
                    break;
                }
                else
                {
                    categories.Add(foundCategory.Substring(22));
                }
            }
            EndFindKvp(handle);

            categories.Insert(0, "Create New");
            categories.Insert(1, "Uncategorized");

            return categories;
        }

        private List<MenuItem.Icon> GetCategoryIcons(List<string> categoryNames)
        {
            List<MenuItem.Icon> icons = new List<MenuItem.Icon> { };

            foreach (var name in categoryNames)
            {
                icons.Add(StorageManager.GetSavedMpCharacterCategoryData("mp_character_category_" + name).Icon);
            }

            return icons;
        }

        private List<string> GetAllMpCharacterNames()
        {
            var names = new List<string>();
            var handle = StartFindKvp("mp_ped_");
            while (true)
            {
                var foundName = FindKvp(handle);
                if (string.IsNullOrEmpty(foundName))
                {
                    break;
                }
                else
                {
                    names.Add(foundName.Substring(7));
                }
            }
            EndFindKvp(handle);

            return names;
        }

        internal void SetHeadBlend()
        {
            SetPedHeadBlendData(Game.PlayerPed.Handle, _dadSelection, _mumSelection, 0, _dadSelection, _mumSelection, 0, _shapeMixValue, _skinMixValue, 0f, false);
        }

        internal void ChangePlayerHair(int newHairIndex)
        {
            ClearPedFacialDecorations(Game.PlayerPed.Handle);
            currentCharacter.PedAppearance.HairOverlay = new KeyValuePair<string, string>("", "");

            if (newHairIndex >= GetNumberOfPedDrawableVariations(Game.PlayerPed.Handle, 2))
            {
                SetPedComponentVariation(Game.PlayerPed.Handle, 2, 0, 0, 0);
                currentCharacter.PedAppearance.hairStyle = 0;
            }
            else
            {
                SetPedComponentVariation(Game.PlayerPed.Handle, 2, newHairIndex, 0, 0);
                currentCharacter.PedAppearance.hairStyle = newHairIndex;
                if (hairOverlays.ContainsKey(newHairIndex))
                {
                    SetPedFacialDecoration(Game.PlayerPed.Handle, (uint)GetHashKey(hairOverlays[newHairIndex].Key), (uint)GetHashKey(hairOverlays[newHairIndex].Value));
                    currentCharacter.PedAppearance.HairOverlay = new KeyValuePair<string, string>(hairOverlays[newHairIndex].Key, hairOverlays[newHairIndex].Value);
                }
            }

            _hairSelection = newHairIndex;
        }

        internal void ChangePlayerHairColor(int color, int highlight)
        {
            SetPedHairColor(Game.PlayerPed.Handle, color, highlight);

            currentCharacter.PedAppearance.hairColor = color;
            currentCharacter.PedAppearance.hairHighlightColor = highlight;

            _hairColorSelection = color;
            _hairHighlightColorSelection = highlight;
        }

        internal void ChangePlayerEyeColor(int color)
        {
            SetPedEyeColor(Game.PlayerPed.Handle, color);

            currentCharacter.PedAppearance.eyeColor = color;

            _eyeColorSelection = color;
        }

        internal void SetPlayerClothing()
        {
            SetPedComponentVariation(Game.PlayerPed.Handle, 3, 15, 0, 0);

            currentCharacter.DrawableVariations.clothes[3] = new KeyValuePair<int, int>(15, 0);

            if (currentCharacter.IsMale)
            {
                SetPedComponentVariation(Game.PlayerPed.Handle, 8, 15, 0, 0);

                currentCharacter.DrawableVariations.clothes[8] = new KeyValuePair<int, int>(15, 0);

                SetPedComponentVariation(Game.PlayerPed.Handle, 11, 15, 0, 0);

                currentCharacter.DrawableVariations.clothes[11] = new KeyValuePair<int, int>(15, 0);

                int pantsColor = _random.Next(GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, 4, 61));

                SetPedComponentVariation(Game.PlayerPed.Handle, 4, 61, pantsColor, 0);

                currentCharacter.DrawableVariations.clothes[4] = new KeyValuePair<int, int>(61, pantsColor);

                SetPedComponentVariation(Game.PlayerPed.Handle, 6, 34, 0, 0);

                currentCharacter.DrawableVariations.clothes[6] = new KeyValuePair<int, int>(34, 0);
            }
            else
            {
                SetPedComponentVariation(Game.PlayerPed.Handle, 8, 14, 0, 0);
                SetPedComponentVariation(Game.PlayerPed.Handle, 8, 14, 0, 0);

                currentCharacter.DrawableVariations.clothes[8] = new KeyValuePair<int, int>(14, 0);

                int braColor = _random.Next(GetNumberOfPedTextureVariations(Game.PlayerPed.Handle, 4, 17));

                SetPedComponentVariation(Game.PlayerPed.Handle, 4, 17, braColor, 0);

                currentCharacter.DrawableVariations.clothes[4] = new KeyValuePair<int, int>(17, braColor);

                SetPedComponentVariation(Game.PlayerPed.Handle, 11, 18, braColor, 0);

                currentCharacter.DrawableVariations.clothes[11] = new KeyValuePair<int, int>(18, braColor);

                SetPedComponentVariation(Game.PlayerPed.Handle, 6, 35, 0, 0);

                currentCharacter.DrawableVariations.clothes[6] = new KeyValuePair<int, int>(35, 0);
            }
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

        internal async Task AppySavedDataToPed(MultiplayerPedData character, int pedHandle)
        {
            #region headblend
            PedHeadBlendData data = character.PedHeadBlendData;
            SetPedHeadBlendData(pedHandle, data.FirstFaceShape, data.SecondFaceShape, data.ThirdFaceShape, data.FirstSkinTone, data.SecondSkinTone, data.ThirdSkinTone, data.ParentFaceShapePercent, data.ParentSkinTonePercent, 0f, data.IsParentInheritance);

            while (!HasPedHeadBlendFinished(pedHandle))
            {
                await Delay(0);
            }
            #endregion

            #region appearance
            PedAppearance appData = character.PedAppearance;
            // hair
            SetPedComponentVariation(pedHandle, 2, appData.hairStyle, 0, 0);
            SetPedHairColor(pedHandle, appData.hairColor, appData.hairHighlightColor);
            if (!string.IsNullOrEmpty(appData.HairOverlay.Key) && !string.IsNullOrEmpty(appData.HairOverlay.Value))
            {
                SetPedFacialDecoration(pedHandle, (uint)GetHashKey(appData.HairOverlay.Key), (uint)GetHashKey(appData.HairOverlay.Value));
            }
            // blemishes
            SetPedHeadOverlay(pedHandle, 0, appData.blemishesStyle, appData.blemishesOpacity);
            // bread
            SetPedHeadOverlay(pedHandle, 1, appData.beardStyle, appData.beardOpacity);
            SetPedHeadOverlayColor(pedHandle, 1, 1, appData.beardColor, appData.beardColor);
            // eyebrows
            SetPedHeadOverlay(pedHandle, 2, appData.eyebrowsStyle, appData.eyebrowsOpacity);
            SetPedHeadOverlayColor(pedHandle, 2, 1, appData.eyebrowsColor, appData.eyebrowsColor);
            // ageing
            SetPedHeadOverlay(pedHandle, 3, appData.ageingStyle, appData.ageingOpacity);
            // makeup
            SetPedHeadOverlay(pedHandle, 4, appData.makeupStyle, appData.makeupOpacity);
            SetPedHeadOverlayColor(pedHandle, 4, 2, appData.makeupColor, appData.makeupColor);
            // blush
            SetPedHeadOverlay(pedHandle, 5, appData.blushStyle, appData.blushOpacity);
            SetPedHeadOverlayColor(pedHandle, 5, 2, appData.blushColor, appData.blushColor);
            // complexion
            SetPedHeadOverlay(pedHandle, 6, appData.complexionStyle, appData.complexionOpacity);
            // sundamage
            SetPedHeadOverlay(pedHandle, 7, appData.sunDamageStyle, appData.sunDamageOpacity);
            // lipstick
            SetPedHeadOverlay(pedHandle, 8, appData.lipstickStyle, appData.lipstickOpacity);
            SetPedHeadOverlayColor(pedHandle, 8, 2, appData.lipstickColor, appData.lipstickColor);
            // moles and freckles
            SetPedHeadOverlay(pedHandle, 9, appData.molesFrecklesStyle, appData.molesFrecklesOpacity);
            // chest hair 
            SetPedHeadOverlay(pedHandle, 10, appData.chestHairStyle, appData.chestHairOpacity);
            SetPedHeadOverlayColor(pedHandle, 10, 1, appData.chestHairColor, appData.chestHairColor);
            // body blemishes 
            SetPedHeadOverlay(pedHandle, 11, appData.bodyBlemishesStyle, appData.bodyBlemishesOpacity);
            // eyecolor
            SetPedEyeColor(pedHandle, appData.eyeColor);
            #endregion

            #region Face Shape Data
            for (var i = 0; i < 19; i++)
            {
                SetPedFaceFeature(pedHandle, i, 0f);
            }

            if (character.FaceShapeFeatures.features != null)
            {
                foreach (var t in character.FaceShapeFeatures.features)
                {
                    SetPedFaceFeature(pedHandle, t.Key, t.Value);
                }
            }
            else
            {
                character.FaceShapeFeatures.features = new Dictionary<int, float>();
            }

            #endregion

            #region Clothing Data
            if (character.DrawableVariations.clothes != null && character.DrawableVariations.clothes.Count > 0)
            {
                foreach (var cd in character.DrawableVariations.clothes)
                {
                    SetPedComponentVariation(pedHandle, cd.Key, cd.Value.Key, cd.Value.Value, 0);
                }
            }
            #endregion

            #region Props Data
            if (character.PropVariations.props != null && character.PropVariations.props.Count > 0)
            {
                foreach (var cd in character.PropVariations.props)
                {
                    if (cd.Value.Key > -1)
                    {
                        int textureIndex = cd.Value.Value > -1 ? cd.Value.Value : 0;
                        SetPedPropIndex(pedHandle, cd.Key, cd.Value.Key, textureIndex, true);
                    }
                }
            }
            #endregion

            #region Tattoos

            if (character.PedTatttoos.HeadTattoos == null)
            {
                character.PedTatttoos.HeadTattoos = new List<KeyValuePair<string, string>>();
            }
            if (character.PedTatttoos.TorsoTattoos == null)
            {
                character.PedTatttoos.TorsoTattoos = new List<KeyValuePair<string, string>>();
            }
            if (character.PedTatttoos.LeftArmTattoos == null)
            {
                character.PedTatttoos.LeftArmTattoos = new List<KeyValuePair<string, string>>();
            }
            if (character.PedTatttoos.RightArmTattoos == null)
            {
                character.PedTatttoos.RightArmTattoos = new List<KeyValuePair<string, string>>();
            }
            if (character.PedTatttoos.LeftLegTattoos == null)
            {
                character.PedTatttoos.LeftLegTattoos = new List<KeyValuePair<string, string>>();
            }
            if (character.PedTatttoos.RightLegTattoos == null)
            {
                character.PedTatttoos.RightLegTattoos = new List<KeyValuePair<string, string>>();
            }
            if (character.PedTatttoos.BadgeTattoos == null)
            {
                character.PedTatttoos.BadgeTattoos = new List<KeyValuePair<string, string>>();
            }

            foreach (var tattoo in character.PedTatttoos.HeadTattoos)
            {
                SetPedDecoration(pedHandle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
            }
            foreach (var tattoo in character.PedTatttoos.TorsoTattoos)
            {
                SetPedDecoration(pedHandle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
            }
            foreach (var tattoo in character.PedTatttoos.LeftArmTattoos)
            {
                SetPedDecoration(pedHandle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
            }
            foreach (var tattoo in character.PedTatttoos.RightArmTattoos)
            {
                SetPedDecoration(pedHandle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
            }
            foreach (var tattoo in character.PedTatttoos.LeftLegTattoos)
            {
                SetPedDecoration(pedHandle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
            }
            foreach (var tattoo in character.PedTatttoos.RightLegTattoos)
            {
                SetPedDecoration(pedHandle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
            }
            foreach (var tattoo in character.PedTatttoos.BadgeTattoos)
            {
                SetPedDecoration(pedHandle, (uint)GetHashKey(tattoo.Key), (uint)GetHashKey(tattoo.Value));
            }
            #endregion
        }

        public struct MpCharacterCategory
        {
            public string Name;
            public string Description;
            public MenuItem.Icon Icon;
        }
    }
}
