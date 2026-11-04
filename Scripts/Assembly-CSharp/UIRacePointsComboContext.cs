using System.Collections;
using Binding;
using Binding.Base;
using DI;
using PrefsModels;
using UnityEngine;

public class UIRacePointsComboContext : BehaviourBindingTarget
{
	private FloatProperty m_comboReadyProgress = new FloatProperty();

	private FloatProperty m_dpProgressVisibility = new FloatProperty(0f);

	private StringProperty m_driftProgress = new StringProperty();

	private FloatProperty m_comboMultipiler = new FloatProperty();

	private BoolProperty m_isComboUpEvent = new BoolProperty(false);

	private BoolProperty m_isComboMultipiler = new BoolProperty(false);

	private BoolProperty m_isPointsActive = new BoolProperty(startValue: true);

	private FloatProperty m_speedFill = new FloatProperty(0f);

	private DriftController m_driftController;

	private bool m_isShowingDP;

	private int m_currentDriftStartPoints;

	private int m_lastDriftPoints;

	private float m_fadeDelta;

	private float m_hideAddedDPAt;

	private bool m_showWrongWayWarning;

	private const float MinSpeed = 30f;

	private const float MaxSpeed = 105f;

	[HideInInspector]
	public bool IsAllowWrongWay;

	[SerializeField]
	private float m_speedFillBarMultiplier = 0.333f;

	private GlobalModel globals;

	private Coroutine m_corotine;

	public void Init(DriftController driftController)
	{
		m_driftController = driftController;
		if (globals == null)
		{
			globals = DependencyInjector.Resolve<GamePrefs>().globals;
		}
	}

	private void SetComboMultipiler(float value)
	{
		if (Mathf.Abs(m_comboMultipiler.value - value) < 0.01f)
		{
			m_isComboUpEvent.value = false;
			return;
		}
		m_isComboUpEvent.value = value > 1f && value > m_comboMultipiler.value;
		m_comboMultipiler.value = value;
		m_isComboMultipiler.value = value > 1f;
	}

	private void OnEnable()
	{
		if (m_dpProgressVisibility.value != 0f && m_dpProgressVisibility.value != 1f)
		{
			bool fadeIn = m_fadeDelta < 0f;
			FadeUI(fadeIn, force: false);
		}
	}

	private void OnDestroy()
	{
		if (m_driftController != null)
		{
			m_driftController.driftFailedEvent -= OnDriftFailedEventHandle;
			m_driftController.earnedPointsEvent -= OnDriftEarnedPointsEvent;
		}
	}

	private void Start()
	{
		m_isPointsActive.value = GameConsole.Bool["r_points"];
		if (m_driftController != null)
		{
			m_driftController.driftFailedEvent += OnDriftFailedEventHandle;
			m_driftController.earnedPointsEvent += OnDriftEarnedPointsEvent;
		}
		if (KeepAliveManager.isUIHidden)
		{
			FadeUI(fadeIn: false);
			m_isShowingDP = false;
		}
		UpdateDriftPointsUi(0);
		SetComboMultipiler(1f);
	}

	public void ResetAll()
	{
		ResetDp();
		m_lastDriftPoints = 0;
		m_fadeDelta = 0f;
		m_isShowingDP = false;
		FadeUI(fadeIn: false);
	}

	private void ResetDp()
	{
		m_driftProgress.value = "0";
	}

	private void OnDriftEarnedPointsEvent(int dp)
	{
		m_currentDriftStartPoints = m_lastDriftPoints;
	}

	private void UpdateDriftPointsUi(int dp)
	{
		int num = dp - m_lastDriftPoints;
		if (num < 1)
		{
			if (m_driftController.driftState == DriftController.DriftState.No)
			{
				if (KeepAliveManager.isUIHidden)
				{
					FadeUI(fadeIn: false);
					m_isShowingDP = false;
				}
				else if (m_isShowingDP && Time.time > m_hideAddedDPAt)
				{
					m_isShowingDP = false;
					FadeUI(fadeIn: false, force: false);
				}
			}
		}
		else
		{
			if (!m_isShowingDP)
			{
				if (m_driftController.isDrift && m_driftController.tandemLeader == null)
				{
					ResetDp();
				}
				m_currentDriftStartPoints = dp - num;
				m_isShowingDP = true;
				FadeUI(fadeIn: true, force: false);
			}
			int value = dp - m_currentDriftStartPoints;
			m_hideAddedDPAt = Time.time + 3f;
			m_driftProgress.value = UIUtils.DigitsWithSeparate(value);
		}
		m_lastDriftPoints = dp;
	}

	private bool IsWrongWayWarning()
	{
		if (!IsAllowWrongWay)
		{
			return false;
		}
		return m_showWrongWayWarning;
	}

	private void OnWrongWayChanged()
	{
		if (IsWrongWayWarning())
		{
			FadeUI(fadeIn: false);
		}
	}

	private void UpdateWrongWayWarning()
	{
		if (m_showWrongWayWarning != m_driftController.raceCar.wrongWay)
		{
			m_showWrongWayWarning = m_driftController.raceCar.wrongWay;
			OnWrongWayChanged();
		}
	}

	public void Refresh(int dp)
	{
		if (!(m_driftController == null))
		{
			UpdateWrongWayWarning();
			if (!IsWrongWayWarning())
			{
				UpdateDriftPointsUi(dp);
				m_comboReadyProgress.value = m_driftController.comboReadyProgress;
				m_speedFill.value = Mathf.InverseLerp(30f, 105f, m_driftController.driftSpeed) * m_speedFillBarMultiplier;
				SetComboMultipiler(m_driftController.comboMultipiler);
			}
		}
	}

	private void OnDriftFailedEventHandle()
	{
		ResetDp();
	}

	private void FadeUI(bool fadeIn, bool force = true)
	{
		if (m_corotine != null)
		{
			StopCoroutine(m_corotine);
			m_corotine = null;
		}
		if (force)
		{
			m_dpProgressVisibility.value = (fadeIn ? 1 : 0);
		}
		else
		{
			m_corotine = StartCoroutine(_FadeUI(fadeIn, fadeIn ? 3f : 2f));
		}
	}

	private IEnumerator _FadeUI(bool fadeIn, float fadeSpeed)
	{
		float alphaEnd = (fadeIn ? 1 : 0);
		while (m_dpProgressVisibility.value != alphaEnd)
		{
			float value = m_dpProgressVisibility.value;
			m_dpProgressVisibility.value = Mathf.MoveTowards(m_dpProgressVisibility.value, alphaEnd, fadeSpeed * Time.deltaTime);
			m_fadeDelta = value - m_dpProgressVisibility.value;
			yield return null;
		}
	}
}
