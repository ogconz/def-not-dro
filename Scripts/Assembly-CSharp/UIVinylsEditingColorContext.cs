using System.Collections.Generic;
using System.Text.RegularExpressions;
using ArrayExtension;
using Binding;
using Binding.Base;
using DB;
using DB.Meta;
using DI;
using GameInput;
using UnityEngine;
using Utlis;
using VinylSystem;

public class UIVinylsEditingColorContext : BaseBindingBehaviourTarget
{
	private VinylWrapEditor m_vinylsEditor;

	private VinylLayer m_vinyl;

	public readonly FloatProperty colorH = new FloatProperty(0f);

	public readonly FloatProperty colorS = new FloatProperty(0f);

	public readonly FloatProperty colorV = new FloatProperty(0f);

	public readonly FloatProperty colorA = new FloatProperty(0f);

	public readonly ColorProperty colorGradient = new ColorProperty(Color.white);

	public readonly StringProperty layerName = new StringProperty();

	public readonly StringProperty mainHex = new StringProperty();

	public readonly StringProperty firstColor = new StringProperty("Hue");

	public readonly StringProperty secondColor = new StringProperty("Saturation");

	public readonly StringProperty thirdColor = new StringProperty("Brightness");

	public readonly StringProperty alphaColor = new StringProperty("Alpha");

	public readonly Property<int> materialIndex = new IntProperty(0);

	public readonly Property<string> materialSelected = new StringProperty();

	private List<VinylColorMeta> m_vinylMaterials = new List<VinylColorMeta>();

	private ColorProperty m_originColor = new ColorProperty();

	private int m_originMaterialIndex;

	private Color[] m_colorsCopy;

	private int[] m_materialPropsCopy;

	private BoolProperty m_selectedOrigin = new BoolProperty(startValue: true);

	private ListProperty m_colorList = new ListProperty();

	private ListProperty m_prevColorList = new ListProperty();

	private List<UIColorChangerContext> m_colorSliders = new List<UIColorChangerContext>();

	private int m_colorSliderIndex;

	private int m_layerIndex;

	private int m_layerCount = 1;

	private IntProperty m_tabIndex = new IntProperty(0);

	private int m_tabCount = 2;

	private int m_colorGridRows = 10;

	private int m_colorGridCols = 21;

	[Dependency]
	private BaseModel m_db;

	private Color m_currentColor = Color.white;

	private float m_prevColorH;

	private const string HEX_PATTERN = "^[#]?([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$";

	public bool isGridMode => m_tabIndex.value == 0;

	private void Awake()
	{
		DependencyInjector.InjectObjectsInto(this);
		GetComponentsInChildren(includeInactive: true, m_colorSliders);
		foreach (UIColorChangerContext itr in m_colorSliders)
		{
			itr.AddSmoothSlideTrait(() => itr.slider.value, delegate(float value)
			{
				OnHSBColorsChanger();
				itr.slider.value = value;
			});
		}
		m_vinylMaterials = m_db.QueryAll<VinylColorMeta>();
		colorA.OnValueChanged += OnHSBColorsChanger;
		colorH.OnValueChanged += OnHSBColorsChanger;
		colorS.OnValueChanged += OnHSBColorsChanger;
		colorV.OnValueChanged += OnHSBColorsChanger;
		m_prevColorList.Add(new VinylColorListElementData(Color.red));
		m_prevColorList.Add(new VinylColorListElementData(Color.green));
		m_prevColorList.Add(new VinylColorListElementData(Color.blue));
		m_prevColorList.Add(new VinylColorListElementData(Color.yellow));
		m_prevColorList.Add(new VinylColorListElementData(Color.grey));
		m_prevColorList.Add(new VinylColorListElementData(Color.black));
		m_prevColorList.Add(new VinylColorListElementData(Color.white));
		m_prevColorList.OnSelectionChanged += OnPrevColorSelection;
		m_colorList.OnSelectionChanged += OnColorSelection;
		m_prevColorList.OnClick += OnColorItemClick;
		m_colorList.OnClick += OnColorItemClick;
	}

	private void FillColors()
	{
		if (m_colorList.count > 0)
		{
			return;
		}
		float num = (float)m_colorGridRows * 0.5f;
		HSBColor hSBColor = new HSBColor(Color.white);
		for (int i = 0; i < m_colorGridRows; i++)
		{
			for (int j = 0; j < m_colorGridCols; j++)
			{
				if (j == 0)
				{
					hSBColor.h = 0f;
					hSBColor.s = 0f;
					hSBColor.b = (float)i / (float)m_colorGridRows;
				}
				else
				{
					hSBColor.h = ((float)j - 1f) / ((float)m_colorGridCols - 2f);
					hSBColor.s = 1f - Mathf.Clamp01(((float)i - num) / num);
					hSBColor.b = Mathf.Clamp01((float)i / num) + 0.1f;
				}
				m_colorList.Add(new VinylColorListElementData(hSBColor.ToColor()));
			}
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
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
				hint = LocalizationKeys.confirm.Localized(),
				type = ActionType.NavSelectBtnA,
				action = Confirm
			}
		});
		m_vinylsEditor = VinylSystemKernel.instance.editor;
		m_vinyl = m_vinylsEditor.editingLayer;
		m_colorsCopy = m_vinyl.colors.Copy();
		m_materialPropsCopy = m_vinyl.materialProps.Copy();
		m_layerIndex = 0;
		m_layerCount = m_vinyl.colors.Length;
		FillColors();
		SelectLayer(m_layerIndex);
	}

	public void SetOriginColor()
	{
		m_selectedOrigin.value = true;
		m_prevColorList.SelectionAll(selected: false);
		m_colorList.SelectionAll(selected: false);
		SelectColor(m_originColor.value, m_originMaterialIndex);
	}

	private void SetPreviwColor(Color color)
	{
		HSBColor hSBColor = HSBColor.FromColor(color);
		colorA.value = hSBColor.a;
		colorH.value = hSBColor.h;
		colorS.value = hSBColor.s;
		colorV.value = hSBColor.b;
	}

	private void OnHSBColorsChanger()
	{
		HSBPaintHelper.ResetColorIfNeed(ref m_prevColorH, colorH, colorS, colorV);
		HSBColor hSBColor = new HSBColor(colorH.value, colorS.value, colorV.value, colorA.value);
		colorGradient.value = new HSBColor(colorH.value, 1f, 1f, 1f).ToColor();
		SelectColor(hSBColor.ToColor(), materialIndex.value);
	}

	public void SelectTab(int index)
	{
		if (index >= 0 && index < m_tabCount)
		{
			m_tabIndex.value = index;
			if (isGridMode)
			{
				m_colorList.SelectionAll(selected: false);
				return;
			}
			SetPreviwColor(m_vinyl.colors[m_layerIndex]);
			SelectColorSlider(m_colorSliderIndex);
		}
	}

	private void SelectPrevColor(int index)
	{
		if (index >= 0 && index < m_prevColorList.count)
		{
			m_prevColorList.Selection(index, selected: true);
			m_colorList.SelectionAll(selected: false);
		}
	}

	private void SelectGridColor(int index)
	{
		if (index >= 0 && index < m_colorList.count)
		{
			m_colorList.Selection(index, selected: true);
			m_prevColorList.SelectionAll(selected: false);
		}
	}

	private void SelectColor(Color color)
	{
		m_currentColor = color;
		m_vinylsEditor.SetColorToEditingLayer(color, materialIndex.value, m_layerIndex);
		Color32 color2 = color;
		mainHex.value = color2.r.ToString("X2") + color2.g.ToString("X2") + color2.b.ToString("X2") + color2.a.ToString("X2");
		UpdateMaterialSelector();
	}

	private void SelectColor(Color color, int material)
	{
		m_currentColor = color;
		materialIndex.value = material;
		m_vinylsEditor.SetColorToEditingLayer(color, material, m_layerIndex);
		Color32 color2 = color;
		mainHex.value = color2.r.ToString("X2") + color2.g.ToString("X2") + color2.b.ToString("X2") + color2.a.ToString("X2");
		UpdateMaterialSelector();
	}

	private void SelectLayer(int index)
	{
		if (index >= 0 && index < m_layerCount)
		{
			m_layerIndex = index;
			materialIndex.value = m_vinyl.materialProps[m_layerIndex];
			m_originColor.value = m_vinyl.colors[m_layerIndex];
			m_originMaterialIndex = m_vinyl.materialProps[m_layerIndex];
			SetOriginColor();
			if (!isGridMode)
			{
				SetPreviwColor(m_originColor.value);
			}
			layerName.value = ((m_layerCount > 1) ? $"Layer {m_layerIndex + 1}" : string.Empty);
			UpdateMaterialSelector();
		}
	}

	private void UpdateMaterialSelector(bool skipSetColor = true)
	{
		materialSelected.value = LocalizationService.Localize(m_vinylMaterials.Find((VinylColorMeta x) => x.id == materialIndex.value).name);
		if (!skipSetColor)
		{
			m_vinylsEditor.SetColorToEditingLayer(m_currentColor, materialIndex.value, m_layerIndex);
		}
	}

	private void SelectColorSlider(int index)
	{
		if (index < 0 || index >= m_colorSliders.Count)
		{
			return;
		}
		m_colorSliderIndex = index;
		foreach (UIColorChangerContext colorSlider in m_colorSliders)
		{
			colorSlider.selected.value = false;
		}
		m_colorSliders[m_colorSliderIndex].selected.value = true;
		m_colorSliders[m_colorSliderIndex].StartListenKeys();
	}

	private void OnColorItemClick(BaseListElementData data)
	{
		if (!data.selected)
		{
			int num = m_prevColorList.IndexOf(data);
			if (num >= 0)
			{
				SelectPrevColor(num);
			}
			else
			{
				SelectGridColor(m_colorList.IndexOf(data));
			}
		}
	}

	private void OnPrevColorSelection(BaseListElementData data)
	{
		if (data.selected)
		{
			m_selectedOrigin.value = false;
			VinylColorListElementData vinylColorListElementData = data as VinylColorListElementData;
			SelectColor(vinylColorListElementData.color, vinylColorListElementData.material);
		}
	}

	private void OnColorSelection(BaseListElementData data)
	{
		if (data.selected)
		{
			m_selectedOrigin.value = false;
			if (data is VinylColorListElementData vinylColorListElementData)
			{
				SelectColor(vinylColorListElementData.color);
			}
		}
	}

	public void EditHex()
	{
		string value = mainHex.value;
		UIMessageBox.ShowInputDialog(LocalizationKeys.input_hex_code.Localized(), value, null, null, LocalizationKeys.Cancel.Localized(), delegate(string x)
		{
			if (!string.IsNullOrEmpty(x) && HexValidator(x))
			{
				SetPreviwColor(UIPaintCustomizationContext.HexToColor(x));
			}
		}, LocalizationKeys.Ok.Localized());
	}

	private bool HexValidator(string hex)
	{
		return Regex.IsMatch(hex, "^[#]?([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})$");
	}

	private void Confirm()
	{
		Color color = m_vinyl.colors[m_layerIndex];
		int num = m_vinyl.materialProps[m_layerIndex];
		VinylColorListElementData vinylColorListElementData = m_prevColorList.GetByIndex(0) as VinylColorListElementData;
		if (vinylColorListElementData.color != color || vinylColorListElementData.material != num)
		{
			m_prevColorList.RemoveAt(m_prevColorList.count - 1);
			m_prevColorList.Insert(0, new VinylColorListElementData(color, num));
		}
		Hide();
		base.owner.Push<UIVinylsEditingContext>();
	}

	private void GoToBack()
	{
		if (!SequenceEqual(m_colorsCopy, m_vinyl.colors) || !SequenceEqual(m_materialPropsCopy, m_vinyl.materialProps))
		{
			GUICommonGodVoice.ShowText(LocalizationKeys.layer_change_discard.Localized(), 3f);
			SoundController.Play2DUISound(SoundSample.VinylCancel, 0.1f, 0.1f);
			m_vinylsEditor.SetColorsToEditingLayer(m_colorsCopy, m_materialPropsCopy);
		}
		Hide();
		base.owner.Push<UIVinylsEditingContext>();
	}

	private bool SequenceEqual<TSource>(TSource[] first, TSource[] second)
	{
		if (first.Length != second.Length)
		{
			return false;
		}
		for (int i = 0; i < first.Length; i++)
		{
			ref readonly TSource reference = ref first[i];
			object obj = second[i];
			if (!reference.Equals(obj))
			{
				return false;
			}
		}
		return true;
	}

	[GameHotkey(ActionType.NavDown, false, true)]
	private void NavDown()
	{
		if (isGridMode)
		{
			int selectedIndex = m_prevColorList.GetSelectedIndex();
			if (selectedIndex >= 0)
			{
				m_prevColorList.Selection(selectedIndex, selected: false);
				selectedIndex = 5 + (int)(((float)selectedIndex + 1f) / (float)m_prevColorList.count * (float)(m_colorGridCols - 7));
				SelectGridColor(selectedIndex);
			}
			else if (m_selectedOrigin.value)
			{
				SelectGridColor(0);
			}
			else
			{
				selectedIndex = m_colorList.GetSelectedIndex();
				SelectGridColor(selectedIndex + m_colorGridCols);
			}
		}
		else
		{
			SelectColorSlider(m_colorSliderIndex + 1);
		}
	}

	[GameHotkey(ActionType.NavUp, false, true)]
	private void NavUp()
	{
		if (isGridMode)
		{
			int selectedIndex = m_colorList.GetSelectedIndex();
			if (selectedIndex >= 0)
			{
				if (selectedIndex > m_colorGridCols - 1)
				{
					SelectGridColor(selectedIndex - m_colorGridCols);
					return;
				}
				if (selectedIndex < 7)
				{
					SetOriginColor();
					return;
				}
				m_colorList.Selection(selectedIndex, selected: false);
				selectedIndex = (int)(((float)selectedIndex - 6f) / ((float)m_colorGridCols - 6f) * (float)m_prevColorList.count);
				SelectPrevColor(selectedIndex);
			}
		}
		else
		{
			SelectColorSlider(m_colorSliderIndex - 1);
		}
	}

	[GameHotkey(ActionType.NavRight, false, true)]
	private void NavRight()
	{
		if (!isGridMode)
		{
			return;
		}
		int selectedIndex = m_colorList.GetSelectedIndex();
		if (selectedIndex >= 0)
		{
			if (selectedIndex % m_colorGridCols < m_colorGridCols - 1)
			{
				SelectGridColor(selectedIndex + 1);
			}
			return;
		}
		selectedIndex = m_prevColorList.GetSelectedIndex();
		if (selectedIndex < m_prevColorList.count)
		{
			m_prevColorList.Selection(selectedIndex + 1, selected: true);
		}
	}

	[GameHotkey(ActionType.NavLeft, false, true)]
	private void NavLeft()
	{
		if (!isGridMode)
		{
			return;
		}
		int selectedIndex = m_colorList.GetSelectedIndex();
		if (selectedIndex > 0)
		{
			if (selectedIndex % m_colorGridCols > 0)
			{
				SelectGridColor(selectedIndex - 1);
			}
			return;
		}
		selectedIndex = m_prevColorList.GetSelectedIndex();
		if (selectedIndex > 0)
		{
			m_prevColorList.Selection(selectedIndex - 1, selected: true);
		}
		else
		{
			SetOriginColor();
		}
	}

	[GameHotkey(ActionType.NavAdditional0BtnX, false, false)]
	public void NavPrevMaterial()
	{
		materialIndex.value--;
		if (materialIndex.value < 0)
		{
			materialIndex.value = m_vinylMaterials.Count - 1;
		}
		UpdateMaterialSelector(skipSetColor: false);
	}

	[GameHotkey(ActionType.NavAdditional1BtnY, false, false)]
	public void NavNextMaterial()
	{
		materialIndex.value++;
		if (materialIndex.value >= m_vinylMaterials.Count)
		{
			materialIndex.value = 0;
		}
		UpdateMaterialSelector(skipSetColor: false);
	}

	public void NavNextLayer()
	{
		SelectLayer(m_layerIndex + 1);
	}

	public void NavPrevLayer()
	{
		SelectLayer(m_layerIndex - 1);
	}

	[GameHotkey(ActionType.NavNextPage, false, false)]
	public void NavNextPage()
	{
		SelectTab(m_tabIndex.value + 1);
	}

	[GameHotkey(ActionType.NavPrevPage, false, false)]
	public void NavPrevPage()
	{
		SelectTab(m_tabIndex.value - 1);
	}
}
