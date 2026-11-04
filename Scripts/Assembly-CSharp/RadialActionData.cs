using System;
using GameInput;

public class RadialActionData
{
	private System.Action m_radialAction;

	private ActionType m_actionType;

	private LocalizationKeys.Key m_localizationKey;

	private int m_forceIndex;

	public System.Action radialAction => m_radialAction;

	public LocalizationKeys.Key localizationKey => m_localizationKey;

	public ActionType actionType => m_actionType;

	public int forceIndex => m_forceIndex;

	public RadialActionData(System.Action radialAction, ActionType actionType, LocalizationKeys.Key localizationKey, int forceIndex = -1)
	{
		m_radialAction = radialAction;
		m_actionType = actionType;
		m_localizationKey = localizationKey;
		m_forceIndex = forceIndex;
	}
}
