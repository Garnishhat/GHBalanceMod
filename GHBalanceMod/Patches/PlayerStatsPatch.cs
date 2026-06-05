using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using HarmonyLib.Tools;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
namespace GHBalanceMod.Patches {

    /*
        Other things I'd love to add:
        Jump height (With an upper cap to prevent insanity)
        Movement speed (See previous statement)
        Carry weight? (Heavy items get lighter the more you succeed?)
     */
    public static class PSM {


        public static string[] Best = new string[4];

        public static PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
        public static PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;
        public static Dictionary<ulong, int> PersonalSuccess = new Dictionary<ulong, int>();
        public static Dictionary<ulong, int> RoundDamage = new Dictionary<ulong, int>();
        public static int TotalSuccessDays = 0;

        public static void BindSuccessToPlayer(ulong player, int myCustomValue) {
            PlayerControllerB controller = GameNetworkManager.Instance.localPlayerController;
            if (controller == null || !controller.isPlayerControlled) {
                return;
            } else {
                if (!PersonalSuccess.ContainsKey(player)) {
                    PersonalSuccess.Add(player, myCustomValue);
                } else {
                    PersonalSuccess[player] = myCustomValue;
                }
            }
        }
        public static int GetSuccessFromPlayer(ulong player) {
            PlayerControllerB controller = GameNetworkManager.Instance.localPlayerController;
            if (controller == null || !controller.isPlayerControlled) {
                return 0;
            } else {
                if (!PersonalSuccess.ContainsKey(player)) {
                    PersonalSuccess.Add(player, 0);
                    return 0;
                } else if (PersonalSuccess.TryGetValue(player, out int value)) {
                    return value;
                } else {
                    return 0;
                }
            }
        }
        public static void BindDamageToPlayer(ulong player, int myCustomValue) {
            PlayerControllerB controller = GameNetworkManager.Instance.localPlayerController;
            if (controller == null || !controller.isPlayerControlled || controller.isPlayerDead) {
                return;
            } else {
                if (!RoundDamage.ContainsKey(player)) {
                    RoundDamage.Add(player, myCustomValue);
                } else {
                    RoundDamage[player] = myCustomValue;
                }
            }
        }
        public static int GetDamageFromPlayer(ulong player) {
            PlayerControllerB controller = GameNetworkManager.Instance.localPlayerController;
            if (controller == null || !controller.isPlayerControlled || controller.isPlayerDead) {
                return 0;
            } else {
                if (!RoundDamage.ContainsKey(player)) {
                    RoundDamage.Add(player, 0);
                    return 0;
                } else if (RoundDamage.TryGetValue(player, out int value)) {
                    return value;
                } else {
                    return 0;
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerPatches {

        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        public static void DealDamage(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default) {
            ulong player = GameNetworkManager.Instance.localPlayerController.actualClientId;

            PSM.BindDamageToPlayer(player, PSM.GetDamageFromPlayer(player) + damageNumber);
            PlayerControllerB controller = GameNetworkManager.Instance.localPlayerController;

            int max = Mathf.Max(40 + ((PSM.GetSuccessFromPlayer(player) + PSM.TotalSuccessDays) * 5), 40);
            int currentHealth = max - PSM.GetDamageFromPlayer(player);
            if (controller == null || controller.isPlayerDead || !controller.AllowPlayerDeath()) {
                return;
            } else {
                if (currentHealth <= 0 && !controller.criticallyInjured && damageNumber < 50) {
                    controller.health = 5;
                } else {
                    controller.health = Mathf.Clamp((int) currentHealth, 0, max);
                }
                HUDManager.Instance.SetCracksOnVisor(controller.health);
                HUDManager.Instance.UpdateHealthUI(controller.health);
                if (currentHealth <= 0) {
                    bool spawnBody = deathAnimation != -1;
                    PSM.BindDamageToPlayer(player, 0);
                    controller.KillPlayer(force, spawnBody, causeOfDeath, deathAnimation);
                } else {
                    if (currentHealth < max / 3 && !controller.criticallyInjured) {
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                        controller.MakeCriticallyInjured(enable: true);
                    } else {
                        if (damageNumber >= 10) {
                            controller.sprintMeter = Mathf.Clamp(controller.sprintMeter + (float)damageNumber / 125f, 0f, 1f);
                        }
                        if (callRPC) {
                            if (NetworkManager.Singleton.IsServer) {
                                controller.DamagePlayerClientRpc(damageNumber, currentHealth);
                            } else {
                                controller.DamagePlayerServerRpc(damageNumber, max - PSM.GetDamageFromPlayer(player));
                            }
                        }
                    }
                    if (fallDamage) {
                        HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
                        WalkieTalkie.TransmitOneShotAudio(controller.movementAudio, StartOfRound.Instance.fallDamageSFX);
                        controller.BreakLegsSFXClientRpc();
                    } else if (hasDamageSFX) {
                        HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.damageSFX, 1f);
                    }
                }
                StartOfRound.Instance.LocalPlayerDamagedEvent.Invoke();
                controller.takingFallDamage = false;
                if (!controller.inSpecialInteractAnimation && !controller.twoHandedAnimation) {
                    controller.playerBodyAnimator.SetTrigger("Damage");
                }
                controller.specialAnimationWeight = 1f;
                controller.PlayQuickSpecialAnimation(0.7f);
            }

            

        }
    }
    // Player notes and speed is broken... But start of round patches is different, so that's odd.
    [HarmonyPatch(typeof(StartMatchLever))]
    internal class StartGameInjection {
        static ManualLogSource LOGGER;
        [HarmonyPatch("StartGame")]
        [HarmonyPostfix]
        public static void Injection() {
            LOGGER = BepInEx.Logging.Logger.CreateLogSource("GHBalanceMod");
            PlayerControllerB controller = GameNetworkManager.Instance.localPlayerController;
            ulong player = controller.actualClientId;
            bool solo = StartOfRound.Instance.connectedPlayersAmount != 0;
            if (controller == null || !controller.isPlayerControlled || controller.isPlayerDead) {
                return;
            } else {
                if (solo) {
                    controller.sprintTime = Mathf.Max((2.0f + PSM.TotalSuccessDays * 0.25f), 2.0f);
                } else {
                    controller.sprintTime = Mathf.Max((2.0f + PSM.TotalSuccessDays + PSM.GetSuccessFromPlayer(player) * 0.25f), 2.0f);
                }
            }
            if (ES3.KeyExists("PSD", GameNetworkManager.Instance.currentSaveFileName) && ES3.KeyExists("GSD", GameNetworkManager.Instance.currentSaveFileName)) {
                try {
                    PSM.PersonalSuccess = ES3.Load("PSD", new Dictionary<ulong, int>());
                    PSM.TotalSuccessDays = ES3.Load("GSD", 0);
                    LOGGER.LogInfo("Data loaded.");
                } catch (Exception e) {
                    LOGGER.LogInfo("ERROR while loading [REDACTED] on local client! : " + e);
                }
            }
        }
    }
    
    [HarmonyPatch(typeof(StartOfRound))]
    internal class StartOfRoundPatches {
        [HarmonyPatch("WritePlayerNotes")]
        [HarmonyPrefix]
        public static void WriteNotes() {
            int AllOnlinePlayers = StartOfRound.Instance.allPlayerScripts.Length;

            PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
            PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;

            int GarnishScrapCount = (TimeOfDay.Instance.timesFulfilledQuota + 1) * 2; // (Minimum of 2)
            int GarnishHighScrapCount = GarnishScrapCount * GarnishScrapCount;
            bool profitable = StartOfRound.Instance.scrapCollectedLastRound >= GarnishScrapCount;
            bool highlyProfitable = StartOfRound.Instance.scrapCollectedLastRound >= GarnishHighScrapCount ||
            StartOfRound.Instance.scrapCollectedLastRound >= 30;
            // Number count also doesn't check to see if it's the actual amount you know, since there's a ton of different combinations and stuff
            if (profitable) {
                if (highlyProfitable) {
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("REALLY Profitable!");
                    }
                    PSM.TotalSuccessDays += 2;
                } else {
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable!");
                    }
                    PSM.TotalSuccessDays += 1;
                }
            } else {
                if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                    StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Failed.");
                    StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Base quota is " + GarnishScrapCount + " items.");
                }
                PSM.TotalSuccessDays -= 2;
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
                        PSM.Best[1] = scripts[i].playerUsername;
                    }
                    if (Steps.Max() == stats[i].stepsTaken && Profit.Max() == stats[i].profitable) {
                        PSM.Best[2] = scripts[i].playerUsername;
                    }
                    if (Damage.Max() == stats[i].damageTaken) {
                        PSM.Best[3] = scripts[i].playerUsername;
                    }
                    if (Turns.Max() == stats[i].turnAmount) {
                        PSM.Best[4] = scripts[i].playerUsername;
                    }
                }

                for (int i = 0; i < AllOnlinePlayers; i++) {
                    bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;

                    StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
                        StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;

                    if (stats[i].isActivePlayer) {
                        ulong player = StartOfRound.Instance.localPlayerController.actualClientId;
                        if (scripts[i].playerUsername == PSM.Best[1] && scripts[i].playerUsername != PSM.Best[2]) {
                            if (lifeCheck) {
                                PSM.BindSuccessToPlayer(player, PSM.GetSuccessFromPlayer(player) - 4);
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!");
                                PSM.Best[1] = "";
                            } else {
                                PSM.BindSuccessToPlayer(player, PSM.GetSuccessFromPlayer(player) - 5);
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wasn't too careful.");                                
                                PSM.Best[1] = "";
                            }
                        }

                        if (scripts[i].playerUsername == PSM.Best[2] && scripts[i].playerUsername != PSM.Best[1]) {
                            if (lifeCheck) {
                                PSM.BindSuccessToPlayer(player, PSM.GetSuccessFromPlayer(player) + 4);
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most Profitable!");
                                PSM.Best[2] = "";
                            } else {
                                PSM.BindSuccessToPlayer(player, PSM.GetSuccessFromPlayer(player) - 1);
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave their life for the cause.");
                                PSM.Best[2] = "";
                            }
                        }

                        if (scripts[i].playerUsername != PSM.Best[2] || scripts[i].playerUsername != PSM.Best[1]) {
                            if (lifeCheck) {
                                PSM.BindSuccessToPlayer(player, PSM.GetSuccessFromPlayer(player) + 1);
                            } else {
                                PSM.BindSuccessToPlayer(player, PSM.GetSuccessFromPlayer(player) - 1);
                            }
                        }

                        if (scripts[i].playerUsername == PSM.Best[3]) {
                            if (lifeCheck) {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most injured!");
                                PSM.Best[3] = "";
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave in to their injuries.");
                                PSM.Best[3] = "";
                            }
                        }

                        if (scripts[i].playerUsername == PSM.Best[4]) {
                            if (lifeCheck) {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wariest!");
                                PSM.Best[4] = "";
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most clueless.");
                                PSM.Best[4] = "";
                            }
                        }
                        StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("(Stats now: " + Mathf.Max(40 + ((PSM.GetSuccessFromPlayer(player) + PSM.TotalSuccessDays) * 5), 40) + "%)");
                    }
                }
            }
            try {
                ES3.Save("PSD", PSM.PersonalSuccess, GameNetworkManager.Instance.currentSaveFileName);
                ES3.Save("GSD", PSM.TotalSuccessDays, GameNetworkManager.Instance.currentSaveFileName);
            } catch (Exception e) {
                Debug.LogError("ERROR while saving [REDACTED] on local client! : " + e);
            }
        }
        [HarmonyPatch("ResetShip")]
        [HarmonyPostfix]
        public static void Clear() {
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
                ulong player = StartOfRound.Instance.allPlayerScripts[i].actualClientId;
                PSM.BindDamageToPlayer(player, 0);
                PSM.BindSuccessToPlayer(player, 0);
            }
            PSM.TotalSuccessDays = 0;
            try {
                ES3.Save("PSD", PSM.PersonalSuccess, GameNetworkManager.Instance.currentSaveFileName);
                ES3.Save("GSD", PSM.TotalSuccessDays, GameNetworkManager.Instance.currentSaveFileName);
            } catch (Exception e) {
                Debug.LogError("ERROR while saving [REDACTED] on local client! : " + e);
            }
        }
    }
    

}