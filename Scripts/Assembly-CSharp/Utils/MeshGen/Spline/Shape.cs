using System;
using UnityEngine;

namespace Utils.MeshGen.Spline
{
	[ExecuteInEditMode]
	public class Shape : Shape2D
	{
		public Mesh mesh;

		private void Start()
		{
			if (mesh == null)
			{
				return;
			}
			Array.Resize(ref data, mesh.vertices.Length);
			Array.Resize(ref lines, (int)((float)mesh.vertices.Length * 1.5f));
			for (int i = 0; i < mesh.vertices.Length; i++)
			{
				data[i].vertex = mesh.vertices[i];
				data[i].normal = mesh.normals[i];
				data[i].uCoord = 0f;
			}
			int num = 0;
			for (int j = 0; j < lines.Length; j++)
			{
				lines[j] = num;
				if (j % 2 == 0)
				{
					num++;
				}
			}
		}

		private void OnEnable()
		{
			Start();
		}
	}
}
