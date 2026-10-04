using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static PlayerSaveData;

namespace ImBlindedByTheLights.HarmonyPatches {
	static class BasegameStaticLights {
		private static readonly System.Reflection.FieldInfo DefaultPresetField =
			AccessTools.Field(typeof(PlayerSpecificSettings), "_environmentEffectsFilterDefaultPreset");
		private static readonly System.Reflection.FieldInfo ExpertPlusPresetField =
			AccessTools.Field(typeof(PlayerSpecificSettings), "_environmentEffectsFilterExpertPlusPreset");

		internal static bool Capture(BeatmapKey beatmapKey, PlayerSpecificSettings playerSpecificSettings, bool forceStatic) {
			if (playerSpecificSettings == null) {
				return false;
			}

			var presetField = beatmapKey.difficulty == BeatmapDifficulty.ExpertPlus
				? ExpertPlusPresetField
				: DefaultPresetField;
			var environmentEffectsFilterPreset = presetField?.GetValue(playerSpecificSettings) is EnvironmentEffectsFilterPreset preset
				? preset
				: EnvironmentEffectsFilterPreset.AllEffects;

			if (forceStatic) {
				DefaultPresetField?.SetValue(playerSpecificSettings, EnvironmentEffectsFilterPreset.NoEffects);
				ExpertPlusPresetField?.SetValue(playerSpecificSettings, EnvironmentEffectsFilterPreset.NoEffects);
				environmentEffectsFilterPreset = EnvironmentEffectsFilterPreset.NoEffects;
			}

			return environmentEffectsFilterPreset == EnvironmentEffectsFilterPreset.NoEffects;
		}
	}
}
