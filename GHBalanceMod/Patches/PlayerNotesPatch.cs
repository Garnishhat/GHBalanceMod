using GameNetcodeStuff;
using HarmonyLib;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GHBalanceMod.Patches {
    [HarmonyPatch(typeof(StartOfRound))]
    internal class PlayerNotesPatch {

        public static int onlinePeoples = StartOfRound.Instance.allPlayerScripts.Length;
        public static int[] personalSuccessDays = new int[onlinePeoples];
        public static int groupSuccessDays = 0;
        static string SetNumbers(float x) {
            string core = " (At " + x + "% Vanilla)";
            return core;
        }
        [HarmonyPatch("WritePlayerNotes")]
        [HarmonyPrefix]
        public static void WritePlayerNotes() {
            PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
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

            float upOneSolo = Mathf.Max(2.0f + (groupSuccessDays + 1 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
            float upTwoSolo = Mathf.Max(2.0f + (groupSuccessDays + 2 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
            float downOneSolo = Mathf.Max(2.0f + (groupSuccessDays - 1 * 0.25f) / 5 * 100, 2.0f / 5 * 100);

            if (!justSold) {
                if (profitable) {
                    groupSuccessDays++;
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable! + 5% Stamina!" + SetNumbers(upOneSolo));
                    }
                } else {
                    groupSuccessDays--;
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Failed! - 5% Stamina!" + SetNumbers(downOneSolo));
                    }
                }

                if (highlyProfitable) {
                    groupSuccessDays += 2;
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Super profitable! + 10% Stamina!" + SetNumbers(upTwoSolo));
                    }
                }
            }

            for (int i = 0; i < onlinePeoples; i++) {
                for (int j = 0; j < onlinePeoples; j++) {
                    bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;

                    StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
                        StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;
                    // If min doesn't work then max is going to be next but with reverse order
                    float downFour = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] - 2 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float downTwo = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] - 2 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float downOne = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] - 1 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float neutral = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float upOne = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] + 1 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float upTwo = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] + 2 * 0.25f) / 5 * 100, 2.0f / 5 * 100);

                    if (StartOfRound.Instance.connectedPlayersAmount > 0 && stats[i].isActivePlayer) {
                        if (stats[i].profitable == Mathf.Max(stats[j].profitable)) {
                            if (lifeCheck) {
                                personalSuccessDays[i] += 2;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most profitable! + 10% Stamina!" + SetNumbers(upTwo));
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most profitable, but you died. + 0% Stamina." + SetNumbers(neutral));
                            }
                        }
                        // More steps less profit = laziest because they're doing twice the movement with none of the profit?
                        // least steps least profit = laziest because didn't even try?
                        if (stats[i].stepsTaken == Mathf.Min(stats[j].stepsTaken) && stats[i].profitable == Mathf.Min(stats[j].profitable)) {
                            if (lifeCheck) {
                                personalSuccessDays[i] -= 2;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("The laziest employee. - 10% Stamina!" + SetNumbers(downTwo));
                            } else {
                                personalSuccessDays[i] -= 4;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("The laziest employee... died. - 20% Stamina!" + SetNumbers(downFour));
                            }
                        }

                        if (stats[i].playerNotes.Count == 0) {
                            if (lifeCheck) {
                                personalSuccessDays[i] += 1;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("You did alright! + 5% Stamina!" + SetNumbers(upOne));
                            } else {
                                personalSuccessDays[i] -= 1;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("You did alright... until you died. - 5% Stamina!" + SetNumbers(downOne));
                            }
                        }

                        if (stats[i].turnAmount == Mathf.Max(stats[j].turnAmount)) {
                            if (lifeCheck) {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("The wariest employee.");
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("The wariest and least careful employee.");
                            }
                        }

                        if (stats[i].damageTaken == Mathf.Max(stats[j].damageTaken)) {
                            if (lifeCheck) {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Sustained the most injuries.");
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave in from having the most injuries.");
                            }
                        }
                    }
                }
            }

        }

        static void ResetArray(int[] x) {
            for (int i = 0; i < x.Length; i++) {
                x[i] = 0;
            }
        }

    }
}