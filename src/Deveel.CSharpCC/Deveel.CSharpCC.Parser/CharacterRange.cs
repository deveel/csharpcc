using System;

namespace Deveel.CSharpCC.Parser {
	public class CharacterRange {
		public CharacterRange(char left, char right) {
			if (left > right)
				CSharpCCErrors.SemanticError(this,
				                             "Invalid range : \"" + (int) left + "\" - \"" + (int) right +
				                             "\". First character shoud be less than or equal to the second one in a range.");

			Left = left;
			Right = right;

		}

		public CharacterRange() {
		}

		public int Line { get; internal set; }

		public int Column { get; internal set; }

		public char Right { get; internal set; }

		public char Left { get; internal set; }
	}
}