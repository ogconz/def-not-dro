using Binding.Base;
using GameInput;
using Rewired;
using UnityEngine;

namespace UI
{
	public class UICameraRotationContext : BaseBindingBehaviourTarget
	{
		private float initialSRTIFFP;

		private float initialSRTIFFR;

		private bool m_disableRotation;

		public CameraRotation cameraRotation { get; private set; }

		public bool disabled
		{
			get
			{
				return m_disableRotation;
			}
			set
			{
				m_disableRotation = value;
				cameraRotation.secondsToReturnToIdleFromFixedPosition = (value ? float.MaxValue : initialSRTIFFP);
				cameraRotation.secondsToReturnToIdleFromForcedRotation = (value ? float.MaxValue : initialSRTIFFR);
			}
		}

		protected override void OnActivate()
		{
			base.OnActivate();
			m_disableRotation = false;
			if (cameraRotation == null)
			{
				cameraRotation = CameraRotation.instance;
			}
			if (cameraRotation == null)
			{
				Debug.LogError("CameraRotation not found");
				return;
			}
			initialSRTIFFP = cameraRotation.secondsToReturnToIdleFromFixedPosition;
			initialSRTIFFR = cameraRotation.secondsToReturnToIdleFromForcedRotation;
		}

		public void FixedUpdate()
		{
			if (!GameInput.InputManager.IsInputLocked() && !UIUtils.IsPointerOverGameObject())
			{
				float deltaTime = Time.deltaTime;
				float num = 0f;
				if (Input.GetKey(KeyCode.DownArrow))
				{
					num = 2f * deltaTime;
				}
				if (Input.GetKey(KeyCode.UpArrow))
				{
					num = -2f * deltaTime;
				}
				Mouse mouse = ReInput.controllers.Mouse;
				if (mouse.enabled && mouse.axisCount > 2)
				{
					num -= Mathf.Clamp(mouse.GetAxisRaw(2) * 0.3f, -1f, 1f) * 60f * deltaTime;
				}
			}
		}

		public void OnPressDown()
		{
			if (!GameInput.InputManager.IsInputLocked() && !UIUtils.IsPointerOverGameObject() && cameraRotation != null)
			{
				cameraRotation.idleRotation = true;
			}
		}

		public void OnPressUp()
		{
			if (cameraRotation != null)
			{
				cameraRotation.idleRotation = false;
			}
		}
	}
}
