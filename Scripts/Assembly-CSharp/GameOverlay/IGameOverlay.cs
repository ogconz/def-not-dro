using System;
using System.Collections;
using System.Collections.Generic;
using CarX.Platform;
using GameOverlay.Regions;
using Rewired;
using UnityEngine;

namespace GameOverlay
{
	public interface IGameOverlay
	{
		IPlayer player { get; }

		IInviteService inviteService { get; }

		IRichPresence richPresence { get; }

		IVirtualKeyboard virtualKeyboard { get; }

		IRegionDetection<RegionData> regionDetection { get; }

		bool isShut { get; }

		AccountId localUniqId { get; }

		List<IPlayer> friends { get; }

		bool IsValidAge { get; }

		bool NeedUpdatePatch { get; }

		bool IsUserGeneratedContentRestricted { get; }

		bool IsUserShareContentRestricted { get; }

		bool IsFriendShowUGCAllowed { get; }

		bool DoneProcessingInvitation { get; }

		bool IsCrossPlayRestricted { get; }

		bool IsCommunicationRestricted { get; }

		bool IsCrossTextCommunicationRestricted { get; }

		Action<PlayerId, PlayerState> friendPresenceChangedAction { get; set; }

		bool IsReadyToStart();

		bool IsReadyToSync();

		bool IsSystemOverlayShowed();

		UserPlatform.Id GetPlatform();

		IStatsWorker CreateStatsWorker();

		IStorageSystem CreateStorageSystem();

		string GetUnicPlayerHashString();

		string GetInitiateAttachErrorText();

		bool Init();

		IEnumerator PrepareFirstPlayer();

		void Shutdown();

		void UpdateDLCInfo();

		bool IsDLCInstalled(DLCCode code);

		void ShowBuyDLCPage(DLCCode code);

		string GetDLCName(DLCCode code);

		string GetDLCShortName(DLCCode code);

		void RequestUserName(PlayerId id);

		void SetUserName(PlayerId id, string name);

		string GetUserName(PlayerId id);

		bool IsSupported(PlayerId id);

		void OnPhotoMode(bool isEnter);

		IEnumerator ShareScreenshot(Func<Texture2D> screenCapture);

		void SetPlayedWith(PlayerId id);

		void ShowLastPlayed();

		bool HasProfileSupport();

		void ShowProfile(PlayerId id);

		void InviteFriend(PlayerId id);

		bool IsFriend(PlayerId id);

		bool IsBlockedUser(PlayerId id);

		IEnumerator UpdateAuthSessionTicket();

		string GetAuthSessionTicket();

		bool IsSessionTicketValid();

		void RequestUserAvatarAndName(PlayerId id, Action<PlayerId, string, Texture2D> result, AvatarSize size = AvatarSize.Small);

		void Update();

		string GetLoginCmd(Dictionary<string, string> prms);

		void OnLoginError(Error error);

		void TryEnterToMPZone(bool isAuthorized, Action approvedAction, Action<string> disabledAction);

		string GetStateMessage(bool isAuthorized);

		bool ConnectedToInternet();

		void ShowUGCRestrictionDialog();

		void ShowCrossPlayRestrictionDialog();

		void ShowCommunicationRestrictionDialog();

		string GetRegion();

		bool IsResetInput()
		{
			return false;
		}

		void CheckOnlinePlayAbility();

		IEnumerator CheckCommunicationRestrictions(Action callback);

		IFilterTextManager CreateFilterTextManager();

		IEnumerator TryFilterText(string nonFiltered, Action<string> callback);

		bool IsNativePlatformController(Controller controller);

		void CheckSystemCrossPlatformAbility(Action<bool> isRestrictedCrossMpCallback);
	}
}
