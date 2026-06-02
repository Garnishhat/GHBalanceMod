using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
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
    public class PlayerPersonalStats : MonoBehaviour {
        public static int PersonalSuccessDays;
        public static int RoundDamage;

        /*
        Other things I'd love to add:
        Jump height (With an upper cap to prevent insanity)
        Movement speed (See previous statement)
        Carry weight? (Heavy items get lighter the more you succeed?)
         */

        private PlayerControllerB playerController;
        private void Awake() {
            playerController = GetComponent<PlayerControllerB>();
        }
    }
    public static class Group {
        public static int TotalSuccessDays;
    }
    public static class PlayerStatsPatch {

        public static string[] Best = new string[4];

        public static PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
        public static PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;

        static string SetNumbers(int x) {
            string core = "(Max Stats: " + x + "%)";
            return core;
        }

        [HarmonyPatch(typeof(StartOfRound))]
        [HarmonyPatch("WritePlayerNotes")]
        [HarmonyPrefix]
        public static void WriteNotes() {
            int AllOnlinePlayers = StartOfRound.Instance.allPlayerScripts.Length;

            PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
            PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;

            int GarnishScrapCount = TimeOfDay.Instance.profitQuota / 65; // (Minimum of 2)
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
                    Group.TotalSuccessDays += 2;
                } else {
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable!");
                    }
                    Group.TotalSuccessDays += 1;
                }
            } else {
                if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                    StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Failed.");
                    StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Base quota is " + GarnishScrapCount + " items.");
                }
                Group.TotalSuccessDays -= 2;
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
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;

                    if (stats[i].isActivePlayer) {
                        
                        if (scripts[i].playerUsername == Best[1] && scripts[i].playerUsername != Best[2]) {
                            if (lifeCheck) {
                                PlayerPersonalStats.PersonalSuccessDays -= 4;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!");
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add(SetNumbers(Mathf.Max(40 + ((PlayerPersonalStats.PersonalSuccessDays + Group.TotalSuccessDays) * 5), 40)));
                                Best[1] = "";
                            } else {
                                PlayerPersonalStats.PersonalSuccessDays -= 5;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wasn't too careful.");
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add(SetNumbers(Mathf.Max(40 + ((PlayerPersonalStats.PersonalSuccessDays + Group.TotalSuccessDays) * 5), 40)));
                                Best[1] = "";
                            }
                        }

                        if (scripts[i].playerUsername == Best[2] && scripts[i].playerUsername != Best[1]) {
                            if (lifeCheck) {
                                PlayerPersonalStats.PersonalSuccessDays += 4;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most Profitable!");
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add(SetNumbers(Mathf.Max(40 + ((PlayerPersonalStats.PersonalSuccessDays + Group.TotalSuccessDays) * 5), 40)));
                                Best[2] = "";
                            } else {
                                PlayerPersonalStats.PersonalSuccessDays -= 1;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave their life for the cause.");
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add(SetNumbers(Mathf.Max(40 + ((PlayerPersonalStats.PersonalSuccessDays + Group.TotalSuccessDays) * 5), 40)));
                                Best[2] = "";
                            }
                        }

                        if (scripts[i].playerUsername != Best[2] || scripts[i].playerUsername != Best[1]) {
                            if (lifeCheck) {
                                PlayerPersonalStats.PersonalSuccessDays += 1;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add(SetNumbers(Mathf.Max(40 + ((PlayerPersonalStats.PersonalSuccessDays + Group.TotalSuccessDays) * 5), 40)));
                            } else {
                                PlayerPersonalStats.PersonalSuccessDays -= 1;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add(SetNumbers(Mathf.Max(40 + ((PlayerPersonalStats.PersonalSuccessDays + Group.TotalSuccessDays) * 5), 40)));
                            }
                        }

                        if (scripts[i].playerUsername == Best[3]) {
                            if (lifeCheck) {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most injured!");
                                Best[3] = "";
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave in to their injuries.");
                                Best[3] = "";
                            }
                        }

                        if (scripts[i].playerUsername == Best[4]) {
                            if (lifeCheck) {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wariest!");
                                Best[4] = "";
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most clueless.");
                                Best[4] = "";
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void PlayerStats(ref float ___sprintTime) {
            if (StartOfRound.Instance.connectedPlayersAmount > 0) {
                ___sprintTime = Mathf.Max((2.0f + Group.TotalSuccessDays + PlayerPersonalStats.PersonalSuccessDays * 0.25f), 2.0f);
            } else {
                ___sprintTime = Mathf.Max((2.0f + Group.TotalSuccessDays * 0.25f), 2.0f);
            }
        }
        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        public static void DealDamage(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default) {
            PlayerPersonalStats.RoundDamage += damageNumber;

            PlayerStats stats = StartOfRound.Instance.gameStats.allPlayerStats[StartOfRound.Instance.localPlayerController.playerClientId];
            PlayerControllerB scripts = StartOfRound.Instance.allPlayerScripts[StartOfRound.Instance.localPlayerController.playerClientId];

            int max = Mathf.Max(40 + ((PlayerPersonalStats.PersonalSuccessDays + Group.TotalSuccessDays) * 5), 40);
            if (!stats.isActivePlayer || scripts.isPlayerDead || !scripts.AllowPlayerDeath()) {
                return;
            }

            if (max - PlayerPersonalStats.RoundDamage <= 0 && !scripts.criticallyInjured && damageNumber < 50) {
                scripts.health = 5;
            } else {
                scripts.health = Mathf.Clamp(max - PlayerPersonalStats.RoundDamage, 0, max);
            }
            HUDManager.Instance.SetCracksOnVisor(scripts.health);
            HUDManager.Instance.UpdateHealthUI(scripts.health);
            if (max - PlayerPersonalStats.RoundDamage <= 0) {
                bool spawnBody = deathAnimation != -1;
                PlayerPersonalStats.RoundDamage = 0;
                scripts.KillPlayer(force, spawnBody, causeOfDeath, deathAnimation);
            } else {
                if (max - PlayerPersonalStats.RoundDamage < max / 3 && !scripts.criticallyInjured) {
                    HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                    scripts.MakeCriticallyInjured(enable: true);
                } else {
                    if (damageNumber >= 10) {
                        scripts.sprintMeter = Mathf.Clamp(scripts.sprintMeter + (float)damageNumber / 125f, 0f, 1f);
                    }
                    if (callRPC) {
                        if (NetworkManager.Singleton.IsServer) {
                            scripts.DamagePlayerClientRpc(damageNumber, max - PlayerPersonalStats.RoundDamage);
                        } else {
                            scripts.DamagePlayerServerRpc(damageNumber, max - PlayerPersonalStats.RoundDamage);
                        }
                    }
                }
                if (fallDamage) {
                    HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.fallDamageSFX, 1f);
                    WalkieTalkie.TransmitOneShotAudio(scripts.movementAudio, StartOfRound.Instance.fallDamageSFX);
                    scripts.BreakLegsSFXClientRpc();
                } else if (hasDamageSFX) {
                    HUDManager.Instance.UIAudio.PlayOneShot(StartOfRound.Instance.damageSFX, 1f);
                }
            }
            StartOfRound.Instance.LocalPlayerDamagedEvent.Invoke();
            scripts.takingFallDamage = false;
            if (!scripts.inSpecialInteractAnimation && !scripts.twoHandedAnimation) {
                scripts.playerBodyAnimator.SetTrigger("Damage");
            }
            scripts.specialAnimationWeight = 1f;
            scripts.PlayQuickSpecialAnimation(0.7f);
            
        }
    }    
}