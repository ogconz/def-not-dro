using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[ExecuteInEditMode]
public class MeshGen : MonoBehaviour
{
	private GameObject m_objectGenerator;

	private MeshRenderer m_meshRenderer;

	private MeshFilter m_meshFilter;

	private Mesh m_mesh;

	private BoxCollider m_collider;

	private const string NAME_OBJECT = "meshgenerated";

	private Vector3 m_left = Vector3.left;

	private Vector3 m_right = Vector3.right;

	private Vector3 m_hitLeft = Vector3.left;

	private Vector3 m_hitRight = Vector3.right;

	public Vector2 scaleUV = Vector2.one;

	public Vector2 offsetUV = Vector2.zero;

	public Material material;

	public int stepsCheckHeight = 2;

	public float limitHeightOptimization = 0.05f;

	private int m_coundSteps;

	private List<Vector3> m_pointsHeight = new List<Vector3>();

	private List<Vector3> m_vertices;

	private List<Vector2> m_uvs;

	private List<int> m_triangleIndices;

	private List<Vector3> m_drawingRays = new List<Vector3>(2);

	private const int IGNORE_LAYER = -1073741825;

	private void Start()
	{
		Generate();
	}

	private void OnEnable()
	{
		Generate();
	}

	private void CheckObject()
	{
		Transform transform = base.transform.Find("meshgenerated");
		if (transform == null)
		{
			m_objectGenerator = new GameObject("meshgenerated", typeof(MeshFilter), typeof(MeshRenderer));
			m_objectGenerator.transform.position = base.transform.position;
			m_objectGenerator.transform.rotation = base.transform.rotation;
			m_objectGenerator.transform.localScale = Vector3.one;
			m_objectGenerator.transform.SetParent(base.transform);
			m_objectGenerator.transform.localScale = Vector3.one;
		}
		else
		{
			m_objectGenerator = transform.gameObject;
		}
		m_meshRenderer = m_objectGenerator.GetComponent<MeshRenderer>();
		m_meshFilter = m_objectGenerator.GetComponent<MeshFilter>();
		if (m_mesh != null)
		{
			m_mesh.Clear();
		}
		m_mesh = new Mesh();
		m_meshFilter.sharedMesh = m_mesh;
		m_collider = GetComponent<BoxCollider>();
		m_coundSteps = 0;
	}

	private List<Vector3> Detalization(Vector3 start, Vector3 end)
	{
		List<Vector3> list = new List<Vector3>();
		if (start == end)
		{
			m_coundSteps++;
			return list;
		}
		if (CheckHalfHeight(start, end, out var halfPoint) && m_coundSteps <= stepsCheckHeight)
		{
			m_coundSteps++;
			int coundSteps = m_coundSteps;
			m_coundSteps = coundSteps;
			list.Add(halfPoint);
			list.AddRange(Detalization(halfPoint, end));
		}
		return list;
	}

	private bool CheckHalfHeight(Vector3 start, Vector3 end, out Vector3 halfPoint)
	{
		RaycastHit[] array = Physics.RaycastAll(Vector3.Lerp(start, end, 0.5f) + Vector3.up, Vector3.down, 10f, -1073741825);
		if (array.Length != 0 && (Mathf.Abs(array[0].point.y - start.y) > limitHeightOptimization || Mathf.Abs(array[0].point.y - end.y) > limitHeightOptimization))
		{
			halfPoint = array[0].point;
			return true;
		}
		halfPoint = Vector3.zero;
		return false;
	}

	public void Generate()
	{
		CheckObject();
		stepsCheckHeight = ((stepsCheckHeight > 10) ? 10 : stepsCheckHeight);
		Vector3 left = base.transform.TransformPoint(m_collider.center + new Vector3(0f, m_collider.size.y * 0.5f, m_collider.size.z * 0.5f));
		m_left = left;
		Vector3 right = base.transform.TransformPoint(m_collider.center - new Vector3(0f, (0f - m_collider.size.y) * 0.5f, m_collider.size.z * 0.5f));
		m_right = right;
		RaycastHit[] array = Physics.RaycastAll(m_left, Vector3.down, m_collider.size.y * 2f, -1073741825);
		if (array.Length != 0)
		{
			m_hitLeft = array[0].point;
		}
		array = Physics.RaycastAll(m_right, Vector3.down, m_collider.size.y * 2f, -1073741825);
		if (array.Length != 0)
		{
			m_hitRight = array[0].point;
		}
		m_pointsHeight.Clear();
		m_coundSteps = 0;
		m_pointsHeight.Add(m_hitLeft);
		m_pointsHeight.AddRange(Detalization(m_hitLeft, m_hitRight));
		m_pointsHeight.Add(m_hitRight);
		GenerateMeshByLine();
		m_mesh.Clear();
		m_mesh.SetVertices(m_vertices);
		m_mesh.SetUVs(0, m_uvs);
		m_mesh.SetTriangles(m_triangleIndices, 0);
		m_mesh.RecalculateNormals();
		m_mesh.RecalculateBounds();
		m_meshRenderer.material = material;
	}

	public void GenerateMeshByLine()
	{
		if (m_vertices == null)
		{
			m_vertices = new List<Vector3>(2);
			m_uvs = new List<Vector2>(2);
			m_triangleIndices = new List<int>(6);
		}
		else
		{
			m_vertices.Clear();
			m_uvs.Clear();
			m_triangleIndices.Clear();
		}
		GenerateFirst();
		for (int i = 1; i < m_pointsHeight.Count; i++)
		{
			GeneratePart(i);
		}
	}

	private void GenerateFirst()
	{
		int num = 1;
		int index = num - 1;
		float num2 = m_collider.size.x * 0.5f;
		float num3 = Vector3.Distance(m_pointsHeight[0], m_pointsHeight[m_pointsHeight.Count - 1]);
		float num4 = Vector3.Distance(m_pointsHeight[num], m_pointsHeight[m_pointsHeight.Count - 1]);
		float num5 = Vector3.Distance(m_pointsHeight[index], m_pointsHeight[m_pointsHeight.Count - 1]);
		m_drawingRays.Clear();
		Vector3 vector = base.transform.worldToLocalMatrix.MultiplyPoint3x4(m_pointsHeight[index]);
		Vector3 item = base.transform.worldToLocalMatrix.MultiplyPoint3x4(GetMeshPoint(base.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(num2, vector.y, vector.z))));
		m_vertices.Add(item);
		item = base.transform.worldToLocalMatrix.MultiplyPoint3x4(GetMeshPoint(base.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(0f - num2, vector.y, vector.z))));
		m_vertices.Add(item);
		vector = base.transform.worldToLocalMatrix.MultiplyPoint3x4(m_pointsHeight[num]);
		Vector3 item2 = base.transform.worldToLocalMatrix.MultiplyPoint3x4(GetMeshPoint(base.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(num2, vector.y, vector.z))));
		m_vertices.Add(item2);
		item2 = base.transform.worldToLocalMatrix.MultiplyPoint3x4(GetMeshPoint(base.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(0f - num2, vector.y, vector.z))));
		m_vertices.Add(item2);
		m_uvs.Add(offsetUV + new Vector2(num5 / num3 * scaleUV.x, 1f * scaleUV.y));
		m_uvs.Add(offsetUV + new Vector2(num5 / num3 * scaleUV.x, 0f));
		m_uvs.Add(offsetUV + new Vector2(num4 / num3 * scaleUV.x, 1f * scaleUV.y));
		m_uvs.Add(offsetUV + new Vector2(num4 / num3 * scaleUV.x, 0f));
		int num6 = m_vertices.Count - 4;
		m_triangleIndices.AddRange(new int[6]
		{
			num6,
			num6 + 3,
			num6 + 1,
			num6,
			num6 + 2,
			num6 + 3
		});
	}

	private void GeneratePart(int index)
	{
		float num = m_collider.size.x * 0.5f;
		float num2 = Vector3.Distance(m_pointsHeight[0], m_pointsHeight[m_pointsHeight.Count - 1]);
		float num3 = Vector3.Distance(m_pointsHeight[index], m_pointsHeight[m_pointsHeight.Count - 1]);
		Vector3 vector = base.transform.worldToLocalMatrix.MultiplyPoint3x4(m_pointsHeight[index]);
		Vector3 item = base.transform.worldToLocalMatrix.MultiplyPoint3x4(GetMeshPoint(base.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(num, vector.y, vector.z))));
		m_vertices.Add(item);
		item = base.transform.worldToLocalMatrix.MultiplyPoint3x4(GetMeshPoint(base.transform.localToWorldMatrix.MultiplyPoint3x4(new Vector3(0f - num, vector.y, vector.z))));
		m_vertices.Add(item);
		m_uvs.Add(offsetUV + new Vector2(num3 / num2 * scaleUV.x, 1f * scaleUV.y));
		m_uvs.Add(offsetUV + new Vector2(num3 / num2 * scaleUV.x, 0f));
		if (index != 0)
		{
			int num4 = m_vertices.Count - 4;
			m_triangleIndices.AddRange(new int[6]
			{
				num4,
				num4 + 3,
				num4 + 1,
				num4,
				num4 + 2,
				num4 + 3
			});
		}
	}

	private Vector3 GetMeshPoint(Vector3 globalPoint)
	{
		m_drawingRays.Add(globalPoint);
		RaycastHit[] array = Physics.RaycastAll(globalPoint + Vector3.up * 2f, Vector3.down, 10f, -1073741825);
		Vector3 vector = globalPoint;
		if (array.Length != 0)
		{
			vector = array[0].point;
			m_drawingRays.Add(array[0].point);
		}
		else
		{
			m_drawingRays.Add(globalPoint + Vector3.up);
		}
		return vector + base.transform.up * 0.001f;
	}

	private void DrawGizmos(bool selected)
	{
		Color magenta = Color.magenta;
		Color magenta2 = Color.magenta;
		magenta2.a = 0.3f;
		if (selected)
		{
			magenta.a = 1f;
			magenta2.a = 0.7f;
		}
		Matrix4x4 matrix = Gizmos.matrix;
		_ = base.transform.localToWorldMatrix;
		Gizmos.color = magenta;
		Gizmos.DrawWireCube(m_left, Vector3.one * 0.01f);
		Gizmos.DrawWireCube(m_right, Vector3.one * 0.01f);
		Gizmos.color = magenta2;
		Gizmos.DrawCube(m_left, Vector3.one * 0.01f);
		Gizmos.DrawCube(m_right, Vector3.one * 0.01f);
		Gizmos.matrix = matrix;
		Gizmos.DrawLine(m_left, m_hitLeft);
		Gizmos.DrawLine(m_right, m_hitRight);
		foreach (Vector3 item in m_pointsHeight)
		{
			Gizmos.DrawWireCube(item, Vector3.one * 0.01f);
		}
		for (int i = 0; i < m_drawingRays.Count; i += 2)
		{
			Gizmos.DrawLine(m_drawingRays[i], m_drawingRays[i + 1] + Vector3.up);
		}
	}

	private void OnDrawGizmos()
	{
		DrawGizmos(selected: false);
	}

	private void OnDrawGizmosSelected()
	{
		DrawGizmos(selected: true);
	}
}
