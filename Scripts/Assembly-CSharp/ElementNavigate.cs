using Binding.Base;
using UnityEngine;
using UnityEngine.Events;

public class ElementNavigate : MonoBehaviour
{
	[SerializeField]
	private bool m_interactiveNavigate;

	[SerializeField]
	private bool m_clickable;

	public ElementNavigate left;

	public ElementNavigate up;

	public ElementNavigate right;

	public ElementNavigate down;

	private ElementNavigate[] m_directions = new ElementNavigate[5];

	public UnityEvent onSelect = new UnityEvent();

	public UnityEvent onDeselect = new UnityEvent();

	public UnityEvent onMove = new UnityEvent();

	public UnityEvent onSubmit = new UnityEvent();

	private Animator m_animator;

	private NavigateSound m_sounds;

	public bool interactiveNavigate => m_interactiveNavigate;

	public bool clickable => m_clickable;

	public bool selected { get; private set; }

	public BaseBindingBehaviourTarget target { get; private set; }

	private void Awake()
	{
		selected = false;
		m_animator = GetComponent<Animator>();
		m_sounds = GetComponent<NavigateSound>();
		InitDirections();
	}

	public void InitDirections()
	{
		target = GetParentContext();
		m_directions[0] = left;
		m_directions[1] = up;
		m_directions[2] = right;
		m_directions[3] = down;
	}

	private void OnEnable()
	{
		if (target != null)
		{
			target.RegisterSelectableControl(this);
		}
		if (selected)
		{
			Deselect();
			Select();
		}
	}

	private void OnDisable()
	{
		if (target != null)
		{
			target.UnregisterSelectableControl(this);
		}
	}

	private void OnDestroy()
	{
		if (target != null)
		{
			target.UnregisterSelectableControl(this);
		}
	}

	private BaseBindingBehaviourTarget GetParentContext()
	{
		Transform parent = base.transform.parent;
		while (parent != null)
		{
			BaseBindingBehaviourTarget component = parent.GetComponent<BaseBindingBehaviourTarget>();
			if (component != null)
			{
				return component;
			}
			parent = parent.parent;
		}
		return null;
	}

	public void Select(bool registerInTarget = true)
	{
		if (selected)
		{
			return;
		}
		selected = true;
		if (target != null)
		{
			if (registerInTarget)
			{
				target.selectedItem = base.gameObject;
			}
			target.hoveredItem = base.gameObject;
		}
		if (m_animator != null && selected)
		{
			m_animator.SetTrigger("Hover");
		}
		if (interactiveNavigate && m_sounds != null)
		{
			m_sounds.PlayHover();
		}
		onSelect.Invoke();
	}

	public void Deselect(bool registerInTarget = true)
	{
		if (selected)
		{
			selected = false;
			if (registerInTarget)
			{
				target.selectedItem = null;
			}
			target.hoveredItem = null;
			if (m_animator != null)
			{
				m_animator.SetTrigger("Normal");
			}
			onDeselect.Invoke();
		}
	}

	public void Submit()
	{
		if (m_animator != null)
		{
			m_animator.SetTrigger("Select");
		}
		onSubmit.Invoke();
	}

	public ElementNavigate Move(NavigateDirection direction)
	{
		if (direction == NavigateDirection.None)
		{
			return this;
		}
		ElementNavigate elementNavigate = m_directions[(int)direction];
		if (elementNavigate != null)
		{
			if (!elementNavigate.gameObject.activeInHierarchy)
			{
				return this;
			}
			Deselect();
			elementNavigate.Select();
			onMove.Invoke();
			return elementNavigate;
		}
		return this;
	}
}
