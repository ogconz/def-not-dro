using UnityEngine;

namespace TriLib
{
	public class MeshData
	{
		public string Name;

		public Vector3[] Vertices;

		public Vector3[] Normals;

		public Vector4[] Tangents;

		public Vector4[] BiTangents;

		public Vector2[] Uv;

		public Vector2[] Uv1;

		public Vector2[] Uv2;

		public Vector2[] Uv3;

		public Color[] Colors;

		public int[] Triangles;

		public bool HasBoneInfo;

		public Matrix4x4[] BindPoses;

		public string[] BoneNames;

		public BoneWeight[] BoneWeights;

		public uint MaterialIndex;

		public Mesh Mesh;
	}
}
