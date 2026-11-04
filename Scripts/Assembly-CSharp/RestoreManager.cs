using System;
using System.Collections.Generic;
using System.Diagnostics;
using AssetCore;
using UnityEngine;

public class RestoreManager : MonoBehaviour
{
	public enum RestoreState
	{
		Prepare = 0,
		FirstIteration = 1,
		SecondIteration = 2,
		LastIteration = 3,
		Complete = 4
	}

	[Serializable]
	public class CacheRestoreObject
	{
		public GameObject target;

		public Transform transform;

		public RestoreState state;

		public Vector3 srcPosition;

		public Quaternion srcRotation;

		public Vector3 srcScale;

		public Rigidbody body;

		public List<Renderer> renderers = new List<Renderer>();

		public List<Collider> colliders = new List<Collider>();

		public bool marked;

		public float timer;

		public ChangeLayerAfterFirstContact changerLayer;

		private bool m_lastRenderersFlag = true;

		public void SetColliders(bool flag)
		{
			foreach (Collider collider in colliders)
			{
				if (collider != null)
				{
					collider.enabled = flag;
				}
			}
		}

		public void SetRenderers(bool flag)
		{
			if (flag == m_lastRenderersFlag)
			{
				return;
			}
			foreach (Renderer renderer in renderers)
			{
				if (renderer != null)
				{
					renderer.enabled = flag;
				}
			}
			m_lastRenderersFlag = flag;
		}

		public void ResetLayer()
		{
			if ((bool)changerLayer)
			{
				changerLayer.SetLayer(0);
			}
		}

		public void CacheColliders()
		{
			target.GetComponentsInChildren(colliders);
			if (colliders.Count > 0 && changerLayer == null && colliders[0].gameObject.layer == 0)
			{
				changerLayer = target.AddComponent<ChangeLayerAfterFirstContact>();
				changerLayer.Init(colliders);
			}
		}

		public void CacheRenderers()
		{
			target.GetComponentsInChildren(renderers);
		}
	}

	private Map m_map;

	[SerializeField]
	private float m_maxTimerBlinking = 2f;

	[SerializeField]
	private float m_maxTimerAfterMoved = 5f;

	private List<CacheRestoreObject> m_observableObjects = new List<CacheRestoreObject>(256);

	private List<CacheRestoreObject> m_observableObjectsSubScene = new List<CacheRestoreObject>(256);

	private int m_observeIndex;

	private List<CacheRestoreObject> m_restoreObjects = new List<CacheRestoreObject>(32);

	private Stopwatch m_timer = new Stopwatch();

	private bool m_fastIndicator;

	private bool m_isSubSceneActivated;

	private float m_blinkingTimer;

	public static RestoreManager instance { get; private set; }

	private void Awake()
	{
		instance = this;
	}

	private void Update()
	{
		if (m_map == null || m_observableObjects.Count == 0)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		m_blinkingTimer += deltaTime;
		if (m_blinkingTimer > 0.25f)
		{
			m_blinkingTimer = 0f;
			m_fastIndicator = !m_fastIndicator;
		}
		if (!m_isSubSceneActivated && m_observableObjectsSubScene.Count > 0)
		{
			m_observableObjects.RemoveAll((CacheRestoreObject item) => m_observableObjectsSubScene.Contains(item));
			m_restoreObjects.RemoveAll((CacheRestoreObject item) => m_observableObjectsSubScene.Contains(item));
			m_observableObjectsSubScene.Clear();
			m_observeIndex = 0;
		}
		for (int num = m_restoreObjects.Count - 1; num >= 0; num--)
		{
			CacheRestoreObject cacheRestoreObject = m_restoreObjects[num];
			if (cacheRestoreObject.marked)
			{
				ProcessObject(cacheRestoreObject, deltaTime);
			}
		}
		ProcessMap();
	}

	public void Initialize(Map map)
	{
		if (!(map == null))
		{
			m_map = map;
			m_observeIndex = 0;
			PrepareCache();
		}
	}

	public void Clear(bool clearMap)
	{
		if (clearMap)
		{
			m_map = null;
		}
		m_observableObjects.Clear();
		m_restoreObjects.Clear();
	}

	private void PrepareCache()
	{
		if (m_map == null)
		{
			return;
		}
		Clear(clearMap: false);
		for (int i = 0; i < m_map.placementGroups.Length; i++)
		{
			Placement.PlacementGroup placementGroup = m_map.placementGroups[i];
			for (int j = 0; j < placementGroup.instances.Length; j++)
			{
				GameObject gameObject = placementGroup.instances[j];
				if (!(gameObject == null))
				{
					Vector3 position = placementGroup.positions[j];
					Quaternion rotation = placementGroup.rotations[j];
					Vector3 scale = placementGroup.scales[j];
					CacheRestoreObject cacheRestoreObject = CreateFrom(gameObject, position, rotation, scale);
					if (cacheRestoreObject != null)
					{
						m_observableObjects.Add(cacheRestoreObject);
					}
				}
			}
		}
	}

	public void PrepareCacheByPlacement(Placement placement)
	{
		if (placement == null || placement.placementGroups == null)
		{
			return;
		}
		for (int i = 0; i < placement.placementGroups.Length; i++)
		{
			Placement.PlacementGroup placementGroup = placement.placementGroups[i];
			if (placementGroup == null || placementGroup.instances == null)
			{
				continue;
			}
			for (int j = 0; j < placementGroup.instances.Length; j++)
			{
				GameObject gameObject = placementGroup.instances[j];
				if (!(gameObject == null))
				{
					Vector3 position = placementGroup.positions[j];
					Quaternion rotation = placementGroup.rotations[j];
					Vector3 scale = placementGroup.scales[j];
					CacheRestoreObject cacheRestoreObject = CreateFrom(gameObject, position, rotation, scale);
					if (cacheRestoreObject != null)
					{
						m_observableObjectsSubScene.Add(cacheRestoreObject);
					}
				}
			}
		}
		m_observableObjects.AddRange(m_observableObjectsSubScene);
		m_isSubSceneActivated = true;
	}

	public void ClearCachedPlacement()
	{
		m_isSubSceneActivated = false;
	}

	private CacheRestoreObject CreateFrom(GameObject instance, Vector3 position, Quaternion rotation, Vector3 scale)
	{
		Rigidbody component = instance.GetComponent<Rigidbody>();
		if (component == null)
		{
			return null;
		}
		CacheRestoreObject cacheRestoreObject = new CacheRestoreObject();
		cacheRestoreObject.srcPosition = position;
		cacheRestoreObject.srcRotation = rotation;
		cacheRestoreObject.srcScale = scale;
		cacheRestoreObject.target = instance;
		cacheRestoreObject.transform = instance.transform;
		cacheRestoreObject.body = component;
		cacheRestoreObject.CacheColliders();
		cacheRestoreObject.CacheRenderers();
		return cacheRestoreObject;
	}

	private void ProcessMap()
	{
		m_timer.Restart();
		int num = 0;
		int count = m_observableObjects.Count;
		int num2 = 50;
		int num3 = 400;
		bool flag = false;
		while (true)
		{
			if (!flag)
			{
				if (num > count || num > num3)
				{
					break;
				}
				if (--num2 <= 0)
				{
					num2 = 50;
					if (m_timer.ElapsedMilliseconds > 1)
					{
						break;
					}
				}
			}
			num++;
			m_observeIndex++;
			if (m_observeIndex >= count)
			{
				m_observeIndex = 0;
			}
			CacheRestoreObject cacheRestoreObject = m_observableObjects[m_observeIndex];
			if (!cacheRestoreObject.marked)
			{
				Vector3 localPosition = cacheRestoreObject.transform.localPosition;
				Vector3 srcPosition = cacheRestoreObject.srcPosition;
				if (!(Mathf.Abs(localPosition.x - srcPosition.x) < 0.01f) || !(Mathf.Abs(localPosition.y - srcPosition.y) < 0.01f))
				{
					cacheRestoreObject.timer = 0f;
					cacheRestoreObject.marked = true;
					cacheRestoreObject.state = RestoreState.Prepare;
					m_restoreObjects.Add(cacheRestoreObject);
				}
			}
		}
		m_timer.Stop();
	}

	private void SetupRigidBody(CacheRestoreObject cache)
	{
		Physics.SyncTransforms();
		Rigidbody body = cache.body;
		if (body != null)
		{
			float drag = body.drag;
			float angularDrag = body.angularDrag;
			float mass = body.mass;
			bool freezeRotation = body.freezeRotation;
			RigidbodyConstraints constraints = body.constraints;
			CollisionDetectionMode collisionDetectionMode = body.collisionDetectionMode;
			Vector3 centerOfMass = body.centerOfMass;
			RigidbodyInterpolation interpolation = body.interpolation;
			float maxAngularVelocity = body.maxAngularVelocity;
			UnityEngine.Object.DestroyImmediate(body);
			cache.body = cache.target.AddComponent<Rigidbody>();
			body = cache.body;
			body.drag = drag;
			body.angularDrag = angularDrag;
			body.mass = mass;
			body.freezeRotation = freezeRotation;
			body.constraints = constraints;
			body.collisionDetectionMode = collisionDetectionMode;
			body.centerOfMass = centerOfMass;
			body.interpolation = interpolation;
			body.maxAngularVelocity = maxAngularVelocity;
			body.Sleep();
		}
	}

	private void ProcessObject(CacheRestoreObject cache, float dt)
	{
		switch (cache.state)
		{
		case RestoreState.Prepare:
			PrepareIteration(cache, dt);
			break;
		case RestoreState.FirstIteration:
			FirstIteration(cache, dt);
			break;
		case RestoreState.SecondIteration:
			SecondIteration(cache, dt);
			break;
		case RestoreState.LastIteration:
			LastIteration(cache);
			break;
		case RestoreState.Complete:
			break;
		}
	}

	private void PrepareIteration(CacheRestoreObject cache, float dt)
	{
		cache.timer += dt;
		if (cache.timer > m_maxTimerAfterMoved)
		{
			if (cache.body != null)
			{
				cache.body.isKinematic = true;
				cache.body.useGravity = false;
			}
			cache.SetColliders(flag: false);
			cache.timer = 0f;
			cache.state = RestoreState.FirstIteration;
		}
	}

	private void FirstIteration(CacheRestoreObject cache, float dt)
	{
		cache.timer += dt;
		cache.SetRenderers(m_fastIndicator);
		if (cache.timer >= m_maxTimerBlinking)
		{
			Transform obj = cache.target.transform;
			obj.localPosition = cache.srcPosition;
			obj.localRotation = cache.srcRotation;
			obj.localScale = cache.srcScale;
			cache.state = RestoreState.SecondIteration;
			cache.timer = 0f;
		}
	}

	private void SecondIteration(CacheRestoreObject cache, float dt)
	{
		cache.timer += dt;
		if (cache.timer <= m_maxTimerBlinking)
		{
			cache.SetRenderers(m_fastIndicator);
			return;
		}
		cache.SetRenderers(flag: true);
		cache.state = RestoreState.LastIteration;
	}

	private void LastIteration(CacheRestoreObject cache)
	{
		if (!(cache.target == null))
		{
			cache.SetColliders(flag: true);
			cache.ResetLayer();
			if ((bool)cache.body)
			{
				cache.body.useGravity = true;
				cache.body.isKinematic = false;
				SetupRigidBody(cache);
			}
			cache.marked = false;
			cache.timer = 0f;
			m_restoreObjects.Remove(cache);
			cache.state = RestoreState.Complete;
		}
	}
}
