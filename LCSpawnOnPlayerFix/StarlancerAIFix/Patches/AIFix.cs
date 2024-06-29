using System;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace StarlancerAIFix.Patches
{
	// Token: 0x02000004 RID: 4
	public class AIFix
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002160 File Offset: 0x00000360
		private static void CacheAINodes()
		{
			if (AIFix.outsideAINodes == null || AIFix.outsideAINodes.Length == 0 || AIFix.outsideAINodes[0] == null)
			{
				AIFix.outsideAINodes = GameObject.FindGameObjectsWithTag("OutsideAINode");
				if (AIFix.outsideAINodes == null || AIFix.outsideAINodes.Length == 0)
				{
					Debug.LogError("No OutsideAINodes found!");
				}
				AIFix.outsideNodePositions = new Vector3[AIFix.outsideAINodes.Length];
				for (int i = 0; i < AIFix.outsideAINodes.Length; i++)
				{
					if (AIFix.outsideAINodes[i] != null)
					{
						AIFix.outsideNodePositions[i] = AIFix.outsideAINodes[i].transform.position;
					}
				}
			}
			if (AIFix.insideAINodes == null || AIFix.insideAINodes.Length == 0 || AIFix.insideAINodes[0] == null)
			{
				AIFix.insideAINodes = GameObject.FindGameObjectsWithTag("AINode");
				if (AIFix.insideAINodes == null || AIFix.insideAINodes.Length == 0)
				{
					Debug.LogError("No AINodes found!");
				}
				AIFix.insideNodePositions = new Vector3[AIFix.insideAINodes.Length];
				for (int j = 0; j < AIFix.insideAINodes.Length; j++)
				{
					if (AIFix.insideAINodes[j] != null)
					{
						AIFix.insideNodePositions[j] = AIFix.insideAINodes[j].transform.position;
					}
				}
			}
		}

		// Token: 0x06000004 RID: 4 RVA: 0x00002298 File Offset: 0x00000498
		[HarmonyPatch(typeof(EnemyAI), "Start")]
		[HarmonyPostfix]
		private static void AIFixPatch(EnemyAI __instance)
		{
			AIFix.CacheAINodes();
			if (AIFix.outsideNodePositions == null || AIFix.insideNodePositions == null)
			{
				Debug.LogError("Node positions not initialized!");
				return;
			}
			Vector3 position = __instance.transform.position;
			Vector3 closestNode = AIFix.GetClosestNode(AIFix.outsideNodePositions, position);
			Vector3 closestNode2 = AIFix.GetClosestNode(AIFix.insideNodePositions, position);
			if (!__instance.isOutside && (closestNode - position).sqrMagnitude < (closestNode2 - position).sqrMagnitude)
			{
				AIFix.SwitchToOutsideAI(__instance);
				return;
			}
			if (__instance.isOutside && (closestNode - position).sqrMagnitude > (closestNode2 - position).sqrMagnitude)
			{
				AIFix.SwitchToInsideAI(__instance);
			}
		}

		// Token: 0x06000005 RID: 5 RVA: 0x00002344 File Offset: 0x00000544
		private static Vector3 GetClosestNode(Vector3[] nodePositions, Vector3 enemyPos)
		{
			if (nodePositions == null || nodePositions.Length == 0)
			{
				Debug.LogError("Node positions array is null or empty!");
				return Vector3.zero;
			}
			Vector3 vector = nodePositions[0];
			float num = (vector - enemyPos).sqrMagnitude;
			for (int i = 1; i < nodePositions.Length; i++)
			{
				float sqrMagnitude = (nodePositions[i] - enemyPos).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					vector = nodePositions[i];
					num = sqrMagnitude;
				}
			}
			return vector;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000023B6 File Offset: 0x000005B6
		private static void SwitchToOutsideAI(EnemyAI instance)
		{
			instance.SetEnemyOutside(true);
			AIFix.SetFavoriteSpot(instance);
			Debug.Log(string.Format("{0} spawned outside; Switching to exterior AI. Setting Favorite Spot to {1}.", instance.gameObject.name, instance.favoriteSpot));
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000023E5 File Offset: 0x000005E5
		private static void SwitchToInsideAI(EnemyAI instance)
		{
			instance.SetEnemyOutside(false);
			AIFix.SetFavoriteSpot(instance);
			Debug.Log(string.Format("{0} spawned inside; Switching to interior AI. Setting Favorite Spot to {1}.", instance.gameObject.name, instance.favoriteSpot));
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002414 File Offset: 0x00000614
		private static void SetFavoriteSpot(EnemyAI instance)
		{
			if (instance.allAINodes == null || instance.allAINodes.Length == 0)
			{
				Debug.LogError("AllAINodes array is null or empty!");
				return;
			}
			int num = Random.Range(0, instance.allAINodes.Length);
			instance.favoriteSpot = instance.allAINodes[num].transform;
		}

		// Token: 0x06000009 RID: 9 RVA: 0x00002460 File Offset: 0x00000660
		[HarmonyPatch(typeof(JesterAI), "Update")]
		[HarmonyPostfix]
		private static void JesterAIPatch(JesterAI __instance, ref bool ___targetingPlayer, ref float ___noPlayersToChaseTimer, ref int ___previousState)
		{
			int currentBehaviourStateIndex = __instance.currentBehaviourStateIndex;
			if (currentBehaviourStateIndex == 0)
			{
				AIFix.HandleRoaming(__instance);
				return;
			}
			if (currentBehaviourStateIndex != 2)
			{
				return;
			}
			AIFix.HandlePursuit(__instance, ref ___targetingPlayer, ref ___noPlayersToChaseTimer, ref ___previousState);
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000248C File Offset: 0x0000068C
		private static void HandleRoaming(JesterAI instance)
		{
			if (instance.isOutside && instance.targetPlayer == null && !instance.roamMap.inProgress)
			{
				instance.StartSearch(instance.transform.position, instance.roamMap);
				instance.SwitchToBehaviourState(0);
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x000024DC File Offset: 0x000006DC
		private static void HandlePursuit(JesterAI instance, ref bool targetingPlayer, ref float noPlayersToChaseTimer, ref int previousState)
		{
			if (instance.isOutside)
			{
				if (previousState != 2)
				{
					AIFix.InitializePursuitState(instance);
					previousState = 2;
				}
				targetingPlayer = AIFix.CheckForPlayersOutside();
				if (!targetingPlayer)
				{
					noPlayersToChaseTimer -= Time.deltaTime;
					if (noPlayersToChaseTimer <= 0f)
					{
						instance.SwitchToBehaviourState(0);
						return;
					}
				}
				else
				{
					noPlayersToChaseTimer = 5f;
				}
			}
		}

		// Token: 0x0600000C RID: 12 RVA: 0x0000252C File Offset: 0x0000072C
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

		// Token: 0x0600000D RID: 13 RVA: 0x000025DC File Offset: 0x000007DC
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

		// Token: 0x0600000E RID: 14 RVA: 0x0000261C File Offset: 0x0000081C
		[HarmonyPatch(typeof(SandWormAI), "StartEmergeAnimation")]
		[HarmonyPostfix]
		private static void SandwormResetPatch(SandWormAI __instance)
		{
			if (!__instance.isOutside)
			{
				int num = Random.Range(0, __instance.allAINodes.Length);
				__instance.endOfFlightPathPosition = __instance.allAINodes[num].transform.position;
			}
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002658 File Offset: 0x00000858
		[HarmonyPatch(typeof(SpringManAI), "DoAIInterval")]
		[HarmonyPostfix]
		private static void SpringManAnimPatch(SpringManAI __instance)
		{
			if (__instance.currentBehaviourStateIndex == 0 && __instance.isOutside && __instance.searchForPlayers.inProgress && __instance.agent.speed >= 1f)
			{
				__instance.creatureAnimator.SetFloat("walkSpeed", 1f);
			}
		}

		// Token: 0x06000010 RID: 16 RVA: 0x000026A9 File Offset: 0x000008A9
		[HarmonyPatch(typeof(PufferAI), "Start")]
		[HarmonyPostfix]
		private static void PufferPrefabPatch(PufferAI __instance)
		{
			if (__instance.isOutside)
			{
				__instance.currentBehaviourStateIndex = 1;
			}
		}

		// Token: 0x06000011 RID: 17 RVA: 0x000026BC File Offset: 0x000008BC
		[HarmonyPatch(typeof(EnemyAI), "EnableEnemyMesh")]
		[HarmonyPrefix]
		private static bool EnemyMeshPatch(EnemyAI __instance, bool enable, bool overrideDoNotSet = false)
		{
			int layer = enable ? 19 : 23;
			AIFix.CleanAndSetLayer<SkinnedMeshRenderer>(__instance.skinnedMeshRenderers, layer, overrideDoNotSet);
			AIFix.CleanAndSetLayer<MeshRenderer>(__instance.meshRenderers, layer, overrideDoNotSet);
			return false;
		}

		// Token: 0x06000012 RID: 18 RVA: 0x000026F0 File Offset: 0x000008F0
		private static void CleanAndSetLayer<T>(T[] renderers, int layer, bool overrideDoNotSet) where T : Renderer
		{
			List<T> list = new List<T>();
			foreach (T t in renderers)
			{
				if (t != null && (!t.CompareTag("DoNotSet") || overrideDoNotSet))
				{
					t.gameObject.layer = layer;
					list.Add(t);
				}
			}
			renderers = list.ToArray();
		}

		// Token: 0x0400000A RID: 10
		private static GameObject[] outsideAINodes;

		// Token: 0x0400000B RID: 11
		private static GameObject[] insideAINodes;

		// Token: 0x0400000C RID: 12
		private static Vector3[] outsideNodePositions;

		// Token: 0x0400000D RID: 13
		private static Vector3[] insideNodePositions;

		// Token: 0x02000006 RID: 6
		[HarmonyPatch]
		private static class EnemyAIPatch
		{
			// Token: 0x06000015 RID: 21 RVA: 0x00002770 File Offset: 0x00000970
			private static MethodBase TargetMethod()
			{
				MethodInfo method = typeof(EnemyAI).GetMethod("Awake", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method == null)
				{
					Debug.LogError("Awake method not found in EnemyAI");
				}
				return method;
			}

			// Token: 0x06000016 RID: 22 RVA: 0x0000279B File Offset: 0x0000099B
			[HarmonyPostfix]
			private static void AIAwakePatch()
			{
				AIFix.CacheAINodes();
			}
		}
	}
}
