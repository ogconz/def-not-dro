using System;
using System.Collections;
using System.Threading;
using Binding;
using Binding.Base;
using Cysharp.Threading.Tasks;
using DB.Meta;
using GameInput;
using GameOverlay;
using UnityEngine;
using VinylSystem;

public class UIVinylsDecalSelectionContext : BaseBindingBehaviourTarget
{
	private const float CategoryLoadPeriod = 0.3f;

	private VinylWrapEditor m_vinylEditor;

	private VinylLayer m_selectedLayer;

	private Camera m_camera;

	[SerializeField]
	private VinylLayersGrid m_vinylsList;

	[SerializeField]
	private VinylCategoryList m_categorysList;

	private BoolProperty m_showLoadPanel = new BoolProperty();

	private CancellationTokenSource m_safeAutoHidePanelCts;

	private GamePrefs m_prefs;

	private float m_categorySelectTime;

	private bool m_isCategoryloaded;

	private bool m_carVinylLayerSelected;

	private int m_vinylsCount => m_vinylsList.GetCount();

	private int m_categoriesCount => m_categorysList.GetCount();

	private void Awake()
	{
		VinylCategoryList categorysList = m_categorysList;
		categorysList.onClick = (Action<int>)Delegate.Combine(categorysList.onClick, new Action<int>(SelectCategory));
		VinylLayersGrid vinylsList = m_vinylsList;
		vinylsList.onClick = (Action<int>)Delegate.Combine(vinylsList.onClick, new Action<int>(SelectVinylAndCategory));
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		m_vinylEditor = VinylSystemKernel.instance.editor;
		m_isCategoryloaded = false;
		ShowLoadPanel(show: false);
		if (m_camera == null)
		{
			m_camera = Camera.main;
		}
		ShowTopPanel(ETopPanelState.Title, string.Empty);
		HideTopPanel();
		base.owner.Get<UIBottomPanelContext>().BringForwardByDepth();
		StartCoroutine(FillCategoriesList());
	}

	protected override void OnDeactivate()
	{
		m_carVinylLayerSelected = false;
		base.OnDeactivate();
	}

	public void DisposeCategories()
	{
		UnloadAllCategories();
		m_vinylsList.Clear();
		m_categorysList.Clear();
	}

	private void UpdateBottomHotkeys(int index)
	{
		VinylCategoryItem item = m_categorysList.GetItem(index);
		UpdateBottomPanel(item.dlcCode != DLCCode.Unknown && !Overlay.instance.IsDLCInstalled(item.dlcCode));
	}

	private void UpdateBottomPanel(bool buyBtn = false)
	{
		ShowBottomPanel(new ActionHintData[2]
		{
			new ActionHintData
			{
				hint = LocalizationKeys.nav_hint_goto_back.Localized(),
				type = ActionType.NavCancelBtnB,
				action = GoToBack
			},
			new ActionHintData
			{
				hint = (buyBtn ? LocalizationKeys.nav_hint_buy.Localized() : LocalizationKeys.nav_hint_select.Localized()),
				type = ActionType.NavSelectBtnA,
				action = Select
			}
		});
	}

	private void FillVinylsList(VinylCategoryItem category)
	{
		m_vinylsList.Clear();
		m_vinylsList.AddRange(category.GetVinylsElements());
		m_vinylsList.RecalcList();
		int num = 0;
		bool flag = false;
		if (!m_carVinylLayerSelected)
		{
			num = base.owner.Get<UIVinylsEditingLayersContext>().selectedRawId;
			VinylMeta vinylById = Singleton<VinylMetaProvider>.instance.meta.GetVinylById(num);
			flag = vinylById == null || vinylById.availablePlatform == AvailablePlatform.None;
		}
		if (num <= 0 || flag)
		{
			TimeController.StartNow(CoroutineSelectVinyl(0));
			return;
		}
		for (int i = 0; i < m_vinylsCount; i++)
		{
			VinylLayerItem item = m_vinylsList.GetItem(i);
			if (item != null && item.id == num)
			{
				SelectVinylByIndexOnLoadedCategoryAsync(i).Forget();
				m_carVinylLayerSelected = true;
				break;
			}
		}
		if (!m_carVinylLayerSelected)
		{
			SelectVinyl(0);
		}
	}

	private IEnumerator FillCategoriesList()
	{
		if (m_categoriesCount == 0)
		{
			foreach (VinylCategorysMeta category in Singleton<VinylMetaProvider>.instance.GetCategorys())
			{
				if (category.availablePlatform != AvailablePlatform.None && category.id != -1)
				{
					m_categorysList.AddItem(new VinylCategoryItem(category));
				}
			}
			m_categorysList.RecalcList();
			m_categorysList.Sort();
			yield return null;
		}
		if (m_vinylEditor.editingLayer != null)
		{
			SelectByRawId(m_vinylEditor.editingLayer.rawId);
		}
		else
		{
			int selectedRawId = base.owner.Get<UIVinylsEditingLayersContext>().selectedRawId;
			if (selectedRawId >= 0)
			{
				SelectByRawId(selectedRawId);
			}
			else
			{
				yield return CoroutineSelectCategory(0);
			}
		}
		for (int i = 0; i < m_categoriesCount; i++)
		{
			VinylCategoryItemController controller = m_categorysList.GetController(i);
			if (controller != null)
			{
				controller.UpdateData();
			}
		}
	}

	private void UnloadAllCategories()
	{
		for (int i = 0; i < m_categorysList.items.Count; i++)
		{
			m_categorysList.GetItem(i).UnloadResources();
		}
	}

	private bool IsResourcesLoaded()
	{
		for (int i = 0; i < m_vinylsCount; i++)
		{
			VinylLayerItemController controller = m_vinylsList.GetController(i);
			if (!(controller == null) && !controller.item.IsLoaded())
			{
				return false;
			}
		}
		return true;
	}

	private void SelectByRawId(int rawId)
	{
		VinylCategorysMeta meta = Singleton<VinylMetaProvider>.instance.meta.GetCategoryByVinylId(rawId);
		if (meta == null)
		{
			return;
		}
		VinylCategoryItem item = m_categorysList.Find((VinylCategoryItem x) => x.meta.id == meta.id);
		int num = m_categorysList.IndexOf(item);
		int selectedIndex = m_categorysList.GetSelectedIndex();
		bool flag = num < 0;
		if (selectedIndex != num || flag)
		{
			if (flag)
			{
				num = 0;
			}
			TimeController.StartNow(CoroutineSelectCategory(num));
		}
		else
		{
			UpdateBottomHotkeys(selectedIndex);
		}
	}

	private IEnumerator CoroutineSelectVinyl(int index)
	{
		yield return null;
		SelectVinyl(index);
	}

	private IEnumerator CoroutineSelectCategory(int index)
	{
		yield return null;
		SelectCategory(index);
	}

	private void SelectCategory(int index)
	{
		if (index >= 0 && index < m_categoriesCount)
		{
			m_isCategoryloaded = false;
			ShowLoadPanel(show: true);
			m_categorySelectTime = Time.unscaledTime;
			m_categorysList.selectedIndex = index;
			UpdateBottomHotkeys(index);
		}
	}

	private void SelectVinylAndCategory(int index)
	{
		VinylLayerItemController controller = m_vinylsList.GetController(index);
		if (!(controller == null))
		{
			if (controller.selected.value)
			{
				Select();
			}
			else
			{
				SelectVinyl(index);
			}
		}
	}

	private void Update()
	{
		TryLoadCategoryWithPeriod();
	}

	private bool IsSelectDelay()
	{
		return Time.unscaledTime - m_categorySelectTime < 0.3f;
	}

	private void TryLoadCategoryWithPeriod()
	{
		if (!IsSelectDelay() && !m_isCategoryloaded && !(m_categorysList == null))
		{
			VinylCategoryItem vinylCategoryItem = m_categorysList.GetSelectedItem();
			if (vinylCategoryItem != null)
			{
				FillVinylsList(vinylCategoryItem);
				ShowLoadPanel(show: false, forceApply: true);
				m_isCategoryloaded = true;
			}
		}
	}

	private void ShowLoadPanel(bool show, bool forceApply = false)
	{
		if (forceApply)
		{
			m_showLoadPanel.value = show;
		}
	}

	private void SelectVinyl(int index)
	{
		if (!m_showLoadPanel.value)
		{
			m_vinylsList.selectedIndex = Mathf.Clamp(index, 0, m_vinylsCount - 1);
		}
	}

	private async UniTask SelectVinylByIndexOnLoadedCategoryAsync(int vinylIndex)
	{
		m_safeAutoHidePanelCts?.Cancel();
		m_safeAutoHidePanelCts = new CancellationTokenSource();
		m_safeAutoHidePanelCts.CancelAfter(10000);
		CancellationToken token = m_safeAutoHidePanelCts.Token;
		ShowLoadPanel(show: true, forceApply: true);
		while (!IsResourcesLoaded() && !token.IsCancellationRequested)
		{
			await UniTask.Yield(token);
		}
		ShowLoadPanel(show: false, forceApply: true);
		SelectVinyl((!token.IsCancellationRequested) ? vinylIndex : 0);
		m_safeAutoHidePanelCts = null;
	}

	private void GoToBack()
	{
		Hide();
		if (m_vinylEditor.editingLayer != null && m_vinylEditor.isLayerEditing)
		{
			base.owner.Push<UIVinylsEditingContext>();
		}
		else
		{
			base.owner.Push<UIVinylsEditingLayersContext>();
		}
	}

	private void Select()
	{
		if (m_showLoadPanel.value || !m_isCategoryloaded || IsSelectDelay())
		{
			return;
		}
		VinylCategoryItem vinylCategoryItem = m_categorysList.GetSelectedItem();
		if (vinylCategoryItem == null)
		{
			return;
		}
		if (vinylCategoryItem.dlcCode != DLCCode.Unknown && !Overlay.instance.IsDLCInstalled(vinylCategoryItem.dlcCode))
		{
			UIMessageBox.ShowDLCDialog(LocalizationKeys.no_dlc_vinyl_apply.Localized(), vinylCategoryItem.dlcCode);
			return;
		}
		VinylLayerItem vinylLayerItem = m_vinylsList.GetSelectedItem();
		if (vinylLayerItem == null)
		{
			return;
		}
		UIVinylsEditingLayersContext uIVinylsEditingLayersContext = base.owner.Get<UIVinylsEditingLayersContext>();
		int selectedLayerIndex = uIVinylsEditingLayersContext.selectedLayerIndex;
		if (selectedLayerIndex >= 0 && uIVinylsEditingLayersContext.mustReplace)
		{
			m_selectedLayer = m_vinylEditor.GetLayer(selectedLayerIndex);
			m_vinylEditor.StartEditLayer(m_selectedLayer);
			m_vinylEditor.SetRawIdToEditingLayer(vinylLayerItem.id);
			uIVinylsEditingLayersContext.RemoveLayer(selectedLayerIndex);
			uIVinylsEditingLayersContext.InsertLayer(selectedLayerIndex, m_selectedLayer);
			uIVinylsEditingLayersContext.mustReplace = false;
		}
		else
		{
			selectedLayerIndex++;
			m_selectedLayer = m_vinylEditor.CreateNewLayer(vinylLayerItem.id);
			m_vinylEditor.MoveLayer(m_selectedLayer, selectedLayerIndex);
			uIVinylsEditingLayersContext.InsertLayer(selectedLayerIndex, m_selectedLayer);
			Vector2 position = new Vector2(Screen.width, Screen.height) * 0.5f + Vector2.down * Screen.height * 0.05f;
			Vector3 eulerAngles = m_camera.transform.eulerAngles;
			Vector3 zero = Vector3.zero;
			if (eulerAngles.y > 225f && eulerAngles.y < 315f && eulerAngles.x < 45f)
			{
				zero = Vector3.left;
			}
			else if (eulerAngles.y > 45f && eulerAngles.y < 135f && eulerAngles.x < 45f)
			{
				zero = Vector3.right;
			}
			else if (eulerAngles.x > 20f)
			{
				zero = m_camera.transform.forward * 0.1f;
				zero.y = -1f;
				zero.Normalize();
			}
			else
			{
				zero = m_camera.transform.forward;
			}
			m_vinylEditor.StartEditLayer(m_selectedLayer);
			m_vinylEditor.SetScreenSpacePositionToEditingLayer(m_camera, position);
			m_vinylEditor.SetRotationToEditingLayer(Quaternion.LookRotation(zero));
			m_vinylEditor.ScaleEditingLayer(0.7f);
			SoundController.Play2DUISound(SoundSample.VinylCreate);
		}
		Hide();
		if (!base.owner.IsActive<UIVinylsEditingContext>())
		{
			base.owner.Push<UIVinylsEditingContext>();
		}
	}

	public void NextCategory()
	{
		SelectCategory(m_categorysList.GetSelectedIndex() + 1);
	}

	public void PrevCategory()
	{
		SelectCategory(m_categorysList.GetSelectedIndex() - 1);
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	private void NavRight()
	{
		SelectVinyl(m_vinylsList.GetSelectedIndex() + 1);
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	private void NavLeft()
	{
		SelectVinyl(m_vinylsList.GetSelectedIndex() - 1);
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	private void NavUp()
	{
		SelectVinyl(m_vinylsList.GetSelectedIndex() - m_vinylsList.GetRowsCount());
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	private void NavDown()
	{
		SelectVinyl(m_vinylsList.GetSelectedIndex() + m_vinylsList.GetRowsCount());
	}

	public IEnumerator ShowCategorysVinylsCount()
	{
		for (int i = 0; i < m_categoriesCount; i++)
		{
			VinylCategoryItem item = m_categorysList.GetItem(i);
			if (item != null)
			{
				item.SetVinylsCountVisibility(!item.showVinylsCount);
				yield return null;
			}
		}
		m_categorysList.UpdateList();
	}
}
