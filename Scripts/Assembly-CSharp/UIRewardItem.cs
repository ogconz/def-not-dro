using Binding;
using Binding.Base;
using UnityEngine;

public class UIRewardItem : BaseBindingBehaviourTarget
{
	private readonly SpriteProperty m_sprite = new SpriteProperty();

	private readonly StringProperty m_itemsCount = new StringProperty();

	private readonly StringProperty m_multiplierText = new StringProperty();

	private readonly BoolProperty m_multiplierActive = new BoolProperty(false);

	public void SetItemData(Sprite sprite, string count, int multiplier = 1)
	{
		m_sprite.value = sprite;
		m_itemsCount.value = count;
		m_multiplierActive.value = multiplier > 1;
		m_multiplierText.value = $"{multiplier}x";
	}
}
