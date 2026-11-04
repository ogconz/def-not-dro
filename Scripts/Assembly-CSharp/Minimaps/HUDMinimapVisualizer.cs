using System;
using DI;
using HUD;
using PrefsModels;
using UnityEngine;

namespace Minimaps
{
	public class HUDMinimapVisualizer : RotatableMapVisualizer
	{
		private const float BorderRadius = 105f;

		private GlobalModel m_globals;

		protected override void OnEnable()
		{
			base.OnEnable();
			if (m_globals == null)
			{
				m_globals = DependencyInjector.Resolve<GamePrefs>().globals;
			}
			GlobalModel globals = m_globals;
			globals.onShowMinimapChanged = (Action)Delegate.Combine(globals.onShowMinimapChanged, new Action(SetVisibilityByGlobals));
			SetupVisibility(m_globals.showMinimap);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			GlobalModel globals = m_globals;
			globals.onShowMinimapChanged = (Action)Delegate.Remove(globals.onShowMinimapChanged, new Action(SetVisibilityByGlobals));
		}

		private void SetVisibilityByGlobals()
		{
			SetupVisibility(m_globals.showMinimap);
		}

		protected override void UpdateWaypointItem(UIMinimapItem uiWaypointItem)
		{
			Vector3 vector = m_mapRoot.InverseTransformPoint(uiWaypointItem.transform.position);
			if (vector.magnitude >= 105f)
			{
				vector = vector.normalized * 105f;
				uiWaypointItem.transform.position = m_mapRoot.TransformPoint(vector);
				uiWaypointItem.angle = (Mathf.Atan2(vector.y, vector.x) + MathF.PI / 2f) * 57.29578f;
			}
			else
			{
				uiWaypointItem.angle = m_currentMapAngle;
			}
			uiWaypointItem.gameObject.SetActive(uiWaypointItem.item.transform.position != Vector3.zero);
		}
	}
}
