using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace Deveel.CSharpCC.Util {
	internal class CSharpFileGenetor {
		private string currentLine;

		public CSharpFileGenetor(string templateName, IDictionary<string, object> options) {
			Options = options;
			TemplateName = templateName;
		}

		public string TemplateName { get; private set; }

		public IDictionary<string, object> Options { get; private set; }

		public void Generate(TextWriter output) {
			using (var stream = GetType().Assembly.GetManifestResourceStream(TemplateName)) {
				if (stream == null)
					throw new IOException("Invalid template name: " + TemplateName);

				var input = new StreamReader(stream);
				Process(input, output, false);
			}
		}

		private String PeekLine(TextReader input) {
			if (currentLine == null)
				currentLine = input.ReadLine();

			return currentLine;
		}

		private String GetLine(TextReader input) {
			String line = currentLine;
			currentLine = null;

			if (line == null)
				input.ReadLine();

			return line;
		}

		private bool Evaluate(string condition) {
			condition = condition.Trim();

			object obj;
			if (!Options.TryGetValue(condition, out obj)) {
				return condition.Equals("true", StringComparison.OrdinalIgnoreCase) ||
				       condition.Equals("yes", StringComparison.OrdinalIgnoreCase);
			}

			if (obj is Boolean) {
				return (bool) obj;
			} else if (obj is String) {
				String s = ((String) obj).Trim();
				return s.Length > 0 && !s.Equals("false", StringComparison.OrdinalIgnoreCase) &&
				       !s.Equals("no", StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}

		private String Substitute(string text) {
			int startPos;

			if ((startPos = text.IndexOf("${")) == -1) {
				return text;
			}

			// Find matching "}".
			int braceDepth = 1;
			int endPos = startPos + 2;

			while (endPos < text.Length && braceDepth > 0) {
				if (text[endPos] == '{')
					braceDepth++;
				else if (text[endPos] == '}')
					braceDepth--;

				endPos++;
			}

			if (braceDepth != 0)
				throw new IOException("Mismatched \"{}\" input template string: " + text);

			string variableExpression = text.Substring(startPos + 2, (endPos - (startPos + 2) - 1));

			// Find the end of the variable name
			String value = null;

			for (int i = 0; i < variableExpression.Length; i++) {
				char ch = variableExpression[i];

				if (ch == ':' && i < variableExpression.Length - 1 && variableExpression[i + 1] == '-') {
					value = SubstituteWithDefault(variableExpression.Substring(0, i), variableExpression.Substring(i + 2));
					break;
				} else if (ch == '?') {
					value = SubstituteWithConditional(variableExpression.Substring(0, i), variableExpression.Substring(i + 1));
					break;
				} else if (ch != '_' && !Char.IsLetterOrDigit(ch)) {
					throw new IOException("Invalid variable input " + variableExpression);
				}
			}

			if (value == null) {
				value = SubstituteWithDefault(variableExpression, "");
			}

			return text.Substring(0, startPos) + value + text.Substring(endPos);
		}

	    private bool IsValidIdentifier(string text) {
            CodeDomProvider provider = CodeDomProvider.CreateProvider("C#");
	        return provider.IsValidIdentifier(text);
	    }

	    private String SubstituteWithConditional(String variableName, String values) {
			// Split values into true and false values.

			int pos = values.IndexOf(':');
			if (pos == -1)
				throw new IOException("No ':' separator input " + values);

			if (Evaluate(variableName))
				return Substitute(values.Substring(0, pos));
			else
				return Substitute(values.Substring(pos + 1));
		}

		private String SubstituteWithDefault(string variableName, String defaultValue) {
			object obj;
			if (!Options.TryGetValue(variableName.Trim(), out obj) ||
			    obj.ToString().Length == 0)
				return Substitute(defaultValue);

			return obj.ToString();
		}

		private void Write(TextWriter output, String text) {
			while (text.IndexOf("${") != -1) {
				text = Substitute(text);
			}

			output.WriteLine(text);
		}

		private void Process(TextReader input, TextWriter output, bool ignoring) {
//    output.println("*** process ignore=" + ignoring + " : " + peekLine(input));
			while (PeekLine(input) != null) {
				if (PeekLine(input).Trim().StartsWith("#if")) {
					String line = GetLine(input).Trim();
					bool condition = Evaluate(line.Substring(3).Trim());

					Process(input, output, ignoring || !condition);

					if (PeekLine(input) != null && PeekLine(input).Trim().StartsWith("#else")) {
						GetLine(input); // Discard the #else line
						Process(input, output, ignoring || condition);
					}

					line = GetLine(input);

					if (line == null)
						throw new IOException("Missing \"#fi\"");

					if (!line.Trim().StartsWith("#fi"))
						throw new IOException("Expected \"#fi\", got: " + line);
				} else if (PeekLine(input).Trim().StartsWith("#")) {
					break;
				} else {
					String line = GetLine(input);
					if (!ignoring)
						Write(output, line);
				}
			}

			output.Flush();
		}

	}
}