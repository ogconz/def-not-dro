using UnityEngine;

namespace Utils.MeshGen.Spline
{
	public class Shape2D : MonoBehaviour
	{
		public VertexData2D[] data;

		public int[] lines;

		public VertexData2D this[int index] => data[index];
	}
}
