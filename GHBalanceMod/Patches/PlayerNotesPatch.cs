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
        static string SetNumbers(int x) {
            string core = " (Max Stats: " + x + " (Health, Stamina))";
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

            float upOneSoloSpeed = Mathf.Max(2.0f + (groupSuccessDays + 1 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
            float upTwoSoloSpeed = Mathf.Max(2.0f + (groupSuccessDays + 2 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
            float downOneSoloSpeed = Mathf.Max(2.0f + (groupSuccessDays - 1 * 0.25f) / 5 * 100, 2.0f / 5 * 100);

            int upOneSoloHealth = Mathf.Max(40 + (groupSuccessDays + 1 * 5), 40);
            int upTwoSoloHealth = Mathf.Max(40 + (groupSuccessDays + 2 * 5), 40);
            int downOneSoloHealth = Mathf.Max(40 + (groupSuccessDays - 1 * 5), 40); // if 100% health, then... what. Okay...

            if (!justSold) {
                if (highlyProfitable) {
                    groupSuccessDays += 2;
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("REALLY Profitable!" + SetNumbers(upTwoSoloHealth));
                    }
                }
                if (profitable) {
                    groupSuccessDays++;
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable!" + SetNumbers(upOneSoloHealth));
                    }
                } else {
                    groupSuccessDays--;
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Failed." + SetNumbers(downOneSoloHealth));
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Base quota is: " + profitable);
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
                    float downFourSpeed = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] - 2 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float downTwoSpeed = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] - 2 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float downOneSpeed = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] - 1 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float neutralSpeed = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float upOneSpeed = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] + 1 * 0.25f) / 5 * 100, 2.0f / 5 * 100);
                    float upTwoSpeed = Mathf.Max(2.0f + (groupSuccessDays * 0.25f) + (personalSuccessDays[i] + 2 * 0.25f) / 5 * 100, 2.0f / 5 * 100);

                    int downFourHealth = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] - 2 * 5), 40);
                    int downTwoHealth = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] - 2 * 5), 40);
                    int downOneHealth = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] - 1 * 5), 40);
                    int neutralHealth = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] * 5), 40);
                    int upOneHealth = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] + 1 * 5), 40);
                    int upTwoHealth = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] + 2 * 5), 40);

                 
                    if (StartOfRound.Instance.connectedPlayersAmount > 0 && stats[i].isActivePlayer) {
                        if (stats[i].profitable == Mathf.Max(stats[j].profitable)) {
                            if (lifeCheck) {
                                personalSuccessDays[i] += 2;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Highly profitable!" + SetNumbers(upTwoHealth));
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Gave their life for the cause." + SetNumbers(neutralHealth));
                            }
                        }
                        // More steps less profit = laziest because they're doing twice the movement with none of the profit?
                        // least steps least profit = laziest because didn't even try?
                        if (stats[i].stepsTaken == Mathf.Min(stats[j].stepsTaken) && stats[i].profitable == Mathf.Min(stats[j].profitable)) {
                            if (lifeCheck) {
                                personalSuccessDays[i] -= 2;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!" + SetNumbers(downTwoHealth));
                            } else {
                                personalSuccessDays[i] -= 4;
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Paid for their laziness." + SetNumbers(downFourHealth));
                            }
                        }

                        if (stats[i].turnAmount == Mathf.Max(stats[j].turnAmount)) {
                            if (lifeCheck) {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Wariest!");
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most clueless.");
                            }
                        }

                        if (stats[i].damageTaken == Mathf.Max(stats[j].damageTaken)) {
                            if (lifeCheck) {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most injured!");
                            } else {
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Died from injuries.");
                            }
                        }

                        if (lifeCheck) {
                            personalSuccessDays[i] += 1;
                            StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Lived!" + SetNumbers(upOneHealth));
                        } else {
                            personalSuccessDays[i] -= 1;
                            StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Died." + SetNumbers(downOneHealth));
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