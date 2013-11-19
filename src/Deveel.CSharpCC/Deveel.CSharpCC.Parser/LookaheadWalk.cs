using System;
using System.Collections;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
	public static class LookaheadWalk {
		public static bool considerSemanticLA;
		public static ArrayList sizeLimitedMatches;
		public static void reInit() {
			considerSemanticLA = false;
			sizeLimitedMatches = null;
		}

		public static IList<MatchInfo> genFirstSet(IList<MatchInfo> partialMatches, Expansion exp) {
			if (exp is RegularExpression) {
				IList<MatchInfo> retval = new List<MatchInfo>();
				for (int i = 0; i < partialMatches.Count; i++) {
					MatchInfo m = (MatchInfo) partialMatches[i];
					MatchInfo mnew = new MatchInfo();
					for (int j = 0; j < m.firstFreeLoc; j++) {
						mnew.match[j] = m.match[j];
					}
					mnew.firstFreeLoc = m.firstFreeLoc;
					mnew.match[mnew.firstFreeLoc++] = ((RegularExpression) exp).Ordinal;
					if (mnew.firstFreeLoc == MatchInfo.laLimit) {
						sizeLimitedMatches.Add(mnew);
					} else {
						retval.Add(mnew);
					}
				}
				return retval;
			} else if (exp is NonTerminal) {
				NormalProduction prod = ((NonTerminal) exp).Production;
				if (prod is CodeProduction) {
					return new List<MatchInfo>();
				} else {
					return genFirstSet(partialMatches, prod.Expansion);
				}
			} else if (exp is Choice) {
				IList<MatchInfo> retval = new List<MatchInfo>();
				Choice ch = (Choice) exp;
				foreach (Expansion e in ch.Choices) {
					IList<MatchInfo> v = genFirstSet(partialMatches, e);
					listAppend(retval, v);
				}
				return retval;
			} else if (exp is Sequence) {
				IList<MatchInfo> v = partialMatches;
				Sequence seq = (Sequence) exp;
				foreach (Expansion unit in seq.Units) {
					v = genFirstSet(v, unit);
					if (v.Count == 0)
						break;
				}
				return v;
			} else if (exp is OneOrMore) {
				IList<MatchInfo> retval = new List<MatchInfo>();
				IList<MatchInfo> v = partialMatches;
				OneOrMore om = (OneOrMore) exp;
				while (true) {
					v = genFirstSet(v, om.Expansion);
					if (v.Count == 0)
						break;

					listAppend(retval, v);
				}
				return retval;
			} else if (exp is ZeroOrMore) {
				IList<MatchInfo> retval = new List<MatchInfo>();
				listAppend(retval, partialMatches);
				IList<MatchInfo> v = partialMatches;
				ZeroOrMore zm = (ZeroOrMore) exp;
				while (true) {
					v = genFirstSet(v, zm.Expansion);
					if (v.Count == 0)
						break;

					listAppend(retval, v);
				}
				return retval;
			} else if (exp is ZeroOrOne) {
				IList<MatchInfo> retval = new List<MatchInfo>();
				listAppend(retval, partialMatches);
				listAppend(retval, genFirstSet(partialMatches, ((ZeroOrOne) exp).Expansion));
				return retval;
			} else if (exp is TryBlock) {
				return genFirstSet(partialMatches, ((TryBlock) exp).Expansion);
			} else if (considerSemanticLA &&
			           exp is Lookahead &&
			           ((Lookahead) exp).ActionTokens.Count != 0
				) {
				return new List<MatchInfo>();
			} else {
				IList<MatchInfo> retval = new List<MatchInfo>();
				listAppend(retval, partialMatches);
				return retval;
			}
		}

		public static IList<MatchInfo> genFollowSet(IList<MatchInfo> partialMatches, Expansion exp, long generation) {
			if (exp.MyGeneration == generation) {
				return new List<MatchInfo>();
			}

			exp.MyGeneration = generation;
			if (exp.Parent == null) {
				IList<MatchInfo> retval = new List<MatchInfo>();
				listAppend(retval, partialMatches);
				return retval;
			} else if (exp.Parent is NormalProduction) {
				IList<NonTerminal> parents = ((NormalProduction) exp.Parent).Parents;
				IList<MatchInfo> retval = new List<MatchInfo>();
				//System.out.println("1; gen: " + generation + "; exp: " + exp);
				for (int i = 0; i < parents.Count; i++) {
					IList<MatchInfo> v = genFollowSet(partialMatches, parents[i], generation);
					listAppend(retval, v);
				}
				return retval;
			} else if (exp.Parent is Sequence) {
				Sequence seq = (Sequence) exp.Parent;
				IList<MatchInfo> v = partialMatches;
				for (int i = exp.Ordinal + 1; i < seq.Units.Count; i++) {
					v = genFirstSet(v, seq.Units[i]);
					if (v.Count == 0)
						return v;
				}

				IList<MatchInfo> v1 = new List<MatchInfo>();
				IList<MatchInfo> v2 = new List<MatchInfo>();
				listSplit(v, partialMatches, v1, v2);
				if (v1.Count != 0) {
					//System.out.println("2; gen: " + generation + "; exp: " + exp);
					v1 = genFollowSet(v1, seq, generation);
				}
				if (v2.Count != 0) {
					//System.out.println("3; gen: " + generation + "; exp: " + exp);
					v2 = genFollowSet(v2, seq, Expansion.NextGenerationIndex++);
				}
				listAppend(v2, v1);
				return v2;
			} else if (exp.Parent is OneOrMore || 
				exp.Parent is ZeroOrMore) {
				IList<MatchInfo> moreMatches = new List<MatchInfo>();
				listAppend(moreMatches, partialMatches);
				IList<MatchInfo> v = partialMatches;
				while (true) {
					v = genFirstSet(v, exp);
					if (v.Count == 0)
						break;
					listAppend(moreMatches, v);
				}

				IList<MatchInfo> v1 = new List<MatchInfo>();
				IList<MatchInfo> v2 = new List<MatchInfo>();
				listSplit(moreMatches, partialMatches, v1, v2);
				if (v1.Count != 0) {
					//System.out.println("4; gen: " + generation + "; exp: " + exp);
					v1 = genFollowSet(v1, (Expansion) exp.Parent, generation);
				}
				if (v2.Count != 0) {
					//System.out.println("5; gen: " + generation + "; exp: " + exp);
					v2 = genFollowSet(v2, (Expansion) exp.Parent, Expansion.NextGenerationIndex++);
				}
				listAppend(v2, v1);
				return v2;
			} else {
				//System.out.println("6; gen: " + generation + "; exp: " + exp);
				return genFollowSet(partialMatches, (Expansion) exp.Parent, generation);
			}
		}


		private static void listSplit(IList<MatchInfo> toSplit, IList<MatchInfo> mask, IList<MatchInfo> partInMask, IList<MatchInfo> rest) {
			for (int i = 0; i < toSplit.Count; i++) {
				for (int j = 0; j < mask.Count; j++) {
					if (toSplit[i] == mask[j]) {
						partInMask.Add(toSplit[i]);
						goto OuterLoop;
					}
				}
				rest.Add(toSplit[i]);
			}
			OuterLoop:
			;
		}

		private static void listAppend(IList<MatchInfo> vToAppendTo, IList<MatchInfo> vToAppend) {
			for (int i = 0; i < vToAppend.Count; i++) {
				vToAppendTo.Add(vToAppend[i]);
			}
		}

	}
}