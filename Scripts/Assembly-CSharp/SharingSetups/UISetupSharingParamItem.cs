using System;
using Binding;
using Binding.Base;
using UnityEngine;

namespace SharingSetups
{
	public class UISetupSharingParamItem : BaseBindingBehaviourTarget
	{
		private const string VIEW_FORMAT = "{0} {1}";

		private const string FORMAT = "N2";

		private StringProperty m_ctrlName = new StringProperty();

		private StringProperty m_currVal = new StringProperty();

		private StringProperty m_newVal = new StringProperty();

		private RaceCar m_userCar;

		private bool m_newParamDataInited;

		public string ctrlName { get; private set; }

		public string currVal { get; private set; }

		public string units { get; private set; }

		public DynoParamsItemViewModel dynoViewModel { get; private set; }

		private void OnUpdateHandler()
		{
			if (dynoViewModel.IsToeCamberItem() && m_newParamDataInited)
			{
				SetOverridedNewValue(dynoViewModel.controller.name);
			}
		}

		private void SetOverridedNewValue(string ctrlName)
		{
			if (m_userCar == null)
			{
				ClearNewVal();
				TimeController.onUpdate = (Action)Delegate.Remove(TimeController.onUpdate, new Action(OnUpdateHandler));
				return;
			}
			Vector3 eulerAngles = m_userCar.carModel.carWheels[0].hub.parent.localRotation.eulerAngles;
			Vector3 eulerAngles2 = m_userCar.carModel.carWheels[2].hub.parent.localRotation.eulerAngles;
			float num = ctrlName switch
			{
				"front_toe" => Mathf.DeltaAngle(0f, eulerAngles.y), 
				"rear_toe" => Mathf.DeltaAngle(0f, eulerAngles2.y), 
				"front_camber" => Mathf.DeltaAngle(0f, eulerAngles.z), 
				"rear_camber" => Mathf.DeltaAngle(0f, eulerAngles2.z), 
				_ => 0f, 
			};
			m_newVal.value = num.ToString("N2");
		}

		public void SetItem(string ctrlName, DynoParamsItemViewModel model, RaceCar ucerCar, Func<string, string> overrideCallback)
		{
			TimeController.onUpdate = (Action)Delegate.Remove(TimeController.onUpdate, new Action(OnUpdateHandler));
			TimeController.onUpdate = (Action)Delegate.Combine(TimeController.onUpdate, new Action(OnUpdateHandler));
			this.ctrlName = ctrlName;
			dynoViewModel = model;
			units = model.units;
			m_userCar = ucerCar;
			currVal = model.outputValue;
			if (model.IsToeCamberItem())
			{
				currVal = overrideCallback(ctrlName);
			}
			m_ctrlName.value = LocalizationKeys.CarProfileParameterCaption.FormatLocalized(ctrlName);
			m_currVal.value = $"{currVal} {model.units}";
		}

		protected override void OnDestroy()
		{
			TimeController.onUpdate = (Action)Delegate.Remove(TimeController.onUpdate, new Action(OnUpdateHandler));
			base.OnDestroy();
		}

		public void SetNewParam(string newParam, string units)
		{
			m_newParamDataInited = true;
			m_newVal.value = $"{newParam} {units}";
		}

		public void ClearNewVal()
		{
			m_newParamDataInited = false;
			m_newVal.value = string.Empty;
		}
	}
}
