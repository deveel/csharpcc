using System;
using System.IO;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	public static class CSharpFiles {
		static String ReplaceBackslash(String str) {
			StringBuilder b;
			int i = 0, len = str.Length;

			while (i < len && str[i++] != '\\') ;

			if (i == len)  // No backslash found.
				return str;

			char c;
			b = new StringBuilder();
			for (i = 0; i < len; i++)
				if ((c = str[i]) == '\\')
					b.Append("\\\\");
				else
					b.Append(c);

			return b.ToString();
		}

		private static double GetVersion(String fileName) {
			string commentHeader = "/* " + getIdString(CSharpCCGlobals.ToolNames, fileName) + " Version ";
			string file = Path.Combine(Options.getOutputDirectory().FullName, ReplaceBackslash(fileName));

			if (!File.Exists(file)) {
				// Has not yet been created, so it must be up to date.
				return typeof (CSharpCCParser).Assembly.GetName().Version.Major;
			}

			TextReader reader = null;
			try {
				reader = new StringReader(file);
				String str;
				double version = 0.0;

				// Although the version comment should be the first line, sometimes the
				// user might have put comments before it.
				while ((str = reader.ReadLine()) != null) {
					if (str.StartsWith(commentHeader)) {
						str = str.Substring(commentHeader.Length);
						int pos = str.IndexOf(' ');
						if (pos >= 0)
							str = str.Substring(0, pos);
						if (str.Length > 0) {
							try {
								version = Double.Parse(str);
							} catch (FormatException) {
								// Ignore - leave version as 0.0
							}
						}

						break;
					}
				}

				return version;
			} catch (IOException) {
				return 0.0;
			} finally {
				if (reader != null) {
					try {
						reader.Close();
					} catch (IOException e) {
					}
				}
			}
		}

	}
}