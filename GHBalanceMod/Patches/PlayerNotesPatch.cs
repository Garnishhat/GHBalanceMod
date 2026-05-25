using GameNetcodeStuff;
using HarmonyLib;
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
        public static PlayerStats[] stats = StartOfRound.Instance.gameStats.allPlayerStats;
        public static PlayerControllerB[] scripts = StartOfRound.Instance.allPlayerScripts;
        static string SetNumbers(int x) {
            string core = " (Max Stats: " + x + "%)";
            return core;
        }

        [HarmonyPatch("WritePlayerNotes")]
        [HarmonyPrefix]
        public static void WriteNotes() {
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

            int upOneSoloHealth = Mathf.Max(40 + (groupSuccessDays + 1 * 5), 40);
            int upThreeSoloHealth = Mathf.Max(40 + (groupSuccessDays + 3 * 5), 40);
            int downOneSoloHealth = Mathf.Max(40 + (groupSuccessDays - 1 * 5), 40); // if 100% health, then... what. Okay...

            // Number count also doesn't check to see if it's the actual amount you know, since there's a ton of different combinations and stuff

            if (!justSold) {
                if (profitable) {
                    if (highlyProfitable) {
                        groupSuccessDays += 3;
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("REALLY Profitable!" + SetNumbers(upThreeSoloHealth));
                    } else {
                        groupSuccessDays++;
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable!" + SetNumbers(upOneSoloHealth));       
                    }
                } else {
                    groupSuccessDays--;
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

                    int upFour = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] + 4) * 5, 40);
                    int downOne = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] - 1) * 5, 40);
                    int upOne = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] + 1) * 5, 40);
                    int downThree = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] - 3) * 5, 40);
                    int downFive = Mathf.Max(40 + (groupSuccessDays * 5) + (personalSuccessDays[i] - 5) * 5, 40);

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
                            if (stats[i].damageTaken > stats[j].damageTaken) {
                                count++;
                                if (count == onlinePeoples - 1) {
                                    mostInjured = scripts[i].actualClientId;
                                    count = 0;
                                }
                            }

                            if (stats[i].profitable > stats[j].profitable && stats[i].stepsTaken > stats[j].stepsTaken) {
                                count1++;
                                if (count1 == onlinePeoples - 1) {
                                    mostProfitable = scripts[i].actualClientId;
                                    count1 = 0;
                                }
                            }

                            if (stats[j].stepsTaken > stats[i].stepsTaken) {
                                count2++;
                                if (count2 == onlinePeoples - 1) {
                                    mostLazy = scripts[i].actualClientId;
                                    count2 = 0;
                                }
                            }

                            if (stats[i].turnAmount > stats[j].turnAmount) {
                                count3++;
                                if (count3 == onlinePeoples - 1) {
                                    mostParanoid = scripts[i].actualClientId;
                                    count3 = 0;
                                }
                            }

                            if (scripts[i].actualClientId != mostProfitable ||
                                scripts[i].actualClientId != mostLazy &&
                                scripts[i].actualClientId != 0) {
                                noAccolades[i] = scripts[i].actualClientId;
                            }
                    }


                    for (int j = 0; j < onlinePeoples; j++) {
                        if (stats[i].isActivePlayer) {
                            if (scripts[i].actualClientId == mostProfitable) {
                                if (lifeCheck) {
                                    personalSuccessDays[i] += 4;
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Profitable!" + SetNumbers(upFour));
                                } else {
                                    personalSuccessDays[i] -= 1;
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Self sacrificed." + SetNumbers(downOne));
                                }
                            }

                            if (scripts[i].actualClientId == mostLazy) {
                                if (lifeCheck) {
                                    personalSuccessDays[i] -= 3;
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Laziest!" + SetNumbers(downThree));
                                } else {
                                    personalSuccessDays[i] -= 5;
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
                                    personalSuccessDays[i] += 1;
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Lived!"+ SetNumbers(upOne));
                                } else {
                                    personalSuccessDays[i] += 1;
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


                        // Was giving the player the laziest and the most profitable. This, of course, is not intended.
                        // Players check against their own stats to check if they're laziest or most profitable. Another way?
                        // Multiples of 8!?!?? 
                        // less stamina = health decrease

                        // Multiple people get the same 

                        // I want the most profitable player. 

                        // More steps less profit = laziest because they're doing twice the movement with none of the profit?
                        // least steps least profit = laziest because didn't even try?

                        /*
                        (string, int, string)[] upProfit = new (string, int, string)[onlinePeoples];
                        (string, int, string)[] upTurns = new (string, int, string)[onlinePeoples];
                        (string, int, string)[] upSteps = new (string, int, string)[onlinePeoples];
                        (string, int, string)[] upDamage = new (string, int, string)[onlinePeoples];

                        (string, int, string)[] downProfit = new (string, int, string)[onlinePeoples];
                        (string, int, string)[] downTurns = new (string, int, string)[onlinePeoples];
                        (string, int, string)[] downSteps = new (string, int, string)[onlinePeoples];
                        (string, int, string)[] downDamage = new (string, int, string)[onlinePeoples];
                         */

                        /*
                            upProfit[k] = (scripts[k].playerUsername, Mathf.Max(stats[k].profitable), "Most Profit");
                            upTurns[k] = (scripts[k].playerUsername, Mathf.Max(stats[k].turnAmount), "Most Paranoid");
                            upSteps[k] = (scripts[k].playerUsername, Mathf.Max(stats[k].stepsTaken), "Most Steps");
                            upDamage[k] = (scripts[k].playerUsername, Mathf.Max(stats[k].damageTaken), "Most Damage");

                            downProfit[k] = (scripts[k].playerUsername, Mathf.Min(stats[k].profitable), "Least Profit");
                            downTurns[k] = (scripts[k].playerUsername, Mathf.Min(stats[k].turnAmount), "Least Paranoid");
                            downSteps[k] = (scripts[k].playerUsername, Mathf.Min(stats[k].stepsTaken), "Least Steps");
                            downDamage[k] = (scripts[k].playerUsername, Mathf.Min(stats[k].damageTaken), "Least Damage");
                         */

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