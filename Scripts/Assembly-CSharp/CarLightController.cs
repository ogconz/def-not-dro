using System;
using System.Collections;
using System.Collections.Generic;
using CarModelSystem;
using CarModelSystem.Data;
using DI;
using UnityEngine;
using VLB;

public class CarLightController : IDayNightBehaviour
{
	private class OriginLightObject
	{
		public Light light;

		public VolumetricLightBeam lightVolume;

		public float volumeIntencity;

		public float lightIntencity;

		public float voltage;

		public GameObject gameObject
		{
			get
			{
				if (light != null)
				{
					return light.gameObject;
				}
				return null;
			}
		}

		public OriginLightObject(Light light)
		{
			this.light = light;
			lightIntencity = light.intensity;
			voltage = 1f;
			lightVolume = light.gameObject.GetComponent<VolumetricLightBeam>();
			if (lightVolume != null)
			{
				volumeIntencity = lightVolume.intensityGlobal;
			}
		}

		public void SetLightIntensity(float power)
		{
			light.intensity = lightIntencity * voltage * power;
			if (lightVolume != null)
			{
				lightVolume.intensityGlobal = volumeIntencity * power;
				lightVolume.UpdateAfterManualPropertyChange();
				if (lightVolume.intensityGlobal <= 0.01f && lightVolume.enabled)
				{
					lightVolume.enabled = false;
				}
				else if (!lightVolume.enabled)
				{
					lightVolume.enabled = true;
				}
			}
		}

		public static implicit operator Light(OriginLightObject origin)
		{
			return origin.light;
		}
	}

	private const float BRAKE_PRESS_LIGHT = 0.05f;

	private const int REVERSE_GEAR = -1;

	private const string FRONTLIGHT_ROOTPREFIX = "LightsFront_light";

	private static readonly int m_emissionIntensity = Shader.PropertyToID("_EmissionIntensity");

	[SerializeField]
	private Transform m_backLightsProjectorRoot;

	private GraphicsQuality m_graphicsQuality;

	private RaceCar m_car;

	private List<OriginLightObject> m_frontLights = new List<OriginLightObject>();

	private Material m_previousReplacedMaterial;

	private Material m_interiorMaterial;

	private GameObject m_backLightProjector;

	private Light m_tailLight;

	private bool m_forceDisabled;

	private bool m_interiorLightsEnabled;

	private bool m_prevLightActivity;

	private Coroutine m_blinkLightsCoroutine;

	private Transform[] m_frontLightParents;

	private Transform m_backLightParent;

	private bool m_isLightsBlinking => m_blinkLightsCoroutine != null;

	private bool m_carModelReady
	{
		get
		{
			if (m_car != null)
			{
				return m_car.carModel != null;
			}
			return false;
		}
	}

	private bool m_carEngineValid
	{
		get
		{
			if (!BaseCar.IsNull(m_car))
			{
				return m_car.engineController != null;
			}
			return false;
		}
	}

	private bool m_isIgnitionOn
	{
		get
		{
			if (m_carEngineValid)
			{
				return m_car.engineController.isIgnitionOn;
			}
			return false;
		}
	}

	private bool m_brakeLightsActivityCondition
	{
		get
		{
			if (m_isIgnitionOn)
			{
				return m_car.carX.brake > 0.05f;
			}
			return false;
		}
	}

	private bool m_reversLightsActivityCondition
	{
		get
		{
			if (m_isIgnitionOn)
			{
				return m_car.carX.gear == -1;
			}
			return false;
		}
	}

	private bool m_mainLightsActivityCondition
	{
		get
		{
			if (m_isIgnitionOn)
			{
				return m_car.headlightsEnabled;
			}
			return false;
		}
	}

	protected override void OnDestroy()
	{
		if (m_graphicsQuality != null)
		{
			GraphicsQuality graphicsQuality = m_graphicsQuality;
			graphicsQuality.onLightPowerChange = (Action)Delegate.Remove(graphicsQuality.onLightPowerChange, new Action(OnUpdateLightIntensity));
		}
		ReleaseLightStuff(releaseMateraial: true);
		base.OnDestroy();
	}

	public void SetForceDisabled(bool disabled)
	{
		m_forceDisabled = disabled;
	}

	public override void ChangeToDay()
	{
		if (m_carModelReady)
		{
			m_car.headlightsEnabled = false;
			SetFrontLightsActive(active: false);
			m_car.carModel.SetLightsState(enabled: false, CarLightGroup.Main, resetTimer: true);
			SetInteriorLight(dayFlag: true);
			SetBackLightFX(dayFlag: true);
		}
	}

	public override void ChangeToNight(bool force = false)
	{
		if (m_carModelReady)
		{
			force = force || States.mainCurrent is GarageGUIState;
			if (!force && m_car != null && !m_car.engineController.isIgnitionOn)
			{
				ChangeToDay();
				return;
			}
			m_car.headlightsEnabled = true;
			SetFrontLightsActive(active: true);
			m_car.carModel.SetLightsState(enabled: true, CarLightGroup.Main, resetTimer: true);
			SetInteriorLight(dayFlag: false);
			SetBackLightFX(dayFlag: false);
			OnUpdateLightIntensity();
		}
	}

	public void UseSourceParent(bool useSourceParent)
	{
		if (!(m_car == null) && !m_car.carModel.IsCarPartsLoading)
		{
			if (useSourceParent)
			{
				RestoreFrontLightParents();
				RestoreRearLightParents();
			}
			else
			{
				CacheFrontLightParents();
				CacheRearLightParent();
			}
		}
	}

	private void CacheFrontLightParents()
	{
		if (m_frontLightParents != null || m_frontLights.Count == 0)
		{
			return;
		}
		m_frontLightParents = new Transform[m_frontLights.Count];
		for (int i = 0; i < m_frontLights.Count; i++)
		{
			GameObject gameObject = m_frontLights[i].gameObject;
			if (gameObject != null)
			{
				m_frontLightParents[i] = gameObject.transform.parent;
				gameObject.transform.SetParent(m_car.transform, worldPositionStays: true);
			}
		}
	}

	private void RestoreFrontLightParents()
	{
		if (m_frontLightParents == null || m_frontLights.Count == 0)
		{
			return;
		}
		for (int i = 0; i < m_frontLights.Count; i++)
		{
			GameObject gameObject = m_frontLights[i].gameObject;
			if (gameObject != null)
			{
				gameObject.transform.SetParent(m_frontLightParents[i], worldPositionStays: true);
			}
		}
		m_frontLightParents = null;
	}

	private void CacheRearLightParent()
	{
		if (m_backLightParent == null)
		{
			m_backLightParent = m_backLightProjector.transform.parent;
			m_backLightProjector.transform.SetParent(m_car.transform, worldPositionStays: true);
		}
	}

	private void RestoreRearLightParents()
	{
		if (m_backLightParent != null)
		{
			m_backLightProjector.transform.SetParent(m_backLightParent, worldPositionStays: true);
			m_backLightParent = null;
		}
	}

	private void SetFrontLightsActive(bool active)
	{
		if (m_frontLights == null)
		{
			return;
		}
		for (int i = 0; i < m_frontLights.Count; i++)
		{
			if (m_frontLights[i].gameObject != null)
			{
				GameObject gameObject = m_frontLights[i].gameObject;
				if (gameObject != null && gameObject.activeSelf != active)
				{
					gameObject.SetActive(active);
				}
			}
		}
	}

	private float GetLightIntensity()
	{
		if (!BaseCar.IsNull(m_car))
		{
			if (!m_car.isNetworkCar && !m_car.isGhost && m_car.isLocalPlayer)
			{
				return m_car.prefs.graphics.lightPlayerPower;
			}
			return m_car.prefs.graphics.lightOpponentPower;
		}
		return 0f;
	}

	private void OnUpdateLightIntensity()
	{
		float lightIntensity = GetLightIntensity();
		foreach (OriginLightObject frontLight in m_frontLights)
		{
			if (!(frontLight.gameObject == null))
			{
				frontLight.SetLightIntensity(lightIntensity);
			}
		}
	}

	public void SetLightsBrightnessByVoltageScale(float voltageDropScale)
	{
		DayNightController instance = DayNightController.instance;
		if (instance == null)
		{
			return;
		}
		if ((bool)instance.FrontLightPrefab)
		{
			float lightIntensity = GetLightIntensity();
			foreach (OriginLightObject frontLight in m_frontLights)
			{
				if (!(frontLight.gameObject == null))
				{
					frontLight.voltage = voltageDropScale;
					frontLight.SetLightIntensity(lightIntensity);
				}
			}
		}
		if ((bool)instance.TailLight && m_tailLight != null)
		{
			m_tailLight.intensity = instance.TailLight.intensity * voltageDropScale;
		}
		if (m_interiorMaterial != null)
		{
			m_interiorMaterial.SetFloat(m_emissionIntensity, m_interiorLightsEnabled ? (CarModelConfig.config.interiorEmission * voltageDropScale) : 0f);
		}
		if (m_carEngineValid && (m_car.engineController.checkEngine || m_isLightsBlinking))
		{
			bool active = voltageDropScale < 1f && !m_isLightsBlinking;
			SetActiveBlinkingLightsState(active);
		}
		void SetActiveBlinkingLightsState(bool flag)
		{
			if (flag)
			{
				m_blinkLightsCoroutine = TimeController.StartNow(BlinkLightsAsync());
			}
			else
			{
				if (m_isLightsBlinking)
				{
					TimeController.StopNow(m_blinkLightsCoroutine);
					m_blinkLightsCoroutine = null;
				}
				UpdateCarLightsStates(includeMainGroup: true);
			}
		}
	}

	private IEnumerator BlinkLightsAsync()
	{
		m_prevLightActivity = true;
		while (m_carModelReady && m_isIgnitionOn)
		{
			CarModel carModel = m_car.carModel;
			if (m_mainLightsActivityCondition && m_car.headlightsEnabled)
			{
				carModel.SetLightsState(m_prevLightActivity, CarLightGroup.Main, resetTimer: true);
			}
			if (m_reversLightsActivityCondition)
			{
				carModel.SetLightsState(m_prevLightActivity, CarLightGroup.Reverse, resetTimer: true);
			}
			if (m_brakeLightsActivityCondition)
			{
				carModel.SetLightsState(m_prevLightActivity, CarLightGroup.Brake, resetTimer: true);
			}
			m_prevLightActivity = !m_prevLightActivity;
			yield return null;
		}
		SetLightsBrightnessByVoltageScale(1f);
	}

	public void UpdateCarLightsStates(bool includeMainGroup = false)
	{
		if (m_carModelReady)
		{
			CarModel carModel = m_car.carModel;
			carModel.SetLightsState(m_brakeLightsActivityCondition, CarLightGroup.Brake, m_brakeLightsActivityCondition && includeMainGroup);
			carModel.SetLightsState(m_reversLightsActivityCondition, CarLightGroup.Reverse, m_reversLightsActivityCondition && includeMainGroup);
			if (includeMainGroup)
			{
				carModel.SetLightsState(m_mainLightsActivityCondition, CarLightGroup.Main, m_mainLightsActivityCondition);
			}
		}
	}

	private void SetBackLightFX(bool dayFlag)
	{
		if (m_backLightProjector != null)
		{
			m_backLightProjector.SetActive(!dayFlag);
		}
	}

	private void SetInteriorLight(bool dayFlag)
	{
		if ((bool)m_interiorMaterial)
		{
			m_interiorMaterial.SetFloat(m_emissionIntensity, dayFlag ? 0f : CarModelConfig.config.interiorEmission);
			m_interiorLightsEnabled = !dayFlag;
		}
	}

	public void OnCarLoaded(RaceCar car)
	{
		if (m_forceDisabled)
		{
			return;
		}
		bool flag = m_car == null;
		if (!flag)
		{
			ReleaseLightStuff(releaseMateraial: false);
		}
		m_car = car;
		InitLightStuff();
		DayNightController instance = DayNightController.instance;
		if (instance == null)
		{
			return;
		}
		m_graphicsQuality = DependencyInjector.Resolve<GraphicsQuality>();
		if (m_graphicsQuality != null)
		{
			GraphicsQuality graphicsQuality = m_graphicsQuality;
			graphicsQuality.onLightPowerChange = (Action)Delegate.Combine(graphicsQuality.onLightPowerChange, new Action(OnUpdateLightIntensity));
		}
		if (instance.IsAppliedMode)
		{
			if (flag ? (!instance.IsDayLight) : m_car.headlightsEnabled)
			{
				ChangeToNight(force: true);
			}
			else
			{
				ChangeToDay();
			}
		}
	}

	private void ReleaseLightStuff(bool releaseMateraial)
	{
		m_frontLights.Clear();
		if (releaseMateraial && m_interiorMaterial != null)
		{
			m_previousReplacedMaterial = null;
			UnityEngine.Object.Destroy(m_interiorMaterial);
			m_interiorMaterial = null;
		}
		if ((bool)m_backLightProjector)
		{
			UnityEngine.Object.Destroy(m_backLightProjector);
			m_backLightProjector = null;
		}
	}

	private void InitLightStuff()
	{
		m_car.carModel.skeleton.GetRoots("LightsFront", out var rootList);
		int num = 0;
		if (rootList != null)
		{
			foreach (Transform item in rootList)
			{
				if (item != null)
				{
					num += AllocateLights(item);
				}
			}
		}
		InitInteriorLightStuff();
		InitBackLightStuff();
	}

	private void InitBackLightStuff()
	{
		if (!DayNightController.instance)
		{
			return;
		}
		Transform parent = m_car.carModel.transform;
		m_backLightProjector = UnityEngine.Object.Instantiate(DayNightController.instance.BackLightProjector, parent, worldPositionStays: false);
		m_tailLight = m_backLightProjector.GetComponent<Light>();
		if ((bool)m_tailLight)
		{
			m_tailLight.enabled = parent.FindChildDeph((Transform child) => child.CompareTag("RearLights"));
		}
		if (m_backLightsProjectorRoot != null)
		{
			m_backLightProjector.transform.position = m_backLightsProjectorRoot.position;
		}
	}

	private void InitInteriorLightStuff()
	{
		Transform transform = m_car.carModel.transform;
		MeshRenderer meshRenderer = null;
		MeshRenderer[] componentsInChildren = transform.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer meshRenderer2 in componentsInChildren)
		{
			string text = meshRenderer2.name.ToLower();
			if (text.Contains("interior") && text.Contains("h"))
			{
				meshRenderer = meshRenderer2;
				break;
			}
		}
		if ((bool)meshRenderer)
		{
			if (m_previousReplacedMaterial == null)
			{
				m_previousReplacedMaterial = meshRenderer.sharedMaterial;
			}
			if (m_interiorMaterial == null)
			{
				m_interiorMaterial = meshRenderer.material;
			}
			transform.ForEachChildrenComponents(delegate(MeshRenderer mr)
			{
				ReplaceMaterial(mr, m_previousReplacedMaterial, m_interiorMaterial);
			});
		}
	}

	private static void ReplaceMaterial(MeshRenderer meshRenderer, Material targetMaterial, Material newMaterial)
	{
		if (meshRenderer.sharedMaterial == targetMaterial)
		{
			meshRenderer.material = newMaterial;
		}
	}

	private int AllocateLights(Transform frRoot)
	{
		int num = 0;
		Transform[] componentsInChildren = frRoot.GetComponentsInChildren<Transform>();
		foreach (Transform transform in componentsInChildren)
		{
			if (!transform.name.StartsWith("LightsFront_light"))
			{
				continue;
			}
			Light[] array = ((transform.childCount != 0) ? transform.GetComponentsInChildren<Light>(includeInactive: true) : new Light[1] { CreateLight(transform) });
			for (int j = 0; j < array.Length; j++)
			{
				if ((bool)array[j])
				{
					m_frontLights.Add(new OriginLightObject(array[j]));
					array[j].gameObject.SetActive(value: false);
					num++;
				}
			}
		}
		return num;
	}

	private Light CreateLight(Transform root)
	{
		if ((bool)DayNightController.instance && DayNightController.instance.FrontLightPrefab != null)
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(DayNightController.instance.FrontLightPrefab.gameObject, root, worldPositionStays: false);
			if (!(gameObject != null))
			{
				return null;
			}
			return gameObject.GetComponent<Light>();
		}
		return null;
	}
}
