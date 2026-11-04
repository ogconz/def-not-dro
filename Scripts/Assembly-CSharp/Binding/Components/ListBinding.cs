using System;
using System.Collections.Generic;
using Binding.Base;
using Binding.Elements;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Binding.Components
{
	public class ListBinding : BaseBinding<ListProperty>
	{
		public enum AutoScrollPivotType
		{
			LeftOrTop = 0,
			RightOrBottom = 1,
			Center = 2
		}

		[SerializeField]
		private BaseListElement m_elementPrefab;

		[SerializeField]
		private AutoScrollPivotType m_autoScrollPivot;

		[SerializeField]
		private float m_dragTolerance = 5f;

		[SerializeField]
		private float m_scrollSpeedByDrag = 100f;

		[SerializeField]
		private bool m_useCustomScroll;

		[SerializeField]
		private bool m_useTween;

		[SerializeField]
		private bool m_usePool;

		[SerializeField]
		private int m_poolCount;

		[Range(0f, 5f)]
		[SerializeField]
		private int m_safeScrollOffset;

		private List<BaseListElement> m_elements = new List<BaseListElement>();

		private ScrollRect m_scrollRect;

		private RectTransform m_content;

		private RectTransform m_viewport;

		private BaseDraggableListElement m_draggableElement;

		private RectTransform m_draggableElementBlank;

		private Vector2 m_contentStartPosition;

		private int m_draggableElementIndex;

		private bool m_canDragElements;

		private bool m_scrollVertical;

		private bool m_scrollHorizontal;

		public event Action OnElementsCountChanged;

		protected void Awake()
		{
			m_scrollRect = GetComponent<ScrollRect>();
			m_draggableElement = null;
			m_draggableElementBlank = null;
			m_draggableElementIndex = -1;
			m_canDragElements = m_elementPrefab is BaseDraggableListElement;
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
			if (m_usePool)
			{
				for (int i = 0; i < m_poolCount; i++)
				{
					UnityEngine.Object.Instantiate(m_elementPrefab, m_content, worldPositionStays: false).gameObject.SetActive(value: false);
				}
			}
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

		private void Update()
		{
			if (m_draggableElement != null)
			{
				DragElementToMousePosition();
				MoveBlankElementIfNeeded();
				MoveContentByDragIfNeeded();
			}
		}

		protected override void OnUpdateValue()
		{
			RebuildList();
		}

		private BaseListElement InstantiateElement(BaseListElementData data, int index = -1)
		{
			BaseListElement baseListElement = null;
			if (m_usePool)
			{
				foreach (Transform item in m_content)
				{
					if (!item.gameObject.activeSelf)
					{
						item.gameObject.SetActive(value: true);
						baseListElement = item.GetComponent<BaseListElement>();
						break;
					}
				}
			}
			if (baseListElement == null)
			{
				baseListElement = UnityEngine.Object.Instantiate(m_elementPrefab, m_content, worldPositionStays: false);
			}
			if (index != -1)
			{
				baseListElement.transform.SetSiblingIndex(index);
			}
			baseListElement.Init(data);
			BaseDraggableListElement baseDraggableListElement = baseListElement as BaseDraggableListElement;
			if (baseDraggableListElement != null)
			{
				baseDraggableListElement.OnPressed += ElementPressHandler;
				baseDraggableListElement.OnCanStartDragging += ElementCanStartDragHandler;
				baseDraggableListElement.OnCanEndDragging += ElementCanEndDragHandler;
			}
			return baseListElement;
		}

		private void DestroyElement(BaseListElement element)
		{
			BaseDraggableListElement baseDraggableListElement = element as BaseDraggableListElement;
			if (baseDraggableListElement != null)
			{
				baseDraggableListElement.OnPressed -= ElementPressHandler;
				baseDraggableListElement.OnCanStartDragging -= ElementCanStartDragHandler;
				baseDraggableListElement.OnCanEndDragging -= ElementCanEndDragHandler;
			}
			if (m_usePool)
			{
				element.Init(null);
				element.gameObject.SetActive(value: false);
			}
			else
			{
				UnityEngine.Object.Destroy(element.gameObject);
			}
		}

		private void CreateDraggableElementBlank(BaseListElement element)
		{
			RectTransform rectTransform = element.transform as RectTransform;
			GameObject gameObject = new GameObject("DraggableElementBlank");
			m_draggableElementBlank = gameObject.AddComponent<RectTransform>();
			m_draggableElementBlank.SetParent(m_content);
			m_draggableElementBlank.SetSiblingIndex(m_draggableElementIndex);
			m_draggableElementBlank.anchorMax = rectTransform.anchorMax;
			m_draggableElementBlank.anchorMin = rectTransform.anchorMin;
			m_draggableElementBlank.pivot = rectTransform.pivot;
			m_draggableElementBlank.sizeDelta = rectTransform.sizeDelta;
		}

		private void DragElementToMousePosition()
		{
			if (m_scrollVertical)
			{
				m_draggableElement.transform.position = new Vector3(m_draggableElement.transform.position.x, Input.mousePosition.y);
			}
			else if (m_scrollHorizontal)
			{
				m_draggableElement.transform.position = new Vector3(Input.mousePosition.x, m_draggableElement.transform.position.y);
			}
		}

		private void MoveBlankElementIfNeeded()
		{
			int siblingIndex = m_draggableElementBlank.GetSiblingIndex();
			Vector3 position = m_draggableElement.transform.position;
			for (int i = 0; i < m_content.childCount; i++)
			{
				if (i == siblingIndex)
				{
					continue;
				}
				Vector3 position2 = m_content.GetChild(i).position;
				Vector3 nextPosition = ((i < m_content.childCount - 1) ? m_content.GetChild(i + 1).position : GetExtremePosition(vertical: true, m_content, 0f));
				if (i == 0)
				{
					Vector3 extremePosition = GetExtremePosition(vertical: true, m_content, 1f);
					if (ElementPositionInRange(m_scrollVertical, extremePosition, position2, position))
					{
						m_draggableElementBlank.SetSiblingIndex(0);
						break;
					}
				}
				if (ElementPositionInRange(m_scrollVertical, position2, nextPosition, position))
				{
					int siblingIndex2 = ((siblingIndex > i) ? (i + 1) : i);
					m_draggableElementBlank.SetSiblingIndex(siblingIndex2);
					break;
				}
			}
		}

		private void MoveContentByDragIfNeeded()
		{
			Vector3 position = m_draggableElement.transform.position;
			if (m_scrollVertical)
			{
				Vector3 extremePosition = GetExtremePosition(vertical: true, m_viewport, 1f);
				Vector3 extremePosition2 = GetExtremePosition(vertical: true, m_viewport, 0f);
				if (position.y > extremePosition.y && m_content.anchoredPosition.y > 0f)
				{
					m_content.position = new Vector3(m_content.position.x, m_content.position.y - m_scrollSpeedByDrag * Time.deltaTime);
				}
				if (position.y < extremePosition2.y && m_content.anchoredPosition.y < m_content.rect.height - m_viewport.rect.height)
				{
					m_content.position = new Vector3(m_content.position.x, m_content.position.y + m_scrollSpeedByDrag * Time.deltaTime);
				}
			}
			else
			{
				_ = m_scrollHorizontal;
			}
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

		private void ScrollToElement(int index)
		{
			if (m_scrollRect == null)
			{
				return;
			}
			if (m_useCustomScroll)
			{
				CustomScrollToElement(index);
			}
			else
			{
				float num = (m_scrollRect.vertical ? m_content.rect.height : m_content.rect.width);
				float num2 = (m_scrollRect.vertical ? m_viewport.rect.height : m_viewport.rect.width);
				if (num > num2 && index > 0)
				{
					RectTransform rectTransform = m_elements[index].transform as RectTransform;
					Vector2 vector = m_viewport.InverseTransformPoint(GetAutoScrollPosition(rectTransform));
					Vector2 vector2 = m_viewport.InverseTransformPoint(m_content.position);
					m_content.anchoredPosition = vector2 - vector;
				}
				else
				{
					m_content.anchoredPosition = Vector2.zero;
				}
			}
			m_scrollRect.velocity = Vector2.zero;
		}

		private void CustomScrollToElement(int index)
		{
			RectTransform rectTransform = m_elements[index].transform as RectTransform;
			if (m_scrollHorizontal)
			{
				float num = m_content.rect.width - m_viewport.rect.width;
				float x = rectTransform.anchoredPosition.x;
				float num2 = m_scrollRect.horizontalNormalizedPosition * num;
				float num3 = num2 - m_viewport.rect.width;
				float num4 = num2 + rectTransform.rect.width;
				float offsetForScroll = GetOffsetForScroll(index);
				float? num5 = null;
				if (x + offsetForScroll > num3)
				{
					num5 = num3 - offsetForScroll;
				}
				else if (x - offsetForScroll < num4)
				{
					num5 = num4 + offsetForScroll;
				}
				if (num5.HasValue)
				{
					float num6 = x - num5.Value;
					float num7 = num2 + num6;
					float num8 = ((num > 0f) ? (num7 / num) : 0f);
					if (m_useTween)
					{
						m_scrollRect.DOHorizontalNormalizedPos(num8, 0.2f);
					}
					else
					{
						m_scrollRect.horizontalNormalizedPosition = num8;
					}
				}
				return;
			}
			Vector3 vector = m_viewport.localPosition - rectTransform.localPosition;
			float num9 = m_content.rect.height - m_viewport.rect.height;
			float num10 = m_content.rect.height - vector.y;
			float num11 = m_scrollRect.verticalNormalizedPosition * num9;
			float num12 = num11 + m_viewport.rect.height;
			float num13 = num11 + rectTransform.rect.height;
			float offsetForScroll2 = GetOffsetForScroll(index);
			float? num14 = null;
			if (num10 + offsetForScroll2 >= num12)
			{
				num14 = num12 - offsetForScroll2;
			}
			else if (num10 - offsetForScroll2 <= num13)
			{
				num14 = num13 + offsetForScroll2;
			}
			if (num14.HasValue)
			{
				float num15 = num10 - num14.Value;
				float num16 = num11 + num15;
				float num17 = ((num9 > 0f) ? (num16 / num9) : 0f);
				if (m_useTween)
				{
					m_scrollRect.DOVerticalNormalizedPos(num17, 0.2f);
				}
				else
				{
					m_scrollRect.verticalNormalizedPosition = num17;
				}
			}
		}

		private void ScrollToPositionHandler(float position)
		{
			if (!(m_scrollRect == null) && m_elements.Count != 0)
			{
				int num = m_elements.Count - 1;
				int index = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(position) * (float)num), 0, Mathf.Max(0, num));
				RectTransform trans = (RectTransform)m_elements[index].transform;
				float num2 = (m_scrollHorizontal ? m_content.GetWidth() : m_content.GetHeight());
				float num3 = (m_scrollHorizontal ? m_viewport.GetWidth() : m_viewport.GetHeight());
				float num4 = (m_scrollHorizontal ? trans.GetWidth() : trans.GetHeight());
				float num5 = (m_scrollHorizontal ? m_scrollRect.horizontalNormalizedPosition : m_scrollRect.verticalNormalizedPosition);
				float num6 = Mathf.Abs(num2 - num3);
				float num7 = position * num2;
				float num8 = num5 * num6;
				float num9 = num8;
				float num10 = num8 + Mathf.Min(num3, num2);
				float value = num4 + GetOffsetForScroll(index);
				value = Mathf.Clamp(value, 0f, num6 * 0.5f);
				float num11 = Mathf.Clamp(num7 - value, 0f, num2);
				float num12 = Mathf.Clamp(num7 + value, 0f, num2);
				if (num11 < num9)
				{
					float num13 = num9 - num11;
					num8 -= num13;
				}
				else if (num12 > num10)
				{
					float num14 = num12 - num10;
					num8 += num14;
				}
				float num15 = num8 / num6;
				Vector2 vector = new Vector2(m_scrollHorizontal ? num15 : 0f, m_scrollVertical ? num15 : 0f);
				if (m_useTween)
				{
					m_scrollRect.DONormalizedPos(vector, 0.2f);
				}
				else
				{
					m_scrollRect.normalizedPosition = vector;
				}
				m_scrollRect.velocity = Vector2.zero;
			}
		}

		private float GetOffsetForScroll(int index)
		{
			int value = Mathf.Min(index, m_elements.Count - 1 - index);
			value = Mathf.Clamp(value, 0, m_safeScrollOffset);
			RectTransform rectTransform = (RectTransform)m_elements[index].transform;
			return (m_scrollHorizontal ? rectTransform.rect.width : rectTransform.rect.height) * (float)value;
		}

		private void GetPivotAndOffset(bool vertical, out float offset, out float pivot)
		{
			offset = 0f;
			pivot = 1f;
			switch (m_autoScrollPivot)
			{
			case AutoScrollPivotType.RightOrBottom:
				offset += (vertical ? m_viewport.rect.height : m_viewport.rect.width);
				pivot = 0f;
				break;
			case AutoScrollPivotType.Center:
				offset += (vertical ? (m_viewport.rect.height / 2f) : (m_viewport.rect.width / 2f));
				pivot = 0.5f;
				break;
			}
		}

		private Vector3 GetAutoScrollPosition(RectTransform rectTransform)
		{
			GetPivotAndOffset(m_scrollVertical, out var offset, out var pivot);
			return GetExtremePosition(m_scrollVertical, rectTransform, pivot, offset);
		}

		private Vector3 GetExtremePosition(bool vertical, RectTransform rectTransform, float pivot, float offset = 0f)
		{
			Vector3 zero = Vector3.zero;
			if (vertical)
			{
				zero.x = rectTransform.position.x;
				zero.y = rectTransform.position.y + (pivot - rectTransform.pivot.y) * rectTransform.rect.height + offset;
			}
			else
			{
				zero.x = rectTransform.position.x - (pivot - rectTransform.pivot.x) * rectTransform.rect.width + offset;
				zero.y = rectTransform.position.y;
			}
			return zero;
		}

		private void RebuildList()
		{
			Clear();
			foreach (BaseListElementData item2 in base.property.value)
			{
				BaseListElement item = InstantiateElement(item2);
				m_elements.Add(item);
			}
			this.OnElementsCountChanged.Call();
		}

		private void MoveElement(int index, int position)
		{
			BaseListElement baseListElement = m_elements[index];
			m_elements.RemoveAt(index);
			m_elements.Insert(position, baseListElement);
			baseListElement.transform.SetSiblingIndex(position);
		}

		private void Clear()
		{
			foreach (BaseListElement element in m_elements)
			{
				DestroyElement(element);
			}
			m_elements.Clear();
			this.OnElementsCountChanged.Call();
		}

		private void Subscribe()
		{
			base.property.OnElementAdded += ElementAddHandler;
			base.property.OnElementInserted += ElementInsertHandler;
			base.property.OnElementRemoved += ElementRemoveHandler;
			base.property.OnElementChanged += ElementChangeHandler;
			base.property.OnElementMoved += ElementMoveHandler;
			base.property.OnScrollToElement += ScrollToElementHandler;
			base.property.OnScrollToPosition += ScrollToPositionHandler;
			base.property.OnFullReorder += FullReorderHandler;
			base.property.OnElementDataEdited += UpdateViewHandler;
			base.property.OnSelectionChanged += UpdateViewHandler;
		}

		private void Unsubscribe()
		{
			base.property.OnElementAdded -= ElementAddHandler;
			base.property.OnElementInserted -= ElementInsertHandler;
			base.property.OnElementRemoved -= ElementRemoveHandler;
			base.property.OnElementChanged -= ElementChangeHandler;
			base.property.OnElementMoved -= ElementMoveHandler;
			base.property.OnScrollToElement -= ScrollToElementHandler;
			base.property.OnScrollToPosition -= ScrollToPositionHandler;
			base.property.OnFullReorder -= FullReorderHandler;
			base.property.OnElementDataEdited -= UpdateViewHandler;
			base.property.OnSelectionChanged -= UpdateViewHandler;
		}

		private void ElementAddHandler(BaseListElementData data)
		{
			BaseListElement item = InstantiateElement(data);
			m_elements.Add(item);
			this.OnElementsCountChanged.Call();
		}

		private void ElementInsertHandler(int index, BaseListElementData data)
		{
			BaseListElement item = InstantiateElement(data, index);
			m_elements.Insert(index, item);
			this.OnElementsCountChanged.Call();
		}

		private void ElementRemoveHandler(int index, BaseListElementData data)
		{
			BaseListElement element = m_elements[index];
			m_elements.RemoveAt(index);
			DestroyElement(element);
			this.OnElementsCountChanged.Call();
		}

		private void ElementChangeHandler(int index, BaseListElementData data)
		{
			m_elements[index].Init(data);
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
			m_elements.Sort();
			int num = 0;
			foreach (BaseListElementData item in base.property.value)
			{
				_ = item;
				m_elements[num].transform.SetSiblingIndex(num);
				num++;
			}
		}

		public void ElementPressHandler(BaseDraggableListElement element)
		{
			if (m_canDragElements)
			{
				m_contentStartPosition = m_content.anchoredPosition;
			}
		}

		private void ElementCanStartDragHandler(BaseDraggableListElement element)
		{
			if (m_canDragElements && (m_contentStartPosition - m_content.anchoredPosition).magnitude < m_dragTolerance)
			{
				m_draggableElementIndex = element.transform.GetSiblingIndex();
				m_draggableElement = element;
				element.transform.SetParent(m_viewport);
				CreateDraggableElementBlank(element);
				element.StartDrag();
				SetScrollActive(active: false);
			}
		}

		private void ElementCanEndDragHandler(BaseDraggableListElement element)
		{
			if (m_canDragElements && m_draggableElement != null)
			{
				int siblingIndex = m_draggableElementBlank.GetSiblingIndex();
				UnityEngine.Object.Destroy(m_draggableElementBlank.gameObject);
				m_draggableElementBlank = null;
				m_draggableElement.transform.SetParent(m_content);
				m_draggableElement = null;
				base.property.Move(element.data, siblingIndex);
				element.EndDrag();
				SetScrollActive(active: true);
			}
		}

		private BaseListElement GetListElementByData(BaseListElementData element)
		{
			return m_elements.Find((BaseListElement e) => e.data == element);
		}

		private void UpdateViewHandler(BaseListElementData element)
		{
			BaseListElement listElementByData = GetListElementByData(element);
			if (listElementByData != null)
			{
				listElementByData.OnDataUpdated();
			}
		}
	}
}
