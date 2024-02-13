using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using ModifiedMovement;
using ModifiedMovement.Patches;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModifiedMovement
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
	[BepInDependency("io.github.CSync")]
    public class Plugin : BaseUnityPlugin
    {
		private readonly Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);

		public static Config Config { get; private set; }

		public static Plugin Instance { get; internal set; }

		public static new ManualLogSource Logger { get; private set; }

        private void Awake()
        {
			if (Instance == null)
			{
				Instance = this;
			}

			Logger = base.Logger;

			Config = new(base.Config);

			//harmony.PatchAll(typeof(Plugin));
			//harmony.PatchAll(typeof(PlayerControllerBPatch));
			//harmony.PatchAll(typeof(GameNetworkManager));
			//harmony.PatchAll(typeof(Config));
			harmony.PatchAll(Assembly.GetExecutingAssembly());

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_NAME} is loaded!");
        }
    }

	public static class PluginInfo
	{
		public const string PLUGIN_GUID = "ModifiedMovement";
		public const string PLUGIN_NAME = "ModifiedMovement";
		public const string PLUGIN_VERSION = "1.3.0";
	}
}

namespace ModifiedMovement.Patches
{
	[HarmonyPatch(typeof(PlayerControllerB))]
	internal class PlayerControllerBPatch
	{
		[HarmonyPostfix]
		[HarmonyPatch("Update")]
		static void ModifiedMovementPatch(ref float ___sprintMeter, ref bool ___isSprinting, ref bool ___isWalking, ref float ___sprintTime, ref float ___carryWeight, ref float ___sprintMultiplier)
		{
			if (___isSprinting)
			{
				___sprintMeter = Mathf.Clamp(___sprintMeter - ((Time.deltaTime / ___sprintTime * ___carryWeight) * Config.Instance.StaminaUsageMultiplier.Value), 0f, Config.Instance.MaxStaminaMultiplier.Value);
				___sprintMultiplier = Mathf.Lerp(___sprintMultiplier, Config.Instance.SprintSpeed.Value, Time.deltaTime);
			}
			else
			{
				___sprintMultiplier = Mathf.Lerp(___sprintMultiplier, 1f, Time.deltaTime);

				if (___isWalking)
				{
					___sprintMeter = Mathf.Clamp(___sprintMeter + ((Time.deltaTime / (___sprintTime + 9f)) * Config.Instance.StaminaRegenMultiplierWalking.Value), 0f, Config.Instance.MaxStaminaMultiplier.Value);
				}
				else
				{
					___sprintMeter = Mathf.Clamp(___sprintMeter + ((Time.deltaTime / (___sprintTime + 4f)) * Config.Instance.StaminaRegenMultiplierStationary.Value), 0f, Config.Instance.MaxStaminaMultiplier.Value);
				}
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch("Update")]
		static void WalkSpeedPatch(ref float ___movementSpeed)
		{
			___movementSpeed = Config.Instance.MoveSpeed.Value;
		}

		[HarmonyPrefix]
		[HarmonyPatch("PlayerJump")]
		static void ModifiedJumpPatch(ref float ___jumpForce)
		{
			___jumpForce *= Config.Instance.JumpMultiplier.Value;
		}

		[HarmonyPostfix]
		[HarmonyPatch("Jump_performed")]
		static void ModifiedJumpStaminaPatch(ref float ___sprintMeter)
		{
			___sprintMeter = Mathf.Clamp(___sprintMeter - (0.08f * Config.Instance.JumpStaminaUsageMultiplier), 0f, Config.Instance.MaxStaminaMultiplier);
		}

		[HarmonyPostfix]
		[HarmonyPatch("ConnectClientToPlayerObject")]
		public static void InitializeLocalPlayer()
		{
			if (Config.IsHost)
			{
				Config.MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync", Config.OnRequestSync);
				Config.Synced = true;

				Plugin.Logger.LogInfo("Initialize Is Host");
				return;
			}

			Plugin.Logger.LogInfo("Initialize Client");
			Config.Synced = false;
			Config.MessageManager.RegisterNamedMessageHandler($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", Config.OnReceiveSync);
			Config.RequestSync();
		}
	}

	[HarmonyPatch(typeof(GameNetworkManager))]
	internal class NetworkManagerPatch
	{
		[HarmonyPostfix]
		[HarmonyPatch("StartDisconnect")]
		public static void PlayerLeave()
		{
			Config.RevertSync();
		}
	}
}