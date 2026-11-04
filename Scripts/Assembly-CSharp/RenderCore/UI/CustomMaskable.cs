using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RenderCore.UI
{
	[AddComponentMenu("UI/Custom Maskable", 13)]
	[ExecuteInEditMode]
	[RequireComponent(typeof(RectTransform))]
	[RequireComponent(typeof(MaskableGraphic))]
	[DisallowMultipleComponent]
	public class CustomMaskable : UIBehaviour, IMaterialModifier
	{
		[Serializable]
		private struct MaterialParameters : IMaterialParameters
		{
			internal CustomMask.ClipShape clipShape;

			internal Vector4 clipParams0;

			internal Vector4 clipParams1;

			public bool Compare(IMaterialParameters other)
			{
				MaterialParameters materialParameters = (MaterialParameters)(object)other;
				if (clipShape == materialParameters.clipShape && clipParams0 == materialParameters.clipParams0)
				{
					return clipParams1 == materialParameters.clipParams1;
				}
				return false;
			}

			public void Modify(Material m)
			{
				if (clipShape == CustomMask.ClipShape.Circle)
				{
					m.EnableKeyword("UI_CLIP_CIRCLE");
					m.SetVector("_ClipParams0", clipParams0);
				}
				else if (clipShape == CustomMask.ClipShape.RectRounded)
				{
					m.EnableKeyword("UI_CLIP_RECT_ROUNDED");
					m.SetVector("_ClipParams0", clipParams0);
					m.SetVector("_ClipParams1", clipParams1);
				}
				else if (clipShape == CustomMask.ClipShape.Rect)
				{
					m.EnableKeyword("UI_CLIP_RECT");
				}
			}
		}

		private static readonly MaterialsCache m_materialsCache = new MaterialsCache();

		private MaskableGraphic m_maskableGraphic;

		private MaterialParameters m_materialParameters;

		private Material m_maskMaterial;

		private CustomMask m_parentMask;

		private bool m_isRegistered;

		public static bool isUsed => m_materialsCache.isUsed;

		private void Register()
		{
			if (!m_isRegistered && base.isActiveAndEnabled)
			{
				m_parentMask = GetComponentInParent<CustomMask>();
				if (m_parentMask != null && m_parentMask.isActiveAndEnabled)
				{
					m_parentMask.Register(this);
				}
				m_isRegistered = true;
			}
		}

		private void Unregister()
		{
			if (m_isRegistered)
			{
				if (m_parentMask != null)
				{
					m_parentMask.Unregister(this);
				}
				m_isRegistered = false;
			}
		}

		public void Reregister()
		{
			Unregister();
			Register();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			Register();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			Unregister();
			m_materialsCache.Remove(m_maskMaterial);
			if (m_maskableGraphic == null)
			{
				m_maskableGraphic = GetComponent<MaskableGraphic>();
			}
			if (m_maskableGraphic != null)
			{
				m_maskableGraphic.SetMaterialDirty();
			}
		}

		protected override void OnBeforeTransformParentChanged()
		{
			base.OnBeforeTransformParentChanged();
			Unregister();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			Register();
		}

		public void SetClipParams(Vector4 clipParams0, Vector4 clipParams1, CustomMask.ClipShape clipShape)
		{
			m_materialParameters.clipShape = clipShape;
			m_materialParameters.clipParams0 = clipParams0;
			m_materialParameters.clipParams1 = clipParams1;
			if (m_maskableGraphic == null)
			{
				m_maskableGraphic = GetComponent<MaskableGraphic>();
			}
			if (m_maskableGraphic != null)
			{
				m_maskableGraphic.SetMaterialDirty();
			}
		}

		public Material GetModifiedMaterial(Material baseMaterial)
		{
			Material maskMaterial = m_materialsCache.Add(baseMaterial, m_materialParameters);
			m_materialsCache.Remove(m_maskMaterial);
			m_maskMaterial = maskMaterial;
			return m_maskMaterial;
		}
	}
}
