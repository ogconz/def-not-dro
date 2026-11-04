using System;
using Binding;
using Cysharp.Threading.Tasks;
using GameOverlay;
using ModIO;
using Services.UGC.Interfaces;

namespace Service.UGC.ModIO.Authentication
{
	public abstract class ModIOAuthentication : IUGCAuthentication
	{
		protected TermsOfUse m_termsOfUse;

		private BoolProperty m_isAuthenticated = new BoolProperty();

		public Action<bool> onAuthChanged { get; set; }

		public bool Initialize()
		{
			m_isAuthenticated.OnValueChanged += OnAuthenticatedChangedHandler;
			Result result = ModIOUnity.InitializeForUser(GetUserProfileIdentifier());
			if (!result.Succeeded())
			{
				GameLog.Print(AlertLevel.Error, "ModMaps", "ModIO plugin initialization failed: " + result.message);
				return false;
			}
			return true;
		}

		protected virtual string GetUserProfileIdentifier()
		{
			return Overlay.GetPlayerHashString();
		}

		public async UniTask<bool> IsAuthenticatedAsync()
		{
			Result result = await ModIOUnityAsync.IsAuthenticated();
			return m_isAuthenticated.value = result.Succeeded();
		}

		public abstract UniTask<bool> AuthenticateAsync();

		public bool Deauthenticate()
		{
			m_isAuthenticated.value = false;
			return true;
		}

		private void OnAuthenticatedChangedHandler()
		{
			onAuthChanged.SafeCall(m_isAuthenticated.value);
		}
	}
}
