using System;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace StarlancerAIFix.Patches
{
    public class AIFix
    {
        private static GameObject[] outsideAINodes;
        private static GameObject[] insideAINodes;
        private static Vector3[] outsideNodePositions;
        private static Vector3[] insideNodePositions;

        private static void CacheAINodes()
        {
            if (outsideAINodes == null || outsideAINodes.Length == 0 || outsideAINodes[0] == null)
            {
                outsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
                if (outsideAINodes == null || outsideAINodes.Length == 0)
                {
                    Debug.LogError("No OutsideAINodes found!");
                }
                else
                {
                    outsideNodePositions = new Vector3[outsideAINodes.Length];
                    for (int i = 0; i < outsideAINodes.Length; i++)
                    {
                        outsideNodePositions[i] = outsideAINodes[i]?.transform.position ?? Vector3.zero;
                    }
                }
            }

            if (insideAINodes == null || insideAINodes.Length == 0 || insideAINodes[0] == null)
            {
                insideAINodes = GameObject.FindGameObjectsWithTag("AINode");
                if (insideAINodes == null || insideAINodes.Length == 0)
                {
                    Debug.LogError("No AINodes found!");
                }
                else
                {
                    insideNodePositions = new Vector3[insideAINodes.Length];
                    for (int i = 0; i < insideAINodes.Length; i++)
                    {
                        insideNodePositions[i] = insideAINodes[i]?.transform.position ?? Vector3.zero;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EnemyAI), "Start")]
        [HarmonyPostfix]
        private static void AIFixPatch(EnemyAI __instance)
        {
            CacheAINodes();
            if (outsideNodePositions == null || insideNodePositions == null)
            {
                Debug.LogError("Node positions not initialized!");
                return;
            }

            Vector3 position = __instance.transform.position;
            Vector3 closestOutsideNode = GetClosestNode(outsideNodePositions, position);
            Vector3 closestInsideNode = GetClosestNode(insideNodePositions, position);

            if (!__instance.isOutside && IsCloser(closestOutsideNode, closestInsideNode, position))
            {
                SwitchToOutsideAI(__instance);
            }
            else if (__instance.isOutside && !IsCloser(closestOutsideNode, closestInsideNode, position))
            {
                SwitchToInsideAI(__instance);
            }
        }

        private static Vector3 GetClosestNode(Vector3[] nodePositions, Vector3 enemyPos)
        {
            if (nodePositions == null || nodePositions.Length == 0)
            {
                Debug.LogError("Node positions array is null or empty!");
                return Vector3.zero;
            }

            Vector3 closestNode = nodePositions[0];
            float minDistance = (closestNode - enemyPos).sqrMagnitude;

            foreach (Vector3 nodePosition in nodePositions)
            {
                float distance = (nodePosition - enemyPos).sqrMagnitude;
                if (distance < minDistance)
                {
                    closestNode = nodePosition;
                    minDistance = distance;
                }
            }

            return closestNode;
        }

        private static bool IsCloser(Vector3 node1, Vector3 node2, Vector3 position)
        {
            return (node1 - position).sqrMagnitude < (node2 - position).sqrMagnitude;
        }

        private static void SwitchToOutsideAI(EnemyAI instance)
        {
            instance.SetEnemyOutside(true);
            SetFavoriteSpot(instance);
            Debug.Log($"{instance.gameObject.name} spawned outside; Switching to exterior AI. Setting Favorite Spot to {instance.favoriteSpot}.");
        }

        private static void SwitchToInsideAI(EnemyAI instance)
        {
            instance.SetEnemyOutside(false);
            SetFavoriteSpot(instance);
            Debug.Log($"{instance.gameObject.name} spawned inside; Switching to interior AI. Setting Favorite Spot to {instance.favoriteSpot}.");
        }

        private static void SetFavoriteSpot(EnemyAI instance)
        {
            if (instance.allAINodes == null || instance.allAINodes.Length == 0)
            {
                Debug.LogError("AllAINodes array is null or empty!");
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, instance.allAINodes.Length);
            instance.favoriteSpot = instance.allAINodes[randomIndex].transform;
        }

        [HarmonyPatch(typeof(JesterAI), "Update")]
        [HarmonyPostfix]
        private static void JesterAIPatch(JesterAI __instance, ref bool ___targetingPlayer, ref float ___noPlayersToChaseTimer, ref int ___previousState)
        {
            switch (__instance.currentBehaviourStateIndex)
            {
                case 0:
                    HandleRoaming(__instance);
                    break;
                case 2:
                    HandlePursuit(__instance, ref ___targetingPlayer, ref ___noPlayersToChaseTimer, ref ___previousState);
                    break;
            }
        }

        private static void HandleRoaming(JesterAI instance)
        {
            if (instance.isOutside && instance.targetPlayer == null && !instance.roamMap.inProgress)
            {
                instance.StartSearch(instance.transform.position, instance.roamMap);
                instance.SwitchToBehaviourState(0);
            }
        }

        private static void HandlePursuit(JesterAI instance, ref bool targetingPlayer, ref float noPlayersToChaseTimer, ref int previousState)
        {
            if (!instance.isOutside) return;

            if (previousState != 2)
            {
                InitializePursuitState(instance);
                previousState = 2;
            }

            targetingPlayer = CheckForPlayersOutside();
            if (!targetingPlayer)
            {
                noPlayersToChaseTimer -= Time.deltaTime;
                if (noPlayersToChaseTimer <= 0f)
                {
                    instance.SwitchToBehaviourState(0);
                }
            }
            else
            {
                noPlayersToChaseTimer = 5f;
            }
        }

        private static void InitializePursuitState(JesterAI instance)
        {
            instance.farAudio.Stop();
            instance.creatureAnimator.SetBool("poppedOut", true);
            instance.creatureAnimator.SetFloat("CrankSpeedMultiplier", 1f);
            instance.creatureSFX.PlayOneShot(instance.popUpSFX);
            WalkieTalkie.TransmitOneShotAudio(instance.creatureSFX, instance.popUpSFX, 1f);
            instance.creatureVoice.clip = instance.screamingSFX;
            instance.creatureVoice.Play();
            instance.agent.speed = 0f;
            instance.mainCollider.isTrigger = true;
            instance.agent.stoppingDistance = 0f;
        }

        private static bool CheckForPlayersOutside()
        {
            foreach (PlayerControllerB playerControllerB in StartOfRound.Instance.allPlayerScripts)
            {
                if (playerControllerB.isPlayerControlled && !playerControllerB.isInsideFactory)
                {
                    return true;
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(SandWormAI), "StartEmergeAnimation")]
        [HarmonyPostfix]
        private static void SandwormResetPatch(SandWormAI __instance)
        {
            if (!__instance.isOutside)
            {
                int randomIndex = UnityEngine.Random.Range(0, __instance.allAINodes.Length);
                __instance.endOfFlightPathPosition = __instance.allAINodes[randomIndex].transform.position;
            }
        }

        [HarmonyPatch(typeof(SpringManAI), "DoAIInterval")]
        [HarmonyPostfix]
        private static void SpringManAnimPatch(SpringManAI __instance)
        {
            if (__instance.currentBehaviourStateIndex == 0 && __instance.isOutside && __instance.searchForPlayers.inProgress && __instance.agent.speed >= 1f)
            {
                __instance.creatureAnimator.SetFloat("walkSpeed", 1f);
            }
        }

        [HarmonyPatch(typeof(PufferAI), "Start")]
        [HarmonyPostfix]
        private static void PufferPrefabPatch(PufferAI __instance)
        {
            if (__instance.isOutside)
            {
                __instance.currentBehaviourStateIndex = 1;
            }
        }

        [HarmonyPatch(typeof(EnemyAI), "EnableEnemyMesh")]
        [HarmonyPrefix]
        private static bool EnemyMeshPatch(EnemyAI __instance, bool enable, bool overrideDoNotSet = false)
        {
            int layer = enable ? 19 : 23;
            CleanAndSetLayer(__instance.skinnedMeshRenderers, layer, overrideDoNotSet);
            CleanAndSetLayer(__instance.meshRenderers, layer, overrideDoNotSet);
            return false;
        }

        private static void CleanAndSetLayer<T>(T[] renderers, int layer, bool overrideDoNotSet) where T : Renderer
        {
            List<T> validRenderers = new List<T>();
            foreach (T renderer in renderers)
            {
                if (renderer != null && (!renderer.CompareTag("DoNotSet") || overrideDoNotSet))
                {
                    renderer.gameObject.layer = layer;
                    validRenderers.Add(renderer);
                }
            }

            renderers = validRenderers.ToArray();
        }

        [HarmonyPatch(typeof(EnemyAI), "Awake")]
        [HarmonyPostfix]
        private static void AIAwakePatch()
        {
            CacheAINodes();
        }
    }
}
