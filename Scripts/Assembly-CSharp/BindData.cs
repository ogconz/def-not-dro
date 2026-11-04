using System;
using GameInput;

[Serializable]
public class BindData
{
	public GameInput.Action action;

	public BindElement baseElement;

	public BindButtonElement elementButton;

	public BindAxisElement elementAxis;

	public bool invert;

	public float minAxis;

	public float maxAxis;

	public float currentAxis;

	public float currentNormalizedAxis;

	public bool calibrationMode;

	public int indexNavigate = -1;

	public bool setPropsMode;

	public void CopyTo(BindData to)
	{
		if (to != null)
		{
			to.invert = invert;
			to.minAxis = minAxis;
			to.maxAxis = maxAxis;
			to.currentAxis = currentAxis;
			to.currentNormalizedAxis = currentNormalizedAxis;
		}
	}

	public void UpdateAxisMinMax()
	{
		if (!setPropsMode && elementAxis != null)
		{
			elementAxis.minValue = minAxis * 0.001f;
			elementAxis.maxValue = maxAxis * 0.001f;
		}
	}

	public void OnActionInvertAxisPressDown()
	{
		if (!setPropsMode && elementAxis != null)
		{
			invert = !invert;
			elementAxis.invert = invert;
		}
	}

	public bool NavigateDown()
	{
		if (elementAxis != null)
		{
			indexNavigate++;
			if (indexNavigate > 2)
			{
				return true;
			}
		}
		else
		{
			indexNavigate++;
			if (indexNavigate >= 1)
			{
				return true;
			}
		}
		return false;
	}

	public bool NavigateUp()
	{
		indexNavigate--;
		return indexNavigate < 0;
	}
}
