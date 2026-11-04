using Render.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RenderCore.UI
{
	[AddComponentMenu("UI/Color Mapping")]
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Graphic))]
	[RequireComponent(typeof(RectTransform))]
	public class ColorMapping : UIBehaviour, IMaterialModifier, IMaterialParameters
	{
		private static readonly MaterialsCache m_materialsCache = new MaterialsCache();

		[SerializeField]
		private Color m_secondaryColor;

		private Material m_material;

		public static bool isUsed => m_materialsCache.isUsed;

		public Color secondaryColor
		{
			set
			{
				m_secondaryColor = value;
				GetComponent<Graphic>().SetMaterialDirty();
			}
		}

		public bool Compare(IMaterialParameters other)
		{
			return false;
		}

		public void Modify(Material m)
		{
			m.EnableKeyword("UI_MAT_2COLORMAPPING");
			m.SetColor(PropertyIDs.secondaryColor, m_secondaryColor);
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			GetComponent<Graphic>().SetMaterialDirty();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_materialsCache.Remove(m_material);
			GetComponent<Graphic>().SetMaterialDirty();
		}

		public Material GetModifiedMaterial(Material baseMaterial)
		{
			if (base.isActiveAndEnabled)
			{
				Material material = m_materialsCache.Add(baseMaterial, this);
				m_materialsCache.Remove(m_material);
				m_material = material;
				return m_material;
			}
			return baseMaterial;
		}
	}
}
