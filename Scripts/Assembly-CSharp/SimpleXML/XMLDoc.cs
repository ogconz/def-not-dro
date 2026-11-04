using System;
using System.Collections.Generic;

namespace SimpleXML
{
	public class XMLDoc
	{
		private enum ConstructFromState
		{
			SearchName = 0,
			SearchAttrName = 1,
			SearchAttrValue = 2
		}

		public Dictionary<string, string> attributes = new Dictionary<string, string>();

		public Dictionary<string, string> keyValues = new Dictionary<string, string>();

		private bool m_buildKeyValues;

		public XMLDoc parentNode;

		public string nodeName;

		public string nodeValue;

		public List<XMLDoc> childs = new List<XMLDoc>();

		public XMLDoc(XMLDoc parent, bool buildKeyValues)
		{
			parentNode = parent;
			m_buildKeyValues = buildKeyValues;
		}

		public XMLDoc(string source, bool buildKeyValues)
		{
			m_buildKeyValues = buildKeyValues;
			int pos = 0;
			BuildFromString(source, ref pos);
			TrimRoot();
		}

		public string GetAttribute(string key)
		{
			attributes.TryGetValue(key.ToLower(), out var value);
			return value;
		}

		public bool HasAttribute(string key)
		{
			return attributes.ContainsKey(key.ToLower());
		}

		public XMLDoc FindChild(string nodeName)
		{
			if (childs.Count == 0)
			{
				return null;
			}
			nodeName = nodeName.ToLower();
			foreach (XMLDoc child in childs)
			{
				if (child.nodeName == nodeName)
				{
					return child;
				}
			}
			return null;
		}

		public XMLDoc NextSibling()
		{
			if (parentNode == null)
			{
				return null;
			}
			int num = parentNode.childs.IndexOf(this);
			if (num == -1)
			{
				return null;
			}
			num++;
			if (num >= parentNode.childs.Count)
			{
				return null;
			}
			return parentNode.childs[num];
		}

		public bool GetAttribDef(string name, bool def)
		{
			if (attributes.TryGetValue(name.ToLower(), out var value))
			{
				if (bool.TryParse(value, out var result))
				{
					return result;
				}
				return def;
			}
			return def;
		}

		public string GetAttribDef(string name, string def)
		{
			if (attributes.TryGetValue(name.ToLower(), out var value))
			{
				return value;
			}
			return def;
		}

		public int GetAttribDef(string name, int def)
		{
			if (attributes.TryGetValue(name.ToLower(), out var value))
			{
				if (int.TryParse(value, out var result))
				{
					return result;
				}
				return def;
			}
			return def;
		}

		public float GetAttribDef(string name, float def)
		{
			if (attributes.TryGetValue(name.ToLower(), out var value))
			{
				if (float.TryParse(value, out var result))
				{
					return result;
				}
				return def;
			}
			return def;
		}

		public ulong GetAttribDef(string name, ulong def)
		{
			if (attributes.TryGetValue(name.ToLower(), out var value))
			{
				if (ulong.TryParse(value, out var result))
				{
					return result;
				}
				return def;
			}
			return def;
		}

		public string GetValByKey(string name, string def)
		{
			if (keyValues.TryGetValue(name.ToLower(), out var value))
			{
				return value;
			}
			return def;
		}

		public bool GetValByKey(string name, bool def)
		{
			if (keyValues.TryGetValue(name.ToLower(), out var value))
			{
				if (bool.TryParse(value, out var result))
				{
					return result;
				}
				return def;
			}
			return def;
		}

		public int GetValByKey(string name, int def)
		{
			if (keyValues.TryGetValue(name.ToLower(), out var value))
			{
				if (int.TryParse(value, out var result))
				{
					return result;
				}
				return def;
			}
			return def;
		}

		public float GetValByKey(string name, float def)
		{
			if (keyValues.TryGetValue(name.ToLower(), out var value))
			{
				if (float.TryParse(value, out var result))
				{
					return result;
				}
				return def;
			}
			return def;
		}

		private void TrimRoot()
		{
			if (childs.Count != 1)
			{
				return;
			}
			XMLDoc xMLDoc = childs[0];
			List<XMLDoc> list = childs;
			childs = childs[0].childs;
			Dictionary<string, string> dictionary = xMLDoc.attributes;
			xMLDoc.attributes = attributes;
			attributes = dictionary;
			Dictionary<string, string> dictionary2 = xMLDoc.keyValues;
			xMLDoc.keyValues = keyValues;
			keyValues = dictionary2;
			xMLDoc.childs = list;
			list.Clear();
			foreach (XMLDoc child in childs)
			{
				child.parentNode = this;
			}
		}

		private XMLDoc AddChild()
		{
			XMLDoc xMLDoc = new XMLDoc(this, m_buildKeyValues);
			childs.Add(xMLDoc);
			return xMLDoc;
		}

		private void BuildFromString(string FS, ref int pos)
		{
			HashSet<char> hashSet = new HashSet<char> { '!', '?' };
			Func<int, char, int> func = delegate(int from, char subchar)
			{
				int i = from;
				bool flag = false;
				for (; i < FS.Length; i++)
				{
					if (FS[i] == '"')
					{
						flag = !flag;
					}
					if (FS[i] == subchar && !flag)
					{
						return i;
					}
				}
				return i;
			};
			Func<int, char, int> func2 = delegate(int from, char subchar)
			{
				int num3 = from;
				bool flag = false;
				while (num3 >= 0)
				{
					if (FS[num3] == '"')
					{
						flag = !flag;
					}
					if (FS[num3] == subchar && !flag)
					{
						return num3;
					}
					num3--;
				}
				return num3;
			};
			int num = pos;
			while (num < FS.Length)
			{
				int arg = num;
				arg = func(arg, '<');
				if (arg + 1 >= FS.Length)
				{
					pos = arg + 1;
					break;
				}
				if (arg + 1 < FS.Length && FS[arg + 1] == '/')
				{
					int num2 = func2(arg - 1, '>');
					if (num2 != arg - 1)
					{
						nodeValue = FS.Substring(num2 + 1, arg - num2 - 1).Trim();
					}
					pos = arg + 1;
					break;
				}
				while (arg + 1 < FS.Length && hashSet.Contains(FS[arg + 1]))
				{
					if (arg + 3 < FS.Length && FS[arg + 2] == '-' && FS[arg + 3] == '-')
					{
						arg += 3;
						while (arg < FS.Length)
						{
							arg++;
							arg = func(arg, '>');
							if (FS[arg - 1] == '-' && FS[arg - 2] == '-')
							{
								arg = func(arg, '<');
								break;
							}
						}
					}
					else
					{
						arg++;
						arg = func(arg, '<');
					}
				}
				if (arg < FS.Length)
				{
					num = func(arg + 1, '>');
					if (FS[num - 1] == '/')
					{
						AddChild().ConstructFrom(FS.Substring(arg + 1, num - arg - 2));
						pos = num + 1;
					}
					else
					{
						XMLDoc xMLDoc = AddChild();
						xMLDoc.ConstructFrom(FS.Substring(arg + 1, num - arg - 1));
						pos = num + 1;
						xMLDoc.BuildFromString(FS, ref pos);
					}
					num = pos;
					continue;
				}
				break;
			}
		}

		private void ConstructFrom(string source)
		{
			ConstructFromState constructFromState = ConstructFromState.SearchName;
			int c = 0;
			int s = 0;
			int last_e = 0;
			string text = "";
			string text2 = "";
			string text3 = "";
			string value = "";
			HashSet<char> stopChars = new HashSet<char> { ' ', '=', '\t', '\n', '\r' };
			HashSet<char> delimeterChars = new HashSet<char> { ' ', '\t', '\n', '\r' };
			Action action = delegate
			{
				while (c < source.Length && stopChars.Contains(source[c]))
				{
					int num2 = c + 1;
					c = num2;
					num2 = s + 1;
					s = num2;
				}
			};
			Action action2 = delegate
			{
				while (c < source.Length && !stopChars.Contains(source[c]))
				{
					int num2 = c + 1;
					c = num2;
				}
			};
			Func<bool> func = delegate
			{
				last_e = c;
				while (last_e < source.Length && delimeterChars.Contains(source[last_e]))
				{
					int num2 = last_e + 1;
					last_e = num2;
				}
				return source[last_e] == '=';
			};
			if (string.IsNullOrEmpty(source))
			{
				GameLog.Print(AlertLevel.Warning, "XML", "Void group detected");
			}
			while (c < source.Length)
			{
				s = c;
				switch (constructFromState)
				{
				case ConstructFromState.SearchName:
					action();
					action2();
					if (c + 1 < source.Length && func())
					{
						constructFromState = ConstructFromState.SearchAttrValue;
						text = source.Substring(s, c - s);
						c = last_e;
					}
					else
					{
						constructFromState = ConstructFromState.SearchAttrName;
						nodeName = source.Substring(s, c - s).ToLower();
					}
					break;
				case ConstructFromState.SearchAttrName:
					action();
					action2();
					if (c + 1 < source.Length && func())
					{
						constructFromState = ConstructFromState.SearchAttrValue;
						text = source.Substring(s, c - s);
						c = last_e;
					}
					else
					{
						constructFromState = ConstructFromState.SearchAttrName;
						text = source.Substring(s, c - s);
						text2 = "";
						attributes.Add(text.ToLower(), text2);
					}
					break;
				case ConstructFromState.SearchAttrValue:
					action();
					if (source[s] != '"')
					{
						GameLog.Print(AlertLevel.Error, "XML", "Expected value not found");
						constructFromState = ConstructFromState.SearchAttrName;
						break;
					}
					if (c + 1 <= source.Length)
					{
						int num = c + 1;
						c = num;
						while (c < source.Length && source[c] != '"')
						{
							num = c + 1;
							c = num;
						}
						if (source[c] != '"')
						{
							GameLog.Print(AlertLevel.Error, "XML", "Value not closed");
							return;
						}
						text2 = source.Substring(s + 1, c - s - 1);
						text2 = text2.Replace("&quot;", "\"");
						attributes.Add(text.ToLower(), text2);
						if (m_buildKeyValues)
						{
							if (text == "key")
							{
								text3 = text2.ToLower();
							}
							if (text == "val")
							{
								value = text2;
							}
						}
						constructFromState = ConstructFromState.SearchAttrName;
						num = c + 1;
						c = num;
						break;
					}
					GameLog.Print(AlertLevel.Error, "XML", "Value not closed");
					return;
				}
			}
			if (parentNode != null && m_buildKeyValues && !string.IsNullOrEmpty(text3))
			{
				parentNode.keyValues[text3] = value;
			}
		}
	}
}
