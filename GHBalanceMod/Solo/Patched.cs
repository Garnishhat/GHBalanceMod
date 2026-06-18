using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace GHBalanceMod.Solo {

	[HarmonyPatch(typeof(Debug))]
	public static class LoggerPatch {
		[HarmonyPatch(nameof(Debug.Log), new System.Type[] { typeof(object) })]
		[HarmonyPrefix]
		public static bool PrefixLog() {
			return false;
		}
	}
	
	[HarmonyPatch(typeof(GameNetworkManager))]
	internal class Save : NetworkBehaviour {
		[HarmonyPatch(nameof(GameNetworkManager.SaveGame))]
		[HarmonyPostfix]
		public static void Data() { 
			if (!StartOfRound.Instance.isChallengeFile) {
				SoloHelpers.Save();
			}
			SoloHelpers.ScrapCollected = 0;
			if (StartOfRound.Instance.currentLevel.planetHasTime) {
				SoloHelpers.DaysPassed++;
				BalanceModBase.Log("Days Passed: " + SoloHelpers.DaysPassed);
			}
			SoloHelpers.Bind();
		}
	}
	
	[HarmonyPatch(typeof(PlayerControllerB))]
	internal class Injection : NetworkBehaviour {
		[HarmonyPatch(nameof(PlayerControllerB.ConnectClientToPlayerObject))] 
		[HarmonyPostfix]
		public static void Start() { // I only need to load this when the host starts a game.
			if (!StartOfRound.Instance.isChallengeFile) {
				SoloHelpers.Load();
			}
			SoloHelpers.ScrapCollected = 0;
			SoloHelpers.Bind();
		}
	}

	[HarmonyPatch(typeof(StartOfRound))]
	internal class PlayerNotes : NetworkBehaviour {
		[HarmonyPatch(nameof(StartOfRound.WritePlayerNotes))]
		[HarmonyPrefix]
		public static void Write(ref bool ___allPlayersDead) { // Complicated, I'll get back to this later.
			SoloHelpers.DisplayNotes(___allPlayersDead);
		}
		[HarmonyPatch(nameof(StartOfRound.FirePlayersAfterDeadlineClientRpc))]
		[HarmonyPostfix]
		public static void Start() { // I only need to load this when the host starts a game.
			if (StartOfRound.Instance.IsServer) {
				SoloHelpers.QuotasMet = TimeOfDay.Instance.timesFulfilledQuota;
				SoloHelpers.Clear();
				BalanceModBase.Log("Attempting to clear!");
				SoloHelpers.Perks(FindObjectOfType<Terminal>());
			}
		}
	}

	[HarmonyPatch(typeof(HUDManager))]
	internal class DisplayScrapPatch : NetworkBehaviour {
		[HarmonyPatch(nameof(HUDManager.DisplayNewScrapFound))]
		[HarmonyPostfix]
		public static void Start() {
			if (StartOfRound.Instance.currentLevel.planetHasTime && !StartOfRound.Instance.inShipPhase && TimeOfDay.Instance.currentDayTime > 2f) {
				SoloHelpers.ScrapCollected++;
				BalanceModBase.Log($"Scrap collected!");
				BalanceModBase.Log($"Current Scrap : {SoloHelpers.ScrapCollected}, Required : {TimeOfDay.Instance.timesFulfilledQuota + 1}-{((TimeOfDay.Instance.timesFulfilledQuota + 1) * 4) - 1}, Minimum for high profit: {(TimeOfDay.Instance.timesFulfilledQuota + 1) * 4}+" );
			}
		}
	}





}
