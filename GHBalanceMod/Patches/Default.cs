using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace GHBalanceMod.Patches {
	/*
		public static int AllOnlinePlayers = StartOfRound.Instance.allPlayerScripts.Length;

		public static PlayerStats[] Stats = StartOfRound.Instance.gameStats.allPlayerStats;
		public static PlayerControllerB[] Scripts = StartOfRound.Instance.allPlayerScripts;

			bool Challenge = GameNetworkManager.Instance.currentSaveFileName == "LCChallengeFile";
	*/

	internal class HostInformation : NetworkBehaviour {
		public static int ScrapThisRound = 0;
		public static int GroupSuccessDays = 0;
		public static Dictionary<string, int> PersonalSuccessDays = new Dictionary<string, int>();
		public static string[] Best = new string[4];

		[ClientRpc] // Needs tweaks.
		public static void BindValue(int GroupDays, Dictionary<string, int> PersonalDays) {
			int Total = StartOfRound.Instance.connectedPlayersAmount + 1;
			bool Solo = StartOfRound.Instance.connectedPlayersAmount == 0;

			if (GameNetworkManager.Instance.currentSaveFileName != "LCChallengeFile") {
				if (TimeOfDay.Instance.timeUntilDeadline == 3 && TimeOfDay.Instance.timesFulfilledQuota == 0) {
					Clear();
				}
			} // Otherwise it just doesn't get saved so it doesn't matter if it gets cleared here or not.
			if (Solo) {
				StartOfRound.Instance.localPlayerController.health = Mathf.Max(40 + (GroupDays * 5), 40);
				StartOfRound.Instance.localPlayerController.sprintTime = Mathf.Max(2.0f + (GroupDays * 0.25f), 2.0f);
				StartOfRound.Instance.localPlayerController.sprintMeter = 1.0f;
			} else {
				for (int i = 0; i < Total; i++) {
					bool Local = StartOfRound.Instance.allPlayerScripts[i].IsOwner;
					StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer = Local && StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;

					if (StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer) {
						if (!PersonalDays.ContainsKey(StartOfRound.Instance.allPlayerScripts[i].playerUsername)) {
							PersonalDays.Add(StartOfRound.Instance.allPlayerScripts[i].playerUsername, 0);
						}
						int TotalDays = PersonalDays[StartOfRound.Instance.allPlayerScripts[i].playerUsername] + GroupDays;
						StartOfRound.Instance.allPlayerScripts[i].health = Mathf.Max(40 + (TotalDays * 5), 40);
						StartOfRound.Instance.allPlayerScripts[i].sprintTime = Mathf.Max(2.0f + (TotalDays * 0.25f), 2.0f);
						StartOfRound.Instance.allPlayerScripts[i].sprintMeter = 1.0f;
					}
				}
			}
		}
		public static void Clear() {
			GroupSuccessDays = 0;
			PersonalSuccessDays.Clear();
		}
		public static void Save() {
			ES3Settings settings = new ES3Settings(ES3.EncryptionType.AES, Encrypted.Password);
			bool Solo = StartOfRound.Instance.connectedPlayersAmount == 0;
			try {
				ES3.Save("GSD", GroupSuccessDays, "SDC" + GameNetworkManager.Instance.currentSaveFileName, settings);
			} catch (Exception e) {
				Debug.LogError("ERROR while saving [GSD] on local client! : " + e);
			}
			if (!Solo) {
				try {
					ES3.Save("PSD", PersonalSuccessDays, "SDC" + GameNetworkManager.Instance.currentSaveFileName, settings);
				} catch (Exception e) {
					Debug.LogError("ERROR while saving [PSD] on local client! : " + e);
				}
			}
		}
		public static void Load() {
			ES3Settings settings = new ES3Settings(ES3.EncryptionType.AES, Encrypted.Password);
			bool Solo = StartOfRound.Instance.connectedPlayersAmount == 0;
			if (ES3.KeyExists("GSD", "SDC" + GameNetworkManager.Instance.currentSaveFileName, settings)) {
				GroupSuccessDays = ES3.Load("GSD", "SDC" + GameNetworkManager.Instance.currentSaveFileName, 0, settings);
			}
			if (!Solo) {
				if (ES3.KeyExists("PSD", "SDC" + GameNetworkManager.Instance.currentSaveFileName, settings)) {
					PersonalSuccessDays = ES3.Load("PSD", "SDC" + GameNetworkManager.Instance.currentSaveFileName, new Dictionary<string, int>(), settings);
				}
			}
		}
		public static bool CalculatedBest() {
			int[] Steps = new int[StartOfRound.Instance.allPlayerScripts.Length];
			int[] Profit = new int[StartOfRound.Instance.allPlayerScripts.Length];
			int[] Turns = new int[StartOfRound.Instance.allPlayerScripts.Length];
			int[] Damage = new int[StartOfRound.Instance.allPlayerScripts.Length];

			bool CheckBest =
			(Best[1] != "" && Best[2] != "" && Best[3] != "" && Best[4] != "") ||
			(Best[1] != null && Best[2] != null && Best[3] != null && Best[4] != null);

			PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
			PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;

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

			if (CheckBest) {
				return true;
			} else {
				return false;
			}
		}
		
		public static void CalcGroupSuccessDays() {
			
		}
		public static void CalcPersonalSuccessDays() {
			
		}
	}
	
	internal class HostDefault {
		public static void PlayerNotes(in bool AllDead) {
			int GarnishScrapCount = (TimeOfDay.Instance.timesFulfilledQuota + 1) * 2; // (2, 4, 6, 8, etc.)
			int GarnishHighScrapCount = GarnishScrapCount * 2; // (4, 8, 12, 16, etc.)
			bool Profitable = HostInformation.ScrapThisRound >= GarnishScrapCount;
			bool HighlyProfitable = HostInformation.ScrapThisRound >= Mathf.Min(GarnishHighScrapCount, 30);
			bool Solo = StartOfRound.Instance.connectedPlayersAmount == 0;

			if (AllDead) {
				HostInformation.GroupSuccessDays -= 5;
				if (!Solo) {
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
						StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
							StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
							StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
							StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;

						if (StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer) {
							StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Failed. -10 points EACH!");
							if (!HostInformation.PersonalSuccessDays.ContainsKey(StartOfRound.Instance.allPlayerScripts[i].playerUsername)) {
								HostInformation.PersonalSuccessDays.Add(StartOfRound.Instance.allPlayerScripts[i].playerUsername, -5);
							} else {
								HostInformation.PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[i].playerUsername] -= 5;
							}
							StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Q: " + (TimeOfDay.Instance.quotaFulfilled) + ", G: " + HostInformation.GroupSuccessDays + ", P: " + HostInformation.PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[0].playerUsername]);
						}
					}
				}
			} else {
				if (HighlyProfitable) {
					if (StartOfRound.Instance.connectedPlayersAmount == 0) {
						StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("REALLY Profitable!");
					}
					HostInformation.GroupSuccessDays += (2 + TimeOfDay.Instance.timesFulfilledQuota);
				} else if (Profitable) {
					if (StartOfRound.Instance.connectedPlayersAmount == 0) {
						StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable!");
					}
					HostInformation.GroupSuccessDays += 2;
				} else {
					if (StartOfRound.Instance.connectedPlayersAmount == 0) {
						StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Failed.");
						StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Base quota is " + GarnishScrapCount + " items.");
					}
					HostInformation.GroupSuccessDays -= 4;
				}
				if (StartOfRound.Instance.connectedPlayersAmount == 0) {
					if (!HostInformation.PersonalSuccessDays.ContainsKey(StartOfRound.Instance.localPlayerController.playerUsername)) {
						HostInformation.PersonalSuccessDays.Add(StartOfRound.Instance.localPlayerController.playerUsername, 0);
					}
					StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Q: " + (TimeOfDay.Instance.quotaFulfilled) + ", G: " + HostInformation.GroupSuccessDays + ", P: " + HostInformation.PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[0].playerUsername]);
				}
				if (StartOfRound.Instance.connectedPlayersAmount > 0) {
					for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
						bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;

						StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
							StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
							StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
							StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled; // NOT saying "By anyone in particular." Just as long as it's not... not controlled, I guess.

						if (StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer) {
							string player = StartOfRound.Instance.allPlayerScripts[i].playerUsername;
							if (player == HostInformation.Best[1]) {
								if (lifeCheck) {
									HostInformation.PersonalSuccessDays[HostInformation.Best[1]] -= 4;
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!");
								} else {
									HostInformation.PersonalSuccessDays[HostInformation.Best[1]] -= 5;
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wasn't too careful.");
								}
							}

							if (player == HostInformation.Best[2]) {
								if (lifeCheck) {
									HostInformation.PersonalSuccessDays[HostInformation.Best[2]] += 4;
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most Profitable!");
								} else {
									HostInformation.PersonalSuccessDays[HostInformation.Best[2]] -= 1;
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave their life for the cause.");
								}
							}
							if (player == HostInformation.Best[3]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most injured!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave in to their injuries.");
								}
							}

							if (player == HostInformation.Best[4]) {
								if (lifeCheck) {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wariest!");
								} else {
									StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most clueless.");
								}
							}

							if (player != HostInformation.Best[2] && player != HostInformation.Best[1]) {
								if (!HostInformation.PersonalSuccessDays.ContainsKey(StartOfRound.Instance.allPlayerScripts[i].playerUsername)) {
									HostInformation.PersonalSuccessDays.Add(StartOfRound.Instance.allPlayerScripts[i].playerUsername, 0);
								}
								if (lifeCheck) {
									HostInformation.PersonalSuccessDays[player] += 1;
								} else {
									HostInformation.PersonalSuccessDays[player] -= 1;
								}
							}
							StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Q: " + (TimeOfDay.Instance.quotaFulfilled) + ", G: " + HostInformation.GroupSuccessDays + ", P: " + HostInformation.PersonalSuccessDays[StartOfRound.Instance.allPlayerScripts[0].playerUsername]);
						}
					}
				}
			}
			HostInformation.ScrapThisRound = 0;
			HostInformation.Save();
		}
		public static void IncreaseScrapCount() {
			if (StartOfRound.Instance.currentLevel.planetHasTime && !StartOfRound.Instance.inShipPhase) {
				HostInformation.ScrapThisRound++;
			}
		}
		public static void StartGame() {
			Array.Clear(HostInformation.Best, 0, 4);
			if (HostInformation.ScrapThisRound != 0) {
				// Cheater cheater REDDIT READER!!
				HostInformation.GroupSuccessDays -= 15;
			}
			HostInformation.ScrapThisRound = 0;

			HostInformation.Load();
			HostInformation.BindValue(HostInformation.GroupSuccessDays, HostInformation.PersonalSuccessDays);
		}
		public static void DamagePlayer() {

		} // Not used currently.
	}

	internal class ClientDefault {
		public static void PlayerNotes(in bool AllDead) {
			if (AllDead) {
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
					StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
					StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
					StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
					StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;
					if (StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer) {
						StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Failed. -10 points EACH!");
					}
				}
			} else {
				for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
					bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead &&
					!StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;

					StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
						StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
						StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
						StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;

					if (StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer) {
						if (StartOfRound.Instance.allPlayerScripts[i].playerUsername == HostInformation.Best[1] &&
						StartOfRound.Instance.allPlayerScripts[i].playerUsername != HostInformation.Best[2]) {
							if (lifeCheck) {
								StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!");
							} else {
								StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wasn't too careful.");
							}
						}

						if (StartOfRound.Instance.allPlayerScripts[i].playerUsername == HostInformation.Best[2] &&
						StartOfRound.Instance.allPlayerScripts[i].playerUsername != HostInformation.Best[1]) {
							if (lifeCheck) {
								StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most Profitable!");
							} else {
								StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave their life for the cause.");
							}
						}
						if (StartOfRound.Instance.allPlayerScripts[i].playerUsername == HostInformation.Best[3]) {
							if (lifeCheck) {
								StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most injured!");
							} else {
								StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave in to their injuries.");
							}
						}

						if (StartOfRound.Instance.allPlayerScripts[i].playerUsername == HostInformation.Best[4]) {
							if (lifeCheck) {
								StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wariest!");
							} else {
								StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most clueless.");
							}
						}

					}
				}
			}
		}
		public static void DamagePlayer() {

		} // Not used currently.
	}
}
