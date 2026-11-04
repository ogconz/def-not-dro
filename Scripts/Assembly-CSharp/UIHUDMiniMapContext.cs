using System;
using Binding;
using DI;
using Minimaps;
using PrefsModels;

public class UIHUDMiniMapContext : UIMapBaseContext<HUDMinimapVisualizer>
{
	private BoolProperty m_visible = new BoolProperty(startValue: true);

	private GlobalModel m_globals;

	protected override void OnActivate()
	{
		base.OnActivate();
		m_globals = DependencyInjector.Resolve<GamePrefs>().globals;
		SetupVisibility();
		GlobalModel globals = m_globals;
		globals.onShowMinimapChanged = (Action)Delegate.Combine(globals.onShowMinimapChanged, new Action(SetupVisibility));
	}

	protected override void OnDeactivate()
	{
		GlobalModel globals = m_globals;
		globals.onShowMinimapChanged = (Action)Delegate.Remove(globals.onShowMinimapChanged, new Action(SetupVisibility));
		base.OnDeactivate();
	}

	private void SetupVisibility()
	{
		m_visible.value = m_globals.showMinimap;
	}
}
