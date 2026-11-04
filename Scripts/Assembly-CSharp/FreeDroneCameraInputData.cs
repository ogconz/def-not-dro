using System.Collections.Generic;

public class FreeDroneCameraInputData
{
	public bool holdRotateButton;

	public float rotHorizontal;

	public float rotVertical;

	public float heightDown;

	public int changerFov;

	public bool turboMode;

	public bool changeFOV;

	public float movement;

	public float heightUp;

	public float strafe;

	public Dictionary<int, int> fovValues = new Dictionary<int, int>
	{
		{ 0, 10 },
		{ 1, 20 },
		{ 2, 40 },
		{ 3, 60 },
		{ 4, 80 }
	};

	private bool m_blockedInput;

	private int m_fovIndex;

	private float m_currFov;

	public bool fovChanged { get; internal set; }

	public int fovIndex
	{
		get
		{
			return m_fovIndex;
		}
		set
		{
			m_fovIndex = value;
			fovChanged = true;
		}
	}

	public bool blockedInput
	{
		get
		{
			return m_blockedInput;
		}
		set
		{
			m_blockedInput = value;
			if (value)
			{
				movement = 0f;
				strafe = 0f;
				heightUp = 0f;
				heightDown = 0f;
				holdRotateButton = false;
			}
		}
	}

	public void UpdateFovIndex(float currFovValue)
	{
		m_currFov = currFovValue;
		int count = fovValues.Keys.Count;
		for (int i = 0; i < count; i++)
		{
			if (i == count - 1)
			{
				fovIndex = i;
				break;
			}
			if (currFovValue >= (float)fovValues[i] && currFovValue < (float)fovValues[i + 1])
			{
				fovIndex = i;
				break;
			}
		}
		fovChanged = false;
	}

	public void IncrementFovIndex(bool allowWraparoundIndex)
	{
		float num = fovValues[fovIndex];
		if (m_currFov >= num)
		{
			if (fovIndex + 1 >= fovValues.Keys.Count)
			{
				fovIndex = ((!allowWraparoundIndex) ? (fovValues.Keys.Count - 1) : 0);
			}
			else
			{
				fovIndex++;
			}
		}
		else
		{
			fovIndex = fovIndex;
		}
	}

	public void DecrementFovIndex(bool allowWraparoundIndex)
	{
		float num = fovValues[fovIndex];
		if (m_currFov <= num)
		{
			if (fovIndex - 1 < 0)
			{
				fovIndex = (allowWraparoundIndex ? (fovValues.Keys.Count - 1) : 0);
			}
			else
			{
				fovIndex--;
			}
		}
		else
		{
			fovIndex = fovIndex;
		}
	}
}
