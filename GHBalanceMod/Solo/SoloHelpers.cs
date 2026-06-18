using BepInEx.Logging;
using GameNetcodeStuff;
using GHBalanceMod.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/*
netcode-patch -uv 2022.3.62 -nv 1.12.2 -tv 1.0.0 D:\NetcodePatcher\plugins D:\NetcodePatcher\deps
*/
namespace GHBalanceMod.Solo {
	// I have no idea what's happening here. What *IS* happening?
	/*
	public class ExampleNetworkHandler : NetworkBehaviour {
		public static event Action<string> LevelEvent;
		public static ExampleNetworkHandler Instance { get; private set; }
		public override void OnNetworkSpawn() {
			LevelEvent = null;

			if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
				Instance?.gameObject.GetComponent<NetworkObject>().Despawn();
			Instance = this;

			base.OnNetworkSpawn();
		}
		[ClientRpc]
		public void EventClientRpc(string eventName) {
			LevelEvent?.Invoke(eventName);
		}
	}
	*/
	internal class SoloHelpers : NetworkBehaviour {
		public static ulong HostSteamID = StartOfRound.Instance.allPlayerScripts[0].playerSteamId;
	
		public static int DaysPassed = 0;
		public static int QuotasMet = 0;
		public static int ScrapCollected;

		public static Dictionary<ulong, int> PersonalSuccessDays = new Dictionary<ulong, int>();
		public static int GroupSuccessDays = 0;
		public static int PermanentGroupSuccesses = 0;

		public static ulong[] Best = new ulong[4];
		static GroupDayType Day = GroupDayType.Unset;
		enum GroupDayType {
			Unset,
			High,
			Low,
			Failed
		}

		public enum CurrentFileType {
			ChallengeHost,
			ChallengeClient,
			NormalHost,
			NormalClient
		}
		public static void Bind() {
			Ensure();
			if (StartOfRound.Instance.connectedPlayersAmount > 0) {
				for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++) {
					int TotalDays = GroupSuccessDays + PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[i].playerSteamId];
					try {
						ShowLoaded(true);
						StartOfRound.Instance.allPlayerScripts[i].health = Mathf.Max(40 + (TotalDays * 5), 40);
						StartOfRound.Instance.allPlayerScripts[i].sprintTime = Mathf.Max(2.0f + (TotalDays * 0.25f), 2.0f);
						StartOfRound.Instance.allPlayerScripts[i].climbSpeed = Mathf.Max(1.0f + (TotalDays * 0.25f), 1.0f);
						StartOfRound.Instance.allPlayerScripts[i].sprintMeter = 1.0f;
						BalanceModBase.Log($"Successfully bound days to {StartOfRound.Instance.allPlayerScripts[i].playerUsername}!");
						ShowLoaded(false);
					} catch (Exception e) {
						BalanceModBase.LogError($"binding days to {StartOfRound.Instance.allPlayerScripts[i].playerUsername}", e);
					}
				}
			} else {
				int TotalDays = GroupSuccessDays + PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[0].playerSteamId];
				try {
					ShowLoaded(true);
					StartOfRound.Instance.allPlayerScripts[0].health = Mathf.Max(40 + (TotalDays * 5), 40);
					StartOfRound.Instance.allPlayerScripts[0].sprintTime = Mathf.Max(2.0f + (TotalDays * 0.25f), 2.0f);
					StartOfRound.Instance.allPlayerScripts[0].climbSpeed = Mathf.Max(1.0f + (TotalDays * 0.25f), 1.0f);
					StartOfRound.Instance.allPlayerScripts[0].sprintMeter = 1.0f;
					BalanceModBase.Log($"Successfully bound days to {StartOfRound.Instance.allPlayerScripts[0].playerUsername}!");
					ShowLoaded(false);
				} catch (Exception e) {
					BalanceModBase.LogError($"binding days to {StartOfRound.Instance.allPlayerScripts[0].playerUsername}", e);
				}
			}
		}
		public static void Ensure() {
			if (StartOfRound.Instance.connectedPlayersAmount == 0) {
				ulong ID = StartOfRound.Instance.allPlayerScripts[0].playerSteamId;
				if (!PersonalSuccessDays.ContainsKey(ID)) {
					PersonalSuccessDays.Add(ID, 0);
				}
			} else {
				for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++) {
					ulong ID = StartOfRound.Instance.allPlayerScripts[i].playerSteamId;
					if (!PersonalSuccessDays.ContainsKey(ID)) {
						PersonalSuccessDays.Add(ID, 0);
					}
				}
			}
			int PersonalDays = PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[0].playerSteamId];
			int BonusDays = 0;
			for (int i = 0; i < TimeOfDay.Instance.timesFulfilledQuota; i++) {
				BonusDays += i;
			}
			BonusDays *= 3;
			int SuccessDays = 2 * DaysPassed;
			int TotalDays = SuccessDays + BonusDays;
			int SuccessPersonalDays = 4 * DaysPassed;
			int TotalPersonalDays = SuccessPersonalDays + BonusDays;
			try {
				if (GroupSuccessDays < -10 || GroupSuccessDays > TotalDays) {
					Mathf.Clamp(GroupSuccessDays, -10, TotalDays);
					BalanceModBase.Log($"Clamped Group Days! ({PersonalDays})");
				}
			} catch (Exception e) {
				BalanceModBase.LogError("clamping Group Days", e);
			}
			if (DaysPassed > StartOfRound.Instance.gameStats.daysSpent || DaysPassed < 0) {
				Mathf.Clamp(DaysPassed, 0, StartOfRound.Instance.gameStats.daysSpent - TimeOfDay.Instance.timesFulfilledQuota);
				BalanceModBase.Log($"Clamped Days Passed! ({DaysPassed})");
			}
			if (StartOfRound.Instance.connectedPlayersAmount == 0) {
				try {
					if (PersonalDays < -20 || PersonalDays > TotalPersonalDays) {
						Mathf.Clamp(PersonalDays, -20, TotalPersonalDays);
						BalanceModBase.Log($"Clamped Personal Days for {StartOfRound.Instance.allPlayerScripts[0].playerUsername}! ({PersonalDays})");
					}
				} catch (Exception e) {
					BalanceModBase.LogError($"clamping Personal Days for {StartOfRound.Instance.allPlayerScripts[0].playerUsername}", e);
				}
			} else {
				for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++) {
					try {
						int PersonalDaysMulti = PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[i].playerSteamId];
						if (PersonalDaysMulti < -20 || PersonalDaysMulti > TotalPersonalDays) {
							Mathf.Clamp(PersonalDaysMulti, -20, TotalPersonalDays); 
							BalanceModBase.Log($"Clamped Personal Days for {StartOfRound.Instance.allPlayerScripts[i].playerUsername}! ({PersonalDaysMulti})");
						}
					} catch (Exception e) {
						BalanceModBase.LogError($"clamping Personal Days for {StartOfRound.Instance.allPlayerScripts[i].playerUsername}", e);
					}
				}
			}
		}
		public static void Load() {
			if (StartOfRound.Instance.IsServer) {
				string Location = "online/" + GameNetworkManager.Instance.currentSaveFileName;
				ES3Settings Key = new ES3Settings(ES3.EncryptionType.AES, Encryption.Password);
				Ensure();
				ShowLoaded(true);

				if (ES3.KeyExists("DaysPassed", Location, Key)) {
					try {
						DaysPassed = ES3.Load("DaysPassed", Location, 0, Key);
						BalanceModBase.Log("Loaded DaysPassed!");
					} catch (Exception e) {
						BalanceModBase.LogError("loading day count", e);
					}
				}
				
				if (ES3.KeyExists("PersistentDays", Location, Key)) {
					try {
						PermanentGroupSuccesses = ES3.Load("PersistentDays", Location, 0, Key);
						BalanceModBase.Log("Loaded Persistent!");
					} catch (Exception e) {
						BalanceModBase.LogError("loading persistent days", e);
					}
				}

				if (ES3.KeyExists("GSD", Location, Key)) {
					try {
						GroupSuccessDays = ES3.Load("GSD", Location, 0, Key);
						BalanceModBase.Log("Loaded GSD!");
					} catch (Exception e) {
						BalanceModBase.LogError("loading GSD", e);
					}
				}
				if (ES3.KeyExists("PSD", Location, Key)) {
					try {
						PersonalSuccessDays = ES3.Load("PSD", Location, new Dictionary<ulong, int>(), Key);
						BalanceModBase.Log("Loaded PSD!");
					} catch (Exception e) {
						BalanceModBase.LogError("loading PSD", e);
					}
				}
				ShowLoaded(false);
			}	
		}
		public static void Save() {
			string Location = "online/" + GameNetworkManager.Instance.currentSaveFileName;
			ES3Settings Key = new ES3Settings(ES3.EncryptionType.AES, Encryption.Password);
			Ensure();
			try {
				ES3.Save("GSD", GroupSuccessDays, Location, Key);
				BalanceModBase.Log("Saved GSD!");
			} catch (Exception e) {
				BalanceModBase.LogError("saving GSD", e);
			}

			try {
				ES3.Save("PersistentDays", PermanentGroupSuccesses, Location, Key);
				BalanceModBase.Log("Saved PersistentDays!");
			} catch (Exception e) {
				BalanceModBase.LogError("saving PersistentDays", e);
			}

			try {
				ES3.Save("DaysPassed", DaysPassed, Location, Key);
				BalanceModBase.Log("Saved DaysPassed!");
			} catch (Exception e) {
				BalanceModBase.LogError("saving DaysPassed", e);
			}

			try {
				ES3.Save("PSD", PersonalSuccessDays, Location, Key);
				BalanceModBase.Log("Saved PSD!");
			} catch (Exception e) {
				BalanceModBase.LogError("saving PSD", e);
			}
		}
		public static void Clear() {
			int BonusDays = 0;
			for (int i = 0; i < QuotasMet; i++) {
				BonusDays += i;
			}
			BonusDays *= 3;
			int SuccessDays = 2 * DaysPassed;
			int TotalDays = SuccessDays + BonusDays;
			if (GroupSuccessDays == TotalDays && QuotasMet >= 5) {
				Perks(FindObjectOfType<Terminal>());
			} else {
				if (PermanentGroupSuccesses > 0) {
					Perks(FindObjectOfType<Terminal>());
				} else if (PermanentGroupSuccesses < 0) {
					Nerfs(FindObjectOfType<Terminal>());
				}
				try {
					GroupSuccessDays = 0;
					PersonalSuccessDays.Clear();
					Ensure();
					if (GroupSuccessDays == 0 && PersonalSuccessDays[HostSteamID] == 0) {
						BalanceModBase.Log("Successfully reset data!");
					}
				} catch (Exception e) {
					BalanceModBase.LogError("resetting stats", e);
				}
			}
			DaysPassed = 0;
			GameNetworkManager.Instance.SaveGame();
		}
		public static void Perks(Terminal terminal) {
			int rolls = Math.Abs(PermanentGroupSuccesses);
			List<int> results = new List<int>(rolls);
			results.Clear();
			for (int j = 0; j < rolls; j++) {
				System.Random Switcher = new System.Random(87652 + j);
				int picked = Switcher.Next(0, 20);
				if (!results.Contains(picked)) {
					if (terminal != null) { 
							switch (picked) {
							case 1: {
								PermanentGroupSuccesses++;
								Save();
								break;
							}
							case 2: {
								if (!StartOfRound.Instance.magnetOn) {
									GameObject gameObject = Instantiate(StartOfRound.Instance.VehiclesList[0], StartOfRound.Instance.magnetPoint.position + StartOfRound.Instance.magnetPoint.forward * 5f, Quaternion.identity, RoundManager.Instance.VehiclesContainer);
									StartOfRound.Instance.attachedVehicle = gameObject.GetComponent<VehicleController>();
									gameObject.GetComponent<VehicleController>().ToggleHeadlightsServerRpc(false);
									StartOfRound.Instance.isObjectAttachedToMagnet = true;
									StartOfRound.Instance.attachedVehicle.NetworkObject.Spawn();
									StartOfRound.Instance.magnetOn = true;
									StartOfRound.Instance.magnetLever.initialBoolState = true;
									StartOfRound.Instance.magnetLever.setInitialState = true;
									StartOfRound.Instance.magnetLever.SetInitialState();
								}
								if (!terminal.hasWarrantyTicket) {
									terminal.hasWarrantyTicket = true;
								}
								break;
							}
							case 3: {
								(int, int)[] Gifts = new (int, int)[15];
								Gifts[0] = (0, 0); // Walkie Talkie (ID, Amount)
								Gifts[1] = (1, 0); // Flashlight (ID, Amount)
								Gifts[2] = (2, 0); // Shovel (ID, Amount)
								Gifts[3] = (3, 0); // Lockpicker (ID, Amount)
								Gifts[4] = (4, 0); // Pro Flashlight (ID, Amount)
								Gifts[5] = (5, 0); // Stun Grenade (ID, Amount)
								Gifts[6] = (6, 0); // Boombox (ID, Amount)
								Gifts[7] = (7, 0); // Inhalant (TZP) (ID, Amount)
								Gifts[8] = (8, 1); // Zap Gun (ID, Amount)
								Gifts[9] = (9, 0); // Jetpack (ID, Amount)
								Gifts[10] = (10, 2); // Extension Ladder (ID, Amount)
								Gifts[11] = (11, 1); // Radar Booster (ID, Amount)
								Gifts[12] = (12, 0); // Spray Paint (ID, Amount)
								Gifts[13] = (13, 2); // Weed Killer (ID, Amount)
								Gifts[14] = (14, 0); // Belt Bag (ID, Amount)

								(int, bool)[] GiftFurniture = new (int, bool)[34];
								GiftFurniture[0] = (0, false); // Orange Suit (ID, Spawn) // Null
								GiftFurniture[7] = (7, false); // Cupboard (ID, Spawn) // Null
								GiftFurniture[8] = (8, false); // File Cabinet (ID, Spawn) // Null
								GiftFurniture[11] = (11, false); // Light Switch (ID, Spawn) // Null
								GiftFurniture[15] = (15, false); // Bunkbeds (ID, Spawn) // Null
								GiftFurniture[16] = (16, false); // Terminal (ID, Spawn) // Biggest Null

								GiftFurniture[1] = (1, true); // Green Suit (ID, Spawn)
								GiftFurniture[2] = (2, true); // Hazard Suit (ID, Spawn)
								GiftFurniture[3] = (3, true); // Pajama Suit (ID, Spawn)
								GiftFurniture[24] = (24, true); // Purple Suit (ID, Spawn)
								GiftFurniture[25] = (25, true); // Bee Suit (ID, Spawn)
								GiftFurniture[26] = (26, true); // Bunny Suit (ID, Spawn)

								GiftFurniture[5] = (5, false); // Teleporter (ID, Spawn)
								GiftFurniture[10] = (10, false); // Shower (ID, Spawn)
								GiftFurniture[17] = (17, false); // Signal Translator (ID, Spawn)
								GiftFurniture[18] = (18, false); // Loud Horn (ID, Spawn)
								GiftFurniture[19] = (19, false); // Inverse Teleporter (ID, Spawn)
								GiftFurniture[27] = (27, false); // Disco Ball (ID, Spawn)
								GiftFurniture[29] = (29, false); // Sofa (ID, Spawn)
								GiftFurniture[32] = (32, true); // Electric Chair (ID, Spawn)

								GiftFurniture[4] = (4, false); // Cozy Lights (ID, Spawn)
								GiftFurniture[6] = (6, false); // Television (ID, Spawn)
								GiftFurniture[9] = (9, false); // Toilet (ID, Spawn)
								GiftFurniture[12] = (12, false); // Record Player (ID, Spawn)
								GiftFurniture[13] = (13, false); // Table (End) (ID, Spawn)
								GiftFurniture[14] = (14, false); // Table (Romantic) (ID, Spawn)
								GiftFurniture[20] = (20, false); // Jack O Lantern (ID, Spawn)
								GiftFurniture[21] = (21, false); // Welcome Mat (ID, Spawn)
								GiftFurniture[22] = (22, false); // Goldfish (ID, Spawn)
								GiftFurniture[23] = (23, false); // Plushie Pajama Man (ID, Spawn)
								GiftFurniture[28] = (28, false); // Microwave (ID, Spawn)
								GiftFurniture[30] = (30, false); // Fridge (ID, Spawn)
								GiftFurniture[31] = (31, false); // Painting (ID, Spawn)
								GiftFurniture[33] = (33, false); // Dog House (ID, Spawn)

								if (StartOfRound.Instance.connectedPlayersAmount > 0) {
									int Spare = (StartOfRound.Instance.connectedPlayersAmount + 1) * 2;
									Gifts[0] = (0, 2); // Walkie Talkie (ID, Amount)
									Gifts[2] = (2, Spare); // Shovel (ID, Amount)
									Gifts[4] = (4, Spare); // Pro Flashlight (ID, Amount)
								} else {
									Gifts[2] = (2, 2); // Shovel (ID, Amount)
									Gifts[4] = (4, 2); // Pro Flashlight (ID, Amount)
								}

								Transform transform = StartOfRound.Instance.playerSpawnPositions[0];
								Transform inElevator = StartOfRound.Instance.elevatorTransform;
								Vector3 Coordinates = transform.position;
								Coordinates.y += 0.5f;
								if (StartOfRound.Instance.unlockablesList.unlockables.Count == 34) {
									for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables.Count; i++) {
										if (GiftFurniture[i].Item2) {
											StartOfRound.Instance.BuyShipUnlockableServerRpc(GiftFurniture[i].Item1, terminal.groupCredits);
										}
									}
								}
								for (int i = 0; i < 15; i++) {
									for (int k = 0; k < Gifts[i].Item2; k++) {
										GameObject item = Instantiate(terminal.buyableItemsList[Gifts[i].Item1].spawnPrefab, Coordinates, Quaternion.identity, inElevator);
										item.GetComponent<RadarBoosterItem>()?.SetRadarBoosterNameLocal("Blessing");
										if (item.GetComponent<RadarBoosterItem>() != null) {
											item.GetComponent<RadarBoosterItem>().radarBoosterName = "Blessing";
										}
										item.GetComponent<GrabbableObject>().fallTime = 0f;
										item.GetComponent<GrabbableObject>().itemProperties.saveItemVariable = true;
										item.GetComponent<GrabbableObject>().isInShipRoom = true;
										item.GetComponent<GrabbableObject>().isInElevator = true;
										item.GetComponent<GrabbableObject>().itemProperties.isScrap = false;
										item.GetComponent<GrabbableObject>().scrapValue = 0;

										item.GetComponent<NetworkObject>().Spawn();
									}
								}
								break;
							}
							case 4: {
								for (int i = 0; i < terminal.buyableItemsList.Length; i++) {
									terminal.itemSalesPercentages[i] = 50;
								}
								break;
							}
							case 5: {
									int[] Moons = new int[13];
									Moons[0] = 0; // Experimentation
									Moons[1] = 1; // Assurance
									Moons[2] = 2; // Vow
									Moons[3] = 3; // Company
									Moons[4] = 4; // March
									Moons[5] = 5; // Adamance
									Moons[6] = 6; // Rend
									Moons[7] = 7; // Dine
									Moons[8] = 8; // Offense
									Moons[9] = 9; // Titan
									Moons[10] = 10; // Artifice
									Moons[11] = 11; // Liquidation
									Moons[12] = 12; // Embrion

									for (int i = 0; i < 13; i++) {
										BalanceModBase.Log(StartOfRound.Instance.levels[i].PlanetName + ", number: " + i);
										if (StartOfRound.Instance.levels[i].planetHasTime) {
											StartOfRound.Instance.levels[i].currentWeather = LevelWeatherType.None;
										}
									}
									break;
							}
							case 6: {
								StartOfRound.Instance.ChangeLevelServerRpc(6, terminal.groupCredits);						
								break;
							}
							case 7: {
								StartOfRound.Instance.ChangeLevelServerRpc(7, terminal.groupCredits);
								break;
							}
							case 8: {
								terminal.startingCreditsAmount = 120;
								break;
							}
							case 9: {
								if (!StartOfRound.Instance.magnetOn) {
									GameObject gameObject = Instantiate(StartOfRound.Instance.VehiclesList[0], StartOfRound.Instance.magnetPoint.position + StartOfRound.Instance.magnetPoint.forward * 5f, Quaternion.identity, RoundManager.Instance.VehiclesContainer);
									StartOfRound.Instance.attachedVehicle = gameObject.GetComponent<VehicleController>();
									gameObject.GetComponent<VehicleController>().ToggleHeadlightsServerRpc(false);
									StartOfRound.Instance.isObjectAttachedToMagnet = true;
									StartOfRound.Instance.attachedVehicle.NetworkObject.Spawn();
									StartOfRound.Instance.magnetOn = true;
									StartOfRound.Instance.magnetLever.initialBoolState = true;
									StartOfRound.Instance.magnetLever.setInitialState = true;
									StartOfRound.Instance.magnetLever.SetInitialState();
								} else if (!terminal.hasWarrantyTicket) {
									terminal.hasWarrantyTicket = true;
								}
								(int, bool)[] GiftFurniture = new (int, bool)[34];
								GiftFurniture[0] = (0, false); // Orange Suit (ID, Spawn) // Null
								GiftFurniture[7] = (7, false); // Cupboard (ID, Spawn) // Null
								GiftFurniture[8] = (8, false); // File Cabinet (ID, Spawn) // Null
								GiftFurniture[11] = (11, false); // Light Switch (ID, Spawn) // Null
								GiftFurniture[15] = (15, false); // Bunkbeds (ID, Spawn) // Null
								GiftFurniture[16] = (16, false); // Terminal (ID, Spawn) // Biggest Null

								GiftFurniture[1] = (1, true); // Green Suit (ID, Spawn)
								GiftFurniture[2] = (2, true); // Hazard Suit (ID, Spawn)
								GiftFurniture[3] = (3, true); // Pajama Suit (ID, Spawn)
								GiftFurniture[24] = (24, true); // Purple Suit (ID, Spawn)
								GiftFurniture[25] = (25, true); // Bee Suit (ID, Spawn)
								GiftFurniture[26] = (26, true); // Bunny Suit (ID, Spawn)

								GiftFurniture[5] = (5, true); // Teleporter (ID, Spawn)
								GiftFurniture[10] = (10, true); // Shower (ID, Spawn)
								GiftFurniture[17] = (17, true); // Signal Translator (ID, Spawn)
								GiftFurniture[18] = (18, true); // Loud Horn (ID, Spawn)
								GiftFurniture[19] = (19, true); // Inverse Teleporter (ID, Spawn)
								GiftFurniture[27] = (27, true); // Disco Ball (ID, Spawn)
								GiftFurniture[29] = (29, true); // Sofa (ID, Spawn)
								GiftFurniture[32] = (32, true); // Electric Chair (ID, Spawn)
						
								GiftFurniture[4] = (4, false); // Cozy Lights (ID, Spawn)
								GiftFurniture[6] = (6, false); // Television (ID, Spawn)
								GiftFurniture[9] = (9, false); // Toilet (ID, Spawn)
								GiftFurniture[12] = (12, false); // Record Player (ID, Spawn)
								GiftFurniture[13] = (13, false); // Table (End) (ID, Spawn)
								GiftFurniture[14] = (14, false); // Table (Romantic) (ID, Spawn)
								GiftFurniture[20] = (20, false); // Jack O Lantern (ID, Spawn)
								GiftFurniture[21] = (21, false); // Welcome Mat (ID, Spawn)
								GiftFurniture[22] = (22, false); // Goldfish (ID, Spawn)
								GiftFurniture[23] = (23, false); // Plushie Pajama Man (ID, Spawn)
								GiftFurniture[28] = (28, false); // Microwave (ID, Spawn)
								GiftFurniture[30] = (30, false); // Fridge (ID, Spawn)
								GiftFurniture[31] = (31, false); // Painting (ID, Spawn)
								GiftFurniture[33] = (33, false); // Dog House (ID, Spawn)

								if (StartOfRound.Instance.unlockablesList.unlockables.Count == 34) {
									for (int i = 0; i < StartOfRound.Instance.unlockablesList.unlockables.Count; i++) {
										if (GiftFurniture[i].Item2) {
											StartOfRound.Instance.BuyShipUnlockableServerRpc(GiftFurniture[i].Item1, terminal.groupCredits);
										}
									}
								}

								break;
							}
							default: {
								PermanentGroupSuccesses++;
								Save();
								break;
							}
						}
					}
					results.Add(picked);
				}
			}
		}
		public static void Nerfs(Terminal terminal) {

			int rolls = Math.Abs(PermanentGroupSuccesses);
			List<int> results = new List<int>(rolls);
			results.Clear();
			for (int j = 0; j < rolls; j++) {
				System.Random Switcher = new System.Random(87652 + j);
				System.Random random = new System.Random(2980283 + j);
				int picked = Switcher.Next(0, 20);
				if (!results.Contains(picked)) {
					if (terminal != null) {
						switch (picked) {
							case 1: {
								PermanentGroupSuccesses--;
								Save();
								break;
							}
							case 2: {
								if (terminal.hasWarrantyTicket) {
									terminal.hasWarrantyTicket = false;
								} else if (StartOfRound.Instance.magnetOn) {
									StartOfRound.Instance.magnetOn = false;
								} 
								break;
							}
							case 3: {
								for (int i = 0; i < terminal.buyableItemsList.Length; i++) {
									terminal.itemSalesPercentages[i] = 225;
								}
								break;
							}
							case 4: {
								switch (random.Next(0, 15)) {
									case 1: {
										for (int i = 0; i < 13; i++) {
											if (StartOfRound.Instance.levels[i].planetHasTime) {
												StartOfRound.Instance.levels[i].currentWeather = LevelWeatherType.Eclipsed;
											}
										}
										break;
									}
									case 2: {
										for (int i = 0; i < 13; i++) {
											if (StartOfRound.Instance.levels[i].planetHasTime) {
												StartOfRound.Instance.levels[i].currentWeather = LevelWeatherType.Foggy;
											}
										}
										break;
									}
									case 3: {
										for (int i = 0; i < 13; i++) {
											if (StartOfRound.Instance.levels[i].planetHasTime) {
												StartOfRound.Instance.levels[i].currentWeather = LevelWeatherType.Stormy;
											}
										}
										break;
									}
									default: {
										for (int i = 0; i < 13; i++) {
											if (StartOfRound.Instance.levels[i].planetHasTime) {
												StartOfRound.Instance.levels[i].currentWeather = LevelWeatherType.Eclipsed;
											}
										}
										break;
									}
								}					
								break;
							}
							case 5: {
								terminal.startingCreditsAmount = 30;
								break;
							}
							case 6: {
								int[] Moons = new int[13];
								Moons[0] = 0; // Experimentation
								Moons[1] = 1; // Assurance
								Moons[2] = 2; // Vow
								Moons[3] = 3; // Company
								Moons[4] = 4; // March
								Moons[5] = 5; // Adamance
								Moons[6] = 6; // Rend
								Moons[7] = 7; // Dine
								Moons[8] = 8; // Offense
								Moons[9] = 9; // Titan
								Moons[10] = 10; // Artifice
								Moons[11] = 11; // Liquidation
								Moons[12] = 12; // Embrion
								StartOfRound.Instance.ChangeLevelServerRpc(12, terminal.groupCredits);
								for (int i = 0; i < 13; i++) {
									if (StartOfRound.Instance.levels[i].planetHasTime) {
										StartOfRound.Instance.levels[i].currentWeather = LevelWeatherType.Eclipsed;
									}
								}
								for (int i = 0; i < terminal.buyableItemsList.Length; i++) {
									terminal.itemSalesPercentages[i] = 200;
								}
								break;
							}
							default: {
					
								break;
							}
						}
						results.Add(picked);
					}
				}
			}
		}
		public static void ShowLoaded(bool previous) {
			if (previous) {
			BalanceModBase.Log("Previous Group Success Days: " + GroupSuccessDays);
				for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++) {
				BalanceModBase.Log($"Previous Personal Success Days ({StartOfRound.Instance.allPlayerScripts[i].playerUsername}): " + PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[i].playerSteamId]);
				}
			} else {
			BalanceModBase.Log("Current Group Success Days: " + GroupSuccessDays);
				for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++) {
				BalanceModBase.Log($"Current Personal Success Days ({StartOfRound.Instance.allPlayerScripts[i].playerUsername}): " + PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[i].playerSteamId]);
				}
			}
		}
		public static bool CalculatedBest() {
				int[] Steps = new int[StartOfRound.Instance.allPlayerScripts.Length];
				int[] Profit = new int[StartOfRound.Instance.allPlayerScripts.Length];
				int[] Turns = new int[StartOfRound.Instance.allPlayerScripts.Length];
				int[] Damage = new int[StartOfRound.Instance.allPlayerScripts.Length];

				PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
				PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;

				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
					Profit[i] = stats[i].profitable;
					Damage[i] = stats[i].damageTaken;
					Steps[i] = stats[i].stepsTaken;
					Turns[i] = stats[i].turnAmount;
				}
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
					if (Profit.Max() == stats[i].profitable) {
						if (Steps.Max() == stats[i].stepsTaken) {
							Best[1] = scripts[i].playerSteamId;
						} else {
							Best[1] = 1;
						}
					}
					if (Profit.Min() == stats[i].profitable) {
						if (Steps.Min() == stats[i].stepsTaken) {
							Best[2] = scripts[i].playerSteamId;
						} else {
							Best[2] = 1;
						}
					}
					if (Damage.Max() == stats[i].damageTaken) {
						Best[3] = scripts[i].playerSteamId;
					}
					if (Turns.Max() == stats[i].turnAmount) {
						Best[4] = scripts[i].playerSteamId;
					}
				}			
			bool Check = Best[1] != 0 && Best[2] != 0 && Best[3] != 0 && Best[4] != 0;
			if (Check) {
				return true;
			} else {
				return false;
			}
		}
		public static bool CalculateNotes(bool AllDead) {
		BalanceModBase.Log("Total scrap this round : " + ScrapCollected);
			if (!AllDead) {
				if (StartOfRound.Instance.connectedPlayersAmount > 0) {
					if (CalculatedBest()) {
						for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++) {
							bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;
							ulong player = StartOfRound.Instance.allPlayerScripts[i].playerSteamId;
							if (player == Best[2]) {
								if (lifeCheck) {
									PersonalSuccessDays[player] -= 4;
								} else {
									PersonalSuccessDays[player] -= 5;
								}
							} else if (player == Best[1]) {
								if (lifeCheck) {
									PersonalSuccessDays[player] += 4;
								} else {
									PersonalSuccessDays[player] -= 1;
								}
							} else {
								if (lifeCheck) {
									PersonalSuccessDays[player] += 1;
								} else {
									PersonalSuccessDays[player] -= 1;
								}
							}
						}						
					}
				}
			}

			int LowScrapCount = (TimeOfDay.Instance.timesFulfilledQuota + 1); // (1, 2, 3, 4, etc.)
			int HighScrapCount = LowScrapCount * 4; // (4, 8, 12, 16, etc.)
			bool Profitable = ScrapCollected >= LowScrapCount;
			bool HighlyProfitable = ScrapCollected >= HighScrapCount;
			int OldGroupDays = GroupSuccessDays;
			int OldPersonalDays = PersonalSuccessDays[HostSteamID];

			if (HighlyProfitable) {
				Day = GroupDayType.High;
			} else if (Profitable) {
				Day = GroupDayType.Low;
			} else {
				Day = GroupDayType.Failed;
			}

			bool Solo = StartOfRound.Instance.connectedPlayersAmount == 0;

			for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
				PlayerStats stats = StartOfRound.Instance.gameStats.allPlayerStats[i];
				PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
				stats.isActivePlayer =
					player.disconnectedMidGame ||
					player.isPlayerDead ||
					player.isPlayerControlled;
			}

			if (AllDead) {
				if (!Solo) {
					for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++) {
						PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[i].playerSteamId] -= 5;
					}
				} else {
					GroupSuccessDays -= 5;
				}
				GroupSuccessDays -= 5;
			} else {
				switch (Day) {
					case GroupDayType.High: {
						GroupSuccessDays += (2 + TimeOfDay.Instance.timesFulfilledQuota);
						break;
					}
					case GroupDayType.Low: {
						GroupSuccessDays += 2;
						break;
					}
					case GroupDayType.Failed: {
						GroupSuccessDays -= 4;
						break;
					}
					default: {
						break;
					}
				}
			}

			if (PersonalSuccessDays[HostSteamID] != OldPersonalDays && GroupSuccessDays != OldGroupDays) {
				return true;
			} else {
				return false;
			}
		}
		public static void DisplayNotes(bool AllDead) {
			int LowScrapCount = (TimeOfDay.Instance.timesFulfilledQuota + 1); // (1, 2, 3, 4, etc.)
			int HighScrapCount = LowScrapCount * 4; // (4, 8, 12, 16, etc.)
			bool Profitable = ScrapCollected >= LowScrapCount;
			bool HighlyProfitable = ScrapCollected >= HighScrapCount;

			CalculateNotes(AllDead);		

			StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add($"Scrap: {ScrapCollected}/{LowScrapCount}");
			if (HighlyProfitable) {
				StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add($"REALLY profitable!! ({2 + TimeOfDay.Instance.timesFulfilledQuota} points!!)");
			} else if (Profitable) {
				StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add($"Profitable!! (2 points!!)");
			} else {
				StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add($"Failed. (-4 points!!)");
			}
			if (StartOfRound.Instance.connectedPlayersAmount > 0) {
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
					bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;
					StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
						StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
						StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
						StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;
					if (StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer) {
						ulong SteamID = StartOfRound.Instance.allPlayerScripts[i].playerSteamId;
						if (AllDead) {
							StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("All players died. -10 points EACH!");
						} else {
							if (SteamID == Best[1]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Most Profitable! (+4 points.)");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Gave their life for the cause. (-1 point.)");
								}
							} else if (SteamID == Best[2]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Laziest! (-4 points!)");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Wasn't too careful. (-5 points.)");
								}
							}
							if (SteamID == Best[3]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Most injured!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Gave in to their injuries.");
								}
							}
							if (SteamID == Best[4]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Wariest!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Most clueless.");
								}
							}
							if (SteamID != Best[1] && SteamID != Best[2]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Lived! (+1 point.)");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Died. (-1 point.)");
								}
							}
						}
					}
				}
			}
			if (StartOfRound.Instance.connectedPlayersAmount > 0) {
				for (int i = 0; i < StartOfRound.Instance.connectedPlayersAmount + 1; i++) {
					ulong ID = StartOfRound.Instance.allPlayerScripts[i].playerSteamId;
					StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add($"Quota: {TimeOfDay.Instance.timesFulfilledQuota + 1}, GSD: {Mathf.Max(GroupSuccessDays, -10)}, PSD: {Mathf.Max(PersonalSuccessDays[ID], -20)}");
					
				}
			} else {
				ulong ID = StartOfRound.Instance.allPlayerScripts[0].playerSteamId;
				StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add($"Quota: {TimeOfDay.Instance.timesFulfilledQuota + 1}, GSD: {Mathf.Max(GroupSuccessDays, -10)}, PSD: {Mathf.Max(PersonalSuccessDays[ID], -20)}");
			}
		}
	}

	internal class ClientListeners {
		public static void Subscribe() {
			// Syncs the information?
		}
		public static void Unsubscribe() {
			
		}
	}
}
