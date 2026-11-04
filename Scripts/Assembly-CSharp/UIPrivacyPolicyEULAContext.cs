using System;
using DI;
using PrefsModels;
using UnityEngine;

[GameHotkeyBlocker(true)]
[RaycastBlocker(true)]
public class UIPrivacyPolicyEULAContext : UIPrivacyPolicyBaseDialogContext
{
	private enum AccessState
	{
		AgeConfirm = 0,
		ParentAgreegment = 1,
		ParentDenied = 2,
		LicenseAgreement = 3,
		PrivacyPolicy = 4,
		PrivacyPolicyDenied = 5
	}

	public static Action<bool> onEulaClosedResult;

	private const string URLLicenceAgreementRu = "https://carx-online.com/ru/carx-technologies-eula";

	private const string URLLicenceAgreementEng = "https://carx-online.com/en/carx-technologies-eula";

	private const string URLPrivacyPolicyRu = "https://carx-online.com/ru/carx-technologies-privacy-policy";

	private const string URLPrivacyPolicyEng = "https://carx-online.com/en/carx-technologies-privacy-policy";

	private static string s_urlLicenceAgreement;

	private static string s_urlPrivacyPolicy;

	private static GlobalModel s_globals;

	protected override void OnActivate()
	{
		if (s_globals == null)
		{
			s_globals = DependencyInjector.Resolve<GamePrefs>().globals;
		}
		InitializeLanguageDialog();
		SetToState(AccessState.AgeConfirm);
		base.OnActivate();
	}

	public static void InitializeLanguageDialog()
	{
		SystemLanguage currentLanguage = LocalizationService.currentLanguage;
		s_urlLicenceAgreement = ((currentLanguage == SystemLanguage.Russian) ? "https://carx-online.com/ru/carx-technologies-eula" : "https://carx-online.com/en/carx-technologies-eula");
		s_urlPrivacyPolicy = ((currentLanguage == SystemLanguage.Russian) ? "https://carx-online.com/ru/carx-technologies-privacy-policy" : "https://carx-online.com/en/carx-technologies-privacy-policy");
	}

	private void SetToState(AccessState currentState)
	{
		m_buttonsDialog.Clear();
		m_buttonsDialogLinks.Clear();
		m_visibleButtonsLink.value = false;
		switch (currentState)
		{
		case AccessState.AgeConfirm:
			InitAgeConfirmDialog();
			break;
		case AccessState.ParentAgreegment:
			InitParentAgreementDialog();
			break;
		case AccessState.ParentDenied:
			InitParentDeniedDialog();
			break;
		case AccessState.LicenseAgreement:
			InitLicenseAgreement();
			break;
		case AccessState.PrivacyPolicy:
			InitPrivacyPolicy();
			break;
		case AccessState.PrivacyPolicyDenied:
			InitPrivacyPolicyDenied();
			break;
		}
		CreateDialog();
	}

	private void InitAgeConfirmDialog()
	{
		if (s_globals.ageConfirmApproved)
		{
			SetToState(AccessState.LicenseAgreement);
			return;
		}
		m_titleText.value = LocalizationKeys.eula_age_confirm_title.Localized();
		m_contentText.value = LocalizationKeys.eula_age_confirm_main.Localized();
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.Yes.Localized(),
			action = delegate
			{
				SetToState(AccessState.LicenseAgreement);
				s_globals.ageConfirmApproved = true;
			}
		});
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.No.Localized(),
			action = delegate
			{
				SetToState(AccessState.ParentAgreegment);
			}
		});
	}

	private void InitParentAgreementDialog()
	{
		m_contentText.value = LocalizationKeys.eula_parents_confirm_main.Localized();
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.confirm.Localized(),
			action = delegate
			{
				SetToState(AccessState.LicenseAgreement);
				s_globals.ageConfirmApproved = true;
			}
		});
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.refuse.Localized(),
			action = delegate
			{
				SetToState(AccessState.ParentDenied);
			}
		});
	}

	private void InitParentDeniedDialog()
	{
		m_contentText.value = LocalizationKeys.eula_parent_confirm_main_block.Localized();
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.nav_hint_goto_back.Localized(),
			action = delegate
			{
				SetToState(AccessState.ParentAgreegment);
			}
		});
	}

	private void InitLicenseAgreement()
	{
		if (s_globals.licenseApproved)
		{
			SetToState(AccessState.PrivacyPolicy);
			return;
		}
		m_titleText.value = LocalizationKeys.eula_licence_agreegment_title.Localized();
		m_contentText.value = LocalizationKeys.eula_licence_agreegment_main.Localized();
		m_visibleButtonsLink.value = true;
		m_buttonsDialogLinks.Add(new DialogButtonData
		{
			text = m_titleText.value,
			action = delegate
			{
				Application.OpenURL(s_urlLicenceAgreement);
			}
		});
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.accept.Localized(),
			action = delegate
			{
				SetToState(AccessState.PrivacyPolicy);
				s_globals.licenseApproved = true;
			}
		});
	}

	public static void OpenPrivacyPolicy()
	{
		Application.OpenURL(s_urlPrivacyPolicy);
	}

	public static void OpenEULA()
	{
		Application.OpenURL(s_urlLicenceAgreement);
	}

	private void InitPrivacyPolicy()
	{
		if (s_globals.privacyPolicyApprovedV2)
		{
			Close();
			return;
		}
		m_titleText.value = LocalizationKeys.eula_privacy_policy_title.Localized();
		m_contentText.value = LocalizationKeys.eula_privacy_policy_main.Localized();
		m_visibleButtonsLink.value = true;
		m_buttonsDialogLinks.Add(new DialogButtonData
		{
			text = m_titleText.value,
			action = delegate
			{
				OpenPrivacyPolicy();
			}
		});
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.accept.Localized(),
			action = delegate
			{
				s_globals.privacyPolicyApprovedV2 = true;
				Close();
			}
		});
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.refuse.Localized(),
			action = delegate
			{
				SetToState(AccessState.PrivacyPolicyDenied);
			}
		});
	}

	private void InitPrivacyPolicyDenied()
	{
		m_contentText.value = LocalizationKeys.eula_privacy_policy_main_block.Localized();
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.nav_hint_goto_back.Localized(),
			action = delegate
			{
				SetToState(AccessState.PrivacyPolicy);
			}
		});
		m_buttonsDialog.Add(new DialogButtonData
		{
			text = LocalizationKeys.nav_hint_continue.Localized(),
			action = delegate
			{
				Close();
			}
		});
	}

	public static UIPrivacyPolicyEULAContext Show()
	{
		ContextOwner contextOwner = (States.mainCurrent as BaseGameState).contextOwner;
		if (contextOwner.IsActive<UIPrivacyPolicyEULAContext>())
		{
			return null;
		}
		return contextOwner.Push<UIPrivacyPolicyEULAContext>();
	}

	public static bool CheckIsEulaAccess()
	{
		if (s_globals == null)
		{
			s_globals = DependencyInjector.Resolve<GamePrefs>().globals;
		}
		return s_globals.eulaAccepted;
	}

	private void Close()
	{
		foreach (DialogButton button in base.buttons)
		{
			button.gameObject.SetActive(value: false);
		}
		Hide();
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		onEulaClosedResult.SafeCall(s_globals.eulaAccepted);
	}
}
