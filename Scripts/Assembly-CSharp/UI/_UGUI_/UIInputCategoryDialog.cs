using System;
using System.Collections.Generic;
using Binding;
using GameInput;
using GameOverlay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI._UGUI_
{
	[GameHotkeyBlocker(true)]
	[RaycastBlocker(true)]
	public class UIInputCategoryDialog : UIBaseDialogContext
	{
		private const int MAX_SELECTED_CATEGORIES = 5;

		private const int INPUT_LENGTH = 22;

		[SerializeField]
		private TMP_InputField m_inputField;

		[SerializeField]
		private TMP_Text m_placeholder;

		[SerializeField]
		private GridLayoutGroup m_categoryGroupLayout;

		[SerializeField]
		private UIDialogCategoryPanelView m_categoryPanelViewPrefab;

		[SerializeField]
		private CreateRoomParamsList m_paramsList;

		[SerializeField]
		private Sprite m_privateIcon;

		[SerializeField]
		private Sprite m_privateInactiveIcon;

		public readonly List<int> selectedCategoriesIndexes = new List<int>(5);

		private readonly StringProperty m_description = new StringProperty();

		private readonly List<UIDialogCategoryPanelView> m_categories = new List<UIDialogCategoryPanelView>(15);

		private ToggleListSettingsItem m_toggleItem;

		private BaseEventData m_baseEventData;

		private int m_currentFocusCategory = -1;

		private bool m_shownKeyboardPopup;

		private bool m_categoriesFocus;

		private bool m_prevInputFieldFocused;

		public TMP_InputField input => m_inputField;

		public bool? isPrivate => m_toggleItem?.value;

		public List<string> categoryList { get; set; } = new List<string>();

		public string text
		{
			get
			{
				return m_inputField.text;
			}
			set
			{
				m_inputField.text = value;
			}
		}

		public string placeHolder
		{
			get
			{
				return m_placeholder.text;
			}
			set
			{
				if (value != null)
				{
					m_placeholder.text = value;
				}
			}
		}

		public string description
		{
			get
			{
				return m_description.value;
			}
			set
			{
				m_description.value = value;
			}
		}

		public InputField.ContentType contentType
		{
			get
			{
				return (InputField.ContentType)m_inputField.contentType;
			}
			set
			{
				m_inputField.contentType = (TMP_InputField.ContentType)value;
			}
		}

		private void Awake()
		{
			m_inputField.onSelect.AddListener(OnSelectInputField);
			m_inputField.onEndEdit.AddListener(OnEndEditInputField);
			m_baseEventData = new BaseEventData(EventSystem.current);
		}

		private void Update()
		{
			UpdateLockInput();
			HandleInputFieldFocus();
		}

		private void HandleInputFieldFocus()
		{
			if (m_prevInputFieldFocused != m_inputField.isFocused && m_inputField.isFocused && Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
			{
				m_shownKeyboardPopup = true;
				steamOverlay.ShowOverlayKeyboard(LocalizationKeys.input_livery_name.Localized(), string.Empty, 22);
			}
			m_prevInputFieldFocused = m_inputField.isFocused;
		}

		protected override void OnDestroy()
		{
			m_inputField.onSelect.RemoveAllListeners();
			m_inputField.onEndEdit.RemoveAllListeners();
			base.OnDestroy();
		}

		protected override void OnActivate()
		{
			SetToggleView();
			SubscribePS4TextReceive();
			if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
			{
				steamOverlay.onCloseOverlayKeyboard = null;
				steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Combine(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceieveInput));
			}
			SetFocusInputField();
		}

		private void SetToggleView()
		{
			m_paramsList.Clear();
			if (m_toggleItem == null)
			{
				m_toggleItem = new ToggleListSettingsItem();
			}
			m_toggleItem.value = true;
			m_toggleItem.title = LocalizationKeys.liveries_publish_private.Localized();
			m_toggleItem.value = false;
			m_toggleItem.sprite = m_privateIcon;
			ToggleListSettingsItem toggleItem = m_toggleItem;
			toggleItem.onChangedValue = (System.Action)Delegate.Combine(toggleItem.onChangedValue, new System.Action(OnToggleHandler));
			m_paramsList.AddItem(m_toggleItem);
			m_toggleItem.OnChangeValue();
		}

		private void OnToggleHandler()
		{
			m_toggleItem.sprite = (m_toggleItem.value ? m_privateIcon : m_privateInactiveIcon);
			m_paramsList.UpdateList();
		}

		protected override void OnDeactivate()
		{
			m_inputField.text = string.Empty;
			m_shownKeyboardPopup = false;
			selectedCategoriesIndexes.Clear();
			UnSubscribePS4TextReceive();
			if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
			{
				steamOverlay.onCloseOverlayKeyboard = (Action<string>)Delegate.Remove(steamOverlay.onCloseOverlayKeyboard, new Action<string>(OnReceieveInput));
			}
			ToggleListSettingsItem toggleItem = m_toggleItem;
			toggleItem.onChangedValue = (System.Action)Delegate.Remove(toggleItem.onChangedValue, new System.Action(OnToggleHandler));
			base.OnDeactivate();
		}

		private void SubscribePS4TextReceive()
		{
		}

		private void UnSubscribePS4TextReceive()
		{
		}

		public override void NavigateLeft()
		{
			BaseListSettingsItem baseListSettingsItem = m_paramsList.GetSelectedItem();
			if (baseListSettingsItem != null)
			{
				baseListSettingsItem.PrevValue();
				indexNavigation.value = -1;
				ResetSelection();
			}
			else if (m_categoriesFocus)
			{
				int num = m_currentFocusCategory - 1;
				if (num >= 0)
				{
					SetFocusCategory(num);
				}
			}
			else
			{
				base.NavigateLeft();
			}
		}

		public override void NavigateRight()
		{
			BaseListSettingsItem baseListSettingsItem = m_paramsList.GetSelectedItem();
			if (baseListSettingsItem != null)
			{
				baseListSettingsItem.NextValue();
				indexNavigation.value = -1;
				ResetSelection();
			}
			else if (m_categoriesFocus)
			{
				int num = m_currentFocusCategory + 1;
				if (num < m_categories.Count)
				{
					SetFocusCategory(num);
				}
			}
			else
			{
				base.NavigateRight();
			}
		}

		public override void OnClickOk()
		{
			if (m_categoriesFocus)
			{
				SelectCurrentCategory();
			}
			else
			{
				base.OnClickOk();
			}
		}

		[GameHotkey(ActionType.NavUp, false, false)]
		[GameHotkey(ActionType.NavDPadUp, false, false)]
		public void NavigateUp()
		{
			if (m_categoriesFocus)
			{
				int num = m_currentFocusCategory - m_categoryGroupLayout.constraintCount;
				if (num >= 0)
				{
					SetFocusCategory(num);
					return;
				}
				UnfocusLastCategory();
				SetFocusInputField();
			}
			else if (m_paramsList.selectedIndex >= 0)
			{
				SetFocusCategory(m_categories.Count - 1);
			}
			else
			{
				indexNavigation.value = -1;
				m_paramsList.selectedIndex = 0;
				ResetSelection();
			}
		}

		[GameHotkey(ActionType.NavDown, false, false)]
		[GameHotkey(ActionType.NavDPadDown, false, false)]
		public void NavigateDown()
		{
			if (m_paramsList.selectedIndex >= 0)
			{
				m_paramsList.selectedIndex = -1;
				indexNavigation.value = 0;
			}
			else
			{
				if (!m_categoriesFocus)
				{
					return;
				}
				int num = m_currentFocusCategory + m_categoryGroupLayout.constraintCount;
				if (num < m_categories.Count)
				{
					SetFocusCategory(num);
					return;
				}
				int num2 = m_currentFocusCategory - m_currentFocusCategory % m_categoryGroupLayout.constraintCount + m_categoryGroupLayout.constraintCount;
				if (num2 < m_categories.Count)
				{
					SetFocusCategory(num2);
					return;
				}
				m_categoriesFocus = false;
				UnfocusLastCategory();
				m_paramsList.selectedIndex = 0;
				UpdateSelection();
			}
		}

		public void InitAllowedCategories()
		{
			ClearAllowedCategories();
			for (int i = 0; i < categoryList.Count; i++)
			{
				string text = categoryList[i];
				UIDialogCategoryPanelView uIDialogCategoryPanelView = UnityEngine.Object.Instantiate(m_categoryPanelViewPrefab, m_categoryGroupLayout.gameObject.transform);
				uIDialogCategoryPanelView.SetTitle(text);
				uIDialogCategoryPanelView.SetIndex(i);
				uIDialogCategoryPanelView.OnClick = (Action<int>)Delegate.Combine(uIDialogCategoryPanelView.OnClick, new Action<int>(OnClickCategory));
				m_categories.Add(uIDialogCategoryPanelView);
			}
		}

		private void OnClickCategory(int index)
		{
			SetFocusCategory(index);
			SelectCurrentCategory();
		}

		private void OnEndEditInputField(string inputText)
		{
			m_categoriesFocus = true;
			SetFocusCategory(0);
			m_shownKeyboardPopup = false;
		}

		private void OnSelectInputField(string inputText)
		{
			indexNavigation.value = -1;
			m_categoriesFocus = false;
			UnfocusLastCategory();
			ResetSelection();
			_ = m_shownKeyboardPopup;
		}

		private void OnReceieveInput(string inputText)
		{
			m_inputField.text = inputText;
			m_inputField.DeactivateInputField(clearSelection: true);
			OnEndEditInputField(inputText);
		}

		private void OnReceieveInput()
		{
			m_inputField.text = string.Empty;
			m_inputField.DeactivateInputField(clearSelection: true);
			OnEndEditInputField(string.Empty);
		}

		private void ClearAllowedCategories()
		{
			foreach (UIDialogCategoryPanelView category in m_categories)
			{
				category.OnClick = (Action<int>)Delegate.Remove(category.OnClick, new Action<int>(OnClickCategory));
				UnityEngine.Object.Destroy(category.gameObject);
			}
			m_categories.Clear();
		}

		private void UpdateLockInput()
		{
			if (m_inputField.isFocused)
			{
				if (InputManager.GetLockedInputObject() != m_inputField)
				{
					InputManager.LockInput(m_inputField);
				}
			}
			else if (InputManager.GetLockedInputObject() == m_inputField)
			{
				InputManager.LockInput(null);
			}
		}

		private void SetFocusInputField()
		{
			m_categoriesFocus = false;
			m_paramsList.selectedIndex = -1;
			m_inputField.OnSelect(m_baseEventData);
		}

		private void SetFocusCategory(int index)
		{
			indexNavigation.value = -1;
			m_paramsList.selectedIndex = -1;
			ResetSelection();
			m_categoriesFocus = true;
			UnfocusLastCategory();
			m_categories[index].hover.value = true;
			m_currentFocusCategory = index;
		}

		private void UnfocusLastCategory()
		{
			if (m_currentFocusCategory >= 0)
			{
				m_categories[m_currentFocusCategory].hover.value = false;
			}
			m_currentFocusCategory = -1;
		}

		private void SelectCurrentCategory()
		{
			UIDialogCategoryPanelView uIDialogCategoryPanelView = m_categories[m_currentFocusCategory];
			if (uIDialogCategoryPanelView.selected.value)
			{
				uIDialogCategoryPanelView.selected.value = false;
				selectedCategoriesIndexes.Remove(m_currentFocusCategory);
			}
			else if (selectedCategoriesIndexes.Count < 5)
			{
				uIDialogCategoryPanelView.selected.value = true;
				selectedCategoriesIndexes.Add(m_currentFocusCategory);
			}
		}
	}
}
