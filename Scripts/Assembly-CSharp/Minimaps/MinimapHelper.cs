using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Minimaps
{
	public static class MinimapHelper
	{
		public const float zoomSpeed = 30f;

		private const float startAnimTime = 0.5f;

		public static readonly Vector2 defaultIconSize = new Vector2(32f, 32f);

		private static readonly Vector3 m_mapOnLoadRootScale = new Vector3(0.3f, 0.3f, 1f);

		private static readonly Vector3 m_baseMapRootScale = new Vector3(1.2f, 1.2f, 1f);

		public static Vector2 worldMapBoundsCenter => Minimap.instance.boundsCenter;

		public static Vector2 worldMapBoundsHalfSize => Minimap.instance.boundsSize * 0.5f;

		public static Vector2 GetCarMapPosition(RaceCar car)
		{
			if (car == null)
			{
				return Vector2.zero;
			}
			Vector3 position = car.getTransform.position;
			return new Vector2(position.x, position.z) - worldMapBoundsCenter;
		}

		public static Vector2 WorldToMap(Vector3 worldPos, RectTransform rectTransform)
		{
			Vector2 boundsSize = Minimap.instance.boundsSize;
			Vector2 vector = new Vector2(worldPos.x, worldPos.z) - worldMapBoundsCenter;
			float num = rectTransform.sizeDelta.x / boundsSize.x;
			return vector * num;
		}

		public static bool IsManyZoneExists(IList<Checkpoint> checkpoints)
		{
			if (checkpoints.Count <= 1)
			{
				return false;
			}
			int zone = checkpoints[0].zone;
			for (int i = 1; i < checkpoints.Count; i++)
			{
				Checkpoint checkpoint = checkpoints[i];
				if (checkpoint.zone > 0 || zone != checkpoint.zone)
				{
					return true;
				}
			}
			return false;
		}

		public static void ZoomAnimation(Vector2 newPosition, RectTransform maskTransform, RectTransform imageTransform, float newScaleX, float newScaleY)
		{
			DOTween.To(() => imageTransform.rect.width, delegate(float x)
			{
				imageTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, x);
			}, newScaleX, 0.15f).SetEase(Ease.OutQuint).SetUpdate(isIndependentUpdate: true)
				.Play();
			DOTween.To(() => imageTransform.rect.height, delegate(float y)
			{
				imageTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, y);
			}, newScaleY, 0.15f).SetEase(Ease.OutQuint).SetUpdate(isIndependentUpdate: true)
				.Play();
			DoMapMove(newPosition, maskTransform);
		}

		public static void DoMapMove(Vector2 newPosition, RectTransform maskTransform)
		{
			maskTransform.DOLocalMove(newPosition, 0.15f).SetEase(Ease.OutQuint).SetUpdate(isIndependentUpdate: true)
				.Play();
		}

		public static Vector2 CalculateNewAxisScaleValue(Vector2 currectAxisSizes, float deltaSize, float defaultMapScale, float minSizeScale, float maxSizeScale)
		{
			float value = currectAxisSizes.x + 30f * deltaSize;
			float value2 = currectAxisSizes.y + 30f * deltaSize;
			float min = defaultMapScale * minSizeScale;
			float min2 = defaultMapScale * minSizeScale;
			float max = defaultMapScale * maxSizeScale;
			float max2 = defaultMapScale * maxSizeScale;
			float x = Mathf.Clamp(value, min, max);
			float y = Mathf.Clamp(value2, min2, max2);
			return new Vector2(x, y);
		}

		public static Vector3 CalculatePivotOffset(Vector2 zoomPoint, RectTransform maskTransform, float widthRatio, float heightRatio)
		{
			return new Vector3((zoomPoint.x - maskTransform.localPosition.x) * (1f - widthRatio), (zoomPoint.y - maskTransform.localPosition.y) * (1f - heightRatio), 0f);
		}

		public static void PlayStartAnimation(Transform rootTransform)
		{
			rootTransform.localScale = m_mapOnLoadRootScale;
			rootTransform.DOKill(complete: true);
			rootTransform.DOScale(m_baseMapRootScale, 0.5f).SetEase(Ease.OutExpo).SetUpdate(isIndependentUpdate: true)
				.Play();
		}

		public static Vector2 GetCarMapPosOffset()
		{
			Vector2 carMapPosition = GetCarMapPosition(MinimapProvider.instance.playerCar);
			return new Vector2
			{
				x = carMapPosition.x * 0.5f / worldMapBoundsHalfSize.x,
				y = carMapPosition.y * 0.5f / worldMapBoundsHalfSize.y
			};
		}

		public static float GetTargetMinimapAngle()
		{
			Vector3 directionForMinimap = MinimapProvider.instance.playerCar.GetDirectionForMinimap();
			return ((Vector3.Dot(directionForMinimap, Vector3.Cross(Vector3.up, Vector3.forward)) <= 0f) ? 1f : (-1f)) * Vector3.Angle(directionForMinimap, Vector3.forward);
		}
	}
}
