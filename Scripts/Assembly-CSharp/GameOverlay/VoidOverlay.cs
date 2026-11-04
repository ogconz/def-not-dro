using System;
using System.Collections;
using System.Collections.Generic;
using CarX.Platform;
using GameOverlay.Regions;
using Rewired;
using UnityEngine;

namespace GameOverlay
{
	public class VoidOverlay : IGameOverlay
	{
		private class VoidPlayer : IPlayer
		{
			public PlayerId id { get; set; }

			public Texture2D avatar { get; set; }

			public string nickName { get; set; }

			public PlayerState state { get; set; }
		}

		private string m_authTicket = string.Empty;

		private VoidRichPresence m_richPresence;

		private VoidRegionDetection m_regionDetection;

		private IVirtualKeyboard m_virtualKeyboard = new VoidVirtualKeyboard();

		public IPlayer player => new VoidPlayer();

		public IInviteService inviteService => new VoidInviteService();

		public bool isShut => false;

		public AccountId localUniqId => player.id.accountId;

		public List<IPlayer> friends => new List<IPlayer>();

		public bool IsValidAge => true;

		public bool NeedUpdatePatch => false;

		public bool IsUserGeneratedContentRestricted => false;

		public bool IsUserShareContentRestricted => false;

		public bool IsFriendShowUGCAllowed => true;

		public bool DoneProcessingInvitation => true;

		public bool IsCrossPlayRestricted => false;

		public bool IsCommunicationRestricted => false;

		public bool IsCrossTextCommunicationRestricted => false;

		public Action<PlayerId, PlayerState> friendPresenceChangedAction { get; set; }

		public IRichPresence richPresence => m_richPresence;

		public IRegionDetection<RegionData> regionDetection => m_regionDetection;

		public IVirtualKeyboard virtualKeyboard => m_virtualKeyboard;

		public void CheckOnlinePlayAbility()
		{
		}

		public IEnumerator CheckCommunicationRestrictions(Action callback)
		{
			callback.SafeCall();
			yield break;
		}

		public void CheckSystemCrossPlatformAbility(Action<bool> isRestrictedCrossMpCallback)
		{
			isRestrictedCrossMpCallback.Call(param: false);
		}

		public bool ConnectedToInternet()
		{
			return true;
		}

		public IFilterTextManager CreateFilterTextManager()
		{
			return new EmptyFilterTextManager();
		}

		public IStatsWorker CreateStatsWorker()
		{
			return new VoidStatsWorker();
		}

		public IStorageSystem CreateStorageSystem()
		{
			return new PCPrefsStorageSystem();
		}

		public IEnumerator UpdateAuthSessionTicket()
		{
			m_authTicket = "EmptyAuthTicket";
			yield break;
		}

		public string GetAuthSessionTicket()
		{
			return m_authTicket;
		}

		public string GetDLCName(DLCCode code)
		{
			return string.Empty;
		}

		public string GetDLCShortName(DLCCode code)
		{
			return string.Empty;
		}

		public string GetInitiateAttachErrorText()
		{
			return "Fatal error";
		}

		public string GetLoginCmd(Dictionary<string, string> prms)
		{
			return "loginGeneric";
		}

		public UserPlatform.Id GetPlatform()
		{
			return UserPlatform.Id.Void;
		}

		public string GetRegion()
		{
			return m_regionDetection.GetRegionData().GetFormattedRegionStr();
		}

		public string GetStateMessage(bool isAuthorized)
		{
			if (!isAuthorized)
			{
				return LocalizationKeys.sync_not_connect.Localized() + " " + LocalizationKeys.multiplayer_unavailable.Localized();
			}
			return string.Empty;
		}

		public string GetUnicPlayerHashString()
		{
			return player.id.ToString().GetHashCode().ToString("X");
		}

		public string GetUserName(PlayerId id)
		{
			return string.Empty;
		}

		public bool Init()
		{
			return true;
		}

		public void InviteFriend(PlayerId id)
		{
		}

		public bool IsBlockedUser(PlayerId id)
		{
			return false;
		}

		public bool IsContentInstalled(ulong contentId)
		{
			return false;
		}

		public bool IsDLCInstalled(DLCCode code)
		{
			return false;
		}

		public bool IsFriend(PlayerId id)
		{
			return false;
		}

		public bool IsNativePlatformController(Controller controller)
		{
			return true;
		}

		public bool IsReadyToStart()
		{
			return true;
		}

		public bool IsReadyToSync()
		{
			return !player.id.IsInvalid;
		}

		public bool IsSessionTicketValid()
		{
			return !string.IsNullOrEmpty(m_authTicket);
		}

		public bool IsSupported(PlayerId id)
		{
			return false;
		}

		public bool IsSystemOverlayShowed()
		{
			return false;
		}

		public void OnLoginError(Error error)
		{
		}

		public void OnPhotoMode(bool isEnter)
		{
		}

		public IEnumerator PrepareFirstPlayer()
		{
			player.id = new PlayerId(GetPlatform(), (ulong)Environment.UserName.GetHashCode());
			player.nickName = Environment.UserName;
			player.avatar = GameManager.instance.defaultAvatar;
			yield break;
		}

		public void RequestUserAvatarAndName(PlayerId id, Action<PlayerId, string, Texture2D> result, AvatarSize size = AvatarSize.Small)
		{
			result.Call(id, string.Empty, GameManager.instance.defaultAvatar);
		}

		public void RequestUserName(PlayerId id)
		{
		}

		public void SetPlayedWith(PlayerId id)
		{
		}

		public void SetUserName(PlayerId id, string name)
		{
		}

		public IEnumerator ShareScreenshot(Func<Texture2D> screenCapture)
		{
			yield break;
		}

		public void ShowBuyDLCPage(DLCCode code)
		{
		}

		public void ShowLastPlayed()
		{
		}

		public bool HasProfileSupport()
		{
			return false;
		}

		public void ShowProfile(PlayerId id)
		{
		}

		public void ShowUGCRestrictionDialog()
		{
		}

		public void ShowCrossPlayRestrictionDialog()
		{
		}

		public void ShowCommunicationRestrictionDialog()
		{
		}

		public void Shutdown()
		{
		}

		public void TryEnterToMPZone(bool isAuthorized, Action approvedAction, Action<string> disabledAction)
		{
			approvedAction.Call();
		}

		public IEnumerator TryFilterText(string nonFiltered, Action<string> callback)
		{
			callback.Call(nonFiltered);
			yield break;
		}

		public void Update()
		{
		}

		public void UpdateDLCInfo()
		{
		}
	}
}
