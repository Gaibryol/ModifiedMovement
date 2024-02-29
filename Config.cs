using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using System;
using System.Runtime.Serialization;
using HarmonyLib;
using Unity.Collections;
using Unity.Netcode;

namespace ModifiedMovement
{
	[DataContract]
	public class Config : SyncedConfig<Config>
	{
		[DataMember] public SyncedEntry<float> MaxStaminaMultiplier { get; private set; }
		[DataMember] public SyncedEntry<float> StaminaUsageMultiplier { get; private set; }
		[DataMember] public SyncedEntry<float> StaminaRegenMultiplierWalking { get; private set; }
		[DataMember] public SyncedEntry<float> StaminaRegenMultiplierStationary { get; private set; }
		[DataMember] public SyncedEntry<float> SprintSpeed { get; private set; }
		[DataMember] public SyncedEntry<float> JumpMultiplier { get; private set; }
		[DataMember] public SyncedEntry<float> JumpStaminaUsageMultiplier { get; private set; }
		[DataMember] public SyncedEntry<float> MoveSpeed {  get; private set; }
		[DataMember] public SyncedEntry<float> ClimbSpeed { get; private set; }
		[DataMember] public SyncedEntry<float> LimpMultiplier { get; private set; }

		public Config(ConfigFile cfg) : base(PluginInfo.PLUGIN_GUID)
		{
			ConfigManager.Register(this);

			MaxStaminaMultiplier = cfg.BindSyncedEntry(
				"Movespeed",
				"MaxStaminaMultiplier",
				1.5f,
				"Multiplier for maximum stamina (Vanilla Lethal Company = 1)"
			);

			StaminaUsageMultiplier = cfg.BindSyncedEntry(
				"Movespeed",
				"StaminaUsageMultiplier",
				1f,
				"Multiplier for the rate stamina gets used when sprinting (Vanilla Lethal Company = 1)"
			);

			StaminaRegenMultiplierWalking = cfg.BindSyncedEntry(
				"Movespeed",
				"StaminaRegenMultiplierWalking",
				1.25f,
				"Multiplier for the rate stamina regenerates when walking (Vanilla Lethal Company = 1)"
			);

			StaminaRegenMultiplierStationary = cfg.BindSyncedEntry(
				"Movespeed",
				"StaminaRegenMultiplierStationary",
				1.5f,
				"Multiplier for the rate stamina regenerates when stationary (Vanilla Lethal Company = 1)"
			);

			SprintSpeed = cfg.BindSyncedEntry(
				"Movespeed",
				"SprintSpeed",
				2.5f,
				"Multiplier for the walking speed to calculate sprinting speed (Vanilla Lethal Company = 2.25)"
			);

			MoveSpeed = cfg.BindSyncedEntry(
				"Movespeed",
				"WalkSpeed",
				5f,
				"Maximum walking speed (Vanilla Lethal Company = 5)"
			);

			ClimbSpeed = cfg.BindSyncedEntry(
				"Movespeed",
				"ClimbSpeed",
				4f,
				"Maximum climbing speed (Vanilla Lethal Company = 1)"
			);

			JumpMultiplier = cfg.BindSyncedEntry(
				"Movespeed",
				"JumpMultiplier",
				1f,
				"Multiplier for the jump height (Vanilla Lethal Company = 1)"
			);

			JumpStaminaUsageMultiplier = cfg.BindSyncedEntry(
				"Movespeed",
				"JumpStaminaUsageMultiplier",
				0.5f,
				"Multiplier for the rate stamina gets used when jumping (Vanilla Lethal Company = 1)"
			);

			LimpMultiplier = cfg.BindSyncedEntry(
				"Movespeed",
				"LimpMultiplier",
				0.2f,
				"Multiplier for player movespeed when limping (Vanilla Lethal Company = 0.2)"
			);
		}
	}
}
