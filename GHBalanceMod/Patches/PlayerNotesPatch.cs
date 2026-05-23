using GameNetcodeStuff;
using HarmonyLib;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHBalanceMod.Patches {
    [HarmonyPatch(typeof(StartOfRound))]
    internal class PlayerNotesPatch {

        public static int onlinePeoples = StartOfRound.Instance.allPlayerScripts.Length;
        public static int[] personalSuccessDays = new int[onlinePeoples];
        public static float groupSuccessDays = 0;

        [HarmonyPatch("WritePlayerNotes")]
        [HarmonyPrefix]
        public static void WritePlayerNotes() {
            int confirm = 0;
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

            int GarnishScrapCount = 4 * (TimeOfDay.Instance.timesFulfilledQuota + 1); // (Minimum of 4)
            int GarnishHighScrapCount = 8 * (TimeOfDay.Instance.timesFulfilledQuota + 1) + 5; // (Minimum of 13)

            bool profitable = StartOfRound.Instance.scrapCollectedLastRound >= GarnishScrapCount;
            bool highlyProfitable = StartOfRound.Instance.scrapCollectedLastRound >= GarnishHighScrapCount;
            bool justSold = TimeOfDay.Instance.daysUntilDeadline == 3 && !firstDay;
            if (firstDay) {
                ResetArray(personalSuccessDays);
                groupSuccessDays = 0;
            }

                if (!justSold) {
                if (profitable) {
                    groupSuccessDays++;
                    float personalSprintTime = StartOfRound.Instance.allPlayerScripts[0].sprintTime / 5 * 100;
                    string speedStats = " (" + personalSprintTime + "% normal duration)";
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Profitable! +8.33% Sprint duration!" + speedStats);
                    }
                } else {
                    groupSuccessDays--;
                    float personalSprintTime = StartOfRound.Instance.allPlayerScripts[0].sprintTime / 5 * 100;
                    string speedStats = " (" + personalSprintTime + "% normal duration)";
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Failed! -16.66% Sprint duration!" + speedStats);
                    }
                }

                if (highlyProfitable) {
                    groupSuccessDays += 2;
                    float personalSprintTime = StartOfRound.Instance.allPlayerScripts[0].sprintTime / 5 * 100;
                    string speedStats = " (" + personalSprintTime + "% normal duration)";
                    if (StartOfRound.Instance.connectedPlayersAmount == 0) {
                        StartOfRound.Instance.gameStats.allPlayerStats[0].playerNotes.Add("Super profitable! +16.66% Sprint duration!" + speedStats);
                    }
                }
            }

            for (int i = 0; i < onlinePeoples; i++) {
                for (int j = 0; j < onlinePeoples; j++) {
                    bool lifeCheck = !StartOfRound.Instance.allPlayerScripts[i].isPlayerDead && !StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame;
                    bool coreCheck = confirm == onlinePeoples && lifeCheck;
                    bool secondCheck = confirm == onlinePeoples;

                    float[] currentSpeed = new float[onlinePeoples];
                    StartOfRound.Instance.gameStats.allPlayerStats[i].isActivePlayer =
                        StartOfRound.Instance.allPlayerScripts[i].disconnectedMidGame ||
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerDead ||
                        StartOfRound.Instance.allPlayerScripts[i].isPlayerControlled;
                    string[] specials = new string[2];
                    if (StartOfRound.Instance.connectedPlayersAmount > 0 && stats[i].isActivePlayer && !stats[j].isActivePlayer) {
                        if (stats[i].profitable > stats[j].profitable) {
                            confirm = j; 
                            if (coreCheck) {
                                personalSuccessDays[i] += 2;
                                currentSpeed[i] = StartOfRound.Instance.allPlayerScripts[i].sprintTime;
                                string numbers = " (" + currentSpeed[i] / 5 * 100 + "% normal duration)";

                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most profitable! +16.66% Sprint duration!" + numbers);
                                specials[1] = StartOfRound.Instance.allPlayerScripts[i].playerUsername;
                                confirm = 0;
                            } else if (secondCheck) {
                                currentSpeed[i] = StartOfRound.Instance.allPlayerScripts[i].sprintTime;
                                string numbers = " (" + currentSpeed[i] / 5 * 100 + "% normal duration)";
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Most profitable, but you died. Duration normal." + numbers);
                                specials[1] = StartOfRound.Instance.allPlayerScripts[i].playerUsername;
                                confirm = 0;
                            }
                        }
                        // More steps less profit = laziest because they're doing twice the movement with none of the profit?
                        // least steps least profit = laziest because didn't even try?
                        if (stats[i].stepsTaken < stats[j].stepsTaken && stats[i].profitable < stats[j].profitable) {
                            confirm = j;
                            if (coreCheck) {
                                personalSuccessDays[i] -= 2;
                                currentSpeed[i] = StartOfRound.Instance.allPlayerScripts[i].sprintTime;
                                string numbers = " (" + currentSpeed[i] / 5 * 100 + "% normal duration)";
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("The laziest employee. -16.66% Sprint duration!" + numbers);
                                specials[2] = StartOfRound.Instance.allPlayerScripts[i].playerUsername;
                                confirm = 0;
                            } else if (secondCheck) {
                                personalSuccessDays[i] -= 4;
                                currentSpeed[i] = StartOfRound.Instance.allPlayerScripts[i].sprintTime;
                                string numbers = " (" + currentSpeed[i] / 5 * 100 + "% normal duration)";
                                StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("The laziest employee... died. -33.32% Sprint duration!" + numbers);
                                specials[2] = StartOfRound.Instance.allPlayerScripts[i].playerUsername;
                                confirm = 0;
                            }

                            if (stats[i].damageTaken > stats[j].damageTaken) {
                                confirm = j;
                                if (secondCheck) {
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("Sustained the most injuries.");
                                    confirm = 0;
                                }
                            }

                            if (stats[i].profitable > stats[j].turnAmount) {
                                confirm = j;
                                if (secondCheck) {
                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("The most paranoid employee.");
                                    confirm = 0;
                                }
                            }

                            if (specials[1] != null && specials[2] != null) {
                                if (!(StartOfRound.Instance.allPlayerScripts[i].playerUsername == specials[1] || StartOfRound.Instance.allPlayerScripts[i].playerUsername == specials[2]) && !lifeCheck) {

                                    personalSuccessDays[i] += 1;
                                    currentSpeed[i] = StartOfRound.Instance.allPlayerScripts[i].sprintTime;
                                    string numbers = " (" + currentSpeed[i] / 5 * 100 + "% normal duration)";

                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("You did alright! +8.33% Sprint duration!" + numbers);
                                    
                                } else if (!(StartOfRound.Instance.allPlayerScripts[i].playerUsername == specials[1] || StartOfRound.Instance.allPlayerScripts[i].playerUsername == specials[2])) {
                                    personalSuccessDays[i] -= 1;
                                    currentSpeed[i] = StartOfRound.Instance.allPlayerScripts[i].sprintTime;
                                    string numbers = " (" + currentSpeed[i] / 5 * 100 + "% normal duration)";

                                    StartOfRound.Instance.gameStats.allPlayerStats[i].playerNotes.Add("You did alright... until you died. -8.33% Sprint duration!" + numbers);
                                    
                                    specials[2] = StartOfRound.Instance.allPlayerScripts[i].playerUsername;
                                    confirm = 0;
                                }
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