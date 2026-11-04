using Binding;
using Binding.Base;
using Chat.Overlay.UI.State;
using DG.Tweening;

namespace Chat.Overlay.UI.InputField
{
	public class InputFieldVisibilityHandler : BaseBindingTarget
	{
		private const float ShowAlpha = 1f;

		private const float HideAlpha = 0f;

		private const float FadeDuration = 0.2f;

		private const float HideDelay = 1f;

		private CanvasGroupProperty m_inputFieldCanvas = new CanvasGroupProperty();

		private StateContext m_state;

		private TweenRunner m_showTweenRunner = new TweenRunner();

		private TweenRunner m_hideTweenRunner = new TweenRunner();

		public void Setup(StateContext state)
		{
			m_state = state;
			m_state.showInputField.onChanged += OnInputFieldShowHandler;
		}

		public void Release()
		{
			m_state.showInputField.onChanged -= OnInputFieldShowHandler;
			m_state = null;
		}

		private void OnInputFieldShowHandler(bool needShow)
		{
			if (needShow)
			{
				m_hideTweenRunner.Release();
				m_showTweenRunner.Run(m_inputFieldCanvas.value.DOFade(1f, 0.2f));
			}
			else
			{
				m_hideTweenRunner.Run(m_inputFieldCanvas.value.DOFade(0f, 0.2f).SetDelay(1f));
			}
		}
	}
}
