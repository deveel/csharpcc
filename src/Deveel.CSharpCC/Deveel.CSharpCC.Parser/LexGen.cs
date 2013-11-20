using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Deveel.CSharpCC.Parser {
	public class LexGen {
        private static TextWriter ostr;
        private static String staticString;
        private static String tokMgrClassName;
	    private static bool namespaceInserted;

        // Hashtable of vectors
        private static IDictionary<string, IList<TokenProduction>> allTpsForState = new Dictionary<string, IList<TokenProduction>>();
        public static int lexStateIndex = 0;
        private static int[] kinds;
        public static int maxOrdinal = 1;
        public static String lexStateSuffix;
        internal static String[] newLexState;
        public static int[] lexStates;
        public static bool[] ignoreCase;
        public static Action[] actions;
        public static IDictionary<string, NfaState> initStates = new Dictionary<string, NfaState>();
        public static int stateSetSize;
        public static int maxLexStates;
        public static String[] lexStateName;
        private static NfaState[] singlesToSkip;
        public static long[] toSkip;
        public static long[] toSpecial;
        public static long[] toMore;
        public static long[] toToken;
        public static int defaultLexState;
        public static RegularExpression[] rexprs;
        public static int[] maxLongsReqd;
        public static int[] initMatch;
        public static int[] canMatchAnyChar;
        public static bool hasEmptyMatch;
        public static bool[] canLoop;
        public static bool[] stateHasActions;
        public static bool hasLoop = false;
        public static bool[] canReachOnMore;
        public static bool[] hasNfa;
        public static bool[] mixed;
        public static NfaState initialState;
        public static int curKind;
        private static bool hasSkipActions = false;
        private static bool hasMoreActions = false;
        private static bool hasTokenActions = false;
        private static bool hasSpecial = false;
        private static bool hasSkip = false;
        private static bool hasMore = false;
        public static RegularExpression curRE;
        public static bool keepLineCol;

        // Assumes l != 0L
        static char MaxChar(long l)
        {
            for (int i = 64; i-- > 0; )
                if ((l & (1L << i)) != 0L)
                    return (char)i;

            return (char) 0xffff;
        }

        public static void AddCharToSkip(char c, int kind) {
            singlesToSkip[lexStateIndex].AddChar(c);
            singlesToSkip[lexStateIndex].kind = kind;
        }

        private static void PrintClassHead() {
            int i, j;

            try {
                string tmp = Path.Combine(Options.getOutputDirectory().FullName, tokMgrClassName + ".cs");
                ostr = new StreamWriter(tmp);
                List<string> tn = new List<string>(CSharpCCGlobals.ToolNames);
                tn.Add(CSharpCCGlobals.ToolName);

                ostr.WriteLine("/* " + CSharpCCGlobals.GetIdString(tn, tokMgrClassName + ".cs") + " */");

                int l = 0, kind;
                i = 1;
                for (;;) {
                    if (CSharpCCGlobals.cu_to_insertion_point_1.Count <= l)
                        break;

                    // TODO: write the namespace ...
                    kind = CSharpCCGlobals.cu_to_insertion_point_1[l].kind;
					int gKind = kind;
                    if (kind == CSharpCCParserConstants.NAMESPACE ||
                        kind == CSharpCCParserConstants.USING) {
                        for (; i < CSharpCCGlobals.cu_to_insertion_point_1.Count; i++) {
                            kind = CSharpCCGlobals.cu_to_insertion_point_1[i].kind;
                            if (kind == CSharpCCParserConstants.SEMICOLON ||
                                kind == CSharpCCParserConstants.ABSTRACT ||
                                kind == CSharpCCParserConstants.SEALED ||
                                kind == CSharpCCParserConstants.PUBLIC ||
                                kind == CSharpCCParserConstants.CLASS ||
                                kind == CSharpCCParserConstants.INTERFACE) {
                                CSharpCCGlobals.cline = CSharpCCGlobals.cu_to_insertion_point_1[l].beginLine;
                                CSharpCCGlobals.ccol = CSharpCCGlobals.cu_to_insertion_point_1[l].beginColumn;
                                for (j = l; j < i; j++) {
                                    CSharpCCGlobals.PrintToken(CSharpCCGlobals.cu_to_insertion_point_1[j], ostr);
                                }
	                            if (kind == CSharpCCParserConstants.SEMICOLON) {
									if (gKind == CSharpCCParserConstants.USING) {
										CSharpCCGlobals.PrintToken((Token)(CSharpCCGlobals.cu_to_insertion_point_1[j]), ostr);
									} else if (gKind == CSharpCCParserConstants.NAMESPACE) {
										ostr.WriteLine(" {");
									    namespaceInserted = true;
									}
	                            }
	                            ostr.WriteLine("");
                                break;
                            }
                        }
                        l = ++i;
                    } else
                        break;
                }

                ostr.WriteLine("");
                ostr.WriteLine("// Token Manager.");
                if (Options.getSupportClassVisibilityPublic()) {
                    ostr.Write("public ");
                }
                ostr.WriteLine("class " + tokMgrClassName + " : " + CSharpCCGlobals.cu_name + "Constants {");
            } catch (IOException) {
                CSharpCCErrors.SemanticError("Could not create file : " + tokMgrClassName + ".cs\n");
                throw new InvalidOperationException();
            }

            if (CSharpCCGlobals.token_mgr_decls != null &&
                CSharpCCGlobals.token_mgr_decls.Count > 0) {
                Token t = CSharpCCGlobals.token_mgr_decls[0];
                bool commonTokenActionSeen = false;
                bool commonTokenActionNeeded = Options.getCommonTokenAction();

                CSharpCCGlobals.PrintTokenSetup(CSharpCCGlobals.token_mgr_decls[0]);
                CSharpCCGlobals.ccol = 1;

                for (j = 0; j < CSharpCCGlobals.token_mgr_decls.Count; j++) {
                    t = CSharpCCGlobals.token_mgr_decls[j];
                    if (t.kind == CSharpCCParserConstants.IDENTIFIER &&
                        commonTokenActionNeeded &&
                        !commonTokenActionSeen)
                        commonTokenActionSeen = t.image.Equals("CommonTokenAction");

                    CSharpCCGlobals.PrintToken(t, ostr);
                }

                ostr.WriteLine("");
                if (commonTokenActionNeeded && !commonTokenActionSeen)
                    CSharpCCErrors.Warning("You have the COMMON_TOKEN_ACTION option set. " +
                                           "But it appears you have not defined the method :\n" +
                                           "      " + staticString + "void CommonTokenAction(Token t)\n" +
                                           "in your TOKEN_MGR_DECLS. The generated token manager will not compile.");

            } else if (Options.getCommonTokenAction()) {
                CSharpCCErrors.Warning("You have the COMMON_TOKEN_ACTION option set. " +
                                       "But you have not defined the method :\n" +
                                       "      " + staticString + "void CommonTokenAction(Token t)\n" +
                                       "in your TOKEN_MGR_DECLS. The generated token manager will not compile.");
            }

            ostr.WriteLine("");
            ostr.WriteLine("  // Debug output.");
            ostr.WriteLine("  public " + staticString + " System.IO.TextWriter debugStream = Console.Out;");
            ostr.WriteLine("  // Set debug output.");
            ostr.WriteLine("  public " + staticString + " void SetDebugStream(System.IO.TextWriter ds) { debugStream = ds; }");

            if (Options.getTokenManagerUsesParser() && !Options.getStatic()) {
                ostr.WriteLine("");
                ostr.WriteLine("  // The parser.");
                ostr.WriteLine("  public " + CSharpCCGlobals.cu_name + " parser = null;");
            }
        }

        private static void DumpDebugMethods() {

            ostr.WriteLine("  " + staticString + " int kindCnt = 0;");
            ostr.WriteLine("  internal " + staticString + " string ccKindsForBitVector(int i, long vec)");
            ostr.WriteLine("  {");
            ostr.WriteLine("    String retVal = \"\";");
            ostr.WriteLine("    if (i == 0)");
            ostr.WriteLine("       kindCnt = 0;");
            ostr.WriteLine("    for (int j = 0; j < 64; j++)");
            ostr.WriteLine("    {");
            ostr.WriteLine("       if ((vec & (1L << j)) != 0L)");
            ostr.WriteLine("       {");
            ostr.WriteLine("          if (kindCnt++ > 0)");
            ostr.WriteLine("             retVal += \", \";");
            ostr.WriteLine("          if (kindCnt % 5 == 0)");
            ostr.WriteLine("             retVal += \"\\n     \";");
            ostr.WriteLine("          retVal += tokenImage[i * 64 + j];");
            ostr.WriteLine("       }");
            ostr.WriteLine("    }");
            ostr.WriteLine("    return retVal;");
            ostr.WriteLine("  }");
            ostr.WriteLine("");

            ostr.WriteLine("  internal " + staticString + " string ccKindsForStateVector(" + "int lexState, int[] vec, int start, int end)");
            ostr.WriteLine("  {");
            ostr.WriteLine("    bool[] kindDone = new bool[" + maxOrdinal + "];");
            ostr.WriteLine("    string retVal = \"\";");
            ostr.WriteLine("    int cnt = 0;");
            ostr.WriteLine("    for (int i = start; i < end; i++)");
            ostr.WriteLine("    {");
            ostr.WriteLine("     if (vec[i] == -1)");
            ostr.WriteLine("       continue;");
            ostr.WriteLine("     int[] stateSet = statesForState[curLexState][vec[i]];");
            ostr.WriteLine("     for (int j = 0; j < stateSet.length; j++)");
            ostr.WriteLine("     {");
            ostr.WriteLine("       int state = stateSet[j];");
            ostr.WriteLine("       if (!kindDone[kindForState[lexState][state]])");
            ostr.WriteLine("       {");
            ostr.WriteLine("          kindDone[kindForState[lexState][state]] = true;");
            ostr.WriteLine("          if (cnt++ > 0)");
            ostr.WriteLine("             retVal += \", \";");
            ostr.WriteLine("          if (cnt % 5 == 0)");
            ostr.WriteLine("             retVal += \"\\n     \";");
            ostr.WriteLine("          retVal += tokenImage[kindForState[lexState][state]];");
            ostr.WriteLine("       }");
            ostr.WriteLine("     }");
            ostr.WriteLine("    }");
            ostr.WriteLine("    if (cnt == 0)");
            ostr.WriteLine("       return \"{  }\";");
            ostr.WriteLine("    else");
            ostr.WriteLine("       return \"{ \" + retVal + \" }\";");
            ostr.WriteLine("  }");
            ostr.WriteLine("");
        }

        private static void BuildLexStatesTable() {
            IEnumerator it = CSharpCCGlobals.rexprlist.GetEnumerator();
            TokenProduction tp;
            int i;

            String[] tmpLexStateName = new String[CSharpCCGlobals.lexstate_I2S.Count];
            while (it.MoveNext()) {
                tp = (TokenProduction) it.Current;
                IList<RegExprSpec> respecs = tp.RegexSpecs;
                IList<TokenProduction> tps;

                for (i = 0; i < tp.LexStates.Length; i++) {
                    if (!allTpsForState.TryGetValue(tp.LexStates[i], out tps)) {
                        tmpLexStateName[maxLexStates++] = tp.LexStates[i];
                        allTpsForState[tp.LexStates[i]] = tps = new List<TokenProduction>();
                    }

                    tps.Add(tp);
                }

                if (respecs == null || respecs.Count == 0)
                    continue;

                RegularExpression re;
                for (i = 0; i < respecs.Count; i++)
                    if (maxOrdinal <= (re = respecs[i].RegularExpression).Ordinal)
                        maxOrdinal = re.Ordinal + 1;
            }

            kinds = new int[maxOrdinal];
            toSkip = new long[maxOrdinal/64 + 1];
            toSpecial = new long[maxOrdinal/64 + 1];
            toMore = new long[maxOrdinal/64 + 1];
            toToken = new long[maxOrdinal/64 + 1];
            toToken[0] = 1L;
            actions = new Action[maxOrdinal];
            actions[0] = CSharpCCGlobals.actForEof;
            hasTokenActions = CSharpCCGlobals.actForEof != null;
            initStates = new Dictionary<string, NfaState>();
            canMatchAnyChar = new int[maxLexStates];
            canLoop = new bool[maxLexStates];
            stateHasActions = new bool[maxLexStates];
            lexStateName = new String[maxLexStates];
            singlesToSkip = new NfaState[maxLexStates];
            Array.Copy(tmpLexStateName, 0, lexStateName, 0, maxLexStates);

            for (i = 0; i < maxLexStates; i++)
                canMatchAnyChar[i] = -1;

            hasNfa = new bool[maxLexStates];
            mixed = new bool[maxLexStates];
            maxLongsReqd = new int[maxLexStates];
            initMatch = new int[maxLexStates];
            newLexState = new String[maxOrdinal];
            newLexState[0] = CSharpCCGlobals.nextStateForEof;
            hasEmptyMatch = false;
            lexStates = new int[maxOrdinal];
            ignoreCase = new bool[maxOrdinal];
            rexprs = new RegularExpression[maxOrdinal];
            RStringLiteral.allImages = new String[maxOrdinal];
            canReachOnMore = new bool[maxLexStates];
        }

        private static int GetIndex(String name) {
            for (int i = 0; i < lexStateName.Length; i++)
                if (lexStateName[i] != null && lexStateName[i].Equals(name))
                    return i;

            throw new InvalidOperationException(); // Should never come here
        }

		public static void start() {
			if (!Options.getBuildTokenManager() ||
			    Options.getUserTokenManager() ||
			    CSharpCCErrors.ErrorCount > 0)
				return;

			keepLineCol = Options.getKeepLineColumn();
			List<RegularExpression> choices = new List<RegularExpression>();
			IEnumerator e;
			TokenProduction tp;
			int i, j;

			staticString = (Options.getStatic() ? "static " : "");
			tokMgrClassName = CSharpCCGlobals.cu_name + "TokenManager";

			PrintClassHead();
			BuildLexStatesTable();

			e = allTpsForState.Keys.GetEnumerator();

			bool ignoring = false;

			while (e.MoveNext()) {
				NfaState.ReInit();
				RStringLiteral.ReInit();

				String key = (String) e.Current;

				lexStateIndex = GetIndex(key);
				lexStateSuffix = "_" + lexStateIndex;
				IList<TokenProduction> allTps = allTpsForState[key];
				initStates[key] = initialState = new NfaState();
				ignoring = false;

				singlesToSkip[lexStateIndex] = new NfaState();
				singlesToSkip[lexStateIndex].dummy = true;

				if (key.Equals("DEFAULT"))
					defaultLexState = lexStateIndex;

				for (i = 0; i < allTps.Count; i++) {
					tp = allTps[i];
					int kind = tp.Kind;
					bool ignore = tp.IgnoreCase;
					IList<RegExprSpec> rexps = tp.RegexSpecs;

					if (i == 0)
						ignoring = ignore;

					for (j = 0; j < rexps.Count; j++) {
						RegExprSpec respec = rexps[j];
						curRE = respec.RegularExpression;

						rexprs[curKind = curRE.Ordinal] = curRE;
						lexStates[curRE.Ordinal] = lexStateIndex;
						ignoreCase[curRE.Ordinal] = ignore;

						if (curRE.IsPrivate) {
							kinds[curRE.Ordinal] = -1;
							continue;
						}

						if (curRE is RStringLiteral &&
						    !((RStringLiteral) curRE).Image.Equals("")) {
							((RStringLiteral) curRE).GenerateDfa(ostr, curRE.Ordinal);
							if (i != 0 && !mixed[lexStateIndex] && ignoring != ignore)
								mixed[lexStateIndex] = true;
						} else if (curRE.CanMatchAnyChar) {
							if (canMatchAnyChar[lexStateIndex] == -1 ||
							    canMatchAnyChar[lexStateIndex] > curRE.Ordinal)
								canMatchAnyChar[lexStateIndex] = curRE.Ordinal;
						} else {
							Nfa temp;

							if (curRE is RChoice)
								choices.Add(curRE);

							temp = curRE.GenerateNfa(ignore);
							temp.End.isFinal = true;
							temp.End.kind = curRE.Ordinal;
							initialState.AddMove(temp.Start);
						}

						if (kinds.Length < curRE.Ordinal) {
							int[] tmp = new int[curRE.Ordinal + 1];

							Array.Copy(kinds, 0, tmp, 0, kinds.Length);
							kinds = tmp;
						}
						//System.out.println("   ordina : " + curRE.ordinal);

						kinds[curRE.Ordinal] = kind;

						if (respec.NextState != null &&
						    !respec.NextState.Equals(lexStateName[lexStateIndex]))
							newLexState[curRE.Ordinal] = respec.NextState;

						if (respec.Action != null && respec.Action.ActionTokens != null &&
						    respec.Action.ActionTokens.Count > 0)
							actions[curRE.Ordinal] = respec.Action;

						switch (kind) {
							case TokenProduction.SPECIAL:
								hasSkipActions |= (actions[curRE.Ordinal] != null) ||
								                  (newLexState[curRE.Ordinal] != null);
								hasSpecial = true;
								toSpecial[curRE.Ordinal/64] |= 1L << (curRE.Ordinal%64);
								toSkip[curRE.Ordinal/64] |= 1L << (curRE.Ordinal%64);
								break;
							case TokenProduction.SKIP:
								hasSkipActions |= (actions[curRE.Ordinal] != null);
								hasSkip = true;
								toSkip[curRE.Ordinal/64] |= 1L << (curRE.Ordinal%64);
								break;
							case TokenProduction.MORE:
								hasMoreActions |= (actions[curRE.Ordinal] != null);
								hasMore = true;
								toMore[curRE.Ordinal/64] |= 1L << (curRE.Ordinal%64);

								if (newLexState[curRE.Ordinal] != null)
									canReachOnMore[GetIndex(newLexState[curRE.Ordinal])] = true;
								else
									canReachOnMore[lexStateIndex] = true;

								break;
							case TokenProduction.TOKEN:
								hasTokenActions |= (actions[curRE.Ordinal] != null);
								toToken[curRE.Ordinal/64] |= 1L << (curRE.Ordinal%64);
								break;
						}
					}
				}

				// Generate a static block for initializing the nfa transitions
				NfaState.ComputeClosures();

				for (i = 0; i < initialState.epsilonMoves.Count; i++)
					initialState.epsilonMoves[i].GenerateCode();

				if (hasNfa[lexStateIndex] = (NfaState.generatedStates != 0)) {
					initialState.GenerateCode();
					initialState.GenerateInitMoves(ostr);
				}

				if (initialState.kind != Int32.MaxValue && initialState.kind != 0) {
					if ((toSkip[initialState.kind/64] & (1L << initialState.kind)) != 0L ||
					    (toSpecial[initialState.kind/64] & (1L << initialState.kind)) != 0L)
						hasSkipActions = true;
					else if ((toMore[initialState.kind/64] & (1L << initialState.kind)) != 0L)
						hasMoreActions = true;
					else
						hasTokenActions = true;

					if (initMatch[lexStateIndex] == 0 ||
					    initMatch[lexStateIndex] > initialState.kind) {
						initMatch[lexStateIndex] = initialState.kind;
						hasEmptyMatch = true;
					}
				} else if (initMatch[lexStateIndex] == 0)
					initMatch[lexStateIndex] = Int32.MaxValue;

				RStringLiteral.FillSubString();

				if (hasNfa[lexStateIndex] && !mixed[lexStateIndex])
					RStringLiteral.GenerateNfaStartStates(ostr, initialState);

				RStringLiteral.DumpDfaCode(ostr);

				if (hasNfa[lexStateIndex])
					NfaState.DumpMoveNfa(ostr);

				if (stateSetSize < NfaState.generatedStates)
					stateSetSize = NfaState.generatedStates;
			}

			for (i = 0; i < choices.Count; i++)
				((RChoice) choices[i]).CheckUnmatchability();

			NfaState.DumpStateSets(ostr);
			CheckEmptyStringMatch();
			NfaState.DumpNonAsciiMoveMethods(ostr);
			RStringLiteral.DumpStrLiteralImages(ostr);
			DumpStaticVarDeclarations();
			DumpFillToken();
			DumpGetNextToken();

			if (Options.getDebugTokenManager()) {
				NfaState.DumpStatesForKind(ostr);
				DumpDebugMethods();
			}

			if (hasLoop) {
				ostr.WriteLine(staticString + "int[] ccEmptyLineNo = new int[" + maxLexStates + "];");
				ostr.WriteLine(staticString + "int[] ccEmptyColNo = new int[" + maxLexStates + "];");
				ostr.WriteLine(staticString + "bool[] ccBeenHere = new bool[" + maxLexStates + "];");
			}

			if (hasSkipActions)
				DumpSkipActions();
			if (hasMoreActions)
				DumpMoreActions();
			if (hasTokenActions)
				DumpTokenActions();

			NfaState.PrintBoilerPlate(ostr);
			ostr.WriteLine( /*{*/ "}");

			if (namespaceInserted)
				ostr.WriteLine("}");
				
			ostr.Close();
		}

		private static void CheckEmptyStringMatch() {
            int i, j, k, len;
            bool[] seen = new bool[maxLexStates];
            bool[] done = new bool[maxLexStates];
            String cycle;
            String reList;

            for (i = 0; i < maxLexStates; i++) {
                if (done[i] || initMatch[i] == 0 || initMatch[i] == Int32.MaxValue ||
                    canMatchAnyChar[i] != -1)
                    continue;

                done[i] = true;
                len = 0;
                cycle = "";
                reList = "";

                for (k = 0; k < maxLexStates; k++)
                    seen[k] = false;

                j = i;
                seen[i] = true;
                cycle += lexStateName[j] + "-->";
                while (newLexState[initMatch[j]] != null) {
                    cycle += newLexState[initMatch[j]];
                    if (seen[j = GetIndex(newLexState[initMatch[j]])])
                        break;

                    cycle += "-->";
                    done[j] = true;
                    seen[j] = true;
                    if (initMatch[j] == 0 || initMatch[j] == Int32.MaxValue ||
                        canMatchAnyChar[j] != -1)
                        goto Outer;
                    if (len != 0)
                        reList += "; ";
                    reList += "line " + rexprs[initMatch[j]].Line + ", column " +
                              rexprs[initMatch[j]].Column;
                    len++;
                }

                if (newLexState[initMatch[j]] == null)
                    cycle += lexStateName[lexStates[initMatch[j]]];

                for (k = 0; k < maxLexStates; k++)
                    canLoop[k] |= seen[k];

                hasLoop = true;
                if (len == 0)
                    CSharpCCErrors.Warning(rexprs[initMatch[i]],
                        "Regular expression" + ((rexprs[initMatch[i]].Label.Equals(""))
                            ? ""
                            : (" for " + rexprs[initMatch[i]].Label)) +
                        " can be matched by the empty string (\"\") in lexical state " +
                        lexStateName[i] + ". This can result in an endless loop of " +
                        "empty string matches.");
                else {
                    CSharpCCErrors.Warning(rexprs[initMatch[i]],
                        "Regular expression" + ((rexprs[initMatch[i]].Label.Equals(""))
                            ? ""
                            : (" for " + rexprs[initMatch[i]].Label)) +
                        " can be matched by the empty string (\"\") in lexical state " +
                        lexStateName[i] + ". This regular expression along with the " +
                        "regular expressions at " + reList + " forms the cycle \n   " +
                        cycle + "\ncontaining regular expressions with empty matches." +
                        " This can result in an endless loop of empty string matches.");
                }
            }
            Outer:
            ;
        }

        private static void DumpStaticVarDeclarations() {
            int i;
            String charStreamName;

            ostr.WriteLine("");
            ostr.WriteLine("// Lexer state names.");
            ostr.WriteLine("public static readonly string[] lexStateNames = {");
            for (i = 0; i < maxLexStates; i++)
                ostr.WriteLine("   \"" + lexStateName[i] + "\",");
            ostr.WriteLine("};");

            if (maxLexStates > 1) {
                ostr.WriteLine("");
                ostr.WriteLine("// Lex State array.");
                ostr.Write("public static readonly int[] ccNewLexState = {");

                for (i = 0; i < maxOrdinal; i++) {
                    if (i%25 == 0)
                        ostr.Write("\n   ");

                    if (newLexState[i] == null)
                        ostr.Write("-1, ");
                    else
                        ostr.Write(GetIndex(newLexState[i]) + ", ");
                }
                ostr.WriteLine("\n};");
            }

            if (hasSkip || hasMore || hasSpecial) {
                // Bit vector for TOKEN
                ostr.Write("static readonly long[] ccToToken = {");
                for (i = 0; i < maxOrdinal/64 + 1; i++) {
                    if (i%4 == 0)
                        ostr.Write("\n   ");
                    ostr.Write("0x" + toToken[i].ToString("X") + "L, ");
                }
                ostr.WriteLine("\n};");
            }

            if (hasSkip || hasSpecial) {
                // Bit vector for SKIP
                ostr.Write("static readonly long[] ccToSkip = {");
                for (i = 0; i < maxOrdinal/64 + 1; i++) {
                    if (i%4 == 0)
                        ostr.Write("\n   ");
                    ostr.Write("0x" + toSkip[i].ToString("X") + "L, ");
                }
                ostr.WriteLine("\n};");
            }

            if (hasSpecial) {
                // Bit vector for SPECIAL
                ostr.Write("static readonly long[] ccToSpecial = {");
                for (i = 0; i < maxOrdinal/64 + 1; i++) {
                    if (i%4 == 0)
                        ostr.Write("\n   ");
                    ostr.Write("0x" + toSpecial[i].ToString("X") + "L, ");
                }
                ostr.WriteLine("\n};");
            }

            if (hasMore) {
                // Bit vector for MORE
                ostr.Write("static readonly long[] ccToMore = {");
                for (i = 0; i < maxOrdinal/64 + 1; i++) {
                    if (i%4 == 0)
                        ostr.Write("\n   ");
                    ostr.Write("0x" + toMore[i].ToString("X") + "L, ");
                }
                ostr.WriteLine("\n};");
            }

            if (Options.getUserCharStream())
                charStreamName = "ICharStream";
            else {
                if (Options.getUnicodeEscape())
                    charStreamName = "CharStream";
                else
                    charStreamName = "SimpleCharStream";
            }

            ostr.WriteLine("internal " + staticString + " " + charStreamName + " inputStream;");

            ostr.WriteLine("private " + staticString + "readonly int[] ccRounds = new int[" + stateSetSize + "];");
            ostr.WriteLine("private " + staticString + "readonly int[] ccStateSet = new int[" + (2*stateSetSize) + "];");

            if (hasMoreActions || hasSkipActions || hasTokenActions) {
                ostr.WriteLine("private " + staticString + "readonly " + Options.stringBufOrBuild() + " ccImage = new " +
                               Options.stringBufOrBuild() + "();");
                ostr.WriteLine("private " + staticString + Options.stringBufOrBuild() + " image = ccImage;");
                ostr.WriteLine("private " + staticString + "int ccImageLen;");
                ostr.WriteLine("private " + staticString + "int lengthOfMatch;");
            }

            ostr.WriteLine(staticString + "protected char curChar;");

            if (Options.getTokenManagerUsesParser() && !Options.getStatic()) {
                ostr.WriteLine("");
                ostr.WriteLine("// Constructor with parser.");
                ostr.WriteLine("public " + tokMgrClassName + "(" + CSharpCCGlobals.cu_name + " parser, " + charStreamName + " stream){");
                ostr.WriteLine("   this.parser = parser;");
            } else {
                ostr.WriteLine("// Constructor.");
                ostr.WriteLine("public " + tokMgrClassName + "(" + charStreamName + " stream){");
            }

            if (Options.getStatic() && !Options.getUserCharStream()) {
                ostr.WriteLine("   if (inputStream != null)");
                ostr.WriteLine("      throw new TokenManagerError(\"ERROR: Second call to constructor of static lexer. " +
                               "You must use ReInit() to initialize the static variables.\", TokenManagerError.STATIC_LEXER_ERROR);");
            } else if (!Options.getUserCharStream()) {
                if (Options.getUnicodeEscape())
                    ostr.WriteLine("   if (CharStream.staticFlag)");
                else
                    ostr.WriteLine("   if (SimpleCharStream.staticFlag)");

                ostr.WriteLine("      throw new Error(\"ERROR: Cannot use a static ICharStream class with a " +
                               "non-static lexical analyzer.\");");
            }

            ostr.WriteLine("   inputStream = stream;");

            ostr.WriteLine("}");

            if (Options.getTokenManagerUsesParser() && !Options.getStatic()) {
                ostr.WriteLine("");
                ostr.WriteLine("// Constructor with parser.");
                ostr.WriteLine("public " + tokMgrClassName + "(" + CSharpCCGlobals.cu_name + " parser, " +
                               charStreamName + " stream, int lexState)");
                ostr.WriteLine("   : this(parser, stream) {");
            } else {
                ostr.WriteLine("");
                ostr.WriteLine("// Constructor.");
                ostr.WriteLine("public " + tokMgrClassName + "(" + charStreamName + " stream, int lexState)");
                ostr.WriteLine("   : this(stream) {");
            }
            ostr.WriteLine("   SwitchTo(lexState);");
            ostr.WriteLine("}");

            // Reinit method for reinitializing the parser (for static parsers).
            ostr.WriteLine("");
            ostr.WriteLine("/** Reinitialise parser. */");
            ostr.WriteLine("public " + staticString + "void ReInit(" + charStreamName + " stream)");
            ostr.WriteLine("{");
            ostr.WriteLine("   ccMatchedPos = ccNewStateCnt = 0;");
            ostr.WriteLine("   curLexState = defaultLexState;");
            ostr.WriteLine("   inputStream = stream;");
            ostr.WriteLine("   ReInitRounds();");
            ostr.WriteLine("}");

            // Method to reinitialize the jjrounds array.
            ostr.WriteLine("private " + staticString + "void ReInitRounds()");
            ostr.WriteLine("{");
            ostr.WriteLine("   int i;");
            ostr.WriteLine("   ccRound = 0x" + (Int32.MinValue + 1).ToString("X") + ";");
            ostr.WriteLine("   for (i = " + stateSetSize + "; i-- > 0;)");
            ostr.WriteLine("      ccRounds[i] = 0x" + Int32.MinValue.ToString("X") + ";");
            ostr.WriteLine("}");

            // Reinit method for reinitializing the parser (for static parsers).
            ostr.WriteLine("");
            ostr.WriteLine("// Reinitialise parser.");
            ostr.WriteLine("public " + staticString + "void ReInit(" + charStreamName + " stream, int lexState)");
            ostr.WriteLine("{");
            ostr.WriteLine("   ReInit(stream);");
            ostr.WriteLine("   SwitchTo(lexState);");
            ostr.WriteLine("}");

            ostr.WriteLine("");
            ostr.WriteLine("// Switch to specified lex state.");
            ostr.WriteLine("public " + staticString + "void SwitchTo(int lexState)");
            ostr.WriteLine("{");
            ostr.WriteLine("   if (lexState >= " + lexStateName.Length + " || lexState < 0)");
            ostr.WriteLine("      throw new TokenManagerError(\"Error: Ignoring invalid lexical state : \"" +
                           " + lexState + \". State unchanged.\", TokenManagerError.INVALID_LEXICAL_STATE);");
            ostr.WriteLine("   else");
            ostr.WriteLine("      curLexState = lexState;");
            ostr.WriteLine("}");

            ostr.WriteLine("");
        }

        private static void DumpFillToken() {
            double tokenVersion = CSharpFiles.GetVersion("Token.cs");
            bool hasBinaryNewToken = tokenVersion > 4.09;

            ostr.WriteLine("internal " + staticString + " Token ccFillToken()");
            ostr.WriteLine("{");
            ostr.WriteLine("   Token t;");
            ostr.WriteLine("   string curTokenImage;");
            if (keepLineCol) {
                ostr.WriteLine("   int beginLine;");
                ostr.WriteLine("   int endLine;");
                ostr.WriteLine("   int beginColumn;");
                ostr.WriteLine("   int endColumn;");
            }

            if (hasEmptyMatch) {
                ostr.WriteLine("   if (ccMatchedPos < 0)");
                ostr.WriteLine("   {");
                ostr.WriteLine("      if (image == null)");
                ostr.WriteLine("         curTokenImage = \"\";");
                ostr.WriteLine("      else");
                ostr.WriteLine("         curTokenImage = image.ToString();");

                if (keepLineCol) {
                    ostr.WriteLine("      beginLine = endLine = inputStream.BeginLine;");
                    ostr.WriteLine("      beginColumn = endColumn = inputStream.BeginColumn;");
                }

                ostr.WriteLine("   }");
                ostr.WriteLine("   else");
                ostr.WriteLine("   {");
                ostr.WriteLine("      string im = ccStrLiteralImages[ccMatchedKind];");
                ostr.WriteLine("      curTokenImage = (im == null) ? inputStream.GetImage() : im;");

                if (keepLineCol) {
                    ostr.WriteLine("      beginLine = inputStream.BeginLine;");
                    ostr.WriteLine("      beginColumn = inputStream.BeginColumn;");
                    ostr.WriteLine("      endLine = inputStream.EndLine;");
                    ostr.WriteLine("      endColumn = inputStream.EndColumn;");
                }

                ostr.WriteLine("   }");
            } else {
                ostr.WriteLine("   string im = ccStrLiteralImages[ccMatchedKind];");
                ostr.WriteLine("   curTokenImage = (im == null) ? inputStream.GetImage() : im;");
                if (keepLineCol) {
                    ostr.WriteLine("   beginLine = inputStream.BeginLine;");
                    ostr.WriteLine("   beginColumn = inputStream.BeginColumn;");
                    ostr.WriteLine("   endLine = inputStream.EndLine;");
                    ostr.WriteLine("   endColumn = inputStream.EndColumn;");
                }
            }

            if (Options.getTokenFactory().Length > 0) {
                ostr.WriteLine("   t = " + Options.getTokenFactory() + ".NewToken(ccMatchedKind, curTokenImage);");
            } else if (hasBinaryNewToken) {
                ostr.WriteLine("   t = Token.NewToken(ccMatchedKind, curTokenImage);");
            } else {
                ostr.WriteLine("   t = Token.NewToken(ccMatchedKind);");
                ostr.WriteLine("   t.Kind = ccMatchedKind;");
                ostr.WriteLine("   t.Image = curTokenImage;");
            }

            if (keepLineCol) {
                ostr.WriteLine("");
                ostr.WriteLine("   t.BeginLine = beginLine;");
                ostr.WriteLine("   t.EndLine = endLine;");
                ostr.WriteLine("   t.BeginColumn = beginColumn;");
                ostr.WriteLine("   t.EndColumn = endColumn;");
            }

            ostr.WriteLine("");
            ostr.WriteLine("   return t;");
            ostr.WriteLine("}");
        }

        private static void DumpGetNextToken() {
            int i;

            ostr.WriteLine("");
            ostr.WriteLine(staticString + "int curLexState = " + defaultLexState + ";");
            ostr.WriteLine(staticString + "int defaultLexState = " + defaultLexState + ";");
            ostr.WriteLine(staticString + "int ccNewStateCnt;");
            ostr.WriteLine(staticString + "int ccRound;");
            ostr.WriteLine(staticString + "int ccMatchedPos;");
            ostr.WriteLine(staticString + "int ccMatchedKind;");
            ostr.WriteLine("");
            ostr.WriteLine("// Get the next Token.");
            ostr.WriteLine("public " + staticString + "Token GetNextToken() ");
            ostr.WriteLine("{");
            if (hasSpecial) {
                ostr.WriteLine("  Token specialToken = null;");
            }
            ostr.WriteLine("  Token matchedToken;");
            ostr.WriteLine("  int curPos = 0;");
            ostr.WriteLine("");
            // OLD: ostr.WriteLine("  EOFLoop :\n  for (;;)");
            ostr.WriteLine("  for (;;)");
            ostr.WriteLine("  {");
            ostr.WriteLine("   try");
            ostr.WriteLine("   {");
            ostr.WriteLine("      curChar = inputStream.BeginToken();");
            ostr.WriteLine("   }");
            ostr.WriteLine("   catch(System.IO.IOException e)");
            ostr.WriteLine("   {");

            if (Options.getDebugTokenManager())
                ostr.WriteLine("      debugStream.WriteLine(\"Returning the <EOF> token.\");");

            ostr.WriteLine("      ccMatchedKind = 0;");
            ostr.WriteLine("      matchedToken = ccFillToken();");

            if (hasSpecial)
                ostr.WriteLine("      matchedToken.SpecialToken = specialToken;");

            if (CSharpCCGlobals.nextStateForEof != null ||
                CSharpCCGlobals.actForEof != null)
                ostr.WriteLine("      TokenLexicalActions(matchedToken);");

            if (Options.getCommonTokenAction())
                ostr.WriteLine("      CommonTokenAction(matchedToken);");

            ostr.WriteLine("      return matchedToken;");
            ostr.WriteLine("   }");

            if (hasMoreActions || hasSkipActions || hasTokenActions) {
                ostr.WriteLine("   image = ccImage;");
                ostr.WriteLine("   image.Length = 0;");
                ostr.WriteLine("   ccImageLen = 0;");
            }

            ostr.WriteLine("");

            String prefix = "";
            if (hasMore) {
                ostr.WriteLine("   for (;;)");
                ostr.WriteLine("   {");
                prefix = "  ";
            }

            String endSwitch = "";
            String caseStr = "";
            // this also sets up the start state of the nfa
            if (maxLexStates > 1) {
                ostr.WriteLine(prefix + "   switch(curLexState)");
                ostr.WriteLine(prefix + "   {");
                endSwitch = prefix + "   }";
                caseStr = prefix + "     case ";
                prefix += "    ";
            }

            prefix += "   ";
            for (i = 0; i < maxLexStates; i++) {
                if (maxLexStates > 1)
                    ostr.WriteLine(caseStr + i + ":");

                if (singlesToSkip[i].HasTransitions()) {
                    // added the backup(0) to make JIT happy
                    ostr.WriteLine(prefix + "try { inputStream.Backup(0);");
                    if (singlesToSkip[i].asciiMoves[0] != 0L &&
                        singlesToSkip[i].asciiMoves[1] != 0L) {
                        ostr.WriteLine(prefix + "   while ((curChar < 64" + " && (0x" + (singlesToSkip[i].asciiMoves[0]).ToString("X") +
                                       "L & (1L << curChar)) != 0L) || \n" + prefix + "          (curChar >> 6) == 1" + " && (0x" +
                                       (singlesToSkip[i].asciiMoves[1]).ToString("X") + "L & (1L << (curChar & 077))) != 0L)");
                    } else if (singlesToSkip[i].asciiMoves[1] == 0L) {
                        ostr.WriteLine(prefix + "   while (curChar <= " +
                                       (int) MaxChar(singlesToSkip[i].asciiMoves[0]) + " && (0x" +
                                       (singlesToSkip[i].asciiMoves[0]).ToString("X") + "L & (1L << curChar)) != 0L)");
                    } else if (singlesToSkip[i].asciiMoves[0] == 0L) {
                        ostr.WriteLine(prefix + "   while (curChar > 63 && curChar <= " +
                                       ((int) MaxChar(singlesToSkip[i].asciiMoves[1]) + 64) +
                                       " && (0x" +
                                       (singlesToSkip[i].asciiMoves[1]).ToString("X") +
                                       "L & (1L << (curChar & 077))) != 0L)");
                    }

                    if (Options.getDebugTokenManager()) {
                        ostr.WriteLine(prefix + "{");
                        ostr.WriteLine("      debugStream.WriteLine(" +
                                       (maxLexStates > 1
                                           ? "\"<\" + lexStateNames[curLexState] + \">\" + "
                                           : "") +
                                       "\"Skipping character : \" + " +
                                       "TokenManagerError.AddEscapes(curChar.ToString()) + \" (\" + (int)curChar + \")\");");
                    }
                    ostr.WriteLine(prefix + "      curChar = inputStream.BeginToken();");

                    if (Options.getDebugTokenManager())
                        ostr.WriteLine(prefix + "}");

                    ostr.WriteLine(prefix + "}");
                    ostr.WriteLine(prefix + "catch (System.IO.IOException e1) { goto EOFLoop; }");
                }

                if (initMatch[i] != Int32.MaxValue && initMatch[i] != 0) {
                    if (Options.getDebugTokenManager())
                        ostr.WriteLine("      debugStream.WriteLine(\"   Matched the empty string as \" + tokenImage[" + initMatch[i] +
                                       "] + \" token.\");");

                    ostr.WriteLine(prefix + "ccMatchedKind = " + initMatch[i] + ";");
                    ostr.WriteLine(prefix + "ccMatchedPos = -1;");
                    ostr.WriteLine(prefix + "curPos = 0;");
                } else {
                    ostr.WriteLine(prefix + "ccMatchedKind = 0x" + Int32.MaxValue.ToString("X") + ";");
                    ostr.WriteLine(prefix + "ccMatchedPos = 0;");
                }

                if (Options.getDebugTokenManager())
                    ostr.WriteLine("      debugStream.WriteLine(" +
                                   (maxLexStates > 1 ? "\"<\" + lexStateNames[curLexState] + \">\" + " : "") +
                                   "\"Current character : \" + " +
                                   "TokenManagerError.AddEscapes(curChar.ToString()) + \" (\" + (int)curChar + \") " +
                                   "at line \" + inputStream.EndLine + \" column \" + inputStream.EndColumn);");

                ostr.WriteLine(prefix + "curPos = ccMoveStringLiteralDfa0_" + i + "();");

                if (canMatchAnyChar[i] != -1) {
                    if (initMatch[i] != Int32.MaxValue && initMatch[i] != 0)
                        ostr.WriteLine(prefix + "if (ccMatchedPos < 0 || (ccMatchedPos == 0 && ccMatchedKind > " + canMatchAnyChar[i] + "))");
                    else
                        ostr.WriteLine(prefix + "if (ccMatchedPos == 0 && ccMatchedKind > " + canMatchAnyChar[i] + ")");
                    ostr.WriteLine(prefix + "{");

                    if (Options.getDebugTokenManager())
                        ostr.WriteLine("           debugStream.WriteLine(\"   Current character matched as a \" + tokenImage[" +
                                       canMatchAnyChar[i] + "] + \" token.\");");
                    ostr.WriteLine(prefix + "   ccMatchedKind = " + canMatchAnyChar[i] + ";");

                    if (initMatch[i] != Int32.MaxValue && initMatch[i] != 0)
                        ostr.WriteLine(prefix + "   ccMatchedPos = 0;");

                    ostr.WriteLine(prefix + "}");
                }

                if (maxLexStates > 1)
                    ostr.WriteLine(prefix + "break;");
            }

            if (maxLexStates > 1)
                ostr.WriteLine(endSwitch);
            else if (maxLexStates == 0)
                ostr.WriteLine("       ccMatchedKind = 0x" + Int32.MaxValue.ToString("X") + ";");

            if (maxLexStates > 1)
                prefix = "  ";
            else
                prefix = "";

            if (maxLexStates > 0) {
                ostr.WriteLine(prefix + "   if (ccMatchedKind != 0x" + Int32.MaxValue.ToString("X") + ")");
                ostr.WriteLine(prefix + "   {");
                ostr.WriteLine(prefix + "      if (ccMatchedPos + 1 < curPos)");

                if (Options.getDebugTokenManager()) {
                    ostr.WriteLine(prefix + "      {");
                    ostr.WriteLine(prefix + "         debugStream.WriteLine(" +
                                   "\"   Putting back \" + (curPos - ccMatchedPos - 1) + \" characters into the input stream.\");");
                }

                ostr.WriteLine(prefix + "         inputStream.Backup(curPos - ccMatchedPos - 1);");

                if (Options.getDebugTokenManager())
                    ostr.WriteLine(prefix + "      }");

                if (Options.getDebugTokenManager()) {
                    if (Options.getUnicodeEscape() ||
                        Options.getUserCharStream())
                        ostr.WriteLine("    debugStream.WriteLine(" +
                                       "\"****** FOUND A \" + tokenImage[ccMatchedKind] + \" MATCH " +
                                       "(\" + TokenManagerError.AddEscapes(new String(inputStream.GetSuffix(ccMatchedPos + 1))) + " +
                                       "\") ******\\n\");");
                    else
                        ostr.WriteLine("    debugStream.WriteLine(" +
                                       "\"****** FOUND A \" + tokenImage[ccMatchedKind] + \" MATCH " +
                                       "(\" + TokenManagerError.AddEscapes(new String(inputStream.GetSuffix(ccMatchedPos + 1))) + " +
                                       "\") ******\\n\");");
                }

                if (hasSkip || hasMore || hasSpecial) {
                    ostr.WriteLine(prefix + "      if ((ccToToken[ccMatchedKind >> 6] & " + "(1L << (ccMatchedKind & 077))) != 0L)");
                    ostr.WriteLine(prefix + "      {");
                }

                ostr.WriteLine(prefix + "         matchedToken = ccFillToken();");

                if (hasSpecial)
                    ostr.WriteLine(prefix + "         matchedToken.SpecialToken = specialToken;");

                if (hasTokenActions)
                    ostr.WriteLine(prefix + "         TokenLexicalActions(matchedToken);");

                if (maxLexStates > 1) {
                    ostr.WriteLine("       if (ccNewLexState[ccMatchedKind] != -1)");
                    ostr.WriteLine(prefix + "       curLexState = ccNewLexState[ccMatchedKind];");
                }

                if (Options.getCommonTokenAction())
                    ostr.WriteLine(prefix + "         CommonTokenAction(matchedToken);");

                ostr.WriteLine(prefix + "         return matchedToken;");

                if (hasSkip || hasMore || hasSpecial) {
                    ostr.WriteLine(prefix + "      }");

                    if (hasSkip || hasSpecial) {
                        if (hasMore) {
                            ostr.WriteLine(prefix + "      else if ((ccToSkip[ccMatchedKind >> 6] & " +
                                           "(1L << (ccMatchedKind & 077))) != 0L)");
                        } else
                            ostr.WriteLine(prefix + "      else");

                        ostr.WriteLine(prefix + "      {");

                        if (hasSpecial) {
                            ostr.WriteLine(prefix + "         if ((ccToSpecial[ccMatchedKind >> 6] & " +
                                           "(1L << (ccMatchedKind & 077))) != 0L)");
                            ostr.WriteLine(prefix + "         {");

                            ostr.WriteLine(prefix + "            matchedToken = ccFillToken();");

                            ostr.WriteLine(prefix + "            if (specialToken == null)");
                            ostr.WriteLine(prefix + "               specialToken = matchedToken;");
                            ostr.WriteLine(prefix + "            else");
                            ostr.WriteLine(prefix + "            {");
                            ostr.WriteLine(prefix + "               matchedToken.SpecialToken = specialToken;");
                            ostr.WriteLine(prefix + "               specialToken = (specialToken.Next = matchedToken);");
                            ostr.WriteLine(prefix + "            }");

                            if (hasSkipActions)
                                ostr.WriteLine(prefix + "            SkipLexicalActions(matchedToken);");

                            ostr.WriteLine(prefix + "         }");

                            if (hasSkipActions) {
                                ostr.WriteLine(prefix + "         else");
                                ostr.WriteLine(prefix + "            SkipLexicalActions(null);");
                            }
                        } else if (hasSkipActions)
                            ostr.WriteLine(prefix + "         SkipLexicalActions(null);");

                        if (maxLexStates > 1) {
                            ostr.WriteLine("         if (ccNewLexState[ccMatchedKind] != -1)");
                            ostr.WriteLine(prefix + "         curLexState = ccNewLexState[ccMatchedKind];");
                        }

                        ostr.WriteLine(prefix + "         goto EOFLoop;");
                        ostr.WriteLine(prefix + "      }");
                    }

                    if (hasMore) {
                        if (hasMoreActions)
                            ostr.WriteLine(prefix + "      MoreLexicalActions();");
                        else if (hasSkipActions || hasTokenActions)
                            ostr.WriteLine(prefix + "      ccImageLen += ccMatchedPos + 1;");

                        if (maxLexStates > 1) {
                            ostr.WriteLine("      if (ccNewLexState[ccMatchedKind] != -1)");
                            ostr.WriteLine(prefix + "      curLexState = ccNewLexState[ccMatchedKind];");
                        }
                        ostr.WriteLine(prefix + "      curPos = 0;");
                        ostr.WriteLine(prefix + "      ccMatchedKind = 0x" + Int32.MaxValue.ToString("X") + ";");

                        ostr.WriteLine(prefix + "      try {");
                        ostr.WriteLine(prefix + "         curChar = inputStream.ReadChar();");

                        if (Options.getDebugTokenManager())
                            ostr.WriteLine("   debugStream.WriteLine(" +
                                           (maxLexStates > 1 ? "\"<\" + lexStateNames[curLexState] + \">\" + " : "") +
                                           "\"Current character : \" + " +
                                           "TokenManagerError.AddEscapes(curChar.ToString()) + \" (\" + (int)curChar + \") " +
                                           "at line \" + inputStream.EndLine + \" column \" + inputStream.EndColumn);");
                        ostr.WriteLine(prefix + "         continue;");
                        ostr.WriteLine(prefix + "      }");
                        ostr.WriteLine(prefix + "      catch (System.IO.IOException) { }");
                    }
                }

                ostr.WriteLine(prefix + "   }");
                ostr.WriteLine(prefix + "   int errorLine = inputStream.EndLine;");
                ostr.WriteLine(prefix + "   int errorColumn = inputStream.EndColumn;");
                ostr.WriteLine(prefix + "   string errorAfter = null;");
                ostr.WriteLine(prefix + "   bool EOFSeen = false;");
                ostr.WriteLine(prefix + "   try { inputStream.ReadChar(); inputStream.Backup(1); }");
                ostr.WriteLine(prefix + "   catch (System.IO.IOException) {");
                ostr.WriteLine(prefix + "      EOFSeen = true;");
                ostr.WriteLine(prefix + "      errorAfter = curPos <= 1 ? \"\" : inputStream.GetImage();");
                ostr.WriteLine(prefix + "      if (curChar == '\\n' || curChar == '\\r') {");
                ostr.WriteLine(prefix + "         errorLine++;");
                ostr.WriteLine(prefix + "         errorColumn = 0;");
                ostr.WriteLine(prefix + "      }");
                ostr.WriteLine(prefix + "      else");
                ostr.WriteLine(prefix + "         errorColumn++;");
                ostr.WriteLine(prefix + "   }");
                ostr.WriteLine(prefix + "   if (!EOFSeen) {");
                ostr.WriteLine(prefix + "      inputStream.Backup(1);");
                ostr.WriteLine(prefix + "      errorAfter = curPos <= 1 ? \"\" : inputStream.GetImage();");
                ostr.WriteLine(prefix + "   }");
                ostr.WriteLine(prefix +
                               "   throw new TokenManagerError(EOFSeen, curLexState, errorLine, errorColumn, errorAfter, curChar, TokenManagerError.LEXICAL_ERROR);");
            }

            if (hasMore)
                ostr.WriteLine(prefix + " }");

            //TODO: Check this is the right position for the EOFLoop label!!!
            ostr.WriteLine("  EOFLoop :;");

            ostr.WriteLine("  }");
            ostr.WriteLine("}");
            ostr.WriteLine("");
        }

        public static void DumpSkipActions() {
            Action act;

            ostr.WriteLine(staticString + "void SkipLexicalActions(Token matchedToken)");
            ostr.WriteLine("{");
            ostr.WriteLine("   switch(ccMatchedKind)");
            ostr.WriteLine("   {");

            for (int i = 0; i < maxOrdinal; i++) {
                if ((toSkip[i/64] & (1L << (i%64))) == 0L)
                    continue;

                for (;;) {
                    if (((act = actions[i]) == null ||
                         act.ActionTokens == null ||
                         act.ActionTokens.Count == 0) && !canLoop[lexStates[i]])
                        goto Outer;

                    ostr.WriteLine("      case " + i + " :");

                    if (initMatch[lexStates[i]] == i && canLoop[lexStates[i]]) {
                        ostr.WriteLine("         if (ccMatchedPos == -1)");
                        ostr.WriteLine("         {");
                        ostr.WriteLine("            if (ccBeenHere[" + lexStates[i] + "] &&");
                        ostr.WriteLine("                ccEmptyLineNo[" + lexStates[i] + "] == inputStream.BeginLine &&");
                        ostr.WriteLine("                ccEmptyColNo[" + lexStates[i] + "] == inputStream.BeginColumn)");
                        ostr.WriteLine("               throw new TokenManagerError(" +
                                       "(\"Error: Bailing out of infinite loop caused by repeated empty string matches " +
                                       "at line \" + input_stream.BeginLine + \", " +
                                       "column \" + input_stream.BeginColumn + \".\"), TokenManagerError.LOOP_DETECTED);");
                        ostr.WriteLine("            ccEmptyLineNo[" + lexStates[i] + "] = inpuStream.BeginLine;");
                        ostr.WriteLine("            ccEmptyColNo[" + lexStates[i] + "] = inputStream.BeginColumn;");
                        ostr.WriteLine("            ccBeenHere[" + lexStates[i] + "] = true;");
                        ostr.WriteLine("         }");
                    }

                    if ((act = actions[i]) == null ||
                        act.ActionTokens.Count == 0)
                        break;

                    ostr.Write("         image.Append");
                    if (RStringLiteral.allImages[i] != null) {
                        ostr.WriteLine("(ccStrLiteralImages[" + i + "]);");
                        ostr.WriteLine("        lengthOfMatch = ccStrLiteralImages[" + i + "].Length;");
                    } else {
                        ostr.WriteLine("(inputStream.GetSuffix(ccImageLen + (lengthOfMatch = ccMatchedPos + 1)));");
                    }

                    CSharpCCGlobals.PrintTokenSetup(act.ActionTokens[0]);
                    CSharpCCGlobals.ccol = 1;

                    for (int j = 0; j < act.ActionTokens.Count; j++)
                        CSharpCCGlobals.PrintToken(act.ActionTokens[j], ostr);
                    ostr.WriteLine("");

                    break;
                }

                ostr.WriteLine("         break;");
            }
            Outer:
            ;

            ostr.WriteLine("      default :");
            ostr.WriteLine("         break;");
            ostr.WriteLine("   }");
            ostr.WriteLine("}");
        }

        public static void DumpMoreActions() {
            Action act;

            ostr.WriteLine(staticString + "void MoreLexicalActions()");
            ostr.WriteLine("{");
            ostr.WriteLine("   jjimageLen += (lengthOfMatch = ccMatchedPos + 1);");
            ostr.WriteLine("   switch(ccMatchedKind)");
            ostr.WriteLine("   {");


            for (int i = 0; i < maxOrdinal; i++) {
                if ((toMore[i/64] & (1L << (i%64))) == 0L)
                    continue;

                for (;;) {
                    if (((act = actions[i]) == null ||
                         act.ActionTokens == null ||
                         act.ActionTokens.Count == 0) && !canLoop[lexStates[i]])
                        goto Outer;

                    ostr.WriteLine("      case " + i + " :");

                    if (initMatch[lexStates[i]] == i && canLoop[lexStates[i]]) {
                        ostr.WriteLine("         if (ccMatchedPos == -1)");
                        ostr.WriteLine("         {");
                        ostr.WriteLine("            if (ccBeenHere[" + lexStates[i] + "] &&");
                        ostr.WriteLine("                ccEmptyLineNo[" + lexStates[i] + "] == inputStream.BeginLine &&");
                        ostr.WriteLine("                ccEmptyColNo[" + lexStates[i] + "] == input_stream.BeginColumn)");
                        ostr.WriteLine("               throw new TokenManagerError(" +
                                       "(\"Error: Bailing out of infinite loop caused by repeated empty string matches " +
                                       "at line \" + inputStream.BeginLine + \", " +
                                       "column \" + inputStream.BeginColumn + \".\"), TokenManagerError.LOOP_DETECTED);");
                        ostr.WriteLine("            ccEmptyLineNo[" + lexStates[i] + "] = inputStream.BeginLine;");
                        ostr.WriteLine("            ccEmptyColNo[" + lexStates[i] + "] = inputStream.BeginColumn;");
                        ostr.WriteLine("            ccBeenHere[" + lexStates[i] + "] = true;");
                        ostr.WriteLine("         }");
                    }

                    if ((act = actions[i]) == null ||
                        act.ActionTokens.Count == 0) {
                        break;
                    }

                    ostr.Write("         image.Append");

                    if (RStringLiteral.allImages[i] != null)
                        ostr.WriteLine("(ccStrLiteralImages[" + i + "]);");
                    else
                        ostr.WriteLine("(inputStream.GetSuffix(ccImageLen));");

                    ostr.WriteLine("         ccImageLen = 0;");
                    CSharpCCGlobals.PrintTokenSetup(act.ActionTokens[0]);
                    CSharpCCGlobals.ccol = 1;

                    for (int j = 0; j < act.ActionTokens.Count; j++)
                        CSharpCCGlobals.PrintToken(act.ActionTokens[j], ostr);
                    ostr.WriteLine("");

                    break;
                }

                ostr.WriteLine("         break;");
            }
            Outer:
            ;

            ostr.WriteLine("      default :");
            ostr.WriteLine("         break;");

            ostr.WriteLine("   }");
            ostr.WriteLine("}");
        }

        public static void DumpTokenActions() {
            Action act;
            int i;

            ostr.WriteLine(staticString + "void TokenLexicalActions(Token matchedToken)");
            ostr.WriteLine("{");
            ostr.WriteLine("   switch(ccMatchedKind)");
            ostr.WriteLine("   {");


            for (i = 0; i < maxOrdinal; i++) {
                if ((toToken[i/64] & (1L << (i%64))) == 0L)
                    continue;

                for (;;) {
                    if (((act = actions[i]) == null ||
                         act.ActionTokens == null ||
                         act.ActionTokens.Count == 0) && !canLoop[lexStates[i]])
                        goto Outer;

                    ostr.WriteLine("      case " + i + " :");

                    if (initMatch[lexStates[i]] == i && canLoop[lexStates[i]]) {
                        ostr.WriteLine("         if (ccMatchedPos == -1)");
                        ostr.WriteLine("         {");
                        ostr.WriteLine("            if (ccBeenHere[" + lexStates[i] + "] &&");
                        ostr.WriteLine("                ccEmptyLineNo[" + lexStates[i] + "] == inputStream.BeginLine &&");
                        ostr.WriteLine("                ccEmptyColNo[" + lexStates[i] + "] == inputStream.BeginColumn)");
                        ostr.WriteLine("               throw new TokenManagerError(" +
                                       "(\"Error: Bailing out of infinite loop caused by repeated empty string matches " +
                                       "at line \" + inputStream.BeginLine + \", " +
                                       "column \" + inputStream.BeginColumn + \".\"), TokenManagerError.LOOP_DETECTED);");
                        ostr.WriteLine("            ccEmptyLineNo[" + lexStates[i] + "] = inputStream.BeginLine;");
                        ostr.WriteLine("            ccEmptyColNo[" + lexStates[i] + "] = inputStream.BeginColumn;");
                        ostr.WriteLine("            ccBeenHere[" + lexStates[i] + "] = true;");
                        ostr.WriteLine("         }");
                    }

                    if ((act = (Action) actions[i]) == null ||
                        act.ActionTokens.Count == 0)
                        break;

                    if (i == 0) {
                        ostr.WriteLine("      image.Length = 0;"); // For EOF no image is there
                    } else {
                        ostr.Write("        image.Append");

                        if (RStringLiteral.allImages[i] != null) {
                            ostr.WriteLine("(ccStrLiteralImages[" + i + "]);");
                            ostr.WriteLine("        lengthOfMatch = ccStrLiteralImages[" + i + "].Length;");
                        } else {
                            ostr.WriteLine("(inputStream.GetSuffix(ccImageLen + (lengthOfMatch = ccMatchedPos + 1)));");
                        }
                    }

                    CSharpCCGlobals.PrintTokenSetup(act.ActionTokens[0]);
                    CSharpCCGlobals.ccol = 1;

                    for (int j = 0; j < act.ActionTokens.Count; j++)
                        CSharpCCGlobals.PrintToken(act.ActionTokens[j], ostr);
                    ostr.WriteLine("");

                    break;
                }

                ostr.WriteLine("         break;");
            }

            Outer:
            ;

            ostr.WriteLine("      default :");
            ostr.WriteLine("         break;");
            ostr.WriteLine("   }");
            ostr.WriteLine("}");
        }

        public static void reInit() {
            ostr = null;
            staticString = null;
            tokMgrClassName = null;
            allTpsForState = new Dictionary<string, IList<TokenProduction>>();
            lexStateIndex = 0;
            kinds = null;
            maxOrdinal = 1;
            lexStateSuffix = null;
            newLexState = null;
            lexStates = null;
            ignoreCase = null;
            actions = null;
            initStates = new Dictionary<string, NfaState>();
            stateSetSize = 0;
            maxLexStates = 0;
            lexStateName = null;
            singlesToSkip = null;
            toSkip = null;
            toSpecial = null;
            toMore = null;
            toToken = null;
            defaultLexState = 0;
            rexprs = null;
            maxLongsReqd = null;
            initMatch = null;
            canMatchAnyChar = null;
            hasEmptyMatch = false;
            canLoop = null;
            stateHasActions = null;
            hasLoop = false;
            canReachOnMore = null;
            hasNfa = null;
            mixed = null;
            initialState = null;
            curKind = 0;
            hasSkipActions = false;
            hasMoreActions = false;
            hasTokenActions = false;
            hasSpecial = false;
            hasSkip = false;
            hasMore = false;
            curRE = null;
        }

    }
}