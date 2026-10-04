using HarmonyLib;
using System.Linq;
using System;
using System.Collections.Generic;

namespace ImBlindedByTheLights.HarmonyPatches {
	[HarmonyPatch(typeof(BeatmapDataTransformHelper), nameof(BeatmapDataTransformHelper.CreateTransformedBeatmapData))]
	static class FilterBeatmapLightEvents {
		static void Prefix(IReadonlyBeatmapData beatmapData, BeatmapKey beatmapKey, PlayerSpecificSettings playerSpecificSettings, out FilterSnapshot __state) {
			__state = FilterSnapshot.Current.Value;
			if (__state == null || !__state.TryClaim(beatmapData, beatmapKey, playerSpecificSettings))
				__state = FilterSnapshot.Capture(beatmapData, beatmapKey, playerSpecificSettings);
			__state.ThrowIfFailed();
		}

		static void Postfix(ref IReadonlyBeatmapData __result, FilterSnapshot __state) {
			if(!__state.Enabled)
				return;

			if(__state.DisableBackLasers || __state.DisableCenterLights || __state.DisableRingLights || __state.DisableRotatingLasers) {
				__result = __result.GetFilteredCopy(x => {
					if(!(x is BasicBeatmapEventData bbed))
						return x;

					switch(bbed.basicBeatmapEventType) {
						case BasicBeatmapEventType.Event0:
							if(__state.DisableBackLasers) return null;
							break;
						case BasicBeatmapEventType.Event1:
							if(__state.DisableBackLasers) return null;
							break;
						case BasicBeatmapEventType.Event2:
						case BasicBeatmapEventType.Event3:
						case BasicBeatmapEventType.Event12:
						case BasicBeatmapEventType.Event13:
							if(__state.DisableRotatingLasers) return null;
							break;
						case BasicBeatmapEventType.Event4:
							if(__state.DisableCenterLights) return null;
							break;
					}
					return x;
				});
			}

			if(__state.StaticWhenNoLights) {
				foreach(var x in __result.allBeatmapDataItems) {
					if(!(x is BasicBeatmapEventData bbed))
						continue;

					var lType = (int)bbed.basicBeatmapEventType;

					if(lType >= 0 && lType < 8 && (bbed.value > 0 || bbed.floatValue > 0)) {
						__state.HasLights = true;
						break;
					}
				}
			}
		}

		static Exception Finalizer(Exception __exception, IReadonlyBeatmapData __result, FilterSnapshot __state) {
			if (__exception == null && __result != null && __state != null)
				FilterSnapshot.Publish(__result, __state);
			return __exception;
		}
	}
}
