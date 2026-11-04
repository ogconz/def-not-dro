using System;
using Binding;
using Binding.Base;
using CarX;
using DI;
using PrefsModels;
using UnityEngine;

namespace HUD
{
	public class UIRearviewMirrorContext : BehaviourBindingTarget
	{
		[SerializeField]
		private Camera m_cameraPrefab;

		private Camera m_camera;

		private float m_cameraTargetFov = 60f;

		private RenderTexture m_renderTarget;

		private RaceCar m_raceCar;

		private GamePrefs m_prefs;

		public readonly StringProperty hintReceiver = new StringProperty();

		private BoolProperty m_visible = new BoolProperty(false);

		private TextureProperty m_mirror = new TextureProperty();

		private RectTransform m_rectTransform;

		private Camera m_uiCamera;

		private Canvas m_canvas;

		private Vector2 m_minSize = new Vector2(300f, 100f);

		private void ClearCamera()
		{
			if (m_camera != null)
			{
				UnityEngine.Object.Destroy(m_camera.gameObject);
				m_camera = null;
			}
		}

		private void SetupCamera()
		{
			ClearCamera();
			if (!(m_raceCar == null))
			{
				m_camera = UnityEngine.Object.Instantiate(m_cameraPrefab, m_raceCar.transform);
				m_camera.enabled = m_visible.value;
				ApplyRenderTarget();
				CarDesc desc = null;
				m_raceCar.GetComponent<Car>().GetCarDesc(ref desc);
				float y = desc.weight.bounds.size.y - desc.weight.bounds.size.y / 6f;
				Vector3 forward = -m_raceCar.transform.forward;
				m_camera.transform.rotation = Quaternion.LookRotation(forward);
				m_camera.transform.localPosition = new Vector3(0f, y, 0f - (m_raceCar.lengthRear + 0.2f));
			}
		}

		private void ApplyRenderTarget()
		{
			if ((bool)m_camera)
			{
				Rect rect = m_rectTransform.rect;
				int num = (int)rect.width;
				int num2 = (int)rect.height;
				if (m_renderTarget == null || m_renderTarget.width != num || m_renderTarget.height != num2)
				{
					RenderTexture.ReleaseTemporary(m_renderTarget);
					m_renderTarget = RenderTexture.GetTemporary(num, num2, 16, RenderTextureFormat.Default);
				}
				m_camera.targetTexture = m_renderTarget;
				m_mirror.value = m_renderTarget;
				UpdateCameraFov();
				m_camera.fieldOfView = m_cameraTargetFov;
			}
		}

		public void UpdateCameraFov()
		{
			Rect rect = m_rectTransform.rect;
			float width = rect.width;
			float b = ((Camera.main != null) ? Camera.main.fieldOfView : 60f);
			float num = Mathf.Lerp(GameConsole.Float["r_fovMirrorBase"], b, 1f - ((float)Screen.width - width) / ((float)Screen.width - 320f));
			float height = rect.height;
			float num2 = width / height;
			float num3 = 2f * Mathf.Atan(Mathf.Tan(num * (MathF.PI / 180f) / 2f) / num2);
			m_cameraTargetFov = Mathf.Abs(num3 * 57.29578f);
		}

		public void SetCar(RaceCar raceCar)
		{
			RaceCar raceCar2 = m_raceCar;
			m_raceCar = raceCar;
			if ((bool)raceCar2 && raceCar2 != raceCar)
			{
				SetupCamera();
			}
		}

		private void UpdateVisibility()
		{
			m_visible.value = m_prefs.globals.isRearviewMirrorVisible && !GameConsole.Bool["ChampionshipMode"];
			if (m_camera != null)
			{
				m_camera.enabled = m_visible.value;
			}
		}

		private void Initialize()
		{
			LoadInfo(m_prefs.globals.rearviewMirrorInfo);
		}

		private void Start()
		{
			m_prefs = DependencyInjector.Resolve<GamePrefs>();
			m_rectTransform = base.transform as RectTransform;
			m_canvas = m_rectTransform.GetComponentInParent<Canvas>().rootCanvas;
			m_uiCamera = m_canvas.worldCamera;
			if (m_uiCamera == null)
			{
				m_uiCamera = Camera.main;
			}
			GlobalModel globals = m_prefs.globals;
			globals.onRearviewMirrorChanged = (Action)Delegate.Combine(globals.onRearviewMirrorChanged, new Action(UpdateVisibility));
			UpdateVisibility();
			Initialize();
			SetupCamera();
			GameConsole.SetCmd("r_fovMirrorBase", delegate
			{
				UpdateCameraFov();
			});
		}

		private void OnDestroy()
		{
			if (m_prefs != null && m_prefs.globals != null)
			{
				GlobalModel globals = m_prefs.globals;
				globals.onRearviewMirrorChanged = (Action)Delegate.Remove(globals.onRearviewMirrorChanged, new Action(UpdateVisibility));
			}
			ClearCamera();
			RenderTexture.ReleaseTemporary(m_renderTarget);
		}

		protected void Update()
		{
			if ((bool)m_camera && m_visible.value && m_camera.fieldOfView != m_cameraTargetFov)
			{
				m_camera.fieldOfView = Mathf.Lerp(m_camera.fieldOfView, m_cameraTargetFov, Time.unscaledDeltaTime * 5f);
				if (Mathf.Abs(m_camera.fieldOfView - m_cameraTargetFov) < 0.1f)
				{
					m_camera.fieldOfView = m_cameraTargetFov;
				}
			}
		}

		public void SaveInfo()
		{
			GlobalModel.RearviewMirrorInfo rearviewMirrorInfo = new GlobalModel.RearviewMirrorInfo();
			if (m_uiCamera != null)
			{
				rearviewMirrorInfo.pos = m_uiCamera.ScreenToViewportPoint(m_rectTransform.localPosition);
				rearviewMirrorInfo.size = m_uiCamera.ScreenToViewportPoint(m_rectTransform.sizeDelta);
			}
			m_prefs.globals.rearviewMirrorInfo = rearviewMirrorInfo;
			m_prefs.globals.Save();
		}

		private void LoadInfo(GlobalModel.RearviewMirrorInfo info)
		{
			if (info == null || info.size == Vector2.zero)
			{
				m_rectTransform.sizeDelta = m_minSize;
				m_rectTransform.anchoredPosition = new Vector2(15f, -15f);
				SaveInfo();
			}
			else if (m_uiCamera != null)
			{
				m_rectTransform.localPosition = m_uiCamera.ViewportToScreenPoint(info.pos);
				m_rectTransform.sizeDelta = m_uiCamera.ViewportToScreenPoint(info.size);
			}
			ClampToWindow();
		}

		public void OnResizeOrMoveMirror()
		{
			ClampToWindow();
			ApplyRenderTarget();
		}

		private void ClampToWindow()
		{
			Rect rect = (m_rectTransform.parent as RectTransform).rect;
			Vector2 sizeDelta = m_rectTransform.sizeDelta;
			sizeDelta.x = Mathf.Clamp(sizeDelta.x, m_minSize.x, rect.width);
			sizeDelta.y = Mathf.Clamp(sizeDelta.y, m_minSize.y, rect.height);
			m_rectTransform.sizeDelta = sizeDelta;
			Vector3 localPosition = m_rectTransform.localPosition;
			Vector3 vector = rect.min - m_rectTransform.rect.min;
			Vector3 vector2 = rect.max - m_rectTransform.rect.max;
			localPosition.x = Mathf.Clamp(localPosition.x, vector.x, vector2.x);
			localPosition.y = Mathf.Clamp(localPosition.y, vector.y, vector2.y);
			m_rectTransform.localPosition = localPosition;
		}
	}
}
