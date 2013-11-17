using System;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
	public class RSequence : RegularExpression {
		private readonly IList<RegularExpression> units = new List<RegularExpression>();

		public RSequence(IList<RegularExpression> seq) {
			Ordinal = Int32.MaxValue;
			units = seq;
		}

		public RSequence() {
		}

		public IList<RegularExpression> Units {
			get { return units; }
		} 

		public override Nfa GenerateNfa(bool ignoreCase) {
			if (units.Count == 1)
				return units[0].GenerateNfa(ignoreCase);

			Nfa retVal = new Nfa();
			NfaState startState = retVal.Start;
			NfaState finalState = retVal.End;
			Nfa temp1;
			Nfa temp2 = null;

			RegularExpression curRE;

			curRE = units[0];
			temp1 = curRE.GenerateNfa(ignoreCase);
			startState.AddMove(temp1.Start);

			for (int i = 1; i < units.Count; i++) {
				curRE = units[i];

				temp2 = curRE.GenerateNfa(ignoreCase);
				temp1.End.AddMove(temp2.Start);
				temp1 = temp2;
			}

			temp2.End.AddMove(finalState);

			return retVal;

		}
	}
}