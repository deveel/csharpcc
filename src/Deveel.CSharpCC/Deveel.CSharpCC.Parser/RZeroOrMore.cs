using System;

namespace Deveel.CSharpCC.Parser {
	public class RZeroOrMore : RegularExpression {
		public RZeroOrMore(Token t, RegularExpression r) {
			Column = t.beginColumn;
			Line = t.beginLine;
			RegularExpression = r;
		}

		public RZeroOrMore() {
		}

		public RegularExpression RegularExpression { get; internal set; }

		public override Nfa GenerateNfa(bool ignoreCase) {
			Nfa retVal = new Nfa();
			NfaState startState = retVal.Start;
			NfaState finalState = retVal.End;

			Nfa temp = RegularExpression.GenerateNfa(ignoreCase);

			startState.AddMove(temp.Start);
			startState.AddMove(finalState);
			temp.End.AddMove(finalState);
			temp.End.AddMove(temp.Start);

			return retVal;
		}
	}
}