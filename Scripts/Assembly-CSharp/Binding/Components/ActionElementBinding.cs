using System;
using GameInput;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Binding.Components
{
	public class ActionElementBinding : MonoBehaviour
	{
		[SerializeField]
		private ActionType m_actionType = ActionType.MenuLayerSeparator;

		[SerializeField]
		private string m_format = "{0}";

		[SerializeField]
		private bool m_forceUpperCase = true;

		[SerializeField]
		private bool m_shortingName = true;

		[SerializeField]
		private string m_hintText = "";

		[SerializeField]
		private Image m_defaultKeyImage;

		[SerializeField]
		private Image m_keyImage;

		[SerializeField]
		private TextMeshProUGUI m_keyHint;

		[SerializeField]
		private TextMeshProUGUI m_hintLabel;

		private Text m_keyHintUText;

		public bool isRepeatAction;

		private GameInput.InputManager m_inputManager;

		private ActionElementResources m_actionsResources;

		private bool m_isHintInited;

		[Tooltip("Only visual, no OnClick subscribers.")]
		[SerializeField]
		protected bool m_onlyHint;

		[SerializeField]
		private UnityEvent onClick;

		protected bool m_hasRuntimeListener;

		private HotkeyManager.HotKeyAction m_hotKey;

		private BaseContext m_owner;

		private bool m_isRegisteredHotkey;

		private BindElement m_prevBind;

		public ActionType actionType => m_actionType;

		public string hintText => m_hintText;

		public Image clickEffectImage
		{
			get
			{
				if (!m_keyImage.isActiveAndEnabled)
				{
					return m_defaultKeyImage;
				}
				return m_keyImage;
			}
		}

		public bool onlyHint => m_onlyHint;

		public bool isEmpty => m_keyHint.text == string.Empty;

		private void Awake()
		{
			if (m_keyImage == null)
			{
				Transform transform = base.transform.FindChildDeph("Icon");
				if (transform != null)
				{
					m_keyImage = transform.GetComponent<Image>();
				}
			}
			if (m_keyHint == null)
			{
				Transform transform2 = base.transform.FindChildDeph("KeyHint");
				if (transform2 != null)
				{
					m_keyHint = transform2.GetComponent<TextMeshProUGUI>();
				}
			}
			if (m_hintLabel == null)
			{
				Transform transform3 = base.transform.FindChildDeph("Hint");
				if (transform3 != null)
				{
					m_hintLabel = transform3.GetComponent<TextMeshProUGUI>();
				}
			}
			if (m_actionType == ActionType.ErrorAction || m_actionType == ActionType.MenuLayerSeparator)
			{
				GameLog.Print(AlertLevel.Warning, "InputHint", "No parameter was specified for " + m_hintText + " (" + base.gameObject.name + ")");
			}
		}

		private void Start()
		{
		}

		private void OnEnable()
		{
			if (m_inputManager == null)
			{
				m_inputManager = GameInput.InputManager.instance;
			}
			if (m_actionsResources == null)
			{
				m_actionsResources = ScriptableSingleton<ActionElementResources>.instance;
			}
			if (m_actionType != ActionType.ErrorAction)
			{
				if (m_inputManager != null)
				{
					GameInput.InputManager inputManager = m_inputManager;
					inputManager.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(inputManager.onChangeActiveDevice, new Action<Controller>(ChangedDevice));
					GameInput.InputManager inputManager2 = m_inputManager;
					inputManager2.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(inputManager2.onChangeActiveDevice, new Action<Controller>(ChangedDevice));
					ChangedDevice(m_inputManager.lastController);
				}
				else
				{
					Debug.Log(base.transform.name + "; " + base.gameObject.name);
				}
				RegisterHotkey();
			}
		}

		private void RegisterHotkey(bool ignoreRepeat = false)
		{
			if (!m_onlyHint && actionType != ActionType.ErrorAction && !m_isRegisteredHotkey && base.isActiveAndEnabled)
			{
				m_isRegisteredHotkey = true;
				HotkeyManager.RegisterHotkey(this, ignoreRepeat);
			}
		}

		private void UnregisterHotkey()
		{
			if (m_isRegisteredHotkey)
			{
				m_isRegisteredHotkey = false;
				HotkeyManager.UnregisterHotkey(this);
			}
		}

		private void OnDisable()
		{
			if (m_inputManager != null)
			{
				GameInput.InputManager inputManager = m_inputManager;
				inputManager.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(inputManager.onChangeActiveDevice, new Action<Controller>(ChangedDevice));
			}
			UnregisterHotkey();
		}

		public void SetData(string hintText, ActionType typeAction, System.Action action, bool ignoreRepeat = false)
		{
			if (m_onlyHint)
			{
				Debug.LogError("[AEB] set action to Only Hint Element disabled");
				return;
			}
			m_actionType = typeAction;
			m_hintText = ((!string.IsNullOrEmpty(hintText)) ? hintText : string.Empty);
			if ((bool)m_hintLabel)
			{
				m_hintLabel.text = m_hintText;
			}
			onClick.RemoveAllListeners();
			onClick.AddListener(action.SafeCall);
			m_hasRuntimeListener = true;
			GameInput.InputManager inputManager = m_inputManager;
			inputManager.onChangeActiveDevice = (Action<Controller>)Delegate.Remove(inputManager.onChangeActiveDevice, new Action<Controller>(ChangedDevice));
			GameInput.InputManager inputManager2 = m_inputManager;
			inputManager2.onChangeActiveDevice = (Action<Controller>)Delegate.Combine(inputManager2.onChangeActiveDevice, new Action<Controller>(ChangedDevice));
			ChangedDevice(m_inputManager.lastController);
			RegisterHotkey(ignoreRepeat);
		}

		public void Release()
		{
			onClick.RemoveAllListeners();
			m_hasRuntimeListener = false;
			base.gameObject.SetActive(value: false);
			m_actionType = ActionType.ErrorAction;
		}

		private void UpdateOutputBinding(string element)
		{
			string text = (m_forceUpperCase ? element.ToUpper() : element.ToLower());
			if (m_shortingName && text.Length > 1)
			{
				string[] array = text.Split(' ');
				text = ((array.Length > 1) ? (array[0].Substring(0, 1) + array[1].Substring(0, 1)) : array[0].Substring(0, 1));
			}
			SetKeyHintText(text);
		}

		private void SetKeyHintText(string text)
		{
			if (m_keyHint != null)
			{
				m_keyHint.gameObject.SetActive(value: true);
				m_keyHint.text = string.Format(m_format, text);
			}
			else if ((bool)m_keyHintUText)
			{
				m_keyHintUText.gameObject.SetActive(value: true);
				m_keyHintUText.text = string.Format(m_format, text);
			}
		}

		public BaseContext GetParentBaseContext()
		{
			if (m_owner == null)
			{
				m_owner = GetComponentInParent<BaseContext>();
			}
			return m_owner;
		}

		public HotkeyManager.HotKeyAction GetHotkey()
		{
			if (m_hotKey == null)
			{
				m_hotKey = new HotkeyManager.HotKeyAction(OnClick, actionType, "OnClick->" + ((onClick.GetPersistentEventCount() > 0) ? onClick.GetPersistentMethodName(0) : "Empty(null)"), skipInputFX: false, isRepeatAction);
			}
			return m_hotKey;
		}

		private void ChangedDevice(Controller controller)
		{
			if (controller == null)
			{
				return;
			}
			Controller controller2 = controller;
			if (controller.type == ControllerType.Mouse)
			{
				controller2 = ReInput.controllers.Keyboard;
			}
			if (!m_isHintInited || m_inputManager.IsNativePlatformController(controller2))
			{
				BindElement actionInfoByDevice = m_inputManager.GetActionInfoByDevice(m_actionType, controller2, m_prevBind);
				if (actionInfoByDevice == null && !string.IsNullOrEmpty(m_hintText))
				{
					GameLog.Print(AlertLevel.Error, "UI", "Not found bind " + m_actionType.ToString() + " for " + controller.Name() + "(" + base.gameObject.name + ")");
				}
				else
				{
					m_isHintInited = m_actionType != ActionType.ErrorAction;
				}
				m_prevBind = actionInfoByDevice;
				if (m_actionsResources.isEmpty)
				{
					UpdateByBind(actionInfoByDevice);
				}
				else
				{
					UpdateHints(actionInfoByDevice);
				}
			}
		}

		public void OnClick()
		{
			if (HotkeyManager.CanClick(this) && !m_onlyHint)
			{
				GameInput.InputManager.instance.PlayActionElementBindingPressedEffect(this, onClick.Invoke);
			}
		}

		public void OnClickNoEffect()
		{
			if (HotkeyManager.CanClick(this) && !m_onlyHint)
			{
				onClick.Invoke();
			}
		}

		public static Sprite GetSpriteBindElement(BindElement bind)
		{
			if (bind == null)
			{
				return null;
			}
			ActionElementResources.PlatformTypeElementAction platformType = ActionElementResources.PlatformTypeElementAction.Default;
			if (bind.deviceInfo != null && (bind.deviceInfo.rewiredType == ControllerType.Joystick || bind.deviceInfo.rewiredType == ControllerType.Custom))
			{
				platformType = ((!bind.deviceInfo.isPS4PadCompatible) ? ActionElementResources.PlatformTypeElementAction.Xbox : ActionElementResources.PlatformTypeElementAction.DualShock);
			}
			return ScriptableSingleton<ActionElementResources>.instance.GetBindActionSprite(bind, platformType);
		}

		private void UpdateHints(BindElement bind)
		{
			Sprite spriteBindElement = GetSpriteBindElement(bind);
			if ((bool)spriteBindElement)
			{
				if (m_keyImage != null)
				{
					m_keyImage.gameObject.SetActive(value: true);
					m_keyImage.sprite = spriteBindElement;
				}
				if (m_defaultKeyImage != null)
				{
					m_defaultKeyImage.gameObject.SetActive(value: false);
				}
			}
			else
			{
				UpdateByBind(bind);
			}
		}

		private void UpdateByBind(BindElement bind)
		{
			if (bind != null)
			{
				if (m_defaultKeyImage != null)
				{
					m_defaultKeyImage.gameObject.SetActive(value: true);
				}
				if (m_keyImage != null)
				{
					m_keyImage.gameObject.SetActive(value: false);
				}
				UpdateOutputBinding(bind.elementId);
			}
			else
			{
				if (m_defaultKeyImage != null)
				{
					m_defaultKeyImage.gameObject.SetActive(value: true);
				}
				if (m_keyImage != null)
				{
					m_keyImage.gameObject.SetActive(value: false);
				}
				ShowKeyHint(value: false);
			}
		}

		private void ShowKeyHint(bool value)
		{
			if ((bool)m_keyHint)
			{
				m_keyHint.gameObject.SetActive(value);
			}
			else if ((bool)m_keyHintUText)
			{
				m_keyHintUText.gameObject.SetActive(value);
			}
		}
	}
}
