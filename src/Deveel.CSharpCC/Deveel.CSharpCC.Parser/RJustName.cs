using System;

namespace Deveel.CSharpCC.Parser {
	public class RJustName : RegularExpression {
		public RJustName(Token token, string image) {
			Line = token.beginLine;
			Column = token.beginColumn;
			Label = image;
		}

		public RegularExpression RegularExpression { get; private set; }

		public override Nfa GenerateNfa(bool ignoreCase) {
			return RegularExpression.GenerateNfa(ignoreCase);
		}
	}
}