using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Utils.ColorExtensions;

[RequireComponent(typeof(Image))]
public class ImageHoverTweenBinding : MonoBehaviour
{
	private RectTransform m_hoverTransform;

	private Image m_hoverImage;

	[SerializeField]
	private float m_initialAlphaValue;

	[SerializeField]
	private float m_hoverAlphaValue = 1f;

	[SerializeField]
	private float m_fadeDuration = 0.1f;

	[SerializeField]
	private float m_hoverAnimDuration = 0.35f;

	[SerializeField]
	private Vector3 m_startHoverScale = new Vector3(1.05f, 1.05f, 1f);

	private void Awake()
	{
		m_hoverTransform = GetComponent<RectTransform>();
		m_hoverImage = GetComponent<Image>();
	}

	private void OnEnable()
	{
		SetAnimate();
	}

	private void SetAnimate()
	{
		SetDefaultHoverValues();
		KillAllTweenAnimations();
		DOAnimation();
	}

	private void SetDefaultHoverValues()
	{
		m_hoverTransform.localScale = m_startHoverScale;
		m_hoverImage.SetAlpha(m_initialAlphaValue);
	}

	private void KillAllTweenAnimations()
	{
		m_hoverTransform.DOKill(complete: true);
		m_hoverImage.DOKill(complete: true);
	}

	private void DOAnimation()
	{
		m_hoverImage.DOFade(m_hoverAlphaValue, m_fadeDuration).SetEase(Ease.InOutQuad).SetUpdate(isIndependentUpdate: true);
		m_hoverTransform.DOScale(Vector3.one, m_hoverAnimDuration).SetEase(Ease.OutExpo).SetUpdate(isIndependentUpdate: true);
	}
}
