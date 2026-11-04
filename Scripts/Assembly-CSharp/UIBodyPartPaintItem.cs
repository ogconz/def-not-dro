using System;
using System.Collections.Generic;
using Binding;
using Binding.Base;
using CarModelSystem.Data;
using UnityEngine;

public class UIBodyPartPaintItem : BaseBindingBehaviourTarget
{
	[Flags]
	public enum PaintMaterial
	{
		Metallic = 1,
		Matte = 2,
		Chrome = 4,
		Chameleon = 8,
		Candy = 0x10,
		Gloss = 0x20,
		Satin = 0x40,
		AllBody = 0x7F,
		AllWheels = 0x7F
	}

	public readonly Property<bool> selected = new BoolProperty(false);

	public readonly Property<bool> addedToPaint = new BoolProperty(false);

	public readonly Property<bool> ableToPaint = new BoolProperty(startValue: true);

	public readonly Property<Sprite> icon = new SpriteProperty();

	public readonly Property<string> description = new StringProperty();

	public RectTransform rectTransform;

	private readonly List<PaintMaterial> m_availableMaterials = new List<PaintMaterial>(1);

	private UIPaintCustomizationContext.BasePaintState m_basePaintState;

	private int m_index;

	private int m_materialIndex;

	private int m_indexConfig;

	private int m_maxIndexConfig;

	public UIPaintCustomizationContext.PartType partType { get; private set; }

	public bool canChangeMaterial { get; private set; }

	public int selectedMaterialIndex => PaintTypeToMaterialIndex();

	public int materialId => PaintTypeToMaterialIndex();

	public int indexConfig => GetIndexConfig();

	public void SetData(UIPaintCustomizationContext.BasePaintState holder, int index, Sprite sprite, UIPaintCustomizationContext.PartType type, PaintMaterial materials, string partDesc)
	{
		m_basePaintState = holder;
		m_index = index;
		icon.value = sprite;
		partType = type;
		description.value = partDesc ?? string.Empty;
		canChangeMaterial = materials != PaintMaterial.Gloss;
		if (canChangeMaterial)
		{
			foreach (PaintMaterial value in Enum.GetValues(typeof(PaintMaterial)))
			{
				if (materials.HasFlag(value) && value != PaintMaterial.AllBody)
				{
					m_availableMaterials.Add(value);
				}
			}
		}
		m_materialIndex = 0;
	}

	public void SetPartMaterial(int materialId, int indexConfig)
	{
		if (!canChangeMaterial)
		{
			return;
		}
		PaintMaterial paintMaterial = MaterialIdToPaintType(materialId);
		for (int i = 0; i < m_availableMaterials.Count; i++)
		{
			if (m_availableMaterials[i] == paintMaterial)
			{
				m_materialIndex = i;
				m_indexConfig = indexConfig;
				break;
			}
		}
	}

	public void OnPartSelect()
	{
		UIPaintCustomizationContext context = m_basePaintState.context;
		if (!context.lockPartSelection)
		{
			if (context.MultiselectMode)
			{
				m_basePaintState.SelectPart(m_index);
				context.OnSelectedPartChanged(m_index);
				m_basePaintState.TogglePart(m_index);
			}
			else
			{
				context.SelectItemInCategory(m_index, UIPaintCustomizationContext.CategoryNavMode.Parts);
			}
		}
	}

	public int GetIndexConfigCount()
	{
		ValidateMaxIndexConfig();
		return m_maxIndexConfig;
	}

	public void PrevIndexConfig()
	{
		if (canChangeMaterial)
		{
			m_indexConfig--;
			if (m_indexConfig < 0)
			{
				m_indexConfig = m_maxIndexConfig - 1;
			}
		}
	}

	public void NextIndexConfig()
	{
		if (canChangeMaterial)
		{
			m_indexConfig++;
			if (m_indexConfig >= m_maxIndexConfig)
			{
				m_indexConfig = 0;
			}
		}
	}

	public void PrevMaterial()
	{
		if (canChangeMaterial)
		{
			m_materialIndex--;
			if (m_materialIndex < 0)
			{
				m_materialIndex = m_availableMaterials.Count - 1;
			}
			ValidateMaxIndexConfig();
		}
	}

	public void NextMaterial()
	{
		if (canChangeMaterial)
		{
			m_materialIndex++;
			if (m_materialIndex >= m_availableMaterials.Count)
			{
				m_materialIndex = 0;
			}
			ValidateMaxIndexConfig();
		}
	}

	public int PaintTypeToMaterialIndex()
	{
		if (m_availableMaterials.Count == 0)
		{
			return 6;
		}
		return m_availableMaterials[m_materialIndex] switch
		{
			PaintMaterial.Metallic => 1, 
			PaintMaterial.Matte => 2, 
			PaintMaterial.Chrome => 3, 
			PaintMaterial.Chameleon => 4, 
			PaintMaterial.Candy => 5, 
			PaintMaterial.Gloss => 6, 
			PaintMaterial.Satin => 7, 
			_ => 6, 
		};
	}

	private void ValidateMaxIndexConfig()
	{
		m_maxIndexConfig = CarModelConfig.config.carPaintUtility.GetIndexConfigs(CarVisualManager.MaterialIdToCarPaintType(m_materialIndex + 1)).Length;
	}

	public int GetIndexConfig()
	{
		return m_indexConfig;
	}

	public void ResetIndexConfig()
	{
		if (canChangeMaterial)
		{
			m_indexConfig = 0;
		}
	}

	public bool AvailableAdditionalColorToMaterial()
	{
		if (m_materialIndex < 0 && m_materialIndex >= m_availableMaterials.Count)
		{
			return false;
		}
		return m_availableMaterials[m_materialIndex] == PaintMaterial.Chameleon;
	}

	public static PaintMaterial MaterialIdToPaintType(int id)
	{
		return id switch
		{
			1 => PaintMaterial.Metallic, 
			2 => PaintMaterial.Matte, 
			3 => PaintMaterial.Chrome, 
			4 => PaintMaterial.Chameleon, 
			5 => PaintMaterial.Candy, 
			6 => PaintMaterial.Gloss, 
			7 => PaintMaterial.Satin, 
			_ => PaintMaterial.Gloss, 
		};
	}
}
