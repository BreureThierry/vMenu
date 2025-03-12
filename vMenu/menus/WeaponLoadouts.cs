using System.Collections.Generic;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class WeaponLoadouts
    {
        // Variables
        private Menu menu = null;
        private readonly Menu SavedLoadoutsMenu = new("Chargements enregistrés", "Liste des chargements d'armes enregistrés");
        private readonly Menu ManageLoadoutMenu = new("Gérer le chargement", "Gérer le chargement d'armes enregistré");

        public bool WeaponLoadoutsSetLoadoutOnRespawn { get; private set; } = UserDefaults.WeaponLoadoutsSetLoadoutOnRespawn;

        private readonly Dictionary<string, List<ValidWeapon>> SavedWeapons = new();

        public static Dictionary<string, List<ValidWeapon>> GetSavedWeapons()
        {
            var handle = StartFindKvp("vmenu_string_saved_weapon_loadout_");
            var saves = new Dictionary<string, List<ValidWeapon>>();
            while (true)
            {
                var kvp = FindKvp(handle);
                if (string.IsNullOrEmpty(kvp))
                {
                    break;
                }
                saves.Add(kvp, JsonConvert.DeserializeObject<List<ValidWeapon>>(GetResourceKvpString(kvp)));
            }
            EndFindKvp(handle);
            return saves;
        }

        private string SelectedSavedLoadoutName { get; set; } = "";
        // vmenu_temp_weapons_loadout_before_respawn
        // vmenu_string_saved_weapon_loadout_

        /// <summary>
        /// Returns the saved weapons list, as well as sets the <see cref="SavedWeapons"/> variable.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, List<ValidWeapon>> RefreshSavedWeaponsList()
        {
            if (SavedWeapons.Count > 0)
            {
                SavedWeapons.Clear();
            }

            var handle = StartFindKvp("vmenu_string_saved_weapon_loadout_");
            var saves = new List<string>();
            while (true)
            {
                var kvp = FindKvp(handle);
                if (string.IsNullOrEmpty(kvp))
                {
                    break;
                }
                saves.Add(kvp);
            }
            EndFindKvp(handle);

            foreach (var save in saves)
            {
                SavedWeapons.Add(save, JsonConvert.DeserializeObject<List<ValidWeapon>>(GetResourceKvpString(save)));
            }

            return SavedWeapons;
        }

        /// <summary>
        /// Creates the menu if it doesn't exist yet and sets the event handlers.
        /// </summary>
        public void CreateMenu()
        {
            menu = new Menu(Game.Player.Name, "weapon loadouts management");

            MenuController.AddSubmenu(menu, SavedLoadoutsMenu);
            MenuController.AddSubmenu(SavedLoadoutsMenu, ManageLoadoutMenu);

            var saveLoadout = new MenuItem("Enregistrer le chargement", "Enregistrez vos armes actuelles dans un nouveau slot de chargement.");
            var savedLoadoutsMenuBtn = new MenuItem("Gérer les chargements", "Gérer les chargements d'armes enregistrés.") { Label = "→→→" };
            var enableDefaultLoadouts = new MenuCheckboxItem("Restaurer le chargement par défaut à la réapparition", "Si vous avez défini un chargement comme chargement par défaut, alors votre chargement sera automatiquement équipé chaque fois que vous (re)apparaissez.", WeaponLoadoutsSetLoadoutOnRespawn);

            menu.AddMenuItem(saveLoadout);
            menu.AddMenuItem(savedLoadoutsMenuBtn);
            MenuController.BindMenuItem(menu, SavedLoadoutsMenu, savedLoadoutsMenuBtn);
            if (IsAllowed(Permission.WLEquipOnRespawn))
            {
                menu.AddMenuItem(enableDefaultLoadouts);

                menu.OnCheckboxChange += (sender, checkbox, index, _checked) =>
                {
                    WeaponLoadoutsSetLoadoutOnRespawn = _checked;
                };
            }


            void RefreshSavedWeaponsMenu()
            {
                var oldCount = SavedLoadoutsMenu.Size;
                SavedLoadoutsMenu.ClearMenuItems(true);

                RefreshSavedWeaponsList();

                foreach (var sw in SavedWeapons)
                {
                    var btn = new MenuItem(sw.Key.Replace("vmenu_string_saved_weapon_loadout_", ""), "Click to manage this loadout.") { Label = "→→→" };
                    SavedLoadoutsMenu.AddMenuItem(btn);
                    MenuController.BindMenuItem(SavedLoadoutsMenu, ManageLoadoutMenu, btn);
                }

                if (oldCount > SavedWeapons.Count)
                {
                    SavedLoadoutsMenu.RefreshIndex();
                }
            }


            var spawnLoadout = new MenuItem("Équiper le chargement", "Invoque ce chargement d'armes enregistré. Cela supprimera toutes vos armes actuelles et les remplacera par ce slot enregistré.");
            var renameLoadout = new MenuItem("Renommer le chargement", "Renomme ce chargement enregistré.");
            var cloneLoadout = new MenuItem("Cloner le chargement", "Clone ce chargement enregistré dans un nouveau slot.");
            var setDefaultLoadout = new MenuItem("Définir comme chargement par défaut", "Définissez ce chargement comme étant votre chargement par défaut chaque fois que vous (re)apparaissez. Cela annulera l'option 'Restaurer les armes' dans le menu Paramètres généraux. Vous pouvez activer cette option dans le menu principal des chargements d'armes.");
            var replaceLoadout = new MenuItem("~r~Remplacer le chargement", "~r~Cela remplace ce slot enregistré avec les armes que vous avez actuellement dans votre inventaire. Cette action ne peut pas être annulée !");
            var deleteLoadout = new MenuItem("~r~Supprimer le chargement", "~r~Cela supprimera ce chargement enregistré. Cette action ne peut pas être annulée !");

            if (IsAllowed(Permission.WLEquip))
            {
                ManageLoadoutMenu.AddMenuItem(spawnLoadout);
            }

            ManageLoadoutMenu.AddMenuItem(renameLoadout);
            ManageLoadoutMenu.AddMenuItem(cloneLoadout);
            ManageLoadoutMenu.AddMenuItem(setDefaultLoadout);
            ManageLoadoutMenu.AddMenuItem(replaceLoadout);
            ManageLoadoutMenu.AddMenuItem(deleteLoadout);

            // Save the weapons loadout.
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == saveLoadout)
                {
                    var name = await GetUserInput("Enter a save name", 30);
                    if (string.IsNullOrEmpty(name))
                    {
                        Notify.Error(CommonErrors.InvalidInput);
                    }
                    else
                    {
                        if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + name))
                        {
                            Notify.Error(CommonErrors.SaveNameAlreadyExists);
                        }
                        else
                        {
                            if (SaveWeaponLoadout("vmenu_string_saved_weapon_loadout_" + name))
                            {
                                Log("saveweapons called from menu select (save loadout button)");
                                Notify.Success($"Your weapons have been saved as ~g~<C>{name}</C>~s~.");
                            }
                            else
                            {
                                Notify.Error(CommonErrors.UnknownError);
                            }
                        }
                    }
                }
            };

            // manage spawning, renaming, deleting etc.
            ManageLoadoutMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (SavedWeapons.ContainsKey(SelectedSavedLoadoutName))
                {
                    var weapons = SavedWeapons[SelectedSavedLoadoutName];

                    if (item == spawnLoadout) // spawn
                    {
                        await SpawnWeaponLoadoutAsync(SelectedSavedLoadoutName, false, true, false);
                    }
                    else if (item == renameLoadout || item == cloneLoadout) // rename or clone
                    {
                        var newName = await GetUserInput("Enter a save name", SelectedSavedLoadoutName.Replace("vmenu_string_saved_weapon_loadout_", ""), 30);
                        if (string.IsNullOrEmpty(newName))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                        }
                        else
                        {
                            if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + newName))
                            {
                                Notify.Error(CommonErrors.SaveNameAlreadyExists);
                            }
                            else
                            {
                                SetResourceKvp("vmenu_string_saved_weapon_loadout_" + newName, JsonConvert.SerializeObject(weapons));
                                Notify.Success($"Your weapons loadout has been {(item == renameLoadout ? "renamed" : "cloned")} to ~g~<C>{newName}</C>~s~.");

                                if (item == renameLoadout)
                                {
                                    DeleteResourceKvp(SelectedSavedLoadoutName);
                                }

                                ManageLoadoutMenu.GoBack();
                            }
                        }
                    }
                    else if (item == setDefaultLoadout) // set as default
                    {
                        SetResourceKvp("vmenu_string_default_loadout", SelectedSavedLoadoutName);
                        Notify.Success("This is now your default loadout.");
                        item.LeftIcon = MenuItem.Icon.TICK;
                    }
                    else if (item == replaceLoadout) // replace
                    {
                        if (replaceLoadout.Label == "Are you sure?")
                        {
                            replaceLoadout.Label = "";
                            SaveWeaponLoadout(SelectedSavedLoadoutName);
                            Log("save weapons called from replace loadout");
                            Notify.Success("Your saved loadout has been replaced with your current weapons.");
                        }
                        else
                        {
                            replaceLoadout.Label = "Are you sure?";
                        }
                    }
                    else if (item == deleteLoadout) // delete
                    {
                        if (deleteLoadout.Label == "Are you sure?")
                        {
                            deleteLoadout.Label = "";
                            DeleteResourceKvp(SelectedSavedLoadoutName);
                            ManageLoadoutMenu.GoBack();
                            Notify.Success("Your saved loadout has been deleted.");
                        }
                        else
                        {
                            deleteLoadout.Label = "Are you sure?";
                        }
                    }
                }
            };

            // Reset the 'are you sure' states.
            ManageLoadoutMenu.OnMenuClose += (sender) =>
            {
                deleteLoadout.Label = "";
                renameLoadout.Label = "";
            };
            // Reset the 'are you sure' states.
            ManageLoadoutMenu.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) =>
            {
                deleteLoadout.Label = "";
                renameLoadout.Label = "";
            };

            // Refresh the spawned weapons menu whenever this menu is opened.
            SavedLoadoutsMenu.OnMenuOpen += (sender) =>
            {
                RefreshSavedWeaponsMenu();
            };

            // Set the current saved loadout whenever a loadout is selected.
            SavedLoadoutsMenu.OnItemSelect += (sender, item, index) =>
            {
                if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + item.Text))
                {
                    SelectedSavedLoadoutName = "vmenu_string_saved_weapon_loadout_" + item.Text;
                }
                else // shouldn't ever happen, but just in case
                {
                    ManageLoadoutMenu.GoBack();
                }
            };

            // Reset the index whenever the ManageLoadout menu is opened. Just to prevent auto selecting the delete option for example.
            ManageLoadoutMenu.OnMenuOpen += (sender) =>
            {
                ManageLoadoutMenu.RefreshIndex();
                var kvp = GetResourceKvpString("vmenu_string_default_loadout");
                if (string.IsNullOrEmpty(kvp) || kvp != SelectedSavedLoadoutName)
                {
                    setDefaultLoadout.LeftIcon = MenuItem.Icon.NONE;
                }
                else
                {
                    setDefaultLoadout.LeftIcon = MenuItem.Icon.TICK;
                }

            };

            // Refresh the saved weapons menu.
            RefreshSavedWeaponsMenu();
        }

        /// <summary>
        /// Gets the menu.
        /// </summary>
        /// <returns></returns>
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
