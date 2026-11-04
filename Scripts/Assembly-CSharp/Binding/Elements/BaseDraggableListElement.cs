using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Binding.Elements
{
	public abstract class BaseDraggableListElement : BaseListElement, IPointerDownHandler, IEventSystemHandler
	{
		private static readonly int StartDragTrigger = Animator.StringToHash("StartDrag");

		private static readonly int EndDragTrigger = Animator.StringToHash("EndDrag");

		[SerializeField]
		private float m_dragInitiationTime = 1f;

		[SerializeField]
		private float m_clickTolerance = 5f;

		protected Animator m_animator;

		private Vector3 m_pointerDownPosition;

		private bool m_isPointerDown;

		private bool m_dragInProgress;

		public event Action<BaseDraggableListElement> OnPressed;

		public event Action<BaseDraggableListElement> OnCanStartDragging;

		public event Action<BaseDraggableListElement> OnCanEndDragging;

		protected virtual void Awake()
		{
			m_animator = GetComponent<Animator>();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			StopAllCoroutines();
		}

		public virtual void StartDrag()
		{
			if (m_animator != null && m_animator.HasParameter(StartDragTrigger))
			{
				m_animator.SetTrigger(StartDragTrigger);
			}
		}

		public virtual void EndDrag()
		{
			if (m_animator != null && m_animator.HasParameter(EndDragTrigger))
			{
				m_animator.SetTrigger(EndDragTrigger);
			}
		}

		private void Update()
		{
			if (Input.GetMouseButtonUp(0))
			{
				if (m_dragInProgress)
				{
					m_dragInProgress = false;
					this.OnCanEndDragging.Call(this);
				}
				else if (m_isPointerDown && (m_pointerDownPosition - base.transform.position).magnitude < m_clickTolerance)
				{
					base.data.Click();
				}
				m_isPointerDown = false;
			}
		}

		private IEnumerator DragInitiationCoroutine()
		{
			float startTime = Time.time;
			while (m_isPointerDown && Time.time < startTime + m_dragInitiationTime)
			{
				yield return null;
			}
			if (m_isPointerDown)
			{
				m_dragInProgress = true;
				this.OnCanStartDragging.Call(this);
			}
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			m_pointerDownPosition = base.transform.position;
			m_isPointerDown = true;
			this.OnPressed.Call(this);
			StartCoroutine(DragInitiationCoroutine());
		}
	}
}
