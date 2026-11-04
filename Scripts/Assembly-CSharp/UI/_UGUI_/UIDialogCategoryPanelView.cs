using System;
using Binding;
using Binding.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI._UGUI_
{
	public class UIDialogCategoryPanelView : BaseBindingBehaviourTarget
	{
		[SerializeField]
		private TMP_Text m_title;

		[SerializeField]
		private Button m_button;

		public Action<int> OnClick;

		public readonly Property<bool> selected = new BoolProperty();

		public readonly Property<bool> hover = new BoolProperty();

		private int m_index;

		private void Awake()
		{
			m_button.onClick.AddListener(delegate
			{
				OnClick(m_index);
			});
		}

		private void OnDisable()
		{
			m_button.onClick.RemoveAllListeners();
		}

		public void SetTitle(string title)
		{
			m_title.SetText(title);
		}

		public void SetIndex(int index)
		{
			m_index = index;
		}
	}
}
