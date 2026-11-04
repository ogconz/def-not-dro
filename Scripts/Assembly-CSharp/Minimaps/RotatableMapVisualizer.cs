using HUD;
using UnityEngine;

namespace Minimaps
{
	public class RotatableMapVisualizer : BaseMinimapVisualizer
	{
		private const float CheckpointDist = 400f;

		private const float CheckpointHeightDist = 2f;

		protected float m_currentMapAngle;

		private float m_mapAngleVelocity;

		private void UpdateScrollPosition()
		{
			if (!(MinimapProvider.instance.playerCar == null))
			{
				SetCurrectMapAngle();
				if (float.IsNaN(m_currentMapAngle) || float.IsInfinity(m_currentMapAngle))
				{
					SetDefaultMinimapData();
				}
				RootLocalRotate();
			}
		}

		private void SetCurrectMapAngle()
		{
			float targetMinimapAngle = MinimapHelper.GetTargetMinimapAngle();
			m_currentMapAngle = Mathf.SmoothDampAngle(m_currentMapAngle, targetMinimapAngle, ref m_mapAngleVelocity, 0.25f);
		}

		private void SetDefaultMinimapData()
		{
			m_currentMapAngle = 0f;
			m_mapAngleVelocity = 0f;
		}

		private void RootLocalRotate()
		{
			Vector2 carMapPosOffset = MinimapHelper.GetCarMapPosOffset();
			m_mapRoot.sizeDelta = m_mapImage.rectTransform.sizeDelta;
			m_mapRoot.pivot = carMapPosOffset + Vector2.one * 0.5f;
			m_mapRoot.localEulerAngles = Vector3.back * m_currentMapAngle;
		}

		private float GetDistFactor(MinimapItem item)
		{
			float startLineDistance = MinimapProvider.instance.checkpointManager.GetStartLineDistance();
			return Mathf.Clamp01(1f - Mathf.Abs(startLineDistance + m_carDist - (item.host as Checkpoint).distance) / 400f);
		}

		private float CalculateDistFactorDelta(MinimapItem item)
		{
			float y = MinimapProvider.instance.playerCar.getTransform.position.y;
			float y2 = item.transform.position.y;
			return Mathf.Clamp01(1f - Mathf.Abs(y - y2) / 2f);
		}

		private void SetCheckpointActualSize(UIMinimapItem icon, float distFactor)
		{
			icon.size = new Vector2(icon.size.x, 6f);
			icon.scale = Vector2.one * distFactor;
		}

		private void Update()
		{
			if (!(Minimap.instance == null))
			{
				UpdateIcons();
				UpdateMinimapTexture();
				m_stageMaterial1.SetCustomTextureDeltaSize();
				UpdateScrollPosition();
			}
		}

		protected override bool IsNeedActivate(MinimapItem item, bool defaultShowValue)
		{
			if (item.isCheckpoint)
			{
				if (States.current is BaseRaceModeState baseRaceModeState)
				{
					return baseRaceModeState.raceMode != RaceMode.Freeride;
				}
				return false;
			}
			return defaultShowValue;
		}

		protected override void UpdateCheckpointItem(UIMinimapItem uiCheckpointItem)
		{
			if (!(MinimapProvider.instance.playerCar == null))
			{
				float num = GetDistFactor(uiCheckpointItem.item);
				if (MinimapProvider.instance.data.zoneList != null && num > 0.01f)
				{
					num *= CalculateDistFactorDelta(uiCheckpointItem.item);
				}
				SetCheckpointActualSize(uiCheckpointItem, num);
			}
		}
	}
}
