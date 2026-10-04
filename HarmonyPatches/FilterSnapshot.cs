using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using HarmonyLib;

namespace ImBlindedByTheLights.HarmonyPatches {
	internal sealed class FilterSnapshot {
		internal static readonly AsyncLocal<FilterSnapshot> Current = new AsyncLocal<FilterSnapshot>();
		private static readonly ConditionalWeakTable<IReadonlyBeatmapData, Flags> Results = new ConditionalWeakTable<IReadonlyBeatmapData, Flags>();
		private static readonly ConditionalWeakTable<BeatmapCallbacksController, Flags> Controllers = new ConditionalWeakTable<BeatmapCallbacksController, Flags>();
		private static readonly object ResultGate = new object();
		private static readonly Flags Empty = new Flags(false, false);

		private readonly IReadonlyBeatmapData _source;
		private readonly PlayerSpecificSettings _settings;
		private readonly BeatmapDifficulty _difficulty;
		private readonly ExceptionDispatchInfo _captureError;
		private int _claimed;

		internal readonly bool Enabled;
		internal readonly bool StaticEnabled;
		internal readonly bool DisableBackLasers;
		internal readonly bool DisableCenterLights;
		internal readonly bool DisableRingLights;
		internal readonly bool DisableRotatingLasers;
		internal readonly bool StaticWhenNoLights;
		internal bool HasLights;

		private FilterSnapshot(IReadonlyBeatmapData source, BeatmapKey key, PlayerSpecificSettings settings) {
			_source = source;
			_settings = settings;
			_difficulty = key.difficulty;
			var config = Config.Instance;
			Enabled = config.enablePlugin;
			var forceStatic = settings != null && Enabled && config.staticInHeadset && config.staticOnDesktop;
			StaticEnabled = BasegameStaticLights.Capture(key, settings, forceStatic);
			if (Enabled) {
				DisableBackLasers = config.disableBackLasers;
				DisableCenterLights = config.disableCenterLights;
				DisableRingLights = config.disableRingLights;
				DisableRotatingLasers = config.disableRotatingLasers;
				StaticWhenNoLights = config.staticWhenNoLights;
			}
		}

		private FilterSnapshot(IReadonlyBeatmapData source, BeatmapKey key, PlayerSpecificSettings settings, Exception error) {
			_source = source;
			_settings = settings;
			_difficulty = key.difficulty;
			_captureError = ExceptionDispatchInfo.Capture(error);
		}

		internal static FilterSnapshot Capture(IReadonlyBeatmapData source, BeatmapKey key, PlayerSpecificSettings settings) {
			return new FilterSnapshot(source, key, settings);
		}

		internal static FilterSnapshot CaptureAsync(IReadonlyBeatmapData source, BeatmapKey key, PlayerSpecificSettings settings) {
			try {
				return Capture(source, key, settings);
			} catch (Exception error) {
				// Preserve the async API's task-fault contract for capture failures.
				return new FilterSnapshot(source, key, settings, error);
			}
		}

		internal void ThrowIfFailed() {
			_captureError?.Throw();
		}

		internal bool TryClaim(IReadonlyBeatmapData source, BeatmapKey key, PlayerSpecificSettings settings) {
			return ReferenceEquals(_source, source) && ReferenceEquals(_settings, settings)
				&& _difficulty == key.difficulty && Interlocked.CompareExchange(ref _claimed, 1, 0) == 0;
		}

		internal static void Publish(IReadonlyBeatmapData result, FilterSnapshot snapshot) {
			var flags = new Flags(snapshot.StaticEnabled, snapshot.HasLights);
			lock (ResultGate) {
				Results.Remove(result);
				Results.Add(result, flags);
			}
		}

		internal static void Bind(BeatmapCallbacksController controller, IReadonlyBeatmapData result) {
			Flags flags;
			lock (ResultGate) {
				if (!Results.TryGetValue(result, out flags))
					flags = Empty;
			}
			Controllers.Add(controller, flags);
		}

		internal static Flags ForController(BeatmapCallbacksController controller) {
			return controller != null && Controllers.TryGetValue(controller, out var flags) ? flags : Empty;
		}

		internal sealed class Flags {
			internal readonly bool StaticEnabled;
			internal readonly bool HasLights;

			internal Flags(bool staticEnabled, bool hasLights) {
				StaticEnabled = staticEnabled;
				HasLights = hasLights;
			}
		}
	}

	[HarmonyPatch(typeof(BeatmapDataTransformHelper), nameof(BeatmapDataTransformHelper.CreateTransformedBeatmapDataAsync))]
	internal static class CaptureFilterSnapshot {
		internal sealed class Scope {
			internal readonly FilterSnapshot Previous;
			internal Scope(FilterSnapshot previous) {
				Previous = previous;
			}
		}

		static void Prefix(IReadonlyBeatmapData beatmapData, BeatmapKey beatmapKey, PlayerSpecificSettings playerSpecificSettings, out Scope __state) {
			__state = new Scope(FilterSnapshot.Current.Value);
			FilterSnapshot.Current.Value = FilterSnapshot.CaptureAsync(beatmapData, beatmapKey, playerSpecificSettings);
		}

		static Exception Finalizer(Exception __exception, Scope __state) {
			if (__state != null)
				FilterSnapshot.Current.Value = __state.Previous;
			return __exception;
		}
	}

	[HarmonyPatch(typeof(BeatmapCallbacksController), MethodType.Constructor, new[] { typeof(BeatmapCallbacksController.InitData) })]
	internal static class BindFilterSnapshot {
		static void Postfix(BeatmapCallbacksController __instance, BeatmapCallbacksController.InitData initData) {
			if (initData?.beatmapData != null)
				FilterSnapshot.Bind(__instance, initData.beatmapData);
		}
	}
}
