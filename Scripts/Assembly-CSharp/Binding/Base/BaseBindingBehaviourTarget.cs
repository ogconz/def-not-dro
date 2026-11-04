using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Binding.Base
{
	public abstract class BaseBindingBehaviourTarget : BaseContext, IBindingTarget
	{
		private BindingProps<BaseBindingBehaviourTarget> m_bindingProps;

		private ElementNavigate m_selectedItem;

		public GraphicRaycaster raycaster;

		private ElementNavigate m_hoveredItem;

		public List<ElementNavigate> navElements { get; } = new List<ElementNavigate>();

		public ElementNavigate lastHoveredNavElement { get; private set; }

		protected ElementNavigate m_currActiveNavElement { get; private set; }

		public string Name => base.name;

		public ElementNavigate selectedNavigate => m_selectedItem;

		public GameObject selectedItem
		{
			get
			{
				if (!(m_selectedItem != null))
				{
					return null;
				}
				return m_selectedItem.gameObject;
			}
			set
			{
				if (m_selectedItem != null && m_selectedItem.gameObject == value)
				{
					return;
				}
				if (m_selectedItem != null)
				{
					m_selectedItem.Deselect();
				}
				if (value != null)
				{
					m_selectedItem = value.GetComponent<ElementNavigate>();
					if ((bool)m_selectedItem)
					{
						m_selectedItem.Select();
					}
				}
				else
				{
					m_selectedItem = null;
				}
				m_currActiveNavElement = m_selectedItem;
				this.OnChangeSelected.SafeCall();
			}
		}

		public GameObject hoveredItem
		{
			get
			{
				if (!(m_hoveredItem != null))
				{
					return null;
				}
				return m_hoveredItem.gameObject;
			}
			set
			{
				if (m_hoveredItem != null)
				{
					m_hoveredItem.Deselect(m_hoveredItem.interactiveNavigate);
				}
				if (value != null)
				{
					m_hoveredItem = value.GetComponent<ElementNavigate>();
					if (m_hoveredItem != null)
					{
						m_hoveredItem.Select(m_hoveredItem.interactiveNavigate);
					}
				}
				else
				{
					m_hoveredItem = null;
				}
				this.OnChangeHovered.SafeCall();
			}
		}

		public Bounds boundsContext { get; private set; }

		public event Action OnForceUpdateProperties;

		public event Action OnChangeSelected;

		public event Action OnChangeHovered;

		public BaseProperty FindProperty(string propertyName, int deep)
		{
			if (m_bindingProps == null)
			{
				m_bindingProps = new BindingProps<BaseBindingBehaviourTarget>(this);
			}
			return m_bindingProps.FindProperty(propertyName, deep);
		}

		protected void ForceUpdateProperties()
		{
			this.OnForceUpdateProperties.Call();
		}

		public virtual bool IsAllowNavigation()
		{
			return navElements.Count > 0;
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			m_gameManager.isUnityUIActive = true;
			boundsContext = CalculateBounds(base.transform as RectTransform, 1f, calcChilds: true);
			StartCoroutine(SetDefaultElement());
			raycaster = GetComponent<GraphicRaycaster>();
		}

		private IEnumerator SetDefaultElement()
		{
			yield return null;
			if (m_currActiveNavElement == null && navElements.Count > 0)
			{
				ElementNavigate elementNavigate = navElements[0];
				SetCurrent(elementNavigate);
				elementNavigate.Select();
			}
		}

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
			m_gameManager.isUnityUIActive = false;
		}

		public void SetCurrent(ElementNavigate element)
		{
			m_currActiveNavElement = element;
			lastHoveredNavElement = element;
		}

		public void RegisterSelectableControl(ElementNavigate element)
		{
			if (!navElements.Contains(element))
			{
				navElements.Add(element);
			}
		}

		public void UnregisterSelectableControl(ElementNavigate element)
		{
			navElements.Remove(element);
			SetCurrent(null);
		}

		public void ClearSelectableControls()
		{
			navElements.Clear();
		}

		public void ClearHover()
		{
			HoverElement(null);
		}

		public virtual void SelectionMove(NavigateDirection direction)
		{
			if (m_currActiveNavElement != null)
			{
				ElementNavigate currActiveNavElement = m_currActiveNavElement;
				SetCurrent(m_currActiveNavElement.Move(direction));
				if (m_currActiveNavElement == currActiveNavElement && m_currActiveNavElement != null)
				{
					OnFailedMoveSelection(direction);
				}
				return;
			}
			foreach (ElementNavigate navElement in navElements)
			{
				if (navElement.interactiveNavigate)
				{
					navElement.Select();
					SetCurrent(navElement);
					break;
				}
			}
		}

		protected virtual void OnFailedMoveSelection(NavigateDirection direction)
		{
		}

		public void HoverElement(ElementNavigate element)
		{
			if (lastHoveredNavElement == element)
			{
				return;
			}
			if (m_currActiveNavElement == element)
			{
				lastHoveredNavElement = element;
				return;
			}
			if (lastHoveredNavElement != null)
			{
				lastHoveredNavElement.Deselect((element != null) ? element.interactiveNavigate : lastHoveredNavElement.interactiveNavigate);
			}
			lastHoveredNavElement = element;
			if (!(lastHoveredNavElement == null))
			{
				lastHoveredNavElement.Select(lastHoveredNavElement.interactiveNavigate);
				if (element.interactiveNavigate)
				{
					m_currActiveNavElement = lastHoveredNavElement;
				}
			}
		}

		public void CheckRaycast(List<RaycastResult> hits)
		{
			bool flag = false;
			foreach (RaycastResult hit in hits)
			{
				if (lastHoveredNavElement != null && hit.gameObject.GetInstanceID() == lastHoveredNavElement.gameObject.GetInstanceID())
				{
					flag = true;
				}
				if (flag)
				{
					HoverElement(lastHoveredNavElement);
					continue;
				}
				foreach (ElementNavigate navElement in navElements)
				{
					if (navElement.gameObject.GetInstanceID() == hit.gameObject.GetInstanceID())
					{
						HoverElement(navElement);
						break;
					}
				}
			}
			if (!flag && lastHoveredNavElement != null && m_currActiveNavElement != lastHoveredNavElement && lastHoveredNavElement.interactiveNavigate)
			{
				lastHoveredNavElement.Deselect(lastHoveredNavElement.interactiveNavigate);
				lastHoveredNavElement = null;
			}
		}

		public void SubmitCurrentSelected()
		{
			if (m_currActiveNavElement != null)
			{
				m_currActiveNavElement.Submit();
			}
		}

		private Bounds CalculateBounds(RectTransform transformObject, float uiScaleFactor = 1f, bool calcChilds = false)
		{
			if (transformObject == null)
			{
				return default(Bounds);
			}
			Bounds result = new Bounds(transformObject.position, new Vector3(transformObject.rect.width, transformObject.rect.height, 0f) * uiScaleFactor);
			if (calcChilds && transformObject.childCount > 0)
			{
				foreach (RectTransform item in transformObject)
				{
					Bounds bounds = new Bounds(item.position, new Vector3(item.rect.width, item.rect.height, 0f) * uiScaleFactor);
					result.Encapsulate(bounds);
				}
			}
			return result;
		}
	}
}
