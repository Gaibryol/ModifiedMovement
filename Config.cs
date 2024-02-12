using BepInEx.Configuration;
using System;
using Unity.Collections;
using Unity.Netcode;

namespace ModifiedMovement
{
	public class Config : SyncedInstance<Config>
	{
		public static ConfigEntry<float> maxStaminaMultiplier;
		public static ConfigEntry<float> staminaUsageMultiplier;
		public static ConfigEntry<float> staminaRegenMultiplierWalking;
		public static ConfigEntry<float> staminaRegenMultiplierStationary;
		public static ConfigEntry<float> sprintSpeed;

		public Config(ConfigFile cfg)
		{
			InitInstance(this);

			maxStaminaMultiplier = cfg.Bind(
				"Movespeed",
				"MaxStaminaMultiplier",
				1f,
				"Multiplier for maximum stamina (Default = 1)"
			);

			staminaUsageMultiplier = cfg.Bind(
				"Movespeed",
				"SprintUsageMultiplier",
				1f,
				"Multiplier for the rate stamina gets used when sprinting (Default = 1)"
			);

			staminaRegenMultiplierWalking = cfg.Bind(
				"Movespeed",
				"StaminaRegenMultiplierWalking",
				1f,
				"Multiplier for the rate stamina regenerates when walking (Default = 1)"
			);

			staminaRegenMultiplierStationary = cfg.Bind(
				"Movespeed",
				"StaminaRegenMultiplierStationary",
				1f,
				"Multiplier for the rate stamina regenerates when stationary (Default = 1)"
			);

			sprintSpeed = cfg.Bind(
				"Movespeed",
				"SprintSpeed",
				2.25f,
				"Maximum sprinting speed (Default = 2.25)"
			);
		}

		public static void RequestSync()
		{
			if (!IsClient) return;

			using FastBufferWriter stream = new(IntSize, Allocator.Temp);
			MessageManager.SendNamedMessage("ModName_OnRequestConfigSync", 0uL, stream);
		}

		public static void OnRequestSync(ulong clientId, FastBufferReader _)
		{
			if (!IsHost) return;

			//Plugin.Logger.LogInfo($"Config sync request received from client: {clientId}");

			byte[] array = SerializeToBytes(Instance);
			int value = array.Length;

			using FastBufferWriter stream = new(value + IntSize, Allocator.Temp);

			try
			{
				stream.WriteValueSafe(in value, default);
				stream.WriteBytesSafe(array);

				MessageManager.SendNamedMessage("ModName_OnReceiveConfigSync", clientId, stream);
			}
			catch (Exception e)
			{
				//Plugin.Logger.LogInfo($"Error occurred syncing config with client: {clientId}\n{e}");
			}
		}

		public static void OnReceiveSync(ulong _, FastBufferReader reader)
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

			SyncInstance(data);

			//Plugin.Logger.LogInfo("Successfully synced config with host.");
		}
	}
}
