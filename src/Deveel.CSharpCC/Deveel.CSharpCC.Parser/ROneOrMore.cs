using System;

namespace Deveel.CSharpCC.Parser {
	public class ROneOrMore : RegularExpression {
		public ROneOrMore(Token token, RegularExpression expression) {
			Column = token.beginColumn;
			Line = token.beginLine;
			RegularExpression = expression;
		}

		public RegularExpression RegularExpression { get; internal set; }

		public override Nfa GenerateNfa(bool ignoreCase) {
			Nfa retVal = new Nfa();
			NfaState startState = retVal.Start;
			NfaState finalState = retVal.End;

			Nfa temp = RegularExpression.GenerateNfa(ignoreCase);

			startState.AddMove(temp.Start);
			temp.End.AddMove(temp.Start);
			temp.End.AddMove(finalState);

			return retVal;
		}
	}
}