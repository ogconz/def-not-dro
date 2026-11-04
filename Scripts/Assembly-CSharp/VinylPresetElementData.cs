using System;
using Binding;
using UnityEngine;

public class VinylPresetElementData : BaseListElementData
{
	public BoolProperty isPlus = new BoolProperty();

	public BoolProperty isNormal = new BoolProperty();

	public BoolProperty isEmpty = new BoolProperty();

	public TextureProperty m_preview = new TextureProperty();

	private StringProperty m_index = new StringProperty();

	private BoolProperty m_showDownloadedIcon = new BoolProperty();

	private BoolProperty m_locked = new BoolProperty();

	public CarVilylsInfo.VinylPreset preset { get; private set; }

	public VinylPresetElementData(CarVilylsInfo.VinylPreset preset)
	{
		Init(preset);
	}

	public void Init(CarVilylsInfo.VinylPreset preset)
	{
		this.preset = preset;
		Refresh();
	}

	public void Refresh()
	{
		isPlus.value = preset == null;
		isNormal.value = preset != null && preset.layers.Count > 0;
		isEmpty.value = preset != null && preset.layers.Count <= 0;
		m_showDownloadedIcon.value = preset != null && preset.downloaded;
		m_locked.value = preset != null && preset.locked;
		if (isNormal.value)
		{
			m_index.value = DateTime.FromBinary(preset.time).ToString();
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (m_preview.value is RenderTexture temp)
		{
			RenderTexture.ReleaseTemporary(temp);
		}
	}
}
