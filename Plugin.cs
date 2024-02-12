using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using ModifiedMovement;
using ModifiedMovement.Patches;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModifiedMovement
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("io.github.CSync")]
    public class Plugin : BaseUnityPlugin
    {
		private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

		public static Config MyConfig { get; internal set; }

		private static Plugin Instance;

        private void Awake()
        {
			if (Instance == null)
			{
				Instance = this;
			}

			MyConfig = new(base.Config);

			harmony.PatchAll(typeof(Plugin));
			harmony.PatchAll(typeof(PlayerControllerBPatch));
			harmony.PatchAll(typeof(Config));

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }
    }
}

namespace ModifiedMovement.Patches
{
	[HarmonyPatch(typeof(PlayerControllerB))]
	internal class PlayerControllerBPatch
	{
		[HarmonyPatch("Update")]
		[HarmonyPostfix]
		static void ModifiedMovementPatch(ref float ___sprintMeter, ref bool ___isSprinting, ref bool ___isWalking, ref float ___sprintTime, ref float ___carryWeight, ref float ___sprintMultiplier)
		{
			if (___isSprinting)
			{
				___sprintMeter = Mathf.Clamp(___sprintMeter - ((Time.deltaTime / ___sprintTime * ___carryWeight) * Config.Instance.staminaUsageMultiplier.Value), 0f, Config.Instance.maxStaminaMultiplier.Value);
				___sprintMultiplier = Mathf.Lerp(___sprintMultiplier, Config.Instance.sprintSpeed.Value, Time.deltaTime);
			}
			else
			{
				___sprintMultiplier = Mathf.Lerp(___sprintMultiplier, 1f, Time.deltaTime);

				if (___isWalking)
				{
					___sprintMeter = Mathf.Clamp(___sprintMeter + ((Time.deltaTime / (___sprintTime + 9f)) * Config.Instance.staminaRegenMultiplierWalking.Value), 0f, Config.Instance.maxStaminaMultiplier.Value);
				}
				else
				{
					___sprintMeter = Mathf.Clamp(___sprintMeter + ((Time.deltaTime / (___sprintTime + 4f)) * Config.Instance.staminaRegenMultiplierStationary.Value), 0f, Config.Instance.maxStaminaMultiplier.Value);
				}
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch("ConnectClientToPlayerObject")]
		public static void InitializeLocalPlayer()
		{
			if (Config.IsHost)
			{
				Config.MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync", Config.OnRequestSync);
				Config.Synced = true;

				return;
			}

			Config.Synced = false;
			Config.MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", Config.OnReceiveSync);
			Config.RequestSync();
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameNetworkManager), "StartDisconnect")]
		public static void PlayerLeave()
		{
			Config.RevertSync();
		}
	}
}