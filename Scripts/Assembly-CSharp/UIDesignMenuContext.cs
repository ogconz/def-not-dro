using Binding;
using Binding.Base;
using GameInput;
using GameOverlay;

[AudioFXBlocker(false)]
public class UIDesignMenuContext : BaseBindingBehaviourTarget
{
	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	private BoolProperty m_vinylsSupport = new BoolProperty(false);

	private BoolProperty m_paintSupport = new BoolProperty(false);

	protected override void OnActivate()
	{
		base.OnActivate();
		Overlay.instance.richPresence.Show(RichPresenceStatus.CarDesigning);
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.design_title.Localized());
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = GoToBack
			},
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = Selection
			}
		});
		m_vinylsSupport.value = false;
		m_paintSupport.value = false;
		if ((bool)m_car3dView)
		{
			m_car3dView.Set3DModelVisible(value: true);
			RaceCar raceCar = m_car3dView.GetRaceCar();
			if ((bool)raceCar)
			{
				m_vinylsSupport.value = !raceCar.vinylsNotSupport;
			}
			m_paintSupport.value = true;
		}
		if (CameraRotation.instance != null)
		{
			CameraRotation.instance.useZoom = true;
		}
	}

	protected override void OnDeactivate()
	{
		if (CameraRotation.instance != null)
		{
			CameraRotation.instance.useZoom = false;
		}
		HideTopPanel();
	}

	private void GoToBack()
	{
		Hide();
		base.owner.Push<UIGarageContext>();
	}

	private void Selection()
	{
		SubmitCurrentSelected();
	}

	public void GoToPaint()
	{
		if (m_paintSupport.value)
		{
			Hide();
			base.owner.Push<UIPaintCustomizationContext>();
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.paint_not_support.Localized(), 3f);
		}
	}

	public void GoVinyls()
	{
		if (m_vinylsSupport.value)
		{
			Hide();
			base.owner.Push<UIVinylsContext>();
		}
		else
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.vinyls_not_support.Localized(), 3f);
		}
	}

	public void GoChevrons()
	{
	}
}
