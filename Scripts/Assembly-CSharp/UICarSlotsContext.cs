using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using Customization;
using Cysharp.Threading.Tasks;
using DI;
using GameInput;
using GameOverlay;
using PrefsModels;
using Rewired;
using UnityEngine;
using Utils.VinylLayerExtensions;
using VinylSystem;

public class UICarSlotsContext : BaseBindingBehaviourTarget
{
	private const float SelectionDelay = 0.55f;

	private const int MaxCaptionLength = 46;

	private const string DefaultSlotCaption = "Preset {0}";

	private const float DistanceMax = 7f;

	private const float YMinLimit = -5f;

	private const float YMaxLimit = 60f;

	private const float CamXTilt = 7f;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private CarVisualManager m_visualManager;

	[Dependency]
	private PlayerCarFactory m_carFactory;

	private ListProperty m_presetsList = new ListProperty();

	private StringProperty m_slotsCount = new StringProperty();

	private ListProperty m_questList = new ListProperty();

	private BoolProperty m_showQuestList = new BoolProperty();

	private BaseSlotProcessStrategy m_slotProcessStrategy;

	private CameraInputController m_inputController;

	private CameraPhotoSession m_cameraPhotoSession;

	private Camera m_uiCamera;

	private List<ActionHintData> m_bottomHints = new List<ActionHintData>();

	private ActionHintData m_customConfirmHint;

	private UniTask? m_currentUniTask;

	private float m_lastTimeOfSelectSlot;

	private int m_forceApplySlotId = -1;

	private bool m_isVisualChecked;

	private bool m_cameraConfigured;

	protected override void OnActivate()
	{
		base.OnActivate();
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.car_slots_caption.Localized());
		m_presetsList.OnClick += OnClickSlotHandler;
		m_presetsList.OnDoubleClick += OnDoubleClickSlotHandler;
		m_presetsList.OnSelectionChanged += OnSelectSlotHandler;
		m_lastTimeOfSelectSlot = 0f;
		m_forceApplySlotId = -1;
		m_isVisualChecked = false;
		ResetConfirmHint();
	}

	protected override void OnDeactivate()
	{
		m_presetsList.OnClick -= OnClickSlotHandler;
		m_presetsList.OnDoubleClick -= OnDoubleClickSlotHandler;
		m_presetsList.OnSelectionChanged -= OnSelectSlotHandler;
		m_slotProcessStrategy.Deactivate();
		base.OnDeactivate();
		ClearSlotList();
	}

	public void Init(int targetCarId)
	{
		if (m_slotProcessStrategy == null)
		{
			m_slotProcessStrategy = GetStrategyOfState();
		}
		m_slotProcessStrategy.SetCarId(targetCarId);
		SyncLiveryBetweenSlots();
		SetCaptionForSlotsIfNeed();
		FillSlotList();
		m_slotProcessStrategy.Activate();
		ForcePreviewSelectedSlot();
	}

	private void Update()
	{
		if (m_slotProcessStrategy != null)
		{
			m_slotProcessStrategy.Update();
			PreviewSelectedSlot();
		}
	}

	private void LateUpdate()
	{
		UpdateCamera();
	}

	public void GenerateSnapshot()
	{
		int carId = m_slotProcessStrategy.GetCarId();
		int slotId = m_prefs.carSettings.GetActiveSlotId(carId);
		Texture2D data = RenderPreviewToTexture();
		IconStorageSystem.SaveLiveryPreview(carId, slotId, data);
		int index = m_presetsList.IndexOf((BaseListElementData element) => element is SlotListElementData slotListElementData && slotListElementData.slotId == slotId);
		m_presetsList.GetByIndex<SlotListElementData>(index)?.Init(carId, slotId);
	}

	public void UpdateSelection()
	{
		int carId = m_slotProcessStrategy.GetCarId();
		int slotId = m_prefs.carSettings.GetActiveSlotId(carId);
		int num = m_presetsList.IndexOf((BaseListElementData element) => element is SlotListElementData slotListElementData && slotListElementData.slotId == slotId);
		m_presetsList.Selection((num >= 0) ? num : 0, selected: true);
	}

	public void ReplaceConfirmHint(string text)
	{
		m_customConfirmHint = new ActionHintData
		{
			hint = text,
			type = ActionType.NavSelectBtnA
		};
		ShowBottomPanel();
	}

	public void ResetConfirmHint()
	{
		m_customConfirmHint = new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_select.Localized(),
			type = ActionType.NavSelectBtnA,
			action = ApplyAndExitHandler
		};
		ShowBottomPanel();
	}

	private void OnClickSlotHandler(BaseListElementData element)
	{
		if (!element.selected)
		{
			m_presetsList.Selection(m_presetsList.IndexOf(element), selected: true);
		}
	}

	private void OnDoubleClickSlotHandler(BaseListElementData element)
	{
		OnClickSlotHandler(element);
		if (IsSlotBlocked(element))
		{
			BuyDlcHandler();
		}
		else if (IsSlotUsed(element))
		{
			ApplyAndExitHandler();
		}
	}

	private void OnSelectSlotHandler(BaseListElementData element)
	{
		if (element.selected)
		{
			int num = m_presetsList.IndexOf(element);
			if (num >= 0)
			{
				m_presetsList.ScrollTo(num);
				m_lastTimeOfSelectSlot = Time.time;
			}
			FillQuestList();
			ShowBottomPanel();
		}
	}

	private void ShowBottomPanel()
	{
		m_bottomHints.Clear();
		m_bottomHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GoToBackHandler
		});
		if (IsSlotBlocked(m_presetsList.GetSelected()))
		{
			m_bottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.buy_dlc.Localized(),
				type = ActionType.NavSelectBtnA,
				action = BuyDlcHandler
			});
			ShowBottomPanel(m_bottomHints);
			return;
		}
		if (!IsSlotUsed(m_presetsList.GetSelected()))
		{
			if (m_slotProcessStrategy is GarageSlotProcessStrategy)
			{
				m_bottomHints.Add(new ActionHintData
				{
					hint = LocalizationKeys.car_slots_create_new.Localized(),
					type = ActionType.NavAdditional1BtnY,
					action = CreateNewSlotHandler
				});
			}
			ShowBottomPanel(m_bottomHints);
			return;
		}
		m_bottomHints.Add(m_customConfirmHint);
		if (m_slotProcessStrategy is GarageSlotProcessStrategy)
		{
			m_bottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.car_slots_copy.Localized(),
				type = ActionType.NavAdditional0BtnX,
				action = CopySlotHandler
			});
			m_bottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.car_slots_create_new.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = CreateNewSlotHandler
			});
			m_bottomHints.Add(new ActionHintData
			{
				hint = LocalizationKeys.car_slots_delete.Localized(),
				type = ActionType.NavBtnExpandRight,
				action = RemoveSlotHandler
			});
		}
		m_bottomHints.Add(new ActionHintData
		{
			hint = LocalizationKeys.car_slots_rename.Localized(),
			type = ActionType.NavBtnExpand,
			action = RenameSlotHandler
		});
		ShowBottomPanel(m_bottomHints);
	}

	public void FillSlotList()
	{
		ClearSlotList();
		int carId = m_slotProcessStrategy.GetCarId();
		foreach (int item in m_prefs.slotLock.ConstructSortedSlotList(carId))
		{
			m_presetsList.Add(new SlotListElementData(carId, item));
		}
		UpdateSelection();
		m_slotsCount.value = $"{m_prefs.slotLock.GetUnlockedAndUsedSlots(carId).Count} / {PrefsCarSlotsModel.GetMaxSlotCount()}";
		ShowBottomPanel();
	}

	private void ClearSlotList()
	{
		for (int i = 0; i < m_presetsList.count; i++)
		{
			m_presetsList[i].Dispose();
		}
		m_presetsList.Clear();
	}

	private void FillQuestList()
	{
		ClearQuestList();
		int carId = m_slotProcessStrategy.GetCarId();
		int selectedIndex = m_presetsList.GetSelectedIndex();
		if (selectedIndex < 0 || !IsSlotBlocked(m_presetsList.GetSelected()))
		{
			return;
		}
		foreach (var item3 in m_prefs.slotProgression.ConstructTaskList(carId, selectedIndex))
		{
			string item = item3.Item1;
			bool item2 = item3.Item2;
			string localizedTask = m_prefs.slotProgression.GetLocalizedTask(item, selectedIndex);
			bool isSeparator = string.IsNullOrEmpty(item);
			m_questList.Add(new SlotQuestListElement(localizedTask, item2, isSeparator));
		}
		m_showQuestList.value = m_questList.count > 0;
	}

	private void ClearQuestList()
	{
		for (int i = 0; i < m_questList.count; i++)
		{
			m_questList[i].Dispose();
		}
		m_questList.Clear();
		m_showQuestList.value = false;
	}

	private int GetSelectedSlotId()
	{
		if (!(m_presetsList.GetSelected() is SlotListElementData slotListElementData))
		{
			return -1;
		}
		return slotListElementData.slotId;
	}

	private bool IsSlotBlocked(BaseListElementData baseElement)
	{
		if (baseElement is SlotListElementData slotListElementData)
		{
			return slotListElementData.isBlocked;
		}
		return true;
	}

	private bool IsSlotUsed(BaseListElementData baseElement)
	{
		if (baseElement is SlotListElementData slotListElementData)
		{
			return slotListElementData.isUsed;
		}
		return false;
	}

	private void ForcePreviewSelectedSlot()
	{
		m_lastTimeOfSelectSlot = 0f;
		PreviewSelectedSlot();
	}

	private void PreviewSelectedSlot()
	{
		if (IsAnyTaskRunning())
		{
			return;
		}
		bool flag = false;
		bool flag2 = Time.time > m_lastTimeOfSelectSlot + 0.55f;
		int lastCarId = m_prefs.globals.lastCarId;
		int carId = m_slotProcessStrategy.GetCarId();
		if (lastCarId != carId)
		{
			flag = true;
			m_isVisualChecked = false;
		}
		int activeSlotId = m_prefs.carSettings.GetActiveSlotId(carId);
		int selectedSlotId = GetSelectedSlotId();
		if (activeSlotId != selectedSlotId && IsSlotUsed(m_presetsList.GetSelected()))
		{
			flag = true;
			m_isVisualChecked = false;
		}
		if (selectedSlotId < 0)
		{
			return;
		}
		if (!m_isVisualChecked)
		{
			m_isVisualChecked = true;
			if (m_visualManager.ClearVisualIfNeed(carId))
			{
				flag = true;
			}
		}
		if (flag && flag2)
		{
			m_currentUniTask = m_slotProcessStrategy.PreviewSlotForCarAsync(selectedSlotId);
		}
	}

	private void GoToBackHandler()
	{
		if (IsAnyTaskRunning())
		{
			return;
		}
		m_currentUniTask = UniTask.Create(async delegate
		{
			if (m_forceApplySlotId == -1)
			{
				await m_slotProcessStrategy.DiscardChangesAsync();
			}
			else
			{
				int carId = m_slotProcessStrategy.GetCarId();
				if (m_prefs.carSettings.GetActiveSlotId(carId) != m_forceApplySlotId)
				{
					await m_slotProcessStrategy.PreviewSlotForCarAsync(m_forceApplySlotId);
				}
				await m_slotProcessStrategy.ConfirmChangesAsync();
			}
			int carId2 = m_slotProcessStrategy.GetCarId();
			m_prefs.legacyCarSettings.Import(m_prefs.carSettings.GetActiveSlot(carId2));
		});
	}

	private void ApplyAndExitHandler()
	{
		if (IsAnyTaskRunning())
		{
			return;
		}
		m_currentUniTask = UniTask.Create(async delegate
		{
			int carId = m_slotProcessStrategy.GetCarId();
			int activeSlotId = m_prefs.carSettings.GetActiveSlotId(carId);
			int selectedSlotId = GetSelectedSlotId();
			if (activeSlotId != selectedSlotId && m_prefs.slotLock.IsSlotUnlocked(carId, selectedSlotId))
			{
				await m_slotProcessStrategy.PreviewSlotForCarAsync(selectedSlotId);
			}
			await m_slotProcessStrategy.ConfirmChangesAsync();
			m_prefs.legacyCarSettings.Import(m_prefs.carSettings.GetActiveSlot(carId));
		});
	}

	private void BuyDlcHandler()
	{
		Overlay.instance.ShowBuyDLCPage(DLCCode.CarPresetsUnlock);
	}

	private void CopySlotHandler()
	{
		if (!IsAnyTaskRunning())
		{
			m_currentUniTask = UniTask.Create(async delegate
			{
				int selectedSlotId = GetSelectedSlotId();
				int carId = m_slotProcessStrategy.GetCarId();
				PrefsCarSlotDataModel slot = m_prefs.carSettings.GetSlotById(carId, selectedSlotId).Copy();
				await AddAndApplySlot(slot);
				CopyPreviewIcon(carId, selectedSlotId, slot.slotId);
			});
		}
	}

	private void CreateNewSlotHandler()
	{
		if (IsAnyTaskRunning())
		{
			return;
		}
		m_currentUniTask = UniTask.Create(async delegate
		{
			int carId = m_slotProcessStrategy.GetCarId();
			PrefsCarSlotDataModel prefsCarSlotDataModel = PrefsCarSlotDataModel.CreateDefaultModel(carId);
			int dlcChassisId = GetDlcChassisId(carId);
			if (dlcChassisId != -1)
			{
				prefsCarSlotDataModel.visual.parts.Add(dlcChassisId);
			}
			await AddAndApplySlot(prefsCarSlotDataModel);
		});
	}

	private async UniTask AddAndApplySlot(PrefsCarSlotDataModel slot)
	{
		if (m_prefs.slotLock.GetUnlockedAndFreeSlots(slot.carId).Count <= 0)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.car_slots_exceed_error.Localized(), 3f);
			return;
		}
		string text = await InvokeRenameDialogAsync(GetAvailableCaption());
		if (text == null)
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.car_slots_empty_caption_error.Localized(), 3f);
			return;
		}
		slot.caption = text.Trim();
		m_prefs.carSettings.AddSlot(slot);
		if (slot.slotId != -1)
		{
			m_forceApplySlotId = slot.slotId;
			await m_slotProcessStrategy.PreviewSlotForCarAsync(slot.slotId);
			m_prefs.legacyCarSettings.Import(slot);
			FillSlotList();
		}
	}

	private void CopyPreviewIcon(int carId, int sourceSlotId, int targetSlotId)
	{
		Texture2D texture2D = IconStorageSystem.ReadLiveryPreview(carId, sourceSlotId);
		if (texture2D != null)
		{
			IconStorageSystem.SaveLiveryPreview(carId, targetSlotId, texture2D);
			FillSlotList();
		}
	}

	private int GetDlcChassisId(int carId)
	{
		List<CarPart> carBodies = CarPartDatabase.GetCarBodies(carId);
		int result = -1;
		foreach (CarPart item in carBodies)
		{
			if (!(item.type != "chassis") && m_carFactory.SpecialCarCanApplyPart(carId, item))
			{
				if (item.DLCCode == DLCCode.Unknown)
				{
					result = -1;
					break;
				}
				result = item.id;
			}
		}
		return result;
	}

	private void RemoveSlotHandler()
	{
		int carId = m_slotProcessStrategy.GetCarId();
		if (m_prefs.slotLock.GetUnlockedAndUsedSlots(carId).Count <= 1)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.car_slots_last_delete_error.Localized(), 3f);
		}
		else if (!IsAnyTaskRunning())
		{
			UIMessageBox.ShowQuestion(LocalizationKeys.window_caption_remove_slot.Localized(), null, LocalizationKeys.Cancel.Localized(), delegate
			{
				m_currentUniTask = ProcessRemove();
			}, LocalizationKeys.Yes.Localized());
		}
		async UniTask ProcessRemove()
		{
			int selectedSlotId = GetSelectedSlotId();
			if (selectedSlotId == m_forceApplySlotId)
			{
				m_forceApplySlotId = -1;
			}
			foreach (CarVilylsInfo.VinylPreset presets in m_prefs.carSettings.GetVinylsInfoForCar(carId).GetPresetsList())
			{
				IconStorageSystem.DeleteLiveryPreview(carId, presets.liveryId);
			}
			IconStorageSystem.DeleteLiveryPreview(carId, null);
			IconStorageSystem.DeleteLiveryPreview(carId, "dlc");
			m_prefs.carSettings.RemoveSlot(carId, selectedSlotId);
			selectedSlotId = m_prefs.carSettings.GetActiveSlotId(carId);
			await m_slotProcessStrategy.PreviewSlotForCarAsync(selectedSlotId);
			FillSlotList();
		}
	}

	private void RenameSlotHandler()
	{
		int selectedSlotId = GetSelectedSlotId();
		PrefsCarSlotDataModel slot = m_prefs.carSettings.GetSlotById(m_slotProcessStrategy.GetCarId(), selectedSlotId);
		InvokeRenameDialog(slot.caption, OnReceiveText);
		void OnReceiveText(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				GUICommonGodVoice.ShowText(LocalizationKeys.car_slots_empty_caption_error.Localized(), 3f);
			}
			else
			{
				slot.caption = text.Trim();
				slot.Save();
				if (!(this == null))
				{
					FillSlotList();
				}
			}
		}
	}

	private Texture2D RenderPreviewToTexture()
	{
		RenderTexture temporary = RenderTexture.GetTemporary(220, 124, 16, RenderTextureFormat.Default);
		Camera vinylPreviewCamera = Singleton<CustomCameraPresets>.instance.vinylPreviewCamera;
		vinylPreviewCamera.gameObject.SetActive(value: true);
		vinylPreviewCamera.targetTexture = temporary;
		vinylPreviewCamera.Render();
		Texture2D texture2D = new Texture2D(220, 124, TextureFormat.RGB24, mipChain: false);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = temporary;
		texture2D.ReadPixels(new Rect(0f, 0f, 220f, 124f), 0, 0);
		texture2D.Apply();
		RenderTexture.active = active;
		RenderTexture.ReleaseTemporary(temporary);
		vinylPreviewCamera.gameObject.SetActive(value: false);
		vinylPreviewCamera.targetTexture = null;
		return texture2D;
	}

	private void SyncLiveryBetweenSlots()
	{
		int carId = m_slotProcessStrategy.GetCarId();
		int activeSlotId = m_prefs.carSettings.GetActiveSlotId(carId);
		CarVilylsInfo vinylsInfoForCar = m_prefs.carSettings.GetVinylsInfoForCar(carId);
		foreach (int usedSlot in m_prefs.carSettings.GetUsedSlots(carId))
		{
			if (usedSlot == activeSlotId)
			{
				continue;
			}
			PrefsCarSlotDataModel slotById = m_prefs.carSettings.GetSlotById(carId, usedSlot);
			string vinylPresetLiveryId = slotById.visual.vinylPresetLiveryId;
			if (vinylPresetLiveryId == null)
			{
				continue;
			}
			CarVilylsInfo.VinylPreset preset = vinylsInfoForCar.GetPreset(vinylPresetLiveryId);
			if (preset == null)
			{
				slotById.visual.vinylLayers.Clear();
				slotById.visual.vinylPresetLiveryId = null;
			}
			else
			{
				if (!IsLiveryDifferent(slotById.visual.vinylLayers, preset.layers))
				{
					continue;
				}
				slotById.visual.vinylLayers = new List<VinylLayer>(preset.layers);
			}
			slotById.Save();
			IconStorageSystem.DeleteLiveryPreview(carId, usedSlot);
		}
	}

	private bool IsLiveryDifferent(List<VinylLayer> firstLivery, List<VinylLayer> secondLivery)
	{
		if (firstLivery == null && secondLivery == null)
		{
			return false;
		}
		if ((firstLivery == null && secondLivery != null) || (firstLivery != null && secondLivery == null))
		{
			return true;
		}
		if (firstLivery.Count != secondLivery.Count)
		{
			return true;
		}
		for (int i = 0; i < firstLivery.Count; i++)
		{
			if (!firstLivery[i].LayerEquals(secondLivery[i]))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsInputBlocked()
	{
		ControllerType type = GameInput.InputManager.instance.lastController.type;
		if ((type == ControllerType.Keyboard || type == ControllerType.Mouse) && UIUtils.IsPointerOverGameObject())
		{
			return true;
		}
		if (!Input.GetMouseButton(0))
		{
			return false;
		}
		if (!Physics.Raycast(m_uiCamera.ScreenPointToRay(Input.mousePosition), out var hitInfo, float.PositiveInfinity))
		{
			return false;
		}
		return hitInfo.collider.gameObject.layer == 13;
	}

	public void InitCamera()
	{
		if (m_inputController == null)
		{
			m_inputController = new CameraInputController();
		}
		if (m_uiCamera == null)
		{
			m_uiCamera = Camera.main;
		}
		if (m_uiCamera == null)
		{
			m_uiCamera = UnityEngine.Object.FindFirstObjectByType<Camera>();
		}
		if (m_cameraPhotoSession == null)
		{
			m_cameraPhotoSession = UnityEngine.Object.FindFirstObjectByType<CameraPhotoSession>();
		}
	}

	public void ConfigureCamera(RaceCar target, Vector3 position, float height)
	{
		if (!(m_cameraPhotoSession == null) && !m_cameraConfigured)
		{
			CameraSwitch.instance.SetCar(target);
			CameraSwitch.instance.ActivatePhotoSessionCamera();
			m_cameraPhotoSession.ChangeTarget(CameraPhotoSession.PhotoSessionCameraTarget.Center);
			m_cameraPhotoSession.SetLimits(7f, -5f, 60f);
			m_cameraPhotoSession.SetHeight(height);
			m_cameraPhotoSession.SetVerticalTilt(7f);
			Vector3 vector = target.getTransform.position - position;
			float num = Mathf.Sqrt(vector.x * vector.x + vector.z * vector.z);
			if (num > 0f)
			{
				m_cameraPhotoSession.SetCameraPosition(Mathf.Atan2(vector.x, vector.z) * 57.29578f, Mathf.Atan(vector.y / num) * 57.29578f, vector.magnitude);
			}
			m_cameraConfigured = true;
		}
	}

	public void ResetCamera()
	{
		if (m_cameraPhotoSession != null)
		{
			m_cameraPhotoSession.SetHeight(0f);
			m_cameraPhotoSession.SetVerticalTilt(0f);
		}
	}

	private void UpdateCamera()
	{
		if (!(m_uiCamera == null) && !(m_cameraPhotoSession == null))
		{
			float deltaXRotation = float.Epsilon;
			float deltaYRotation = 0f;
			float deltaDistance = 0f;
			if (!IsInputBlocked())
			{
				m_inputController?.CalcStandalone(Time.unscaledDeltaTime, ref deltaXRotation, ref deltaYRotation, ref deltaDistance);
				GetGamepadZoomDelta(ref deltaDistance);
			}
			m_cameraPhotoSession.UpdateCameraPositionWithoutLerp(deltaXRotation, deltaYRotation, deltaDistance);
		}
	}

	private void GetGamepadZoomDelta(ref float zoomDelta)
	{
		zoomDelta += GameInput.InputManager.instance.GetActionValue(ActionType.ZoomOut) * 2f * Time.unscaledDeltaTime;
		zoomDelta -= GameInput.InputManager.instance.GetActionValue(ActionType.ZoomIn) * 2f * Time.unscaledDeltaTime;
	}

	private void SetCaptionForSlotsIfNeed()
	{
		int carId = m_slotProcessStrategy.GetCarId();
		List<int> usedSlots = m_prefs.carSettings.GetUsedSlots(carId);
		for (int i = 0; i < usedSlots.Count; i++)
		{
			PrefsCarSlotDataModel slotById = m_prefs.carSettings.GetSlotById(carId, usedSlots[i]);
			if (string.IsNullOrWhiteSpace(slotById.caption))
			{
				slotById.caption = GetAvailableCaption();
				slotById.Save();
			}
		}
	}

	private string GetAvailableCaption()
	{
		for (int i = 0; i < 10; i++)
		{
			string text = $"Preset {i + 1}";
			if (IsCaptionFree(text))
			{
				return text;
			}
		}
		return $"Preset {string.Empty}";
	}

	private bool IsCaptionFree(string caption)
	{
		int carId = m_slotProcessStrategy.GetCarId();
		List<int> usedSlots = m_prefs.carSettings.GetUsedSlots(carId);
		for (int i = 0; i < usedSlots.Count; i++)
		{
			PrefsCarSlotDataModel slotById = m_prefs.carSettings.GetSlotById(carId, usedSlots[i]);
			if (slotById.caption != null && string.Equals(slotById.caption, caption, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsAnyTaskRunning()
	{
		if (m_currentUniTask.HasValue)
		{
			ref UniTask? currentUniTask = ref m_currentUniTask;
			if (!currentUniTask.HasValue)
			{
				return false;
			}
			return currentUniTask.GetValueOrDefault().Status == UniTaskStatus.Pending;
		}
		return false;
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	private void NextItem()
	{
		m_presetsList.Selection(m_presetsList.GetSelectedIndex() + 1, selected: true);
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	private void PrevItem()
	{
		m_presetsList.Selection(m_presetsList.GetSelectedIndex() - 1, selected: true);
	}

	private void InvokeRenameDialog(string defaultText, Action<string> onReceiveText)
	{
		if (Overlay.instance is SteamOverlay steamOverlay && GameManager.IsLinuxOsPlatform())
		{
			steamOverlay.ShowOverlayKeyboard(LocalizationKeys.car_slots_write_caption.Localized(), defaultText, 46);
			return;
		}
		UIMessageBox.ShowInputDialog(LocalizationKeys.car_slots_write_caption.Localized(), defaultText, null, delegate
		{
			onReceiveText.SafeCall(null);
		}, LocalizationKeys.Cancel.Localized(), onReceiveText, LocalizationKeys.Ok.Localized(), 46);
	}

	private async UniTask<string> InvokeRenameDialogAsync(string defaultText)
	{
		AutoResetUniTaskCompletionSource<string> tokenResult = AutoResetUniTaskCompletionSource<string>.Create();
		InvokeRenameDialog(defaultText, delegate(string text)
		{
			tokenResult.TrySetResult(text);
		});
		return await tokenResult.Task;
	}

	private BaseSlotProcessStrategy GetStrategyOfState()
	{
		BaseState mainCurrent = States.mainCurrent;
		if (!(mainCurrent is XDSRaceModeState))
		{
			if (!(mainCurrent is SyncNetFreerideRaceModeState))
			{
				if (!(mainCurrent is FreerideRaceModeState))
				{
					if (mainCurrent is GarageGUIState)
					{
						return new GarageSlotProcessStrategy();
					}
					return null;
				}
				return new FreerideSlotProcessStrategy();
			}
			return new NetworkFreerideSlotProcessStrategy();
		}
		return new SingleXDSSlotProcessStrategy();
	}
}
