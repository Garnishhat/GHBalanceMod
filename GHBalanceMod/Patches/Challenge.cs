using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace GHBalanceMod.Patches {
	internal class HostChallenge {
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

			HostInformation.BindValue(HostInformation.GroupSuccessDays, HostInformation.PersonalSuccessDays);
		}
		public static void DamagePlayer() {

		} // Not used currently.
	}

	internal class ClientChallenge {
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
