using System.Collections.Generic;
using Binding;
using Binding.Base;
using GameInput;
using Rewired;
using Unity.Mathematics;
using UnityEngine;
using Utils.VinylLayerExtensions;
using VinylSystem;

public class UIVinylsEditingContext : BaseBindingBehaviourTarget
{
	private VinylWrapEditor m_vinylsEditor;

	private GameInput.InputManager m_inputManager;

	private CameraRotation m_cameraRotation;

	private float m_accelerateInput;

	private bool m_rotateVinylProj;

	private StringProperty m_position = new StringProperty();

	private StringProperty m_angle = new StringProperty();

	private StringProperty m_scale = new StringProperty();

	private BoolProperty m_isAddControllsMode = new BoolProperty(false);

	private BoolProperty m_isSymmetry = new BoolProperty(false);

	private IntProperty m_groupMaskState = new IntProperty();

	private Camera m_camera;

	private Camera m_uiCamera;

	[SerializeField]
	private RectTransform m_selector;

	private UIVinylsEditingLayersContext m_layersContext;

	private VinylLayer m_copyLayer;

	private List<MultiActionsData> m_sideHints = new List<MultiActionsData>();

	private List<ActionHintData> m_bottomHints = new List<ActionHintData>();

	private float m_speedMove = 150f;

	private float m_speedScale = 2f;

	private float m_speedRotate = 180f;

	private void Awake()
	{
		m_isSymmetry.OnValueChanged += delegate
		{
			m_vinylsEditor.SetSymmetryToEditingLayer(m_isSymmetry.value);
		};
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		if (m_camera == null)
		{
			m_camera = Camera.main;
		}
		if (m_uiCamera == null)
		{
			Canvas componentInParent = GetComponentInParent<Canvas>();
			m_uiCamera = ((componentInParent.renderMode == RenderMode.ScreenSpaceOverlay) ? null : componentInParent.worldCamera);
		}
		if (m_vinylsEditor == null)
		{
			m_vinylsEditor = VinylSystemKernel.instance.editor;
		}
		if (m_inputManager == null)
		{
			m_inputManager = GameInput.InputManager.instance;
		}
		if (m_cameraRotation == null)
		{
			m_cameraRotation = CameraRotation.instance;
		}
		VinylSystemKernel.instance.worker.Update();
		m_layersContext = base.owner.Get<UIVinylsEditingLayersContext>();
		m_isAddControllsMode.value = false;
		RefreshHint();
		m_copyLayer = m_vinylsEditor.editingLayer.Copy();
		m_isSymmetry.value = m_copyLayer.isSymmetry;
		UpdateGroupMaskButtons();
		m_inputManager.ignoreDuplicateActions = true;
	}

	protected override void OnDeactivate()
	{
		m_inputManager.ignoreDuplicateActions = false;
		HideSidePanel();
		base.OnDeactivate();
		m_isAddControllsMode.value = false;
	}

	private void RefreshHint()
	{
		m_bottomHints.Clear();
		m_bottomHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = NavCancelBtnBHandler
		});
		m_bottomHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.confirm.Localized(),
			type = ActionType.NavSelectBtnA,
			action = ConfirmHandler
		});
		ShowBottomPanel(m_bottomHints);
		RefreshSidePanelHints();
	}

	private void RefreshSidePanelHints()
	{
		m_sideHints.Clear();
		string hint = (m_layersContext.IsHintsHidden() ? LocalizationKeys.editor_show_hints.Localized() : LocalizationKeys.editor_hide_hints.Localized());
		m_sideHints.Add(new MultiActionsData(hint, ActionType.HideSidePanel, ToggleHintVisibleHandler));
		if (m_layersContext.IsHintsHidden())
		{
			ShowSidePanel(m_sideHints, ignoreRepeat: false, transparent: true);
			return;
		}
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.nav_add_controlls.Localized(), ActionType.NavAdditionalPrevBtnL2, ToggleAddControllerModeHandler));
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.rotate.Localized(), ActionType.NavPrevPage, ActionType.NavNextPage));
		if (m_isAddControllsMode.value)
		{
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.scale.Localized(), ActionType.NavDPadUp, ActionType.NavDPadDown, ActionType.NavDPadLeft, ActionType.NavDPadRight));
		}
		else
		{
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.scale.Localized(), ActionType.NavDPadUp, ActionType.NavDPadDown));
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.flip.Localized(), (ActionType.NavDPadLeft, NavDPadLeftHandler), (ActionType.NavDPadRight, NavDPadRightHandler)));
		}
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.projection.Localized(), ActionType.NavAdditionalNextBtnR2, RotateLayerFromCamera));
		if (m_isAddControllsMode.value)
		{
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.groupmask.Localized(), ActionType.NavBtnExpandRight, GroupMaskButtonClickHandler));
		}
		else
		{
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.nav_hint_reset_camera.Localized(), ActionType.NavBtnExpandRight, ResetCameraHandler));
		}
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.symmetry.Localized(), ActionType.NavAdditional1BtnY, ToggleSymmetryLayerHandler));
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.generic_copy.Localized(), ActionType.NavAdditional0BtnX, DuplicateLayerHandler));
		VinylLayer editingLayer = m_vinylsEditor.editingLayer;
		if (editingLayer != null && editingLayer.colors.Length != 0)
		{
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.nav_hint_edit_color.Localized(), ActionType.NavBtnExpand, EditLayerColorHandler));
		}
		ShowSidePanel(m_sideHints, ignoreRepeat: true);
	}

	private void ToggleHintVisibleHandler()
	{
		m_layersContext.ToggleHintVisibleHandler();
		RefreshSidePanelHints();
	}

	private void CompleteEdititing()
	{
		m_vinylsEditor.CompleteEditLayer();
	}

	public void ConfirmHandler()
	{
		CompleteEdititing();
		Hide();
		base.owner.Push<UIVinylsEditingLayersContext>();
	}

	public void NavCancelBtnBHandler()
	{
		if (!m_vinylsEditor.editingLayer.LayerEquals(m_copyLayer))
		{
			m_vinylsEditor.ReplaceEditingLayer(m_copyLayer.Copy());
			GUICommonGodVoice.ShowText(LocalizationKeys.layer_change_discard.Localized(), 3f);
			SoundController.Play2DUISound(SoundSample.VinylCancel, 0.1f, 0.1f);
		}
		CompleteEdititing();
		Hide();
		UIVinylsEditingLayersContext uIVinylsEditingLayersContext = base.owner.Push<UIVinylsEditingLayersContext>();
		int selectedLayerIndex = uIVinylsEditingLayersContext.selectedLayerIndex;
		VinylLayer layer = m_vinylsEditor.GetLayer(selectedLayerIndex);
		uIVinylsEditingLayersContext.RemoveLayer(selectedLayerIndex);
		uIVinylsEditingLayersContext.InsertLayer(selectedLayerIndex, layer);
	}

	[GameHotkey(ActionType.NavBtnExpand, false, false)]
	public void EditLayerColorHandler()
	{
		if (m_vinylsEditor.editingLayer.colors.Length != 0)
		{
			Hide();
			base.owner.Push<UIVinylsEditingColorContext>();
		}
	}

	[GameHotkey(ActionType.NavAdditional0BtnX, false, false)]
	public void DuplicateLayerHandler()
	{
		if (m_vinylsEditor.IsMaxCountLayers())
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.vinyls_max_layers.Localized(), 3f);
			return;
		}
		UIVinylsEditingLayersContext uIVinylsEditingLayersContext = base.owner.Get<UIVinylsEditingLayersContext>();
		int index = uIVinylsEditingLayersContext.selectedLayerIndex + 1;
		VinylLayer layer = m_vinylsEditor.DuplicateEditingLayer();
		m_vinylsEditor.StartEditLayer(layer);
		uIVinylsEditingLayersContext.InsertLayer(index, layer);
		GUICommonGodVoice.ShowText(LocalizationKeys.dublicate_layer.Localized(), 3f);
	}

	public void CreateLayerHandler()
	{
		if (m_vinylsEditor.IsMaxCountLayers())
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.vinyls_max_layers.Localized(), 3f);
			return;
		}
		Hide();
		base.owner.Push<UIVinylsEditingLayersContext>().CreateLayerHandler();
	}

	[GameHotkey(ActionType.NavDPadRight, false, false)]
	public void NavDPadRightHandler()
	{
		if (!m_isAddControllsMode.value)
		{
			m_vinylsEditor.SetFlipToEditingLayer(horizontal: false, !m_vinylsEditor.editingLayer.isVerticalFlip);
			GUICommonGodVoice.ShowText(LocalizationKeys.flip_layer.Localized(), 3f);
		}
	}

	[GameHotkey(ActionType.NavDPadLeft, false, false)]
	public void NavDPadLeftHandler()
	{
		if (!m_isAddControllsMode.value)
		{
			m_vinylsEditor.SetFlipToEditingLayer(horizontal: true, !m_vinylsEditor.editingLayer.isHorizontalFlip);
			GUICommonGodVoice.ShowText(LocalizationKeys.flip_layer.Localized(), 3f);
		}
	}

	[GameHotkey(ActionType.NavAdditional1BtnY, false, false)]
	public void ToggleSymmetryLayerHandler()
	{
		m_vinylsEditor.SetSymmetryToEditingLayer(!m_vinylsEditor.editingLayer.isSymmetry);
		m_isSymmetry.value = m_vinylsEditor.editingLayer.isSymmetry;
		GUICommonGodVoice.ShowText(m_isSymmetry.value ? LocalizationKeys.layer_symmetry_on.Localized() : LocalizationKeys.layer_symmetry_off.Localized(), 3f);
	}

	[GameHotkey(ActionType.NavBtnExpandRight, false, false)]
	public void GroupMaskButtonClickHandler()
	{
		int groupMask = m_vinylsEditor.editingLayer.groupMask;
		groupMask = ((groupMask == 0 || groupMask == -1) ? 1 : (((groupMask & 2) == 0) ? (groupMask << 1) : (-1)));
		m_vinylsEditor.SetGroupMaskToEditingLayer(groupMask);
		UpdateGroupMaskButtons();
		string text = string.Empty;
		switch (m_groupMaskState.value)
		{
		case 0:
			text = LocalizationKeys.layer_groupmask_body.Localized();
			break;
		case 1:
			text = LocalizationKeys.layer_groupmask_glass.Localized();
			break;
		case 2:
			text = LocalizationKeys.layer_groupmask_all.Localized();
			break;
		}
		GUICommonGodVoice.ShowText(text, 3f);
	}

	[GameHotkey(ActionType.NavBtnExpandRight, false, false)]
	private void ResetCameraHandler()
	{
		if (!m_isAddControllsMode.value)
		{
			Quaternion worldSpaceRotationOfEditingLayer = m_vinylsEditor.GetWorldSpaceRotationOfEditingLayer();
			m_cameraRotation.RotateToPosition(worldSpaceRotationOfEditingLayer, 0.2f);
		}
	}

	[GameHotkey(ActionType.NavAdditionalPrevBtnL2, false, false)]
	private void ToggleAddControllerModeHandler()
	{
		m_isAddControllsMode.value = !m_isAddControllsMode.value;
		RefreshHint();
		if (m_layersContext.IsHintsHidden())
		{
			string text = (m_isAddControllsMode.value ? LocalizationKeys.settings_game_inclinometer_on.Localized() : LocalizationKeys.settings_game_inclinometer_off.Localized());
			GUICommonGodVoice.ShowText(LocalizationKeys.nav_add_controlls.Localized() + ": " + text, 3f);
		}
	}

	private void FixedUpdate()
	{
		if (m_vinylsEditor.isLiveryEditing && m_vinylsEditor.isLayerEditing)
		{
			UpdateNavLayer();
		}
	}

	private void RefreshStats()
	{
		if (m_vinylsEditor.editingLayer != null)
		{
			Vector2 screenSpacePositionOfEditingLayer = m_vinylsEditor.GetScreenSpacePositionOfEditingLayer(m_camera);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(m_selector.parent as RectTransform, screenSpacePositionOfEditingLayer, m_uiCamera, out var localPoint);
			m_selector.anchoredPosition = localPoint;
		}
	}

	private bool NavScaleLayer(float coefSpeed)
	{
		Vector2 vector = Vector2.zero;
		if (m_inputManager.IsActionHold(ActionType.NavDPadDown))
		{
			vector = ((!m_isAddControllsMode.value) ? (Vector2.one * -1f * (Vector2)math.normalize(m_vinylsEditor.editingLayer.scale)) : Vector2.down);
		}
		else if (m_inputManager.IsActionHold(ActionType.NavDPadUp))
		{
			vector = ((!m_isAddControllsMode.value) ? (Vector2.one * (Vector2)math.normalize(m_vinylsEditor.editingLayer.scale)) : Vector2.up);
		}
		else if (m_inputManager.IsActionHold(ActionType.NavDPadRight))
		{
			if (m_isAddControllsMode.value)
			{
				vector = Vector2.right;
			}
		}
		else if (m_inputManager.IsActionHold(ActionType.NavDPadLeft) && m_isAddControllsMode.value)
		{
			vector = Vector2.left;
		}
		if (vector != Vector2.zero)
		{
			float2 float5 = vector * coefSpeed * Time.deltaTime * m_speedScale;
			Vector2 scale = m_vinylsEditor.editingLayer.scale;
			if (scale.x + float5.x > float.MaxValue)
			{
				float5.x = float.MaxValue - scale.x;
				if (!m_isAddControllsMode.value)
				{
					return false;
				}
			}
			if (scale.x + float5.x < 0.0035f)
			{
				float5.x = 0.0035f - scale.x;
				if (!m_isAddControllsMode.value)
				{
					return false;
				}
			}
			if (scale.y + float5.y > float.MaxValue)
			{
				float5.y = float.MaxValue - scale.y;
				if (!m_isAddControllsMode.value)
				{
					return false;
				}
			}
			if (scale.y + float5.y < 0.0035f)
			{
				float5.y = 0.0035f - scale.y;
				if (!m_isAddControllsMode.value)
				{
					return false;
				}
			}
			m_vinylsEditor.ScaleEditingLayer(float5, 0.0035f, float.MaxValue, additiveMode: true);
			return true;
		}
		return false;
	}

	private void UpdateNavLayer()
	{
		if (m_inputManager.lastController == null)
		{
			return;
		}
		m_accelerateInput = Mathf.Clamp(m_accelerateInput + Time.deltaTime, 0.1f, 1f);
		float num = Mathf.Pow(m_accelerateInput, 2f);
		bool flag = false;
		if (m_inputManager.IsActionHold(ActionType.NavNextPage))
		{
			m_vinylsEditor.RotateEditingLayer((0f - m_speedRotate) * num * Time.deltaTime);
		}
		else if (m_inputManager.IsActionHold(ActionType.NavPrevPage))
		{
			m_vinylsEditor.RotateEditingLayer(m_speedRotate * num * Time.deltaTime);
		}
		else if (!NavScaleLayer(num))
		{
			flag = true;
		}
		if (Input.GetMouseButton(1))
		{
			Vector2 firstTouchOrMousePosition = UnityExtensions.GetFirstTouchOrMousePosition();
			m_vinylsEditor.SetScreenSpacePositionToEditingLayer(m_camera, firstTouchOrMousePosition);
			RotateLayerFromCamera();
		}
		else
		{
			Vector2 vector = Vector2.zero;
			if (ReInput.controllers.GetLastActiveControllerType() == ControllerType.Joystick)
			{
				vector = ReInput.controllers.GetLastActiveController<Joystick>().GetAxis2D(0);
			}
			else
			{
				if (m_inputManager.IsActionHold(ActionType.NavRight))
				{
					vector.x += 1f;
				}
				if (m_inputManager.IsActionHold(ActionType.NavLeft))
				{
					vector.x -= 1f;
				}
				if (m_inputManager.IsActionHold(ActionType.NavUp))
				{
					vector.y += 1f;
				}
				if (m_inputManager.IsActionHold(ActionType.NavDown))
				{
					vector.y -= 1f;
				}
			}
			if (vector.sqrMagnitude > 0f)
			{
				Vector2 screenSpacePositionOfEditingLayer = m_vinylsEditor.GetScreenSpacePositionOfEditingLayer(m_camera);
				screenSpacePositionOfEditingLayer += vector * num * Time.deltaTime * m_speedMove;
				m_vinylsEditor.SetScreenSpacePositionToEditingLayer(m_camera, screenSpacePositionOfEditingLayer);
				flag = false;
			}
		}
		ControllerType type = m_inputManager.lastController.type;
		if (type == ControllerType.Keyboard || type == ControllerType.Mouse)
		{
			if (m_inputManager.IsActionHold(ActionType.NavAdditionalNextBtnR2))
			{
				RotateLayerFromCamera();
			}
		}
		else
		{
			if (m_inputManager.IsActionPressed(ActionType.NavAdditionalNextBtnR2))
			{
				m_rotateVinylProj = true;
			}
			if (m_inputManager.IsActionReleased(ActionType.NavAdditionalNextBtnR2))
			{
				m_rotateVinylProj = false;
			}
			if (m_rotateVinylProj)
			{
				RotateLayerFromCamera();
			}
		}
		if (flag)
		{
			m_accelerateInput = 0f;
		}
		RefreshStats();
	}

	[GameHotkey(ActionType.NavAdditionalNextBtnR2, false, false)]
	public void RotateLayerFromCamera()
	{
		m_vinylsEditor.SetRotationToEditingLayer(m_camera.transform.rotation);
	}

	private void UpdateGroupMaskButtons()
	{
		bool flag = m_vinylsEditor.editingLayer.IsGroupEnabled(0);
		bool flag2 = m_vinylsEditor.editingLayer.IsGroupEnabled(1);
		if (flag && !flag2)
		{
			m_groupMaskState.value = 0;
		}
		else if (!flag && flag2)
		{
			m_groupMaskState.value = 1;
		}
		else if (flag && flag2)
		{
			m_groupMaskState.value = 2;
		}
	}
}
