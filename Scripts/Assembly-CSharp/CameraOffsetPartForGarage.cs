using System;
using System.Collections.Generic;
using Generic.Attributes;
using UnityEngine;

public class CameraOffsetPartForGarage : MonoBehaviour
{
	[Serializable]
	public class CamOffsetCustomization
	{
		public CustomizationCameraPreset stateType;

		public Vector3 position;

		public Vector3 rotation;
	}

	[SerializeField]
	[ArrayElementTitle("stateType")]
	private List<CamOffsetCustomization> m_presets;

	private List<GameObject> m_targets;

	private List<GameObject> m_targetsLookAt;

	private CamOffsetCustomization m_currentCache;

	private void Awake()
	{
		CreateAllTargets();
		UpdateAllTargets();
	}

	private void CreateAllTargets()
	{
		m_targets = new List<GameObject>(m_presets.Count);
		m_targetsLookAt = new List<GameObject>(m_presets.Count);
	}

	private void UpdateAllTargets()
	{
		if (m_targets != null && m_targetsLookAt != null)
		{
			for (int i = 0; i < m_presets.Count; i++)
			{
				m_targets.Add(new GameObject("TargetForCamera"));
				m_targets[i].transform.SetParent(base.transform);
				m_targetsLookAt.Add(new GameObject("LookAtTargetForCamera"));
				m_targetsLookAt[i].transform.SetParent(base.transform);
			}
		}
	}

	private void ClearAllTargets()
	{
		if (m_targets != null && m_targetsLookAt != null)
		{
			for (int i = 0; i < m_targets.Count; i++)
			{
				UnityEngine.Object.Destroy(m_targets[i]);
				UnityEngine.Object.Destroy(m_targetsLookAt[i]);
			}
			m_targets.Clear();
			m_targetsLookAt.Clear();
		}
	}

	private void TryFindPreset(CustomizationCameraPreset stateType)
	{
		if (m_currentCache == null || m_currentCache.stateType != stateType)
		{
			m_currentCache = m_presets.Find((CamOffsetCustomization p) => p.stateType == stateType);
		}
	}

	private GameObject FindTargetInList(CustomizationCameraPreset stateType, List<GameObject> targets)
	{
		int num = m_presets.FindIndex((CamOffsetCustomization preset) => preset.stateType == stateType);
		if (num == -1)
		{
			return null;
		}
		return targets[num];
	}

	public static GameObject FindTargetLookAt(CameraOffsetPartForGarage presetOffset, CustomizationCameraPreset stateType)
	{
		return presetOffset.FindTargetInList(stateType, presetOffset.m_targetsLookAt);
	}

	public static GameObject FindTarget(CameraOffsetPartForGarage presetOffset, CustomizationCameraPreset stateType)
	{
		return presetOffset.FindTargetInList(stateType, presetOffset.m_targets);
	}

	public Vector3 GetPositionOffset(CustomizationCameraPreset stateType)
	{
		TryFindPreset(stateType);
		return m_currentCache?.position ?? Vector3.zero;
	}

	public Vector3 GetRotationOffset(CustomizationCameraPreset stateType)
	{
		TryFindPreset(stateType);
		return m_currentCache?.rotation ?? Vector3.zero;
	}
}
