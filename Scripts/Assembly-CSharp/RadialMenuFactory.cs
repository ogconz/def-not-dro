using System.Collections.Generic;
using UnityEngine;

public class RadialMenuFactory
{
	private const string RadialActionsDataPath = "ui/radialmenu/radialactions/";

	private List<RadialActionData> m_radialActionDatas = new List<RadialActionData>();

	private UIRadialMenuContext m_radialMenuTemplate;

	public RadialMenuFactory(UIRadialMenuContext radialMenuTemplate)
	{
		m_radialMenuTemplate = radialMenuTemplate;
	}

	public UIRadialMenuContext CreateRadialMenuContextWithActions(params RadialActionData[] radialActions)
	{
		List<UIRadialAction> radialActionViews = m_radialMenuTemplate.radialActionViews;
		if (!IsRadialActionValidCreation(radialActions.Length, radialActionViews.Count))
		{
			return null;
		}
		m_radialActionDatas.AddRange(radialActions);
		for (int i = 0; i < m_radialActionDatas.Count; i++)
		{
			if (!IsRadialActionIndexValidInSlots(m_radialActionDatas[i].forceIndex, radialActionViews.Count))
			{
				return null;
			}
			InitializeNewRadialMenuItem(radialActionViews, i);
		}
		return m_radialMenuTemplate;
	}

	private Sprite GetIconFromResources(RadialActionData radialActionData)
	{
		return ResourcesWrapper.Load<Sprite>(string.Format("{0}{1}", "ui/radialmenu/radialactions/", radialActionData.actionType).ToLower());
	}

	private void InitializeNewRadialMenuItem(List<UIRadialAction> radialActionViews, int index)
	{
		Sprite iconFromResources = GetIconFromResources(m_radialActionDatas[index]);
		int index2 = ((m_radialActionDatas[index].forceIndex != -1) ? m_radialActionDatas[index].forceIndex : index);
		radialActionViews[index2].SetData(m_radialActionDatas[index].radialAction, m_radialActionDatas[index].actionType, m_radialActionDatas[index].localizationKey.Localized(), iconFromResources);
	}

	private bool IsRadialActionValidCreation(int radialActionsLength, int radialActionsViews)
	{
		bool num = radialActionsLength <= radialActionsViews;
		if (!num)
		{
			Debug.LogError($"Can't create radial menu. Radial actions count = {radialActionsLength} is greater than radial menu slots = {radialActionsViews}");
		}
		return num;
	}

	private bool IsRadialActionIndexValidInSlots(int forceIndex, int radialActionsViews)
	{
		bool num = forceIndex <= radialActionsViews;
		if (!num)
		{
			Debug.LogError($"Can't create radial menu. Force index = {forceIndex} is greater than radial menu slots = {radialActionsViews}");
		}
		return num;
	}
}
