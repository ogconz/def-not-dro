using System.Collections.Generic;
using GameInput;
using Minimaps;

public class UIGlobalMinimapContext : UIMapBaseContext<GlobalMinimapVisualizer>
{
	private readonly List<ActionHintData> m_bottomHints = new List<ActionHintData>();

	private readonly List<MultiActionsData> m_sideHints = new List<MultiActionsData>();

	protected override void OnActivate()
	{
		base.OnActivate();
		ShowHints();
		MinimapHelper.PlayStartAnimation(m_visualizer.rootTransform);
	}

	protected override void OnDeactivate()
	{
		m_bottomHints.Clear();
		HideSidePanel();
		base.OnDeactivate();
	}

	private void ShowHints()
	{
		BuildBottomHints();
		ShowBottomPanel(m_bottomHints);
		BuildSideHints();
		ShowSidePanel(m_sideHints);
	}

	private void BuildBottomHints()
	{
		m_bottomHints.Clear();
		m_bottomHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GoToBack
		});
	}

	private void BuildSideHints()
	{
		m_sideHints.Clear();
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.scale.Localized(), ActionType.ZoomOut, ActionType.ZoomIn));
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.visualise_map_legend.Localized(), ActionType.NextCamera, HideMapLegend));
	}

	private void HideMapLegend()
	{
		m_visualizer.TogglePlayerNamesVisibility();
	}

	private void GoToBack()
	{
		base.owner.Get<UIInGameMenuContext>().SetHidden(hidden: false);
		Hide();
	}
}
