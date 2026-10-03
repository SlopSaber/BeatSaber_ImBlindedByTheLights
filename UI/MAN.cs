using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using UnityEngine;
using System.Threading.Tasks;

namespace ImBlindedByTheLights.UI {
	class MAN {
		Config config = Config.Instance;

		private readonly string version = $"Version {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} by Kinsi55\nCommissioned by NoneTaken";

		[UIComponent("sponsorsText")] CurvedTextMeshPro sponsorsText = null;
		[UIComponent("sponsorModal")] ModalView sponsorModal = null;
		Task sponsorsTask;
		Task<string> sponsorsDownload;
		TaskCompletionSource<bool> sponsorsCompletion;
		ViewController ownerView;
		Transform originalParent;
		ViewController presentationView;
		Transform presentationParent;
		ModalView presentationModal;
		CurvedTextMeshPro presentationText;
		bool modalOpen;
		bool retired;

		[UIAction("#post-parse")]
		void SponsorsParsed() {
			if(ownerView != null)
				ownerView.didDeactivateEvent -= OwnerDeactivated;
			RetireSponsorsPresentation();
			ownerView = sponsorModal != null ? sponsorModal.GetComponentInParent<ViewController>(true) : null;
			originalParent = sponsorModal != null ? sponsorModal.transform.parent : null;
			if(ownerView != null && !retired)
				ownerView.didDeactivateEvent += OwnerDeactivated;
		}

		[UIAction("#OpenSponsorModal")]
		void SponsorsShown() => modalOpen = !retired;

		[UIAction("#CloseSponsorModal")]
		void SponsorsHidden() => RetireSponsorsPresentation();

		void OwnerDeactivated(bool removedFromHierarchy, bool screenSystemDisabling) => RetireSponsorsPresentation();

		internal void RetireSponsorsPresentation() {
			modalOpen = false;
			presentationView = null;
			presentationParent = null;
			presentationModal = null;
			presentationText = null;
		}

		internal void Retire() {
			retired = true;
			RetireSponsorsPresentation();
			if(ownerView != null)
				ownerView.didDeactivateEvent -= OwnerDeactivated;
			ownerView = null;
			originalParent = null;
			sponsorsCompletion?.TrySetCanceled();
		}

		void OpenSponsorsLink() => Process.Start("https://github.com/sponsors/kinsi55");
		void OpenSponsorsModal() {
			if(retired)
				return;
			presentationView = ownerView;
			presentationParent = originalParent;
			presentationModal = sponsorModal;
			presentationText = sponsorsText;
			if(sponsorsTask != null && !sponsorsTask.IsCompleted)
				return;
			sponsorsText.text = "Loading...";
			sponsorsDownload = Task.Run(DownloadSponsors);
			sponsorsCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			sponsorsTask = sponsorsCompletion.Task;
			SharedCoroutineStarter.instance.StartCoroutine(PublishSponsors(sponsorsDownload, sponsorsCompletion));
		}

		static string DownloadSponsors() {
			try {
				using var client = new WebClient();
				return client.DownloadString("http://kinsi.me/sponsors/bsout.php");
			} catch {
				return "Failed to load";
			}
		}

		IEnumerator PublishSponsors(Task<string> download, TaskCompletionSource<bool> completion) {
			// The click action runs before the parser event shows and reparents the modal.
			yield return null;
			while(!download.IsCompleted)
				yield return null;
			try {
				string desc = download.GetAwaiter().GetResult();
				if(!retired && modalOpen && presentationView != null && presentationView == ownerView
					&& presentationView.isActivated && presentationView.gameObject.activeInHierarchy
					&& presentationParent != null && presentationParent == originalParent && presentationParent.gameObject.activeInHierarchy
					&& presentationModal != null && presentationModal == sponsorModal && presentationModal.isShown
					&& presentationText != null && presentationText == sponsorsText && presentationText.gameObject.activeInHierarchy) {
					presentationText.text = desc;
					presentationText.gameObject.SetActive(false);
					presentationText.gameObject.SetActive(true);
				}
				completion.TrySetResult(true);
			} catch(Exception ex) {
				completion.TrySetException(ex);
			} finally {
				if(ReferenceEquals(sponsorsDownload, download))
					sponsorsDownload = null;
			}
		}
	}
}
