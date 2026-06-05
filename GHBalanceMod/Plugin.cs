using BepInEx;
using BepInEx.Logging;
using GHBalanceMod.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static IngamePlayerSettings;

namespace GHBalanceMod {
    
    [BepInPlugin(modGUID, modName, modVersion)]
    public class BalanceModBase : BaseUnityPlugin {
        private const string modGUID = "GHBalanceMod";
        private const string modName = "GH Balance Mod";
        private const string modVersion = "6.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        
        private static BalanceModBase Instance;

        internal ManualLogSource LOGGER;

        void Awake() {
            if (Instance == null) {
                Instance = this;
            }

            LOGGER = BepInEx.Logging.Logger.CreateLogSource(modGUID);
            LOGGER.LogInfo("GH Balanced has awoken! Loading saved data...");
            harmony.PatchAll(typeof(BalanceModBase));
            // Initializes the default value
            harmony.PatchAll(typeof(PlayerControllerPatches));
            harmony.PatchAll(typeof(StartOfRoundPatches));
            harmony.PatchAll(typeof(StartGameInjection));

        }
    }
}
