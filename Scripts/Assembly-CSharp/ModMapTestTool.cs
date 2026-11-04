using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ModMapTestTool
{
	public const float BYTES_TO_MEGABYTES = 1048576f;

	private static readonly List<GameObject> m_gameObjects = new List<GameObject>();

	private static Dictionary<Vector3, int> m_vertexCountPositionDiscreate = new Dictionary<Vector3, int>();

	public static Func<string, bool> playCallback;

	public static Action<string, string> errorCallback;

	private string m_currentName;

	private GameObject m_root;

	private List<ValidItem> m_listValid = new List<ValidItem>();

	public static readonly ValidItemData Target = new ValidItemData(4096f, 24f, new ValidItem("Transform", 1, 20000), new ValidItem("MeshCollider", 1, 10000), new ValidItem("BoxCollider", 0, 10000), new ValidItem("SphereCollider", 0, 1000), new ValidItem("CapsuleCollider", 0, 1000), new ValidItem("Rigidbody", 0, 1000), new ValidItem("FixedJoint", 0, 100), new ValidItem("SpringJoint", 0, 100), new ValidItem("HingeJoint", 0, 100), new ValidItem("ConfigurableJoint", 0, 100), new ValidItem("ReflectionProbe", 1, 1), new ValidItem("HDAdditionalLightData", 0, 500), new ValidItem("HDAdditionalReflectionData", 0, 200), new ValidItem("Volume", 1, 1), new ValidItem("MeshRenderer", 0, 10000, new ValidMeshRendererPlayer()), new ValidItem("MeshFilter", 0, 10000), new ValidItem("Light", 0, 500), new ValidItem("LODGroup", 0, 10000), new ValidItem("Animator", 0, 100), new ValidItem("Canvas", 0, 10), new ValidItem("CanvasScaler", 0, 10), new ValidItem("CanvasRenderer", 0, 50), new ValidItem("RectTransform", 0, 100), new ValidItem("TextMeshProUGUI", 0, 50), new ValidItem("RawImage", 0, 20), new ValidItem("VideoPlayer", 0, 5, new ValidVideoPlayer()), new ValidItem("ParticleSystem", 0, 200), new ValidItem("ParticleSystemRenderer", 0, 200), new ValidItem("VisualEffect", 0, 200), new ValidItem("VFXRenderer", 0, 200), new ValidItem("GameMarkerData", 1, 10000), new ValidItem("CacheData", 0, 1), new ValidItem("Minimap", 1, 1), new ValidItem("SceneObjectIDMapSceneAsset", 0, 1));

	public static void InitTests(string sceneName)
	{
		m_gameObjects.Clear();
		AddTreeGameObjectToList(SceneManager.GetSceneByName(sceneName).GetRootGameObjects());
	}

	public static bool IsNotCorrectMapFileSize(string name, string pathToFile)
	{
		FileInfo fileInfo = new FileInfo(pathToFile);
		if (!fileInfo.Exists)
		{
			TryErrorMessage(name, "Map file is not exist");
			return true;
		}
		float num = (float)fileInfo.Length / 1048576f;
		bool num2 = Target.maxSizeInMb < num;
		if (num2)
		{
			string text = num.ToString();
			float maxSizeInMb = Target.maxSizeInMb;
			TryErrorMessage(name, "Map size is " + text + "/" + maxSizeInMb);
		}
		return num2;
	}

	public static bool IsNotCorrectMetaFileSize(string pathToFile)
	{
		FileInfo fileInfo = new FileInfo(pathToFile);
		if (!fileInfo.Exists)
		{
			TryErrorMessage(null, "Meta file is not exist");
			return true;
		}
		float num = (float)fileInfo.Length / 1048576f;
		bool num2 = Target.maxSizeInMbMeta < num;
		if (num2)
		{
			string text = num.ToString();
			float maxSizeInMbMeta = Target.maxSizeInMbMeta;
			TryErrorMessage(null, "Meta size is " + text + "/" + maxSizeInMbMeta);
		}
		return num2;
	}

	private static void SpawnPointTestExistence()
	{
		int num = 0;
		foreach (GameObject gameObject in m_gameObjects)
		{
			GameMarkerData component = gameObject.GetComponent<GameMarkerData>();
			if (component != null && component.markerData.GetHead() == "spawnpoint")
			{
				num++;
			}
		}
		if (num == 0)
		{
			throw new Exception("No one spawn point in map");
		}
		if (num > 1)
		{
			throw new Exception("Multiple spawn point in map");
		}
	}

	public static bool RunTest(string sceneName)
	{
		try
		{
			SpawnPointTestExistence();
			return false;
		}
		catch (Exception ex)
		{
			TryErrorMessage(sceneName, ex.Message);
			return true;
		}
	}

	private static void AddTreeGameObjectToList(GameObject[] gameObjects)
	{
		foreach (GameObject gameObject in gameObjects)
		{
			for (int j = 0; j < gameObject.transform.childCount; j++)
			{
				Transform child = gameObject.transform.GetChild(j);
				m_gameObjects.Add(child.gameObject);
				AddTreeGameObjectToList(new GameObject[1] { child.gameObject });
			}
		}
	}

	public static void TryErrorMessage(string name, string message)
	{
		if (!string.IsNullOrEmpty(message))
		{
			errorCallback?.Invoke(name, message);
		}
	}

	public static ModMapTestTool Play(string name)
	{
		bool? flag = playCallback?.Invoke(name);
		if (flag.HasValue && !flag.Value)
		{
			return null;
		}
		if (GameObject.Find("root") == null)
		{
			TryErrorMessage(name, "");
			return null;
		}
		return new ModMapTestTool
		{
			m_root = GameObject.Find("root"),
			m_currentName = name
		};
	}

	public ModMapTestTool With((string, int, int) value)
	{
		m_listValid.Add(new ValidItem(value.Item1, value.Item2, value.Item3));
		return this;
	}

	public ModMapTestTool WithList(List<ValidItem> value)
	{
		m_listValid.Clear();
		m_listValid.AddRange(value);
		return this;
	}

	private void ValidComponents(Transform parent)
	{
		Component[] components = parent.GetComponents(typeof(Component));
		foreach (Component component in components)
		{
			if (component != null && !ValidType(component, m_listValid))
			{
				Type type = component.GetType();
				TryErrorMessage(m_currentName, new ValidItem(type.Name, int.MinValue, int.MaxValue, null, 1).ToString());
				return;
			}
		}
		for (int j = 0; j < parent.transform.childCount; j++)
		{
			Transform child = parent.transform.GetChild(j);
			ValidComponents(child);
		}
	}

	public void ValidComponents()
	{
		foreach (ValidItem item in m_listValid)
		{
			item.Reset();
		}
		Transform transform = m_root.transform;
		ValidComponents(transform);
		foreach (ValidItem item2 in m_listValid)
		{
			item2.ValidProcess();
			if (!item2.isSuccess)
			{
				TryErrorMessage(m_currentName, item2.ToString());
				break;
			}
		}
	}

	public static bool ValidType(Component component, List<ValidItem> types, bool addToValidList = true)
	{
		bool result = false;
		Type type = component.GetType();
		for (int i = 0; i < types.Count; i++)
		{
			if (type.Name == types[i].type)
			{
				result = true;
				if (addToValidList)
				{
					types[i] = new ValidItem(types[i].type, types[i].min, types[i].max, types[i].validComponentProcess, types[i].current + 1, types[i].components);
				}
				types[i].components.Add(component);
				break;
			}
		}
		return result;
	}

	public static bool ValidType(Type type, List<string> types)
	{
		bool result = false;
		for (int i = 0; i < types.Count; i++)
		{
			if (type.Name == types[i])
			{
				result = true;
				break;
			}
		}
		return result;
	}
}
