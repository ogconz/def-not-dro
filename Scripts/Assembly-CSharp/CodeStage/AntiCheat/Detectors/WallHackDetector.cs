using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CodeStage.AntiCheat.Detectors
{
	[DisallowMultipleComponent]
	public class WallHackDetector : ActDetectorBase
	{
		private const string COMPONENT_NAME = "WallHack Detector";

		private const string SERVICE_CONTAINER_NAME = "[WH Detector Service]";

		private readonly Vector3 rigidPlayerVelocity = new Vector3(0f, 0f, 1f);

		internal static bool isRunning;

		[Tooltip("World position of the container for service objects within 3x3x3 cube (drawn as red wireframe cube in scene).")]
		public Vector3 spawnPosition;

		private int whLayer = -1;

		private GameObject serviceContainer;

		private Rigidbody rigidPlayer;

		private CharacterController charControllerPlayer;

		private float charControllerVelocity;

		private Dictionary<Action, Coroutine> m_coroutines = new Dictionary<Action, Coroutine>();

		public static WallHackDetector Instance { get; private set; }

		private static WallHackDetector GetOrCreateInstance
		{
			get
			{
				if (Instance == null)
				{
					WallHackDetector wallHackDetector = UnityEngine.Object.FindObjectOfType<WallHackDetector>();
					if (wallHackDetector != null)
					{
						Instance = wallHackDetector;
					}
					else
					{
						if (ActDetectorBase.detectorsContainer == null)
						{
							ActDetectorBase.detectorsContainer = new GameObject("Anti-Cheat Toolkit Detectors");
						}
						ActDetectorBase.detectorsContainer.AddComponent<WallHackDetector>();
					}
				}
				return Instance;
			}
		}

		public static void StartDetection(Action callback)
		{
			StartDetection(callback, GetOrCreateInstance.spawnPosition);
		}

		public static void StartDetection(Action callback, Vector3 servicePosition)
		{
			GetOrCreateInstance.StartDetectionInternal(callback, servicePosition);
		}

		public static void StopDetection()
		{
			if (Instance != null)
			{
				Instance.StopDetectionInternal();
			}
		}

		public static void Dispose()
		{
			if (Instance != null)
			{
				Instance.DisposeInternal();
			}
		}

		private WallHackDetector()
		{
		}

		private void Awake()
		{
			if (Init(Instance, "WallHack Detector"))
			{
				Instance = this;
			}
		}

		private void StartDetectionInternal(Action callback, Vector3 servicePosition)
		{
			if (isRunning)
			{
				Debug.LogWarning("[ACTk] WallHack Detector already running!");
				return;
			}
			if (!base.enabled)
			{
				Debug.LogWarning("[ACTk] WallHack Detector disabled but StartDetection still called from somewhere!");
				return;
			}
			onDetection = callback;
			spawnPosition = servicePosition;
			InitDetector();
			isRunning = true;
		}

		protected override void StopDetectionInternal()
		{
			if (isRunning)
			{
				UninitDetector();
				onDetection = null;
				isRunning = false;
			}
		}

		protected override void PauseDetector()
		{
			if (isRunning)
			{
				isRunning = false;
				StopRigidModule();
				StopControllerModule();
			}
		}

		protected override void ResumeDetector()
		{
			isRunning = true;
			StartRigidModule();
			StartControllerModule();
		}

		protected override void DisposeInternal()
		{
			base.DisposeInternal();
			if (Instance == this)
			{
				Instance = null;
			}
		}

		private void InitDetector()
		{
			InitCommon();
			InitRigidModule();
			InitControllerModule();
			StartRigidModule();
			StartControllerModule();
		}

		private void UninitDetector()
		{
			isRunning = false;
			StopRigidModule();
			StopControllerModule();
			UnityEngine.Object.Destroy(serviceContainer);
		}

		private void InitCommon()
		{
			if (whLayer == -1)
			{
				whLayer = LayerMask.NameToLayer("Ignore Raycast");
			}
			serviceContainer = new GameObject("[WH Detector Service]");
			serviceContainer.layer = whLayer;
			serviceContainer.transform.position = spawnPosition;
			UnityEngine.Object.DontDestroyOnLoad(serviceContainer);
			GameObject obj = new GameObject("Wall");
			obj.AddComponent<BoxCollider>();
			obj.layer = whLayer;
			obj.transform.parent = serviceContainer.transform;
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localScale = new Vector3(3f, 3f, 0.5f);
		}

		private void InitRigidModule()
		{
			GameObject gameObject = new GameObject("RigidPlayer");
			gameObject.AddComponent<CapsuleCollider>().height = 2f;
			gameObject.layer = whLayer;
			gameObject.transform.parent = serviceContainer.transform;
			gameObject.transform.localPosition = new Vector3(0.75f, 0f, -1f);
			rigidPlayer = gameObject.AddComponent<Rigidbody>();
			rigidPlayer.useGravity = false;
		}

		private void InitControllerModule()
		{
			GameObject gameObject = new GameObject("ControlledPlayer");
			gameObject.AddComponent<CapsuleCollider>().height = 2f;
			gameObject.layer = whLayer;
			gameObject.transform.parent = serviceContainer.transform;
			gameObject.transform.localPosition = new Vector3(-0.75f, 0f, -1f);
			charControllerPlayer = gameObject.AddComponent<CharacterController>();
		}

		private void StartRigidModule()
		{
			rigidPlayer.rotation = Quaternion.identity;
			rigidPlayer.angularVelocity = Vector3.zero;
			rigidPlayer.transform.localPosition = new Vector3(0.75f, 0f, -1f);
			rigidPlayer.velocity = rigidPlayerVelocity;
			Invoke(StartRigidModule, 4f);
		}

		private void StopRigidModule()
		{
			rigidPlayer.velocity = Vector3.zero;
			CancelInvoke(StartRigidModule);
		}

		private void StartControllerModule()
		{
			charControllerPlayer.transform.localPosition = new Vector3(-0.75f, 0f, -1f);
			charControllerVelocity = 0.01f;
			Invoke(StartControllerModule, 4f);
		}

		private void StopControllerModule()
		{
			charControllerVelocity = 0f;
			CancelInvoke(StartControllerModule);
		}

		private void FixedUpdate()
		{
			if (isRunning && rigidPlayer.transform.localPosition.z > 1f)
			{
				StopRigidModule();
				Detect();
			}
		}

		private void Update()
		{
			if (isRunning && charControllerVelocity > 0f)
			{
				charControllerPlayer.Move(new Vector3(UnityEngine.Random.Range(-0.002f, 0.002f), 0f, charControllerVelocity));
				if (charControllerPlayer.transform.localPosition.z > 1f)
				{
					StopControllerModule();
					Detect();
				}
			}
		}

		private void Detect()
		{
			if (onDetection != null)
			{
				onDetection();
			}
			if (autoDispose)
			{
				Dispose();
			}
			else
			{
				StopDetection();
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.red;
			Gizmos.DrawWireCube(spawnPosition, new Vector3(3f, 3f, 3f));
		}

		public void Invoke(Action action, float time)
		{
			Coroutine value = StartCoroutine(InvokeCoroutine(action, time));
			m_coroutines.Add(action, value);
		}

		public void InvokeRepeating(Action action, float time, float repeatRate)
		{
			Coroutine value = StartCoroutine(InvokeRepeatingCoroutine(action, time, repeatRate));
			m_coroutines.Add(action, value);
		}

		public void CancelInvoke(Action action)
		{
			if (m_coroutines.TryGetValue(action, out var value))
			{
				m_coroutines.Remove(action);
				StopCoroutine(value);
			}
		}

		public new void CancelInvoke()
		{
			foreach (Coroutine value in m_coroutines.Values)
			{
				StopCoroutine(value);
			}
			m_coroutines.Clear();
		}

		private IEnumerator InvokeCoroutine(Action action, float time)
		{
			yield return new WaitForSeconds(time);
			action.Call();
			m_coroutines.Remove(action);
		}

		private IEnumerator InvokeRepeatingCoroutine(Action action, float time, float repeatRate)
		{
			yield return new WaitForSeconds(time);
			while (true)
			{
				action.Call();
				yield return new WaitForSeconds(repeatRate);
			}
		}
	}
}
