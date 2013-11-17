using System;

namespace Deveel.CSharpCC.Parser {
	public class RZeroOrOne : RegularExpression {
		public RegularExpression RegularExpression { get; internal set; }

		public override Nfa GenerateNfa(bool ignoreCase) {
			Nfa retVal = new Nfa();
			NfaState startState = retVal.Start;
			NfaState finalState = retVal.End;

			Nfa temp = RegularExpression.GenerateNfa(ignoreCase);

			startState.AddMove(temp.Start);
			startState.AddMove(finalState);
			temp.End.AddMove(finalState);

			return retVal;
		}
	}
}