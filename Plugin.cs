using BepInEx;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using ModifiedMovement;
using ModifiedMovement.Patches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ModifiedMovement
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
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
		public const string PLUGIN_VERSION = "1.5.0";
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
		static void WalkClimbSpeedPatch(ref float ___movementSpeed, ref float ___climbSpeed, ref float ___limpMultiplier)
		{
			___movementSpeed = Config.Instance.MoveSpeed.Value;
			___climbSpeed = Config.Instance.ClimbSpeed.Value;
			___limpMultiplier = Config.Instance.LimpMultiplier.Value;
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
	}
}