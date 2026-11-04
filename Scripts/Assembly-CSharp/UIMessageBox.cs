using System;
using System.Collections.Generic;
using DI;
using GameInput;
using GameOverlay;
using UI._UGUI_;
using UnityEngine;
using UnityEngine.UI;

public class UIMessageBox : MonoBehaviour
{
	private GameManager m_gameManager;

	public static UIMessageBox instance { get; private set; }

	public System.Action closeAction { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	private void Start()
	{
		m_gameManager = DependencyInjector.Resolve<GameManager>();
	}

	private ContextOwner GetOwnerContext()
	{
		BaseGameState baseGameState = States.current as BaseGameState;
		if (!(baseGameState != null))
		{
			return null;
		}
		return baseGameState.contextOwner;
	}

	public static void ShowMode0(string text, string title, bool allowClose, System.Action closeAction, string closeBtnText = null, ContextOwner owner = null)
	{
		owner = owner ?? instance.GetOwnerContext();
		UIBaseDialogContext ctx = owner.Get<UIBaseDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = owner.Push<UIBaseDialogContext>();
			ctx.title.value = title;
			ctx.allowClose.value = allowClose;
			ctx.actionCancel = delegate
			{
				ctx.Hide();
			};
			ctx.BuildDialog(new DialogButtonData[1]
			{
				new DialogButtonData
				{
					text = closeBtnText,
					action = closeAction
				}
			});
		}
	}

	public static void ShowMode0(string text, string title = null, bool allowClose = true, string closeButtonText = null, ContextOwner owner = null)
	{
		ShowMode0(text, title, allowClose, null, closeButtonText, owner);
	}

	public static void ShowMode1(string text1, string text2, string title, bool allowClose = true, System.Action closeAction = null, string closeBtnText = null, ContextOwner owner = null)
	{
		GUICommonGodVoice.ShowText(title + ": " + text1 + "; " + text2);
		GUICommonGodVoice.ShowText(FormatTitleText(FormatTitleText(title, ": ", text1), "; ", text2));
	}

	public static void ShowHint(string text, string title, string console_dontshow = null, ContextOwner owner = null)
	{
		GUICommonGodVoice.ShowText(FormatTitleText(title, ": ", text));
	}

	private static string FormatTitleText(string title, string separator, string text)
	{
		if (string.IsNullOrEmpty(title))
		{
			return text;
		}
		if (string.IsNullOrEmpty(text))
		{
			return title;
		}
		return title + separator + text;
	}

	public static void ShowQuestion(string title, System.Action actionCancel = null, string btnCancelText = "", System.Action actionOk = null, string btnOkText = "")
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIBaseDialogContext ctx = ownerContext.Get<UIBaseDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = ownerContext.Push<UIBaseDialogContext>();
			ctx.title.value = title;
			ctx.actionCancel = delegate
			{
				ctx.Hide();
				actionCancel.SafeCall();
			};
			ctx.BuildDialog(new DialogButtonData[2]
			{
				new DialogButtonData
				{
					text = btnOkText,
					action = delegate
					{
						actionOk.SafeCall();
						ctx.Hide();
					}
				},
				new DialogButtonData
				{
					text = btnCancelText,
					action = ctx.actionCancel
				}
			});
		}
	}

	public static void ShowWarning(string title, System.Action actionNext = null, string btnNextText = "")
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIBaseDialogContext ctx = ownerContext.Get<UIBaseDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = ownerContext.Push<UIBaseDialogContext>();
			ctx.title.value = title;
			ctx.actionCancel = delegate
			{
				ctx.Hide();
				actionNext.SafeCall();
			};
			ctx.BuildDialog(new DialogButtonData[1]
			{
				new DialogButtonData
				{
					text = btnNextText,
					action = ctx.actionCancel
				}
			});
		}
	}

	public static void ShowDialog(string title, System.Action actionCancel, DialogButtonData[] btns)
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIBaseDialogContext ctx = ownerContext.Get<UIBaseDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = ownerContext.Push<UIBaseDialogContext>();
			ctx.title.value = title;
			ctx.actionCancel = delegate
			{
				actionCancel.SafeCall();
				ctx.Hide();
			};
			foreach (DialogButtonData obj in btns)
			{
				obj.action = (System.Action)Delegate.Combine(obj.action, new System.Action(ctx.Hide));
			}
			ctx.BuildDialog(btns);
		}
	}

	public static void ShowPrivacyPolicyBaseDialog(string title, string content, string btnOkText, System.Action actionOk, string btnCancelText, System.Action actionCancel, string termsOfUseText, System.Action actionTermsOfUse, string privacyPolicyText, System.Action actionPrivacyPolicy)
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIPrivacyPolicyBaseDialogContext ctx = ownerContext.Get<UIPrivacyPolicyBaseDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = ownerContext.Push<UIPrivacyPolicyBaseDialogContext>();
			ctx.SetDialogData(title, content, new List<DialogButtonData>
			{
				new DialogButtonData
				{
					text = btnOkText,
					action = delegate
					{
						ctx.Hide();
						actionOk.SafeCall();
					}
				},
				new DialogButtonData
				{
					text = btnCancelText,
					action = delegate
					{
						ctx.Hide();
						actionCancel.SafeCall();
					}
				}
			}, new List<DialogButtonData>
			{
				new DialogButtonData
				{
					text = termsOfUseText,
					action = actionTermsOfUse
				},
				new DialogButtonData
				{
					text = privacyPolicyText,
					action = actionPrivacyPolicy
				}
			});
		}
	}

	public static void ShowComplexDialog(string title, System.Action actionCancel = null, string btnCancelText = "", System.Action actionOk = null, string btnOkText = "", System.Action actionNo = null, string btnNoText = "", System.Action extraAction = null, string extraBtnText = "")
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIBaseDialogContext ctx = ownerContext.Get<UIBaseDialogContext>();
		if (ctx != null && ctx.gameObject.activeSelf)
		{
			return;
		}
		ctx = ownerContext.Push<UIBaseDialogContext>();
		ctx.title.value = title;
		ctx.actionCancel = delegate
		{
			ctx.Hide();
			actionCancel.SafeCall();
		};
		List<DialogButtonData> list = new List<DialogButtonData>();
		list.Add(new DialogButtonData
		{
			text = btnOkText,
			action = delegate
			{
				ctx.Hide();
				actionOk.SafeCall();
			}
		});
		if (!string.IsNullOrWhiteSpace(btnNoText))
		{
			list.Add(new DialogButtonData
			{
				text = btnNoText,
				action = delegate
				{
					ctx.Hide();
					actionNo.SafeCall();
				}
			});
		}
		if (extraAction != null)
		{
			list.Add(new DialogButtonData
			{
				text = extraBtnText,
				action = delegate
				{
					ctx.Hide();
					extraAction.SafeCall();
				}
			});
		}
		list.Add(new DialogButtonData
		{
			text = btnCancelText,
			action = ctx.actionCancel
		});
		ctx.BuildDialog(list.ToArray());
	}

	public static void ShowInputDialog(string title, string value, string placeHolder = null, InputField.ContentType contentType = InputField.ContentType.Standard, System.Action actionCancel = null, string btnCancelText = "", Action<string> actionOk = null, string btnOkText = "", int maxLength = -1)
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIInputDialogContext ctx = ownerContext.Get<UIInputDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = ownerContext.Push<UIInputDialogContext>();
			ctx.title.value = ((title == null) ? "" : title);
			ctx.text = ((value == null) ? "" : value);
			if (maxLength > 0)
			{
				ctx.input.characterLimit = maxLength;
			}
			ctx.placeHolder = ((placeHolder == null) ? "" : placeHolder);
			ctx.contentType = contentType;
			ctx.actionCancel = delegate
			{
				actionCancel.SafeCall();
				ctx.Hide();
			};
			ctx.BuildDialog(new DialogButtonData[2]
			{
				new DialogButtonData
				{
					text = btnOkText,
					action = delegate
					{
						actionOk.SafeCall(ctx.text);
						ctx.Hide();
					}
				},
				new DialogButtonData
				{
					text = btnCancelText,
					action = ctx.actionCancel
				}
			});
		}
	}

	public static void ShowInputDialog(string title, string value, string placeHolder = null, System.Action actionCancel = null, string btnCancelText = "", Action<string> actionOk = null, string btnOkText = "", int maxLength = -1)
	{
		ShowInputDialog(title, value, placeHolder, InputField.ContentType.Standard, actionCancel, btnCancelText, actionOk, btnOkText, maxLength);
	}

	public static void ShowPasswordDialog(string title, string value, string placeHolder = null, System.Action actionCancel = null, string btnCancelText = "", Action<string> actionOk = null, string btnOkText = "")
	{
		ShowInputDialog(title, value, placeHolder, InputField.ContentType.Password, actionCancel, btnCancelText, actionOk, btnOkText);
	}

	public static void ShowDLCDialog(string message, DLCCode dlcCode, System.Action extraAction = null, string extraText = "")
	{
		ShowComplexDialog(message, null, LocalizationKeys.Cancel.Localized(), delegate
		{
			Overlay.instance.ShowBuyDLCPage(dlcCode);
		}, LocalizationKeys.buy_dlc.Localized(), delegate
		{
			Overlay.instance.ShowBuyDLCPage(DLCCode.SeasonPass);
		}, LocalizationKeys.buy_season_pass.Localized(), extraAction, extraText);
	}

	public static void ShowSubscriptionDialogToBuy(System.Action extraAction = null, string extraText = "")
	{
		ShowComplexDialog(LocalizationKeys.subscription_main_window_info.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
		{
			Overlay.instance.ShowBuyDLCPage(DLCCode.SeasonPass);
		}, LocalizationKeys.buy_season_pass.Localized(), delegate
		{
			Application.OpenURL("https://discord.gg/bXpFeuU");
		}, LocalizationKeys.settings_discord.Localized(), extraAction, extraText);
	}

	public static void ShowItemDialog(string itemName, int count, Sprite itemIcon, System.Action onDone = null)
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UINewItemDialogContext ctx = ownerContext.Get<UINewItemDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = ownerContext.Push<UINewItemDialogContext>();
			ctx.itemName.value = itemName;
			ctx.itemsCount.value = $"x{count}";
			ctx.itemSprite.value = itemIcon;
			ctx.allowClose.value = false;
			ctx.actionCancel = delegate
			{
				ctx.Hide();
				onDone.SafeCall();
			};
			ctx.BuildDialog(new DialogButtonData[1]
			{
				new DialogButtonData
				{
					text = LocalizationKeys.Ok.Localized(),
					action = delegate
					{
						ctx.Hide();
						onDone.SafeCall();
					}
				}
			});
		}
	}

	public static void ShowRewardDialog(string rewardDescription, string rewardVendorDescription, Sprite rewardIcon, bool isCar, System.Action onDone = null)
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIRewardReceivedDialogContext ctx = ownerContext.Get<UIRewardReceivedDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = ownerContext.Push<UIRewardReceivedDialogContext>();
			ctx.rewardDescription.value = rewardDescription;
			ctx.rewardVendorDescription.value = rewardVendorDescription;
			ctx.rewardSprite.value = rewardIcon;
			ctx.showCarReward.value = isCar;
			ctx.allowClose.value = false;
			ctx.actionCancel = delegate
			{
				ctx.Hide();
				onDone.SafeCall();
			};
			ctx.BuildDialog(new DialogButtonData[1]
			{
				new DialogButtonData
				{
					text = LocalizationKeys.Ok.Localized(),
					action = delegate
					{
						ctx.Hide();
						onDone.SafeCall();
					}
				}
			});
		}
	}

	public static void ShowProgressBar(string title, Action<Action<float>, System.Action> onInitProgress, string textCancelButton = null, System.Action onCancel = null, bool showBG = false)
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIProgressBarContext ctx = ownerContext.Get<UIProgressBarContext>();
		if (ctx != null && ctx.gameObject.activeSelf)
		{
			return;
		}
		ctx = ownerContext.Push<UIProgressBarContext>();
		ctx.title.value = title;
		ctx.showBG.value = showBG;
		System.Action param = delegate
		{
			ctx.DestroyButtonsContainer();
			ctx.Hide();
		};
		if (!string.IsNullOrWhiteSpace(textCancelButton))
		{
			List<DialogButtonData> list = new List<DialogButtonData>();
			list.Add(new DialogButtonData
			{
				text = textCancelButton,
				action = delegate
				{
					ctx.DestroyButtonsContainer();
					ctx.Hide();
					onCancel.SafeCall();
				}
			});
			ctx.BuildDialog(list.ToArray());
		}
		onInitProgress.SafeCall(ctx.FillProgressBar, param);
	}

	public static void ShowMessageWithTitle(string title, string message, System.Action onDone = null, string btn0Text = "", System.Action onBtn0Pressed = null, string btn1Text = "", System.Action onBtn1Pressed = null)
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIMessageWithTitleContext ctx = ownerContext.Get<UIMessageWithTitleContext>();
		if (ctx != null && ctx.gameObject.activeSelf)
		{
			return;
		}
		ctx = ownerContext.Push<UIMessageWithTitleContext>();
		ctx.messageTitle.value = title;
		ctx.messageText.value = message;
		ctx.allowClose.value = false;
		ctx.actionCancel = delegate
		{
			ctx.Hide();
			onDone.SafeCall();
		};
		List<DialogButtonData> list = new List<DialogButtonData>();
		list.Add(new DialogButtonData
		{
			text = LocalizationKeys.Ok.Localized(),
			action = delegate
			{
				ctx.Hide();
				onDone.SafeCall();
			}
		});
		if (!string.IsNullOrWhiteSpace(btn0Text))
		{
			list.Add(new DialogButtonData
			{
				text = btn0Text,
				action = delegate
				{
					ctx.Hide();
					onBtn0Pressed.SafeCall();
				}
			});
		}
		if (!string.IsNullOrWhiteSpace(btn1Text))
		{
			list.Add(new DialogButtonData
			{
				text = btn1Text,
				action = delegate
				{
					ctx.Hide();
					onBtn1Pressed.SafeCall();
				}
			});
		}
		ctx.BuildDialog(list.ToArray());
	}

	public static void HideBaseDialog()
	{
		UIBaseDialogContext uIBaseDialogContext = instance.GetOwnerContext().Get<UIBaseDialogContext>();
		if (!(uIBaseDialogContext == null) && uIBaseDialogContext.gameObject.activeSelf)
		{
			uIBaseDialogContext.Hide();
		}
	}

	public static void ShowDeviceTypeListQuestion(string mainTitle, string optionalTitle, List<GameInput.DeviceType> allowDeviceTypes, System.Action actionCancel = null, string btnCancelText = "", Action<GameInput.DeviceType> actionOk = null, string btnOkText = "")
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIBaseDeviceToggleDialogContext ctx = ownerContext.Get<UIBaseDeviceToggleDialogContext>();
		if (!(ctx != null) || !ctx.gameObject.activeSelf)
		{
			ctx = ownerContext.Push<UIBaseDeviceToggleDialogContext>();
			ctx.title.value = mainTitle;
			ctx.optionalTitle.value = optionalTitle;
			ctx.allowedDeviceTypeList = allowDeviceTypes;
			ctx.actionCancel = delegate
			{
				ctx.Hide();
				actionCancel.SafeCall();
			};
			ctx.BuildDialog(new DialogButtonData[2]
			{
				new DialogButtonData
				{
					text = btnOkText,
					action = delegate
					{
						actionOk.SafeCall(allowDeviceTypes[ctx.index]);
						ctx.Hide();
					}
				},
				new DialogButtonData
				{
					text = btnCancelText,
					action = ctx.actionCancel
				}
			});
			ctx.InitAllowedDevicesType();
		}
	}

	public static void ShowInputCategoryDialog(string title, string value, List<string> categories, string placeHolder = null, string description = null, InputField.ContentType contentType = InputField.ContentType.Standard, string btnOkText = "", string btnCancelText = "", Action<string, List<int>, bool?> actionOk = null, System.Action actionCancel = null, int maxLength = -1, Func<string, List<int>, bool> successPredicate = null)
	{
		ContextOwner ownerContext = instance.GetOwnerContext();
		UIInputCategoryDialog ctx = ownerContext.Get<UIInputCategoryDialog>();
		if (ctx != null && ctx.gameObject.activeSelf)
		{
			return;
		}
		ctx = ownerContext.Push<UIInputCategoryDialog>();
		ctx.title.value = title;
		ctx.text = value;
		ctx.placeHolder = placeHolder;
		ctx.contentType = contentType;
		ctx.categoryList = categories;
		ctx.description = description;
		if (maxLength > 0)
		{
			ctx.input.characterLimit = maxLength;
		}
		ctx.actionCancel = delegate
		{
			actionCancel.SafeCall();
			ctx.Hide();
		};
		ctx.BuildDialog(new DialogButtonData[2]
		{
			new DialogButtonData
			{
				text = btnOkText,
				action = delegate
				{
					if (successPredicate == null)
					{
						CallSuccessAction();
					}
					else if (successPredicate(ctx.text, ctx.selectedCategoriesIndexes))
					{
						CallSuccessAction();
					}
				}
			},
			new DialogButtonData
			{
				text = btnCancelText,
				action = ctx.actionCancel
			}
		});
		ctx.InitAllowedCategories();
		void CallSuccessAction()
		{
			actionOk?.Invoke(ctx.text, ctx.selectedCategoriesIndexes, ctx.isPrivate);
			ctx.Hide();
		}
	}
}
