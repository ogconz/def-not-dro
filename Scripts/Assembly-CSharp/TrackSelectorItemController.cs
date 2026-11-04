using Binding;
using Binding.Base;

public class TrackSelectorItemController : BaseItemController<TrackSelectorListItem>
{
	private TrackSelectorListItem m_listItem;

	private TrackSubItemController[] m_subItemsControllers;

	public readonly Property<int> selectionIndex = new IntProperty(-1);

	protected override void Initialize()
	{
		base.Initialize();
		InitSubItemsControllersIfNeed();
	}

	private void InitSubItemsControllersIfNeed()
	{
		if (m_subItemsControllers == null)
		{
			m_subItemsControllers = GetComponentsInChildren<TrackSubItemController>();
		}
	}

	public override void OnClick()
	{
		m_listItem?.OnClick();
		base.OnClick();
	}

	public override void SetItem(TrackSelectorListItem listItem)
	{
		InitSubItemsControllersIfNeed();
		m_listItem = listItem;
		if (m_listItem != null)
		{
			selectionIndex.value = m_listItem.selectionIndex;
			if (m_subItemsControllers != null)
			{
				for (int i = 0; i < m_subItemsControllers.Length; i++)
				{
					m_subItemsControllers[i]?.SetItem(m_listItem.subItems[i]);
				}
			}
		}
		else
		{
			selectionIndex.value = -1;
			if (m_subItemsControllers != null)
			{
				TrackSubItemController[] subItemsControllers = m_subItemsControllers;
				foreach (TrackSubItemController trackSubItemController in subItemsControllers)
				{
					if (!(trackSubItemController == null))
					{
						trackSubItemController.SetItem(null);
					}
				}
			}
		}
		base.SetItem(listItem);
	}
}
