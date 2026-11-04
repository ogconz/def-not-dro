using UnityEngine;

public static class RectTransformExtensions
{
	public static void SetDefaultScale(this RectTransform trans)
	{
		trans.localScale = Vector3.one;
	}

	public static void SetPivotAndAnchors(this RectTransform trans, Vector2 aVec)
	{
		trans.pivot = aVec;
		trans.anchorMin = aVec;
		trans.anchorMax = aVec;
	}

	public static Vector2 GetSize(this RectTransform trans)
	{
		return trans.rect.size;
	}

	public static float GetWidth(this RectTransform trans)
	{
		return trans.rect.width;
	}

	public static float GetHeight(this RectTransform trans)
	{
		return trans.rect.height;
	}

	public static void SetPositionOfPivot(this RectTransform trans, Vector2 newPos)
	{
		trans.localPosition = new Vector3(newPos.x, newPos.y, trans.localPosition.z);
	}

	public static void SetLeftBottomPosition(this RectTransform trans, Vector2 newPos)
	{
		trans.localPosition = new Vector3(newPos.x + trans.pivot.x * trans.rect.width, newPos.y + trans.pivot.y * trans.rect.height, trans.localPosition.z);
	}

	public static void SetLeftTopPosition(this RectTransform trans, Vector2 newPos)
	{
		trans.localPosition = new Vector3(newPos.x + trans.pivot.x * trans.rect.width, newPos.y - (1f - trans.pivot.y) * trans.rect.height, trans.localPosition.z);
	}

	public static void SetRightBottomPosition(this RectTransform trans, Vector2 newPos)
	{
		trans.localPosition = new Vector3(newPos.x - (1f - trans.pivot.x) * trans.rect.width, newPos.y + trans.pivot.y * trans.rect.height, trans.localPosition.z);
	}

	public static void SetRightTopPosition(this RectTransform trans, Vector2 newPos)
	{
		trans.localPosition = new Vector3(newPos.x - (1f - trans.pivot.x) * trans.rect.width, newPos.y - (1f - trans.pivot.y) * trans.rect.height, trans.localPosition.z);
	}

	public static void SetSize(this RectTransform trans, Vector2 newSize)
	{
		Vector2 size = trans.rect.size;
		Vector2 vector = newSize - size;
		trans.offsetMin -= new Vector2(vector.x * trans.pivot.x, vector.y * trans.pivot.y);
		trans.offsetMax += new Vector2(vector.x * (1f - trans.pivot.x), vector.y * (1f - trans.pivot.y));
	}

	public static void SetWidth(this RectTransform trans, float newSize)
	{
		trans.SetSize(new Vector2(newSize, trans.rect.size.y));
	}

	public static void SetHeight(this RectTransform trans, float newSize)
	{
		trans.SetSize(new Vector2(trans.rect.size.x, newSize));
	}

	public static void TieToParent(this RectTransform rect, Transform parent)
	{
		rect.SetParent(parent);
		rect.anchorMin = Vector2.zero;
		rect.anchorMax = Vector2.one;
		rect.offsetMin = Vector2.zero;
		rect.offsetMax = Vector2.zero;
	}

	public static Rect GetScreenRect(this RectTransform rectTransform)
	{
		Vector3[] array = new Vector3[4];
		rectTransform.GetWorldCorners(array);
		float num = float.PositiveInfinity;
		float num2 = float.NegativeInfinity;
		float num3 = float.PositiveInfinity;
		float num4 = float.NegativeInfinity;
		for (int i = 0; i < 4; i++)
		{
			Vector3 vector = RectTransformUtility.WorldToScreenPoint(null, array[i]);
			if (vector.x < num)
			{
				num = vector.x;
			}
			if (vector.x > num2)
			{
				num2 = vector.x;
			}
			if (vector.y < num3)
			{
				num3 = vector.y;
			}
			if (vector.y > num4)
			{
				num4 = vector.y;
			}
		}
		return new Rect(num, num3, num2 - num, num4 - num3);
	}

	public static bool Overlaps(this RectTransform a, RectTransform b)
	{
		return a.WorldRect().Overlaps(b.WorldRect());
	}

	public static bool Overlaps(this RectTransform a, RectTransform b, bool allowInverse)
	{
		return a.WorldRect().Overlaps(b.WorldRect(), allowInverse);
	}

	public static Rect WorldRect(this RectTransform rectTransform)
	{
		Vector2 sizeDelta = rectTransform.sizeDelta;
		float num = sizeDelta.x * rectTransform.lossyScale.x;
		float num2 = sizeDelta.y * rectTransform.lossyScale.y;
		Vector3 position = rectTransform.position;
		return new Rect(position.x - num * 0.5f, position.y - num2 * 0.5f, num, num2);
	}
}
