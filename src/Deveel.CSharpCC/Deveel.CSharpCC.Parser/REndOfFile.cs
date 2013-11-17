using System;

namespace Deveel.CSharpCC.Parser {
	public class REndOfFile : RegularExpression {
		public override Nfa GenerateNfa(bool ignoreCase) {
			return null;
		}
	}
}