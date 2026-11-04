using System;
using ArrayExtension;
using CarX;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class VCamCustomSetting : MonoBehaviour
{
	[SerializeField]
	private string m_followTargetTag;

	[SerializeField]
	private string m_followTargetName;

	[SerializeField]
	private string m_lookAtTargetTag;

	[SerializeField]
	private string m_lookAtTargetName;

	[Space]
	[Header("Fallback properties")]
	[SerializeField]
	private string m_fallbackFollowTargetTag;

	[SerializeField]
	private string m_fallbackFollowTargetName;

	[SerializeField]
	private string m_fallbackLookAtTargetTag;

	[SerializeField]
	private string m_fallbackLookAtTargetName;

	private CinemachineVirtualCamera m_virtualCamera;

	private bool m_enabledOffsets;

	private void Awake()
	{
		m_virtualCamera = GetComponent<CinemachineVirtualCamera>();
	}

	public void SetTarget(Car target, CustomizationCameraPreset stateType, bool isOffsetEnabled = false)
	{
		if (!(target == null))
		{
			Transform[] componentsInChildren = target.GetComponentsInChildren<Transform>();
			Transform placeTransform = FindByMatches(componentsInChildren, FindByTag(m_followTargetTag), FindByTag(m_fallbackFollowTargetTag), FindByName(m_followTargetName), FindByName(GetAltName(m_followTargetName)), FindByName(m_fallbackFollowTargetName), FindByName(GetAltName(m_fallbackFollowTargetName)));
			Transform lookAtTargetTransform = FindByMatches(componentsInChildren, FindByTag(m_lookAtTargetTag), FindByTag(m_fallbackLookAtTargetTag), FindByName(m_lookAtTargetName), FindByName(GetAltName(m_lookAtTargetName)), FindByName(m_fallbackLookAtTargetName), FindByName(GetAltName(m_fallbackLookAtTargetName)));
			m_enabledOffsets = isOffsetEnabled;
			TryFollowCameraToTarget(placeTransform, stateType);
			TryCameraLookAtTarget(lookAtTargetTransform, stateType);
		}
	}

	private Transform FindByMatches(Transform[] transforms, params Predicate<Transform>[] matches)
	{
		Transform transform = null;
		for (int i = 0; i < matches.Length; i++)
		{
			transform = transforms.Find(matches[i]);
			if (transform != null)
			{
				break;
			}
		}
		return transform;
	}

	private Predicate<Transform> FindByName(string targetName)
	{
		return InternalFindByName;
		bool InternalFindByName(Transform t)
		{
			return CompareName(t, targetName);
		}
	}

	private Predicate<Transform> FindByTag(string targetTag)
	{
		return InternalFindByTag;
		bool InternalFindByTag(Transform t)
		{
			return CompareTag(t, targetTag);
		}
	}

	private string GetAltName(string targetName)
	{
		return targetName.Replace("Point", "CameraPoint");
	}

	private bool CompareTag(Transform target, string targetTag)
	{
		if (!string.IsNullOrEmpty(targetTag))
		{
			return target.CompareTag(targetTag);
		}
		return false;
	}

	private bool CompareName(Transform target, string targetName)
	{
		return target.name.Equals(targetName, StringComparison.InvariantCultureIgnoreCase);
	}

	private void TryFollowCameraToTarget(Transform placeTransform, CustomizationCameraPreset stateType)
	{
		if (!(placeTransform == null))
		{
			m_virtualCamera.Follow = SetCameraOffset(placeTransform, stateType, CameraOffsetPartForGarage.FindTarget, isLookTarget: false);
		}
	}

	private void TryCameraLookAtTarget(Transform lookAtTargetTransform, CustomizationCameraPreset stateType)
	{
		if (!(lookAtTargetTransform == null))
		{
			m_virtualCamera.LookAt = SetCameraOffset(lookAtTargetTransform, stateType, CameraOffsetPartForGarage.FindTargetLookAt, isLookTarget: true);
		}
	}

	private Transform SetCameraOffset(Transform to, CustomizationCameraPreset stateType, Func<CameraOffsetPartForGarage, CustomizationCameraPreset, GameObject> funcFindTarget, bool isLookTarget)
	{
		if (m_enabledOffsets)
		{
			CameraOffsetPartForGarage cameraOffsetPartForGarage = null;
			Vector3 zero = Vector3.zero;
			if (to.parent != null)
			{
				cameraOffsetPartForGarage = to.parent.GetComponent<CameraOffsetPartForGarage>();
			}
			if (!(cameraOffsetPartForGarage != null))
			{
				return to;
			}
			zero = (isLookTarget ? cameraOffsetPartForGarage.GetRotationOffset(stateType) : cameraOffsetPartForGarage.GetPositionOffset(stateType));
			if (zero != Vector3.zero)
			{
				GameObject obj = funcFindTarget(cameraOffsetPartForGarage, stateType);
				obj.transform.position = to.position + zero;
				obj.transform.rotation = to.rotation;
				return obj.transform;
			}
		}
		return to;
	}
}
