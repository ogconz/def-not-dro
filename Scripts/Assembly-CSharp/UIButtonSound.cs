using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonSound : MonoBehaviour, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
	public enum Trigger
	{
		Click = 0,
		RightClick = 1,
		Down = 2,
		Up = 3,
		Enter = 4,
		Exit = 5
	}

	[Range(0f, 1f)]
	public float volume = 1f;

	[Range(0f, 2f)]
	public float pitch = 1f;

	[SerializeField]
	private Trigger m_trigger;

	[SerializeField]
	private SoundSample m_clip;

	private Selectable m_target;

	private void Awake()
	{
		m_target = GetComponent<Selectable>();
	}

	private bool TryPlaySound(Trigger trigger)
	{
		if (trigger != m_trigger)
		{
			return false;
		}
		if (m_target == null || m_target.interactable)
		{
			PlaySound();
			return true;
		}
		return false;
	}

	private void PlaySound()
	{
		SoundController.Play2DSound(m_clip, volume, pitch);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			TryPlaySound(Trigger.RightClick);
		}
		else
		{
			TryPlaySound(Trigger.Click);
		}
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		TryPlaySound(Trigger.Enter);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		TryPlaySound(Trigger.Exit);
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		TryPlaySound(Trigger.Down);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		TryPlaySound(Trigger.Up);
	}
}
