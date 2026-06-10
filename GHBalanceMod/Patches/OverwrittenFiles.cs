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
		[HarmonyPatch("DisplayNewScrapFound")]
		[HarmonyPostfix]
		public static void CollectedScrap() {
			if (StartOfRound.Instance.IsServer) {
				if (GameNetworkManager.Instance.currentSaveFileName == "LCChallengeFile") {
					HostChallenge.IncreaseScrapCount();
				} else {
					HostDefault.IncreaseScrapCount();
				}
			}
		}
	}

	[HarmonyPatch(typeof(StartMatchLever))]
	internal class StartInjection : NetworkBehaviour {
		[HarmonyPatch("PullLever")]
		[HarmonyPostfix]
		public static void Injection() {
			if (StartOfRound.Instance.IsServer) {
				if (GameNetworkManager.Instance.currentSaveFileName == "LCChallengeFile") {
					HostChallenge.StartGame();
				} else {
					HostDefault.StartGame();
				}
			}
		}
	}
	
	[HarmonyPatch(typeof(StartOfRound))]
	internal class PlayerNotes : NetworkBehaviour {

		[HarmonyPatch("WritePlayerNotes")]
		[HarmonyPrefix]
		[ClientRpc]
		public static void WriteNotes(ref bool ___allPlayersDead) {
			if (HostInformation.CalculatedBest() || StartOfRound.Instance.connectedPlayersAmount == 0) {
				if (StartOfRound.Instance.IsServer) {
					if (GameNetworkManager.Instance.currentSaveFileName != "LCChallengeFile") {
						HostDefault.PlayerNotes(in ___allPlayersDead);
					} else {
						HostChallenge.PlayerNotes(in ___allPlayersDead);
					}
				} else {
					if (GameNetworkManager.Instance.currentSaveFileName != "LCChallengeFile") {
						ClientDefault.PlayerNotes(in ___allPlayersDead);
					} else {
						ClientChallenge.PlayerNotes(in ___allPlayersDead);
					}
				}
			}	
		}
	}
}
