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
using GameplaySetup = BeatSaberMarkupLanguage.GameplaySetup.GameplaySetup;


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
		private MAN _menuHost;
		private GameplaySetup _menuRegistrationOwner;

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

			MainMenuAwaiter.MainMenuInitializing += InitializeMenu;
		}

		private void InitializeMenu() {
			RetireMenu();
			_menuRegistrationOwner = GameplaySetup.Instance;
			_menuHost = new MAN();
			_menuRegistrationOwner.AddTab("BlindedByTheLights", "ImBlindedByTheLights.UI.GamePlaySetupTab.bsml", _menuHost);
		}

		private void RetireMenu() {
			if(_menuHost == null)
				return;
			_menuHost.Retire();
			_menuHost = null;
			GameplaySetup registrationOwner = _menuRegistrationOwner;
			_menuRegistrationOwner = null;
			registrationOwner?.RemoveTab("BlindedByTheLights");
		}

		private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1) {
			_menuHost?.RetireSponsorsPresentation();
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
			MainMenuAwaiter.MainMenuInitializing -= InitializeMenu;
			try {
				RetireMenu();
			} finally {
				SceneManager.activeSceneChanged -= SceneManager_activeSceneChanged;
				StopLightInitialization();
				LightSwitch.Clear();
				harmony.UnpatchSelf();
			}
		}
	}
}
