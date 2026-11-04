using System.Collections;
using Binding;
using Binding.Base;
using UnityEngine;

namespace HUD
{
	public class UIDriftEvent : BehaviourBindingTarget
	{
		private StringProperty m_text = new StringProperty();

		private SpriteProperty m_icon = new SpriteProperty();

		private BoolProperty m_needBg = new BoolProperty();

		private BoolProperty m_skip = new BoolProperty(false);

		private BoolProperty m_clipping = new BoolProperty(false);

		[SerializeField]
		private Animator m_animator;

		public BaseDriftAchievement.AchievementType typeAchiev { get; private set; }

		private void Awake()
		{
			m_skip.OnValueChanged += Skip_OnValueChanged;
		}

		private void Skip_OnValueChanged()
		{
			if (m_skip.value && (bool)m_animator)
			{
				m_animator.Play("skip");
			}
		}

		public void Init(BaseDriftAchievement.AchievementType type, string text, bool needBg, Sprite icon, bool clipping)
		{
			typeAchiev = type;
			m_skip.value = false;
			m_text.value = text;
			m_icon.value = icon;
			m_needBg.value = needBg;
			m_clipping.value = clipping;
		}

		public void Release()
		{
			Object.Destroy(base.gameObject);
		}

		public IEnumerator RelseaseCoroutine(float delay)
		{
			yield return new WaitForSeconds(delay);
			Release();
		}

		public void Skip()
		{
			m_skip.value = true;
			StartCoroutine(RelseaseCoroutine(1f));
		}

		public void Show()
		{
			base.gameObject.SetActive(value: true);
		}
	}
}
