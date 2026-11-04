using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public class UISpinner : MonoBehaviour
{
	private static UISpinner m_instance;

	[SerializeField]
	private TextMeshProUGUI m_text;

	[SerializeField]
	private GameObject m_container;

	private void Awake()
	{
		m_instance = this;
		if (m_container == null)
		{
			m_container = base.gameObject;
		}
	}

	private IEnumerator ShowCoroutine(float delay)
	{
		if (delay > 0f)
		{
			yield return new WaitForSecondsRealtime(delay);
		}
		m_container.SetActive(value: true);
	}

	public static void Show(string text = null)
	{
		if ((bool)m_instance)
		{
			m_instance.m_text.text = (string.IsNullOrEmpty(text) ? LocalizationKeys.Loading.Localized() : text);
			m_instance.StopAllCoroutines();
			m_instance.StartCoroutine(m_instance.ShowCoroutine(0f));
		}
	}

	public static void Hide()
	{
		if ((bool)m_instance)
		{
			m_instance.StopAllCoroutines();
			m_instance.m_container.SetActive(value: false);
		}
	}

	public static void HideOnCarLoaded(RaceCar car)
	{
		if (car == null)
		{
			Hide();
			return;
		}
		if (car.IsVisualLoaded)
		{
			Hide();
			return;
		}
		Action<RaceCar> carLoaded = null;
		carLoaded = delegate
		{
			Hide();
			car.OnCarVisualLoadFinishEvent -= carLoaded;
		};
		car.OnCarVisualLoadFinishEvent += carLoaded;
	}

	public static async UniTask WaitUntil(Func<bool> predicate, int timeout)
	{
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout);
		await WaitUntil(predicate, cancellationTokenSource.Token);
	}

	public static async UniTask WaitUntil(Func<bool> predicate, CancellationToken cancellationToken)
	{
		Show();
		try
		{
			while (!predicate() && !cancellationToken.IsCancellationRequested)
			{
				await UniTask.Yield(cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
		}
		Hide();
	}
}
