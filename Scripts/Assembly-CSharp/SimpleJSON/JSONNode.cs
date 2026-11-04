using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SimpleJSON
{
	internal class JSONNode
	{
		public static int allocations;

		public virtual JSONNode this[int aIndex]
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual JSONNode this[string aKey]
		{
			get
			{
				return null;
			}
			set
			{
			}
		}

		public virtual string Value
		{
			get
			{
				return "";
			}
			set
			{
			}
		}

		public virtual int Count => 0;

		public virtual IEnumerable<JSONNode> Children
		{
			get
			{
				yield break;
			}
		}

		public IEnumerable<JSONNode> DeepChildren
		{
			get
			{
				foreach (JSONNode child in Children)
				{
					foreach (JSONNode deepChild in child.DeepChildren)
					{
						yield return deepChild;
					}
				}
			}
		}

		public virtual int AsInt
		{
			get
			{
				int result = 0;
				if (int.TryParse(Value, out result))
				{
					return result;
				}
				return 0;
			}
			set
			{
				Value = FastIntToString(value);
			}
		}

		public virtual float AsFloat
		{
			get
			{
				float result = 0f;
				if (float.TryParse(Value, out result))
				{
					return result;
				}
				return 0f;
			}
			set
			{
				Value = value.ToString();
			}
		}

		public virtual double AsDouble
		{
			get
			{
				double result = 0.0;
				if (double.TryParse(Value, out result))
				{
					return result;
				}
				return 0.0;
			}
			set
			{
				Value = value.ToString();
			}
		}

		public virtual bool AsBool
		{
			get
			{
				bool result = false;
				if (bool.TryParse(Value, out result))
				{
					return result;
				}
				return !string.IsNullOrEmpty(Value);
			}
			set
			{
				Value = (value ? "true" : "false");
			}
		}

		public virtual JSONArray AsArray => this as JSONArray;

		public virtual JSONClass AsObject => this as JSONClass;

		public virtual void Add(string aKey, JSONNode aItem)
		{
		}

		protected JSONNode()
		{
			allocations++;
		}

		public virtual void Free()
		{
			allocations--;
		}

		public bool IsNullValue()
		{
			string value = Value;
			if (value.Length == 4)
			{
				return "null".Equals(value, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		public virtual void Add(JSONNode aItem)
		{
			Add("", aItem);
		}

		public virtual JSONNode Remove(string aKey)
		{
			return null;
		}

		public virtual JSONNode Remove(int aIndex)
		{
			return null;
		}

		public virtual JSONNode Remove(JSONNode aNode)
		{
			return aNode;
		}

		public virtual bool HasKey(string key)
		{
			return false;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			ToString(ref sb);
			return sb.ToString();
		}

		public virtual void ToString(ref StringBuilder sb)
		{
			sb.Append("JSONNode");
		}

		public virtual void ToString(ref StringBuilder sb, string aPrefix)
		{
			sb.Append("JSONNode");
		}

		public static string FastIntToString(int value)
		{
			if (value >= 0)
			{
				return UIUtils.NumToString((uint)value);
			}
			return value.ToString();
		}

		public static implicit operator string(JSONNode d)
		{
			if (!(d == null))
			{
				return d.Value;
			}
			return null;
		}

		public static bool operator ==(JSONNode a, object b)
		{
			if (b == null && a is JSONLazyCreator)
			{
				return true;
			}
			return (object)a == b;
		}

		public static bool operator !=(JSONNode a, object b)
		{
			return !(a == b);
		}

		public override bool Equals(object obj)
		{
			return (object)this == obj;
		}

		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		internal static void Escape(ref StringBuilder sb, string aText)
		{
			foreach (char c in aText)
			{
				switch (c)
				{
				case '\\':
					sb.Append("\\\\");
					break;
				case '"':
					sb.Append("\\\"");
					break;
				case '\n':
					sb.Append("\\n");
					break;
				case '\r':
					sb.Append("\\r");
					break;
				case '\t':
					sb.Append("\\t");
					break;
				case '\b':
					sb.Append("\\b");
					break;
				case '\f':
					sb.Append("\\f");
					break;
				default:
					sb.Append(c);
					break;
				}
			}
		}

		internal static JSONNode Parse(string aJSON, JSONCache cache)
		{
			Stack<JSONNode> stack = new Stack<JSONNode>(16);
			JSONNode jSONNode = null;
			int i = 0;
			StringBuilder stringBuilder = cache.stringBuilder;
			stringBuilder.Clear();
			string text = "";
			bool flag = false;
			int num = 0;
			for (; i < aJSON.Length; i++)
			{
				switch (aJSON[i])
				{
				case '{':
					if (flag)
					{
						stringBuilder.Append(aJSON[i]);
						continue;
					}
					stack.Push(cache.AllocateJSONClass());
					if (jSONNode != null)
					{
						text = text.Trim();
						if (jSONNode is JSONArray)
						{
							jSONNode.Add(stack.Peek());
						}
						else if (text != "")
						{
							jSONNode.Add(text, stack.Peek());
						}
					}
					text = "";
					stringBuilder.Clear();
					jSONNode = stack.Peek();
					continue;
				case '[':
					if (flag)
					{
						stringBuilder.Append(aJSON[i]);
						continue;
					}
					stack.Push(cache.AllocateJSONArray());
					if (jSONNode != null)
					{
						text = text.Trim();
						if (jSONNode is JSONArray)
						{
							jSONNode.Add(stack.Peek());
						}
						else if (text != "")
						{
							jSONNode.Add(text, stack.Peek());
						}
					}
					text = "";
					stringBuilder.Clear();
					jSONNode = stack.Peek();
					continue;
				case ']':
				case '}':
					if (flag)
					{
						stringBuilder.Append(aJSON[i]);
						continue;
					}
					if (stack.Count == 0)
					{
						throw new Exception("JSON Parse: Too many closing brackets");
					}
					stack.Pop();
					if (stringBuilder.Length != 0)
					{
						text = text.Trim();
						if (jSONNode is JSONArray)
						{
							jSONNode.Add(cache.AllocateJSONData(stringBuilder.ToString()));
						}
						else if (text != "")
						{
							jSONNode.Add(text, cache.AllocateJSONData(stringBuilder.ToString()));
						}
					}
					text = "";
					stringBuilder.Clear();
					if (stack.Count > 0)
					{
						jSONNode = stack.Peek();
					}
					continue;
				case ':':
					if (flag)
					{
						stringBuilder.Append(aJSON[i]);
						continue;
					}
					text = stringBuilder.ToString();
					stringBuilder.Clear();
					continue;
				case '"':
					flag = !flag;
					num = (flag ? (i + 1) : 0);
					continue;
				case ',':
					if (flag)
					{
						stringBuilder.Append(aJSON[i]);
						continue;
					}
					if (stringBuilder.Length != 0)
					{
						if (jSONNode is JSONArray)
						{
							jSONNode.Add(cache.AllocateJSONData(stringBuilder.ToString()));
						}
						else if (text != "")
						{
							jSONNode.Add(text, cache.AllocateJSONData(stringBuilder.ToString()));
						}
					}
					text = "";
					stringBuilder.Clear();
					continue;
				case '\t':
				case ' ':
					if (flag)
					{
						stringBuilder.Append(aJSON[i]);
					}
					continue;
				case '\\':
					i++;
					if (flag)
					{
						num = 0;
						char c = aJSON[i];
						switch (c)
						{
						case 't':
							stringBuilder.Append('\t');
							break;
						case 'r':
							stringBuilder.Append('\r');
							break;
						case 'n':
							stringBuilder.Append('\n');
							break;
						case 'b':
							stringBuilder.Append('\b');
							break;
						case 'f':
							stringBuilder.Append('\f');
							break;
						case 'u':
						{
							string s = aJSON.Substring(i + 1, 4);
							stringBuilder.Append((char)int.Parse(s, NumberStyles.AllowHexSpecifier));
							i += 4;
							break;
						}
						default:
							stringBuilder.Append(c);
							break;
						}
					}
					continue;
				case '\n':
				case '\r':
					continue;
				}
				if (flag && num > 0 && text.Length != 0 && stringBuilder.Length >= 1024)
				{
					int num2 = aJSON.IndexOf('"', i);
					if (num2 != -1)
					{
						if (jSONNode is JSONArray)
						{
							jSONNode.Add(cache.AllocateJSONData(aJSON.Substring(num, num2 - num)));
						}
						else if (text != "")
						{
							jSONNode.Add(text, cache.AllocateJSONData(aJSON.Substring(num, num2 - num)));
						}
						flag = false;
						i = num2;
						stringBuilder.Clear();
					}
					num = 0;
				}
				else
				{
					stringBuilder.Append(aJSON[i]);
				}
			}
			if (flag)
			{
				throw new Exception("JSON Parse: Quotation marks seems to be messed up.");
			}
			return jSONNode;
		}

		public virtual void Serialize(BinaryWriter aWriter)
		{
		}

		public void SaveToStream(Stream aData)
		{
			BinaryWriter aWriter = new BinaryWriter(aData);
			Serialize(aWriter);
		}

		public void SaveToCompressedStream(Stream aData)
		{
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public void SaveToCompressedFile(string aFileName)
		{
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public string SaveToCompressedBase64()
		{
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public static JSONNode LoadFromCompressedFile(string aFileName)
		{
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public static JSONNode LoadFromCompressedStream(Stream aData)
		{
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}

		public static JSONNode LoadFromCompressedBase64(string aBase64)
		{
			throw new Exception("Can't use compressed functions. You need include the SharpZipLib and uncomment the define at the top of SimpleJSON");
		}
	}
}
