using System;
using System.Runtime.InteropServices;
using CarX;
using UnityEngine;
using UnityEngine.Rendering;

namespace NVIDIA
{
	public class Ansel : MonoBehaviour
	{
		public struct ConfigData
		{
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public float[] forward;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public float[] up;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public float[] right;

			public float translationalSpeedInWorldUnitsPerSecond;

			public float rotationalSpeedInDegreesPerSecond;

			public uint captureLatency;

			public uint captureSettleLatency;

			public float metersInWorldUnit;

			[MarshalAs(UnmanagedType.I1)]
			public bool isCameraOffcenteredProjectionSupported;

			[MarshalAs(UnmanagedType.I1)]
			public bool isCameraTranslationSupported;

			[MarshalAs(UnmanagedType.I1)]
			public bool isCameraRotationSupported;

			[MarshalAs(UnmanagedType.I1)]
			public bool isCameraFovSupported;
		}

		public struct CameraData
		{
			public float fov;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
			public float[] projectionOffset;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
			public float[] position;

			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public float[] rotation;
		}

		public struct SessionData
		{
			[MarshalAs(UnmanagedType.I1)]
			public bool isAnselAllowed;

			[MarshalAs(UnmanagedType.I1)]
			public bool is360MonoAllowed;

			[MarshalAs(UnmanagedType.I1)]
			public bool is360StereoAllowed;

			[MarshalAs(UnmanagedType.I1)]
			public bool isFovChangeAllowed;

			[MarshalAs(UnmanagedType.I1)]
			public bool isHighresAllowed;

			[MarshalAs(UnmanagedType.I1)]
			public bool isPauseAllowed;

			[MarshalAs(UnmanagedType.I1)]
			public bool isRotationAllowed;

			[MarshalAs(UnmanagedType.I1)]
			public bool isTranslationAllowed;
		}

		public enum HintBufferType
		{
			kBufferTypeHDR = 0,
			kBufferTypeDepth = 1,
			kBufferTypeHUDless = 2,
			kBufferTypeCount = 3
		}

		public enum UserControlStatus
		{
			kUserControlOk = 0,
			kUserControlIdAlreadyExists = 1,
			kUserControlInvalidId = 2,
			kUserControlInvalidType = 3,
			kUserControlInvalidLabel = 4,
			kUserControlNameTooLong = 5,
			kUserControlInvalidValue = 6,
			kUserControlInvalidLocale = 7,
			kUserControlInvalidCallback = 8
		}

		[SerializeField]
		public float TranslationalSpeedInWorldUnitsPerSecond = 5f;

		[SerializeField]
		public float RotationalSpeedInDegreesPerSecond = 45f;

		[SerializeField]
		public uint CaptureLatency;

		[SerializeField]
		public uint CaptureSettleLatency = 10u;

		[SerializeField]
		public float MetersInWorldUnit = 1f;

		private const string PLUGIN_DLL = "AnselPlugin64";

		private static bool sessionActive;

		private static bool captureActive;

		private bool initialized;

		private Vector3 cameraPos;

		private Quaternion cameraRotation;

		private float cameraFOV;

		private BaseCamera activeCamera;

		private ConfigData anselConfig;

		private CameraData anselCam;

		private Matrix4x4 projectionMatrix;

		private Camera mainCam;

		private CommandBuffer[] hintBufferPreBindCBs;

		private CommandBuffer[] hintBufferPostBindCBs;

		public static bool IsSessionActive => sessionActive;

		public static bool IsCaptureActive => captureActive;

		public static bool IsAvailable => anselIsAvailable();

		private void Awake()
		{
			hintBufferPreBindCBs = new CommandBuffer[3];
			hintBufferPostBindCBs = new CommandBuffer[3];
			for (int i = 0; i < 3; i++)
			{
				hintBufferPreBindCBs[i] = new CommandBuffer();
				hintBufferPreBindCBs[i].IssuePluginEvent(GetMarkBufferPreBindRenderEventFunc(), i);
				hintBufferPostBindCBs[i] = new CommandBuffer();
				hintBufferPostBindCBs[i].IssuePluginEvent(GetMarkBufferPostBindRenderEventFunc(), i);
			}
		}

		public void Start()
		{
			Initialize();
		}

		public void OnDisable()
		{
			if (initialized)
			{
				SessionData ses = new SessionData
				{
					isAnselAllowed = false
				};
				anselConfigureSession(ref ses);
			}
		}

		private void Initialize()
		{
			if (initialized || !Application.isFocused)
			{
				return;
			}
			if (!IsAvailable)
			{
				Debug.LogError("Ansel is not available or enabled on this platform. Did you forget to whitelist your executable?");
				return;
			}
			mainCam = GetComponent<Camera>();
			anselConfig = default(ConfigData);
			anselConfig.right = new float[3] { 1f, 0f, 0f };
			anselConfig.up = new float[3] { 0f, 1f, 0f };
			anselConfig.forward = new float[3] { 0f, 0f, 1f };
			anselConfig.translationalSpeedInWorldUnitsPerSecond = TranslationalSpeedInWorldUnitsPerSecond;
			anselConfig.rotationalSpeedInDegreesPerSecond = RotationalSpeedInDegreesPerSecond;
			anselConfig.captureLatency = CaptureLatency;
			anselConfig.captureSettleLatency = CaptureSettleLatency;
			anselConfig.metersInWorldUnit = MetersInWorldUnit;
			anselConfig.isCameraOffcenteredProjectionSupported = true;
			anselConfig.isCameraRotationSupported = true;
			anselConfig.isCameraTranslationSupported = true;
			anselConfig.isCameraFovSupported = true;
			anselInit(ref anselConfig);
			anselCam = default(CameraData);
			SessionData ses = new SessionData
			{
				isAnselAllowed = true,
				isFovChangeAllowed = true,
				isHighresAllowed = true,
				isPauseAllowed = true,
				isRotationAllowed = true,
				isTranslationAllowed = true,
				is360StereoAllowed = true,
				is360MonoAllowed = true
			};
			anselConfigureSession(ref ses);
			if (!IsAvailable)
			{
				Debug.LogError("Ansel failed to configure session. Please check Ansel log for more details.");
			}
			else
			{
				MonoBehaviour.print("Ansel is initialized and ready to use");
			}
			initialized = true;
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus && !initialized)
			{
				Initialize();
			}
		}

		public void UpdateCoordinateSystem(Vector3 right, Vector3 up, Vector3 forward)
		{
			if (anselIsSessionOn())
			{
				Debug.LogError("Ansel coordinate system cannot be configured while session is active");
				return;
			}
			anselConfig.right = new float[3] { right.x, right.y, right.z };
			anselConfig.up = new float[3] { up.x, up.y, up.z };
			anselConfig.forward = new float[3] { forward.x, forward.y, forward.z };
			anselUpdateConfiguration(ref anselConfig);
		}

		public void ConfigureSession(SessionData ses)
		{
			if (!IsAvailable)
			{
				Debug.LogError("Ansel is not available or enabled on this platform. Did you forget to whitelist your executable?");
			}
			else if (anselIsSessionOn())
			{
				Debug.LogError("Ansel session cannot be configured while session is active");
			}
			else
			{
				anselConfigureSession(ref ses);
			}
		}

		public void OnPreRender()
		{
			if (anselIsSessionOn())
			{
				if (!sessionActive)
				{
					sessionActive = true;
					SaveState();
					MonoBehaviour.print("Started Ansel session");
				}
				Animator component = mainCam.GetComponent<Animator>();
				if ((bool)component)
				{
					component.enabled = false;
				}
				captureActive = anselIsCaptureOn();
				Transform transform = mainCam.transform;
				anselCam.fov = mainCam.fieldOfView;
				anselCam.projectionOffset = new float[2];
				anselCam.position = new float[3]
				{
					transform.position.x,
					transform.position.y,
					transform.position.z
				};
				anselCam.rotation = new float[4]
				{
					transform.rotation.x,
					transform.rotation.y,
					transform.rotation.z,
					transform.rotation.w
				};
				anselUpdateCamera(ref anselCam);
				mainCam.ResetProjectionMatrix();
				mainCam.transform.position = new Vector3(anselCam.position[0], anselCam.position[1], anselCam.position[2]);
				mainCam.transform.rotation = new Quaternion(anselCam.rotation[0], anselCam.rotation[1], anselCam.rotation[2], anselCam.rotation[3]);
				mainCam.fieldOfView = anselCam.fov;
				if (anselCam.projectionOffset[0] != 0f || anselCam.projectionOffset[1] != 0f)
				{
					projectionMatrix = mainCam.projectionMatrix;
					float num = -1f + anselCam.projectionOffset[0];
					float num2 = num + 2f;
					float num3 = -1f + anselCam.projectionOffset[1];
					float num4 = num3 + 2f;
					projectionMatrix[0, 2] = (num + num2) / (num2 - num);
					projectionMatrix[1, 2] = (num4 + num3) / (num4 - num3);
					mainCam.projectionMatrix = projectionMatrix;
				}
			}
			else if (sessionActive)
			{
				sessionActive = false;
				captureActive = false;
				RestoreState();
				MonoBehaviour.print("Stopped Ansel session");
				Animator component2 = mainCam.GetComponent<Animator>();
				if ((bool)component2)
				{
					component2.enabled = true;
				}
			}
		}

		private void SaveState()
		{
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;
			Input.ResetInputAxes();
			if (!(States.mainCurrent is SyncNetFreerideRaceModeState))
			{
				Transform transform = mainCam.transform;
				cameraPos = transform.position;
				cameraRotation = transform.rotation;
				cameraFOV = mainCam.fieldOfView;
			}
			KeepAliveManager.SetUIVisible(isVisible: false);
			BaseCamera[] array = UnityEngine.Object.FindObjectsOfType<BaseCamera>();
			foreach (BaseCamera baseCamera in array)
			{
				if (baseCamera.enabled)
				{
					baseCamera.enabled = false;
					activeCamera = baseCamera;
				}
			}
		}

		private void RestoreState()
		{
			Cursor.visible = true;
			Cursor.lockState = CursorLockMode.None;
			if (!(States.mainCurrent is SyncNetFreerideRaceModeState))
			{
				mainCam.ResetProjectionMatrix();
				mainCam.transform.position = cameraPos;
				mainCam.transform.rotation = cameraRotation;
				mainCam.fieldOfView = cameraFOV;
			}
			KeepAliveManager.SetUIVisible(isVisible: true);
			if ((bool)activeCamera)
			{
				activeCamera.enabled = true;
			}
		}

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern void anselInit(ref ConfigData conf);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern void anselUpdateConfiguration(ref ConfigData conf);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern void anselUpdateCamera(ref CameraData cam);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern void anselConfigureSession(ref SessionData ses);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern bool anselIsSessionOn();

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern bool anselIsCaptureOn();

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern bool anselIsAvailable();

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern void anselStartSession();

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern void anselStopSession();

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern IntPtr GetMarkBufferPreBindRenderEventFunc();

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern IntPtr GetMarkBufferPostBindRenderEventFunc();

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern UserControlStatus anselAddUserControlSlider(uint userControlId, string labelUtf8, float value);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern UserControlStatus anselSetUserControlSliderValue(uint userControlId, float value);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern float anselGetUserControlSliderValue(uint userControlId);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern UserControlStatus anselAddUserControlBoolean(uint userControlId, string labelUtf8, bool value);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern UserControlStatus anselSetUserControlBooleanValue(uint userControlId, bool value);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern bool anselGetUserControlBooleanValue(uint userControlId);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern UserControlStatus anselSetUserControlLabelLocalization(uint userControlId, string lang, string labelUtf8);

		[DllImport("AnselPlugin64", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		private static extern UserControlStatus anselRemoveUserControl(uint userControlId);
	}
}
