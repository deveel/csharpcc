using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	public class RStringLiteral : RegularExpression {
		private static int maxStrKind = 0;
		private static int maxLen = 0;
		private static int charCnt = 0;
		private static IList<IDictionary<string, KindInfo>>  charPosKind = new List<IDictionary<string, KindInfo>>(); // Elements are hashtables
		// with single char keys;
		private static int[] maxLenForActive = new int[100]; // 6400 tokens
		public static String[] allImages;
		private static int[][] intermediateKinds;
		private static int[][] intermediateMatchedPos;

		private static int startStateCnt = 0;
		private static bool[] subString;
		private static bool[] subStringAtPos;
		private static IDictionary<string, long[]>[] statesForPos;

		public RStringLiteral(Token token, string image) {
			Line = token.beginLine;
			Column = token.beginColumn;
			Image = image;
		}

		public string Image { get; internal set; }

		public void GenerateDfa(TextWriter ostr, int kind) {
			String s;
			IDictionary<string, KindInfo> temp;
			KindInfo info;
			int len;

			if (maxStrKind <= Ordinal)
				maxStrKind = Ordinal + 1;

			if ((len = Image.Length) > maxLen)
				maxLen = len;

			char c;
			for (int i = 0; i < len; i++) {
				if (Options.getIgnoreCase())
					s = ("" + (c = Image[i])).ToLower();
				else
					s = "" + (c = Image[i]);

				if (!NfaState.unicodeWarningGiven && c > 0xff &&
				    !Options.getUnicodeEscape() &&
				    !Options.getUserCharStream()) {
					NfaState.unicodeWarningGiven = true;
					CSharpCCErrors.Warning(LexGen.curRE, "Non-ASCII characters used in regular expression." +
					                                     "Please make sure you use the correct TextReader when you create the parser, " +
					                                     "one that can handle your character set.");
				}

				if (i >= charPosKind.Count) // Kludge, but OK
					charPosKind.Add(temp = new Dictionary<string, KindInfo>());
				else
					temp = (IDictionary<string, KindInfo>) charPosKind[i];

				if (!temp.TryGetValue(s, out info))
					temp[s] = info = new KindInfo(LexGen.maxOrdinal);

				if (i + 1 == len)
					info.InsertFinalKind(Ordinal);
				else
					info.InsertValidKind(Ordinal);

				if (!Options.getIgnoreCase() && LexGen.ignoreCase[Ordinal] &&
				    c != Char.ToLower(c)) {
					s = ("" + Image[i]).ToLower();

					if (i >= charPosKind.Count) // Kludge, but OK
						charPosKind.Add(temp = new Dictionary<string, KindInfo>());
					else
						temp = (IDictionary<string, KindInfo>) charPosKind[i];

					if (!temp.TryGetValue(s, out info))
						temp[s] = info = new KindInfo(LexGen.maxOrdinal);

					if (i + 1 == len)
						info.InsertFinalKind(Ordinal);
					else
						info.InsertValidKind(Ordinal);
				}

				if (!Options.getIgnoreCase() && LexGen.ignoreCase[Ordinal] &&
				    c != Char.ToUpper(c)) {
					s = ("" + Image[i]).ToUpper();

					if (i >= charPosKind.Count) // Kludge, but OK
						charPosKind.Add(temp = new Dictionary<string, KindInfo>());
					else
						temp = (IDictionary<string, KindInfo>) charPosKind[i];

					if (!temp.TryGetValue(s, out info))
						temp[s] = info = new KindInfo(LexGen.maxOrdinal);

					if (i + 1 == len)
						info.InsertFinalKind(Ordinal);
					else
						info.InsertValidKind(Ordinal);
				}
			}

			maxLenForActive[Ordinal/64] = Math.Max(maxLenForActive[Ordinal/64], len - 1);
			allImages[Ordinal] = Image;
		}


		public override Nfa GenerateNfa(bool ignoreCase) {
			if (Image.Length == 1) {
				RCharacterList temp = new RCharacterList(Image[0]);
				return temp.GenerateNfa(ignoreCase);
			}

			NfaState startState = new NfaState();
			NfaState theStartState = startState;
			NfaState finalState = null;

			if (Image.Length == 0)
				return new Nfa(theStartState, theStartState);

			int i;

			for (i = 0; i < Image.Length; i++) {
				finalState = new NfaState();
				startState.charMoves = new char[1];
				startState.AddChar(Image[i]);

				if (Options.getIgnoreCase() || ignoreCase) {
					startState.AddChar(Char.ToLower(Image[i]));
					startState.AddChar(Char.ToUpper(Image[i]));
				}

				startState.next = finalState;
				startState = finalState;
			}

			return new Nfa(theStartState, finalState);

		}

		public static void ReInit() {
			maxStrKind = 0;
			maxLen = 0;
			charPosKind = new List<IDictionary<string, KindInfo>>();
			maxLenForActive = new int[100]; // 6400 tokens
			intermediateKinds = null;
			intermediateMatchedPos = null;
			startStateCnt = 0;
			subString = null;
			subStringAtPos = null;
			statesForPos = null;
		}

		public static void DumpStrLiteralImages(TextWriter ostr) {
			String image;
			int i;
			charCnt = 0; // Set to zero in reInit() but just to be sure

			ostr.WriteLine("");
			ostr.WriteLine("// Token literal values.");
			ostr.WriteLine("public static readonly string[] ccStrLiteralImages = {");

			if (allImages == null || allImages.Length == 0) {
				ostr.WriteLine("};");
				return;
			}

			allImages[0] = "";
			for (i = 0; i < allImages.Length; i++) {
				if ((image = allImages[i]) == null ||
				    ((LexGen.toSkip[i/64] & (1L << (i%64))) == 0L &&
				     (LexGen.toMore[i/64] & (1L << (i%64))) == 0L &&
				     (LexGen.toToken[i/64] & (1L << (i%64))) == 0L) ||
				    (LexGen.toSkip[i/64] & (1L << (i%64))) != 0L ||
				    (LexGen.toMore[i/64] & (1L << (i%64))) != 0L ||
				    LexGen.canReachOnMore[LexGen.lexStates[i]] ||
				    ((Options.getIgnoreCase() || LexGen.ignoreCase[i]) &&
				     (!image.Equals(image.ToLower()) ||
				      !image.Equals(image.ToUpper())))) {
					allImages[i] = null;
					if ((charCnt += 6) > 80) {
						ostr.WriteLine("");
						charCnt = 0;
					}

					ostr.Write("null, ");
					continue;
				}

				String toPrint = "\"";
                toPrint += CSharpCCGlobals.AddEscapes(image);
                /*
				for (int j = 0; j < image.Length; j++) {
					if (image[j] <= 0xff)
						toPrint += ("\\" + Convert.ToString(image[j], 8));
					else {
						String hexVal = "0x" + ((int) image[j]).ToString("X");

						if (hexVal.Length == 3)
							hexVal = "0" + hexVal;
						toPrint += ("\\u" + hexVal);
					}
				}
                */
				toPrint += ("\", ");

				if ((charCnt += toPrint.Length) >= 80) {
					ostr.WriteLine("");
					charCnt = 0;
				}

				ostr.Write(toPrint);
			}

			while (++i < LexGen.maxOrdinal) {
				if ((charCnt += 6) > 80) {
					ostr.WriteLine("");
					charCnt = 0;
				}

				ostr.Write("null, ");
				continue;
			}

			ostr.WriteLine("};");
		}

		private static void DumpNullStrLiterals(TextWriter ostr) {
			ostr.Write("{");

			if (NfaState.generatedStates != 0)
				ostr.Write("   return ccMoveNfa" + LexGen.lexStateSuffix + "(" + NfaState.InitStateName() + ", 0);");
			else
				ostr.Write("   return 1;");

			ostr.Write("}");
		}

		private static int GetStateSetForKind(int pos, int kind) {
			if (LexGen.mixed[LexGen.lexStateIndex] || NfaState.generatedStates == 0)
				return -1;

			IDictionary<string, long[]> allStateSets = statesForPos[pos];

			if (allStateSets == null)
				return -1;

			foreach (KeyValuePair<string, long[]> entry in allStateSets) {
				String s = entry.Key;
				long[] actives = entry.Value;

				s = s.Substring(s.IndexOf(", ") + 2);
				s = s.Substring(s.IndexOf(", ") + 2);

				if (s.Equals("null;"))
					continue;

				if (actives != null &&
				    (actives[kind/64] & (1L << (kind%64))) != 0L) {
					return NfaState.AddStartStateSet(s);
				}
			}

			return -1;
		}

		private static String GetLabel(int kind) {
			RegularExpression re = LexGen.rexprs[kind];

			if (re is RStringLiteral)
				return " \"" + CSharpCCGlobals.AddEscapes(((RStringLiteral) re).Image) + "\"";
			else if (!re.Label.Equals(""))
				return " <" + re.Label + ">";
			else
				return " <token of kind " + kind + ">";
		}

		private static int GetLine(int kind) {
			return LexGen.rexprs[kind].Line;
		}

		private static int GetColumn(int kind) {
			return LexGen.rexprs[kind].Column;
		}

		private static bool StartsWithIgnoreCase(String s1, String s2) {
			if (s1.Length < s2.Length)
				return false;

			for (int i = 0; i < s2.Length; i++) {
				char c1 = s1[i], c2 = s2[i];

				if (c1 != c2 && Char.ToLower(c2) != c1 &&
				    Char.ToUpper(c2) != c1)
					return false;
			}

			return true;
		}

	    internal static void FillSubString() {
			String image;
			subString = new bool[maxStrKind + 1];
			subStringAtPos = new bool[maxLen];

			for (int i = 0; i < maxStrKind; i++) {
				subString[i] = false;

				if ((image = allImages[i]) == null ||
				    LexGen.lexStates[i] != LexGen.lexStateIndex)
					continue;

				if (LexGen.mixed[LexGen.lexStateIndex]) {
					// We will not optimize for mixed case
					subString[i] = true;
					subStringAtPos[image.Length - 1] = true;
					continue;
				}

				for (int j = 0; j < maxStrKind; j++) {
					if (j != i && LexGen.lexStates[j] == LexGen.lexStateIndex &&
					    allImages[j] != null) {
						if (allImages[j].IndexOf(image) == 0) {
							subString[i] = true;
							subStringAtPos[image.Length - 1] = true;
							break;
						} else if (Options.getIgnoreCase() &&
						           StartsWithIgnoreCase(allImages[j], image)) {
							subString[i] = true;
							subStringAtPos[image.Length - 1] = true;
							break;
						}
					}
				}
			}
		}

		private static void DumpStartWithStates(TextWriter ostr) {
			ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + "int " +
			             "ccStartNfaWithStates" + LexGen.lexStateSuffix + "(int pos, int kind, int state)");
			ostr.WriteLine("{");
			ostr.WriteLine("   ccMatchedKind = kind;");
			ostr.WriteLine("   ccMatchedPos = pos;");

			if (Options.getDebugTokenManager()) {
				ostr.WriteLine("   debugStream.WriteLine(\"   No more string literal token matches are possible.\");");
				ostr.WriteLine("   debugStream.WriteLine(\"   Currently matched the first \" " +
				             "+ (ccMatchedPos + 1) + \" characters as a \" + tokenImage[ccMatchedKind] + \" token.\");");
			}

			ostr.WriteLine("   try { curChar = inputStream.ReadChar(); }");
			ostr.WriteLine("   catch(System.IO.IOException e) { return pos + 1; }");

			if (Options.getDebugTokenManager())
				ostr.WriteLine("   debugStream.WriteLine(" +
				             (LexGen.maxLexStates > 1 ? "\"<\" + lexStateNames[curLexState] + \">\" + " : "") +
				             "\"Current character : \" + " +
				             "TokenMgrError.AddEscapes(curChar.ToString()) + \" (\" + (int)curChar + \") " +
				             "at line \" + inputStream.EndLine + \" column \" + inputStream.EndColumn);");

			ostr.WriteLine("   return ccMoveNfa" + LexGen.lexStateSuffix + "(state, pos + 1);");
			ostr.WriteLine("}");
		}

		private static bool boilerPlateDumped = false;

		private static void DumpBoilerPlate(TextWriter ostr) {
			ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + "int " +"ccStopAtPos(int pos, int kind)");
			ostr.WriteLine("{");
			ostr.WriteLine("   ccMatchedKind = kind;");
			ostr.WriteLine("   ccMatchedPos = pos;");

			if (Options.getDebugTokenManager()) {
				ostr.WriteLine("   debugStream.WriteLine(\"   No more string literal token matches are possible.\");");
				ostr.WriteLine("   debugStream.WriteLine(\"   Currently matched the first \" + (ccMatchedPos + 1) + " +
				             "\" characters as a \" + tokenImage[ccMatchedKind] + \" token.\");");
			}

			ostr.WriteLine("   return pos + 1;");
			ostr.WriteLine("}");
		}

		private static String[] ReArrange(IDictionary<string, KindInfo> tab) {
			String[] ret = new String[tab.Count];
			int cnt = 0;

			foreach (string s in tab.Keys) {
				int i = 0, j;
				char c = s[0];

				while (i < cnt && ret[i][0] < c)
					i++;

				if (i < cnt)
					for (j = cnt - 1; j >= i; j--)
						ret[j + 1] = ret[j];

				ret[i] = s;
				cnt++;
			}

			return ret;
		}

	    internal static void DumpDfaCode(TextWriter ostr) {
			IDictionary<string, KindInfo> tab;
			String key;
			KindInfo info;
			int maxLongsReqd = maxStrKind/64 + 1;
			int i, j, k;
			bool ifGenerated;
			LexGen.maxLongsReqd[LexGen.lexStateIndex] = maxLongsReqd;

			if (maxLen == 0) {
				ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + " int " + "ccMoveStringLiteralDfa0" + LexGen.lexStateSuffix + "()");

				DumpNullStrLiterals(ostr);
				return;
			}

			if (!boilerPlateDumped) {
				DumpBoilerPlate(ostr);
				boilerPlateDumped = true;
			}

			bool createStartNfa = false;
			
			for (i = 0; i < maxLen; i++) {
				bool atLeastOne = false;
				bool startNfaNeeded = false;
				tab = (IDictionary<string, KindInfo>) charPosKind[i];
				String[] keys = ReArrange(tab);

				ostr.Write("private " + (Options.getStatic() ? "static " : "") + "int ccMoveStringLiteralDfa" + i + LexGen.lexStateSuffix + "(");

				if (i != 0) {
					if (i == 1) {
						for (j = 0; j < maxLongsReqd - 1; j++)
							if (i <= maxLenForActive[j]) {
								if (atLeastOne)
									ostr.Write(", ");
								else
									atLeastOne = true;
								ostr.Write("long active" + j);
							}

						if (i <= maxLenForActive[j]) {
							if (atLeastOne)
								ostr.Write(", ");
							ostr.Write("long active" + j);
						}
					} else {
						for (j = 0; j < maxLongsReqd - 1; j++)
							if (i <= maxLenForActive[j] + 1) {
								if (atLeastOne)
									ostr.Write(", ");
								else
									atLeastOne = true;
								ostr.Write("long old" + j + ", long active" + j);
							}

						if (i <= maxLenForActive[j] + 1) {
							if (atLeastOne)
								ostr.Write(", ");
							ostr.Write("long old" + j + ", long active" + j);
						}
					}
				}
				ostr.Write(")");
				ostr.WriteLine("{");

				if (i != 0) {
					if (i > 1) {
						atLeastOne = false;
						ostr.Write("   if ((");

						for (j = 0; j < maxLongsReqd - 1; j++)
							if (i <= maxLenForActive[j] + 1) {
								if (atLeastOne)
									ostr.Write(" | ");
								else
									atLeastOne = true;
								ostr.Write("(active" + j + " &= old" + j + ")");
							}

						if (i <= maxLenForActive[j] + 1) {
							if (atLeastOne)
								ostr.Write(" | ");
							ostr.Write("(active" + j + " &= old" + j + ")");
						}

						ostr.WriteLine(") == 0L)");
						if (!LexGen.mixed[LexGen.lexStateIndex] && NfaState.generatedStates != 0) {
							ostr.Write("      return ccStartNfa" + LexGen.lexStateSuffix +
							           "(" + (i - 2) + ", ");
							for (j = 0; j < maxLongsReqd - 1; j++)
								if (i <= maxLenForActive[j] + 1)
									ostr.Write("old" + j + ", ");
								else
									ostr.Write("0L, ");
							if (i <= maxLenForActive[j] + 1)
								ostr.WriteLine("old" + j + ");");
							else
								ostr.WriteLine("0L);");
						} else if (NfaState.generatedStates != 0)
							ostr.WriteLine("      return ccMoveNfa" + LexGen.lexStateSuffix +
							             "(" + NfaState.InitStateName() + ", " + (i - 1) + ");");
						else
							ostr.WriteLine("      return " + i + ";");
					}

					if (i != 0 && Options.getDebugTokenManager()) {
						ostr.WriteLine("   if (ccMatchedKind != 0 && ccMatchedKind != Int32.MaxValue)");
						ostr.WriteLine("      debugStream.WriteLine(\"   Currently matched the first \" + " +
						             "(ccMatchedPos + 1) + \" characters as a \" + tokenImage[ccMatchedKind] + \" token.\");");

						ostr.WriteLine("   debugStream.WriteLine(\"   Possible string literal matches : { \"");

						for (int vecs = 0; vecs < maxStrKind/64 + 1; vecs++) {
							if (i <= maxLenForActive[vecs]) {
								ostr.WriteLine(" +");
								ostr.Write("         ccKindsForBitVector(" + vecs + ", ");
								ostr.Write("active" + vecs + ") ");
							}
						}

						ostr.WriteLine(" + \" } \");");
					}

					ostr.WriteLine("   try { curChar = inputStream.ReadChar(); }");
					ostr.WriteLine("   catch(System.IO.IOException) {");

					if (!LexGen.mixed[LexGen.lexStateIndex] && NfaState.generatedStates != 0) {
						ostr.Write("      ccStopStringLiteralDfa" + LexGen.lexStateSuffix + "(" + (i - 1) + ", ");
						for (k = 0; k < maxLongsReqd - 1; k++)
							if (i <= maxLenForActive[k])
								ostr.Write("active" + k + ", ");
							else
								ostr.Write("0L, ");

						if (i <= maxLenForActive[k])
							ostr.WriteLine("active" + k + ");");
						else
							ostr.WriteLine("0L);");


						if (i != 0 && Options.getDebugTokenManager()) {
							ostr.WriteLine("      if (ccMatchedKind != 0 && ccMatchedKind != Int32.MaxValue)");
							ostr.WriteLine("         debugStream.WriteLine(\"   Currently matched the first \" + " +
							             "(ccMatchedPos + 1) + \" characters as a \" + tokenImage[ccMatchedKind] + \" token.\");");
						}
						ostr.WriteLine("      return " + i + ";");
					} else if (NfaState.generatedStates != 0)
						ostr.WriteLine("   return ccMoveNfa" + LexGen.lexStateSuffix + "(" + NfaState.InitStateName() +
						             ", " + (i - 1) + ");");
					else
						ostr.WriteLine("      return " + i + ";");

					ostr.WriteLine("   }");
				}

				if (i != 0 && Options.getDebugTokenManager())
					ostr.WriteLine("   debugStream.WriteLine(" +
					             (LexGen.maxLexStates > 1 ? "\"<\" + lexStateNames[curLexState] + \">\" + " : "") +
					             "\"Current character : \" + " +
					             "TokenMgrError.AddEscapes(curChar.ToString()) + \" (\" + (int)curChar + \") " +
					             "at line \" + inputStream.EndLine + \" column \" + inputStream.EndColumn);");

				ostr.Write("   switch((int)curChar)");
				ostr.WriteLine("   {");

				for (int q = 0; q < keys.Length; q++) {
					key = keys[q];
					info = (KindInfo) tab[key];
					ifGenerated = false;
					char c = key[0];

					if (i == 0 && c < 128 && info.finalKindCnt != 0 &&
					    (NfaState.generatedStates == 0 || !NfaState.CanStartNfaUsingAscii(c))) {
						int kind;
						for (j = 0; j < maxLongsReqd; j++)
							if (info.finalKinds[j] != 0L)
								break;

						for (k = 0; k < 64; k++)
							if ((info.finalKinds[j] & (1L << k)) != 0L &&
							    !subString[kind = (j*64 + k)]) {
								if ((intermediateKinds != null &&
								     intermediateKinds[(j*64 + k)] != null &&
								     intermediateKinds[(j*64 + k)][i] < (j*64 + k) &&
								     intermediateMatchedPos != null &&
								     intermediateMatchedPos[(j*64 + k)][i] == i) ||
								    (LexGen.canMatchAnyChar[LexGen.lexStateIndex] >= 0 &&
								     LexGen.canMatchAnyChar[LexGen.lexStateIndex] < (j*64 + k)))
									break;
								else if ((LexGen.toSkip[kind/64] & (1L << (kind%64))) != 0L &&
								         (LexGen.toSpecial[kind/64] & (1L << (kind%64))) == 0L &&
								         LexGen.actions[kind] == null &&
								         LexGen.newLexState[kind] == null) {
									LexGen.AddCharToSkip(c, kind);

									if (Options.getIgnoreCase()) {
										if (c != Char.ToUpper(c))
											LexGen.AddCharToSkip(Char.ToUpper(c), kind);

										if (c != Char.ToLower(c))
											LexGen.AddCharToSkip(Char.ToLower(c), kind);
									}
									goto CaseLoop;
								}
							}
					}

					// Since we know key is a single character ...
					if (Options.getIgnoreCase()) {
						if (c != Char.ToUpper(c))
							ostr.WriteLine("      case " + (int) Char.ToUpper(c) + ":");

						if (c != Char.ToLower(c))
							ostr.WriteLine("      case " + (int) Char.ToLower(c) + ":");
					}

					ostr.WriteLine("      case " + (int) c + ":");

					long matchedKind;
					String prefix = (i == 0) ? "         " : "            ";

					if (info.finalKindCnt != 0) {
						for (j = 0; j < maxLongsReqd; j++) {
							if ((matchedKind = info.finalKinds[j]) == 0L)
								continue;

							for (k = 0; k < 64; k++) {
								if ((matchedKind & (1L << k)) == 0L)
									continue;

								if (ifGenerated) {
									ostr.Write("         else if ");
								} else if (i != 0)
									ostr.Write("         if ");

								ifGenerated = true;

								int kindToPrint;
								if (i != 0) {
									ostr.WriteLine("((active" + j + " & " + (1L << k) + "L) != 0L)");
								}

								if (intermediateKinds != null &&
								    intermediateKinds[(j*64 + k)] != null &&
								    intermediateKinds[(j*64 + k)][i] < (j*64 + k) &&
								    intermediateMatchedPos != null &&
								    intermediateMatchedPos[(j*64 + k)][i] == i) {
									CSharpCCErrors.Warning(" \"" +
									                     CSharpCCGlobals.AddEscapes(allImages[j*64 + k]) +
									                     "\" cannot be matched as a string literal token " +
									                     "at line " + GetLine(j*64 + k) + ", column " + GetColumn(j*64 + k) +
									                     ". It will be matched as " +
									                     GetLabel(intermediateKinds[(j*64 + k)][i]) + ".");
									kindToPrint = intermediateKinds[(j*64 + k)][i];
								} else if (i == 0 &&
								           LexGen.canMatchAnyChar[LexGen.lexStateIndex] >= 0 &&
								           LexGen.canMatchAnyChar[LexGen.lexStateIndex] < (j*64 + k)) {
									CSharpCCErrors.Warning(" \"" +
									                     CSharpCCGlobals.AddEscapes(allImages[j*64 + k]) +
									                     "\" cannot be matched as a string literal token " +
									                     "at line " + GetLine(j*64 + k) + ", column " + GetColumn(j*64 + k) +
									                     ". It will be matched as " +
									                     GetLabel(LexGen.canMatchAnyChar[LexGen.lexStateIndex]) + ".");
									kindToPrint = LexGen.canMatchAnyChar[LexGen.lexStateIndex];
								} else
									kindToPrint = j*64 + k;

								if (!subString[(j*64 + k)]) {
									int stateSetName = GetStateSetForKind(i, j*64 + k);

									if (stateSetName != -1) {
										createStartNfa = true;
										ostr.WriteLine(prefix + "return ccStartNfaWithStates" +
										             LexGen.lexStateSuffix + "(" + i +
										             ", " + kindToPrint + ", " + stateSetName + ");");
									} else
										ostr.WriteLine(prefix + "return ccStopAtPos" + "(" + i + ", " + kindToPrint + ");");
								} else {
									if ((LexGen.initMatch[LexGen.lexStateIndex] != 0 &&
									     LexGen.initMatch[LexGen.lexStateIndex] != Int32.MaxValue) ||
									    i != 0) {
										ostr.WriteLine("         {");
										ostr.WriteLine(prefix + "ccMatchedKind = " +
										             kindToPrint + ";");
										ostr.WriteLine(prefix + "ccMatchedPos = " + i + ";");
										ostr.WriteLine("         }");
									} else
										ostr.WriteLine(prefix + "ccMatchedKind = " +
										             kindToPrint + ";");
								}
							}
						}
					}

					if (info.validKindCnt != 0) {
						atLeastOne = false;

						if (i == 0) {
							ostr.Write("         return ");

							ostr.Write("ccMoveStringLiteralDfa" + (i + 1) +
							           LexGen.lexStateSuffix + "(");
							for (j = 0; j < maxLongsReqd - 1; j++)
								if ((i + 1) <= maxLenForActive[j]) {
									if (atLeastOne)
										ostr.Write(", ");
									else
										atLeastOne = true;

									ostr.Write(info.validKinds[j] + "L");
								}

							if ((i + 1) <= maxLenForActive[j]) {
								if (atLeastOne)
									ostr.Write(", ");

								ostr.Write((info.validKinds[j]) + "L");
							}
							ostr.WriteLine(");");
						} else {
							ostr.Write("         return ");

							ostr.Write("ccMoveStringLiteralDfa" + (i + 1) + LexGen.lexStateSuffix + "(");

							for (j = 0; j < maxLongsReqd - 1; j++)
								if ((i + 1) <= maxLenForActive[j] + 1) {
									if (atLeastOne)
										ostr.Write(", ");
									else
										atLeastOne = true;

									if (info.validKinds[j] != 0L)
										ostr.Write("active" + j + ", " + (info.validKinds[j]) + "L");
									else
										ostr.Write("active" + j + ", 0L");
								}

							if ((i + 1) <= maxLenForActive[j] + 1) {
								if (atLeastOne)
									ostr.Write(", ");
								if (info.validKinds[j] != 0L)
									ostr.Write("active" + j + ", " + (info.validKinds[j]) + "L");
								else
									ostr.Write("active" + j + ", 0L");
							}

							ostr.WriteLine(");");
						}
					} else {
						// A very special case.
						if (i == 0 && LexGen.mixed[LexGen.lexStateIndex]) {

							if (NfaState.generatedStates != 0)
								ostr.WriteLine("         return ccMoveNfa" + LexGen.lexStateSuffix + "(" + NfaState.InitStateName() + ", 0);");
							else
								ostr.WriteLine("         return 1;");
						} else if (i != 0) // No more str literals to look for
						{
							ostr.WriteLine("         break;");
							startNfaNeeded = true;
						}
					}

				CaseLoop:
					;
				}

				/* default means that the current character is not in any of the
           strings at this position. */
				ostr.WriteLine("      default :");

				if (Options.getDebugTokenManager())
					ostr.WriteLine("      debugStream.WriteLine(\"   No string literal matches possible.\");");

				if (NfaState.generatedStates != 0) {
					if (i == 0) {
						/* This means no string literal is possible. Just move nfa with
                 this guy and return. */
						ostr.WriteLine("         return ccMoveNfa" + LexGen.lexStateSuffix + "(" + NfaState.InitStateName() + ", 0);");
					} else {
						ostr.WriteLine("         break;");
						startNfaNeeded = true;
					}
				} else {
					ostr.WriteLine("         return " + (i + 1) + ";");
				}


				ostr.WriteLine("   }");

				if (i != 0) {
					if (startNfaNeeded) {
						if (!LexGen.mixed[LexGen.lexStateIndex] && NfaState.generatedStates != 0) {
							/* Here, a string literal is successfully matched and no more
                 string literals are possible. So set the kind and state set
                 upto and including this position for the matched string. */

							ostr.Write("   return ccStartNfa" + LexGen.lexStateSuffix + "(" + (i - 1) + ", ");
							for (k = 0; k < maxLongsReqd - 1; k++)
								if (i <= maxLenForActive[k])
									ostr.Write("active" + k + ", ");
								else
									ostr.Write("0L, ");
							if (i <= maxLenForActive[k])
								ostr.WriteLine("active" + k + ");");
							else
								ostr.WriteLine("0L);");
						} else if (NfaState.generatedStates != 0)
							ostr.WriteLine("   return ccMoveNfa" + LexGen.lexStateSuffix +
							             "(" + NfaState.InitStateName() + ", " + i + ");");
						else
							ostr.WriteLine("   return " + (i + 1) + ";");
					}
				}

				ostr.WriteLine("}");
			}

			if (!LexGen.mixed[LexGen.lexStateIndex] && NfaState.generatedStates != 0 && createStartNfa)
				DumpStartWithStates(ostr);
		}

		private static int GetStrKind(String str) {
			for (int i = 0; i < maxStrKind; i++) {
				if (LexGen.lexStates[i] != LexGen.lexStateIndex)
					continue;

				String image = allImages[i];
				if (image != null && image.Equals(str))
					return i;
			}

			return Int32.MaxValue;
		}

	    internal static void GenerateNfaStartStates(TextWriter ostr, NfaState initialState) {
			bool[] seen = new bool[NfaState.generatedStates];
			IDictionary<string, string> stateSets = new Dictionary<string, string>();
			String stateSetString = "";
			int i, j, kind, jjmatchedPos = 0;
			int maxKindsReqd = maxStrKind/64 + 1;
			long[] actives;
			IList<NfaState> newStates = new List<NfaState>();
			IList<NfaState> oldStates = null, jjtmpStates;

			statesForPos = new IDictionary<string, long[]>[maxLen];
			intermediateKinds = new int[maxStrKind + 1][];
			intermediateMatchedPos = new int[maxStrKind + 1][];

			for (i = 0; i < maxStrKind; i++) {
				if (LexGen.lexStates[i] != LexGen.lexStateIndex)
					continue;

				String image = allImages[i];

				if (image == null || image.Length < 1)
					continue;

				try {
					if ((oldStates = new List<NfaState>(initialState.epsilonMoves)).Count == 0) {
						DumpNfaStartStatesCode(statesForPos, ostr);
						return;
					}
				} catch (Exception e) {
					CSharpCCErrors.SemanticError("Error cloning state vector");
				}

				intermediateKinds[i] = new int[image.Length];
				intermediateMatchedPos[i] = new int[image.Length];
				jjmatchedPos = 0;
				kind = Int32.MaxValue;

				for (j = 0; j < image.Length; j++) {
					if (oldStates == null || oldStates.Count <= 0) {
						// Here, j > 0
						kind = intermediateKinds[i][j] = intermediateKinds[i][j - 1];
						jjmatchedPos = intermediateMatchedPos[i][j] = intermediateMatchedPos[i][j - 1];
					} else {
						kind = NfaState.MoveFromSet(image[j], oldStates, newStates);
						oldStates.Clear();

						if (j == 0 && kind != Int32.MaxValue &&
						    LexGen.canMatchAnyChar[LexGen.lexStateIndex] != -1 &&
						    kind > LexGen.canMatchAnyChar[LexGen.lexStateIndex])
							kind = LexGen.canMatchAnyChar[LexGen.lexStateIndex];

						if (GetStrKind(image.Substring(0, j + 1)) < kind) {
							intermediateKinds[i][j] = kind = Int32.MaxValue;
							jjmatchedPos = 0;
						} else if (kind != Int32.MaxValue) {
							intermediateKinds[i][j] = kind;
							jjmatchedPos = intermediateMatchedPos[i][j] = j;
						} else if (j == 0)
							kind = intermediateKinds[i][j] = Int32.MaxValue;
						else {
							kind = intermediateKinds[i][j] = intermediateKinds[i][j - 1];
							jjmatchedPos = intermediateMatchedPos[i][j] = intermediateMatchedPos[i][j - 1];
						}

						stateSetString = NfaState.GetStateSetString(newStates);
					}

					if (kind == Int32.MaxValue &&
					    (newStates == null || newStates.Count == 0))
						continue;

					int p;
					if (stateSets.ContainsKey(stateSetString)) {
						stateSets[stateSetString] = stateSetString;
						for (p = 0; p < newStates.Count; p++) {
							if (seen[newStates[p].stateName])
								newStates[p].inNextOf++;
							else
								seen[newStates[p].stateName] = true;
						}
					} else {
						for (p = 0; p < newStates.Count; p++)
							seen[newStates[p].stateName] = true;
					}

					jjtmpStates = oldStates;
					oldStates = newStates;
					(newStates = jjtmpStates).Clear();

					if (statesForPos[j] == null)
						statesForPos[j] = new Dictionary<string, long[]>();

					if (!(statesForPos[j].TryGetValue(kind + ", " + jjmatchedPos + ", " + stateSetString, out actives))) {
						actives = new long[maxKindsReqd];
						statesForPos[j][kind + ", " + jjmatchedPos + ", " + stateSetString] = actives;
					}

					actives[i/64] |= 1L << (i%64);
					//String name = NfaState.StoreStateSet(stateSetString);
				}
			}

			DumpNfaStartStatesCode(statesForPos, ostr);
		}

		private static void DumpNfaStartStatesCode(IDictionary<string, long[]>[] statesForPos, TextWriter ostr) {
			if (maxStrKind == 0) {
				// No need to generate this function
				return;
			}

			int i, maxKindsReqd = maxStrKind/64 + 1;
			bool condGenerated = false;
			int ind = 0;

			ostr.Write("private" + (Options.getStatic() ? " static" : "") + " int ccStopStringLiteralDfa" + LexGen.lexStateSuffix + "(int pos, ");
			for (i = 0; i < maxKindsReqd - 1; i++)
				ostr.Write("long active" + i + ", ");
			ostr.WriteLine("long active" + i + ")\n{");

			if (Options.getDebugTokenManager())
				ostr.WriteLine("      debugStream.WriteLine(\"   No more string literal token matches are possible.\");");

			ostr.WriteLine("   switch (pos)\n   {");

			for (i = 0; i < maxLen - 1; i++) {
				if (statesForPos[i] == null)
					continue;

				ostr.WriteLine("      case " + i + ":");

				foreach (KeyValuePair<string, long[]> entry in statesForPos[i]) {
					String stateSetString = entry.Key;
					long[] actives = entry.Value;

					for (int j = 0; j < maxKindsReqd; j++) {
						if (actives[j] == 0L)
							continue;

						if (condGenerated)
							ostr.Write(" || ");
						else
							ostr.Write("         if (");

						condGenerated = true;

						ostr.Write("(active" + j + " & " + (actives[j]) + "L) != 0L");
					}

					if (condGenerated) {
						ostr.WriteLine(")");

						String kindStr = stateSetString.Substring(0, ind = stateSetString.IndexOf(", "));
						String afterKind = stateSetString.Substring(ind + 2);
						int jjmatchedPos = Int32.Parse(afterKind.Substring(0, afterKind.IndexOf(", ")));

						if (!kindStr.Equals(Int32.MaxValue.ToString()))
							ostr.WriteLine("         {");

						if (!kindStr.Equals(Int32.MaxValue.ToString())) {
							if (i == 0) {
								ostr.WriteLine("            ccMatchedKind = " + kindStr + ";");

								if ((LexGen.initMatch[LexGen.lexStateIndex] != 0 &&
								     LexGen.initMatch[LexGen.lexStateIndex] != Int32.MaxValue))
									ostr.WriteLine("            ccMatchedPos = 0;");
							} else if (i == jjmatchedPos) {
								if (subStringAtPos[i]) {
									ostr.WriteLine("            if (ccMatchedPos != " + i + ")");
									ostr.WriteLine("            {");
									ostr.WriteLine("               ccMatchedKind = " + kindStr + ";");
									ostr.WriteLine("               ccMatchedPos = " + i + ";");
									ostr.WriteLine("            }");
								} else {
									ostr.WriteLine("            ccMatchedKind = " + kindStr + ";");
									ostr.WriteLine("            ccMatchedPos = " + i + ";");
								}
							} else {
								if (jjmatchedPos > 0)
									ostr.WriteLine("            if (ccMatchedPos < " + jjmatchedPos + ")");
								else
									ostr.WriteLine("            if (ccMatchedPos == 0)");
								ostr.WriteLine("            {");
								ostr.WriteLine("               ccMatchedKind = " + kindStr + ";");
								ostr.WriteLine("               ccMatchedPos = " + jjmatchedPos + ";");
								ostr.WriteLine("            }");
							}
						}

						kindStr = stateSetString.Substring(0, ind = stateSetString.IndexOf(", "));
						afterKind = stateSetString.Substring(ind + 2);
						stateSetString = afterKind.Substring(afterKind.IndexOf(", ") + 2);

						if (stateSetString.Equals("null;"))
							ostr.WriteLine("            return -1;");
						else
							ostr.WriteLine("            return " + NfaState.AddStartStateSet(stateSetString) + ";");

						if (!kindStr.Equals(Int32.MaxValue.ToString()))
							ostr.WriteLine("         }");
						condGenerated = false;
					}
				}

				ostr.WriteLine("         return -1;");
			}

			ostr.WriteLine("      default :");
			ostr.WriteLine("         return -1;");
			ostr.WriteLine("   }");
			ostr.WriteLine("}");

			ostr.Write("private" + (Options.getStatic() ? " static" : "") + " int ccStartNfa" + LexGen.lexStateSuffix + "(int pos, ");
			for (i = 0; i < maxKindsReqd - 1; i++)
				ostr.Write("long active" + i + ", ");
			ostr.WriteLine("long active" + i + ")\n{");

			if (LexGen.mixed[LexGen.lexStateIndex]) {
				if (NfaState.generatedStates != 0)
					ostr.WriteLine("   return ccMoveNfa" + LexGen.lexStateSuffix +
					             "(" + NfaState.InitStateName() + ", pos + 1);");
				else
					ostr.WriteLine("   return pos + 1;");

				ostr.WriteLine("}");
				return;
			}

			ostr.Write("   return ccMoveNfa" + LexGen.lexStateSuffix + "(" +
			           "ccStopStringLiteralDfa" + LexGen.lexStateSuffix + "(pos, ");
			for (i = 0; i < maxKindsReqd - 1; i++)
				ostr.Write("active" + i + ", ");
			ostr.Write("active" + i + ")");
			ostr.WriteLine(", pos + 1);");
			ostr.WriteLine("}");
		}

		public static void reInit() {
			ReInit();

			charCnt = 0;
			allImages = null;
			boilerPlateDumped = false;
		}

		public override StringBuilder Dump(int indent, IList alreadyDumped) {
			return base.Dump(indent, alreadyDumped).Append(' ').Append(Image);
		}


		#region KindInfo

		private class KindInfo {
			public long[] validKinds;
			public long[] finalKinds;
			public int validKindCnt = 0;
			public int finalKindCnt = 0;

			public KindInfo(int maxKind) {
				validKinds = new long[maxKind/64 + 1];
				finalKinds = new long[maxKind/64 + 1];
			}

			public void InsertValidKind(int kind) {
				validKinds[kind/64] |= (1L << (kind%64));
				validKindCnt++;
			}

			public void InsertFinalKind(int kind) {
				finalKinds[kind/64] |= (1L << (kind%64));
				finalKindCnt++;
			}

		}

		#endregion
	}
}