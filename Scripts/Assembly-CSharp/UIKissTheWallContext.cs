using System.Collections;
using Binding;
using Binding.Base;
using DI;
using PrefsModels;
using UnityEngine;
using UnityEngine.UI;

public class UIKissTheWallContext : BehaviourBindingTarget
{
	private static UIKissTheWallContext m_current;

	private GlobalModel m_globals;

	private BoolProperty m_showKissTheWallHUD = new BoolProperty();

	private RectTransform m_rectTransform;

	private RectTransform m_parentRectTr;

	private StringProperty m_text = new StringProperty();

	private Text m_textAllignment;

	private Camera m_mainCamera;

	private Coroutine m_showDistanceCoroutine;

	private bool m_isShowing;

	private bool m_isHidden;

	private Vector3 m_cachedCarPoint;

	private Vector3 m_cachedWallPoint;

	private float m_cachedDist;

	private bool m_updatePosOnLateUpdate;

	private void Awake()
	{
		m_current = this;
		m_globals = DependencyInjector.Resolve<GamePrefs>().globals;
		m_globals.ShowKissTheWallChanged += OnShowKissTheWallChanged;
		m_showKissTheWallHUD.value = m_globals.showKissTheWallHUD;
		States.onStateActivate += OnStateActivated;
		m_rectTransform = GetComponent<RectTransform>();
		m_parentRectTr = m_rectTransform.parent as RectTransform;
		m_textAllignment = GetComponentInChildren<Text>();
		m_mainCamera = Camera.main;
		base.gameObject.SetActive(value: false);
	}

	private void OnDestroy()
	{
		m_current = null;
		m_globals.ShowKissTheWallChanged -= OnShowKissTheWallChanged;
		States.onStateActivate -= OnStateActivated;
	}

	private void OnDisable()
	{
		if (m_showDistanceCoroutine != null)
		{
			StopCoroutine(m_showDistanceCoroutine);
			m_showDistanceCoroutine = null;
		}
		if (base.gameObject.activeSelf)
		{
			base.gameObject.SetActive(value: false);
		}
	}

	private void UpdatePositionOnCanvas(Vector3 carPoint, Vector3 wallPoint, float distance)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle(m_parentRectTr, m_mainCamera.WorldToScreenPoint(carPoint), null, out var localPoint);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(m_parentRectTr, m_mainCamera.WorldToScreenPoint(wallPoint), null, out var localPoint2);
		bool flag = localPoint.x - localPoint2.x <= 0f;
		m_rectTransform.pivot = new Vector2(flag ? 0f : 1f, m_rectTransform.pivot.y);
		m_rectTransform.anchoredPosition = localPoint;
		m_rectTransform.SetWidth(Mathf.Abs(localPoint.x - localPoint2.x));
		m_text.value = distance.ToString("F1");
		m_textAllignment.alignment = (flag ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
	}

	private void LateUpdate()
	{
		if (m_updatePosOnLateUpdate)
		{
			UpdatePositionOnCanvas(m_cachedCarPoint, m_cachedWallPoint, m_cachedDist);
			m_updatePosOnLateUpdate = false;
		}
	}

	private void UpdatePositionRequset(Vector3 carPoint, Vector3 wallPoint, float distance)
	{
		m_cachedCarPoint = carPoint;
		m_cachedWallPoint = wallPoint;
		m_cachedDist = distance;
		m_updatePosOnLateUpdate = true;
	}

	public static void ShowDistanceToTheWall(Vector3 carPoint, Vector3 wallPoint, float distance)
	{
		if (!(m_current == null) && CameraSwitch.instance.isThirdPersonCamera && !m_current.m_isHidden && m_current.transform.parent.gameObject.activeInHierarchy)
		{
			m_current.UpdatePositionRequset(carPoint, wallPoint, distance);
			m_current.m_isShowing = true;
			if (m_current.m_showDistanceCoroutine == null)
			{
				m_current.gameObject.SetActive(value: true);
				m_current.m_showDistanceCoroutine = m_current.StartCoroutine(m_current.ShowCoroutine());
			}
		}
	}

	private IEnumerator ShowCoroutine()
	{
		while (m_isShowing)
		{
			m_isShowing = false;
			yield return null;
		}
		m_showDistanceCoroutine = null;
		m_current.gameObject.SetActive(value: false);
	}

	private void OnShowKissTheWallChanged(bool isShow)
	{
		m_showKissTheWallHUD.value = isShow;
	}

	private void OnStateActivated(BaseState state)
	{
		if (state.isPopupState)
		{
			m_isHidden = true;
			base.gameObject.SetActive(value: false);
			if (m_showDistanceCoroutine != null)
			{
				StopCoroutine(m_showDistanceCoroutine);
				m_showDistanceCoroutine = null;
			}
		}
		else
		{
			m_isHidden = false;
		}
	}
}
