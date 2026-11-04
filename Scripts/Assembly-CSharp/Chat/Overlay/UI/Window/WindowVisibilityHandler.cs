using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;
using DG.Tweening;

namespace Chat.Overlay.UI.Window
{
	public class WindowVisibilityHandler : BaseBindingTarget
	{
		private const float ShowAlpha = 1f;

		private const float HideAlpha = 0.4f;

		private const float FadeDuration = 0.2f;

		private const float HideDelay = 1f;

		private CanvasGroupProperty m_windowCanvas = new CanvasGroupProperty();

		private StateContext m_state;

		private TweenRunner m_showTweenRunner = new TweenRunner();

		private TweenRunner m_hideTweenRunner = new TweenRunner();

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.showWindow.onChanged += OnWindowShowHandler;
			m_windowCanvas.value.interactable = true;
			m_windowCanvas.value.blocksRaycasts = true;
		}

		public void Release()
		{
			m_windowCanvas.value.interactable = false;
			m_windowCanvas.value.blocksRaycasts = false;
			m_state.showWindow.onChanged -= OnWindowShowHandler;
			m_state = null;
		}

		private void OnWindowShowHandler(bool needShow)
		{
			if (needShow)
			{
				m_hideTweenRunner.Release();
				m_showTweenRunner.Run(m_windowCanvas.value.DOFade(1f, 0.2f));
			}
			else
			{
				m_hideTweenRunner.Run(m_windowCanvas.value.DOFade(0.4f, 0.2f).SetDelay(1f));
			}
		}
	}
}
