using UnityEngine;

public static class GizmoNonEditor
{
	private static bool m_isInit = false;

	public static Camera currentCamera;

	public static Material currentMaterial;

	public static Matrix4x4 currentMatrix = Matrix4x4.identity;

	public static void Init()
	{
		if (!m_isInit)
		{
			m_isInit = true;
			currentMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
			currentMaterial.hideFlags = HideFlags.HideAndDontSave;
			currentMaterial.SetInt("_SrcBlend", 5);
			currentMaterial.SetInt("_DstBlend", 10);
			currentMaterial.SetInt("_Cull", 0);
			currentMaterial.SetInt("_ZWrite", 0);
		}
	}

	public static void DrawText(Vector3 position, Vector2 size, string text)
	{
		Vector3 vector = currentCamera.WorldToScreenPoint(position);
		GUI.Label(new Rect(vector.x, (float)Screen.height - vector.y, size.x, size.y), text);
	}

	public static void DrawLine(Vector3 firstPos, Vector3 lastPos, Color color)
	{
		currentMaterial.SetPass(0);
		GL.PushMatrix();
		GL.LoadProjectionMatrix(currentCamera.projectionMatrix);
		GL.modelview = currentCamera.worldToCameraMatrix;
		GL.MultMatrix(currentMatrix);
		GL.Begin(1);
		GL.Color(color);
		GL.Vertex3(firstPos.x, firstPos.y, firstPos.z);
		GL.Vertex3(lastPos.x, lastPos.y, lastPos.z);
		GL.End();
		GL.PopMatrix();
	}

	public static void DrawLineUI(Vector2 firstPos, Vector2 lastPos, Color color)
	{
		currentMaterial.SetPass(0);
		GL.Begin(1);
		GL.Color(color);
		GL.Vertex3(firstPos.x, firstPos.y, 0f);
		GL.Vertex3(lastPos.x, lastPos.y, 0f);
		GL.End();
	}

	public static Vector3[] GetBoxCorner(Vector3 center, Vector3 size)
	{
		return new Vector3[8]
		{
			new Vector3(center.x - size.x, center.y - size.y, center.z - size.z),
			new Vector3(center.x - size.x, center.y + size.y, center.z - size.z),
			new Vector3(center.x + size.x, center.y - size.y, center.z - size.z),
			new Vector3(center.x + size.x, center.y + size.y, center.z - size.z),
			new Vector3(center.x - size.x, center.y - size.y, center.z + size.z),
			new Vector3(center.x - size.x, center.y + size.y, center.z + size.z),
			new Vector3(center.x + size.x, center.y - size.y, center.z + size.z),
			new Vector3(center.x + size.x, center.y + size.y, center.z + size.z)
		};
	}

	public static Vector3[] GetBoxCorner(Vector3 center, Vector3 size, Matrix4x4 matrix4X4)
	{
		return new Vector3[8]
		{
			matrix4X4 * new Vector3(center.x - size.x, center.y - size.y, center.z - size.z),
			matrix4X4 * new Vector3(center.x - size.x, center.y + size.y, center.z - size.z),
			matrix4X4 * new Vector3(center.x + size.x, center.y - size.y, center.z - size.z),
			matrix4X4 * new Vector3(center.x + size.x, center.y + size.y, center.z - size.z),
			matrix4X4 * new Vector3(center.x - size.x, center.y - size.y, center.z + size.z),
			matrix4X4 * new Vector3(center.x - size.x, center.y + size.y, center.z + size.z),
			matrix4X4 * new Vector3(center.x + size.x, center.y - size.y, center.z + size.z),
			matrix4X4 * new Vector3(center.x + size.x, center.y + size.y, center.z + size.z)
		};
	}

	public static void DrawCube(Vector3 center, Vector3 size, Color color)
	{
		size /= 2f;
		Vector3[] boxCorner = GetBoxCorner(center, size);
		DrawCube(boxCorner[0], boxCorner[1], boxCorner[2], boxCorner[3], boxCorner[4], boxCorner[5], boxCorner[6], boxCorner[7], color);
	}

	public static void DrawCube(Vector3[] corners, Vector3 world, Color color)
	{
		DrawCube(corners[0] + world, corners[1] + world, corners[2] + world, corners[3] + world, corners[4] + world, corners[5] + world, corners[6] + world, corners[7] + world, color);
	}

	public static void DrawCube(Vector3 Rect1Pos1, Vector3 Rect1Pos2, Vector3 Rect1Pos3, Vector3 Rect1Pos4, Vector3 Rect2Pos1, Vector3 Rect2Pos2, Vector3 Rect2Pos3, Vector3 Rect2Pos4, Color color)
	{
		currentMaterial.SetPass(0);
		GL.PushMatrix();
		GL.LoadProjectionMatrix(currentCamera.projectionMatrix);
		GL.modelview = currentCamera.worldToCameraMatrix;
		GL.MultMatrix(currentMatrix);
		GL.Begin(1);
		GL.Color(color);
		GL.Vertex3(Rect1Pos1.x, Rect1Pos1.y, Rect1Pos1.z);
		GL.Vertex3(Rect1Pos2.x, Rect1Pos2.y, Rect1Pos2.z);
		GL.Vertex3(Rect1Pos1.x, Rect1Pos1.y, Rect1Pos2.z);
		GL.Vertex3(Rect1Pos3.x, Rect1Pos3.y, Rect1Pos3.z);
		GL.Vertex3(Rect1Pos3.x, Rect1Pos3.y, Rect1Pos3.z);
		GL.Vertex3(Rect1Pos4.x, Rect1Pos4.y, Rect1Pos4.z);
		GL.Vertex3(Rect1Pos4.x, Rect1Pos4.y, Rect1Pos4.z);
		GL.Vertex3(Rect1Pos2.x, Rect1Pos2.y, Rect1Pos2.z);
		GL.Vertex3(Rect2Pos1.x, Rect2Pos1.y, Rect2Pos1.z);
		GL.Vertex3(Rect2Pos2.x, Rect2Pos2.y, Rect2Pos2.z);
		GL.Vertex3(Rect2Pos1.x, Rect2Pos1.y, Rect2Pos1.z);
		GL.Vertex3(Rect2Pos3.x, Rect2Pos3.y, Rect2Pos3.z);
		GL.Vertex3(Rect2Pos3.x, Rect2Pos3.y, Rect2Pos3.z);
		GL.Vertex3(Rect2Pos4.x, Rect2Pos4.y, Rect2Pos4.z);
		GL.Vertex3(Rect2Pos4.x, Rect2Pos4.y, Rect2Pos4.z);
		GL.Vertex3(Rect2Pos2.x, Rect2Pos2.y, Rect2Pos2.z);
		GL.Vertex3(Rect1Pos1.x, Rect1Pos1.y, Rect1Pos1.z);
		GL.Vertex3(Rect2Pos1.x, Rect2Pos1.y, Rect2Pos1.z);
		GL.Vertex3(Rect1Pos2.x, Rect1Pos2.y, Rect1Pos2.z);
		GL.Vertex3(Rect2Pos2.x, Rect2Pos2.y, Rect2Pos2.z);
		GL.Vertex3(Rect1Pos3.x, Rect1Pos3.y, Rect1Pos3.z);
		GL.Vertex3(Rect2Pos3.x, Rect2Pos3.y, Rect2Pos3.z);
		GL.Vertex3(Rect1Pos4.x, Rect1Pos4.y, Rect1Pos4.z);
		GL.Vertex3(Rect2Pos4.x, Rect2Pos4.y, Rect2Pos4.z);
		GL.End();
		GL.PopMatrix();
	}
}
