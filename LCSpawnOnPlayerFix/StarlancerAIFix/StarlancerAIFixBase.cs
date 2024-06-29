using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using StarlancerAIFix.Patches;

namespace StarlancerAIFix
{
	// Token: 0x02000003 RID: 3
	[BepInDependency("xCeezy.LethalEscape", 2)]
	[BepInPlugin("AudioKnight.StarlancerAIFix", "Starlancer AI Fix", "3.6.0")]
	public class StarlancerAIFixBase : BaseUnityPlugin
	{
		// Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
		private void Awake()
		{
			if (StarlancerAIFixBase.Instance == null)
			{
				StarlancerAIFixBase.Instance = this;
			}
			StarlancerAIFixBase.logger = base.Logger;
			StarlancerAIFixBase.logger.LogInfo("Starlancer AI Fix Online.");
			this.harmony.PatchAll(typeof(StarlancerAIFixBase));
			this.harmony.PatchAll(typeof(AIFix));
			foreach (KeyValuePair<string, PluginInfo> keyValuePair in Chainloader.PluginInfos)
			{
				if (keyValuePair.Value.Metadata.GUID.Equals("xCeezy.LethalEscape"))
				{
					StarlancerAIFixBase.logger.LogInfo("LethalEscape is active, disabling LEsc's JesterAI.Update() Postfix to ensure compatibility with SLAI.");
					this.harmony.Unpatch(typeof(JesterAI).GetMethod("Update"), 2, "LethalEscape");
					break;
				}
			}
		}

		// Token: 0x04000004 RID: 4
		private const string modGUID = "AudioKnight.StarlancerAIFix";

		// Token: 0x04000005 RID: 5
		private const string modName = "Starlancer AI Fix";

		// Token: 0x04000006 RID: 6
		private const string modVersion = "3.6.0";

		// Token: 0x04000007 RID: 7
		private readonly Harmony harmony = new Harmony("AudioKnight.StarlancerAIFix");

		// Token: 0x04000008 RID: 8
		public static StarlancerAIFixBase Instance;

		// Token: 0x04000009 RID: 9
		internal static ManualLogSource logger;
	}
}
