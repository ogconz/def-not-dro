using System.Text;

namespace SimpleJSON
{
	internal class JsonFormatter
	{
		public class StringWalker
		{
			private readonly string _s;

			public int Index { get; private set; }

			public bool IsEscaped { get; private set; }

			public char CurrentChar { get; private set; }

			public StringWalker(string s)
			{
				_s = s;
				Index = -1;
			}

			public bool MoveNext()
			{
				if (Index == _s.Length - 1)
				{
					return false;
				}
				if (!IsEscaped)
				{
					IsEscaped = CurrentChar == '\\';
				}
				else
				{
					IsEscaped = false;
				}
				Index++;
				CurrentChar = _s[Index];
				return true;
			}
		}

		public class IndentWriter
		{
			private readonly StringBuilder _result = new StringBuilder();

			private int _indentLevel;

			public void Indent()
			{
				_indentLevel++;
			}

			public void UnIndent()
			{
				if (_indentLevel > 0)
				{
					_indentLevel--;
				}
			}

			public void WriteLine(string line)
			{
				_result.AppendLine(CreateIndent() + line);
			}

			private string CreateIndent()
			{
				StringBuilder stringBuilder = new StringBuilder();
				for (int i = 0; i < _indentLevel; i++)
				{
					stringBuilder.Append("    ");
				}
				return stringBuilder.ToString();
			}

			public override string ToString()
			{
				return _result.ToString();
			}
		}

		private readonly StringWalker _walker;

		private readonly IndentWriter _writer = new IndentWriter();

		private readonly StringBuilder _currentLine = new StringBuilder();

		private bool _quoted;

		public JsonFormatter(string json)
		{
			_walker = new StringWalker(json);
			ResetLine();
		}

		public void ResetLine()
		{
			_currentLine.Length = 0;
		}

		public string Format()
		{
			while (MoveNextChar())
			{
				if (!_quoted && IsOpenBracket())
				{
					WriteCurrentLine();
					AddCharToLine();
					WriteCurrentLine();
					_writer.Indent();
				}
				else if (!_quoted && IsCloseBracket())
				{
					WriteCurrentLine();
					_writer.UnIndent();
					AddCharToLine();
				}
				else if (!_quoted && IsColon())
				{
					AddCharToLine();
					WriteCurrentLine();
				}
				else
				{
					AddCharToLine();
				}
			}
			WriteCurrentLine();
			return _writer.ToString();
		}

		private bool MoveNextChar()
		{
			bool result = _walker.MoveNext();
			if (IsApostrophe())
			{
				_quoted = !_quoted;
			}
			return result;
		}

		public bool IsApostrophe()
		{
			if (_walker.CurrentChar == '"')
			{
				return !_walker.IsEscaped;
			}
			return false;
		}

		public bool IsOpenBracket()
		{
			if (_walker.CurrentChar != '{')
			{
				return _walker.CurrentChar == '[';
			}
			return true;
		}

		public bool IsCloseBracket()
		{
			if (_walker.CurrentChar != '}')
			{
				return _walker.CurrentChar == ']';
			}
			return true;
		}

		public bool IsColon()
		{
			return _walker.CurrentChar == ',';
		}

		private void AddCharToLine()
		{
			_currentLine.Append(_walker.CurrentChar);
		}

		private void WriteCurrentLine()
		{
			string text = _currentLine.ToString().Trim();
			if (text.Length > 0)
			{
				_writer.WriteLine(text);
			}
			ResetLine();
		}
	}
}
