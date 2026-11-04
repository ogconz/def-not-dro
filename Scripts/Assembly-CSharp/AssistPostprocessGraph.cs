using UnityEngine;
using UnityEngine.UI.Extensions;

public class AssistPostprocessGraph : MonoBehaviour
{
	[SerializeField]
	private float m_minX;

	[SerializeField]
	private float m_maxX;

	[SerializeField]
	private float m_minY;

	[SerializeField]
	private float m_maxY;

	[SerializeField]
	private UILineRenderer m_ppBounds;

	[SerializeField]
	private UILineRenderer m_ppBegin;

	[SerializeField]
	private UILineRenderer m_ppEnd;

	[SerializeField]
	private RectTransform m_xLabel;

	[SerializeField]
	private RectTransform m_yLabel;

	public void DrawGraph(float xValue, float yValue)
	{
		float x = xValue * (m_maxX - m_minX) + m_minX;
		float y = yValue * (m_maxY - m_minY) + m_minY;
		Vector3 localPosition = m_xLabel.localPosition;
		m_xLabel.localPosition = new Vector3(x, localPosition.y, localPosition.z);
		Vector3 localPosition2 = m_yLabel.localPosition;
		m_yLabel.localPosition = new Vector3(localPosition2.x, y, localPosition2.z);
		if (m_ppBounds.Points.Length >= 3)
		{
			m_ppBounds.Points[0] = new Vector2(m_ppBounds.Points[0].x, y);
			m_ppBounds.Points[1] = new Vector2(x, y);
			m_ppBounds.Points[2] = new Vector2(x, m_ppBounds.Points[2].y);
			m_ppBounds.SetAllDirty();
		}
		if (m_ppBegin.Points.Length >= 2)
		{
			m_ppBegin.Points[1] = new Vector2(x, y);
			m_ppBegin.SetAllDirty();
		}
		if (m_ppEnd.Points.Length >= 1)
		{
			m_ppEnd.Points[0] = new Vector2(x, y);
			m_ppEnd.SetAllDirty();
		}
	}

	public void SetVisible(bool visible)
	{
		base.gameObject.SetActive(visible);
	}
}
