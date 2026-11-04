using System.Collections.Generic;
using Binding;
using Binding.Components;
using UnityEngine;
using UnityEngine.UI;

public class UISidePanelContext : BaseHintPanelContext
{
	[SerializeField]
	private Transform m_groupsContainer;

	[SerializeField]
	private GameObject m_compositeHintPrefab;

	private BoolProperty m_transparent = new BoolProperty();

	private List<GameObject> m_containers = new List<GameObject>();

	public static UISidePanelContext Show(ContextOwner owner, IReadOnlyList<MultiActionsData> actions, bool ignoreRepeat = false, bool transparent = false)
	{
		if (owner == null)
		{
			UIUtils.LogNullReference("UIBottomPanelContext.Show", "owner");
			return null;
		}
		UISidePanelContext uISidePanelContext = (owner.IsActive<UISidePanelContext>() ? owner.Get<UISidePanelContext>() : owner.Add<UISidePanelContext>());
		if (uISidePanelContext != null)
		{
			uISidePanelContext.SetTransparent(transparent);
			uISidePanelContext.Clear();
			foreach (MultiActionsData action in actions)
			{
				uISidePanelContext.AddElement(action, ignoreRepeat);
			}
			uISidePanelContext.BringForwardByDepth();
		}
		uISidePanelContext.UpdateCanvasOffsetInXrMode();
		return uISidePanelContext;
	}

	public override bool IsAllowNavigation()
	{
		return false;
	}

	public void SetTransparent(bool transparent)
	{
		m_transparent.value = transparent;
	}

	public void AddElement(MultiActionsData dto, bool ignoreRepeat = false)
	{
		Transform transform = AllocateNextContainer();
		HorizontalLayoutGroup component = prefabHint.gameObject.GetComponent<HorizontalLayoutGroup>();
		int num = dto.type.Length;
		for (int i = 0; i < num; i++)
		{
			ActionElementBinding actionElementBinding = m_aebPool.AllocateNext(prefabHint, transform);
			if (actionElementBinding == null)
			{
				Clear();
				break;
			}
			HorizontalLayoutGroup component2 = actionElementBinding.gameObject.GetComponent<HorizontalLayoutGroup>();
			component2.padding = component.padding;
			component2.spacing = component.spacing;
			string hintText = string.Empty;
			if (i == 0)
			{
				hintText = dto.hint;
				component2.padding.right = 0;
			}
			else if (i == num - 1)
			{
				component2.spacing = 0f;
				component2.padding.left = 0;
			}
			else
			{
				component2.spacing = 0f;
				component2.padding.left = 0;
				component2.padding.right = 0;
			}
			actionElementBinding.transform.SetParent(transform);
			actionElementBinding.transform.position = Vector3.zero;
			actionElementBinding.transform.localPosition = Vector3.zero;
			actionElementBinding.transform.SetAsLastSibling();
			actionElementBinding.SetData(hintText, dto.type[i], dto.action[i], ignoreRepeat);
		}
	}

	public void Clear()
	{
		m_aebPool.Clear();
		for (int i = 0; i < m_containers.Count; i++)
		{
			m_containers[i].SetActive(value: false);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		for (int i = 0; i < m_containers.Count; i++)
		{
			if ((bool)m_containers[i])
			{
				Object.Destroy(m_containers[i]);
				m_containers[i] = null;
			}
		}
		m_containers.Clear();
		m_containers = null;
	}

	private Transform AllocateNextContainer()
	{
		GameObject gameObject;
		for (int i = 0; i < m_containers.Count; i++)
		{
			gameObject = m_containers[i];
			if (!gameObject.gameObject.activeSelf)
			{
				gameObject.gameObject.SetActive(value: true);
				return gameObject.transform;
			}
		}
		gameObject = Object.Instantiate(m_compositeHintPrefab, m_groupsContainer);
		m_containers.Add(gameObject);
		return gameObject.transform;
	}
}
