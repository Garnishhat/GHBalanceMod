using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace GHBalanceMod.Patches {
    internal class PlayerStatsPatch {

        public static int onlinePeoples = StartOfRound.Instance.allPlayerScripts.Length;
        public static PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
        public static PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;
        static string SetNumbers(int x) {
            string core = " (Max Stats: " + x + "%)";
            return core;
        }
        
        static int GroupSuccessDays(int added, bool display, bool reset) {
            int groupSuccessDays = 0;
            if (reset) {
                groupSuccessDays = 0;
                return groupSuccessDays;
            } else if (display) {
                return Mathf.Max(40 + (groupSuccessDays + added) * 5, 40);
            } else if (added != 0) {
                return groupSuccessDays += added;
            } else {
                return groupSuccessDays;
            } 
        }
        static int PersonalSuccessDays(int added, int which, bool display, bool reset) {
            int[] personalSuccessDays = new int[onlinePeoples];
            if (reset) {
                for (int i = 0; i < personalSuccessDays.Length; i++) {
                    personalSuccessDays[i] = 0;
                }
                return personalSuccessDays[which];
            } else if (display) {
                return Mathf.Max(40 + (GroupSuccessDays(0, true, false) + (personalSuccessDays[which] - added)) * 5, 40);
            } else if (added != 0) {
                return personalSuccessDays[which] += added;
            } else {
                return personalSuccessDays[which];
            }
        }

        static void ResetArray(int[] x) {

        }

        static void Log(string x) {
            BepInEx.Logging.Logger.CreateLogSource("GHBalanceMod").LogInfo(x);
        }

        public static int[] damageTaken = new int[onlinePeoples];

        [HarmonyPatch(typeof(StartOfRound))]
        [HarmonyPatch("WritePlayerNotes")]
        [HarmonyPrefix]
        public static void WriteNotes() {


            Log("String");

            int trueDeadline;
            switch (TimeOfDay.Instance.daysUntilDeadline) {
                case 0: trueDeadline = 4; break;
                case 1: trueDeadline = 3; break;
                case 2: trueDeadline = 2; break;
                case 3: trueDeadline = 1; break;
                default: trueDeadline = 0; break;
            }
            int totalDays = TimeOfDay.Instance.timesFulfilledQuota * 4 + trueDeadline;
            bool firstDay = totalDays == 1;

            int GarnishScrapCount = TimeOfDay.Instance.profitQuota / 65; // (Minimum of 2)
            int GarnishHighScrapCount = GarnishScrapCount * 2;

            bool profitable = StartOfRound.Instance.scrapCollectedLastRound >= GarnishScrapCount;
            bool highlyProfitable = StartOfRound.Instance.scrapCollectedLastRound >= GarnishHighScrapCount ||
                StartOfRound.Instance.currentShipItemCount <= StartOfRound.Instance.scrapCollectedLastRound;
            bool justSold = TimeOfDay.Instance.daysUntilDeadline == 3 && !firstDay;

            int upOneSoloHealth = GroupSuccessDays(1, true, false);
            int upThreeSoloHealth = GroupSuccessDays(3, true, false);
            int downOneSoloHealth = GroupSuccessDays(-1, true, false); 
            
            // Number count also doesn't check to see if it's the actual amount you know, since there's a ton of different combinations and stuff

            if (!justSold) {
                if (profitable) {
                    if (highlyProfitable) {
                        GroupSuccessDays(3, false, false);
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("REALLY Profitable!" + SetNumbers(upThreeSoloHealth));
                    } else {
                        GroupSuccessDays(1, false, false);
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable!" + SetNumbers(upOneSoloHealth));
                    }
                } else {
                    GroupSuccessDays(-1, false, false);
                    StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Failed." + SetNumbers(downOneSoloHealth));
                    StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Base quota is: " + GarnishScrapCount);
                }
            }

            if (StartOfRound.Instance.connectedPlayersAmount > 0) {
                for (int i = 0; i < onlinePeoples; i++) {

                    bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;

                    StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
                        StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;
                    // If min doesn't work then max is going to be next but with reverse order
                    int upFour = PersonalSuccessDays(4, i, true, false);
                    int downOne = PersonalSuccessDays(-1, i, true, false);
                    int upOne = PersonalSuccessDays(1, i, true, false);
                    int downThree = PersonalSuccessDays(-3, i, true, false);
                    int downFive = PersonalSuccessDays(-5, i, true, false);

                    int count = 0;
                    int count1 = 0;
                    int count2 = 0;
                    int count3 = 0;

                    ulong mostInjured = 0;
                    ulong mostLazy = 0;
                    ulong mostParanoid = 0;
                    ulong mostProfitable = 0;

                    ulong[] noAccolades = new ulong[onlinePeoples - 2];

                    for (int j = 0; j < onlinePeoples; j++) {
                        if (stats[i].damageTaken > stats[j].damageTaken && i != j) {
                            count++;
                            if (count == onlinePeoples - 1) {
                                mostInjured = scripts[i].actualClientId;
                                count = 0;
                            }
                        }

                        if (stats[i].profitable > stats[j].profitable && stats[i].stepsTaken > stats[j].stepsTaken && i != j) {
                            count1++;
                            if (count1 == onlinePeoples - 1) {
                                mostProfitable = scripts[i].actualClientId;
                                count1 = 0;
                            }
                        }

                        if (stats[j].stepsTaken > stats[i].stepsTaken && i != j) {
                            count2++;
                            if (count2 == onlinePeoples - 1) {
                                mostLazy = scripts[i].actualClientId;
                                count2 = 0;
                            }
                        }

                        if (stats[i].turnAmount > stats[j].turnAmount && i != j) {
                            count3++;
                            if (count3 == onlinePeoples - 1) {
                                mostParanoid = scripts[i].actualClientId;
                                count3 = 0;
                            }
                        }

                        if (scripts[i].actualClientId != mostProfitable &&
                            scripts[i].actualClientId != mostLazy) {
                            noAccolades[i] = scripts[i].actualClientId;
                        }
                    }


                    for (int j = 0; j < onlinePeoples; j++) {
                        if (stats[i].isActivePlayer) {
                            if (scripts[i].actualClientId == mostProfitable) {
                                if (lifeCheck) {
                                    PersonalSuccessDays(4, i, false, false);
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Profitable!" + SetNumbers(upFour));
                                } else {
                                    PersonalSuccessDays(-1, i, false, false);
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Self sacrificed." + SetNumbers(downOne));
                                }
                            }

                            if (scripts[i].actualClientId == mostLazy) {
                                if (lifeCheck) {
                                    PersonalSuccessDays(-3, i, false, false);
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!" + SetNumbers(downThree));
                                } else {
                                    PersonalSuccessDays(-5, i, false, false);
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Was too lazy." + SetNumbers(downFive));
                                }
                            }

                            if (scripts[i].actualClientId == mostParanoid) {
                                if (lifeCheck) {
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wariest!");
                                } else {
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most clueless.");
                                }
                            }

                            if (scripts[i].actualClientId == noAccolades[j]) {
                                if (lifeCheck) {
                                    PersonalSuccessDays(1, i, false, false);
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Lived!" + SetNumbers(upOne));
                                } else {
                                    PersonalSuccessDays(-1, i, false, false);
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Died!" + SetNumbers(downOne));
                                }
                            }

                            if (scripts[i].actualClientId == mostInjured) {
                                if (lifeCheck) {
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most injured!");
                                } else {
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Died from injuries.");
                                }
                            }
                        }

                    }
                }
                for (int i = 0; i < onlinePeoples; i++) {
                    StartOfRound.Instance.allPlayerScripts[i].sprintTime = PersonalSuccessDays(0, i, false, false);
                }
            } else {
                for (int i = 0; i < onlinePeoples; i++) {
                    StartOfRound.Instance.allPlayerScripts[i].sprintTime = GroupSuccessDays(0, false, false);
                }
            }
        }


        [HarmonyPatch(typeof(StartOfRound))]
        [HarmonyPatch("ResetStats")]
        [HarmonyPostfix]
        public static void Clear() {
            GroupSuccessDays(1, false, true);
            PersonalSuccessDays(1, 1, false, true);
        }

        [HarmonyPatch(typeof(StartOfRound))]
        [HarmonyPatch("ResetShip")]
        [HarmonyPostfix]
        public static void ChangeStats() {
            for (int i = 0; i < onlinePeoples; i++) {
                StartOfRound.Instance.allPlayerScripts[i].sprintTime = 2.0f;
                StartOfRound.Instance.allPlayerScripts[i].health = 40;
            }
        }

        [HarmonyPatch(typeof(PlayerControllerB))]
        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        public static void DealDamage(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default) {
            for (int i = 0; i < onlinePeoples; i++) {
                damageTaken[i] += damageNumber;
                
                PlayerStats stats = StartOfRound.Instance.gameStats.allPlayerStats[i];
                PlayerControllerB scripts = StartOfRound.Instance.allPlayerScripts[i];
                
                int max = Mathf.Max(40 + (PersonalSuccessDays(0, i, false, false) * 5) + (GroupSuccessDays(0,false, false) * 5), 40);
                
                if (!stats.isActivePlayer || scripts.isPlayerDead || !scripts.AllowPlayerDeath()) {
                    return;
                }

                if (max - damageTaken[i] <= 0 && !scripts.criticallyInjured && damageNumber < 50) {
                    scripts.health = 5;
                } else {
                    scripts.health = Mathf.Clamp(max - damageTaken[i], 0, max);
                }
                HUDManager.Instance.SetCracksOnVisor(scripts.health);
                HUDManager.Instance.UpdateHealthUI(scripts.health);
                if (max - damageTaken[i] <= 0) {
                    bool spawnBody = deathAnimation != -1;
                    damageTaken[i] = 0;
                    scripts.KillPlayer(force, spawnBody, causeOfDeath, deathAnimation);
                } else {
                    if (max - damageTaken[i] < max / 3 && !scripts.criticallyInjured) {
                        HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);
                        scripts.MakeCriticallyInjured(enable: true);
                    } else {
                    if (damageNumber >= 10) {
                            scripts.sprintMeter = Mathf.Clamp(scripts.sprintMeter + (float)damageNumber / 125f, 0f, 1f);
                    }
                    if (callRPC) {
                        if (NetworkManager.Singleton.IsServer) {
                                scripts.DamagePlayerClientRpc(damageNumber, max - damageTaken[i]);
                        } else {
                                scripts.DamagePlayerServerRpc(damageNumber, max - damageTaken[i]);
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
}
