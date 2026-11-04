using System;
using ArrayExtension;
using Cysharp.Threading.Tasks;
using GameOverlay;
using ModIO;
using Service.UGC.ModIO;
using Service.UGC.ModIO.Authentication;
using Services.UGC.Builders;
using Services.UGC.Interfaces;
using Services.UGC.Storage;
using UnityEngine;

namespace Services.UGC.Platforms
{
	public class ModIOService : UGCService<ModIOManager>
	{
		public Action<ModManagementEventType, ModId, Result> onModOperation;

		private ModIOAuthentication m_authentication;

		private IUGCStorageManager m_storage;

		private float m_lastTimeUpdatedManager;

		private int m_targetIndexToUpdate;

		public override IUGCAuthentication authentication => m_authentication;

		public override IUGCStorageManager storage => m_storage;

		public ModIOService()
			: base((IUGCManagersFactoty<ModIOManager>)new ModIOManagersFactory())
		{
		}

		public override async UniTask<bool> Initialize()
		{
			CreatePlatformServices();
			InitializeManagers();
			bool result = InitializeAuthentication();
			if (result)
			{
				await DownloadAllSubscribedPackages();
			}
			return result;
		}

		private void CreatePlatformServices()
		{
			m_authentication = new ModIOSteamAutentication();
			m_storage = new VoidUGCStorageManager();
		}

		private void InitializeManagers()
		{
			foreach (ModIOManager manager in m_managers)
			{
				manager.Initialize(this);
			}
		}

		private bool InitializeAuthentication()
		{
			ModIOAuthentication modIOAuthentication = m_authentication;
			modIOAuthentication.onAuthChanged = (Action<bool>)Delegate.Combine(modIOAuthentication.onAuthChanged, new Action<bool>(OnAuthChangedHandler));
			return m_isInitialized = m_authentication.Initialize();
		}

		public async UniTask DownloadAllSubscribedPackages(Action<float, string> progress = null, Action onFinished = null)
		{
			foreach (ModIOManager manager in m_managers)
			{
				manager.DownloadAllSubscribedPackages();
			}
			onFinished.SafeCall();
		}

		private void OnAuthChangedHandler(bool isAuthenticated)
		{
			if (isAuthenticated && !Overlay.instance.IsUserGeneratedContentRestricted)
			{
				ModIOUnity.EnableModManagement(HandleModManagementEvent);
				FetchUpdates(ForceDeleteInstalledByAnotherUserMods);
			}
			else if (!isAuthenticated)
			{
				ModIOUnity.LogOutCurrentUser();
			}
		}

		public override void Update()
		{
			if (!m_isInitialized)
			{
				return;
			}
			ProgressHandle currentModManagementOperation = ModIOUnity.GetCurrentModManagementOperation();
			for (int i = 0; i < m_managers.Count; i++)
			{
				ModIOManager modIOManager = m_managers[i];
				if (currentModManagementOperation != null)
				{
					modIOManager.HandleProgress(currentModManagementOperation);
				}
				if (ShouldUpdateManagerByIndex(i))
				{
					modIOManager.Update();
				}
			}
		}

		private bool ShouldUpdateManagerByIndex(int index)
		{
			if (TimeToHandleIsExpired() && IsValidState() && m_targetIndexToUpdate == index)
			{
				m_targetIndexToUpdate = ((++index < m_managers.Count) ? index : 0);
				m_lastTimeUpdatedManager = Time.time;
				return true;
			}
			return false;
		}

		private bool TimeToHandleIsExpired()
		{
			if (Time.time - m_lastTimeUpdatedManager < 2.5f)
			{
				return false;
			}
			return true;
		}

		private bool IsValidState()
		{
			return !IsLoadingState();
		}

		private bool IsLoadingState()
		{
			if (!(States.mainCurrent is LoadRaceState) && !(States.current is LoadRaceState) && !(States.mainCurrent is LoadSceneState) && !(States.current is LoadSceneState) && !(States.mainCurrent is StartState))
			{
				return States.current is StartState;
			}
			return true;
		}

		private void ForceDeleteInstalledByAnotherUserMods()
		{
			Result result;
			InstalledMod[] systemInstalledMods = ModIOUnity.GetSystemInstalledMods(out result);
			if (result.Succeeded())
			{
				InstalledMod[] array = systemInstalledMods;
				for (int i = 0; i < array.Length; i++)
				{
					ModIOUnity.ForceUninstallMod(array[i].modProfile.id);
				}
			}
		}

		public void HandleModManagementEvent(ModManagementEventType eventType, ModId modId, Result eventResult)
		{
			onModOperation?.Invoke(eventType, modId, eventResult);
		}

		private void FetchUpdates(params Action[] onComplete)
		{
			ModIOUnity.FetchUpdates(delegate(Result result)
			{
				if (result.Succeeded())
				{
					onComplete.ForEach(delegate(Action action)
					{
						action?.SafeCall();
					});
				}
			});
		}
	}
}
