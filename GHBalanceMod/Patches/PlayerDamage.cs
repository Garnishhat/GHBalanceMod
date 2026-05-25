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
    [HarmonyPatch(typeof(PlayerControllerB))]
    internal class PlayerDamage {
        public static int[] damageTaken = new int[PlayerNotesPatch.onlinePeoples];

        [HarmonyPatch("DamagePlayer")]
        [HarmonyPrefix]
        public static void DealDamage(int damageNumber, bool hasDamageSFX = true, bool callRPC = true, CauseOfDeath causeOfDeath = CauseOfDeath.Unknown, int deathAnimation = 0, bool fallDamage = false, Vector3 force = default) {
            for (int i = 0; i < PlayerNotesPatch.onlinePeoples; i++) {
                damageTaken[i] += damageNumber;
                
                PlayerStats stats = StartOfRound.Instance.gameStats.allPlayerStats[i];
                PlayerControllerB scripts = StartOfRound.Instance.allPlayerScripts[i];
                
                int max = Mathf.Max(40 + (PlayerNotesPatch.personalSuccessDays[i] * 5) + (PlayerNotesPatch.groupSuccessDays * 5), 40);

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
