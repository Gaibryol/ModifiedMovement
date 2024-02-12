using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using ModifiedMovement;
using ModifiedMovement.Patches;
using UnityEngine;

namespace ModifiedMovement
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
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
				___sprintMeter = Mathf.Clamp(___sprintMeter - ((Time.deltaTime / ___sprintTime * ___carryWeight) * Config.staminaUsageMultiplier.Value), 0f, Config.maxStaminaMultiplier.Value);
				___sprintMultiplier = Mathf.Lerp(___sprintMultiplier, Config.sprintSpeed.Value, Time.deltaTime);
			}
			else
			{
				___sprintMultiplier = Mathf.Lerp(___sprintMultiplier, 1f, Time.deltaTime);

				if (___isWalking)
				{
					___sprintMeter = Mathf.Clamp(___sprintMeter + ((Time.deltaTime / (___sprintTime + 9f)) * Config.staminaRegenMultiplierWalking.Value), 0f, Config.maxStaminaMultiplier.Value);
				}
				else
				{
					___sprintMeter = Mathf.Clamp(___sprintMeter + ((Time.deltaTime / (___sprintTime + 4f)) * Config.staminaRegenMultiplierStationary.Value), 0f, Config.maxStaminaMultiplier.Value);
				}
			}
		}

		[HarmonyPatch("ConnectClientToPlayerObject")]
		[HarmonyPostfix]
		public static void InitializeLocalPlayer()
		{
			if (Config.IsHost)
			{
				Config.MessageManager.RegisterNamedMessageHandler("ModName_OnRequestConfigSync", Config.OnRequestSync);
				Config.Synced = true;

				return;
			}

			Config.Synced = false;
			Config.MessageManager.RegisterNamedMessageHandler("ModName_OnReceiveConfigSync", Config.OnReceiveSync);
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