using CarX.Gameplay.ClippingZone;
using TrackLayout.Style;
using UnityEngine;

namespace TrackLayout.Builders
{
	public static class FlagPlacer
	{
		public enum PlaceMode
		{
			LeftProjection = 0,
			Left = 1,
			Right = 2,
			RightProjection = 3
		}

		public static Transform target;

		public static Quaternion rotation;

		public static Vector3 leftBorder;

		public static float leftDistance;

		public static Vector3 rightBorder;

		public static float rightDistance;

		public static TrackStyle style;

		public static PlaceMode mode;

		public static void Setup(TrackStyle style, Transform target, Vector3 direction, Vector3 leftBorder, float leftDistance, float rightDistance, Vector3 rightBorder)
		{
			FlagPlacer.target = target;
			FlagPlacer.style = style;
			FlagPlacer.leftBorder = leftBorder;
			FlagPlacer.rightBorder = rightBorder;
			FlagPlacer.leftDistance = leftDistance;
			FlagPlacer.rightDistance = rightDistance;
			rotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(0f, 90f, 0f);
		}

		public static void PlaceClipPoint(float scaleX)
		{
			PlaceClipPoint(mode, style.ClipPoint, scaleX);
		}

		public static void PlaceFlagSingle()
		{
			PlaceLeg(mode, style.ClipzoneFlag);
		}

		public static void PlaceFlagDouble()
		{
			switch (mode)
			{
			case PlaceMode.LeftProjection:
			case PlaceMode.RightProjection:
				PlaceLeg(PlaceMode.LeftProjection, style.ClipzoneFlag);
				PlaceLeg(PlaceMode.RightProjection, style.ClipzoneFlag);
				break;
			case PlaceMode.Left:
			case PlaceMode.Right:
				PlaceLeg(PlaceMode.Left, style.ClipzoneFlag);
				PlaceLeg(PlaceMode.Right, style.ClipzoneFlag);
				break;
			}
		}

		public static void PlaceArrowWall()
		{
			Vector3 normalized = (rightBorder - leftBorder).normalized;
			Transform transform = Object.Instantiate(style.ClipzoneArrows, target, worldPositionStays: false);
			transform.position = leftBorder + 0.5f * normalized * leftDistance + 2f * Vector3.up;
			transform.localScale = new Vector3(leftDistance, 2f, 1f);
			transform.localRotation = rotation * Quaternion.Euler(0f, 180f, 0f);
			MeshRenderer componentInChildren = transform.GetComponentInChildren<MeshRenderer>();
			if ((bool)componentInChildren)
			{
				componentInChildren.material.mainTextureScale = new Vector2(0.5f * leftDistance, 1f);
			}
			Transform transform2 = Object.Instantiate(style.ClipzoneArrows, target, worldPositionStays: false);
			transform2.position = rightBorder - 0.5f * normalized * rightDistance + 2f * Vector3.up;
			transform2.localScale = new Vector3(rightDistance, 2f, 1f);
			transform2.localRotation = rotation;
			componentInChildren = transform2.GetComponentInChildren<MeshRenderer>();
			if ((bool)componentInChildren)
			{
				componentInChildren.material.mainTextureScale = new Vector2(0.5f * rightDistance, 1f);
			}
		}

		public static void PlaceBillboard()
		{
			float magnitude = (leftBorder - rightBorder).magnitude;
			Transform transform = Object.Instantiate(style.ClipzoneBillboardBody, target, worldPositionStays: false);
			transform.position = 0.5f * (leftBorder + rightBorder) + 8f * Vector3.up;
			transform.localScale = new Vector3(magnitude, 2f, 1f);
			transform.localRotation = rotation;
			MeshRenderer componentInChildren = transform.GetComponentInChildren<MeshRenderer>();
			if ((bool)componentInChildren)
			{
				componentInChildren.material.mainTextureScale = new Vector2(Mathf.Max(1f, Mathf.Round(magnitude / 6f)), 1f);
			}
			PlaceLeg(PlaceMode.LeftProjection, style.ClipzoneBillboardLeg);
			PlaceLeg(PlaceMode.RightProjection, style.ClipzoneBillboardLeg);
		}

		public static void PlaceBillboardMini()
		{
			Vector3 normalized = (rightBorder - leftBorder).normalized;
			float num = (leftBorder - rightBorder).magnitude - leftDistance - rightDistance;
			Transform transform = Object.Instantiate(style.ClipzoneBillboardBody, target, worldPositionStays: false);
			transform.position = leftBorder + normalized * (leftDistance + 0.5f * num) + 8f * Vector3.up;
			transform.localScale = new Vector3(num, 2f, 1f);
			transform.localRotation = rotation;
			MeshRenderer componentInChildren = transform.GetComponentInChildren<MeshRenderer>();
			if ((bool)componentInChildren)
			{
				componentInChildren.material.mainTextureScale = new Vector2(Mathf.Max(1f, Mathf.Round(num / 6f)), 1f);
			}
			Transform transform2 = Object.Instantiate(style.ClipzoneBillboardRope, target, worldPositionStays: false);
			transform2.position = leftBorder + 0.5f * normalized * leftDistance + 8f * Vector3.up;
			transform2.localScale = new Vector3(leftDistance, 0.5f, 1f);
			transform2.localRotation = rotation;
			componentInChildren = transform2.GetComponentInChildren<MeshRenderer>();
			if ((bool)componentInChildren)
			{
				componentInChildren.material.mainTextureScale = new Vector2(Mathf.Max(1f, Mathf.Round(num / 6f)), 1f);
			}
			Transform transform3 = Object.Instantiate(style.ClipzoneBillboardRope, target, worldPositionStays: false);
			transform3.position = rightBorder - 0.5f * normalized * rightDistance + 8f * Vector3.up;
			transform3.localScale = new Vector3(rightDistance, 0.5f, 1f);
			transform3.localRotation = rotation;
			componentInChildren = transform3.GetComponentInChildren<MeshRenderer>();
			if ((bool)componentInChildren)
			{
				componentInChildren.material.mainTextureScale = new Vector2(Mathf.Max(1f, Mathf.Round(num / 6f)), 1f);
			}
			PlaceLeg(PlaceMode.LeftProjection, style.ClipzoneBillboardLeg);
			PlaceLeg(PlaceMode.RightProjection, style.ClipzoneBillboardLeg);
		}

		private static void PlaceLeg(PlaceMode mode, Transform prefab)
		{
			Vector3 normalized = (rightBorder - leftBorder).normalized;
			Quaternion localRotation = rotation;
			Vector3 position = Vector3.zero;
			switch (mode)
			{
			case PlaceMode.LeftProjection:
				position = leftBorder;
				localRotation *= Quaternion.Euler(0f, 90f, 0f);
				break;
			case PlaceMode.Left:
				position = leftBorder + normalized * leftDistance;
				localRotation *= Quaternion.Euler(0f, 90f, 0f);
				break;
			case PlaceMode.Right:
				position = rightBorder - normalized * rightDistance;
				localRotation *= Quaternion.Euler(0f, -90f, 0f);
				break;
			case PlaceMode.RightProjection:
				position = rightBorder;
				localRotation *= Quaternion.Euler(0f, -90f, 0f);
				break;
			}
			Transform transform = Object.Instantiate(prefab, target, worldPositionStays: false);
			transform.position = position;
			transform.localRotation = localRotation;
		}

		private static void PlaceClipPoint(PlaceMode mode, Transform prefab, float scaleX)
		{
			Vector3 normalized = (rightBorder - leftBorder).normalized;
			Quaternion quaternion = rotation;
			Vector3 position = Vector3.zero;
			switch (mode)
			{
			case PlaceMode.LeftProjection:
				position = leftBorder;
				break;
			case PlaceMode.Left:
				position = leftBorder + normalized * leftDistance;
				break;
			case PlaceMode.Right:
				position = rightBorder - normalized * rightDistance;
				quaternion *= Quaternion.Euler(0f, 180f, 0f);
				break;
			case PlaceMode.RightProjection:
				position = rightBorder;
				quaternion *= Quaternion.Euler(0f, 180f, 0f);
				break;
			}
			ClippingPoint component = Object.Instantiate(prefab, position, quaternion, target).GetComponent<ClippingPoint>();
			if ((bool)component)
			{
				component.SetLocalScale(new Vector3(scaleX, 1f, 1f));
				component.Init();
			}
		}
	}
}
