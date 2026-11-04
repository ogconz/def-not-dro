using Cysharp.Threading.Tasks;
using ModIO;
using UnityEngine;
using Utlis;

namespace Service.UGC.ModIO.Authentication
{
	public abstract class ModIOSSOAutentication : ModIOAuthentication
	{
		protected string m_platformAuthToken;

		protected Result? m_authResult;

		public override async UniTask<bool> AuthenticateAsync()
		{
			if (await IsAuthenticatedAsync())
			{
				return true;
			}
			if (!(await CheckConsentTermsOfUseAsync()))
			{
				return false;
			}
			m_platformAuthToken = await GetPlatformAuthTokenAsync();
			bool flag = string.IsNullOrEmpty(m_platformAuthToken);
			if (!flag)
			{
				flag = !(await AuthenticateWithPlatformTokenAsync());
			}
			if (flag)
			{
				if (m_authResult.HasValue && m_authResult.HasValue)
				{
					GameLog.Print(AlertLevel.Error, "ModMaps", "Failed to authenticate with platform token: " + m_authResult.Value.message);
				}
				GUICommonGodVoice.ShowText(LocalizationKeys.ugc_manager_general_authentication_error.Localized(), 3f);
			}
			return await IsAuthenticatedAsync();
		}

		private async UniTask<bool> CheckConsentTermsOfUseAsync()
		{
			bool flag = await GetTermsOfUseAsync();
			if (flag)
			{
				flag = await TrySubmitTermsOfUseUsingDialogAsync();
			}
			return flag;
		}

		private async UniTask<bool> GetTermsOfUseAsync()
		{
			ResultAnd<TermsOfUse> resultAnd = await ModIOUnityAsync.GetTermsOfUse(SystemLanguageConverter.SystemLanguageToIso639LanguageCode(LocalizationService.languageMeta.languageType));
			if (!resultAnd.result.Succeeded())
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.ugc_manager_failed_to_get_terms_of_use.Localized(), 3f);
				GameLog.Print(AlertLevel.Error, "ModMaps", "Failed to get terms of use: " + resultAnd.result.message);
				return false;
			}
			m_termsOfUse = resultAnd.value;
			return true;
		}

		private async UniTask<bool> TrySubmitTermsOfUseUsingDialogAsync()
		{
			AutoResetUniTaskCompletionSource<bool> tcs = AutoResetUniTaskCompletionSource<bool>.Create();
			UIMessageBox.ShowPrivacyPolicyBaseDialog("mod.io", m_termsOfUse.termsOfUse, m_termsOfUse.agreeText, delegate
			{
				tcs.TrySetResult(result: true);
			}, m_termsOfUse.disagreeText, delegate
			{
				tcs.TrySetResult(result: false);
			}, m_termsOfUse.links[1].name, delegate
			{
				Application.OpenURL(m_termsOfUse.links[1].url);
			}, m_termsOfUse.links[2].name, delegate
			{
				Application.OpenURL(m_termsOfUse.links[2].url);
			});
			return await tcs.Task;
		}

		protected abstract UniTask<string> GetPlatformAuthTokenAsync();

		protected abstract UniTask<bool> AuthenticateWithPlatformTokenAsync();
	}
}
