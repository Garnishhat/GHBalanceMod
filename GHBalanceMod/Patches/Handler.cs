using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

// SDC = Success Day Count
// PSD = Personal Success Days
// GSD = Group Success Days

namespace GHBalanceMod.Patches {
	
	[HarmonyPatch(typeof(HUDManager))]
	internal class ScrapCounter : NetworkBehaviour {
		// Middle
		[HarmonyPatch("DisplayNewScrapFound")]
		[HarmonyPostfix]
		public static void CollectedScrap() {
			if (StartOfRound.Instance.IsServer && !StartOfRound.Instance.inShipPhase) {
				PlayerNotes.ScrapThisRound++;
			}
		}
	}

	[HarmonyPatch(typeof(StartMatchLever))]
	internal class StartInjection : NetworkBehaviour {
		[HarmonyPatch("PullLever")]
		[HarmonyPostfix]
		public static void Injection(ref bool ___leverHasBeenPulled) {
			if (___leverHasBeenPulled && StartOfRound.Instance.IsServer) {
				PlayerNotes.BindValue(PlayerNotes.GroupSuccessDays, PlayerNotes.PersonalSuccessDays);
			}
		}
	}
	
	[HarmonyPatch(typeof(StartOfRound))]
	internal class PlayerNotes : NetworkBehaviour {
		public static string[] Best = new string[4];
		public static int GroupSuccessDays = 0;
		public static Dictionary<string, int> PersonalSuccessDays = new Dictionary<string, int>();
		public static int ScrapThisRound = 0;

		[ClientRpc]
		public static void BindValue(int GroupDays, Dictionary<string, int> PersonalDays) {
			int Total = StartOfRound.Instance.connectedPlayersAmount + 1;
			bool Solo = StartOfRound.Instance.connectedPlayersAmount == 0;

			bool MultiplayerReset = false;
			if (!Solo) {
				for (int i = 0; i < Total; i++) {
					if (!PersonalDays.ContainsKey(StartOfRound.Instance.allPlayerScripts[i].playerUsername)) {
						PersonalDays.Add(StartOfRound.Instance.allPlayerScripts[i].playerUsername, 0);
					}
					MultiplayerReset = PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[i].playerUsername] > (4 * (StartOfRound.Instance.gameStats.daysSpent + 1));
				}
			}
			bool Reset = GroupDays > (2 * (StartOfRound.Instance.gameStats.daysSpent + 1)); // If this is a high quota (Or even just, say, day 3) It will read six, so resetting = "6" on day 1 which is like "Nah, that doesn't work, cancel time!!" Sure, Day 4 always adds a buffer, but you'd still be higher than day one's max amount.

			if (Reset && StartOfRound.Instance.gameStats.daysSpent == 0 || MultiplayerReset && StartOfRound.Instance.gameStats.daysSpent == 0) {
				GroupSuccessDays = 0;
				PersonalSuccessDays.Clear();
			} else if (Reset) {
				GroupSuccessDays = 2 * (StartOfRound.Instance.gameStats.daysSpent + 1);
			} else if (MultiplayerReset && !Solo) {
				for (int i = 0; i < Total; i++) {
					if (!PersonalDays.ContainsKey(StartOfRound.Instance.allPlayerScripts[i].playerUsername)) {
						PersonalDays.Add(StartOfRound.Instance.allPlayerScripts[i].playerUsername, 0);
					}
					PersonalDays[StartOfRound.Instance.allPlayerScripts[i].playerUsername] = 4 * (StartOfRound.Instance.gameStats.daysSpent + 1);
				}
			}
			// Save
			ES3Settings settings = new ES3Settings(ES3.EncryptionType.AES, Encrypted.Password); // Yes... now you can see that I'm not completely dumb.
			if (!Solo) {
				try {
					ES3.Save("PSD", PersonalSuccessDays, "SDC" + GameNetworkManager.Instance.currentSaveFileName, settings);
				} catch (Exception e) {
					Debug.LogError("ERROR while saving [PSD] on local client! : " + e);
				}
			}
			try {
				ES3.Save("GSD", GroupSuccessDays, "SDC" + GameNetworkManager.Instance.currentSaveFileName, settings);
			} catch (Exception e) {
				Debug.LogError("ERROR while saving [GSD] on local client! : " + e);
			}
			// Load
			if (!Solo) {
				if (ES3.KeyExists("PSD", "SDC" + GameNetworkManager.Instance.currentSaveFileName, settings)) {
					PersonalSuccessDays = ES3.Load("PSD", "SDC" + GameNetworkManager.Instance.currentSaveFileName, new Dictionary<string, int>(), settings);
				}
			}
			if (ES3.KeyExists("GSD", "SDC" + GameNetworkManager.Instance.currentSaveFileName, settings)) {
				GroupSuccessDays = ES3.Load("GSD", "SDC" + GameNetworkManager.Instance.currentSaveFileName, 0, settings);
			}
			
			if (Solo) {
				StartOfRound.Instance.allPlayerScripts[0].health = Mathf.Max(40 + (GroupDays * 5), 40);
				StartOfRound.Instance.allPlayerScripts[0].sprintTime = Mathf.Max(2.0f + (GroupDays * 0.25f), 2.0f);
				StartOfRound.Instance.allPlayerScripts[0].sprintMeter = 1.0f;
			} else {
				for (int i = 0; i < Total; i++) {
					bool Local = StartOfRound.Instance.allPlayerScripts[i].IsOwner;
					StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer = Local && StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;

					if (StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer) {
						if (!PersonalDays.ContainsKey(StartOfRound.Instance.localPlayerController.playerUsername)) {
							PersonalDays.Add(StartOfRound.Instance.localPlayerController.playerUsername, 0);
						}
						int TotalDays = PersonalDays[StartOfRound.Instance.localPlayerController.playerUsername] + GroupDays;
						StartOfRound.Instance.allPlayerScripts[i].health = Mathf.Max(40 + (TotalDays * 5), 40);
						StartOfRound.Instance.allPlayerScripts[i].sprintTime = Mathf.Max(2.0f + (TotalDays * 0.25f), 2.0f);
						StartOfRound.Instance.allPlayerScripts[i].sprintMeter = 1.0f;
					}
				}
			}			
		}
		
		// Middle
		[HarmonyPatch("WritePlayerNotes")]
		[HarmonyPrefix]
		[ClientRpc]
		public static void WriteNotes() {
			if (StartOfRound.Instance.IsServer) {
				int AllOnlinePlayers = StartOfRound.Instance.allPlayerScripts.Length;
				PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
				PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;

				int GarnishScrapCount = (TimeOfDay.Instance.timesFulfilledQuota + 1) * 2; // (2, 4, 6, 8, etc.)
				int GarnishHighScrapCount = GarnishScrapCount * 2; // (4, 8, 12, 16, etc.)
				bool profitable = ScrapThisRound >= GarnishScrapCount;
				bool highlyProfitable = ScrapThisRound >= GarnishHighScrapCount || ScrapThisRound >= 30;
								
				if (highlyProfitable) {
					if (StartOfRound.Instance.connectedPlayersAmount == 0) {
						StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("REALLY Profitable!");
					}
					GroupSuccessDays += (2 + TimeOfDay.Instance.timesFulfilledQuota);
				} else if (profitable) {
					if (StartOfRound.Instance.connectedPlayersAmount == 0) {
						StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable!");
					}
					GroupSuccessDays += 2;
				} else {
					if (StartOfRound.Instance.connectedPlayersAmount == 0) {
						StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Failed.");
						StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Base quota is " + GarnishScrapCount + " items.");
					}
					GroupSuccessDays -= 4;
				}

				if (StartOfRound.Instance.connectedPlayersAmount == 0) {
					if (!PersonalSuccessDays.ContainsKey(StartOfRound.Instance.localPlayerController.playerUsername)) {
						PersonalSuccessDays.Add(StartOfRound.Instance.localPlayerController.playerUsername, 0);
					}
					StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Times met Quota: " + (TimeOfDay.Instance.quotaFulfilled + 1) + "Stats: " + GroupSuccessDays + ", " + PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[0].playerUsername]);
				}

				if (StartOfRound.Instance.connectedPlayersAmount > 0) {
					int[] Steps = new int[StartOfRound.Instance.allPlayerScripts.Length];
					int[] Profit = new int[StartOfRound.Instance.allPlayerScripts.Length];
					int[] Turns = new int[StartOfRound.Instance.allPlayerScripts.Length];
					int[] Damage = new int[StartOfRound.Instance.allPlayerScripts.Length];
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
						Profit[i] = stats[i].profitable;
						Damage[i] = stats[i].damageTaken;
						Steps[i] = stats[i].stepsTaken;
						Turns[i] = stats[i].turnAmount;
					}
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
						if (Steps.Min() == stats[i].stepsTaken) {
							Best[1] = scripts[i].playerUsername;
						}
						if (Steps.Max() == stats[i].stepsTaken && Profit.Max() == stats[i].profitable) {
							Best[2] = scripts[i].playerUsername;
						}
						if (Damage.Max() == stats[i].damageTaken) {
							Best[3] = scripts[i].playerUsername;
						}
						if (Turns.Max() == stats[i].turnAmount) {
							Best[4] = scripts[i].playerUsername;
						}
					}

					for (int i = 0; i < AllOnlinePlayers; i++) {
						bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;

						StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
							StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
							StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
							StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled; // NOT saying "By anyone in particular." Just as long as it's not... not controlled, I guess.
							
						if (stats[i].isActivePlayer) {
							string player = scripts[i].playerUsername;
							if (player == Best[1]) {
								if (lifeCheck) {
									PersonalSuccessDays[Best[1]] -= 4;
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!");
								} else {
									PersonalSuccessDays[Best[1]] -= 5;
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wasn't too careful.");
								}
							}

							if (player == Best[2]) {
								if (lifeCheck) {
									PersonalSuccessDays[Best[2]] += 4;
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most Profitable!");
								} else {
									PersonalSuccessDays[Best[2]] -= 1;
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave their life for the cause.");
								}
							}
							if (player == Best[3]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most injured!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave in to their injuries.");
								}
							}

							if (player == Best[4]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wariest!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most clueless.");
								}
							}

							if (player != Best[2] && player != Best[1]) {
								if (!PersonalSuccessDays.ContainsKey(StartOfRound.Instance.allPlayerScripts[i].playerUsername)) {
									PersonalSuccessDays.Add(StartOfRound.Instance.allPlayerScripts[i].playerUsername, 0);
								}
								if (lifeCheck) {
									PersonalSuccessDays[player] += 1;
								} else {
									PersonalSuccessDays[player] -= 1;
								}
							}
							StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Q: " + (TimeOfDay.Instance.quotaFulfilled) + ", G: " + GroupSuccessDays + ", P: " + PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[0].playerUsername]);
							
						}
					}
					Best[1] = "";
					Best[2] = "";
					Best[3] = "";
					Best[4] = "";
					BindValue(GroupSuccessDays, PersonalSuccessDays);
					ScrapThisRound = 0;
				}
			} else {
				int AllOnlinePlayers = StartOfRound.Instance.allPlayerScripts.Length;
				PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;
				PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;

				if (StartOfRound.Instance.connectedPlayersAmount > 0) {
					int[] Steps = new int[StartOfRound.Instance.allPlayerScripts.Length];
					int[] Profit = new int[StartOfRound.Instance.allPlayerScripts.Length];
					int[] Turns = new int[StartOfRound.Instance.allPlayerScripts.Length];
					int[] Damage = new int[StartOfRound.Instance.allPlayerScripts.Length];
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
						Profit[i] = stats[i].profitable;
						Damage[i] = stats[i].damageTaken;
						Steps[i] = stats[i].stepsTaken;
						Turns[i] = stats[i].turnAmount;
					}
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
						if (Steps.Min() == stats[i].stepsTaken) {
							Best[1] = scripts[i].playerUsername;
						}
						if (Steps.Max() == stats[i].stepsTaken && Profit.Max() == stats[i].profitable) {
							Best[2] = scripts[i].playerUsername;
						}
						if (Damage.Max() == stats[i].damageTaken) {
							Best[3] = scripts[i].playerUsername;
						}
						if (Turns.Max() == stats[i].turnAmount) {
							Best[4] = scripts[i].playerUsername;
						}
					}

					for (int i = 0; i < AllOnlinePlayers; i++) {
						bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;

						StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
							StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
							StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
							StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;

						if (stats[i].isActivePlayer) {
							if (scripts[i].playerUsername == Best[1] && scripts[i].playerUsername != Best[2]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wasn't too careful.");
								}
							}

							if (scripts[i].playerUsername == Best[2] && scripts[i].playerUsername != Best[1]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most Profitable!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave their life for the cause.");
								}
							}
							if (scripts[i].playerUsername == Best[3]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most injured!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave in to their injuries.");
								}
							}

							if (scripts[i].playerUsername == Best[4]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wariest!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most clueless.");
								}
							}
						}
					}
					Best[1] = "";
					Best[2] = "";
					Best[3] = "";
					Best[4] = "";
				}
			}
		}
	}
}
