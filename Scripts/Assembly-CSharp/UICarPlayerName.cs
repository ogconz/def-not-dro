using System;
using Binding;
using UnityEngine;

public class UICarPlayerName : UICarBasePlayerName
{
	private const float DefaultHeughtOffset = 0.1f;

	private float m_boundHeight;

	private GUICommonNickNames m_manager;

	private FloatProperty m_rootAlpha = new FloatProperty();

	private BoolProperty m_isShown = new BoolProperty(startValue: true);

	private BoolProperty m_badConnection = new BoolProperty(false);

	private bool m_badConnectionBool;

	private Canvas m_uiCanvas;

	private RectTransform m_uiCanvasRect;

	private Camera m_camera;

	[SerializeField]
	private NickNameDistanceConfig m_distanceConfig;

	public float dist { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		m_camera = Camera.main;
		m_uiCanvas = GetComponentInParent<Canvas>();
		m_uiCanvasRect = m_uiCanvas.GetComponent<RectTransform>();
		m_playerCarState = GetComponentInChildren<UIPlayerCarState>();
		XRModeManager instance = XRModeManager.instance;
		if (instance != null)
		{
			instance.OnCameraModeChanged = (Action<Camera>)Delegate.Combine(instance.OnCameraModeChanged, new Action<Camera>(ChangeCamera));
		}
	}

	private void ChangeCamera(Camera camera)
	{
		if (!(camera == null))
		{
			m_camera = camera;
		}
	}

	private void OnDestroy()
	{
		XRModeManager instance = XRModeManager.instance;
		if (instance != null)
		{
			instance.OnCameraModeChanged = (Action<Camera>)Delegate.Remove(instance.OnCameraModeChanged, new Action<Camera>(ChangeCamera));
		}
	}

	public void SetVisible(bool show)
	{
		m_isShown.value = show;
	}

	public override void Set(RaceCar host, string name)
	{
		SetNameLabelData(host, name);
		SetGUICommonNickNamesManager();
		CustomUpdate();
	}

	private void SetGUICommonNickNamesManager()
	{
		m_manager = GUICommonNickNames.instance;
	}

	public void CustomUpdate()
	{
		if (!m_host || !m_camera || !m_host.gameObject.activeSelf)
		{
			m_rootAlpha.value = 0f;
			return;
		}
		float num = m_manager.fadeStart * m_manager.fadeStart;
		float num2 = m_manager.fadeDist * m_manager.fadeDist;
		dist = Vector3.SqrMagnitude(m_host.transform.position - m_camera.transform.position);
		CheckConnectionInNames();
		float num3 = (1f - dist / (num + num2)) * m_manager.heightBiass;
		Vector3 vector = m_host.transform.position + (GetBoundHeight() + num3) * Vector3.up;
		Vector3 vector2 = m_camera.WorldToViewportPoint(vector);
		XRModeManager instance = XRModeManager.instance;
		if (instance != null && instance.isHMDWearing)
		{
			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(m_camera, vector);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(m_uiCanvasRect, screenPoint, m_camera, out var localPoint);
			base.transform.localPosition = localPoint;
		}
		else
		{
			base.transform.position = new Vector3(vector2.x * m_uiCanvas.pixelRect.width, vector2.y * m_uiCanvas.pixelRect.height, 0f);
		}
		if (vector2.z < 0f)
		{
			m_rootAlpha.value = 0f;
			return;
		}
		m_rootAlpha.value = m_distanceConfig.GetAlphaFromDistance(dist);
		float scaleFromDistance = m_distanceConfig.GetScaleFromDistance(dist);
		base.transform.localScale = new Vector3(scaleFromDistance, scaleFromDistance, scaleFromDistance);
		UpdatePlayerStatus();
	}

	public override void UpdatePlayerStatus()
	{
		base.UpdatePlayerStatus();
		if (m_time > 0f)
		{
			m_nickName.value = $"{m_playerName}({Mathf.CeilToInt(m_time)})";
		}
	}

	private void CheckConnectionInNames()
	{
		if (m_badConnectionBool != m_host.networkPlayer.BadConnection)
		{
			m_badConnectionBool = m_host.networkPlayer.BadConnection;
			m_badConnection.value = m_badConnectionBool;
		}
	}

	private float GetBoundHeight()
	{
		bool flag = m_boundHeight > 3f;
		if (m_boundHeight == 0f || flag)
		{
			m_boundHeight = m_host.carX.bounds.size.y + 0.1f;
		}
		return Mathf.Min(m_boundHeight, 3f);
	}
}
