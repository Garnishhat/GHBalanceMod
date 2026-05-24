using BepInEx;
using BepInEx.Logging;
using GHBalanceMod.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHBalanceMod {
    
    [BepInPlugin(modGUID, modName, modVersion)]


    public class BalanceModBase : BaseUnityPlugin {
        private const string modGUID = "GHBalanceMod";
        private const string modName = "GH Balance Mod";
        private const string modVersion = "5.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        
        private static BalanceModBase Instance;

        internal ManualLogSource LOGGER;


        void Awake() {
            if (Instance == null) {
                Instance = this;
            }
            LOGGER = BepInEx.Logging.Logger.CreateLogSource(modGUID);

            LOGGER.LogInfo("GH Balanced has awoken!!");
            
            harmony.PatchAll(typeof(BalanceModBase));
            // Initializes the default value
            harmony.PatchAll(typeof(PlayerControllerBPatch));
            // Performs most of the logic behind the player movement
            harmony.PatchAll(typeof(PlayerNotesPatch));
            
        }



    }
}
