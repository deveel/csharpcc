using System;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
	public class RRepetitionRange : RegularExpression {
		public RRepetitionRange() {
			Min = 0;
			Max = -1;
		}

		public RegularExpression RegularExpression { get; internal set; }

		public int Min { get; internal set; }

		public int Max { get; internal set; }

		public bool HasMax { get; internal set; }

		public override Nfa GenerateNfa(bool ignoreCase) {
			List<RegularExpression> units = new List<RegularExpression>();
			RSequence seq;
			int i;

			for (i = 0; i < Min; i++) {
				units.Add(RegularExpression);
			}

			if (HasMax && Max == -1) // Unlimited
			{
				RZeroOrMore zoo = new RZeroOrMore();
				zoo.RegularExpression = RegularExpression;
				units.Add(zoo);
			}

			while (i++ < Max) {
				RZeroOrOne zoo = new RZeroOrOne();
				zoo.RegularExpression = RegularExpression;
				units.Add(zoo);
			}
			seq = new RSequence(units);
			return seq.GenerateNfa(ignoreCase);

		}
	}
}