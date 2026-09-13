using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static PlayerSaveData;

namespace ImBlindedByTheLights.HarmonyPatches {
	[HarmonyPatch(typeof(BeatmapDataTransformHelper), nameof(BeatmapDataTransformHelper.CreateTransformedBeatmapData))]
	static class BasegameStaticLights {
		private static readonly System.Reflection.FieldInfo DefaultPresetField =
			AccessTools.Field(typeof(PlayerSpecificSettings), "_environmentEffectsFilterDefaultPreset");
		private static readonly System.Reflection.FieldInfo ExpertPlusPresetField =
			AccessTools.Field(typeof(PlayerSpecificSettings), "_environmentEffectsFilterExpertPlusPreset");

		public static bool enabled { get; private set; } = false;
		static void Prefix(BeatmapKey beatmapKey, PlayerSpecificSettings playerSpecificSettings) {
			if (playerSpecificSettings == null) {
				enabled = false;
				return;
			}

			var presetField = beatmapKey.difficulty == BeatmapDifficulty.ExpertPlus
				? ExpertPlusPresetField
				: DefaultPresetField;
			var environmentEffectsFilterPreset = presetField?.GetValue(playerSpecificSettings) is EnvironmentEffectsFilterPreset preset
				? preset
				: EnvironmentEffectsFilterPreset.AllEffects;

			if (Config.Instance.enablePlugin && Config.Instance.staticInHeadset && Config.Instance.staticOnDesktop) {
				DefaultPresetField?.SetValue(playerSpecificSettings, EnvironmentEffectsFilterPreset.NoEffects);
				ExpertPlusPresetField?.SetValue(playerSpecificSettings, EnvironmentEffectsFilterPreset.NoEffects);
				environmentEffectsFilterPreset = EnvironmentEffectsFilterPreset.NoEffects;
			}

			enabled = environmentEffectsFilterPreset == EnvironmentEffectsFilterPreset.NoEffects;
		}
	}
}
