using System;
using System.Collections.Generic;
using DI;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using GpuCulling;
using UnityEngine;

public class FMODBaseScript : CARXBaseScript
{
	public class FMODEventParameter
	{
		private FMODEventNode m_node;

		private int m_lastFrameSetValue = int.MinValue;

		public string name { get; }

		public PARAMETER_ID id { get; }

		public bool isFailedInit { get; }

		public float value
		{
			get
			{
				if (!isFailedInit)
				{
					return m_node.GetParameterValue(id);
				}
				return 0f;
			}
			set
			{
				if (!float.IsNaN(value) && !float.IsInfinity(value))
				{
					int frameCount = Time.frameCount;
					if (m_lastFrameSetValue != frameCount && value != m_node.GetParameterValue(id))
					{
						m_lastFrameSetValue = frameCount;
						m_node.SetParameter(this, value);
					}
				}
			}
		}

		public FMODEventParameter(FMODEventNode node, string paramName, bool requireLog = false)
		{
			m_node = node;
			name = paramName;
			PARAMETER_DESCRIPTION parameter;
			RESULT parameterDescriptionByName = node.eventDescription.getParameterDescriptionByName(paramName, out parameter);
			if (parameterDescriptionByName != RESULT.OK)
			{
				if (requireLog)
				{
					GameLog.Print(AlertLevel.Error, "Sound", "FMOD event parameter " + name + " is failed initialization: " + parameterDescriptionByName);
				}
				isFailedInit = true;
			}
			else
			{
				id = parameter.id;
			}
		}

		public override string ToString()
		{
			return $"'{name}' {{{id.data1},{id.data2}}}'";
		}
	}

	public class FMODEventNode
	{
		public class Parameter
		{
			private FMODEventNode m_host;

			public int ID { get; private set; }

			public Parameter(FMODEventNode host, int ID)
			{
				this.ID = ID;
				m_host = host;
			}

			public void SetValue(float value)
			{
				if (ID != -1)
				{
					m_host.SetParameterValue(ID, value);
				}
			}
		}

		private EventInstance m_event;

		private EventDescription m_eventDescription;

		private FMODEventParameter m_speedMPH;

		private FMODEventParameter m_spotVolume;

		private List<FMODEventParameter> m_params = new List<FMODEventParameter>();

		public EventDescription eventDescription => m_eventDescription;

		public FMODEventNode(string eventRef)
		{
			m_event = RuntimeManager.CreateInstance(eventRef);
			RESULT description = m_event.getDescription(out m_eventDescription);
			if (description != RESULT.OK)
			{
				GameLog.Print(AlertLevel.Error, "Sound", "FMOD get description event " + m_event.ToString() + " is failed: " + description);
			}
		}

		public void RegisterParameters()
		{
			if (m_eventDescription.getParameterDescriptionByName("SpeedMPH", out var parameter) == RESULT.OK)
			{
				m_speedMPH = new FMODEventParameter(this, "SpeedMPH");
			}
			if (m_eventDescription.getParameterDescriptionByName("Spot", out parameter) == RESULT.OK)
			{
				m_spotVolume = new FMODEventParameter(this, "Spot");
			}
		}

		public void Start()
		{
			m_event.start();
		}

		public void Stop(STOP_MODE mode)
		{
			m_event.stop(mode);
		}

		public void SetPlaybackState(bool playingState)
		{
			if (m_event.hasHandle())
			{
				bool flag = IsPlaying();
				if (flag && !playingState)
				{
					m_event.stop(STOP_MODE.IMMEDIATE);
				}
				else if (!flag && playingState)
				{
					m_event.start();
				}
			}
		}

		public bool IsPlaying()
		{
			m_event.getPlaybackState(out var state);
			if (state != PLAYBACK_STATE.PLAYING)
			{
				return state == PLAYBACK_STATE.STARTING;
			}
			return true;
		}

		public float GetParameterValue(string nameParam)
		{
			m_event.getParameterByName(nameParam, out var value);
			return value;
		}

		public float GetParameterValue(PARAMETER_ID id)
		{
			m_event.getParameterByID(id, out var value);
			return value;
		}

		public void SetParameter(FMODEventParameter param, float value)
		{
			if (!param.isFailedInit)
			{
				m_event.setParameterByID(param.id, value);
			}
		}

		public Parameter RegisterParameter(string name)
		{
			FMODEventParameter item = new FMODEventParameter(this, name);
			m_params.Add(item);
			return new Parameter(this, m_params.Count - 1);
		}

		private void SetParameterValue(int index, float value)
		{
			m_params[index].value = value;
		}

		public void Release()
		{
			if (m_event.hasHandle())
			{
				m_event.stop(STOP_MODE.IMMEDIATE);
				m_event.release();
				m_event.clearHandle();
				m_params.Clear();
			}
		}

		public void GetMinMaxDistance(out float minDistance, out float maxDistance)
		{
			m_event.getMinMaxDistance(out minDistance, out maxDistance);
		}

		public void Set3DAttributes(ATTRIBUTES_3D attributes)
		{
			m_event.set3DAttributes(attributes);
		}

		public void SetCarSpeedMPHValue(float value)
		{
			if (m_speedMPH != null)
			{
				m_speedMPH.value = value;
			}
		}

		public void SetCarSpotVolume(float value)
		{
			if (m_spotVolume != null)
			{
				m_spotVolume.value = value;
			}
		}
	}

	private RaceCar m_raceCar;

	protected const string voidSoundEvent = "event:/GameEvents/Void";

	protected const string SWAP_SOUND_TEMPLATE = "event:/GameEvents/Engines/{0}/{1}";

	protected const string CAR_SOUND_TEMPLATE = "event:/GameEvents/Cars/{0}/{1}";

	private Transform m_cachedTransform;

	private Rigidbody m_cachedRigidbody;

	private List<FMODEventNode> m_nodes = new List<FMODEventNode>();

	private bool m_someCreateOperationsFailed;

	private CullingHandle m_cullingHandle;

	private float m_cullingDistance;

	protected GamePrefs m_prefs;

	private CameraSwitch m_cameraSwitch;

	public RaceCar raceCar
	{
		get
		{
			if (m_raceCar != null)
			{
				return m_raceCar;
			}
			return m_raceCar = GetComponentInParent<RaceCar>(includeInactive: true);
		}
	}

	public event Action<float> CullingDistanceChangeEvent;

	protected override bool OnEnableHandler()
	{
		if (m_someCreateOperationsFailed)
		{
			return false;
		}
		if (!raceCar)
		{
			return false;
		}
		if (m_prefs == null)
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
		}
		if (m_cameraSwitch == null)
		{
			m_cameraSwitch = CameraSwitch.instance;
		}
		m_cachedRigidbody = GetComponentInParent<Rigidbody>();
		m_cachedTransform = (m_cachedRigidbody ? m_cachedRigidbody.transform : GetComponent<Transform>());
		Set3DAttributes();
		SetupCulling();
		UpdateCullingDistance();
		return base.OnEnableHandler();
	}

	protected override void OnDisableHandler()
	{
		base.OnDisableHandler();
		m_nodes.ForEach(delegate(FMODEventNode x)
		{
			x.Release();
		});
		m_nodes.Clear();
		ReleaseCulling();
	}

	private void SetupCulling()
	{
		if (!m_cullingHandle.IsValid())
		{
			GpuCullingService instance = GpuCullingService.instance;
			m_cullingHandle = instance.AddEntity();
			instance.BindDynamicTransform(in m_cullingHandle, m_cachedTransform);
			instance.BindCallback(in m_cullingHandle, base.SetActive);
			instance.BindCallback(in m_cullingHandle, SetSoundForNodes);
		}
	}

	private void ReleaseCulling()
	{
		if (m_cullingHandle.IsValid())
		{
			GpuCullingService.instance.RemoveEntity(ref m_cullingHandle);
			m_cullingDistance = 0f;
		}
	}

	public float GetCullingDistance()
	{
		return m_cullingDistance;
	}

	private void UpdateCullingDistance()
	{
		if (m_cullingHandle.IsValid() && !(m_cachedTransform == null))
		{
			GetMinMaxDistance(out var _, out var maxDistance);
			maxDistance = Mathf.Clamp(maxDistance, 0f, 100f);
			if (maxDistance > m_cullingDistance)
			{
				m_cullingDistance = maxDistance;
				GpuCullingService.instance.BindSound(in m_cullingHandle, m_cachedTransform.position, maxDistance);
				this.CullingDistanceChangeEvent?.Invoke(m_cullingDistance);
			}
		}
	}

	public virtual void RefreshSound()
	{
		if (!(m_cachedTransform == null) && !(m_cachedRigidbody == null))
		{
			Set3DAttributes();
		}
	}

	protected override void UpdateHandler(float dt)
	{
		base.UpdateHandler(dt);
		if (!BaseCar.IsNull(raceCar))
		{
			UpdateSound(dt);
		}
	}

	protected virtual float GetVolume()
	{
		if (m_cameraSwitch != null)
		{
			if (!(raceCar == m_cameraSwitch.targetRaceCar))
			{
				return m_prefs.globals.otherCarsVolume;
			}
			return m_prefs.globals.targetCarVolume;
		}
		return 1f;
	}

	public void GetMinMaxDistance(out float minDistance, out float maxDistance)
	{
		minDistance = 0f;
		maxDistance = 0f;
		float minDistance2 = 0f;
		float maxDistance2 = 0f;
		foreach (FMODEventNode node in m_nodes)
		{
			node.GetMinMaxDistance(out minDistance2, out maxDistance2);
			if (minDistance > minDistance2)
			{
				minDistance = minDistance2;
			}
			if (maxDistance < maxDistance2)
			{
				maxDistance = maxDistance2;
			}
		}
	}

	public void SetSoundForNodes(bool enable)
	{
		if (!enable)
		{
			for (int i = 0; i < m_nodes.Count; i++)
			{
				m_nodes[i].Stop(GetStopMode());
			}
		}
	}

	protected virtual void SetSoundTo(FMODEventNode node, bool play)
	{
		if (node != null && (!play || !node.IsPlaying()))
		{
			if (play)
			{
				node.Start();
			}
			else
			{
				node.Stop(GetStopMode());
			}
		}
	}

	protected virtual STOP_MODE GetStopMode()
	{
		return STOP_MODE.ALLOWFADEOUT;
	}

	protected virtual void UpdateSound(float dt)
	{
		float speedMPH = raceCar.carX.speedMPH;
		foreach (FMODEventNode node in m_nodes)
		{
			node.SetCarSpeedMPHValue(speedMPH);
			node.SetCarSpotVolume(GetVolume());
		}
		Set3DAttributes();
	}

	protected bool CreateCarNamedNode(ref FMODEventNode eventNode, string eventRef, string fallbackEventRef = "event:/GameEvents/Void")
	{
		return CreateCustomCarNamedNode(ref eventNode, eventRef, raceCar.metaInfo.name, "void", fallbackEventRef);
	}

	protected bool CreateCustomCarNamedNode(ref FMODEventNode eventNode, string eventRef, string metaName, string eventName, string fallbackEventRef = "event:/GameEvents/Void")
	{
		if (raceCar == null || raceCar.metaInfo == null)
		{
			return false;
		}
		string arg = (string.IsNullOrEmpty(metaName) ? raceCar.metaInfo.name : metaName);
		return CreateNode(ref eventNode, string.Format(eventRef, arg, eventName), fallbackEventRef);
	}

	protected bool CreateNode(ref FMODEventNode eventNode, string eventRef, string fallbackEventRef = "event:/GameEvents/Void")
	{
		eventRef = ResolveEventLink(eventRef, fallbackEventRef);
		if (string.IsNullOrEmpty(eventRef))
		{
			m_someCreateOperationsFailed = true;
			return false;
		}
		try
		{
			eventNode = new FMODEventNode(eventRef);
			eventNode.RegisterParameters();
			m_nodes.Add(eventNode);
			UpdateCullingDistance();
		}
		catch (Exception ex)
		{
			UnityEngine.Debug.LogError("FMOD: CreateNode error: " + ex.Message);
			eventNode = null;
			m_someCreateOperationsFailed = true;
		}
		return true;
	}

	protected void ReleaseNode(ref FMODEventNode eventNode)
	{
		if (eventNode != null)
		{
			m_nodes.Remove(eventNode);
			eventNode.Release();
			eventNode = null;
		}
	}

	protected FMODEventNode CreateNodeIfExist(string eventRef)
	{
		if (RuntimeManager.StudioSystem.lookupID(eventRef, out var _) == RESULT.OK)
		{
			FMODEventNode eventNode = null;
			CreateNode(ref eventNode, eventRef);
			return eventNode;
		}
		return null;
	}

	private string ResolveEventLink(string eventLink, string defaultLink = "event:/GameEvents/Void")
	{
		string text = eventLink;
		FMOD.Studio.System studioSystem = RuntimeManager.StudioSystem;
		if (studioSystem.lookupID(text, out var id) != RESULT.OK)
		{
			text = defaultLink;
			if (studioSystem.lookupID(text, out id) != RESULT.OK)
			{
				text = "event:/GameEvents/Void";
				if (studioSystem.lookupID(text, out id) != RESULT.OK)
				{
					text = "";
				}
			}
		}
		return text;
	}

	protected void Set3DAttributes()
	{
		ATTRIBUTES_3D attributes = To3DAttributes(m_cachedTransform, m_cachedRigidbody);
		for (int i = 0; i < m_nodes.Count; i++)
		{
			m_nodes[i].Set3DAttributes(attributes);
		}
	}

	private static ATTRIBUTES_3D To3DAttributes(Transform paramTransform, Rigidbody rigidbody)
	{
		ATTRIBUTES_3D result = default(ATTRIBUTES_3D);
		Quaternion rotation = paramTransform.rotation;
		Vector3 vec = rotation * Vector3.forward;
		Vector3 vec2 = rotation * Vector3.up;
		Vector3 position = paramTransform.position;
		result.forward = ToFMODVector(vec);
		result.up = ToFMODVector(vec2);
		result.position = ToFMODVector(position);
		if ((bool)rigidbody)
		{
			result.velocity = ToFMODVector(rigidbody.velocity);
		}
		return result;
	}

	private static VECTOR ToFMODVector(Vector3 vec)
	{
		VECTOR result = default(VECTOR);
		result.x = vec.x;
		result.y = vec.y;
		result.z = vec.z;
		return result;
	}
}
