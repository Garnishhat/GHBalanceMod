using BepInEx;
using BepInEx.Logging;
using GHBalanceMod.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
            LOGGER.LogInfo("GH Balanced has awoken!!");
            
            harmony.PatchAll(typeof(BalanceModBase));
            // Initializes the default value
            harmony.PatchAll(typeof(PlayerStatsPatch));
            var settings = new ES3Settings(ES3.EncryptionType.AES, "Scribbles");

            PlayerPersonalStats.PersonalSuccessDays = ES3.Load("PSD", 0, settings);
            Group.TotalSuccessDays = ES3.Load("GSD", 0, settings);
        }

        void OnApplicationQuit() {
            ES3Settings settings = new ES3Settings(ES3.EncryptionType.AES, "Scribbles");
            try {
                ES3.Save("PSD", PlayerPersonalStats.PersonalSuccessDays, GameNetworkManager.Instance.currentSaveFileName, settings);
                ES3.Save("GSD", Group.TotalSuccessDays, GameNetworkManager.Instance.currentSaveFileName, settings);
            } catch (Exception arg) {
                Debug.LogError($"ERROR while saving [REDACTED] on local client! : {arg}");
            }
        }
        /*
        File stuff:

        
        
         */






    }
}
