using System;
using Binding;
using Binding.Base;

namespace Championship
{
	public class UITop32ResultListItem : BaseBindingBehaviourTarget
	{
		private StringProperty m_attemptLabel = new StringProperty();

		private StringProperty m_scoresLabel = new StringProperty();

		private BoolProperty m_selected = new BoolProperty();

		public Action onClick;

		private ElementNavigate m_elementNavigate;

		private void Awake()
		{
			m_elementNavigate = GetComponent<ElementNavigate>();
		}

		public void SetUIData(int attempt, float result)
		{
			string value = DriftControllerXDS.ConvertScoresToResultString(result);
			m_attemptLabel.value = $"{LocalizationKeys.top32_attempt.Localized()} {attempt}:";
			m_scoresLabel.value = value;
		}

		public void OnSelect()
		{
			m_selected.value = true;
		}

		public void OnClick()
		{
			if (m_elementNavigate != null)
			{
				m_elementNavigate.Select();
			}
		}

		public void OnDeselect()
		{
			m_selected.value = false;
		}
	}
}
