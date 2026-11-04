using Binding;
using Binding.Base;
using GameInput;
using UnityEngine;

public class BaseItemController<T> : BaseBindingBehaviourTarget where T : BaseListItem
{
	public Property<bool> selected = new BoolProperty(false);

	public Property<float> width = new FloatProperty(0f);

	public Property<float> height = new FloatProperty(0f);

	private RectTransform m_rectTransform;

	protected IListView<T> m_controller;

	protected ElementNavigate m_elementNavigate;

	protected T m_item;

	public int index { get; set; }

	public bool viewed { get; set; }

	public RectTransform rectTransform
	{
		get
		{
			if (m_rectTransform == null)
			{
				m_rectTransform = base.transform as RectTransform;
			}
			return m_rectTransform;
		}
	}

	public IListView<T> controller => m_controller;

	public T item => m_item;

	protected virtual void Awake()
	{
		viewed = false;
		selected.OnValueChanged += delegate
		{
			if (selected.value && m_elementNavigate != null)
			{
				m_elementNavigate.Select(registerInTarget: false);
			}
		};
	}

	protected virtual void Start()
	{
		Initialize();
	}

	protected virtual void OnEnable()
	{
	}

	protected virtual void OnDisable()
	{
	}

	protected virtual void Initialize()
	{
		Transform parent = base.transform.parent;
		while (parent != null)
		{
			IListView<T> component = parent.GetComponent<IListView<T>>();
			if (component != null)
			{
				SetController(component);
				break;
			}
			parent = parent.parent;
		}
		m_elementNavigate = GetComponent<ElementNavigate>();
	}

	public virtual void SetController(IListView<T> controller)
	{
		m_controller = controller;
	}

	public virtual void SetItem(T newItem)
	{
		m_item = newItem;
		if (newItem != null && (width.value != newItem.width || height.value != newItem.height))
		{
			ChangeSize(new Vector2(newItem.width, newItem.height));
		}
	}

	public virtual void SetSelected(bool select)
	{
		selected.value = select;
	}

	public void ChangeSize(Vector2 newSize, bool recalcList = true)
	{
		if (m_controller == null || index < 0 || index >= m_controller.GetCount())
		{
			return;
		}
		T val = m_controller.GetItem(index);
		if (val != null)
		{
			width.value = newSize.x;
			height.value = newSize.y;
			rectTransform.sizeDelta = newSize;
			bool num = (val.width != newSize.x || val.height != newSize.y) && recalcList;
			val.width = newSize.x;
			val.height = newSize.y;
			if (num)
			{
				m_controller.RecalcList();
			}
		}
	}

	public virtual void OnClick()
	{
		InputManager.PlayClickSound();
		bool flag = false;
		if (m_controller != null)
		{
			flag = m_controller.OnClick(index);
		}
		m_item?.OnClick();
		if (flag)
		{
			OnDoubleClick();
		}
	}

	public virtual void OnUpMouse()
	{
	}

	public virtual void OnDoubleClick()
	{
		m_controller?.OnDoubleClick(index);
		m_item?.OnDoubleClick();
	}
}
