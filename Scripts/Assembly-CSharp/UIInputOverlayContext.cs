using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarX;
using DI;
using GameInput;
using PrefsModels;
using Rewired;
using UnityEngine;

public class UIInputOverlayContext : BaseBindingBehaviourTarget
{
	private const string NO_INPUT_VAL = "X";

	[Dependency]
	protected GamePrefs m_prefs;

	protected GlobalModel m_globals;

	public FloatProperty steerLI = new FloatProperty();

	public FloatProperty steerRI = new FloatProperty();

	public FloatProperty steer = new FloatProperty();

	public FloatProperty momentLI = new FloatProperty();

	public FloatProperty momentRI = new FloatProperty();

	public BoolProperty momentVisible = new BoolProperty();

	public StringProperty steerCtrlID = new StringProperty();

	public StringProperty fxCtrlID = new StringProperty();

	public FloatProperty clutchI = new FloatProperty();

	public FloatProperty clutch = new FloatProperty();

	public FloatProperty brakeI = new FloatProperty();

	public FloatProperty brake = new FloatProperty();

	public FloatProperty accelI = new FloatProperty();

	public FloatProperty accel = new FloatProperty();

	public FloatProperty handbrakeI = new FloatProperty();

	public FloatProperty handbrake = new FloatProperty();

	private BoolProperty m_showInputWhiteHandle = new BoolProperty(startValue: true);

	private RaceCar m_car;

	public static UIInputOverlayContext instance { get; private set; }

	protected override void OnActivate()
	{
		base.OnActivate();
		if (m_prefs.globals.pedalsIndicator)
		{
			m_car = PlayerCarControl.instance.car;
			if ((bool)m_car)
			{
				GameConsole.SetCmd("r_input", delegate
				{
					int.TryParse(GameConsole.cmdArgs[1], out var result);
					momentVisible.value = result == 11;
				});
				if (!base.gameObject.activeInHierarchy)
				{
					base.owner.Push<UIInputOverlayContext>();
				}
			}
		}
		else
		{
			Hide();
		}
	}

	protected override void OnDeactivate()
	{
		SetCar();
		base.OnDeactivate();
	}

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
	}

	protected override void OnDestroy()
	{
		if (instance == this)
		{
			instance = null;
		}
		base.OnDestroy();
	}

	public void SetCar(RaceCar car = null, bool showInputHandle = true)
	{
		m_car = car;
		m_showInputWhiteHandle.value = showInputHandle;
	}

	protected override bool OnEscape()
	{
		return false;
	}

	public void LateUpdate()
	{
		PlayerCarControl playerCarControl = PlayerCarControl.instance;
		if (playerCarControl == null)
		{
			return;
		}
		if (!m_car)
		{
			m_car = playerCarControl.car;
		}
		if (!m_car)
		{
			return;
		}
		Car carX = m_car.carX;
		if (!carX)
		{
			return;
		}
		GameInput.InputManager inputManager = GameInput.InputManager.instance;
		if (inputManager == null)
		{
			return;
		}
		steer.value = inputManager.GetActionValue(ActionType.Turn);
		if (momentVisible.value)
		{
			clutch.value = inputManager.GetActionValue(ActionType.Clutch);
			brake.value = inputManager.GetActionValue(ActionType.Brake);
			accel.value = inputManager.GetActionValue(ActionType.Accelerate);
			handbrake.value = inputManager.GetActionValue(ActionType.HandBrake);
		}
		else
		{
			clutch.value = carX.clutch;
			brake.value = carX.brake;
			accel.value = carX.accelerate;
			handbrake.value = carX.handbrake;
		}
		steerLI.value = Mathf.Max(0f, carX.GetSteer());
		steerRI.value = 0f - Mathf.Min(0f, carX.GetSteer());
		clutchI.value = carX.clutch;
		brakeI.value = carX.brake;
		accelI.value = carX.accelerate;
		handbrakeI.value = carX.handbrake;
		if (!momentVisible.value)
		{
			return;
		}
		momentLI.value = Mathf.Max(0f, carX.steerAlignMom);
		momentRI.value = 0f - Mathf.Min(0f, carX.steerAlignMom);
		ReInput.ControllerHelper controllers = ReInput.controllers;
		if (controllers != null)
		{
			IList<Controller> controllers2 = controllers.Controllers;
			if (controllers2 != null)
			{
				int num = controllers2.IndexOf(inputManager.CurSteeringDevice?.rewiredController);
				steerCtrlID.value = ((num >= 0) ? UIUtils.NumToString((uint)num) : "X");
				int num2 = controllers2.IndexOf(inputManager.CurFXDevice?.rewiredController);
				fxCtrlID.value = ((num2 >= 0) ? UIUtils.NumToString((uint)num2) : "X");
			}
		}
	}
}
