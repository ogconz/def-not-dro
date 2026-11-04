using Cysharp.Threading.Tasks;
using ModIO;
using UnityEngine.UI;

namespace Service.UGC.ModIO.Authentication
{
	public class ModIOEmailAuthentication : ModIOAuthentication
	{
		private const string linkPrivacyPolicyAndTermsOfUse = "https://mod.io/terms";

		public override async UniTask<bool> AuthenticateAsync()
		{
			if (!(await IsAuthenticatedAsync()) && await RequestAuthViaEmailAsync())
			{
				await SubmitEmailAuthCodeAsync();
			}
			return await IsAuthenticatedAsync();
		}

		private async UniTask<bool> RequestAuthViaEmailAsync()
		{
			while (true)
			{
				AutoResetUniTaskCompletionSource<string> tcsAquireAddress = AutoResetUniTaskCompletionSource<string>.Create();
				AutoResetUniTaskCompletionSource<bool> tcsCancel = AutoResetUniTaskCompletionSource<bool>.Create();
				UIMessageBox.ShowInputDialog(string.Format(LocalizationKeys.ugc_manager_authentication_email_title.Localized(), "https://mod.io/terms"), LocalizationKeys.ugc_manager_authentication_email_input_field.Localized(), null, InputField.ContentType.EmailAddress, delegate
				{
					tcsCancel.TrySetResult(result: true);
					tcsAquireAddress.TrySetResult(null);
				}, btnOkText: LocalizationKeys.Ok.Localized(), btnCancelText: LocalizationKeys.Cancel.Localized(), actionOk: delegate(string emailAddress)
				{
					tcsAquireAddress.TrySetResult(emailAddress);
				});
				string text = await tcsAquireAddress.Task;
				if (string.IsNullOrEmpty(text))
				{
					if (tcsCancel.Task.Status.IsCompleted() && tcsCancel.Task.GetAwaiter().GetResult())
					{
						return false;
					}
					await UniTask.Yield();
					continue;
				}
				if ((await ModIOUnityAsync.RequestAuthenticationEmail(text)).Succeeded())
				{
					break;
				}
				AutoResetUniTaskCompletionSource<bool> tcsWarning = AutoResetUniTaskCompletionSource<bool>.Create();
				UIMessageBox.ShowWarning(LocalizationKeys.ugc_manager_authentication_email_error.Localized(), delegate
				{
					tcsWarning.TrySetResult(result: true);
				}, LocalizationKeys.Ok.Localized());
				await tcsWarning.Task;
			}
			return true;
		}

		private async UniTask<bool> SubmitEmailAuthCodeAsync()
		{
			while (true)
			{
				AutoResetUniTaskCompletionSource<string> tcs = AutoResetUniTaskCompletionSource<string>.Create();
				UIMessageBox.ShowInputDialog(LocalizationKeys.ugc_manager_authentication_submit_code_title.Localized(), LocalizationKeys.ugc_manager_authentication_submit_code_input_field.Localized(), null, InputField.ContentType.Standard, delegate
				{
					tcs.TrySetResult(null);
				}, btnOkText: LocalizationKeys.Ok.Localized(), btnCancelText: LocalizationKeys.Cancel.Localized(), actionOk: delegate(string authCode)
				{
					tcs.TrySetResult(authCode);
				});
				string text = await tcs.Task;
				if (string.IsNullOrEmpty(text))
				{
					return false;
				}
				if ((await ModIOUnityAsync.SubmitEmailSecurityCode(text)).Succeeded())
				{
					break;
				}
				AutoResetUniTaskCompletionSource<bool> tcsWarning = AutoResetUniTaskCompletionSource<bool>.Create();
				UIMessageBox.ShowWarning(LocalizationKeys.ugc_manager_authentication_email_submit_code_error.Localized(), delegate
				{
					tcsWarning.TrySetResult(result: true);
				}, LocalizationKeys.Ok.Localized());
				await tcsWarning.Task;
			}
			return true;
		}
	}
}
