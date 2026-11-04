using Binding;
using UnityEngine;
using VinylSystem;

public class VinylLayerItemController : BaseItemController<VinylLayerItem>
{
	private TextureProperty m_icon = new TextureProperty();

	private MaterialProperty m_previewMaterial = new MaterialProperty();

	private BoolProperty m_isEmpty = new BoolProperty();

	private BoolProperty m_isShowModifiers = new BoolProperty();

	private BoolProperty m_isSymmetryActive = new BoolProperty();

	private BoolProperty m_isBackgroundHided = new BoolProperty();

	private IntProperty m_layerIndex = new IntProperty();

	private static Color[] m_defaultColors = new Color[1] { Color.white };

	public override void SetItem(VinylLayerItem newItem)
	{
		UpdateBackgroundVisibility();
		ClearProperties();
		base.SetItem(newItem);
		m_layerIndex.value = base.index + 1;
		UpdateModifiersList();
		m_isEmpty.value = newItem == null;
		if (!m_isEmpty.value)
		{
			newItem.LoadTexture(SetupIcon);
		}
		void SetupIcon(Texture2D icon)
		{
			if (newItem == base.item)
			{
				m_icon.value = icon;
				Color[] colors = ((base.item.vinylLayer != null) ? base.item.vinylLayer.colors : base.item.meta.defaultColors);
				VinylSystemKernel.SetupPreviewMaterial(m_previewMaterial.value, colors);
			}
		}
	}

	private void UpdateModifiersList()
	{
		m_isSymmetryActive.value = base.item != null && base.item.vinylLayer != null && base.item.vinylLayer.isSymmetry;
		m_isShowModifiers.value = m_isSymmetryActive.value;
	}

	private void ClearProperties()
	{
		if (!m_previewMaterial.value)
		{
			m_previewMaterial.value = VinylSystemKernel.CreatePreviewMaterial();
		}
		m_icon.value = VinylMetaProvider.GetPreviewDefaultTexture();
		VinylSystemKernel.SetupPreviewMaterial(m_previewMaterial.value, m_defaultColors);
	}

	private void UpdateBackgroundVisibility()
	{
		if (States.mainCurrent is GarageGUIState garageGUIState)
		{
			m_isBackgroundHided.value = garageGUIState.contextOwner.IsActive<UIVinylsLayerSelectionContext>();
		}
		else
		{
			m_isBackgroundHided.value = false;
		}
	}
}
