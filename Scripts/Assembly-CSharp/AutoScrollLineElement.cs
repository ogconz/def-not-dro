using System.Collections;
using Binding.Base;
using UnityEngine;

public class AutoScrollLineElement : BaseBindingBehaviourTarget
{
	private const float TrackNameLabelsOffset = 60f;

	[SerializeField]
	private RectTransform m_originalTextLineRect;

	[SerializeField]
	private RectTransform m_cloneTextLineRect;

	[SerializeField]
	private RectTransform m_textLineMask;

	private Coroutine m_autoscrollLabelsRoutine;

	private bool m_needResetLabel;

	private void Update()
	{
		if (Time.frameCount % 3 == 0)
		{
			HandleTrackNameLabelAutoScrolling();
		}
	}

	private void OnDisable()
	{
		if (m_autoscrollLabelsRoutine != null)
		{
			StopAutoScrollingLabels();
		}
	}

	private void HandleTrackNameLabelAutoScrolling()
	{
		if (CheckTrackNameLabelAutoscrollCondition())
		{
			if (m_autoscrollLabelsRoutine == null)
			{
				m_autoscrollLabelsRoutine = TimeController.StartNow(AutoScrollingLabels());
			}
			m_needResetLabel = false;
			return;
		}
		if (!m_needResetLabel)
		{
			ResetTrackNameRect();
			m_cloneTextLineRect.gameObject.SetActive(value: false);
			m_needResetLabel = true;
		}
		if (m_autoscrollLabelsRoutine != null)
		{
			StopAutoScrollingLabels();
		}
	}

	private IEnumerator AutoScrollingLabels()
	{
		ResetTrackNameRect();
		m_cloneTextLineRect.gameObject.SetActive(value: true);
		while (true)
		{
			for (int i = 0; i < 3; i++)
			{
				yield return null;
			}
			ReturnHiddenScrollText(m_originalTextLineRect, m_cloneTextLineRect);
			MoveScrollText(m_originalTextLineRect);
			ReturnHiddenScrollText(m_cloneTextLineRect, m_originalTextLineRect);
			MoveScrollText(m_cloneTextLineRect);
		}
	}

	private void ReturnHiddenScrollText(RectTransform rect, RectTransform offsetRect)
	{
		float num = m_textLineMask.anchoredPosition.x - m_textLineMask.GetWidth() / 2f;
		if (rect.anchoredPosition.x + rect.GetWidth() / 2f < num)
		{
			SetLabelRectPosByOffset(rect, offsetRect);
		}
	}

	private void MoveScrollText(RectTransform rect)
	{
		rect.localPosition += Vector3.left * (90f * Time.deltaTime);
	}

	private bool CheckTrackNameLabelAutoscrollCondition()
	{
		return m_originalTextLineRect.GetWidth() > m_textLineMask.GetWidth();
	}

	private void ResetTrackNameRect()
	{
		m_originalTextLineRect.anchoredPosition = Vector2.zero;
		SetLabelRectPosByOffset(m_cloneTextLineRect, m_originalTextLineRect);
	}

	private void StopAutoScrollingLabels()
	{
		TimeController.StopNow(m_autoscrollLabelsRoutine);
		m_autoscrollLabelsRoutine = null;
	}

	private void SetLabelRectPosByOffset(RectTransform rect, RectTransform offsetRect)
	{
		float num = offsetRect.anchoredPosition.x + offsetRect.GetWidth() + 60f;
		rect.anchoredPosition = Vector2.right * num;
	}
}
