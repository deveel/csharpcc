using System;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
	public class RChoice : RegularExpression {
		private readonly IList<RegularExpression> choices = new List<RegularExpression>();

		public IList<RegularExpression> Choices {
			get { return choices; }
		}

		public override Nfa GenerateNfa(bool ignoreCase) {
			CompressCharLists();

			if (choices.Count == 1)
				return choices[0].GenerateNfa(ignoreCase);

			Nfa retVal = new Nfa();
			NfaState startState = retVal.Start;
			NfaState finalState = retVal.End;

			for (int i = 0; i < choices.Count; i++) {
				Nfa temp;
				RegularExpression curRE = choices[i];

				temp = curRE.GenerateNfa(ignoreCase);

				startState.AddMove(temp.Start);
				temp.End.AddMove(finalState);
			}

			return retVal;
		}

		private void CompressCharLists() {
			CompressChoices(); // Unroll nested choices
			RegularExpression curRE;
			RCharacterList curCharList = null;

			for (int i = 0; i < choices.Count; i++) {
				curRE = choices[i];

				while (curRE is RJustName)
					curRE = ((RJustName) curRE).RegularExpression;

				if (curRE is RStringLiteral &&
				    ((RStringLiteral) curRE).Image.Length == 1)
					choices[i] = curRE = new RCharacterList(((RStringLiteral) curRE).Image[0]);

				if (curRE is RCharacterList) {
					if (((RCharacterList) curRE).Negated)
						((RCharacterList) curRE).RemoveNegation();

					IList<object> tmp = ((RCharacterList) curRE).Descriptors;

					if (curCharList == null)
						choices[i] = curRE = curCharList = new RCharacterList();
					else
						choices.RemoveAt(i--);

					for (int j = tmp.Count; j-- > 0;)
						curCharList.Descriptors.Add(tmp[j]);
				}

			}
		}

		private void CompressChoices() {
			RegularExpression curRE;

			for (int i = 0; i < choices.Count; i++) {
				curRE = choices[i];

				while (curRE is RJustName)
					curRE = ((RJustName) curRE).RegularExpression;

				if (curRE is RChoice) {
					choices.RemoveAt(i--);
					for (int j = ((RChoice) curRE).Choices.Count; j-- > 0;)
						choices.Add(((RChoice) curRE).Choices[j]);
				}
			}
		}

		public void CheckUnmatchability() {
			RegularExpression curRE;
			int numStrings = 0;

			for (int i = 0; i < choices.Count; i++) {
				if (!(curRE = choices[i]).IsPrivate &&
				    //curRE instanceof RJustName &&
				    curRE.Ordinal > 0 && curRE.Ordinal < Ordinal &&
				    LexGen.lexStates[curRE.Ordinal] == LexGen.lexStates[Ordinal]) {
					if (Label != null)
						CSharpCCErrors.Warning(this, "Regular Expression choice : " + curRE.Label + " can never be matched as : " + Label);
					else
						CSharpCCErrors.Warning(this, "Regular Expression choice : " + curRE.Label + " can never be matched as token of kind : " + Ordinal);
				}

				if (!curRE.IsPrivate && curRE is RStringLiteral)
					numStrings++;
			}
		}

	}
}