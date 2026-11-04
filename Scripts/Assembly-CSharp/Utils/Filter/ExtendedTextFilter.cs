using System;
using System.Buffers;
using Cysharp.Threading.Tasks;

namespace Utils.Filter
{
	public static class ExtendedTextFilter
	{
		public static async UniTask<string> ApplyFilterAsync(string rawText, Func<string, UniTask<string>> externalFilter)
		{
			char[] buffer = ArrayPool<char>.Shared.Rent(rawText.Length);
			CharMap[] map = ArrayPool<CharMap>.Shared.Rent(rawText.Length);
			ReadOnlySpan<char> clean = rawText;
			Span<char> destination = buffer;
			int length = BuildCleanText(in clean, in destination, (Span<CharMap>)map);
			string text = await externalFilter(new string(buffer, 0, length));
			byte[] array = ArrayPool<byte>.Shared.Rent(text.Length);
			try
			{
				clean = rawText;
				ReadOnlySpan<char> clean2 = text;
				ReadOnlySpan<CharMap> map2 = map;
				Span<byte> changes = array;
				if (FillChanges(in clean, in clean2, in map2, in changes) > 0)
				{
					rawText.CopyTo(0, buffer, 0, rawText.Length);
					clean = text;
					map2 = map;
					changes = array;
					destination = buffer;
					ApplyFilterMask(in clean, in map2, in changes, in destination);
					return new string(buffer);
				}
				return rawText;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(array);
				ArrayPool<CharMap>.Shared.Return(map);
				ArrayPool<char>.Shared.Return(buffer);
			}
		}

		private static int BuildCleanText(in ReadOnlySpan<char> source, in Span<char> clean, in Span<CharMap> map)
		{
			int num = 0;
			int num2 = 0;
			while (num2 < source.Length)
			{
				if (source[num2] == '<')
				{
					int num3 = num2;
					for (num2++; num2 < source.Length && source[num2] != '>'; num2++)
					{
					}
					if (num2 < source.Length)
					{
						num2++;
						continue;
					}
					num2 = num3;
				}
				clean[num] = source[num2];
				map[num] = new CharMap(num, num2);
				num++;
				num2++;
			}
			return num;
		}

		private static int FillChanges(in ReadOnlySpan<char> source, in ReadOnlySpan<char> clean, in ReadOnlySpan<CharMap> map, in Span<byte> changes)
		{
			changes.Clear();
			int num = 0;
			for (int i = 0; i < clean.Length; i++)
			{
				if (source[map[i].cleanIndex] != clean[i])
				{
					changes[i] = 1;
					num++;
				}
			}
			return num;
		}

		private static void ApplyFilterMask(in ReadOnlySpan<char> clean, in ReadOnlySpan<CharMap> map, in Span<byte> changes, in Span<char> destination)
		{
			for (int i = 0; i < changes.Length; i++)
			{
				if (changes[i] != 0)
				{
					int cleanIndex = map[i].cleanIndex;
					destination[cleanIndex] = clean[i];
				}
			}
		}
	}
}
