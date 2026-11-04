using System;
using ArrayExtension;
using HUD;

namespace Chat.Overlay
{
	public class GameContextActivityValidator
	{
		private class StateFilter
		{
			public Type targetState;

			public Type[] requiredContext;

			public Type[] contextBlackList;

			public StateFilter(Type targetState)
			{
				this.targetState = targetState;
			}
		}

		private StateFilter[] m_filters = new StateFilter[4]
		{
			new StateFilter(typeof(GarageGUIState))
			{
				requiredContext = new Type[1] { typeof(UIMainMenuContext) },
				contextBlackList = new Type[2]
				{
					typeof(UISettingsButtonsPanelContext),
					typeof(UIPatchNotesContext)
				}
			},
			new StateFilter(typeof(FreerideRaceModeState))
			{
				requiredContext = new Type[1] { typeof(UIRaceFreerideModeContext) }
			},
			new StateFilter(typeof(DynoRaceModeState))
			{
				requiredContext = new Type[1] { typeof(UIRaceFreerideModeContext) }
			},
			new StateFilter(typeof(SyncNetFreerideRaceModeState))
			{
				requiredContext = new Type[1] { typeof(UIRaceNetworkFreerideModeContext) }
			}
		};

		private Type[] m_globalContextBlackList = new Type[7]
		{
			typeof(UIBaseDialogContext),
			typeof(UIRaceStatsContext),
			typeof(UIFinishContext),
			typeof(UIPostCombatContext),
			typeof(UIPostCombatNetworkContext),
			typeof(UIReportContext),
			typeof(UIXDSInfoContext)
		};

		private GameContextObserver m_contextObserver;

		public void Setup(GameContextObserver contextObserver)
		{
			m_contextObserver = contextObserver;
		}

		public void Release()
		{
			m_contextObserver = null;
		}

		public bool GetActivityState()
		{
			BaseState currentState = m_contextObserver.GetCurrentState();
			ContextOwner currentContextOwner = m_contextObserver.GetCurrentContextOwner();
			if (currentState == null || currentContextOwner == null)
			{
				return false;
			}
			if (TryGetFilter(currentState.GetType(), out var result))
			{
				return ValidateContextOwner(result, currentContextOwner);
			}
			return false;
		}

		private bool ValidateContextOwner(StateFilter filter, ContextOwner contextOwner)
		{
			bool result = false;
			foreach (BaseContext internalOwned in contextOwner.GetInternalOwnedList())
			{
				if (internalOwned.isActive)
				{
					if (IsContextBlocked(filter, internalOwned))
					{
						result = false;
						break;
					}
					if (IsContextRequired(filter, internalOwned))
					{
						result = true;
					}
				}
			}
			return result;
		}

		private bool IsContextRequired(StateFilter filter, BaseContext context)
		{
			return filter.requiredContext.Contains(context.GetType());
		}

		private bool IsContextBlocked(StateFilter filter, BaseContext context)
		{
			if (m_globalContextBlackList.Contains(context.GetType()))
			{
				return true;
			}
			if (filter.contextBlackList.Contains(context.GetType()))
			{
				return true;
			}
			if (IsMainMenuBlocked(context))
			{
				return true;
			}
			if (IsNetworkFreerideBlocked())
			{
				return true;
			}
			return false;
		}

		private bool TryGetFilter(Type state, out StateFilter result)
		{
			StateFilter[] filters = m_filters;
			foreach (StateFilter stateFilter in filters)
			{
				if (stateFilter.targetState == state)
				{
					result = stateFilter;
					return true;
				}
			}
			result = null;
			return false;
		}

		private bool IsMainMenuBlocked(BaseContext context)
		{
			if (!(context is UIMainMenuContext uIMainMenuContext))
			{
				return false;
			}
			int value = uIMainMenuContext.indexCategory.value;
			if ((uint)(value - 3) <= 1u)
			{
				return true;
			}
			return false;
		}

		private bool IsNetworkFreerideBlocked()
		{
			if (UIXDSInfoContext.instance != null)
			{
				return UIXDSInfoContext.instance.activeXDS;
			}
			return false;
		}
	}
}
