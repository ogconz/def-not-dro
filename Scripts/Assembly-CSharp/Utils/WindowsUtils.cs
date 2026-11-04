using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Utils
{
	public static class WindowsUtils
	{
		public static string GetInstalledMessageVC2015_2022()
		{
			string text = "SOFTWARE\\Classes\\Installer\\Dependencies";
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(text);
			if (registryKey == null)
			{
				return string.Empty;
			}
			foreach (string item in from n in registryKey.GetSubKeyNames()
				where !n.ToLower().Contains("dotnet") && !n.ToLower().Contains("microsoft")
				select n)
			{
				using RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey(text + "\\" + item);
				string text2 = registryKey2.GetValue("DisplayName")?.ToString() ?? null;
				if (string.IsNullOrEmpty(text2))
				{
					continue;
				}
				if (Environment.Is64BitOperatingSystem)
				{
					if (Regex.IsMatch(text2, "C\\+\\+ 2013.*\\((x64|x86)\\)"))
					{
						flag = true;
					}
					if (Regex.IsMatch(text2, "C\\+\\+ 2015.*\\((x64|x86)\\)"))
					{
						flag2 = true;
					}
					if (Regex.IsMatch(text2, "C\\+\\+ 2017.*\\((x64|x86)\\)"))
					{
						flag3 = true;
					}
					if (Regex.IsMatch(text2, "C\\+\\+ 2022.*\\((x64|x86)\\)"))
					{
						flag4 = true;
					}
				}
				else
				{
					if (Regex.IsMatch(text2, "C\\+\\+ 2013.*\\(x86\\)"))
					{
						flag = true;
					}
					if (Regex.IsMatch(text2, "C\\+\\+ 2015.*\\(x86\\)"))
					{
						flag2 = true;
					}
					if (Regex.IsMatch(text2, "C\\+\\+ 2017.*\\(x86\\)"))
					{
						flag3 = true;
					}
					if (Regex.IsMatch(text2, "C\\+\\+ 2022.*\\(x86\\)"))
					{
						flag4 = true;
					}
				}
			}
			string text3 = (Environment.Is64BitOperatingSystem ? "Please install : VC Redist x64(" : "Please install : VC Redist x86(");
			List<string> list = new List<string>();
			if (!flag)
			{
				list.Add("2013");
			}
			if (!flag2)
			{
				list.Add("2015");
			}
			if (!flag3)
			{
				list.Add("2017");
			}
			if (!flag4)
			{
				list.Add("2022");
			}
			if (list.Count <= 0)
			{
				return string.Empty;
			}
			text3 += list[0];
			for (int num = 1; num < list.Count; num++)
			{
				text3 = text3 + ", " + list[num];
			}
			return text3 + ")";
		}
	}
}
