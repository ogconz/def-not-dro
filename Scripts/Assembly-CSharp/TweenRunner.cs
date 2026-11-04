using DG.Tweening;

public class TweenRunner
{
	private Tweener m_activeTween;

	public void Run(Tweener tweener)
	{
		Release();
		m_activeTween = tweener.OnComplete(Release);
	}

	public void Release()
	{
		if (m_activeTween != null)
		{
			m_activeTween.Kill();
			m_activeTween = null;
		}
	}
}
