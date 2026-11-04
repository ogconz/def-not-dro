using System.Collections.Generic;
using Binding.Base;
using DI;
using GameInput;
using PrefsModels;
using UnityEngine;

public class UIFreeCamContext : BaseBindingBehaviourTarget
{
	public FreeCamerasList cameraList;

	public readonly Property<string> hintMakeCam = new Property<string>();

	public readonly Property<string> hintShift = new Property<string>("Shift");

	public readonly Property<string> hintMovement = new Property<string>();

	public readonly Property<string> hintView = new Property<string>();

	public readonly Property<string> hintHideUI = new Property<string>();

	private const float EQUAL_DISTANCE = 0.001f;

	[Dependency]
	private GamePrefs m_prefs;

	private CameraSwitch m_mainCamera;

	private CARXFollowCamera m_cameraFollow;

	private List<BaseBindingBehaviourTarget> m_contexts = new List<BaseBindingBehaviourTarget>();

	private (CameraSwitch.ECameraType, int) m_cachedCameraSelection;

	[SerializeField]
	private FreeCamerasManager m_camManagerPrefab;

	private FreeCamerasManager m_camManagerInstance;

	private int m_camIndex;

	public CameraSwitch mainCamera => m_mainCamera;

	private void OnEnable()
	{
		m_mainCamera = Object.FindObjectOfType<CameraSwitch>();
		if (m_mainCamera != null)
		{
			m_mainCamera.AttachUI(this);
			m_cameraFollow = m_mainCamera.GetComponent<CARXFollowCamera>();
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		InputManager inputManager = DependencyInjector.Resolve<InputManager>();
		hintMakeCam.value = "Space";
		hintShift.value = "Shift";
		hintMovement.value = "(" + inputManager.GetActionBinds(ActionType.Accelerate) + ", " + inputManager.GetActionBinds(ActionType.Brake) + ", " + inputManager.GetActionBinds(ActionType.Turn) + ")";
		hintView.value = inputManager.GetActionBinds(ActionType.FreeCamView);
		hintHideUI.value = inputManager.GetActionBinds(ActionType.HideUI);
		ShowBottomPanel(new ActionHintData[1]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = NavCancel
			}
		});
		LoadCameraPoints();
		CarXDboxManager.instance?.Stop();
	}

	public void LoadCameraPoints()
	{
		m_camIndex = 0;
		if (m_camManagerInstance == null)
		{
			m_camManagerInstance = Object.Instantiate(m_camManagerPrefab);
		}
		List<PrefsModelCustomCameras.CameraDescriptor> cameras = m_prefs.customCameras.GetCameras();
		if (cameras != null)
		{
			ClearCameraPoints();
			foreach (PrefsModelCustomCameras.CameraDescriptor item in cameras)
			{
				AddCameraPoint(item.position);
			}
		}
		cameraList.RecalcList();
	}

	protected override void OnDeactivate()
	{
		HideBottomPanel();
		m_prefs.customCameras.UpdateCameras(cameraList.items);
		m_camManagerInstance.ClearDeactivatedCameras();
		CarXDboxManager.instance?.Start();
		base.OnDeactivate();
	}

	private void FixedUpdate()
	{
		InputManager instance = InputManager.instance;
		if (KeepAliveManager.isUIHidden && instance.IsActionPressed(ActionType.NavCancelBtnB))
		{
			KeepAliveManager.ToggleUIInRaceMode();
		}
	}

	public void SubscribeContextUI(BaseBindingBehaviourTarget context)
	{
		m_contexts.Add(context);
	}

	public void CacheRaceCameraSelection(CameraSwitch.ECameraType cameraType, int index)
	{
		m_cachedCameraSelection = (cameraType, index);
	}

	public void ShowUI()
	{
		if (!base.owner.Get<UIFreeCamContext>().isActive)
		{
			base.owner.Add<UIFreeCamContext>();
		}
		foreach (BaseBindingBehaviourTarget context in m_contexts)
		{
			context.Hide();
		}
	}

	public void HideUI()
	{
		if (base.owner.Get<UIFreeCamContext>().isActive)
		{
			base.owner.Hide<UIFreeCamContext>();
		}
		foreach (BaseBindingBehaviourTarget context in m_contexts)
		{
			if (!context.isActive)
			{
				context.owner.Add(context.GetType());
			}
		}
	}

	public void HideAndAttach()
	{
		if (m_mainCamera != null)
		{
			m_mainCamera.AttachCam();
		}
	}

	public void AddCameraPoint(Vector3 position, bool recalcList = true)
	{
		if (!(m_mainCamera == null) && !IsPositionDuplicatesInExistingCameras(position))
		{
			GameObject gameObject = m_camManagerInstance.InitCamera(position);
			string titleCamera = "Camera_" + (m_camIndex + 1);
			FreeCamItemViewModel freeCamItemViewModel = new FreeCamItemViewModel(this, titleCamera, m_camIndex, gameObject.transform.position, gameObject.transform);
			m_cameraFollow.AddCustomCameraPoint(freeCamItemViewModel.transformCamera);
			cameraList.AddItem(freeCamItemViewModel);
			cameraList.RecalcList();
			m_camIndex++;
		}
	}

	private bool IsPositionDuplicatesInExistingCameras(Vector3 position)
	{
		for (int i = 0; i < cameraList.GetCount(); i++)
		{
			Vector3 position2 = cameraList[i].transformCamera.position;
			if (ApproximatelyEqual(position, position2))
			{
				return true;
			}
		}
		return false;
	}

	private bool ApproximatelyEqual(Vector3 p1, Vector3 p2)
	{
		if (Vector3.Distance(p1, p2) < 0.001f)
		{
			return true;
		}
		return false;
	}

	public void AddCameraPoint(bool recalcList = true)
	{
		if (!(m_mainCamera == null))
		{
			AddCameraPoint(m_mainCamera.transform.position, recalcList);
		}
	}

	public void RemoveCameraPoint(FreeCamItemViewModel item)
	{
		m_cameraFollow.RemoveCustomCameraPoint(item.transformCamera);
		m_camManagerInstance.DestroyByIndex(item.index);
		cameraList.RemoveItem(cameraList.IndexOf(item));
	}

	public void RemoveCameraPoint()
	{
		cameraList.RemoveItem(cameraList.selectedIndex);
	}

	public void ClearCameraPoints()
	{
		cameraList.Clear();
		m_cameraFollow.ClearCustomCameraPoints();
	}

	public void OnClick(FreeCamItemViewModel item)
	{
	}

	public void NavCancel()
	{
		CameraSwitch.instance.AttachCam();
		CameraSwitch.instance.ActivateCamera(m_cachedCameraSelection.Item1, m_cachedCameraSelection.Item2);
	}
}
