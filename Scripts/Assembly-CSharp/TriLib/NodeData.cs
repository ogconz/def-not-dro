using UnityEngine;

namespace TriLib
{
	public class NodeData
	{
		public string Name;

		public string Path;

		public Matrix4x4 Matrix;

		public uint[] Meshes;

		public NodeData Parent;

		public NodeData[] Children;

		public GameObject GameObject;
	}
}
