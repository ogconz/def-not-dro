using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Binding;
using Binding.Base;
using Cysharp.Threading.Tasks;
using DI;
using GameInput;
using PrefsModels;
using SyncMultiplayer;
using UnityEngine;
using Utils.VinylLayerExtensions;
using VinylSystem;

public class UIVinylsEditingLayersContext : BaseBindingBehaviourTarget
{
	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	private VinylWrapEditor m_vinylsEditor;

	private CameraRotation m_cameraRotation;

	private Camera m_camera;

	private Camera m_uiCamera;

	private StringProperty m_layersCountInfo = new StringProperty();

	private InputManager m_inputManager;

	[SerializeField]
	private RectTransform m_selector;

	[SerializeField]
	private VinylLayersList m_layersList;

	private CancellationTokenSource m_loadLayersCts;

	private List<VinylLayer> m_prevVinylLayers = new List<VinylLayer>();

	private List<MultiActionsData> m_sideHints = new List<MultiActionsData>();

	private List<ActionHintData> m_bottomHints = new List<ActionHintData>();

	private bool m_visibleHintsMode;

	private int m_layersCount => m_layersList.GetCount();

	private bool m_isLayersLoaded => m_loadLayersCts == null;

	public int selectedRawId => m_layersList.GetSelectedItem()?.id ?? (-1);

	public int selectedLayerIndex => m_layersList.GetSelectedIndex();

	public bool mustReplace { get; set; }

	private void _OnStartVinylSystem()
	{
		m_car3dView.Set3DModelVisible(value: true);
		if (!m_vinylsEditor.isLiveryEditing)
		{
			m_vinylsEditor.StartEditLivery(m_car3dView.GetRaceCar().carModel);
			FillLayersListAsync().Forget();
		}
	}

	private bool HaveChangesInLayers()
	{
		List<VinylLayer> vinylLayers = m_vinylsEditor.GetVinylLayers();
		if (vinylLayers.Count != m_prevVinylLayers.Count)
		{
			return true;
		}
		for (int i = 0; i < vinylLayers.Count; i++)
		{
			if (!vinylLayers[i].LayerEquals(m_prevVinylLayers[i]))
			{
				return true;
			}
		}
		return false;
	}

	private void _OnCompleteVinylSystem(bool needSave, bool rewrite)
	{
		GamePrefs gamePrefs = DependencyInjector.Resolve<GamePrefs>();
		int lastCarId = gamePrefs.globals.lastCarId;
		CarVisualManager.VisualSettings visualForCar = gamePrefs.carSettings.GetVisualForCar(lastCarId);
		CarVilylsInfo vinylsInfoForCar = gamePrefs.carSettings.GetVinylsInfoForCar(lastCarId);
		bool vinylsHasChanged = needSave && rewrite;
		if (rewrite)
		{
			if (needSave)
			{
				List<VinylLayer> vinylLayers = m_vinylsEditor.GetVinylLayers();
				if (vinylLayers.Count > 0)
				{
					CarVilylsInfo.VinylPreset vinylPreset = vinylsInfoForCar.GetPreset(visualForCar.vinylPresetLiveryId);
					if (vinylPreset == null || vinylPreset.liveryId == null)
					{
						vinylPreset = vinylsInfoForCar.CreatePreset();
						vinylsInfoForCar.AddPreset(vinylPreset);
					}
					vinylPreset.time = DateTime.Now.ToBinary();
					vinylPreset.layers.Clear();
					vinylPreset.layers.AddRange(vinylLayers);
					visualForCar.vinylPresetLiveryId = vinylPreset.liveryId;
					visualForCar.vinylPresetId = vinylPreset.id;
				}
				else
				{
					visualForCar.vinylPresetLiveryId = null;
					visualForCar.vinylPresetId = -1;
				}
			}
			else
			{
				m_vinylsEditor.ReplaceAllLayers(m_prevVinylLayers);
			}
		}
		visualForCar.vinylLayers = new List<VinylLayer>(m_vinylsEditor.GetVinylLayers());
		gamePrefs.carSettings.SetVisualForCar(lastCarId, visualForCar, vinylsHasChanged);
		if (needSave)
		{
			BasePrefsModel.SaveAllQueries(inOneFrame: false);
		}
		m_vinylsEditor.CompleteEditLivery();
		List<CarVilylsInfo.VinylPreset> list = new List<CarVilylsInfo.VinylPreset>();
		foreach (CarVilylsInfo.VinylPreset presets in vinylsInfoForCar.GetPresetsList())
		{
			if (presets != null && presets.layers.Count <= 0 && presets != null && presets.liveryId != null)
			{
				list.Add(presets);
				vinylsInfoForCar.RemovePreset(presets);
			}
		}
		m_layersList.Clear();
		Hide();
		UIVinylsContext uIVinylsContext = base.owner.Push<UIVinylsContext>();
		if (uIVinylsContext != null)
		{
			if (rewrite && needSave)
			{
				uIVinylsContext.MarkLiveryForRenderPreview(visualForCar.vinylPresetLiveryId);
			}
			for (int i = 0; i < list.Count; i++)
			{
				IconStorageSystem.DeleteLiveryPreview(lastCarId, list[i].liveryId);
			}
			uIVinylsContext.FillList(force: true);
		}
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
		if (m_cameraRotation == null)
		{
			m_cameraRotation = CameraRotation.instance;
		}
		if (m_inputManager == null)
		{
			m_inputManager = InputManager.instance;
		}
		if (m_vinylsEditor == null)
		{
			m_vinylsEditor = VinylSystemKernel.instance.editor;
		}
		VinylLayersList layersList = m_layersList;
		layersList.onClick = (Action<int>)Delegate.Combine(layersList.onClick, new Action<int>(OnClickLayer));
		VinylLayersList layersList2 = m_layersList;
		layersList2.onChangeList = (System.Action)Delegate.Combine(layersList2.onChangeList, new System.Action(RefreshHint));
		m_layersList.ForceUpdateList();
		_OnStartVinylSystem();
		if (m_cameraRotation != null)
		{
			m_cameraRotation.allowGamepadZoom = true;
			m_cameraRotation.useZoom = false;
			m_cameraRotation.useFovZoom = true;
			m_cameraRotation.SetIdle();
			m_cameraRotation.ResetToMaxDistance();
		}
		RefreshHint();
		m_inputManager.ignoreDuplicateActions = true;
	}

	protected override void OnDeactivate()
	{
		if (m_cameraRotation != null)
		{
			m_cameraRotation.allowGamepadZoom = false;
		}
		m_loadLayersCts?.Cancel();
		m_inputManager.ignoreDuplicateActions = false;
		HideSidePanel();
		base.OnDeactivate();
		VinylLayersList layersList = m_layersList;
		layersList.onClick = (Action<int>)Delegate.Remove(layersList.onClick, new Action<int>(OnClickLayer));
		VinylLayersList layersList2 = m_layersList;
		layersList2.onChangeList = (System.Action)Delegate.Remove(layersList2.onChangeList, new System.Action(RefreshHint));
	}

	private void RefreshHint()
	{
		if (m_isLayersLoaded)
		{
			m_bottomHints.Clear();
			m_bottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = GoToBackHandler
			});
			if (m_layersCount >= m_layersList.GetCellCount() - 2)
			{
				m_bottomHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.expand_ui_list.Localized(),
					type = ActionType.NavDPadUp,
					action = ExpandLayersListHandler
				});
			}
			ShowBottomPanel(m_bottomHints);
			RefreshSidePanelHints();
			RefreshLayerInfo();
		}
	}

	private void RefreshSidePanelHints()
	{
		m_sideHints.Clear();
		string hint = (IsHintsHidden() ? LocalizationKeys.editor_show_hints.Localized() : LocalizationKeys.editor_hide_hints.Localized());
		m_sideHints.Add(new MultiActionsData(hint, ActionType.HideSidePanel, ToggleHintVisibleHandler));
		if (IsHintsHidden())
		{
			ShowSidePanel(m_sideHints, ignoreRepeat: true, transparent: true);
			return;
		}
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.photo_session_zoom.Localized(), ActionType.ZoomOut, ActionType.ZoomIn));
		m_sideHints.Add(new MultiActionsData(LocalizationKeys.nav_hint_add_layer.Localized(), ActionType.NavAdditional0BtnX, CreateLayerHandler));
		if (m_layersCount > 0)
		{
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.nav_hint_remove_layer.Localized(), ActionType.NavBtnExpandRight, RemoveLayerHandler));
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.nav_hint_replace_layer.Localized(), ActionType.NavDPadDown, ReplaceLayerHandler));
			if (m_layersCount > 1)
			{
				m_sideHints.Add(new MultiActionsData(LocalizationKeys.vinyls_sort_layer.Localized(), (ActionType.NavPrevPage, MoveLayerDown), (ActionType.NavNextPage, MoveLayerUp)));
			}
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.symmetry.Localized(), ActionType.NavAdditional1BtnY, ToggleSymmetryLayerHandler));
			m_sideHints.Add(new MultiActionsData(LocalizationKeys.nav_hint_edit.Localized(), ActionType.NavSelectBtnA, SelectHandler));
		}
		ShowSidePanel(m_sideHints, ignoreRepeat: true);
	}

	private async UniTask FillLayersListAsync()
	{
		m_loadLayersCts?.Cancel();
		m_loadLayersCts = new CancellationTokenSource();
		CancellationToken token = m_loadLayersCts.Token;
		List<VinylLayer> layers = m_vinylsEditor.GetVinylLayers();
		if (m_prevVinylLayers.Capacity < layers.Count)
		{
			m_prevVinylLayers.Capacity = layers.Count;
		}
		m_prevVinylLayers.Clear();
		m_layersList.Clear();
		await UniTask.RunOnThreadPool(delegate
		{
			for (int i = 0; i < layers.Count; i++)
			{
				if (token.IsCancellationRequested)
				{
					break;
				}
				m_prevVinylLayers.Add(layers[i].Copy());
				m_layersList.items.Add(new VinylLayerItem(layers[i]));
			}
		}, configureAwait: true, token);
		m_loadLayersCts = null;
		m_layersList.RecalcList();
		RefreshHint();
		TimeController.StartNow(SubmitCurrentLayerSelectionCoroutine());
	}

	public void RemoveLayer(int index)
	{
		m_layersList.RemoveItem(index, shouldScroll: false);
		m_layersList.RecalcList();
	}

	public void InsertLayer(int index, VinylLayer layer)
	{
		m_layersList.InsertItem(index, new VinylLayerItem(layer));
		m_layersList.RecalcList();
		SelectLayer(index);
	}

	private void RefreshLayerInfo()
	{
		m_layersCountInfo.value = $"{m_layersCount} / {VinylSystemConfig.config.maxLayersCount}";
	}

	public void ExpandLayersListHandler()
	{
		base.owner.Push<UIVinylsLayerSelectionContext>().FillLayersList(m_layersList);
		Hide();
	}

	public void ToggleHintVisibleHandler()
	{
		m_visibleHintsMode = !m_visibleHintsMode;
		RefreshSidePanelHints();
	}

	public bool IsHintsHidden()
	{
		return m_visibleHintsMode;
	}

	[GameHotkey(ActionType.NavSelectBtnA, false, false)]
	public void SelectHandler()
	{
		VinylLayerItem vinylLayerItem = m_layersList.GetSelectedItem();
		if (vinylLayerItem?.vinylLayer != null)
		{
			m_vinylsEditor.StartEditLayer(vinylLayerItem.vinylLayer);
			Hide();
			base.owner.Push<UIVinylsEditingContext>();
		}
	}

	[GameHotkey(ActionType.NavBtnExpandRight, false, false)]
	private void RemoveLayerHandler()
	{
		if (m_layersCount > 0)
		{
			int num = selectedLayerIndex;
			VinylLayerItem item = m_layersList.GetItem(num);
			if (item != null)
			{
				m_vinylsEditor.RemoveLayer(item.vinylLayer);
				m_layersList.RemoveItem(num, shouldScroll: false);
				m_layersList.RecalcList();
				SelectLayer(num - 1);
				RefreshHint();
				RefreshLayerInfo();
			}
		}
	}

	[GameHotkey(ActionType.NavAdditional0BtnX, false, false)]
	public void CreateLayerHandler()
	{
		if (m_vinylsEditor.IsMaxCountLayers())
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.vinyls_max_layers.Localized(), 3f);
		}
		else
		{
			ShowDecalSelection();
		}
	}

	[GameHotkey(ActionType.NavDPadDown, false, false)]
	public void ReplaceLayerHandler()
	{
		if (m_layersCount > 0)
		{
			mustReplace = true;
			ShowDecalSelection();
		}
	}

	private void ShowDecalSelection()
	{
		Hide();
		base.owner.Push<UIVinylsDecalSelectionContext>();
	}

	[GameHotkey(ActionType.NavAdditional1BtnY, false, false)]
	private void ToggleSymmetryLayerHandler()
	{
		VinylLayerItem vinylLayerItem = m_layersList.GetSelectedItem();
		if (vinylLayerItem != null)
		{
			vinylLayerItem.vinylLayer.isSymmetry = !vinylLayerItem.vinylLayer.isSymmetry;
			m_layersList.UpdateList();
			m_vinylsEditor.Invalidate();
			GUICommonGodVoice.ShowText(vinylLayerItem.vinylLayer.isSymmetry ? LocalizationKeys.layer_symmetry_on.Localized() : LocalizationKeys.layer_symmetry_off.Localized(), 3f);
		}
	}

	private void GoToBackHandler()
	{
		if (HaveChangesInLayers())
		{
			UIMessageBox.ShowComplexDialog(LocalizationKeys.car_profile_window_save_changes_caption.Localized(), delegate
			{
			}, LocalizationKeys.Cancel.Localized(), delegate
			{
				_OnCompleteVinylSystem(needSave: true, rewrite: true);
				NetworkController.InstanceGame.CaptureLocalCarTextures(m_car3dView.currentCar).Forget();
			}, LocalizationKeys.Yes.Localized(), delegate
			{
				_OnCompleteVinylSystem(needSave: false, rewrite: true);
			}, LocalizationKeys.No.Localized());
		}
		else
		{
			_OnCompleteVinylSystem(needSave: false, rewrite: false);
		}
	}

	private bool IsThisContext()
	{
		if (!(States.mainCurrent is BaseGameState { contextOwner: var contextOwner }))
		{
			return false;
		}
		return contextOwner?.IsActive<UIVinylsEditingLayersContext>() ?? false;
	}

	[GameHotkey(ActionType.NavDPadLeft, false, true)]
	[GameHotkey(ActionType.NavLeft, false, true)]
	private void NavPrevLayer()
	{
		SelectLayer(selectedLayerIndex - 1);
	}

	[GameHotkey(ActionType.NavDPadRight, false, true)]
	[GameHotkey(ActionType.NavRight, false, true)]
	private void NavNextLayer()
	{
		SelectLayer(selectedLayerIndex + 1);
	}

	[GameHotkey(ActionType.NavNextPage, false, true)]
	public void MoveLayerUp()
	{
		int num = selectedLayerIndex;
		if (num >= 0)
		{
			List<VinylLayer> vinylLayers = m_vinylsEditor.GetVinylLayers();
			if (num < ((vinylLayers != null) ? new int?(vinylLayers.Count - 1) : ((int?)null)))
			{
				int num2 = num + 1;
				MoveLayer(num, num2);
				m_vinylsEditor.MoveLayer(num, num2);
				SelectLayer(num2);
			}
		}
	}

	[GameHotkey(ActionType.NavPrevPage, false, true)]
	public void MoveLayerDown()
	{
		int num = selectedLayerIndex;
		if (num > 0 && num < m_vinylsEditor.GetVinylLayers()?.Count)
		{
			int num2 = num - 1;
			MoveLayer(num, num2);
			m_vinylsEditor.MoveLayer(num, num2);
			SelectLayer(num2);
		}
	}

	private void MoveLayer(int oldIndex, int newIndex)
	{
		VinylLayerItem item = m_layersList.GetItem(oldIndex);
		if (item != null)
		{
			m_layersList.RemoveItem(oldIndex, shouldScroll: false);
			m_layersList.RecalcList();
			m_layersList.InsertItem(newIndex, item);
			m_layersList.RecalcList();
		}
	}

	private void OnClickLayer(int index)
	{
		if (m_layersList.selectedIndex == index)
		{
			SelectHandler();
		}
		else
		{
			SelectLayer(index);
		}
	}

	public void SelectLayer(int index)
	{
		if (index >= 0 && index < m_layersCount)
		{
			m_layersList.selectedIndex = index;
		}
	}

	private void _RotateCameraToSelect()
	{
		if (IsThisContext())
		{
			VinylLayerItem vinylLayerItem = m_layersList.GetSelectedItem();
			if (vinylLayerItem?.vinylLayer != null)
			{
				Quaternion worldSpaceRotation = m_vinylsEditor.GetWorldSpaceRotation(vinylLayerItem.vinylLayer);
				m_cameraRotation.RotateToPosition(worldSpaceRotation, 0.2f);
			}
		}
	}

	private IEnumerator SubmitCurrentLayerSelectionCoroutine()
	{
		yield return new WaitForEndOfFrame();
		int num = selectedLayerIndex;
		if (num == -1)
		{
			num = m_layersCount - 1;
		}
		num = Mathf.Clamp(num, 0, m_layersCount - 1);
		SelectLayer(num);
	}

	private void LateUpdate()
	{
		RefreshSelector(m_layersList.GetSelectedItem());
	}

	private void RefreshSelector(VinylLayerItem data)
	{
		VinylLayer vinylLayer = data?.vinylLayer;
		if (vinylLayer == null || m_car3dView == null || m_car3dView.GetRaceCar() == null || m_car3dView.GetRaceCar().carModel == null || m_car3dView.GetRaceCar().carModel.vinylsWrapCollider == null)
		{
			m_selector.gameObject.SetActive(value: false);
			return;
		}
		m_selector.gameObject.SetActive(value: true);
		Vector2 screenSpacePosition = m_vinylsEditor.GetScreenSpacePosition(m_camera, vinylLayer);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(m_selector.parent as RectTransform, screenSpacePosition, m_uiCamera, out var localPoint);
		m_selector.anchoredPosition = localPoint;
	}
}
