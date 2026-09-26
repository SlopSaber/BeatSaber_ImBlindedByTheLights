using BeatSaberMarkupLanguage.Util;
using HarmonyLib;
using ImBlindedByTheLights.HarmonyPatches;
using ImBlindedByTheLights.UI;
using IPA;
using IPA.Config.Stores;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;


// BeatmapDataLoader::GetBeatmapDataFromBeatmapSaveData

/*
	if (beatmapData.beatmapEventsData.Count == 0) {
		beatmapData.AddBeatmapEventData(new BeatmapEventData(0f, BeatmapEventType.Event0, 1, 1f));
		beatmapData.AddBeatmapEventData(new BeatmapEventData(0f, BeatmapEventType.Event4, 1, 1f));
	}
*/

namespace ImBlindedByTheLights {
	[Plugin(RuntimeOptions.SingleStartInit)]
	public class Plugin {
		internal static Plugin Instance { get; private set; }
		internal static IPALogger Log { get; private set; }
		internal static Harmony harmony { get; private set; }
		private Coroutine _lightInitialization;
		private MonoBehaviour _lightInitializationHost;

		[Init]
		public void Init(IPALogger logger, IPA.Config.Config conf) {
			Instance = this;
			Log = logger;
			Config.Instance = conf.Generated<Config>();
		}

		[OnStart]
		public void OnApplicationStart() {
			harmony = new Harmony("Kinsi55.BeatSaber.ImBlindedByTheLights");
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

			MainMenuAwaiter.MainMenuInitializing += delegate {
				BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup.Instance.AddTab("BlindedByTheLights", "ImBlindedByTheLights.UI.GamePlaySetupTab.bsml", new MAN());
			};
		}

		private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1) {
			StopLightInitialization();
			LightSwitch.Clear();
			if(Config.Instance.enablePlugin && arg1.name == "GameCore" && (Config.Instance.staticInHeadset || Config.Instance.staticOnDesktop)) {
				_lightInitializationHost = SharedCoroutineStarter.instance;
				_lightInitialization = _lightInitializationHost.StartCoroutine(LightSwitch.Init());
			}
		}

		private void StopLightInitialization() {
			if(_lightInitialization == null)
				return;
			if(_lightInitializationHost != null)
				_lightInitializationHost.StopCoroutine(_lightInitialization);
			_lightInitialization = null;
			_lightInitializationHost = null;
		}

		[OnExit]
		public void OnApplicationQuit() {
			SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
			StopLightInitialization();
			LightSwitch.Clear();
			harmony.UnpatchSelf();
		}
	}
}
