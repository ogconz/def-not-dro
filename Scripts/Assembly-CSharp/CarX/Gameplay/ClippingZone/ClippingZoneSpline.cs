using System;
using SyncMultiplayer;
using TrackLayout;
using TrackLayout.Behaviours;
using UnityEngine;
using Utils.MeshGen.Spline;

namespace CarX.Gameplay.ClippingZone
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(CompositeTriggerZone))]
	public class ClippingZoneSpline : BaseClipping
	{
		public ClipZoneType zoneType;

		public float pointsFactor;

		private ClippingZoneInfo m_info;

		private void Start()
		{
			OnEnable();
		}

		private void OnEnable()
		{
			m_state = State.Idle;
			CompositeTriggerZone component = GetComponent<CompositeTriggerZone>();
			if (component != null)
			{
				component.onTriggerEnter = (Action<Collider>)Delegate.Remove(component.onTriggerEnter, new Action<Collider>(OnTriggerEnter));
				component.onTriggerEnter = (Action<Collider>)Delegate.Combine(component.onTriggerEnter, new Action<Collider>(OnTriggerEnter));
				component.onTriggerStay = (Action<Collider>)Delegate.Remove(component.onTriggerStay, new Action<Collider>(OnTriggerStay));
				component.onTriggerStay = (Action<Collider>)Delegate.Combine(component.onTriggerStay, new Action<Collider>(OnTriggerStay));
				component.onTriggerExit = (Action<Collider>)Delegate.Remove(component.onTriggerExit, new Action<Collider>(OnTriggerExit));
				component.onTriggerExit = (Action<Collider>)Delegate.Combine(component.onTriggerExit, new Action<Collider>(OnTriggerExit));
			}
			SplineColliders component2 = GetComponent<SplineColliders>();
			if (component2 != null)
			{
				component2.onTriggerEnter = (Action<Collider>)Delegate.Remove(component2.onTriggerEnter, new Action<Collider>(OnTriggerEnter));
				component2.onTriggerEnter = (Action<Collider>)Delegate.Combine(component2.onTriggerEnter, new Action<Collider>(OnTriggerEnter));
				component2.onTriggerStay = (Action<Collider>)Delegate.Remove(component2.onTriggerStay, new Action<Collider>(OnTriggerStay));
				component2.onTriggerStay = (Action<Collider>)Delegate.Combine(component2.onTriggerStay, new Action<Collider>(OnTriggerStay));
				component2.onTriggerExit = (Action<Collider>)Delegate.Remove(component2.onTriggerExit, new Action<Collider>(OnTriggerExit));
				component2.onTriggerExit = (Action<Collider>)Delegate.Combine(component2.onTriggerExit, new Action<Collider>(OnTriggerExit));
			}
		}

		protected override void OnTriggerEnter(Collider other)
		{
			base.OnTriggerEnter(other);
			if (m_driftController == null)
			{
				return;
			}
			m_info = new ClippingZoneInfo
			{
				minSpeed = m_driftController.driftSpeed
			};
			if (other == null || !BaseClipping.IsCar(other))
			{
				return;
			}
			if (zoneType == ClipZoneType.Bad)
			{
				UIXDSInfoContext instance = UIXDSInfoContext.instance;
				if (instance.activeXDS)
				{
					m_state = State.Failed;
					if (pointsFactor == 0f)
					{
						instance.controller.DiscardCurrentSector(DisqualifyReason.BadClipZone);
					}
					else
					{
						instance.controller.DisqualifyYourself(DisqualifyReason.BadClipZone);
					}
				}
			}
			m_info.initialSpeed = 0f;
			CalculateXDS();
		}

		protected override void OnTriggerStay(Collider other)
		{
			base.OnTriggerStay(other);
			if (other == null || !BaseClipping.IsCar(other))
			{
				return;
			}
			CalculateXDS();
			if (!(m_driftController == null) && m_state == State.Idle)
			{
				if (zoneType == ClipZoneType.Default && UIXDSInfoContext.instance.activeXDS && m_driftController.isDrift)
				{
					m_state = State.Activated;
				}
				m_info.minSpeed = Mathf.Min(m_info.minSpeed, m_driftController.driftSpeed);
			}
		}

		protected override void OnTriggerExit(Collider other)
		{
			base.OnTriggerExit(other);
			UIXDSInfoContext instance = UIXDSInfoContext.instance;
			if (zoneType == ClipZoneType.PointsFactor && m_counterCollisions == 0)
			{
				instance.controller.ApplyBonusFactor(1f);
			}
			if (!(m_driftController == null) && !CheckCarSelfCollider(other) && m_counterCollisions == 0 && m_state == State.Activated && !instance.activeXDS)
			{
				m_driftController.ClippingZoneReaction(m_info);
			}
		}

		private void CalculateXDS()
		{
			UIXDSInfoContext instance = UIXDSInfoContext.instance;
			if (zoneType == ClipZoneType.PointsFactor && instance.activeXDS && (pointsFactor < 0f || m_driftController.isDrift))
			{
				instance.controller.ApplyBonusFactor(1f + pointsFactor);
			}
			if (zoneType == ClipZoneType.SpeedControl && !(m_playerCar == null))
			{
				Car carX = m_playerCar.carX;
				if (m_info.initialSpeed == 0f)
				{
					m_info.initialSpeed = carX.speedKMH;
				}
				if (pointsFactor > 0f && (carX.handbrake > 0.1f || carX.brake > 0.1f))
				{
					m_state = State.Failed;
					instance.controller.DisqualifyYourself(DisqualifyReason.BrakeUse);
				}
				if (pointsFactor < 0f && carX.speedKMH > m_info.initialSpeed)
				{
					m_state = State.Failed;
					instance.controller.DisqualifyYourself(DisqualifyReason.AccelerateUse);
				}
			}
		}
	}
}
