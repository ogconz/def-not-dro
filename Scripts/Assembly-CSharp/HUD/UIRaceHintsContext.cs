using System;
using System.Collections.Generic;
using Binding.Base;
using Binding.Components;
using GameInput;
using UnityEngine;

namespace HUD
{
	public class UIRaceHintsContext : BaseBindingBehaviourTarget
	{
		private UIRaceHintsContext m_prevActiveInstance;

		[SerializeField]
		private ActionElementBinding m_prefabHint;

		[SerializeField]
		private Transform m_hintsContainer;

		private ActionElementBindingPool m_aebPool = new ActionElementBindingPool();

		public static UIRaceHintsContext activeInstance { get; private set; }

		public static void ShowHints(ContextOwner owner, ICollection<ActionHintData> actions)
		{
			UIRaceHintsContext uIRaceHintsContext = null;
			uIRaceHintsContext = (owner.IsActive<UIRaceHintsContext>() ? owner.Get<UIRaceHintsContext>() : owner.Add<UIRaceHintsContext>());
			uIRaceHintsContext.Show(actions);
		}

		private void Awake()
		{
			if (m_hintsContainer == null)
			{
				m_hintsContainer = base.transform;
			}
			if (m_hintsContainer.childCount > 0)
			{
				base.transform.DestroyChildren();
			}
		}

		private void OnEnable()
		{
			if (activeInstance != null)
			{
				m_prevActiveInstance = activeInstance;
				Debug.LogWarning("UIRaceHitContext multiply active");
			}
			activeInstance = this;
		}

		private void OnDisable()
		{
			if (activeInstance == this)
			{
				if (m_prevActiveInstance != null && m_prevActiveInstance.isActiveAndEnabled)
				{
					activeInstance = m_prevActiveInstance;
				}
				else
				{
					activeInstance = null;
				}
			}
			m_prevActiveInstance = null;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			BringForwardByDepth();
		}

		protected override void OnDeactivate()
		{
			base.OnDeactivate();
		}

		public void Show(ICollection<ActionHintData> actions)
		{
			Clear();
			foreach (ActionHintData action in actions)
			{
				AddElement(action.hint, action.type, action.action);
			}
		}

		private ActionElementBinding InstantiateHint(string hint, ActionType actionType, System.Action action)
		{
			if (m_prefabHint == null)
			{
				return null;
			}
			ActionElementBinding actionElementBinding = m_aebPool.AllocateNext(m_prefabHint.gameObject, m_hintsContainer);
			if (actionElementBinding != null)
			{
				actionElementBinding.SetData(hint, actionType, action);
			}
			return actionElementBinding;
		}

		public void Clear()
		{
			m_aebPool.Clear();
		}

		public void RemoveElement(ActionElementBinding hint)
		{
			m_aebPool.ReleaseElement(hint);
		}

		public ActionElementBinding AddElement(string hintText, ActionType typeAction, System.Action action)
		{
			return InstantiateHint(hintText, typeAction, action);
		}

		public bool HaveElement(ActionType typeAction)
		{
			return m_aebPool.GetElement(typeAction) != null;
		}
	}
}
