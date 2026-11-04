using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ListView<T, TS> : MonoBehaviour, IListView<T> where T : BaseListItem where TS : BaseItemController<T>
{
	private const int AdditionalVisibleBuffer = 2;

	private const float ScrollVelocityThreshold = 0.1f;

	public bool buildWithEmptyLines;

	public bool disableInstantScroll;

	public int skipFramesToUpdate;

	protected int m_selectedIndex;

	private ScrollRect m_scrollRect;

	private RectTransform m_scrollContainer;

	public EListViewOrientation orientation;

	public EListViewPlacement placement;

	private RectTransform m_viewport;

	private RectTransform m_contentContainer;

	[SerializeField]
	private TS m_prefabItem;

	protected List<TS> m_pool = new List<TS>();

	public Action onChangeSelectedIndex;

	public Action<int> onClick;

	public Action<int> onDoubleClick;

	public Action<IBaseListItem> onAddItem;

	public Action<IBaseListItem> onRemoveItem;

	public Action onChangeList;

	private float m_viewWidth;

	private float m_viewHeight;

	private float m_totalWidth;

	private float m_totalHeight;

	private int m_countInVertical;

	private int m_countInHorizontal;

	private int m_oldCountVertical;

	private int m_oldCountHorizontal;

	private int m_lastIndex = -99;

	private bool m_poolChanged;

	private Vector2 m_offsetXY = Vector2.zero;

	private Vector2 m_lastOffsetXY;

	private int[] m_screenSize = new int[2];

	private TS m_instancePrefab;

	private Vector2 m_defaultElementSize = Vector2.zero;

	private int m_lastAddedItem;

	private int m_changeFrameSkipper;

	private int m_lastUpdateFrame;

	private float m_timeClick;

	private const float m_doubleClickDT = 0.2f;

	private int m_currentClickIndex;

	public virtual int selectedIndex
	{
		get
		{
			return m_selectedIndex;
		}
		set
		{
			int num = selectedIndex;
			if (value < -1)
			{
				m_selectedIndex = -1;
			}
			else if (value >= items.Count)
			{
				m_selectedIndex = items.Count - 1;
			}
			else
			{
				m_selectedIndex = value;
			}
			if (num != value)
			{
				m_poolChanged = true;
				onChangeSelectedIndex.SafeCall();
			}
			if (m_selectedIndex >= 0)
			{
				ScrollTo(m_selectedIndex);
			}
		}
	}

	public ScrollRect scrollRect => m_scrollRect;

	public Transform contentContainer => m_contentContainer;

	public List<T> items { get; } = new List<T>();

	public T this[int indexer] => items[indexer];

	public TS prefabItem => m_prefabItem;

	public int topIndex { get; private set; }

	protected virtual void Awake()
	{
		m_scrollRect = GetComponent<ScrollRect>();
		if (m_scrollRect != null)
		{
			if (m_scrollRect.verticalScrollbar == null && m_scrollRect.horizontalScrollbar == null)
			{
				m_scrollRect.onValueChanged.AddListener(OnScrollChange);
				return;
			}
			m_scrollRect.verticalScrollbar?.onValueChanged.AddListener(OnScrollVertical);
			m_scrollRect.horizontalScrollbar?.onValueChanged.AddListener(OnScrollHorizontal);
		}
	}

	protected virtual void OnDestroy()
	{
	}

	private void OnEnable()
	{
		SearchViewport();
		m_screenSize[0] = 0;
		m_screenSize[1] = 0;
		m_changeFrameSkipper = 0;
		RecalcList();
	}

	private void Update()
	{
		if (m_screenSize[0] != Screen.width || m_screenSize[1] != Screen.height)
		{
			if (m_changeFrameSkipper > 0)
			{
				m_screenSize[0] = Screen.width;
				m_screenSize[1] = Screen.height;
				m_changeFrameSkipper = 0;
				RecalcList();
				return;
			}
			m_changeFrameSkipper++;
		}
		if (Mathf.Abs(m_scrollRect.velocity.SqrMagnitude()) > 0.1f)
		{
			UpdateScrollPosition();
		}
		if (topIndex != m_lastIndex)
		{
			m_lastIndex = topIndex;
			m_poolChanged = true;
		}
		if (Time.frameCount > m_lastUpdateFrame + skipFramesToUpdate)
		{
			UpdatePool();
			m_lastUpdateFrame = Time.frameCount;
		}
	}

	public void UpdateList()
	{
		m_poolChanged = true;
	}

	public void ForceUpdateList()
	{
		UpdateList();
		UpdatePool();
	}

	private void UpdatePool()
	{
		if (m_poolChanged)
		{
			for (int i = 0; i < m_pool.Count; i++)
			{
				UpdateItem(i);
			}
			m_poolChanged = false;
		}
	}

	private void UpdateItem(int elementIndex)
	{
		int num = topIndex + elementIndex;
		TS val = m_pool[elementIndex];
		if (num >= 0 && num < items.Count)
		{
			T val2 = items[num];
			if (!val.gameObject.activeSelf)
			{
				val.gameObject.SetActive(value: true);
			}
			val.rectTransform.sizeDelta = new Vector2(val2.width, val2.height);
			if (m_poolChanged)
			{
				val.index = num;
				val.SetItem(val2);
			}
			val.SetSelected(num == selectedIndex);
			if (val2.GetType() == typeof(HeaderListSettingsItem))
			{
				val.GetComponent<ElementNavigate>().enabled = false;
			}
		}
		else
		{
			val.SetItem(null);
			val.gameObject.SetActive(buildWithEmptyLines);
		}
		Vector2 offsetPosition = GetOffsetPosition(num, m_defaultElementSize);
		float num2 = ((orientation == EListViewOrientation.Vertical) ? (m_offsetXY.y - offsetPosition.y) : (offsetPosition.x - m_offsetXY.x));
		val.rectTransform.anchoredPosition = ((orientation == EListViewOrientation.Vertical) ? new Vector2(offsetPosition.x, num2) : new Vector2(num2, 0f - offsetPosition.y));
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

	public void Reselect(T item)
	{
		for (int i = 0; i < GetCount(); i++)
		{
			if (this[i] == item)
			{
				selectedIndex = i;
				break;
			}
		}
	}

	public int GetTopIndex()
	{
		return topIndex;
	}

	public int GetCount()
	{
		return items.Count;
	}

	public virtual void Clear()
	{
		ClearSelection();
		items.Clear();
		RebuildPool(0);
	}

	public T GetItem(int index)
	{
		if (index < 0 || index >= items.Count)
		{
			return null;
		}
		return items[index];
	}

	public List<T> FindAll(Predicate<T> match)
	{
		return items.FindAll(match);
	}

	public T GetSelectedItem()
	{
		return GetItem(selectedIndex);
	}

	public int GetSelectedIndex()
	{
		return selectedIndex;
	}

	public int IndexOf(T item)
	{
		return items.IndexOf(item);
	}

	public T Find(Predicate<T> match)
	{
		return items.Find(match);
	}

	public void ForEach(Action<T> action)
	{
		items.ForEach(action);
	}

	public virtual bool OnClick(int index)
	{
		int currentClickIndex = m_currentClickIndex;
		float timeClick = m_timeClick;
		m_timeClick = Time.unscaledTime;
		m_currentClickIndex = index;
		onClick.SafeCall(index);
		selectedIndex = index;
		if (Time.unscaledTime - timeClick <= 0.2f && currentClickIndex == index)
		{
			OnDoubleClick(index);
			return true;
		}
		return false;
	}

	public virtual void OnDoubleClick(int index)
	{
		onDoubleClick.SafeCall(index);
	}

	private bool IsVertical()
	{
		return orientation == EListViewOrientation.Vertical;
	}

	private bool IsLinePlacement()
	{
		return placement == EListViewPlacement.Line;
	}

	public int GetRowsCount()
	{
		if (!IsVertical())
		{
			return m_countInVertical;
		}
		return m_countInHorizontal;
	}

	public int GetCellCount()
	{
		if (!IsVertical())
		{
			return m_countInHorizontal;
		}
		return m_countInVertical;
	}

	public float GetScroll()
	{
		if (!IsVertical())
		{
			return m_scrollRect.horizontalScrollbar.value;
		}
		return m_scrollRect.verticalScrollbar.value;
	}

	public void ScrollTo(int scrollIndex)
	{
		if (disableInstantScroll)
		{
			return;
		}
		if (scrollIndex < 0)
		{
			scrollIndex = 0;
		}
		TS controller = GetController(scrollIndex);
		if (controller != null)
		{
			Rect rect = m_viewport.rect;
			Rect rect2 = RectRelativeTo(controller.rectTransform, m_viewport);
			Vector2 point;
			Vector2 point2;
			if (orientation == EListViewOrientation.Horizontal)
			{
				point = new Vector2(rect2.xMin, rect.y);
				point2 = new Vector2(rect2.xMax, rect.y);
			}
			else
			{
				point = new Vector2(rect.x, rect2.y);
				point2 = new Vector2(rect.x, rect2.yMax);
			}
			if (rect.Contains(point) && rect.Contains(point2))
			{
				return;
			}
		}
		float num = ((scrollIndex > 0) ? ((float)scrollIndex / (float)(items.Count - 1)) : 0f);
		if (IsVertical())
		{
			num = 1f - num;
		}
		if (!m_scrollRect)
		{
			return;
		}
		Vector2 vector = (IsVertical() ? new Vector2(0f, num) : new Vector2(num, 0f));
		m_scrollRect.OnScroll(new PointerEventData(EventSystem.current)
		{
			position = vector
		});
		if (IsVertical() && m_scrollRect.verticalScrollbar != null)
		{
			if (m_scrollRect.verticalScrollbar != null)
			{
				m_scrollRect.verticalScrollbar.value = num;
			}
		}
		else if (m_scrollRect.horizontalScrollbar != null && m_scrollRect.horizontalScrollbar != null)
		{
			m_scrollRect.horizontalScrollbar.value = num;
		}
		m_scrollRect.normalizedPosition = vector;
		m_scrollRect.velocity = Vector2.zero;
		OnScrollChange(vector);
	}

	public void ScrollTo(T item)
	{
		ScrollTo(items.IndexOf(item));
	}

	public virtual void ClearSelection()
	{
		selectedIndex = -1;
	}

	public void ApplyChangeList()
	{
		m_poolChanged = true;
		CalcCount();
	}

	private void CalcCount()
	{
		m_totalHeight = 0f;
		m_totalWidth = 0f;
		m_countInHorizontal = 0;
		m_countInVertical = 0;
		if (items.Count == 0)
		{
			topIndex = 0;
			m_lastIndex = -1;
			CalcViewportCount();
			Clear();
			return;
		}
		CalcViewportCount();
		m_lastAddedItem = 0;
		Vector2 lastOffset = Vector2.zero;
		if (m_lastAddedItem < 0 || m_lastAddedItem >= items.Count)
		{
			m_lastAddedItem = 0;
		}
		for (int i = m_lastAddedItem; i < items.Count; i++)
		{
			items[i].offsetIndex = (IsVertical() ? lastOffset.y : lastOffset.x);
			Vector2 sizeOffset = m_defaultElementSize;
			if (items[i].width > 0.1f || items[i].height > 0.1f)
			{
				sizeOffset = new Vector2(items[i].width, items[i].height);
			}
			else
			{
				items[i].defaultWidth = m_defaultElementSize.x;
				items[i].defaultHeight = m_defaultElementSize.y;
			}
			items[i].width = sizeOffset.x;
			items[i].height = sizeOffset.y;
			CalcTotalOffset(ref lastOffset, sizeOffset, i);
		}
		CalcViewportCount();
		UpdateControllerPool();
	}

	private void CalcTotalOffset(ref Vector2 lastOffset, Vector2 sizeOffset, int index)
	{
		switch (placement)
		{
		case EListViewPlacement.Line:
			if (IsVertical())
			{
				lastOffset.y += sizeOffset.y;
				m_totalHeight = lastOffset.y;
			}
			else
			{
				lastOffset.x += sizeOffset.x;
				m_totalWidth = lastOffset.x;
			}
			break;
		case EListViewPlacement.Grid:
			if (IsVertical())
			{
				if (index != 0 && index % m_countInHorizontal == 0)
				{
					lastOffset.x = 0f;
					lastOffset.y += m_defaultElementSize.y;
					m_totalHeight = lastOffset.y + m_defaultElementSize.y;
				}
				else
				{
					lastOffset.x += sizeOffset.x;
					m_totalWidth = Mathf.Max(m_totalWidth, lastOffset.x);
				}
			}
			else if (index != 0 && index % m_countInVertical == 0)
			{
				lastOffset.y = 0f;
				lastOffset.x += m_defaultElementSize.x;
				m_totalWidth = lastOffset.x + m_defaultElementSize.x;
			}
			else
			{
				lastOffset.y += sizeOffset.y;
				m_totalHeight = Mathf.Max(m_totalHeight, lastOffset.y);
			}
			break;
		}
	}

	private void UpdateControllerPool()
	{
		switch (placement)
		{
		case EListViewPlacement.Line:
			switch (orientation)
			{
			case EListViewOrientation.Vertical:
				if (m_countInVertical != m_oldCountVertical)
				{
					RebuildPool(m_countInVertical);
					return;
				}
				break;
			case EListViewOrientation.Horizontal:
				if (m_countInHorizontal != m_oldCountHorizontal)
				{
					RebuildPool(m_countInHorizontal);
					return;
				}
				break;
			}
			ChangeControllerPool();
			break;
		case EListViewPlacement.Grid:
			if (m_countInVertical != m_oldCountVertical || m_countInHorizontal != m_oldCountHorizontal)
			{
				RebuildPool(m_countInVertical * m_countInHorizontal);
			}
			else
			{
				ChangeControllerPool();
			}
			break;
		}
	}

	private Vector2 GetOffsetPosition(int index, Vector2 defaultSize)
	{
		bool flag = index >= 0 && index < items.Count;
		Vector2 zero = Vector2.zero;
		for (int i = 0; i < index; i++)
		{
			Vector2 vector = (flag ? new Vector2(items[i].width, items[i].height) : defaultSize);
			int num = i + 1;
			switch (placement)
			{
			case EListViewPlacement.Line:
				if (IsVertical())
				{
					zero.y += vector.y;
				}
				else
				{
					zero.x += vector.x;
				}
				break;
			case EListViewPlacement.Grid:
				if (IsVertical())
				{
					zero.x += vector.x;
					if (num % m_countInHorizontal == 0)
					{
						zero.x = 0f;
						zero.y += defaultSize.y;
					}
				}
				else
				{
					zero.y += vector.y;
					if (num % m_countInVertical == 0)
					{
						zero.y = 0f;
						zero.x += defaultSize.x;
					}
				}
				break;
			}
		}
		return zero;
	}

	private void ChangeControllerPool()
	{
		if (m_pool.Count <= 0 || m_pool[0].controller != null)
		{
			return;
		}
		foreach (TS item in m_pool)
		{
			item.SetController(this);
		}
	}

	private void CalcViewportCount()
	{
		m_countInVertical = (int)MathTools.Round(m_viewHeight / Mathf.Max(0.01f, m_defaultElementSize.y), MathTools.RoundMode.RndDown);
		m_countInHorizontal = (int)MathTools.Round(m_viewWidth / Mathf.Max(0.01f, m_defaultElementSize.x), MathTools.RoundMode.RndDown);
		if (IsVertical())
		{
			m_countInVertical += 2;
		}
		else
		{
			m_countInHorizontal += 2;
		}
		Vector2 sizeDelta = Vector2.zero;
		switch (placement)
		{
		case EListViewPlacement.Line:
			switch (orientation)
			{
			case EListViewOrientation.Vertical:
				m_countInHorizontal = 1;
				sizeDelta = new Vector2(m_scrollContainer.sizeDelta.x, m_totalHeight);
				break;
			case EListViewOrientation.Horizontal:
				m_countInVertical = 1;
				sizeDelta = new Vector2(m_totalWidth, m_scrollContainer.sizeDelta.y);
				break;
			}
			break;
		case EListViewPlacement.Grid:
			sizeDelta = new Vector2(m_totalWidth, m_totalHeight);
			if (GetCount() > GetRowsCount())
			{
				if (IsVertical())
				{
					float x = (m_viewWidth - m_totalWidth) / 2f;
					m_contentContainer.anchoredPosition = new Vector2(x, 0f);
				}
				else
				{
					float y = (m_viewHeight - m_totalHeight) / 2f;
					m_contentContainer.anchoredPosition = new Vector2(0f, y);
				}
			}
			break;
		}
		m_scrollContainer.sizeDelta = sizeDelta;
		m_contentContainer.sizeDelta = m_viewport.sizeDelta;
	}

	public void RecalcList()
	{
		if (prefabItem == null)
		{
			GameLog.Print(AlertLevel.Error, "ListView", "Prefab not found for " + base.transform.name);
			return;
		}
		Vector3 size = CalculateBounds(m_viewport).size;
		m_viewHeight = size.y;
		m_viewWidth = size.x;
		m_defaultElementSize = CalculateBounds(m_prefabItem.rectTransform).size;
		if (m_instancePrefab == null)
		{
			m_instancePrefab = InstancePrefab(Vector3.zero, m_defaultElementSize);
		}
		m_totalHeight = 0f;
		m_totalWidth = 0f;
		m_countInHorizontal = 0;
		m_countInVertical = 0;
		if (items.Count == 0)
		{
			topIndex = 0;
			m_lastIndex = -1;
			CalcViewportCount();
			Clear();
		}
		else
		{
			CalcCount();
			ForceUpdateList();
		}
	}

	public TS GetSelectedController()
	{
		T item = GetItem(selectedIndex);
		foreach (TS item2 in m_pool)
		{
			if (item2.item == item)
			{
				return item2;
			}
		}
		return null;
	}

	public TS GetController(int index)
	{
		T item = GetItem(index);
		foreach (TS item2 in m_pool)
		{
			if (item2.item == item)
			{
				return item2;
			}
		}
		return null;
	}

	public virtual void Sort()
	{
		items.Sort();
		CalcCount();
	}

	public virtual void Sort(IComparer<T> comparer)
	{
		items.Sort(comparer);
		CalcCount();
	}

	public virtual void Sort(Comparison<T> comparison)
	{
		items.Sort(comparison);
		CalcCount();
	}

	private void RebuildPool(int count)
	{
		if (buildWithEmptyLines)
		{
			switch (placement)
			{
			case EListViewPlacement.Line:
				count = GetCellCount();
				break;
			case EListViewPlacement.Grid:
				count = m_countInVertical * m_countInHorizontal;
				break;
			}
		}
		if (m_pool.Count > count)
		{
			int num = m_pool.Count - count;
			int num2 = m_pool.Count - 1;
			if (buildWithEmptyLines)
			{
				return;
			}
			for (int i = 0; i < num; i++)
			{
				TS val = m_pool[num2 - i];
				if (val.gameObject.activeSelf)
				{
					val.gameObject.SetActive(value: false);
				}
			}
		}
		else
		{
			int num3 = count - m_pool.Count;
			int count2 = m_pool.Count;
			Vector2 defaultElementSize = m_defaultElementSize;
			for (int j = 0; j < num3; j++)
			{
				Vector3 position = GetOffsetPosition(count2 + j, defaultElementSize);
				InstancePrefab(position, defaultElementSize).gameObject.SetActive(buildWithEmptyLines);
			}
		}
	}

	private TS InstancePrefab(Vector3 position, Vector2 size)
	{
		TS val = CreateObject(prefabItem.gameObject, Vector3.zero, Quaternion.identity, m_contentContainer);
		val.SetController(this);
		val.rectTransform.SetWidth(size.x);
		val.rectTransform.SetHeight(size.y);
		val.transform.SetParent(m_contentContainer, worldPositionStays: false);
		val.transform.localScale = Vector3.one;
		val.rectTransform.anchoredPosition = position;
		m_pool.Add(val);
		return val;
	}

	protected virtual TS CreateObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
	{
		GameObject obj = UnityEngine.Object.Instantiate(prefab, parent);
		obj.transform.name = prefab.name;
		return obj.GetComponent<TS>();
	}

	public virtual void AddItem(T item, bool safeScroll = false)
	{
		int scrollIndex = topIndex;
		items.Add(item);
		onAddItem.SafeCall(item);
		onChangeList.SafeCall();
		if (safeScroll)
		{
			RecalcList();
			ScrollTo(scrollIndex);
		}
	}

	public virtual void InsertItem(int index, T item)
	{
		items.Insert(index, item);
		onAddItem.SafeCall(item);
		onChangeList.SafeCall();
	}

	public virtual void AddRange(List<T> list, bool safeScroll = false)
	{
		int scrollIndex = topIndex;
		foreach (T item in list)
		{
			items.Add(item);
			onAddItem.SafeCall(item);
		}
		onChangeList.SafeCall();
		if (safeScroll)
		{
			RecalcList();
			ScrollTo(scrollIndex);
		}
	}

	public virtual void AddRange(T[] array, bool safeScroll = false)
	{
		int scrollIndex = topIndex;
		foreach (T val in array)
		{
			items.Add(val);
			onAddItem.SafeCall(val);
		}
		onChangeList.SafeCall();
		if (safeScroll)
		{
			RecalcList();
			ScrollTo(scrollIndex);
		}
	}

	public virtual void RemoveItem(T item, bool shouldScroll = true)
	{
		RemoveItem(items.IndexOf(item), shouldScroll);
	}

	public virtual void RemoveItem(int index, bool shouldScroll = true)
	{
		m_lastAddedItem = 0;
		T item = null;
		if (items != null && index >= 0 && index < items.Count)
		{
			if (topIndex >= items.Count)
			{
				topIndex = ((items.Count > 0) ? (items.Count - 1) : 0);
			}
			item = items[topIndex];
			onRemoveItem.SafeCall(items[index]);
			items.RemoveAt(index);
		}
		Bounds bounds = CalculateBounds(prefabItem.transform as RectTransform);
		switch (orientation)
		{
		case EListViewOrientation.Vertical:
			m_totalHeight -= bounds.size.y;
			m_totalHeight = Mathf.Max(m_totalHeight, 0f);
			m_scrollContainer.sizeDelta = new Vector2(m_scrollContainer.sizeDelta.x, m_totalHeight);
			break;
		case EListViewOrientation.Horizontal:
			m_totalWidth -= bounds.size.x;
			m_totalWidth = Mathf.Max(m_totalWidth, 0f);
			m_scrollContainer.sizeDelta = new Vector2(m_totalWidth, m_scrollContainer.sizeDelta.y);
			break;
		}
		m_scrollContainer.sizeDelta = m_contentContainer.sizeDelta;
		onChangeList.SafeCall();
		if (shouldScroll)
		{
			ScrollTo(item);
		}
	}

	public static Matrix4x4 TransformTo(Transform from, Transform to)
	{
		return to.worldToLocalMatrix * from.localToWorldMatrix;
	}

	public static Rect RectRelativeTo(RectTransform transform, Transform to)
	{
		Matrix4x4 matrix4x = TransformTo(transform, to);
		Rect rect = transform.rect;
		Vector3 point = new Vector2(rect.xMin, rect.yMin);
		Vector3 point2 = new Vector2(rect.xMax, rect.yMax);
		point = matrix4x.MultiplyPoint(point);
		point2 = matrix4x.MultiplyPoint(point2);
		rect.xMin = point.x;
		rect.yMin = point.y;
		rect.xMax = point2.x;
		rect.yMax = point2.y;
		return rect;
	}

	private void SearchViewport()
	{
		if (m_viewport == null)
		{
			m_viewport = base.transform.Find("Viewport") as RectTransform;
			if (m_viewport == null)
			{
				GameObject gameObject = new GameObject("Viewport");
				gameObject.AddComponent<Mask>().showMaskGraphic = false;
				gameObject.AddComponent<CanvasRenderer>();
				Image image = gameObject.AddComponent<Image>();
				image.raycastTarget = true;
				image.type = Image.Type.Sliced;
				image.fillCenter = true;
				m_viewport = gameObject.transform as RectTransform;
				m_viewport.SetParent(base.transform);
				RectTransform viewport = m_viewport;
				if (viewport != null)
				{
					RectTransform rectTransform = base.transform as RectTransform;
					viewport.anchoredPosition = Vector2.zero;
					viewport.anchorMin = Vector2.zero;
					viewport.anchorMax = Vector2.one;
					viewport.pivot = new Vector2(0.5f, 0.5f);
					viewport.localScale = Vector3.one;
					viewport.sizeDelta = rectTransform.rect.size;
					viewport.offsetMin = Vector2.zero;
					viewport.offsetMax = Vector2.zero;
				}
			}
		}
		if (m_scrollContainer == null && m_viewport != null)
		{
			m_scrollContainer = m_viewport.Find("Content") as RectTransform;
			if (m_scrollContainer == null)
			{
				GameObject gameObject2 = new GameObject("Content", typeof(RectTransform));
				m_scrollContainer = gameObject2.transform as RectTransform;
				m_scrollContainer.SetParent(m_viewport);
				RectTransform scrollContainer = m_scrollContainer;
				if (scrollContainer != null)
				{
					RectTransform viewport2 = m_viewport;
					scrollContainer.anchoredPosition = Vector2.zero;
					scrollContainer.anchorMin = Vector2.up;
					scrollContainer.anchorMax = Vector2.one;
					scrollContainer.pivot = (IsVertical() ? new Vector2(0.5f, 1f) : new Vector2(1f, 0.5f));
					scrollContainer.localScale = Vector3.one;
					scrollContainer.sizeDelta = viewport2.rect.size;
					scrollContainer.offsetMin = Vector2.zero;
					scrollContainer.offsetMax = Vector2.zero;
				}
			}
		}
		if (!(m_contentContainer == null) || !(m_viewport != null))
		{
			return;
		}
		m_contentContainer = m_viewport.Find("VirtualContent") as RectTransform;
		if (m_contentContainer == null)
		{
			GameObject gameObject3 = new GameObject("VirtualContent", typeof(RectTransform));
			m_contentContainer = gameObject3.transform as RectTransform;
			m_contentContainer.SetParent(m_viewport);
			RectTransform rectTransform2 = m_contentContainer;
			if (rectTransform2 != null)
			{
				RectTransform viewport3 = m_viewport;
				rectTransform2.anchoredPosition = Vector2.zero;
				rectTransform2.anchorMin = Vector2.up;
				rectTransform2.anchorMax = Vector2.one;
				rectTransform2.pivot = (IsVertical() ? new Vector2(0.5f, 1f) : new Vector2(1f, 0.5f));
				rectTransform2.localScale = Vector3.one;
				rectTransform2.sizeDelta = viewport3.rect.size;
				rectTransform2.offsetMin = Vector2.zero;
				rectTransform2.offsetMax = Vector2.zero;
			}
		}
	}

	public void OnScrollVertical(float scroll)
	{
		OnScrollChange(new Vector2(0f, scroll));
	}

	public void OnScrollHorizontal(float scroll)
	{
		OnScrollChange(new Vector2(scroll, 0f));
	}

	public void OnScrollChange(Vector2 newScroll)
	{
		if (m_pool.Count == 0)
		{
			m_lastOffsetXY = Vector2.zero;
			return;
		}
		Vector2 vector = new Vector2(newScroll.x, 1f - newScroll.y);
		float num = 0f;
		if (IsVertical())
		{
			float y = vector.y;
			float num2 = m_totalHeight - m_viewHeight;
			if (num2 < 0f)
			{
				num2 = 0f;
			}
			m_offsetXY = new Vector2(0f, num2 * y);
			num = m_offsetXY.y;
		}
		else
		{
			float y = vector.x;
			float num3 = m_totalWidth - m_viewWidth;
			if (num3 < 0f)
			{
				num3 = 0f;
			}
			m_offsetXY = new Vector2(num3 * y, 0f);
			num = m_offsetXY.x;
		}
		topIndex = (IsLinePlacement() ? SearchTopIndexLine(num) : SearchTopIndexGrid(num));
		Vector2 vector2 = m_offsetXY - m_lastOffsetXY;
		m_lastOffsetXY -= vector2;
		m_poolChanged = true;
	}

	public void UpdateScrollPosition()
	{
		OnScrollChange(scrollRect.normalizedPosition);
	}

	private int SearchTopIndexLine(float newIndex)
	{
		if (items.Count == 0)
		{
			return 0;
		}
		int num = 0;
		int num2 = items.Count;
		int num3 = 0;
		while (num < num2)
		{
			num3 = num + (num2 - num) / 2;
			if (items[num3].offsetIndex >= newIndex)
			{
				num2 = num3;
			}
			else
			{
				num = num3 + 1;
			}
		}
		int num4 = num2 - 1;
		if (num4 >= 0)
		{
			return num4;
		}
		return 0;
	}

	private int SearchTopIndexGrid(float newIndex)
	{
		if (items.Count == 0)
		{
			return 0;
		}
		bool num = IsVertical();
		int rowsCount = GetRowsCount();
		float num2 = (num ? m_totalHeight : m_totalWidth);
		float b = (num ? m_defaultElementSize.y : m_defaultElementSize.x);
		int num3 = (int)MathTools.Round(num2 / Mathf.Max(0.01f, b), MathTools.RoundMode.RndDown);
		int num4 = 0;
		int num5 = num3;
		int num6 = 0;
		while (num4 < num5)
		{
			num6 = num4 + (num5 - num4) / 2;
			if (items[num6 * rowsCount].offsetIndex >= newIndex)
			{
				num5 = num6;
			}
			else
			{
				num4 = num6 + 1;
			}
		}
		int num7 = (num5 - 2) * rowsCount;
		if (num7 >= 0)
		{
			return num7;
		}
		return 0;
	}
}
