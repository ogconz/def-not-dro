using System;
using UnityEngine;

namespace TrackLayout.Behaviours
{
	public class TriggerZone : MonoBehaviour
	{
		public Action<Collider> onTriggerEnter;

		public Action<Collider> onTriggerStay;

		public Action<Collider> onTriggerExit;

		public Color editorColor = Color.clear;

		public Vector3 labelOffset;

		private BoxCollider m_boxCollider;

		private Camera m_camera;

		private void Start()
		{
			m_boxCollider = GetComponent<BoxCollider>();
			m_camera = Camera.main;
			GizmoNonEditor.Init();
		}

		private void OnTriggerEnter(Collider other)
		{
			if (onTriggerEnter != null)
			{
				onTriggerEnter(other);
			}
		}

		private void OnGUI()
		{
			if (editorColor != Color.clear && m_boxCollider != null)
			{
				GizmoNonEditor.currentCamera = m_camera;
				GizmoNonEditor.currentMatrix = base.transform.localToWorldMatrix;
				Vector3 vector = m_boxCollider.center + base.transform.position + Vector3.down * m_boxCollider.size.y / 2f;
				GizmoNonEditor.DrawText(vector + labelOffset, new Vector2(200f, 40f), base.name);
				vector = base.transform.InverseTransformPoint(vector);
				GizmoNonEditor.DrawCube(vector, new Vector3(m_boxCollider.size.x, 0.25f, m_boxCollider.size.z), editorColor);
			}
		}

		private void OnTriggerStay(Collider other)
		{
			if (onTriggerStay != null)
			{
				onTriggerStay(other);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (onTriggerExit != null)
			{
				onTriggerExit(other);
			}
		}
	}
}
