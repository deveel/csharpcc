using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	internal static class LookaheadCalc {
		public static void choiceCalc(Choice choice) {
			int first = firstChoice(choice);
			// dbl[i] and dbr[i] are lists of size limited matches for choice i
			// of choice.  dbl ignores matches with semantic lookaheads (when force_la_check
			// is false), while dbr ignores semantic lookahead.
			IList<MatchInfo>[] dbl = new IList<MatchInfo>[choice.Choices.Count];
			IList<MatchInfo>[] dbr = new IList<MatchInfo>[choice.Choices.Count];
			int[] minLA = new int[choice.Choices.Count - 1];
			MatchInfo[] overlapInfo = new MatchInfo[choice.Choices.Count - 1];
			int[] other = new int[choice.Choices.Count - 1];
			MatchInfo m;
			IList<MatchInfo> v;
			bool overlapDetected;
			for (int la = 1; la <= Options.getChoiceAmbiguityCheck(); la++) {
				MatchInfo.laLimit = la;
				LookaheadWalk.considerSemanticLA = !Options.getForceLaCheck();
				for (int i = first; i < choice.Choices.Count - 1; i++) {
					LookaheadWalk.sizeLimitedMatches = new List<MatchInfo>();
					m = new MatchInfo();
					m.firstFreeLoc = 0;
					v = new List<MatchInfo>();
					v.Add(m);
					LookaheadWalk.genFirstSet(v, (Expansion) choice.Choices[i]);
					dbl[i] = LookaheadWalk.sizeLimitedMatches;
				}
				LookaheadWalk.considerSemanticLA = false;
				for (int i = first + 1; i < choice.Choices.Count; i++) {
					LookaheadWalk.sizeLimitedMatches = new List<MatchInfo>();
					m = new MatchInfo();
					m.firstFreeLoc = 0;
					v = new List<MatchInfo>();
					v.Add(m);
					LookaheadWalk.genFirstSet(v, (Expansion) choice.Choices[i]);
					dbr[i] = LookaheadWalk.sizeLimitedMatches;
				}
				if (la == 1) {
					for (int i = first; i < choice.Choices.Count - 1; i++) {
						Expansion exp = (Expansion) choice.Choices[i];
						if (Semanticize.EmptyExpansionExists(exp)) {
							CSharpCCErrors.Warning(exp,
								"This choice can expand to the empty token sequence " +
								"and will therefore always be taken in favor of the choices appearing later.");
							break;
						} else if (CodeCheck(dbl[i])) {
							CSharpCCErrors.Warning(exp,
								"CSHARPCODE non-terminal will force this choice to be taken " +
								"in favor of the choices appearing later.");
							break;
						}
					}
				}
				overlapDetected = false;
				for (int i = first; i < choice.Choices.Count - 1; i++) {
					for (int j = i + 1; j < choice.Choices.Count; j++) {
						if ((m = overlap(dbl[i], dbr[j])) != null) {
							minLA[i] = la + 1;
							overlapInfo[i] = m;
							other[i] = j;
							overlapDetected = true;
							break;
						}
					}
				}
				if (!overlapDetected) {
					break;
				}
			}
			for (int i = first; i < choice.Choices.Count - 1; i++) {
				if (explicitLA((Expansion) choice.Choices[i]) && !Options.getForceLaCheck()) {
					continue;
				}
				if (minLA[i] > Options.getChoiceAmbiguityCheck()) {
					CSharpCCErrors.Warning("Choice conflict involving two expansions at");
					Console.Error.Write("         line " + ((Expansion) choice.Choices[i]).Line);
					Console.Error.Write(", column " + ((Expansion) choice.Choices[i]).Column);
					Console.Error.Write(" and line " + ((Expansion) choice.Choices[other[i]]).Line);
					Console.Error.Write(", column " + ((Expansion) choice.Choices[other[i]]).Column);
					Console.Error.WriteLine(" respectively.");
					Console.Error.WriteLine("         A common prefix is: " + image(overlapInfo[i]));
					Console.Error.WriteLine("         Consider using a lookahead of " + minLA[i] + " or more for earlier expansion.");
				} else if (minLA[i] > 1) {
					CSharpCCErrors.Warning("Choice conflict involving two expansions at");
					Console.Error.Write("         line " + ((Expansion) choice.Choices[i]).Line);
					Console.Error.Write(", column " + ((Expansion) choice.Choices[i]).Column);
					Console.Error.Write(" and line " + ((Expansion) choice.Choices[other[i]]).Line);
					Console.Error.Write(", column " + ((Expansion) choice.Choices[other[i]]).Column);
					Console.Error.WriteLine(" respectively.");
					Console.Error.WriteLine("         A common prefix is: " + image(overlapInfo[i]));
					Console.Error.WriteLine("         Consider using a lookahead of " + minLA[i] + " for earlier expansion.");
				}
			}
		}

		public static void ebnfCalc(Expansion exp, Expansion nested) {
			// exp is one of OneOrMore, ZeroOrMore, ZeroOrOne
			MatchInfo m, m1 = null;
			IList<MatchInfo> v, first, follow;
			int la;
			for (la = 1; la <= Options.getOtherAmbiguityCheck(); la++) {
				MatchInfo.laLimit = la;
				LookaheadWalk.sizeLimitedMatches = new List<MatchInfo>();
				m = new MatchInfo();
				m.firstFreeLoc = 0;
				v = new List<MatchInfo>();
				v.Add(m);
				LookaheadWalk.considerSemanticLA = !Options.getForceLaCheck();
				LookaheadWalk.genFirstSet(v, nested);
				first = LookaheadWalk.sizeLimitedMatches;
				LookaheadWalk.sizeLimitedMatches = new List<MatchInfo>();
				LookaheadWalk.considerSemanticLA = false;
				LookaheadWalk.genFollowSet(v, exp, Expansion.NextGenerationIndex++);
				follow = LookaheadWalk.sizeLimitedMatches;
				if (la == 1) {
					if (CodeCheck(first)) {
						CSharpCCErrors.Warning(nested,
							"CSHARPCODE non-terminal within " + image(exp) +
							" construct will force this construct to be entered in favor of " +
							"expansions occurring after construct.");
					}
				}
				if ((m = overlap(first, follow)) == null) {
					break;
				}
				m1 = m;
			}
			if (la > Options.getOtherAmbiguityCheck()) {
				CSharpCCErrors.Warning("Choice conflict in " + image(exp) + " construct " +
				                       "at line " + exp.Line + ", column " + exp.Column + ".");
				Console.Error.WriteLine("         Expansion nested within construct and expansion following construct");
				Console.Error.WriteLine("         have common prefixes, one of which is: " + image(m1));
				Console.Error.WriteLine("         Consider using a lookahead of " + la + " or more for nested expansion.");
			} else if (la > 1) {
				CSharpCCErrors.Warning("Choice conflict in " + image(exp) + " construct " +
				                       "at line " + exp.Line + ", column " + exp.Column + ".");
				Console.Error.WriteLine("         Expansion nested within construct and expansion following construct");
				Console.Error.WriteLine("         have common prefixes, one of which is: " + image(m1));
				Console.Error.WriteLine("         Consider using a lookahead of " + la + " for nested expansion.");
			}
		}

		private static bool explicitLA(Expansion exp) {
			if (!(exp is Sequence)) {
				return false;
			}
			Sequence seq = (Sequence) exp;
			Object obj = seq.Units[0];
			if (!(obj is Lookahead)) {
				return false;
			}
			Lookahead la = (Lookahead) obj;
			return la.IsExplicit;
		}


		private static int firstChoice(Choice ch) {
			if (Options.getForceLaCheck()) {
				return 0;
			}
			for (int i = 0; i < ch.Choices.Count; i++) {
				if (!explicitLA((Expansion) ch.Choices[i])) {
					return i;
				}
			}
			return ch.Choices.Count;
		}

		private static String image(Expansion exp) {
			if (exp is OneOrMore) {
				return "(...)+";
			} else if (exp is ZeroOrMore) {
				return "(...)*";
			} else /* if (exp instanceof ZeroOrOne) */ {
				return "[...]";
			}
		}


		private static String image(MatchInfo m) {
			String ret = "";
			for (int i = 0; i < m.firstFreeLoc; i++) {
				if (m.match[i] == 0) {
					ret += " <EOF>";
				} else {
					RegularExpression re = (RegularExpression) CSharpCCGlobals.rexps_of_tokens[m.match[i]];
					if (re is RStringLiteral) {
						ret += " \"" + CSharpCCGlobals.AddEscapes(((RStringLiteral) re).Image) + "\"";
					} else if (re.Label != null && !re.Label.Equals("")) {
						ret += " <" + re.Label + ">";
					} else {
						ret += " <token of kind " + i + ">";
					}
				}
			}
			if (m.firstFreeLoc == 0) {
				return "";
			} else {
				return ret.Substring(1);
			}
		}


		static bool CodeCheck(IList<MatchInfo> v) {
			for (int i = 0; i < v.Count; i++) {
				if (((MatchInfo)v[i]).firstFreeLoc == 0) {
					return true;
				}
			}
			return false;
		}

		static MatchInfo overlap(IList<MatchInfo> v1, IList<MatchInfo> v2) {
			MatchInfo m1, m2, m3;
			int size;
			bool diff;
			for (int i = 0; i < v1.Count; i++) {
				m1 = (MatchInfo)v1[i];
				for (int j = 0; j < v2.Count; j++) {
					m2 = (MatchInfo)v2[j];
					size = m1.firstFreeLoc; m3 = m1;
					if (size > m2.firstFreeLoc) {
						size = m2.firstFreeLoc; m3 = m2;
					}
					if (size == 0) return null;
					// we wish to ignore empty expansions and the JAVACODE stuff here.
					diff = false;
					for (int k = 0; k < size; k++) {
						if (m1.match[k] != m2.match[k]) {
							diff = true;
							break;
						}
					}
					if (!diff) return m3;
				}
			}
			return null;
		}

	}
}