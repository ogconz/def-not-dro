using System;
using System.Collections.Generic;
using CarX;
using CarX.Gameplay.ClippingZone;
using TrackLayout;
using TrackLayout.Behaviours;
using TrackLayout.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfigurationManager : MonoBehaviour
{
	private enum WorkMode
	{
		Generic = 0,
		Custom = 1
	}

	private class TrackLayoutDataNode
	{
		public TrackLayoutData data;

		public LegacyTrackContainer container;
	}

	private GameObject syncNetworkNode;

	private WorkMode m_workMode;

	private bool m_initiated;

	private const string FIRST_RACE_CONFIG = "xds_firstRace";

	private const string SECOND_RACE_CONFIG = "xds_secondRace";

	private const string TOP32_REPLAY_CONFIG = "top32Replay";

	private string m_lastSceneName = string.Empty;

	private List<TrackLayoutData> m_layoutData = new List<TrackLayoutData>();

	private readonly Dictionary<string, string> m_configStyle = new Dictionary<string, string>();

	private readonly Dictionary<string, TrackLayoutDataNode> m_legacy = new Dictionary<string, TrackLayoutDataNode>();

	private static List<GameObject> m_childsGO = new List<GameObject>();

	public static ConfigurationManager instance { get; private set; }

	public GameObject current { get; private set; }

	public GameObject lastNetworkPoint { get; private set; }

	public TrackLayoutData selectedTrack { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	private void InitIfNeeded(List<TrackLayoutData> configList = null, string configName = null, RaceMode raceMode = RaceMode.XDS, bool checkWorkMode = true, bool skipInitiated = false)
	{
		if (m_initiated && !skipInitiated)
		{
			return;
		}
		m_initiated = true;
		if (m_workMode != WorkMode.Generic && checkWorkMode)
		{
			return;
		}
		if (configList == null)
		{
			string sceneName = SceneManager.GetActiveScene().name;
			m_legacy.Clear();
			FillTrackLayoutDataForSceneIfNeeded(sceneName);
			m_layoutData.Sort((TrackLayoutData x, TrackLayoutData y) => x.name.CompareTo(y.name));
			if (m_layoutData.Count > 0)
			{
				base.transform.DestroyChildren();
				foreach (TrackLayoutData layoutDatum in m_layoutData)
				{
					m_legacy[layoutDatum.name.ToLower()] = null;
				}
			}
		}
		else
		{
			base.transform.DestroyChildren();
			m_layoutData = configList;
		}
		if (m_layoutData.Count <= 0)
		{
			return;
		}
		if (string.IsNullOrEmpty(configName))
		{
			foreach (TrackLayoutData layoutDatum2 in m_layoutData)
			{
				AddTrackLayout(layoutDatum2, raceMode);
			}
			return;
		}
		AddTrackLayout(m_layoutData.Find((TrackLayoutData x) => IsNameEquals(x.name, configName)), raceMode);
	}

	private void AddTrackLayout(TrackLayoutData itr, RaceMode raceMode)
	{
		if (itr == null)
		{
			return;
		}
		string text = itr.name.ToLower();
		GameObject obj = new GameObject(itr.name);
		Transform transform = obj.transform;
		transform.SetParent(base.transform, worldPositionStays: false);
		obj.SetActive(value: false);
		if (text == "default")
		{
			AttachDataToTrack(text, itr, transform);
			return;
		}
		for (int i = 0; i < 2; i++)
		{
			string style = ((raceMode == RaceMode.Top32ViewReplay) ? "top32Replay" : ((i == 0) ? "xds_firstRace" : "xds_secondRace"));
			Transform transform2 = new GameObject(style).transform;
			transform2.SetParent(transform, worldPositionStays: false);
			AttachDataToTrack(text, itr, transform2, style);
		}
	}

	private void AttachDataToTrack(string name, TrackLayoutData itr, Transform holder, string style = "defaultNetwork")
	{
		TrackLayoutController trackLayoutController = Singleton<TrackLayoutController>.instance;
		trackLayoutController.SetDataRAW(itr);
		trackLayoutController.BindTrack(holder, style);
		m_legacy[name] = new TrackLayoutDataNode
		{
			container = trackLayoutController.legacyTrackContainer,
			data = trackLayoutController.GetData()
		};
		trackLayoutController.legacyTrackContainer = null;
	}

	public BaseClipping[] GetAndResetClippingObjects()
	{
		GameObject gameObject = lastNetworkPoint;
		if (gameObject == null)
		{
			gameObject = current;
		}
		if (gameObject == null)
		{
			return new BaseClipping[0];
		}
		BaseClipping[] componentsInChildren = gameObject.GetComponentsInChildren<BaseClipping>(includeInactive: true);
		BaseClipping[] array = componentsInChildren;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Reset();
		}
		return componentsInChildren;
	}

	private IReadOnlyList<GameObject> GetAllConfigurationData()
	{
		m_childsGO.Clear();
		for (int i = 0; i < base.transform.childCount; i++)
		{
			Transform child = base.transform.GetChild(i);
			m_childsGO.Add(child.gameObject);
		}
		return m_childsGO;
	}

	public List<string> GetAllNetworkNodes()
	{
		List<string> list = new List<string>();
		if (syncNetworkNode == null)
		{
			return list;
		}
		Transform transform = syncNetworkNode.transform;
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			list.Add(child.gameObject.name);
		}
		return list;
	}

	private GameObject FindConfigurationDataByName(IReadOnlyList<GameObject> objects, string configuration)
	{
		for (int i = 0; i < objects.Count; i++)
		{
			GameObject gameObject = objects[i];
			if (IsNameEquals(gameObject.name, configuration))
			{
				return gameObject;
			}
		}
		return null;
	}

	private void InitConfigurationObjects(RaceMode raceMode, string trackConfiguration, bool checkWorkMode = true, bool skipInitiated = false)
	{
		InitIfNeeded(null, (!raceMode.IsNetworkRace()) ? trackConfiguration : null, raceMode, checkWorkMode, skipInitiated);
		IReadOnlyList<GameObject> allConfigurationData = GetAllConfigurationData();
		GameObject gameObject = null;
		if (raceMode.IsNetworkRace())
		{
			syncNetworkNode = FindConfigurationDataByName(allConfigurationData, "SyncNetwork");
			if (!syncNetworkNode)
			{
				syncNetworkNode = new GameObject("SyncNetwork");
				syncNetworkNode.transform.SetParent(base.transform, worldPositionStays: false);
				foreach (GameObject item in allConfigurationData)
				{
					if (!IsNameEquals(item.name, "Default") && !IsNameEquals(item.name, "Reversed"))
					{
						item.transform.SetParent(syncNetworkNode.transform);
					}
				}
				allConfigurationData = GetAllConfigurationData();
			}
			gameObject = FindConfigurationDataByName(allConfigurationData, trackConfiguration + "Network");
		}
		if (gameObject == null)
		{
			gameObject = FindConfigurationDataByName(allConfigurationData, trackConfiguration);
		}
		if (gameObject == null)
		{
			Debug.LogError("Could not find requested configuration " + trackConfiguration + ". Falling back to default configuration");
			gameObject = FindConfigurationDataByName(allConfigurationData, "Default");
			if (gameObject == null)
			{
				Debug.LogError("Could not find Default configuration");
				return;
			}
		}
		AttachTrackContainer(gameObject.name);
		current = gameObject;
		foreach (GameObject item2 in allConfigurationData)
		{
			item2.SetActive(item2 == current);
		}
	}

	public void AttachTrackContainer(string name)
	{
		TrackLayoutController trackLayoutController = Singleton<TrackLayoutController>.instance;
		if (trackLayoutController != null)
		{
			if (m_legacy.TryGetValue(name.ToLower(), out var value) && value != null)
			{
				trackLayoutController.legacyTrackContainer = value.container;
				trackLayoutController.SetDataRAW(value.data);
				trackLayoutController.LoadAdditionalScene(value.data.additionalScene);
			}
			else
			{
				trackLayoutController.legacyTrackContainer = null;
				trackLayoutController.SetDataRAW(null);
				trackLayoutController.LoadAdditionalScene(null);
			}
		}
	}

	public T GetDataByType<T>() where T : MonoBehaviour
	{
		if (current != null)
		{
			T componentInChildren = current.GetComponentInChildren<T>();
			if (componentInChildren == null)
			{
				for (int i = 0; i < current.transform.childCount; i++)
				{
					Transform child = current.transform.GetChild(i);
					if (child.gameObject.activeSelf)
					{
						return child.GetComponentInChildren<T>(includeInactive: true);
					}
				}
			}
			return componentInChildren;
		}
		return null;
	}

	public T GetNetworkDataByType<T>(string name) where T : MonoBehaviour
	{
		if (string.IsNullOrEmpty(name))
		{
			return GetDataByType<T>();
		}
		for (int i = 0; i < syncNetworkNode.transform.childCount; i++)
		{
			Transform child = syncNetworkNode.transform.GetChild(i);
			if (child.name.Equals(name))
			{
				return child.GetComponentInChildren<T>();
			}
		}
		return null;
	}

	public T GetAndActivateData<T>(string name) where T : MonoBehaviour
	{
		lastNetworkPoint = null;
		T result = null;
		if (string.IsNullOrEmpty(name))
		{
			return result;
		}
		IReadOnlyList<GameObject> allConfigurationData = GetAllConfigurationData();
		GameObject gameObject = FindConfigurationDataByName(allConfigurationData, name);
		if (!gameObject)
		{
			return result;
		}
		if (allConfigurationData.Count > 0)
		{
			foreach (GameObject item in allConfigurationData)
			{
				if (item != gameObject)
				{
					item.SetActive(value: false);
				}
			}
		}
		AttachTrackContainer(gameObject.name);
		gameObject.SetActive(value: true);
		Singleton<TrackLayoutController>.instance.ResetPlacementOnAdditionalScene();
		return gameObject.GetComponentInChildren<T>();
	}

	public T GetAndActivateNetworkData<T>(string name) where T : MonoBehaviour
	{
		T result = null;
		if (syncNetworkNode == null || string.IsNullOrEmpty(name))
		{
			return result;
		}
		Transform transform = syncNetworkNode.transform;
		current = syncNetworkNode;
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			child.gameObject.SetActive(IsNameEquals(child.name, name));
			if (!child.gameObject.activeSelf)
			{
				continue;
			}
			result = child.GetComponentInChildren<T>(includeInactive: true);
			lastNetworkPoint = child.gameObject;
			if (!child.gameObject.activeSelf)
			{
				continue;
			}
			lastNetworkPoint = child.gameObject;
			for (int j = 0; j < child.childCount; j++)
			{
				Transform child2 = child.GetChild(j);
				child2.gameObject.SetActive(IsNameEquals(child2.name, "xds_firstRace"));
				if (child2.gameObject.activeSelf)
				{
					result = child2.GetComponentInChildren<T>(includeInactive: true);
				}
			}
		}
		IReadOnlyList<GameObject> allConfigurationData = GetAllConfigurationData();
		if (allConfigurationData.Count > 0)
		{
			foreach (GameObject item in allConfigurationData)
			{
				item.SetActive(value: false);
			}
		}
		AttachTrackContainer(name);
		syncNetworkNode.SetActive(value: true);
		Singleton<TrackLayoutController>.instance.ResetPlacementOnAdditionalScene();
		return result;
	}

	private void SetupFollowCamera()
	{
		FollowCamera followCamera = UnityEngine.Object.FindObjectOfType<FollowCamera>();
		if (followCamera == null)
		{
			Debug.LogWarning("Follow camera not found");
			return;
		}
		CameraTrackingPointsData dataByType = GetDataByType<CameraTrackingPointsData>();
		if (dataByType == null)
		{
			Debug.LogWarning("Data for Follow camera not found");
		}
		else
		{
			followCamera.SetTrackPoints(dataByType.GetPoints());
		}
	}

	public void InitializeReplayConfig(RaceMode raceMode, string configuration)
	{
		InitConfigurationObjects(raceMode, configuration, checkWorkMode: false, skipInitiated: true);
		SetupFollowCamera();
	}

	public void InitializeCustomConfigs(RaceMode raceMode, List<TrackLayoutData> configList)
	{
		InitIfNeeded(configList);
		InitConfigurationObjects(raceMode, "Default");
		SetupFollowCamera();
	}

	public void InitializeConfiguration(RaceMode raceMode, string configuration)
	{
		InitConfigurationObjects(raceMode, configuration);
		SetupFollowCamera();
	}

	public void SetCustomTrack(TrackLayoutData track)
	{
		m_initiated = true;
		selectedTrack = track;
	}

	public void InstantiateCustomConfiguration(string style)
	{
		if (!(selectedTrack == null))
		{
			m_workMode = WorkMode.Custom;
			if (current != null)
			{
				UnityEngine.Object.Destroy(current);
			}
			current = new GameObject(selectedTrack.name);
			Transform transform = current.transform;
			transform.SetParent(base.transform, worldPositionStays: false);
			current.SetActive(value: false);
			Singleton<TrackLayoutController>.instance.SetDataRAW(selectedTrack);
			Singleton<TrackLayoutController>.instance.BindTrack(transform, style);
			Singleton<TrackLayoutController>.instance.LoadAdditionalScene(selectedTrack.additionalScene);
			current.SetActive(value: true);
			SetupFollowCamera();
		}
	}

	private void FillTrackLayoutDataForSceneIfNeeded(string sceneName)
	{
		if (!(m_lastSceneName == sceneName) || m_layoutData == null)
		{
			m_lastSceneName = sceneName;
			m_layoutData = TrackMetaDB.LoadConfig<TrackLayoutData>("Assets/Settings/Tracks/" + sceneName + "/_Network/");
			m_configStyle.Clear();
			for (int i = 0; i < m_layoutData.Count; i++)
			{
				m_configStyle[m_layoutData[i].name] = "xds_firstRace";
			}
		}
	}

	public T GetAndActivateNetworkData<T>(string name, bool isLeader) where T : MonoBehaviour
	{
		T result = null;
		if (syncNetworkNode == null || string.IsNullOrEmpty(name) || m_configStyle.Count == 0)
		{
			return result;
		}
		string text = (isLeader ? "xds_firstRace" : "xds_secondRace");
		m_configStyle[name] = text;
		Transform transform = syncNetworkNode.transform;
		current = syncNetworkNode;
		for (int i = 0; i < transform.childCount; i++)
		{
			Transform child = transform.GetChild(i);
			child.gameObject.SetActive(IsNameEquals(child.name, name));
			if (!child.gameObject.activeSelf)
			{
				continue;
			}
			lastNetworkPoint = child.gameObject;
			for (int j = 0; j < child.childCount; j++)
			{
				Transform child2 = child.GetChild(j);
				child2.gameObject.SetActive(IsNameEquals(child2.name, text));
				if (child2.gameObject.activeSelf)
				{
					result = child2.GetComponentInChildren<T>(includeInactive: true);
				}
			}
		}
		IReadOnlyList<GameObject> allConfigurationData = GetAllConfigurationData();
		if (allConfigurationData.Count > 0)
		{
			foreach (GameObject item in allConfigurationData)
			{
				item.SetActive(value: false);
			}
		}
		AttachTrackContainer(name);
		syncNetworkNode.SetActive(value: true);
		Singleton<TrackLayoutController>.instance.ResetPlacementOnAdditionalScene();
		return result;
	}

	private static bool IsNameEquals(string str1, string str2)
	{
		return str1.StartsWith(str2, StringComparison.CurrentCultureIgnoreCase);
	}

	private void DestroyObjects(IEnumerable<GameObject> objects)
	{
		foreach (GameObject @object in objects)
		{
			@object.SetActive(value: false);
		}
	}
}
