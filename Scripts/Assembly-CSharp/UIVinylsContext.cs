using System;
using System.Collections;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarModelSystem;
using DI;
using GameInput;
using GameOverlay;
using UnityEngine;
using Utils.VinylLayerExtensions;
using VinylSystem;

public class UIVinylsContext : BaseBindingBehaviourTarget
{
	public const int SlotsCountLimit = 10;

	private const float CameraZoomMinInVinyls = 4f;

	private const float CameraZoomMinInGarage = 4f;

	private const float ApplyLiveryDelay = 0.45f;

	[SerializeField]
	private Vector2Int m_textureSize = new Vector2Int(220, 124);

	[ContextDependency]
	private GarageCar3dViewContext m_car3dView;

	[Dependency]
	private GamePrefs m_prefs;

	[Dependency]
	private CarVisualManager m_carVisualManager;

	private static bool s_liveryEditDontRemind;

	private CameraRotation m_cameraRotation;

	private ListProperty m_presetsList = new ListProperty();

	private StringProperty m_slotsCount = new StringProperty();

	private VinylWrapEditor m_vinylsEditor;

	private Coroutine m_renderPreviewCoroutine;

	private float m_cameraLastDistance;

	private List<ActionHintData> m_bottomActions = new List<ActionHintData>(6);

	private string m_liveryIdForRender;

	private int m_lastCarId = -1;

	private string m_liveryIdOnEnter;

	private float m_lastTimeOfSelectPreset;

	private void Awake()
	{
		m_presetsList.OnSelectionChanged += OnSelectionChanged_PresetsList;
		m_presetsList.OnClick += OnClick_PresetsList;
	}

	private void Update()
	{
		HandleApplyLiveryBySelection();
	}

	private void HandleApplyLiveryBySelection()
	{
		if (!(Time.realtimeSinceStartup < m_lastTimeOfSelectPreset + 0.45f) && m_presetsList.GetSelected() is VinylPresetElementData { preset: not null } vinylPresetElementData)
		{
			m_lastTimeOfSelectPreset = Time.realtimeSinceStartup;
			ApplyPreset(vinylPresetElementData.preset);
		}
	}

	private void ForceApplySelectedLivery()
	{
		m_lastTimeOfSelectPreset = 0f;
		HandleApplyLiveryBySelection();
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		base.owner.Preload<UIVinylsEditingLayersContext>();
		if (m_cameraRotation == null)
		{
			m_cameraRotation = CameraRotation.instance;
		}
		if (m_vinylsEditor == null)
		{
			m_vinylsEditor = VinylSystemKernel.instance.editor;
		}
		RemoveEmptySlots();
		SetActiveGarageCamera(active: false);
		ShowTopPanel(ETopPanelState.Title, LocalizationKeys.vinyls_title.Localized());
		m_car3dView.Set3DModelVisible(value: true);
		FillList();
		ShowHints();
		if (m_cameraRotation != null)
		{
			m_cameraRotation.useZoom = true;
			m_cameraRotation.useFovZoom = false;
			m_cameraRotation.ResetFov();
		}
		m_lastTimeOfSelectPreset = 0f;
		m_liveryIdOnEnter = GetCurrentLiveryId();
	}

	protected override void OnDeactivate()
	{
		if (m_renderPreviewCoroutine != null)
		{
			StopCoroutine(m_renderPreviewCoroutine);
			m_renderPreviewCoroutine = null;
		}
		ClearList();
		HideTopPanel();
	}

	private void ShowHints()
	{
		m_bottomActions.Clear();
		m_bottomActions.Add(new ActionHintData
		{
			hint = LocalizationKeys.nav_hint_goto_back.Localized(),
			type = ActionType.NavCancelBtnB,
			action = GoToBack
		});
		if (!(m_presetsList.GetSelected() is VinylPresetElementData vinylPresetElementData))
		{
			ShowBottomPanel(m_bottomActions);
			return;
		}
		bool value = vinylPresetElementData.isPlus.value;
		bool flag = vinylPresetElementData.preset != null && !vinylPresetElementData.preset.locked;
		bool value2 = vinylPresetElementData.isEmpty.value;
		if (value || flag)
		{
			m_bottomActions.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_select.Localized(),
				type = ActionType.NavSelectBtnA,
				action = Selection
			});
		}
		if (!value && flag && !value2)
		{
			m_bottomActions.Add(new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_remove_slot.Localized(),
				type = ActionType.NavAdditional1BtnY,
				action = RemoveSlot
			});
			if (CanBeCloned(vinylPresetElementData))
			{
				m_bottomActions.Add(new ActionHintData
				{
					hint = LocalizationKeys.generic_copy.Localized(),
					type = ActionType.NavAdditional0BtnX,
					action = CloneSlot
				});
			}
		}
		ShowBottomPanel(m_bottomActions);
	}

	private string GetCurrentLiveryId()
	{
		int lastCarId = m_prefs.globals.lastCarId;
		return m_prefs.carSettings.GetVisualForCar(lastCarId).vinylPresetLiveryId;
	}

	private bool CanBeCloned(VinylPresetElementData data)
	{
		if (data == null)
		{
			return false;
		}
		return data.preset?.ownerInfo.playerId.Equals(Overlay.instance.player.id) ?? false;
	}

	private void CloneSlot()
	{
		CarVilylsInfo vinylsInfoForCar = m_prefs.carSettings.GetVinylsInfoForCar(m_lastCarId);
		if (vinylsInfoForCar.GetPresetsList().Count >= 10)
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.liveries_slots_exceed.Localized());
		}
		else
		{
			if (!(m_presetsList.GetSelected() is VinylPresetElementData vinylPresetElementData) || !CanBeCloned(vinylPresetElementData))
			{
				return;
			}
			CarVilylsInfo.VinylPreset vinylPreset = vinylsInfoForCar.CreatePreset();
			List<VinylLayer> list = new List<VinylLayer>();
			foreach (VinylLayer layer in vinylPresetElementData.preset.layers)
			{
				list.Add(layer.Copy());
			}
			vinylPreset.layers = list;
			vinylsInfoForCar.AddPreset(vinylPreset);
			ApplyPreset(vinylPreset);
			FillList(force: true);
		}
	}

	public void FillList(bool force = false)
	{
		int lastCarId = m_prefs.globals.lastCarId;
		CarVisualManager.VisualSettings vs = m_prefs.carSettings.GetVisualForCar(lastCarId);
		CarVilylsInfo vinylsInfoForCar = m_prefs.carSettings.GetVinylsInfoForCar(lastCarId);
		List<CarVilylsInfo.VinylPreset> presetsList = vinylsInfoForCar.GetPresetsList();
		if (vs.vinylLayers.Count > 0 && (string.IsNullOrEmpty(vs.vinylPresetLiveryId) || presetsList.Count == 0))
		{
			CarVilylsInfo.VinylPreset vinylPreset = vinylsInfoForCar.CreatePreset();
			vinylsInfoForCar.AddPreset(vinylPreset);
			vinylPreset.layers.Clear();
			vinylPreset.layers.AddRange(vs.vinylLayers);
			vs.vinylPresetLiveryId = vinylPreset.liveryId;
			vs.vinylPresetId = vinylPreset.id;
			m_carVisualManager.SaveVisualSettingsForCar(lastCarId, vs);
			presetsList = vinylsInfoForCar.GetPresetsList();
		}
		presetsList.Sort((CarVilylsInfo.VinylPreset x1, CarVilylsInfo.VinylPreset x2) => x2.time.CompareTo(x1.time) + x2.locked.CompareTo(x1.locked));
		bool flag = presetsList.Count < 10;
		int num = presetsList.Count + (flag ? 1 : 0);
		bool flag2 = m_presetsList.count != num + 1;
		bool flag3 = false;
		if (!flag2)
		{
			for (int num2 = 0; num2 < m_presetsList.count; num2++)
			{
				if (m_presetsList[num2] is VinylPresetElementData { preset: not null } vinylPresetElementData)
				{
					CarVilylsInfo.VinylPreset preset = vinylsInfoForCar.GetPreset(vinylPresetElementData.preset.liveryId);
					if (preset == null || preset.time != vinylPresetElementData.preset.time)
					{
						flag3 = true;
						break;
					}
				}
			}
		}
		if (force || m_lastCarId != lastCarId || flag2 || flag3)
		{
			m_presetsList.Clear();
			if (flag)
			{
				m_presetsList.Add(new VinylPresetElementData(null));
			}
			for (int num3 = 0; num3 < presetsList.Count; num3++)
			{
				if (presetsList[num3].locked)
				{
					m_presetsList.Add(new VinylPresetElementData(presetsList[num3]));
				}
			}
			CarVilylsInfo.VinylPreset vinylPreset2 = new CarVilylsInfo.VinylPreset();
			vinylPreset2.liveryId = null;
			vinylPreset2.SetOldId(-1);
			m_presetsList.Add(new VinylPresetElementData(vinylPreset2));
			for (int num4 = 0; num4 < presetsList.Count; num4++)
			{
				if (!presetsList[num4].locked)
				{
					m_presetsList.Add(new VinylPresetElementData(presetsList[num4]));
				}
			}
		}
		else if (flag)
		{
			m_presetsList.ForEach(delegate(VinylPresetElementData p)
			{
				p.Refresh();
			});
		}
		else
		{
			for (int num5 = 0; num5 < 10; num5++)
			{
				m_presetsList.GetByIndex<VinylPresetElementData>(num5).Init(presetsList[num5]);
			}
		}
		int num6 = m_presetsList.IndexOf((VinylPresetElementData x) => x.preset != null && x.preset.liveryId == vs.vinylPresetLiveryId);
		if (num6 != -1)
		{
			m_presetsList.Selection(num6, selected: true);
		}
		else
		{
			m_presetsList.Selection(0, selected: true);
		}
		m_slotsCount.value = $"{presetsList.Count} / {10}";
		m_renderPreviewCoroutine = StartCoroutine(RenderPreview());
		m_lastCarId = lastCarId;
	}

	private void ClearList()
	{
		for (int i = 0; i < m_presetsList.count; i++)
		{
			if (m_presetsList[i] is VinylPresetElementData { preset: not null } vinylPresetElementData && vinylPresetElementData.m_preview.value != null)
			{
				UnityEngine.Object.Destroy(vinylPresetElementData.m_preview.value);
				vinylPresetElementData.m_preview.value = null;
			}
		}
		m_presetsList.Clear();
	}

	private void SetActiveGarageCamera(bool active)
	{
		if (!m_cameraRotation.useFovZoom)
		{
			m_cameraLastDistance = m_cameraRotation.fov;
		}
		m_cameraRotation.ResetFov();
		m_cameraRotation.disableReturnToIdle = !active;
		m_cameraRotation.canIdle = active;
		if (active)
		{
			m_cameraRotation.SetFov(m_cameraLastDistance);
			m_cameraRotation.SetIdle();
		}
		else
		{
			m_cameraRotation.Stop();
		}
	}

	private void SetActiveGarageCameraZoom(bool active)
	{
		if (!m_cameraRotation.useZoom)
		{
			m_cameraLastDistance = m_cameraRotation.distanceFromCenter;
		}
		m_cameraRotation.distanceFromCenterMin = ((!active) ? 4f : 4f);
		m_cameraRotation.disableReturnToIdle = !active;
		m_cameraRotation.useZoom = !active;
		m_cameraRotation.canIdle = active;
		if (active)
		{
			m_cameraRotation.SetDistanceFromCenter(m_cameraLastDistance);
			m_cameraRotation.SetIdle();
		}
		else
		{
			m_cameraRotation.Stop();
		}
	}

	private void ClearPreset()
	{
		int lastCarId = m_prefs.globals.lastCarId;
		CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(lastCarId);
		visualForCar.vinylPresetLiveryId = null;
		visualForCar.vinylPresetId = -1;
		visualForCar.vinylLayers.Clear();
		m_carVisualManager.SaveVisualSettingsForCar(lastCarId, visualForCar);
		try
		{
			m_vinylsEditor.StartEditLivery(m_car3dView.GetRaceCar().carModel);
			m_vinylsEditor.RemoveAllLayers();
			m_vinylsEditor.CompleteEditLivery();
		}
		catch (Exception ex)
		{
			GameLog.Print(AlertLevel.Error, "System", ex.ToString());
		}
	}

	private void RemoveLiveryFromSlots(int carId, string liveryId)
	{
		int activeSlotId = m_prefs.carSettings.GetActiveSlotId(carId);
		foreach (int usedSlot in m_prefs.carSettings.GetUsedSlots(carId))
		{
			PrefsCarSlotDataModel slotById = m_prefs.carSettings.GetSlotById(carId, usedSlot);
			if (!(slotById.visual.vinylPresetLiveryId != liveryId))
			{
				slotById.visual.vinylPresetLiveryId = null;
				slotById.visual.vinylPresetId = -1;
				slotById.visual.vinylLayers.Clear();
				if (usedSlot == activeSlotId)
				{
					m_carVisualManager.SaveVisualSettingsForCar(carId, slotById.visual);
				}
				else
				{
					slotById.Save();
				}
				m_prefs.carSettings.SetActiveSlotId(carId, usedSlot);
				IconStorageSystem.DeleteLiveryPreview(carId, liveryId);
				m_prefs.carSettings.SetActiveSlotId(carId, activeSlotId);
			}
		}
	}

	private void GoToBack()
	{
		UIVinylsDecalSelectionContext uIVinylsDecalSelectionContext = base.owner.Get<UIVinylsDecalSelectionContext>();
		if (uIVinylsDecalSelectionContext != null)
		{
			uIVinylsDecalSelectionContext.DisposeCategories();
		}
		ResourcesWrapper.UnloadResources("vinyl_editor");
		SetActiveGarageCamera(active: true);
		ForceApplySelectedLivery();
		Hide();
		if (m_liveryIdOnEnter != GetCurrentLiveryId())
		{
			TimeController.AddCallback(0.5f, m_car3dView.IconGenerate);
		}
		base.owner.Push<UIDesignMenuContext>();
	}

	private void GotoEditLivery()
	{
		if ((m_presetsList.GetSelected() as VinylPresetElementData).isEmpty.value && IsSlotLimitExceeded())
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.liveries_slots_exceed.Localized());
			return;
		}
		ForceApplySelectedLivery();
		Hide();
		base.owner.Push<UIVinylsEditingLayersContext>();
	}

	private void Selection()
	{
		bool flag = IsSlotLimitExceeded();
		VinylPresetElementData vinylPresetElementData = m_presetsList.GetSelected() as VinylPresetElementData;
		if (vinylPresetElementData.isPlus.value)
		{
			if (flag)
			{
				return;
			}
			ClearPreset();
		}
		else if (vinylPresetElementData.preset.locked)
		{
			return;
		}
		if (s_liveryEditDontRemind || !IsLiveryUsedBetweenSlots(vinylPresetElementData.preset?.liveryId, out var links))
		{
			GotoEditLivery();
			return;
		}
		string title = LocalizationKeys.car_slots_livery_already_in_use_edit_warning.LocalizedFormat(links);
		List<DialogButtonData> list = new List<DialogButtonData>
		{
			new DialogButtonData
			{
				text = LocalizationKeys.nav_hint_continue.Localized(),
				action = GotoEditLivery
			}
		};
		if (vinylPresetElementData.preset != null && !vinylPresetElementData.preset.downloaded)
		{
			list.Add(new DialogButtonData
			{
				text = LocalizationKeys.generic_copy.Localized(),
				action = delegate
				{
					if (IsSlotLimitExceeded())
					{
						GUICommonGodVoice.ShowText(LocalizationKeys.liveries_slots_exceed.Localized());
					}
					else
					{
						CloneSlot();
						GotoEditLivery();
					}
				}
			});
		}
		list.Add(new DialogButtonData
		{
			text = LocalizationKeys.car_slots_dont_remind.Localized(),
			action = delegate
			{
				s_liveryEditDontRemind = true;
				GotoEditLivery();
			}
		});
		UIMessageBox.ShowDialog(title, null, list.ToArray());
	}

	private bool IsLiveryUsedBetweenSlots(string liveryId, out int links)
	{
		links = 0;
		if (liveryId == null)
		{
			return false;
		}
		List<int> usedSlots = m_prefs.carSettings.GetUsedSlots(m_lastCarId);
		for (int i = 0; i < usedSlots.Count; i++)
		{
			if (m_prefs.carSettings.GetSlotById(m_lastCarId, usedSlots[i]).visual.vinylPresetLiveryId == liveryId)
			{
				links++;
			}
		}
		return links > 1;
	}

	private bool IsSlotLimitExceeded()
	{
		return m_prefs.carSettings.GetVinylsInfoForCar(m_lastCarId).GetPresetsList().Count >= 10;
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

	private void RemoveSlot()
	{
		int selectedIndex = m_presetsList.GetSelectedIndex();
		VinylPresetElementData vinylPresetElementData = m_presetsList.GetByIndex(selectedIndex) as VinylPresetElementData;
		if (!vinylPresetElementData.isPlus.value && !vinylPresetElementData.isEmpty.value)
		{
			UIMessageBox.ShowQuestion(IsLiveryUsedBetweenSlots(vinylPresetElementData.preset.liveryId, out var links) ? LocalizationKeys.car_slots_livery_already_in_use_delete_warning.LocalizedFormat(links) : LocalizationKeys.window_caption_remove_slot.Localized(), delegate
			{
			}, LocalizationKeys.Cancel.Localized(), delegate
			{
				RemoveSlot(selectedIndex);
			}, LocalizationKeys.Yes.Localized());
		}
	}

	private void RemoveSlot(int selectedIndex)
	{
		VinylPresetElementData vinylPresetElementData = m_presetsList.GetByIndex(selectedIndex) as VinylPresetElementData;
		m_presetsList.Remove(vinylPresetElementData);
		int lastCarId = m_prefs.globals.lastCarId;
		CarVilylsInfo vinylsInfoForCar = m_prefs.carSettings.GetVinylsInfoForCar(lastCarId);
		vinylsInfoForCar.RemovePreset(vinylPresetElementData.preset);
		List<CarVilylsInfo.VinylPreset> presetsList = vinylsInfoForCar.GetPresetsList();
		if (presetsList.Count == 9)
		{
			m_presetsList.Insert(0, new VinylPresetElementData(null));
		}
		m_slotsCount.value = $"{presetsList.Count} / {10}";
		if (selectedIndex == 0)
		{
			selectedIndex++;
		}
		else if (selectedIndex == m_presetsList.count)
		{
			selectedIndex--;
		}
		RemoveLiveryFromSlots(lastCarId, vinylPresetElementData.preset.liveryId);
		ClearPreset();
		m_presetsList.Selection(selectedIndex, selected: true);
		ForceApplySelectedLivery();
	}

	private void OnClick_PresetsList(BaseListElementData obj)
	{
		if (!obj.selected)
		{
			m_presetsList.Selection(m_presetsList.IndexOf(obj), selected: true);
		}
		else
		{
			Selection();
		}
	}

	private void OnSelectionChanged_PresetsList(BaseListElementData obj)
	{
		if (obj.selected)
		{
			StartCoroutine(Corutine_ScrollTo(m_presetsList.IndexOf(obj)));
			m_lastTimeOfSelectPreset = Time.realtimeSinceStartup;
			ShowHints();
		}
	}

	private IEnumerator Corutine_ScrollTo(int index)
	{
		yield return null;
		m_presetsList.ScrollTo(index);
	}

	private void ApplyPreset(CarVilylsInfo.VinylPreset preset)
	{
		int lastCarId = m_prefs.globals.lastCarId;
		CarVisualManager.VisualSettings visualForCar = m_prefs.carSettings.GetVisualForCar(lastCarId);
		if (preset.liveryId != visualForCar.vinylPresetLiveryId)
		{
			m_vinylsEditor.StartEditLivery(m_car3dView.GetRaceCar().carModel);
			m_vinylsEditor.ReplaceAllLayers(preset.layers);
			m_vinylsEditor.CompleteEditLivery();
			visualForCar.vinylPresetId = preset.id;
			visualForCar.vinylPresetLiveryId = preset.liveryId;
			visualForCar.vinylLayers.Clear();
			visualForCar.vinylLayers.AddRange(preset.layers);
			m_carVisualManager.SaveVisualSettingsForCar(lastCarId, visualForCar);
		}
	}

	public void MarkLiveryForRenderPreview(string liveryId)
	{
		m_liveryIdForRender = liveryId;
	}

	private IEnumerator RenderPreview()
	{
		if (m_presetsList.count <= 1)
		{
			yield break;
		}
		yield return WaitBakeVinyls();
		CarModel carModel = m_car3dView.GetRaceCar().carModel;
		int carId = m_prefs.globals.lastCarId;
		CarVisualManager.VisualSettings vs = m_prefs.carSettings.GetVisualForCar(carId);
		Camera previewCamera = Singleton<CustomCameraPresets>.instance.vinylPreviewCamera;
		string lastPresetLiveryId = vs.vinylPresetLiveryId;
		bool lockIsSetted = false;
		try
		{
			int num;
			for (int i = 0; i < m_presetsList.count; num = i + 1, i = num)
			{
				VinylPresetElementData data = m_presetsList.GetByIndex<VinylPresetElementData>(i);
				if (data.preset == null)
				{
					continue;
				}
				if (m_liveryIdForRender != null && m_liveryIdForRender == data.preset.liveryId)
				{
					m_liveryIdForRender = null;
				}
				else
				{
					Texture2D texture2D = IconStorageSystem.ReadLiveryPreview(carId, data.preset.liveryId);
					if (texture2D != null)
					{
						data.m_preview.value = texture2D;
						continue;
					}
				}
				bool oldAsyncLoading = VinylSystemConfig.config.asyncLoading;
				VinylSystemConfig.config.asyncLoading = true;
				bool isCurrentPresset = data.preset.liveryId == vs.vinylPresetLiveryId;
				if (!isCurrentPresset)
				{
					if (!lockIsSetted)
					{
						lockIsSetted = true;
						UILockInputContext.SetLock(base.owner);
					}
					m_vinylsEditor.StartEditLivery(carModel);
					m_vinylsEditor.ReplaceAllLayers(data.preset.layers);
					lastPresetLiveryId = data.preset.liveryId;
				}
				while (carModel.IsLoading())
				{
					yield return null;
				}
				previewCamera.gameObject.SetActive(value: true);
				Texture2D texture2D2 = RenderPreviewToTexture(previewCamera);
				previewCamera.gameObject.SetActive(value: false);
				data.m_preview.value = texture2D2;
				IconStorageSystem.SaveLiveryPreview(carId, data.preset.liveryId, texture2D2);
				if (!isCurrentPresset)
				{
					m_vinylsEditor.ReplaceAllLayers(vs.vinylLayers);
					m_vinylsEditor.CompleteEditLivery();
				}
				VinylSystemConfig.config.asyncLoading = oldAsyncLoading;
				yield return null;
			}
		}
		finally
		{
			UIVinylsContext uIVinylsContext = this;
			if (lastPresetLiveryId != vs.vinylPresetLiveryId)
			{
				carModel.livery.ReplaceLayers(vs.vinylLayers.Crop(), forceValidate: false);
				uIVinylsContext.m_vinylsEditor.CompleteEditLivery();
			}
			if (lockIsSetted)
			{
				UILockInputContext.DisableLock();
			}
		}
		m_renderPreviewCoroutine = null;
	}

	private Texture2D RenderPreviewToTexture(Camera renderCamera)
	{
		RenderTexture renderTexture = (renderCamera.targetTexture = RenderTexture.GetTemporary(m_textureSize.x, m_textureSize.y, 16, RenderTextureFormat.Default));
		renderCamera.Render();
		Texture2D texture2D = new Texture2D(m_textureSize.x, m_textureSize.y, TextureFormat.RGB24, mipChain: false);
		RenderTexture active = RenderTexture.active;
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(new Rect(0f, 0f, m_textureSize.x, m_textureSize.y), 0, 0);
		texture2D.Apply();
		RenderTexture.active = active;
		renderCamera.targetTexture = null;
		RenderTexture.ReleaseTemporary(renderTexture);
		return texture2D;
	}

	private IEnumerator WaitBakeVinyls()
	{
		do
		{
			yield return null;
		}
		while (m_car3dView.GetRaceCar() == null || !m_car3dView.GetRaceCar().IsLiveryReady);
	}

	private void RemoveEmptySlots()
	{
		int lastCarId = m_prefs.globals.lastCarId;
		CarVilylsInfo vinylsInfoForCar = m_prefs.carSettings.GetVinylsInfoForCar(lastCarId);
		foreach (CarVilylsInfo.VinylPreset presets in vinylsInfoForCar.GetPresetsList())
		{
			if (presets != null && presets.layers.Count <= 0)
			{
				vinylsInfoForCar.RemovePreset(presets);
			}
		}
	}
}
