using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using DB;
using DI;
using Db.Meta.CarProfile;
using UnityEngine;

namespace CarProfile
{
	[Serializable]
	public class CarProfileChanger
	{
		private static CarProfileParameterMeta m_nullMeta = new CarProfileParameterMeta();

		[XmlAttribute]
		public int parameterId;

		private CarProfileParameterMeta m_parameter;

		[XmlAttribute]
		[DefaultValue(1f)]
		public float multiplier = 1f;

		[XmlAttribute]
		[DefaultValue(0f)]
		public float shift;

		[XmlAttribute]
		[DefaultValue(0f)]
		public float step;

		[XmlAttribute]
		public float defaultValue;

		[SerializeField]
		private List<float> m_values;

		private const int MAX_TORQUE_PARAMETER_ID = 28;

		[XmlIgnore]
		public CarProfileParameterMeta parameter
		{
			get
			{
				return ResolveParameter();
			}
			set
			{
				m_parameter = value;
			}
		}

		[XmlIgnore]
		public CarProfileChangerType changerType
		{
			get
			{
				if (values.Count > 2 || parameter.isBool)
				{
					return CarProfileChangerType.Fixed;
				}
				if (step == 0f)
				{
					return CarProfileChangerType.Interpolated;
				}
				return CarProfileChangerType.Discrete;
			}
		}

		[XmlAttribute("values")]
		public float[] xmlValues
		{
			get
			{
				return values.ToArray();
			}
			set
			{
				values = value.ToList();
			}
		}

		[XmlIgnore]
		public List<float> values
		{
			get
			{
				return m_values;
			}
			set
			{
				m_values = value;
			}
		}

		public float EvaluateValue(float phase, bool isPercent = false)
		{
			if (values.Count == 0)
			{
				return 0f;
			}
			phase = Mathf.Clamp01(phase);
			switch (changerType)
			{
			case CarProfileChangerType.Fixed:
				return values[Mathf.RoundToInt(phase * (float)(values.Count - 1))];
			case CarProfileChangerType.Discrete:
			{
				if (step < float.Epsilon || isPercent)
				{
					return Mathf.Lerp(values[0], values[values.Count - 1], phase);
				}
				float num = Mathf.Lerp(values[0], values[values.Count - 1], phase);
				return Mathf.Clamp(num - (num + 0.5f * step) % step + (values[0] + 0.5f * step) % step, values[0], values[values.Count - 1]);
			}
			case CarProfileChangerType.Interpolated:
				return Mathf.Lerp(values[0], values[values.Count - 1], phase);
			default:
				return values[0];
			}
		}

		public float EvaluateOriginalValue(float phase, bool isPercent = false)
		{
			float num = EvaluateValue(phase, isPercent);
			if (changerType == CarProfileChangerType.Interpolated)
			{
				num += shift;
			}
			return num;
		}

		public float EvaluateOriginalValue(float phase, int roundDigits)
		{
			return (float)Math.Round(EvaluateOriginalValue(phase), roundDigits);
		}

		public float EvaluatePhase(float value, bool isPercent = false)
		{
			if (values.Count == 0)
			{
				return 0f;
			}
			switch (changerType)
			{
			case CarProfileChangerType.Fixed:
			{
				if (values.Count == 1)
				{
					return values[0];
				}
				int num2 = 0;
				float num3 = float.MaxValue;
				for (int i = 0; i < values.Count; i++)
				{
					float num4 = Mathf.Abs(value - values[i]);
					if (num4 < num3)
					{
						num3 = num4;
						num2 = i;
					}
				}
				return (float)num2 / (float)(values.Count - 1);
			}
			case CarProfileChangerType.Discrete:
			{
				if (step < float.Epsilon || isPercent)
				{
					value = Mathf.Clamp(value, values[0], values[values.Count - 1]);
					float num5 = values[values.Count - 1] - values[0];
					if (!(num5 > 0f))
					{
						return 0.5f;
					}
					return (value - values[0]) / num5;
				}
				float num6 = values[values.Count - 1] - values[0];
				if (num6 > 0f)
				{
					float num7 = values[0];
					float num8 = values[values.Count - 1];
					float num9 = float.MaxValue;
					float num10 = 0f;
					for (; num7 <= num8; num7 += step)
					{
						float num11 = Mathf.Abs(value - num7);
						if (num11 < num9)
						{
							num9 = num11;
							num10 = num7;
						}
					}
					return (num10 - values[0]) / num6;
				}
				return 0f;
			}
			case CarProfileChangerType.Interpolated:
			{
				if (parameterId == 28)
				{
					value -= shift;
				}
				value = Mathf.Clamp(value, values[0], values[values.Count - 1]);
				float num = values[values.Count - 1] - values[0];
				if (!(num > 0f))
				{
					return 0.5f;
				}
				return (value - values[0]) / num;
			}
			default:
				return values[0];
			}
		}

		public CarProfileChanger()
		{
			step = 0f;
			multiplier = 1f;
			shift = 0f;
			defaultValue = 0f;
			values = new List<float>();
			parameterId = 0;
		}

		public CarProfileChanger(float[] valuesArr, float defValue, int paramId, float mult, string units, string format)
		{
			step = 0f;
			multiplier = mult;
			shift = 0f;
			defaultValue = defValue;
			values = new List<float>(valuesArr);
			parameterId = paramId;
		}

		public CarProfileChanger(float min, float max, float defValue, int paramId, float mult, string units, string format, float shift = 0f)
		{
			step = 0f;
			multiplier = mult;
			this.shift = shift;
			defaultValue = defValue - shift;
			values = new List<float>
			{
				min - shift,
				max - shift
			};
			parameterId = paramId;
		}

		public CarProfileChanger(float min, float max, float defValue, int discreteStepsCount, int paramId, float mult, string units, string format)
		{
			multiplier = mult;
			shift = 0f;
			step = (max - min) / ((discreteStepsCount > 0) ? ((float)discreteStepsCount) : 1f);
			defaultValue = defValue;
			values = new List<float> { min, max };
			parameterId = paramId;
		}

		private CarProfileParameterMeta ResolveParameter()
		{
			if (m_parameter == null)
			{
				m_parameter = DependencyInjector.Resolve<BaseModel>().GetById<CarProfileParameterMeta>(parameterId);
				if (m_parameter == null)
				{
					m_parameter = m_nullMeta;
				}
			}
			return m_parameter;
		}

		public CarProfileChanger Copy()
		{
			return new CarProfileChanger
			{
				parameterId = parameterId,
				multiplier = multiplier,
				shift = shift,
				step = step,
				defaultValue = defaultValue,
				values = ((values == null) ? new List<float>() : new List<float>(values)),
				m_parameter = m_parameter.Copy()
			};
		}
	}
}
