using System;
using System.Collections.Generic;
using Binding.Base;
using Binding.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class InfinityListBinding : BaseBinding<ListProperty>
	{
		public enum ScrollType
		{
			Vertical = 0,
			Horizontal = 1
		}

		[SerializeField]
		private ScrollType m_scrollType;

		[SerializeField]
		private BaseListElement m_elementPrefab;

		private BaseListElement[] m_links = new BaseListElement[0];

		private List<BaseListElement> m_pool = new List<BaseListElement>();

		private ScrollRect m_scrollRect;

		private RectTransform m_content;

		private RectTransform m_viewport;

		private bool m_scrollVertical;

		private bool m_scrollHorizontal;

		private float m_itemSize;

		private int m_lastScrollIndex;

		public event Action onElementsCountChanged;

		protected void Awake()
		{
			m_scrollRect = GetComponent<ScrollRect>();
			if (m_scrollRect != null)
			{
				m_content = ((m_scrollRect.content != null) ? m_scrollRect.content : (base.transform as RectTransform));
				m_viewport = ((m_scrollRect.viewport != null) ? m_scrollRect.viewport : (base.transform as RectTransform));
				m_scrollVertical = m_scrollRect.vertical;
				m_scrollHorizontal = m_scrollRect.horizontal;
			}
			else
			{
				m_content = base.transform as RectTransform;
				m_viewport = base.transform as RectTransform;
				m_scrollVertical = false;
				m_scrollHorizontal = false;
			}
			if (m_scrollType == ScrollType.Vertical)
			{
				m_itemSize = m_elementPrefab.GetComponent<RectTransform>().GetHeight();
			}
			else
			{
				m_itemSize = m_elementPrefab.GetComponent<RectTransform>().GetWidth();
			}
			m_itemSize = ((m_itemSize > float.Epsilon) ? m_itemSize : 1f);
		}

		protected override void Start()
		{
			base.Start();
			Subscribe();
		}

		private void OnDestroy()
		{
			if (base.property != null)
			{
				Unsubscribe();
			}
		}

		protected override void OnUpdateValue()
		{
			RebuildList();
		}

		private bool ElementPositionInRange(bool vertical, Vector3 prevPosition, Vector3 nextPosition, Vector3 position)
		{
			if (!vertical)
			{
				if (prevPosition.x > position.x)
				{
					return position.x > nextPosition.x;
				}
				return false;
			}
			if (prevPosition.y > position.y)
			{
				return position.y > nextPosition.y;
			}
			return false;
		}

		private void SetScrollActive(bool active)
		{
			if (active)
			{
				m_scrollRect.vertical = m_scrollVertical;
				m_scrollRect.horizontal = m_scrollHorizontal;
			}
			else
			{
				m_scrollRect.vertical = false;
				m_scrollRect.horizontal = false;
			}
		}

		protected void ScrollToElement(int index)
		{
			if (m_scrollRect == null)
			{
				return;
			}
			float num = (m_scrollRect.vertical ? m_content.rect.height : m_content.rect.width);
			float num2 = (m_scrollRect.vertical ? m_viewport.rect.height : m_viewport.rect.width);
			if (num > num2 && index > 0)
			{
				if (m_scrollVertical)
				{
					m_content.anchoredPosition = new Vector2(0f, m_itemSize * (float)Mathf.Min(index, Mathf.Max(0, base.property.count - m_pool.Count + 1)));
				}
				else
				{
					m_content.anchoredPosition = new Vector2(m_itemSize * (float)Mathf.Min(index, Mathf.Max(0, base.property.count - m_pool.Count + 1)), 0f);
				}
			}
			else
			{
				m_content.anchoredPosition = Vector2.zero;
			}
			m_scrollRect.velocity = Vector2.zero;
		}

		private void RebuildList()
		{
			int num = 0;
			num = ((m_scrollType != ScrollType.Vertical) ? Mathf.Min(base.property.count, Mathf.RoundToInt(m_viewport.GetWidth() / m_itemSize) + 1) : Mathf.Min(base.property.count, Mathf.RoundToInt(m_viewport.GetHeight() / m_itemSize) + 1));
			if (m_pool.Count > num)
			{
				int num2 = m_pool.Count - num;
				for (int i = 0; i < num2; i++)
				{
					BaseListElement baseListElement = m_pool[m_pool.Count - i - 1];
					if (baseListElement != null)
					{
						UnityEngine.Object.Destroy(baseListElement.gameObject);
					}
				}
				m_pool.RemoveRange(m_pool.Count - num2, num2);
			}
			else
			{
				int num3 = num - m_pool.Count;
				for (int j = 0; j < num3; j++)
				{
					m_pool.Add(UnityEngine.Object.Instantiate(m_elementPrefab, m_content, worldPositionStays: false));
				}
			}
			if (m_scrollType == ScrollType.Vertical)
			{
				int num4 = (int)((float)base.property.count * m_itemSize);
				m_content.SetHeight(num4);
				Vector2 anchoredPosition = m_content.anchoredPosition;
				m_content.anchoredPosition = new Vector2(anchoredPosition.x, Mathf.Clamp(anchoredPosition.y, -num4, 0f));
			}
			else
			{
				int num5 = (int)((float)base.property.count * m_itemSize);
				m_content.SetWidth(num5);
				Vector2 anchoredPosition2 = m_content.anchoredPosition;
				m_content.anchoredPosition = new Vector2(Mathf.Clamp(anchoredPosition2.x, -num5, 0f), anchoredPosition2.y);
			}
			if (num == 0)
			{
				return;
			}
			m_links = new BaseListElement[base.property.count];
			int currentIndex = GetCurrentIndex();
			for (int k = 0; k < m_pool.Count; k++)
			{
				BaseListElement baseListElement2 = m_pool[k];
				baseListElement2.transform.SetParent(m_content.transform, worldPositionStays: false);
				baseListElement2.transform.localScale = Vector3.one;
				if (m_scrollType == ScrollType.Vertical)
				{
					RectTransform component = baseListElement2.GetComponent<RectTransform>();
					if (component != null)
					{
						component.pivot = new Vector2(component.pivot.x, 1f);
					}
				}
				else if (m_scrollType == ScrollType.Horizontal)
				{
					RectTransform component2 = baseListElement2.GetComponent<RectTransform>();
					if (component2 != null)
					{
						component2.pivot = new Vector2(0f, component2.pivot.y);
					}
				}
				UpdateItem(m_pool[k], currentIndex + k);
				m_links[currentIndex + k] = baseListElement2;
			}
			this.onElementsCountChanged.Call();
		}

		private Vector3 GetLocationAppear(Vector2 localPos, int index)
		{
			Vector3 vector = localPos;
			return (m_scrollType != ScrollType.Vertical) ? new Vector3(m_itemSize * (float)index, vector.y, 0f) : new Vector3(vector.x, (0f - m_itemSize) * (float)index, 0f);
		}

		private void UpdateItem(BaseListElement poolPrefab, int index)
		{
			Vector3 locationAppear = GetLocationAppear(poolPrefab.transform.localPosition, index);
			poolPrefab.transform.localPosition = locationAppear;
			poolPrefab.Init(base.property[index]);
		}

		private int GetCurrentIndex()
		{
			int num = -1;
			num = ((m_scrollType != ScrollType.Vertical) ? ((int)((0f - m_content.anchoredPosition.x) / m_itemSize)) : ((int)(m_content.anchoredPosition.y / m_itemSize)));
			return Mathf.Clamp(num, 0, base.property.count - m_pool.Count);
		}

		private void MoveElement(int index, int position)
		{
			RebuildList();
		}

		private void Subscribe()
		{
			base.property.OnElementAdded += ElementAddHandler;
			base.property.OnElementRemoved += ElementRemoveHandler;
			base.property.OnElementChanged += ElementChangeHandler;
			base.property.OnElementMoved += ElementMoveHandler;
			base.property.OnScrollToElement += ScrollToElementHandler;
			base.property.OnFullReorder += FullReorderHandler;
			m_scrollRect.onValueChanged.AddListener(OnScrollChange);
		}

		private void Unsubscribe()
		{
			base.property.OnElementAdded -= ElementAddHandler;
			base.property.OnElementRemoved -= ElementRemoveHandler;
			base.property.OnElementChanged -= ElementChangeHandler;
			base.property.OnElementMoved -= ElementMoveHandler;
			base.property.OnScrollToElement -= ScrollToElementHandler;
			base.property.OnFullReorder -= FullReorderHandler;
			m_scrollRect.onValueChanged.RemoveListener(OnScrollChange);
		}

		private void ElementAddHandler(BaseListElementData data)
		{
			RebuildList();
			this.onElementsCountChanged.Call();
		}

		private void ElementRemoveHandler(int index, BaseListElementData data)
		{
			RebuildList();
			this.onElementsCountChanged.Call();
		}

		private void ElementChangeHandler(int index, BaseListElementData data)
		{
			int currentIndex = GetCurrentIndex();
			if (index >= currentIndex && index < currentIndex + m_pool.Count)
			{
				UpdateItem(m_pool[index - currentIndex], index);
			}
		}

		private void ElementMoveHandler(int index, int position)
		{
			MoveElement(index, position);
		}

		private void ScrollToElementHandler(int index)
		{
			if (m_scrollRect != null)
			{
				ScrollToElement(index);
			}
		}

		private void FullReorderHandler()
		{
			RebuildList();
		}

		public void OnScrollChange(Vector2 delta)
		{
			if (base.property.count < 1)
			{
				return;
			}
			int currentIndex = GetCurrentIndex();
			if (m_lastScrollIndex == currentIndex)
			{
				return;
			}
			m_lastScrollIndex = currentIndex;
			if (NeedFullUpdate(currentIndex))
			{
				return;
			}
			BaseListElement baseListElement = m_links[currentIndex];
			if (baseListElement == null)
			{
				int num = currentIndex + m_pool.Count;
				if (num <= base.property.count - 1)
				{
					BaseListElement baseListElement2 = m_links[num];
					if (baseListElement2 != null)
					{
						m_links[num] = baseListElement;
						m_links[currentIndex] = baseListElement2;
						UpdateItem(m_links[currentIndex], currentIndex);
					}
				}
			}
			else
			{
				if (currentIndex <= 0)
				{
					return;
				}
				BaseListElement baseListElement3 = m_links[currentIndex - 1];
				if (baseListElement3 == null)
				{
					return;
				}
				int num2 = currentIndex - 1 + m_pool.Count;
				if (num2 <= base.property.count - 1)
				{
					BaseListElement baseListElement4 = m_links[num2];
					if (baseListElement4 == null)
					{
						m_links[num2] = baseListElement3;
						m_links[currentIndex - 1] = baseListElement4;
						UpdateItem(m_links[num2], num2);
					}
				}
			}
		}

		public bool NeedFullUpdate(int index)
		{
			bool flag = false;
			int num = index + 1;
			for (int i = num; i < num + m_pool.Count - 2; i++)
			{
				if (i < base.property.count)
				{
					BaseListElement baseListElement = m_links[i];
					if (baseListElement == null)
					{
						flag = true;
						break;
					}
					if (baseListElement.data != base.property[i])
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				for (int j = 0; j < base.property.count; j++)
				{
					m_links[j] = null;
				}
				int num2 = index;
				if (num2 + m_pool.Count > base.property.count)
				{
					num2 = base.property.count - m_pool.Count;
				}
				for (int k = 0; k < m_pool.Count; k++)
				{
					m_links[num2 + k] = m_pool[k];
					UpdateItem(m_links[num2 + k], num2 + k);
				}
				return true;
			}
			return false;
		}
	}
}
