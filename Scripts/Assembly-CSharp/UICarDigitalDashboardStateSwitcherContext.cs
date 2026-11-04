using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using DI;
using UnityEngine;

public class UICarDigitalDashboardStateSwitcherContext : BaseBindingBehaviourTarget
{
	private const string KmH = "KM/H";

	private const string MH = "M/H";

	private const int SkipFramesCount = 3;

	[SerializeField]
	private List<GameObject> m_ignitionDependentObjects;

	[SerializeField]
	private List<GameObject> m_inverseIgnitionDependentObjects;

	private BoolProperty m_ignitionEnabled = new BoolProperty();

	private StringProperty m_speedUnit = new StringProperty();

	private GamePrefs m_prefs;

	private RaceCar m_raceCar;

	private bool m_prevSpeedUnit;

	private void Awake()
	{
		m_raceCar = GetComponentInParent<RaceCar>();
		m_prefs = DependencyInjector.Resolve<GamePrefs>();
	}

	private void OnEnable()
	{
		TrySetIgnitionChangedSubscription(subscribe: true);
		SetSpeedUnit(m_prefs.globals.speedKmh);
		SetIgnitionEnabled(m_raceCar.engineController.isIgnitionOn);
	}

	private void OnDisable()
	{
		TrySetIgnitionChangedSubscription(subscribe: false);
	}

	private void SetObjectsIgnition(bool active)
	{
		if (m_ignitionDependentObjects == null)
		{
			return;
		}
		m_ignitionEnabled.value = active;
		foreach (GameObject ignitionDependentObject in m_ignitionDependentObjects)
		{
			ignitionDependentObject.SetActive(active);
		}
		foreach (GameObject inverseIgnitionDependentObject in m_inverseIgnitionDependentObjects)
		{
			inverseIgnitionDependentObject.SetActive(!active);
		}
	}

	private void Update()
	{
		if (Time.frameCount % 3 == 0 && !(m_raceCar == null) && !(m_raceCar.carX == null))
		{
			bool speedKmh = m_prefs.globals.speedKmh;
			if (speedKmh != m_prevSpeedUnit)
			{
				SetSpeedUnit(speedKmh);
			}
		}
	}

	private void TrySetIgnitionChangedSubscription(bool subscribe)
	{
		if (!(m_raceCar == null))
		{
			if (subscribe)
			{
				CarEngineController engineController = m_raceCar.engineController;
				engineController.onIgnitionChanged = (Action<bool>)Delegate.Combine(engineController.onIgnitionChanged, new Action<bool>(SetObjectsIgnition));
			}
			else
			{
				CarEngineController engineController2 = m_raceCar.engineController;
				engineController2.onIgnitionChanged = (Action<bool>)Delegate.Remove(engineController2.onIgnitionChanged, new Action<bool>(SetObjectsIgnition));
			}
		}
	}

	private void SetSpeedUnit(bool speedInKmh)
	{
		m_prevSpeedUnit = speedInKmh;
		m_speedUnit.value = (speedInKmh ? "KM/H" : "M/H");
	}

	private void SetIgnitionEnabled(bool isEnabled)
	{
		m_ignitionEnabled.value = isEnabled;
	}
}
