using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TrackLayout.Behaviours
{
	public class GraphicMesh : MaskableGraphic
	{
		public Vector3[] vertices;

		public Vector2[] uv;

		public int[] triangles;

		[SerializeField]
		private Texture m_texture;

		public override Texture mainTexture
		{
			get
			{
				if (m_texture != null)
				{
					return m_texture;
				}
				if (material != null && material.mainTexture != null)
				{
					return material.mainTexture;
				}
				return Graphic.s_WhiteTexture;
			}
		}

		public Texture texture
		{
			get
			{
				return m_texture;
			}
			set
			{
				if (!(m_texture == value))
				{
					m_texture = value;
					SetMaterialDirty();
				}
			}
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			List<UIVertex> list = new List<UIVertex>(vertices.Length);
			for (int i = 0; i < vertices.Length; i++)
			{
				UIVertex simpleVert = UIVertex.simpleVert;
				simpleVert.position = vertices[i];
				simpleVert.color = color;
				list.Add(simpleVert);
			}
			if (uv != null && uv.Length == vertices.Length)
			{
				for (int j = 0; j < vertices.Length; j++)
				{
					UIVertex value = list[j];
					value.uv0 = uv[j];
					list[j] = value;
				}
			}
			List<int> indices = new List<int>(triangles);
			vh.AddUIVertexStream(list, indices);
		}
	}
}
