using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RenderCore.UI
{
	[AddComponentMenu("UI/Glass Effect", 13)]
	[ExecuteInEditMode]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Graphic))]
	[RequireComponent(typeof(RectTransform))]
	public class GlassEffect : UIBehaviour, IMaterialModifier, IMaterialParameters
	{
		private static readonly MaterialsCache m_materialsCache = new MaterialsCache();

		private Material m_material;

		public static bool isUsed => m_materialsCache.isUsed;

		public bool Compare(IMaterialParameters other)
		{
			return true;
		}

		public void Modify(Material m)
		{
			m.EnableKeyword("UI_MAT_GLASS");
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
