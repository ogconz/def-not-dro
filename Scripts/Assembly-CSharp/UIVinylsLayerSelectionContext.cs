using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;

public class UIVinylsLayerSelectionContext : BaseBindingBehaviourTarget
{
	private StringProperty m_layersCountLabel = new StringProperty();

	[SerializeField]
	private VinylLayersGrid m_layersList;

	private List<ActionHintData> m_bottomHints = new List<ActionHintData>();

	private Dictionary<int, int> m_frameSkipLevelOfLayers = new Dictionary<int, int>
	{
		{ 0, 0 },
		{ 250, 1 },
		{ 600, 2 }
	};

	protected override void OnActivate()
	{
		base.OnActivate();
		VinylLayersGrid layersList = m_layersList;
		layersList.onClick = (Action<int>)Delegate.Combine(layersList.onClick, new Action<int>(SelectVinyl));
		RefreshHint();
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		VinylLayersGrid layersList = m_layersList;
		layersList.onClick = (Action<int>)Delegate.Remove(layersList.onClick, new Action<int>(SelectVinyl));
	}

	public void FillLayersList(VinylLayersList layers)
	{
		SetFrameSkipLevelToList(layers.GetCount());
		m_layersList.Clear();
		m_layersList.AddRange(layers.items);
		m_layersList.RecalcList();
		m_layersList.selectedIndex = layers.GetSelectedIndex();
		RefreshHint();
	}

	private void RefreshHint()
	{
		m_bottomHints.Clear();
		m_bottomHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GoToBack
		});
		m_bottomHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_select.Localized(),
			type = ActionType.NavSelectBtnA,
			action = ConfirmSelectLayer
		});
		ShowBottomPanel(m_bottomHints, null, ignoreRepeat: true);
		m_layersCountLabel.value = LocalizationKeys.vinyls_layers_count.LocalizedFormat(m_layersList.GetCount());
	}

	private void SetFrameSkipLevelToList(int layersCount)
	{
		foreach (KeyValuePair<int, int> frameSkipLevelOfLayer in m_frameSkipLevelOfLayers)
		{
			if (layersCount > frameSkipLevelOfLayer.Key)
			{
				m_layersList.skipFramesToUpdate = frameSkipLevelOfLayer.Value;
			}
		}
	}

	private void GoToBack()
	{
		Hide();
		base.owner.Push<UIVinylsEditingLayersContext>();
	}

	private void ConfirmSelectLayer()
	{
		Hide();
		base.owner.Push<UIVinylsEditingLayersContext>().SelectLayer(m_layersList.GetSelectedIndex());
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	private void NavRight()
	{
		SelectVinyl(m_layersList.GetSelectedIndex() + 1);
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	private void NavLeft()
	{
		SelectVinyl(m_layersList.GetSelectedIndex() - 1);
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	private void NavUp()
	{
		SelectVinyl(m_layersList.GetSelectedIndex() - m_layersList.GetRowsCount());
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	private void NavDown()
	{
		SelectVinyl(m_layersList.GetSelectedIndex() + m_layersList.GetRowsCount());
	}

	private void SelectVinyl(int index)
	{
		if (m_layersList.GetSelectedIndex() == index)
		{
			ConfirmSelectLayer();
			return;
		}
		int num = m_layersList.GetCount() - 1;
		if (index < 0)
		{
			index = num;
		}
		if (index > num)
		{
			index = 0;
		}
		m_layersList.selectedIndex = index;
	}
}
