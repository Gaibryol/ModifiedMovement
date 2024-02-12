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
	public class Config : SyncedInstance<Config>
	{
		[DataMember] public SyncedEntry<float> MaxStaminaMultiplier { get; private set; }
		[DataMember] public SyncedEntry<float> StaminaUsageMultiplier { get; private set; }
		[DataMember] public SyncedEntry<float> StaminaRegenMultiplierWalking { get; private set; }
		[DataMember] public SyncedEntry<float> StaminaRegenMultiplierStationary { get; private set; }
		[DataMember] public SyncedEntry<float> SprintSpeed { get; private set; }
		[DataMember] public SyncedEntry<float> JumpMultiplier { get; private set; }
		[DataMember] public SyncedEntry<float> JumpStaminaUsageMultiplier { get; private set; }

		public Config(ConfigFile cfg)
		{
			InitInstance(this);

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
				"Maximum sprinting speed (Vanilla Lethal Company = 2.25)"
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
		}

		internal static void RequestSync()
		{
			if (!IsClient) return;

			using FastBufferWriter stream = new(IntSize, Allocator.Temp);

			// Method `OnRequestSync` will then get called on host.
			stream.SendMessage($"{PluginInfo.PLUGIN_GUID}_OnRequestConfigSync");
		}

		internal static void OnRequestSync(ulong clientId, FastBufferReader _)
		{
			if (!IsHost) return;

			Plugin.Logger.LogInfo($"Config sync request received from client: {clientId}");

			byte[] array = SerializeToBytes(Instance);
			int value = array.Length;

			using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

			try
			{
				stream.WriteValueSafe(in value, default);
				stream.WriteBytesSafe(array);

				stream.SendMessage($"{PluginInfo.PLUGIN_GUID}_OnReceiveConfigSync", clientId);
			}
			catch (Exception e)
			{
				Plugin.Logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
			}
		}

		internal static void OnReceiveSync(ulong _, FastBufferReader reader)
		{
			if (!reader.TryBeginRead(IntSize))
			{
				Plugin.Logger.LogInfo("Config sync error: Could not begin reading buffer.");
				return;
			}

			reader.ReadValueSafe(out int val, default);
			if (!reader.TryBeginRead(val))
			{
				Plugin.Logger.LogInfo("Config sync error: Host could not sync.");
				return;
			}

			byte[] data = new byte[val];
			reader.ReadBytesSafe(ref data, val);

			try
			{
				SyncInstance(data);
				Plugin.Logger.LogInfo($"Syncing... (ex. Sprint Speed = {Instance.SprintSpeed}");
			}
			catch (Exception e)
			{
				Plugin.Logger.LogInfo($"Error syncing config instance!\n{e}");
			}
		}
	}
}
