using System;
using System.Collections;
using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(FreeDroneCameraMovement))]
public class FreeDroneCamera : MonoBehaviour
{
	public Action onChangedAutofocusMode;

	public Action onChangedTrackingMode;

	private CinemachineTransposer m_bodyTransposer;

	private CinemachineSameAsFollowTarget m_sameAsFollowTargetAim;

	private CinemachineComposer m_composerAim;

	private CinemachineBasicMultiChannelPerlin m_perlin;

	public readonly FreeDroneCameraInputData inputData = new FreeDroneCameraInputData();

	private bool m_autofocusMode;

	private bool m_trackingMode;

	private float m_speedMovement;

	private float m_speedMultiplier;

	private float m_maxSpeed;

	private float m_movementDamping;

	private float m_dampingSpeed;

	private float m_speedRotation;

	private float m_dampRotation;

	private float m_fov;

	private bool m_collisionCamera = true;

	private float m_amplitudeGain;

	private float m_frequencyGain;

	private float m_targetDamping;

	private float m_lockHeadTime;

	public FreeDroneCameraMovement droneCameraMovement { get; private set; }

	public FreeDroneCameraRotation droneCameraRotation { get; private set; }

	public CinemachineVirtualCamera virtualCamera { get; private set; }

	public ref Transform targetTransform => ref droneCameraMovement.targetFollowing;

	public bool autofocusMode
	{
		get
		{
			return m_autofocusMode;
		}
		set
		{
			m_autofocusMode = value;
		}
	}

	public bool trackingMode
	{
		get
		{
			return m_trackingMode;
		}
		set
		{
			m_trackingMode = value;
		}
	}

	public float speedMovement
	{
		get
		{
			return m_speedMovement;
		}
		set
		{
			m_speedMovement = value;
			droneCameraMovement.mainSpeed = m_speedMovement;
		}
	}

	public float speedMultiplier
	{
		get
		{
			return m_speedMultiplier;
		}
		set
		{
			m_speedMultiplier = value;
			droneCameraMovement.shiftAdd = m_speedMultiplier;
		}
	}

	public float maxSpeed
	{
		get
		{
			return m_maxSpeed;
		}
		set
		{
			m_maxSpeed = value;
			droneCameraMovement.maxShift = m_maxSpeed;
		}
	}

	public float movementDamping
	{
		get
		{
			return m_movementDamping;
		}
		set
		{
			m_movementDamping = value;
			if (m_bodyTransposer != null)
			{
				m_bodyTransposer.m_XDamping = m_movementDamping;
				m_bodyTransposer.m_YDamping = m_movementDamping;
				m_bodyTransposer.m_ZDamping = m_movementDamping;
			}
		}
	}

	public float dampingSpeed
	{
		get
		{
			return m_dampingSpeed;
		}
		set
		{
			m_dampingSpeed = value;
			droneCameraMovement.speedDamping = m_dampingSpeed;
		}
	}

	public float speedRotation
	{
		get
		{
			return m_speedRotation;
		}
		set
		{
			m_speedRotation = value;
			droneCameraRotation.speedRotation = m_speedRotation;
		}
	}

	public float dampRotation
	{
		get
		{
			return m_dampRotation;
		}
		set
		{
			m_dampRotation = value;
			droneCameraRotation.rotateForce = m_dampRotation;
		}
	}

	public float fov
	{
		get
		{
			return m_fov;
		}
		set
		{
			m_fov = value;
			droneCameraMovement.SetFov(m_fov);
		}
	}

	public bool collisionCamera
	{
		get
		{
			return m_collisionCamera;
		}
		set
		{
			m_collisionCamera = value;
		}
	}

	public float amplitudeGain
	{
		get
		{
			return m_amplitudeGain;
		}
		set
		{
			m_amplitudeGain = value;
			if (m_perlin != null)
			{
				m_perlin.m_AmplitudeGain = m_amplitudeGain;
			}
		}
	}

	public float frequencyGain
	{
		get
		{
			return m_frequencyGain;
		}
		set
		{
			m_frequencyGain = value;
			if (m_perlin != null)
			{
				m_perlin.m_FrequencyGain = m_frequencyGain;
			}
		}
	}

	public float targetDamping
	{
		get
		{
			return m_targetDamping;
		}
		set
		{
			m_targetDamping = value;
			if (m_composerAim != null)
			{
				m_composerAim.m_VerticalDamping = m_targetDamping;
				m_composerAim.m_HorizontalDamping = m_targetDamping;
			}
		}
	}

	public float lockHeadTime
	{
		get
		{
			return m_lockHeadTime;
		}
		set
		{
			m_lockHeadTime = value;
			if (m_composerAim != null)
			{
				m_composerAim.m_LookaheadTime = m_lockHeadTime;
			}
		}
	}

	private void Awake()
	{
		droneCameraMovement = GetComponent<FreeDroneCameraMovement>();
		droneCameraRotation = GetComponent<FreeDroneCameraRotation>();
		virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>();
		m_bodyTransposer = virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
		m_sameAsFollowTargetAim = virtualCamera.GetCinemachineComponent<CinemachineSameAsFollowTarget>();
		if (m_sameAsFollowTargetAim == null)
		{
			virtualCamera.DestroyCinemachineComponent<CinemachineComposer>();
			m_composerAim = null;
			m_sameAsFollowTargetAim = virtualCamera.AddCinemachineComponent<CinemachineSameAsFollowTarget>();
		}
		m_perlin = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
		if (m_perlin == null)
		{
			m_perlin = virtualCamera.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
		}
		if (m_perlin != null)
		{
			m_perlin.m_AmplitudeGain = m_amplitudeGain;
			m_perlin.m_FrequencyGain = m_frequencyGain;
		}
		droneCameraMovement.Initialize(inputData);
		droneCameraRotation.Initialize(inputData);
		SetDefaultParameters();
		ReinitializeValues();
	}

	private void SetDefaultParameters()
	{
		speedMovement = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedMovement").defaultValue;
		speedMultiplier = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedMultiplier").defaultValue;
		maxSpeed = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("maxSpeed").defaultValue;
		movementDamping = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("movementDamping").defaultValue;
		dampingSpeed = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("dampingSpeed").defaultValue;
		speedRotation = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("speedRotation").defaultValue;
		dampRotation = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("dampRotation").defaultValue;
		fov = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("fov").defaultValue;
		amplitudeGain = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("amplitudeGain").defaultValue;
		frequencyGain = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("frequencyGain").defaultValue;
		targetDamping = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("targetDamping").defaultValue;
		lockHeadTime = ScriptableSingleton<FreeDroneCameraLimits>.instance.GetDataParam("lockHeadTime").defaultValue;
	}

	private void ReinitializeValues()
	{
		speedMovement = m_speedMovement;
		speedMultiplier = m_speedMultiplier;
		maxSpeed = m_maxSpeed;
		movementDamping = m_movementDamping;
		dampingSpeed = m_dampingSpeed;
		speedRotation = m_speedRotation;
		dampRotation = m_dampRotation;
		fov = m_fov;
		amplitudeGain = m_amplitudeGain;
		frequencyGain = m_frequencyGain;
		targetDamping = m_targetDamping;
		lockHeadTime = m_lockHeadTime;
	}

	public void ResetRelative()
	{
		droneCameraMovement.ResetRelative();
	}

	public void SetPosition(Vector3 positionParam)
	{
		droneCameraMovement.SetPosition(positionParam);
	}

	public void LookAt(Vector3 lookAtPosition)
	{
		droneCameraMovement.LookAt(lookAtPosition);
	}

	public void IgnoreCollision()
	{
		droneCameraMovement.IgnoreCollision();
	}

	public void SwitchAutofocusMode(bool autofocusParam)
	{
		if (targetTransform == null)
		{
			return;
		}
		autofocusMode = autofocusParam;
		if (autofocusParam)
		{
			droneCameraMovement.LookAt(targetTransform.position);
		}
		droneCameraMovement.SwitchFollowing(autofocusParam);
		droneCameraRotation.SwitchFollowing(autofocusParam);
		if (autofocusParam)
		{
			if (m_composerAim == null)
			{
				Vector3 trackedObjectOffset = Vector3.up * 0.5f;
				m_composerAim = virtualCamera.AddCinemachineComponent<CinemachineComposer>();
				m_composerAim.ForceCameraPosition(Vector3.zero, Quaternion.LookRotation(Camera.main.transform.forward));
				m_composerAim.m_TrackedObjectOffset = trackedObjectOffset;
			}
			virtualCamera.m_LookAt = targetTransform;
			m_sameAsFollowTargetAim = null;
		}
		else
		{
			if (m_sameAsFollowTargetAim == null)
			{
				m_sameAsFollowTargetAim = virtualCamera.AddCinemachineComponent<CinemachineSameAsFollowTarget>();
				TimeController.StartNow(SetDefaultAimDampingAsync());
			}
			virtualCamera.m_LookAt = null;
			m_composerAim = null;
		}
		onChangedAutofocusMode.SafeCall();
		IEnumerator SetDefaultAimDampingAsync()
		{
			while (!virtualCamera.LookAtTargetChanged)
			{
				yield return null;
			}
			if (m_sameAsFollowTargetAim != null)
			{
				m_sameAsFollowTargetAim.m_Damping = 1.1f;
			}
		}
	}

	public void SwitchTrackingMode(bool trackingParam)
	{
		if (!(targetTransform == null))
		{
			trackingMode = trackingParam;
			droneCameraMovement.SwitchTracking(trackingParam);
			onChangedTrackingMode.SafeCall();
		}
	}

	public void BlockUserControl(bool enableBlock)
	{
		inputData.blockedInput = enableBlock;
	}
}
