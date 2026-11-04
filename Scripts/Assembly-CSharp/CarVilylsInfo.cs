using System;
using System.Collections.Generic;
using GameOverlay;
using SimpleJSON;
using Utils.VinylLayerExtensions;
using VinylSystem;

public class CarVilylsInfo
{
	public class VinylPreset
	{
		[JsonSerialize]
		public string liveryId;

		[JsonSerialize]
		public int version;

		[JsonSerialize]
		public (string code, PlayerId playerId) ownerInfo;

		public long time;

		[JsonSerialize]
		private int m_id;

		[JsonSerialize]
		private bool m_locked;

		[JsonSerialize]
		public List<VinylLayer> layers = new List<VinylLayer>();

		public bool downloaded => !string.IsNullOrEmpty(ownerInfo.code);

		public int id => m_id;

		public bool locked => m_locked;

		public VinylPreset()
		{
			GenerateLiveryIdIfNeed();
		}

		[Obsolete]
		public void SetOldId(int id)
		{
			m_id = id;
		}

		public void SetLocked()
		{
			m_locked = true;
		}

		public void GenerateLiveryIdIfNeed()
		{
			if (string.IsNullOrEmpty(liveryId))
			{
				liveryId = Guid.NewGuid().ToString();
			}
			ownerInfo = (code: string.Empty, playerId: Overlay.instance.player.id);
		}
	}

	[JsonSerialize]
	private int m_ultimateID;

	[Obsolete]
	[JsonSerialize]
	private Dictionary<int, VinylPreset> m_presets = new Dictionary<int, VinylPreset>();

	[JsonSerialize]
	private Dictionary<string, VinylPreset> m_presetsNew = new Dictionary<string, VinylPreset>();

	public bool MergeInternalDataIfNeed(CarVisualManager.VisualSettings vs)
	{
		if (m_presetsNew.Count > 0 || m_presets == null)
		{
			if (m_presets != null)
			{
				DisposeObsoletePresets();
			}
			return false;
		}
		bool result = false;
		foreach (KeyValuePair<int, VinylPreset> preset in m_presets)
		{
			VinylPreset value = preset.Value;
			value.GenerateLiveryIdIfNeed();
			m_presetsNew[preset.Value.liveryId] = value;
			if (vs != null && string.IsNullOrEmpty(vs.vinylPresetLiveryId) && preset.Key == vs.vinylPresetId)
			{
				vs.vinylPresetLiveryId = value.liveryId;
				result = true;
			}
		}
		DisposeObsoletePresets();
		return result;
	}

	private void DisposeObsoletePresets()
	{
		m_presets = null;
	}

	public List<VinylPreset> GetPresetsList()
	{
		return new List<VinylPreset>(m_presetsNew.Values);
	}

	public override bool Equals(object obj)
	{
		if (!(obj is CarVilylsInfo carVilylsInfo))
		{
			return false;
		}
		if (m_presetsNew.Count != carVilylsInfo.m_presetsNew.Count)
		{
			return false;
		}
		foreach (KeyValuePair<string, VinylPreset> item in m_presetsNew)
		{
			if (!carVilylsInfo.m_presetsNew.ContainsKey(item.Key) || !carVilylsInfo.m_presetsNew.ContainsValue(item.Value))
			{
				return false;
			}
			if (!carVilylsInfo.m_presetsNew.TryGetValue(item.Key, out var value))
			{
				continue;
			}
			List<VinylLayer> layers = item.Value.layers;
			List<VinylLayer> layers2 = value.layers;
			if (layers.Count != layers2.Count)
			{
				return false;
			}
			for (int i = 0; i < layers.Count; i++)
			{
				if (!value.layers[i].LayerEquals(layers2[i]))
				{
					return false;
				}
			}
		}
		return m_ultimateID == carVilylsInfo.m_ultimateID;
	}

	public CarVilylsInfo Copy()
	{
		CarVilylsInfo obj = MemberwiseClone() as CarVilylsInfo;
		obj.m_presetsNew = new Dictionary<string, VinylPreset>(m_presetsNew);
		obj.m_ultimateID = m_ultimateID;
		return obj;
	}

	public VinylPreset GetPreset(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return null;
		}
		m_presetsNew.TryGetValue(id, out var value);
		return value;
	}

	public VinylPreset CreatePreset()
	{
		VinylPreset vinylPreset = new VinylPreset();
		vinylPreset.time = DateTime.Now.ToBinary();
		vinylPreset.SetOldId(m_ultimateID++);
		return vinylPreset;
	}

	public void AddPreset(VinylPreset preset)
	{
		m_presetsNew[preset.liveryId] = preset;
	}

	public void RemovePreset(VinylPreset preset)
	{
		m_presetsNew.Remove(preset.liveryId);
	}
}
