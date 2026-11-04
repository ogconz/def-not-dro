using System;
using System.Collections;
using System.Collections.Generic;
using HUD;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class XRCanvasChanger : MonoBehaviour
{
	private struct UIRaceElemParam
	{
		public MonoBehaviour ownerContext;

		public Vector3 cachedCanvasPosition;

		public GameObject originGameObject;
	}

	private enum UIRaceElem
	{
		Speedometer = 0,
		Boostgauge = 1,
		Input = 2,
		Minimap = 3,
		MultiplayerButtonPanel = 4,
		Chat = 5
	}

	private enum SteeringWheelSide
	{
		Left = 0,
		Right = 1,
		None = 2
	}

	private enum CanvasVisibility
	{
		BlackBackground = 0,
		OnTop = 1,
		XRRoom = 2
	}

	public const int RENDER_TEXTURE_DEFAULT_WIDTH = 1920;

	public const int RENDER_TEXTURE_DEFAULT_HEIGHT = 1080;

	private const string BASE_CAM_CUSTOM_PASS_XR_ROOM = "SpecialXRRoomPass";

	private const float CANVAS_PLANE_DISTANCE = 5f;

	[SerializeField]
	private Transform m_UIGUITransform;

	[SerializeField]
	private Canvas m_canvasRoot;

	[SerializeField]
	private Canvas m_canvasRootGodVoice;

	[SerializeField]
	private RenderTexture m_legacyCamRenderTexture;

	private RectTransform m_canvasRectTransform;

	private Transform m_currCameraTransform;

	private Transform m_XRTrackableTransform;

	private Camera m_baseCamera;

	private bool m_needHandlePrecombatContext;

	private bool m_contextsIsOnMoveToWorldSpace;

	private bool m_UIRaceElemetsIsOnMoveToWorldSpace;

	private GameObject m_UICarRaceElementsContainer;

	private const float WORLD_SPACE_SCALE_FACTOR_CONTEXT = 0.0005f;

	private IReadOnlyDictionary<UIRaceElem, Vector4> m_worldSpaceUIElementsRectOffets = new Dictionary<UIRaceElem, Vector4>
	{
		{
			UIRaceElem.Speedometer,
			new Vector4(-360f, 490f, 300f, 0.75f)
		},
		{
			UIRaceElem.Boostgauge,
			new Vector4(-230f, 580f, 300f, 0.75f)
		},
		{
			UIRaceElem.Input,
			new Vector4(-490f, 440f, 330f, 0.75f)
		},
		{
			UIRaceElem.Minimap,
			new Vector4(400f, 485f, 300f, 0.75f)
		},
		{
			UIRaceElem.MultiplayerButtonPanel,
			new Vector4(275f, 450f, 330f, 0.75f)
		},
		{
			UIRaceElem.Chat,
			new Vector4(100f, 100f, 100f, 0.5f)
		}
	};

	private Dictionary<UIRaceElem, UIRaceElemParam> UIWorldSpaceElements = new Dictionary<UIRaceElem, UIRaceElemParam>();

	private RaceCar m_curRaceCar;

	private SteeringWheelSide m_lastCarSteeringWheelSide = SteeringWheelSide.None;

	private int? cachedXRCullingMaskRaceMode;

	public Transform canvasRootTransform => m_canvasRoot.transform;

	public RenderMode currentRenderMode => m_canvasRoot.renderMode;

	public void ConfigureXRRoomCanvas(Camera XRCamera, Camera baseCamera)
	{
		if (States.mainCurrent as BaseRaceState != null && !cachedXRCullingMaskRaceMode.HasValue)
		{
			cachedXRCullingMaskRaceMode = XRCamera.cullingMask;
		}
		m_baseCamera = baseCamera;
		m_currCameraTransform = XRCamera.transform;
		ChangeRootCanvasRenderMode(RenderMode.ScreenSpaceCamera, baseCamera);
		int num = 1 << LayerMask.NameToLayer("UI");
		int cullingMask = 1 << LayerMask.NameToLayer("XRRoom");
		XRCamera.cullingMask = 0;
		XRCamera.cullingMask = cullingMask;
		m_canvasRoot.planeDistance = 5f;
		m_canvasRootGodVoice.planeDistance = 5f;
		m_canvasRoot.sortingOrder = 1;
		m_canvasRootGodVoice.sortingOrder = 2;
		baseCamera.cullingMask &= ~num;
		TryResetToDefaultRenderTextureResolution();
		baseCamera.targetTexture = m_legacyCamRenderTexture;
		TrySetCanvasVisibility(CanvasVisibility.XRRoom);
	}

	private void TrySetActiveBaseCameraXRRoomCustomPass(bool needActivate)
	{
		if (!(m_baseCamera == null) && m_baseCamera.gameObject.TryGetComponent<CustomPassVolume>(out var component))
		{
			CustomPass customPass = component.customPasses.Find((CustomPass p) => p.name == "SpecialXRRoomPass");
			if (customPass != null)
			{
				customPass.enabled = needActivate;
			}
		}
	}

	public void ConfigureNonXRRoomCanvas(Camera XRCamera, Camera baseCamera)
	{
		m_baseCamera = baseCamera;
		m_currCameraTransform = baseCamera.transform;
		ChangeRootCanvasRenderMode(RenderMode.ScreenSpaceOverlay, baseCamera);
		baseCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("UI"));
		baseCamera.targetTexture = null;
		m_canvasRoot.sortingOrder = -1;
		m_canvasRootGodVoice.sortingOrder = -1;
		if (States.mainCurrent is BaseRaceState && cachedXRCullingMaskRaceMode.HasValue)
		{
			XRCamera.cullingMask = cachedXRCullingMaskRaceMode.Value;
			TrySetCanvasVisibility(CanvasVisibility.OnTop);
		}
	}

	public void LaunchWorldSpaceCanvas(Camera mainCamera, Camera eventCamera, Transform trackableTransform, bool needCheckPrecombatContext)
	{
		if (m_canvasRoot.renderMode != RenderMode.WorldSpace)
		{
			m_needHandlePrecombatContext = needCheckPrecombatContext;
			ChangeRootCanvasRenderMode(RenderMode.WorldSpace, eventCamera);
			m_currCameraTransform = mainCamera.transform;
			m_XRTrackableTransform = trackableTransform;
			if (!(CameraSwitch.instance == null))
			{
				m_curRaceCar = CameraSwitch.instance.GetCar();
				TimeController.StartNow(TryConfigureAndPlaceXRRaceCanvasOnCar(m_curRaceCar));
				BaseRaceState.onCarSwapped = (Action<RaceCar, RaceCar>)Delegate.Combine(BaseRaceState.onCarSwapped, new Action<RaceCar, RaceCar>(OnCarSwappedHandler));
			}
		}
	}

	private void OnCarSwappedHandler(RaceCar oldCar, RaceCar newCar)
	{
		TryConfigureAndPlaceCanvasBack();
		TimeController.StartNow(TryConfigureAndPlaceXRRaceCanvasOnCar(newCar));
	}

	private void ToggleUIWorldSpaceVisibility(CameraSwitch.ECameraType camera)
	{
	}

	private IEnumerator HandlePrecombatContextsByState()
	{
		UILoadingScreenContext loadingContext = XRModeManager.GetLoadingScreen();
		if (!(loadingContext == null))
		{
			yield return new WaitUntil(() => !loadingContext.isActive);
		}
	}

	public IEnumerator TryConfigureAndPlaceXRRaceCanvasOnCar(RaceCar raceCar)
	{
		if (!BaseCar.IsNull(raceCar))
		{
			m_contextsIsOnMoveToWorldSpace = true;
			yield return new WaitUntil(() => raceCar.steeringWheelRootTransform != null && raceCar.IsVisualLoaded && XRModeManager.instance.XRSubsystemActive);
			Transform steeringWheelRootTransform = raceCar.steeringWheelRootTransform;
			m_lastCarSteeringWheelSide = ((steeringWheelRootTransform.localPosition.x > 0f) ? SteeringWheelSide.Right : SteeringWheelSide.Left);
			ResetCanvasAndStrech();
			yield return null;
			TrySetCanvasVisibility(CanvasVisibility.BlackBackground);
			yield return TimeController.StartNow(PlaceWorldSpaceContextsInCar(raceCar));
			TrySetCanvasVisibility(CanvasVisibility.OnTop);
		}
	}

	private void ResetCanvasAndStrech()
	{
		m_canvasRoot.transform.localScale = Vector3.one;
		m_canvasRootGodVoice.transform.localScale = Vector3.one;
		m_canvasRoot.sortingOrder = -1;
		m_canvasRootGodVoice.sortingOrder = 2;
		RectTransform component = m_canvasRoot.GetComponent<RectTransform>();
		if (component != null)
		{
			component.SetWidth(2000f);
			component.SetHeight(1400f);
		}
		RectTransform component2 = m_canvasRootGodVoice.GetComponent<RectTransform>();
		if (component2 != null)
		{
			component2.SetWidth(2000f);
			component2.SetHeight(800f);
		}
	}

	public void LaunchScreenSpaceOverlayCanvas(Camera camera)
	{
		if (!(States.mainCurrent as BaseGameState == null))
		{
			m_currCameraTransform = camera.transform;
			TryConfigureAndPlaceCanvasBack();
			ChangeRootCanvasRenderMode(RenderMode.ScreenSpaceOverlay, camera);
			BaseRaceState.onCarSwapped = (Action<RaceCar, RaceCar>)Delegate.Remove(BaseRaceState.onCarSwapped, new Action<RaceCar, RaceCar>(OnCarSwappedHandler));
		}
	}

	private void TryConfigureAndPlaceCanvasBack()
	{
		m_canvasRoot.transform.SetParent(m_UIGUITransform);
		m_canvasRootGodVoice.transform.SetParent(m_UIGUITransform);
		foreach (UIRaceElem value in Enum.GetValues(typeof(UIRaceElem)))
		{
			ResetToOverlayUIRaceElem(value);
		}
		if (m_UICarRaceElementsContainer != null)
		{
			UnityEngine.Object.Destroy(m_UICarRaceElementsContainer);
		}
	}

	private IEnumerator PlaceWorldSpaceContextsInCar(RaceCar raceCar)
	{
		if (States.current is SyncNetFreerideRaceModeState || States.current is FreerideRaceModeState)
		{
			yield return TimeController.StartNow(HandlePrecombatContextsByState());
			m_needHandlePrecombatContext = false;
		}
		if (BaseCar.IsNull(raceCar))
		{
			yield break;
		}
		m_UICarRaceElementsContainer = new GameObject("UIWorldspaceContainer");
		m_UICarRaceElementsContainer.transform.SetParent(raceCar.transform);
		Vector3 position = raceCar.steeringWheelRootTransform.position;
		Vector3 vector = new Vector3(0f, m_XRTrackableTransform.position.y - position.y, 0f);
		GameObject gameObject = new GameObject("ContextContainer");
		Transform transform = gameObject.transform;
		transform.SetPositionAndRotation(position + vector / 2f, raceCar.carModel.transform.rotation);
		transform.SetParent(raceCar.steeringWheelRootTransform);
		m_canvasRoot.transform.SetPositionAndRotation(transform.position, transform.rotation);
		m_canvasRoot.transform.SetParent(gameObject.transform);
		m_canvasRootGodVoice.transform.SetPositionAndRotation(transform.position, transform.rotation);
		m_canvasRootGodVoice.transform.SetParent(gameObject.transform);
		transform.localScale *= 0.0005f;
		if (m_UICarRaceElementsContainer != null)
		{
			transform.transform.SetParent(m_UICarRaceElementsContainer.transform);
		}
		m_contextsIsOnMoveToWorldSpace = false;
		if (m_needHandlePrecombatContext)
		{
			yield return TimeController.StartNow(HandlePrecombatContextsByState());
			m_needHandlePrecombatContext = false;
		}
		UIWorldSpaceElements.Clear();
		m_UIRaceElemetsIsOnMoveToWorldSpace = true;
		foreach (UIRaceElem value in Enum.GetValues(typeof(UIRaceElem)))
		{
			ConfigureWorldSpaceUIRaceElemOnTargetTransform(value, raceCar.steeringWheelRootTransform);
			yield return null;
		}
		m_UIRaceElemetsIsOnMoveToWorldSpace = false;
	}

	private void TrySetCanvasVisibility(CanvasVisibility visibilityParam)
	{
		if (m_currCameraTransform == null)
		{
			return;
		}
		CustomPassVolume component = m_currCameraTransform.gameObject.GetComponent<CustomPassVolume>();
		if (component == null)
		{
			return;
		}
		foreach (CustomPass customPass2 in component.customPasses)
		{
			if (customPass2.enabled)
			{
				CustomPass customPass = customPass2;
				customPass.clearFlags = visibilityParam switch
				{
					CanvasVisibility.OnTop => ClearFlag.Depth, 
					CanvasVisibility.BlackBackground => ClearFlag.Color | ClearFlag.Depth, 
					CanvasVisibility.XRRoom => ClearFlag.None, 
					_ => ClearFlag.All, 
				};
				break;
			}
		}
		TrySetActiveBaseCameraXRRoomCustomPass(visibilityParam == CanvasVisibility.XRRoom);
	}

	private void ResetToOverlayUIRaceElem(UIRaceElem currentUIRaceElemType)
	{
		if (!(States.mainCurrent as BaseRaceState == null) && UIWorldSpaceElements.ContainsKey(currentUIRaceElemType))
		{
			UIRaceElemParam uIRaceElemParam = UIWorldSpaceElements[currentUIRaceElemType];
			MonoBehaviour ownerContext = uIRaceElemParam.ownerContext;
			GameObject originGameObject = uIRaceElemParam.originGameObject;
			Vector3 cachedCanvasPosition = uIRaceElemParam.cachedCanvasPosition;
			originGameObject.transform.SetParent(ownerContext.transform);
			originGameObject.transform.localScale = Vector3.one;
			originGameObject.transform.localRotation = Quaternion.Euler(Vector3.zero);
			originGameObject.GetComponent<RectTransform>().localPosition = cachedCanvasPosition;
			UIWorldSpaceElements.Remove(currentUIRaceElemType);
		}
	}

	private void ConfigureWorldSpaceUIRaceElemOnTargetTransform(UIRaceElem currentUIRaceElemType, Transform targetTrasform)
	{
		BaseRaceState baseRaceState = States.mainCurrent as BaseRaceState;
		if (baseRaceState == null)
		{
			return;
		}
		UIRaceModeContext raceContext = baseRaceState.raceContext;
		if (raceContext == null)
		{
			return;
		}
		UIRaceElemParam value = new UIRaceElemParam
		{
			ownerContext = currentUIRaceElemType switch
			{
				UIRaceElem.Speedometer => raceContext.speedometr, 
				UIRaceElem.Boostgauge => raceContext.boostGauge, 
				UIRaceElem.Minimap => baseRaceState.miniMapContext, 
				UIRaceElem.Input => UIInputOverlayContext.instance, 
				UIRaceElem.MultiplayerButtonPanel => raceContext.GetComponentInChildren<UIFreerideButtonsPanel>(), 
				_ => null, 
			}
		};
		if (value.ownerContext == null)
		{
			return;
		}
		Transform transform = null;
		if (currentUIRaceElemType != UIRaceElem.Chat)
		{
			transform = value.ownerContext.transform;
			value.originGameObject = transform.GetChild(0).gameObject;
		}
		if (!(transform == null) && transform.childCount != 0)
		{
			RectTransform component = value.originGameObject.GetComponent<RectTransform>();
			value.cachedCanvasPosition = component.localPosition;
			Vector4 vector = m_worldSpaceUIElementsRectOffets[currentUIRaceElemType];
			float w = vector.w;
			component.localScale = new Vector3(w, w, w);
			component.anchoredPosition3D = new Vector3(vector.x, vector.y, vector.z);
			if (currentUIRaceElemType == UIRaceElem.Input || currentUIRaceElemType == UIRaceElem.MultiplayerButtonPanel)
			{
				float y = m_XRTrackableTransform.position.y - targetTrasform.position.y;
				component.rotation = Quaternion.LookRotation(targetTrasform.position + new Vector3(0f, y, 0f) - m_XRTrackableTransform.position);
			}
			else
			{
				component.rotation = Quaternion.LookRotation(component.position - m_XRTrackableTransform.position);
			}
			UIWorldSpaceElements.Add(currentUIRaceElemType, value);
		}
	}

	private void ChangeRootCanvasRenderMode(RenderMode renderMode, Camera camera)
	{
		m_canvasRoot.renderMode = renderMode;
		m_canvasRootGodVoice.renderMode = renderMode;
		if (!(camera == null))
		{
			m_canvasRoot.worldCamera = camera;
			m_canvasRootGodVoice.worldCamera = camera;
		}
	}

	public static void GetActiveBaseDialogAndActivateOverride()
	{
		Canvas baseDialogCanvas = GetBaseDialogCanvas();
		if (!(baseDialogCanvas == null))
		{
			ForceCanvasOverride(baseDialogCanvas, needActivate: true);
		}
	}

	public static void GetActiveBaseDialogAndDisableOverride()
	{
		Canvas baseDialogCanvas = GetBaseDialogCanvas();
		if (!(baseDialogCanvas == null))
		{
			ForceCanvasOverride(baseDialogCanvas, needActivate: false);
		}
	}

	private static void ForceCanvasOverride(Canvas canvas, bool needActivate)
	{
		if (!(canvas == null))
		{
			canvas.overrideSorting = needActivate;
		}
	}

	private static Canvas GetBaseDialogCanvas()
	{
		BaseGameState baseGameState = States.current as BaseGameState;
		if (baseGameState == null)
		{
			return null;
		}
		ContextOwner contextOwner = baseGameState.contextOwner;
		if (contextOwner == null)
		{
			return null;
		}
		UIBaseDialogContext uIBaseDialogContext = contextOwner.Get<UIBaseDialogContext>();
		if (uIBaseDialogContext == null)
		{
			return null;
		}
		return uIBaseDialogContext.GetComponent<Canvas>();
	}

	public RectTransform GetRootRectTransform()
	{
		if (m_canvasRectTransform == null)
		{
			m_canvasRectTransform = m_canvasRoot.GetComponent<RectTransform>();
		}
		return m_canvasRectTransform;
	}

	public void GetRenderTextureResolution(out int width, out int height)
	{
		width = m_legacyCamRenderTexture.width;
		height = m_legacyCamRenderTexture.height;
	}

	public void SetRenderTextureResolution(int width, int height)
	{
		m_legacyCamRenderTexture.Release();
		m_legacyCamRenderTexture.width = width;
		m_legacyCamRenderTexture.height = height;
	}

	public void TryResetToDefaultRenderTextureResolution()
	{
		if (m_legacyCamRenderTexture.width != 1920 || m_legacyCamRenderTexture.height != 1080)
		{
			SetRenderTextureResolution(1920, 1080);
		}
	}

	private void OnApplicationQuit()
	{
		TryResetToDefaultRenderTextureResolution();
	}
}
