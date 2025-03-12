using System.Collections.Generic;
using System.Linq;

using CitizenFX.Core;

using MenuAPI;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class PersonalVehicle
    {
        // Variables
        private Menu menu;
        public bool EnableVehicleBlip { get; private set; } = UserDefaults.PVEnableVehicleBlip;

        // Empty constructor
        public PersonalVehicle() { }

        public Vehicle CurrentPersonalVehicle { get; internal set; } = null;

        public Menu VehicleDoorsMenu { get; internal set; } = null;


        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            // Menu
            menu = new Menu(GetSafePlayerName(Game.Player.Name), "Options du véhicule personnel");

            // Éléments du menu
            var setVehice = new MenuItem("Définir le véhicule", "Définit votre véhicule actuel comme votre véhicule personnel. Si vous avez déjà un véhicule personnel défini, cela remplacera votre sélection.") { Label = "Véhicule actuel : Aucun" };
            var toggleEngine = new MenuItem("Activer/Désactiver le moteur", "Active ou désactive le moteur, même lorsque vous n'êtes pas à l'intérieur du véhicule. Cela ne fonctionne pas si quelqu'un d'autre utilise actuellement votre véhicule.");
            var toggleLights = new MenuListItem("Contrôle des phares", new List<string>() { "Forcer l'allumage", "Forcer l'extinction", "Réinitialiser" }, 0, "Cela activera ou désactivera les phares de votre véhicule. Le moteur de votre véhicule doit être en marche pour que cela fonctionne.");
            var toggleStance = new MenuListItem("Position du véhicule", new List<string>() { "Par défaut", "Abaissé" }, 0, "Sélectionnez la position de votre véhicule personnel.");
            var kickAllPassengers = new MenuItem("Expulser les passagers", "Cela supprimera tous les passagers de votre véhicule personnel.");
            // MenuItem
            var lockDoors = new MenuItem("Verrouiller les portes du véhicule", "Cela verrouillera toutes les portes de votre véhicule pour tous les joueurs. Toute personne déjà à l'intérieur pourra toujours quitter le véhicule, même si les portes sont verrouillées.");
            var unlockDoors = new MenuItem("Déverrouiller les portes du véhicule", "Cela déverrouillera toutes les portes de votre véhicule pour tous les joueurs.");
            var doorsMenuBtn = new MenuItem("Portes du véhicule", "Ouvrez, fermez, retirez et restaurez les portes du véhicule ici.")
            {
                Label = "→→→"
            };
            var soundHorn = new MenuItem("Klaxonner", "Fait sonner le klaxon du véhicule.");
            var toggleAlarm = new MenuItem("Activer/Désactiver l'alarme", "Active ou désactive le son de l'alarme du véhicule. Cela ne configure pas une alarme. Cela ne fait que basculer l'état actuel du son de l'alarme.");
            var enableBlip = new MenuCheckboxItem("Ajouter un blip pour le véhicule personnel", "Active ou désactive le blip qui est ajouté lorsque vous marquez un véhicule comme votre véhicule personnel.", EnableVehicleBlip) { Style = MenuCheckboxItem.CheckboxStyle.Cross };
            var exclusiveDriver = new MenuCheckboxItem("Conducteur exclusif", "Si activé, vous serez le seul à pouvoir entrer dans le siège du conducteur. Les autres joueurs ne pourront pas conduire la voiture. Ils pourront toujours être passagers.", false) { Style = MenuCheckboxItem.CheckboxStyle.Cross };
            // Sous-menu
            VehicleDoorsMenu = new Menu("Portes du véhicule", "Gestion des portes du véhicule");
            MenuController.AddSubmenu(menu, VehicleDoorsMenu);
            MenuController.BindMenuItem(menu, VehicleDoorsMenu, doorsMenuBtn);

            // This is always allowed if this submenu is created/allowed.
            menu.AddMenuItem(setVehice);

            // Add conditional features.

            // Toggle engine.
            if (IsAllowed(Permission.PVToggleEngine))
            {
                menu.AddMenuItem(toggleEngine);
            }

            // Toggle lights
            if (IsAllowed(Permission.PVToggleLights))
            {
                menu.AddMenuItem(toggleLights);
            }

            // Toggle stance
            if (IsAllowed(Permission.PVToggleStance))
            {
                menu.AddMenuItem(toggleStance);
            }

            // Kick vehicle passengers
            if (IsAllowed(Permission.PVKickPassengers))
            {
                menu.AddMenuItem(kickAllPassengers);
            }

            // Lock and unlock vehicle doors
            if (IsAllowed(Permission.PVLockDoors))
            {
                menu.AddMenuItem(lockDoors);
                menu.AddMenuItem(unlockDoors);
            }

            if (IsAllowed(Permission.PVDoors))
            {
                menu.AddMenuItem(doorsMenuBtn);
            }

            // Sound horn
            if (IsAllowed(Permission.PVSoundHorn))
            {
                menu.AddMenuItem(soundHorn);
            }

            // Toggle alarm sound
            if (IsAllowed(Permission.PVToggleAlarm))
            {
                menu.AddMenuItem(toggleAlarm);
            }

            // Enable blip for personal vehicle
            if (IsAllowed(Permission.PVAddBlip))
            {
                menu.AddMenuItem(enableBlip);
            }

            if (IsAllowed(Permission.PVExclusiveDriver))
            {
                menu.AddMenuItem(exclusiveDriver);
            }


            // Handle list presses
            menu.OnListItemSelect += (sender, item, itemIndex, index) =>
            {
                var veh = CurrentPersonalVehicle;
                if (veh != null && veh.Exists())
                {
                    if (!NetworkHasControlOfEntity(CurrentPersonalVehicle.Handle))
                    {
                        if (!NetworkRequestControlOfEntity(CurrentPersonalVehicle.Handle))
                        {
                            Notify.Error("Vous ne pouvez actuellement pas contrôler ce véhicule. Quelqu'un d'autre conduit-il actuellement votre véhicule ? Veuillez réessayer après vous être assuré que d'autres joueurs ne contrôlent pas votre véhicule.");
                            return;
                        }
                    }

                    if (item == toggleLights)
                    {
                        PressKeyFob(CurrentPersonalVehicle);
                        if (itemIndex == 0)
                        {
                            SetVehicleLights(CurrentPersonalVehicle.Handle, 3);
                        }
                        else if (itemIndex == 1)
                        {
                            SetVehicleLights(CurrentPersonalVehicle.Handle, 1);
                        }
                        else
                        {
                            SetVehicleLights(CurrentPersonalVehicle.Handle, 0);
                        }
                    }
                    else if (item == toggleStance)
                    {
                        PressKeyFob(CurrentPersonalVehicle);
                        if (itemIndex == 0)
                        {
                            SetReduceDriftVehicleSuspension(CurrentPersonalVehicle.Handle, false);
                        }
                        else if (itemIndex == 1)
                        {
                            SetReduceDriftVehicleSuspension(CurrentPersonalVehicle.Handle, true);
                        }
                    }

                }
                else
                {
                    Notify.Error("Vous n'avez pas encore sélectionné de véhicule personnel ou votre véhicule a été supprimé. Définissez un véhicule personnel avant de pouvoir utiliser ces options.");
                }
            };

            // Handle checkbox changes
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == enableBlip)
                {
                    EnableVehicleBlip = _checked;
                    if (EnableVehicleBlip)
                    {
                        if (CurrentPersonalVehicle != null && CurrentPersonalVehicle.Exists())
                        {
                            if (CurrentPersonalVehicle.AttachedBlip == null || !CurrentPersonalVehicle.AttachedBlip.Exists())
                            {
                                CurrentPersonalVehicle.AttachBlip();
                            }
                            CurrentPersonalVehicle.AttachedBlip.Sprite = BlipSprite.PersonalVehicleCar;
                            CurrentPersonalVehicle.AttachedBlip.Name = "Véhicule personnel";
                        }
                        else
                        {
                            Notify.Error("Vous n'avez pas encore sélectionné de véhicule personnel ou votre véhicule a été supprimé. Définissez un véhicule personnel avant de pouvoir utiliser ces options.");
                        }

                    }
                    else
                    {
                        if (CurrentPersonalVehicle != null && CurrentPersonalVehicle.Exists() && CurrentPersonalVehicle.AttachedBlip != null && CurrentPersonalVehicle.AttachedBlip.Exists())
                        {
                            CurrentPersonalVehicle.AttachedBlip.Delete();
                        }
                    }
                }
                else if (item == exclusiveDriver)
                {
                    if (CurrentPersonalVehicle != null && CurrentPersonalVehicle.Exists())
                    {
                        if (NetworkRequestControlOfEntity(CurrentPersonalVehicle.Handle))
                        {
                            if (_checked)
                            {
                                // SetVehicleExclusiveDriver, but the current version is broken in C# so we manually execute it.
                                CitizenFX.Core.Native.Function.Call((CitizenFX.Core.Native.Hash)0x41062318F23ED854, CurrentPersonalVehicle, true);
                                SetVehicleExclusiveDriver_2(CurrentPersonalVehicle.Handle, Game.PlayerPed.Handle, 1);
                            }
                            else
                            {
                                // SetVehicleExclusiveDriver, but the current version is broken in C# so we manually execute it.
                                CitizenFX.Core.Native.Function.Call((CitizenFX.Core.Native.Hash)0x41062318F23ED854, CurrentPersonalVehicle, false);
                                SetVehicleExclusiveDriver_2(CurrentPersonalVehicle.Handle, 0, 1);
                            }
                        }
                        else
                        {
                            item.Checked = !_checked;
                            Notify.Error("Vous ne pouvez actuellement pas contrôler ce véhicule. Quelqu'un d'autre conduit-il actuellement votre véhicule ? Veuillez réessayer après vous être assuré que d'autres joueurs ne contrôlent pas votre véhicule.");
                        }
                    }
                }
            };

            // Handle button presses.
            menu.OnItemSelect += (sender, item, index) =>
            {
                if (item == setVehice)
                {
                    if (Game.PlayerPed.IsInVehicle())
                    {
                        var veh = GetVehicle();
                        if (veh != null && veh.Exists())
                        {
                            if (Game.PlayerPed == veh.Driver)
                            {
                                CurrentPersonalVehicle = veh;
                                veh.PreviouslyOwnedByPlayer = true;
                                veh.IsPersistent = true;
                                if (EnableVehicleBlip && IsAllowed(Permission.PVAddBlip))
                                {
                                    if (veh.AttachedBlip == null || !veh.AttachedBlip.Exists())
                                    {
                                        veh.AttachBlip();
                                    }
                                    veh.AttachedBlip.Sprite = BlipSprite.PersonalVehicleCar;
                                    veh.AttachedBlip.Name = "Véhicule personnel";
                                }
                                var name = GetLabelText(veh.DisplayName);
                                if (string.IsNullOrEmpty(name) || name.ToLower() == "null")
                                {
                                    name = veh.DisplayName;
                                }
                                item.Label = $"Véhicule actuel : {name}";
                            }
                            else
                            {
                                Notify.Error(CommonErrors.NeedToBeTheDriver);
                            }
                        }
                        else
                        {
                            Notify.Error(CommonErrors.NoVehicle);
                        }
                    }
                    else
                    {
                        Notify.Error(CommonErrors.NoVehicle);
                    }
                }
                else if (CurrentPersonalVehicle != null && CurrentPersonalVehicle.Exists())
                {
                    if (item == kickAllPassengers)
                    {
                        if (CurrentPersonalVehicle.Occupants.Count() > 0 && CurrentPersonalVehicle.Occupants.Any(p => p != Game.PlayerPed))
                        {
                            var netId = VehToNet(CurrentPersonalVehicle.Handle);
                            TriggerServerEvent("vMenu:GetOutOfCar", netId, Game.Player.ServerId);
                        }
                        else
                        {
                            Notify.Info("Il n'y a pas d'autres joueurs dans votre véhicule qui doivent être expulsés.");
                        }
                    }
                    else
                    {
                        if (!NetworkHasControlOfEntity(CurrentPersonalVehicle.Handle))
                        {
                            if (!NetworkRequestControlOfEntity(CurrentPersonalVehicle.Handle))
                            {
                                Notify.Error("Vous ne pouvez actuellement pas contrôler ce véhicule. Quelqu'un d'autre conduit-il actuellement votre véhicule ? Veuillez réessayer après vous être assuré que d'autres joueurs ne contrôlent pas votre véhicule.");
                                return;
                            }
                        }

                        if (item == toggleEngine)
                        {
                            PressKeyFob(CurrentPersonalVehicle);
                            SetVehicleEngineOn(CurrentPersonalVehicle.Handle, !CurrentPersonalVehicle.IsEngineRunning, true, true);
                        }

                        else if (item == lockDoors || item == unlockDoors)
                        {
                            PressKeyFob(CurrentPersonalVehicle);
                            var _lock = item == lockDoors;
                            LockOrUnlockDoors(CurrentPersonalVehicle, _lock);
                        }

                        else if (item == soundHorn)
                        {
                            PressKeyFob(CurrentPersonalVehicle);
                            SoundHorn(CurrentPersonalVehicle);
                        }

                        else if (item == toggleAlarm)
                        {
                            PressKeyFob(CurrentPersonalVehicle);
                            ToggleVehicleAlarm(CurrentPersonalVehicle);
                        }
                    }
                }
                else
                {
                    Notify.Error("Vous n'avez pas encore sélectionné de véhicule personnel ou votre véhicule a été supprimé. Définissez un véhicule personnel avant de pouvoir utiliser ces options.");
                }
            };

            #region Doors submenu 
            var openAll = new MenuItem("Ouvrir toutes les portes", "Ouvre toutes les portes du véhicule.");
            var closeAll = new MenuItem("Fermer toutes les portes", "Ferme toutes les portes du véhicule.");
            var LF = new MenuItem("Porte avant gauche", "Ouvre/ferme la porte avant gauche.");
            var RF = new MenuItem("Porte avant droite", "Ouvre/ferme la porte avant droite.");
            var LR = new MenuItem("Porte arrière gauche", "Ouvre/ferme la porte arrière gauche.");
            var RR = new MenuItem("Porte arrière droite", "Ouvre/ferme la porte arrière droite.");
            var HD = new MenuItem("Capot", "Ouvre/ferme le capot.");
            var TR = new MenuItem("Coffre", "Ouvre/ferme le coffre.");
            var E1 = new MenuItem("Extra 1", "Ouvre/ferme la porte supplémentaire (#1). Notez que cette porte n'est pas présente sur la plupart des véhicules.");
            var E2 = new MenuItem("Extra 2", "Ouvre/ferme la porte supplémentaire (#2). Notez que cette porte n'est pas présente sur la plupart des véhicules.");
            var BB = new MenuItem("Soute à bombes", "Ouvre/ferme la soute à bombes. Disponible uniquement sur certains avions.");
            var doors = new List<string>() { "Avant gauche", "Avant droite", "Arrière gauche", "Arrière droite", "Capot", "Coffre", "Extra 1", "Extra 2", "Soute à bombes" };
            var removeDoorList = new MenuListItem("Retirer une porte", doors, 0, "Retire complètement une porte spécifique du véhicule.");
            var deleteDoors = new MenuCheckboxItem("Supprimer les portes retirées", "Si activé, les portes que vous retirez avec la liste ci-dessus seront supprimées du monde. Si désactivé, elles tomberont simplement au sol.", false);


            VehicleDoorsMenu.AddMenuItem(LF);
            VehicleDoorsMenu.AddMenuItem(RF);
            VehicleDoorsMenu.AddMenuItem(LR);
            VehicleDoorsMenu.AddMenuItem(RR);
            VehicleDoorsMenu.AddMenuItem(HD);
            VehicleDoorsMenu.AddMenuItem(TR);
            VehicleDoorsMenu.AddMenuItem(E1);
            VehicleDoorsMenu.AddMenuItem(E2);
            VehicleDoorsMenu.AddMenuItem(BB);
            VehicleDoorsMenu.AddMenuItem(openAll);
            VehicleDoorsMenu.AddMenuItem(closeAll);
            VehicleDoorsMenu.AddMenuItem(removeDoorList);
            VehicleDoorsMenu.AddMenuItem(deleteDoors);

            VehicleDoorsMenu.OnListItemSelect += (sender, item, index, itemIndex) =>
            {
                var veh = CurrentPersonalVehicle;
                if (veh != null && veh.Exists())
                {
                    if (!NetworkHasControlOfEntity(CurrentPersonalVehicle.Handle))
                    {
                        if (!NetworkRequestControlOfEntity(CurrentPersonalVehicle.Handle))
                        {
                            Notify.Error("Vous ne pouvez actuellement pas contrôler ce véhicule. Quelqu'un d'autre conduit-il actuellement votre véhicule ? Veuillez réessayer après vous être assuré que d'autres joueurs ne contrôlent pas votre véhicule.");
                            return;
                        }
                    }

                    if (item == removeDoorList)
                    {
                        PressKeyFob(veh);
                        SetVehicleDoorBroken(veh.Handle, index, deleteDoors.Checked);
                    }
                }
            };

            VehicleDoorsMenu.OnItemSelect += (sender, item, index) =>
            {
                var veh = CurrentPersonalVehicle;
                if (veh != null && veh.Exists() && !veh.IsDead)
                {
                    if (!NetworkHasControlOfEntity(CurrentPersonalVehicle.Handle))
                    {
                        if (!NetworkRequestControlOfEntity(CurrentPersonalVehicle.Handle))
                        {
                            Notify.Error("Vous ne pouvez actuellement pas contrôler ce véhicule. Quelqu'un d'autre conduit-il actuellement votre véhicule ? Veuillez réessayer après vous être assuré que d'autres joueurs ne contrôlent pas votre véhicule.");
                            return;
                        }
                    }

                    if (index < 8)
                    {
                        var open = GetVehicleDoorAngleRatio(veh.Handle, index) > 0.1f;
                        PressKeyFob(veh);
                        if (open)
                        {
                            SetVehicleDoorShut(veh.Handle, index, false);
                        }
                        else
                        {
                            SetVehicleDoorOpen(veh.Handle, index, false, false);
                        }
                    }
                    else if (item == openAll)
                    {
                        PressKeyFob(veh);
                        for (var door = 0; door < 8; door++)
                        {
                            SetVehicleDoorOpen(veh.Handle, door, false, false);
                        }
                    }
                    else if (item == closeAll)
                    {
                        PressKeyFob(veh);
                        for (var door = 0; door < 8; door++)
                        {
                            SetVehicleDoorShut(veh.Handle, door, false);
                        }
                    }
                    else if (item == BB && veh.HasBombBay)
                    {
                        PressKeyFob(veh);
                        var bombBayOpen = AreBombBayDoorsOpen(veh.Handle);
                        if (bombBayOpen)
                        {
                            veh.CloseBombBay();
                        }
                        else
                        {
                            veh.OpenBombBay();
                        }
                    }
                    else
                    {
                        Notify.Error("Vous n'avez pas encore sélectionné de véhicule personnel ou votre véhicule a été supprimé. Définissez un véhicule personnel avant de pouvoir utiliser ces options.");
                    }
                }
            };
            #endregion
        }



        private async void SoundHorn(Vehicle veh)
        {
            if (veh != null && veh.Exists())
            {
                var timer = GetGameTimer();
                while (GetGameTimer() - timer < 1000)
                {
                    SoundVehicleHornThisFrame(veh.Handle);
                    await Delay(0);
                }
            }
        }

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
