using BepInEx.Configuration;
using CSync.Lib;
using CSync.Util;
using System;
using System.Runtime.Serialization;
using Unity.Collections;
using Unity.Netcode;

namespace ModifiedMovement
{
	[Serializable]
	public class Config : SyncedInstance<Config>
	{
		public ConfigEntry<float> DisplayDebugInfo { get; private set; }

		[DataMember] public SyncedEntry<float> maxStaminaMultiplier;
		[DataMember] public SyncedEntry<float> staminaUsageMultiplier;
		[DataMember] public SyncedEntry<float> staminaRegenMultiplierWalking;
		[DataMember] public SyncedEntry<float> staminaRegenMultiplierStationary;
		[DataMember] public SyncedEntry<float> sprintSpeed;

		public Config(ConfigFile cfg)
		{
			InitInstance(this);

			maxStaminaMultiplier = cfg.BindSyncedEntry(
				"Movespeed",
				"MaxStaminaMultiplier",
				1f,
				"Multiplier for maximum stamina (Default = 1)"
			);

			staminaUsageMultiplier = cfg.BindSyncedEntry(
				"Movespeed",
				"StaminaUsageMultiplier",
				1f,
				"Multiplier for the rate stamina gets used when sprinting (Default = 1)"
			);

			staminaRegenMultiplierWalking = cfg.BindSyncedEntry(
				"Movespeed",
				"StaminaRegenMultiplierWalking",
				1f,
				"Multiplier for the rate stamina regenerates when walking (Default = 1)"
			);

			staminaRegenMultiplierStationary = cfg.BindSyncedEntry(
				"Movespeed",
				"StaminaRegenMultiplierStationary",
				1f,
				"Multiplier for the rate stamina regenerates when stationary (Default = 1)"
			);

			sprintSpeed = cfg.BindSyncedEntry(
				"Movespeed",
				"SprintSpeed",
				2.25f,
				"Maximum sprinting speed (Default = 2.25)"
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
				//Plugin.Logger.LogError($"Error occurred syncing config with client: {clientId}\n{e}");
			}
		}

		internal static void OnReceiveSync(ulong _, FastBufferReader reader)
		{
			if (!reader.TryBeginRead(IntSize))
			{
				//Plugin.Logger.LogError("Config sync error: Could not begin reading buffer.");
				return;
			}

			reader.ReadValueSafe(out int val, default);
			if (!reader.TryBeginRead(val))
			{
				//Plugin.Logger.LogError("Config sync error: Host could not sync.");
				return;
			}

			byte[] data = new byte[val];
			reader.ReadBytesSafe(ref data, val);

			try
			{
				SyncInstance(data);
			}
			catch (Exception e)
			{
				//Plugin.Logger.LogError($"Error syncing config instance!\n{e}");
			}
		}
	}
}
