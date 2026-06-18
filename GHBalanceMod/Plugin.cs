using BepInEx;
using BepInEx.Logging;
using GHBalanceMod.Solo;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace GHBalanceMod {
    
    [BepInPlugin(modGUID, modName, modVersion)]
    public class BalanceModBase : BaseUnityPlugin {
        private const string modGUID = "GHBalances";
        private const string modName = "GH Balances";
        private const string modVersion = "6.1.0";

        private readonly Harmony harmony = new Harmony(modGUID);
		// If "Testing enviornment" is done, I comment these two out and then nothing happens.
		public static void Log(string input) {
			ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(modName);
			Logger.LogFatal(input);
		}
		public static void LogError(string input, Exception e) {
			ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(modName);
			Logger.LogFatal("ERROR while " + input + "! : " + e);
		}

		private static BalanceModBase Instance;
		void Awake() {
			if (Instance == null) {
				Instance = this;
			}
			Log("GH Balanced has awoken! These logs are only cosmetically red to discern between other mods.");
			try {
				harmony.PatchAll(typeof(Injection)); // Beginning
				harmony.PatchAll(typeof(PlayerNotes)); // Middle
				harmony.PatchAll(typeof(Save)); // End (Or New)
				harmony.PatchAll(typeof(DisplayScrapPatch)); // Display Scrap
				harmony.PatchAll(typeof(BalanceModBase));
				// I also uncomment this one.
				// harmony.PatchAll(typeof(LoggerPatch)); // Mute console
			} catch (Exception e) {
				LogError("patching notes", e);
			}
		}
	}
}
