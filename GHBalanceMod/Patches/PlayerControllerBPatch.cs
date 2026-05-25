using GameNetcodeStuff;
using TMPro;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace GHBalanceMod.Patches {
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerControllerBPatch {
        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void ChangeStats(ref float ___sprintTime) {
            if (StartOfRound.Instance.connectedPlayersAmount > 0) {
                for (int i = 0; i < PlayerNotesPatch.onlinePeoples; i++) {
                    float sprint = Mathf.Max(2.0f + (PlayerNotesPatch.personalSuccessDays[i] * 0.25f) + (PlayerNotesPatch.groupSuccessDays * 0.25f), 2.0f);
                    StartOfRound.Instance.allPlayerScripts[i].sprintTime = sprint;
                }
            } else {
                ___sprintTime = Mathf.Max(2.0f + (PlayerNotesPatch.groupSuccessDays * 0.25f), 2.0f);
            }
        }
    }
}
