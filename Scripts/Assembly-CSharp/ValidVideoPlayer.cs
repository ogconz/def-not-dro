using UnityEngine;
using UnityEngine.Video;

public class ValidVideoPlayer : IValidComponentProcess
{
	private int m_videoMaxWidth;

	private int m_videoMaxHeight;

	private int m_maxFramerate;

	private int m_maxTimeInSecond;

	public bool isSuccess { get; private set; } = true;

	public string processMessage { get; private set; }

	public void Reset()
	{
		processMessage = string.Empty;
		isSuccess = true;
	}

	public ValidVideoPlayer(int videoMaxWidth = 1280, int videoMaxHeight = 720, int maxFramerate = 30, int maxTimeInSecond = 15)
	{
		m_videoMaxWidth = videoMaxWidth;
		m_videoMaxHeight = videoMaxHeight;
		m_maxFramerate = maxFramerate;
		m_maxTimeInSecond = maxTimeInSecond;
	}

	public void ValidProcess(Component comp)
	{
		if (!(comp.GetType().Name == "VideoPlayer"))
		{
			return;
		}
		VideoPlayer component = comp.gameObject.GetComponent<VideoPlayer>();
		string text = string.Empty;
		if (component.clip != null)
		{
			if (component.clip.length > (double)m_maxTimeInSecond)
			{
				text += $"{comp.gameObject.name} | Video max duration is {m_maxTimeInSecond} sec\n";
			}
			if (component.clip.frameRate > (double)m_maxFramerate)
			{
				text += $"{comp.gameObject.name} | Video max framerate is {m_maxFramerate}\n";
			}
			if (component.clip.width > m_videoMaxWidth || component.clip.height > m_videoMaxHeight)
			{
				text += $"{comp.gameObject.name} | Video max size is {m_videoMaxWidth} / {m_videoMaxHeight}\n";
			}
		}
		if (component != null && component.source == VideoSource.Url)
		{
			text += $"{comp.gameObject.name} | No support : {component.source}";
		}
		if (!string.IsNullOrEmpty(text))
		{
			isSuccess = false;
			processMessage = text;
		}
	}
}
