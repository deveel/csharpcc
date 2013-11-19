using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Deveel.CSharpCC.Parser {
    public static class ParseEngine {
        private static TextWriter ostr;
        private static int gensymindex = 0;
        private static int indentamt;
        private static bool cc2LA;

        private static IDictionary<Expansion, Phase3Data> phase3table = new Dictionary<Expansion, Phase3Data>();
        private static IList<Phase3Data> phase3list = new List<Phase3Data>();

        private static IDictionary<string, NormalProduction> productionTable = new Dictionary<string, NormalProduction>();

        private static bool CodeCheck(Expansion exp) {
            if (exp is RegularExpression)
                return false;
            if (exp is NonTerminal) {
                NormalProduction prod = ((NonTerminal) exp).Production;
                if (prod is CodeProduction)
                    return true;
                return CodeCheck(prod.Expansion);
            }
            if (exp is Choice) {
                Choice ch = (Choice) exp;
                foreach (var choice in ch.Choices) {
                    if (CodeCheck(choice)) {
                        return true;
                    }
                }
                return false;
            }
            if (exp is Sequence) {
                Sequence seq = (Sequence) exp;
                foreach (var unit in seq.Units) {
                    if (unit is Lookahead && ((Lookahead) unit).IsExplicit) {
                        // An explicit lookahead (rather than one generated implicitly). Assume
                        // the user knows what he / she is doing, e.g.
                        //    "A" ( "B" | LOOKAHEAD("X") jcode() | "C" )* "D"
                        return false;
                    }
                    if (CodeCheck((unit)))
                        return true;
                    if (!Semanticize.EmptyExpansionExists(unit))
                        return false;
                }
                return false;
            }
            if (exp is OneOrMore) {
                OneOrMore om = (OneOrMore) exp;
                return CodeCheck(om.Expansion);
            }
            if (exp is ZeroOrMore) {
                ZeroOrMore zm = (ZeroOrMore) exp;
                return CodeCheck(zm.Expansion);
            }
            if (exp is ZeroOrOne) {
                ZeroOrOne zo = (ZeroOrOne) exp;
                return CodeCheck(zo.Expansion);
            }
            if (exp is TryBlock) {
                TryBlock tb = (TryBlock) exp;
                return CodeCheck(tb.Expansion);
            }
            return false;
        }

        private static bool[] firstSet;
        private static IList<Lookahead> phase2list;

        /**
         * Sets up the array "firstSet" above based on the Expansion argument
         * passed to it.  Since this is a recursive function, it assumes that
         * "firstSet" has been reset before the first call.
         */

        private static void GenFirstSet(Expansion exp) {
            if (exp is RegularExpression) {
                firstSet[((RegularExpression) exp).Ordinal] = true;
            } else if (exp is NonTerminal) {
                if (!(((NonTerminal) exp).Production is CodeProduction)) {
                    GenFirstSet(((NonTerminal) exp).Production.Expansion);
                }
            } else if (exp is Choice) {
                Choice ch = (Choice) exp;
                foreach (var expansion in ch.Choices) {
                    GenFirstSet(expansion);
                }
            } else if (exp is Sequence) {
                Sequence seq = (Sequence) exp;
                Object obj = seq.Units[0];
                if ((obj is Lookahead) && (((Lookahead) obj).ActionTokens.Count != 0)) {
                    cc2LA = true;
                }
                for (int i = 0; i < seq.Units.Count; i++) {
                    Expansion unit = seq.Units[i];
                    // Javacode productions can not have FIRST sets. Instead we generate the FIRST set
                    // for the preceding LOOKAHEAD (the semantic checks should have made sure that
                    // the LOOKAHEAD is suitable).
                    if (unit is NonTerminal && ((NonTerminal) unit).Production is CodeProduction) {
                        if (i > 0 && seq.Units[i - 1] is Lookahead) {
                            Lookahead la = (Lookahead) seq.Units[i - 1];
                            GenFirstSet(la.Expansion);
                        }
                    } else {
                        GenFirstSet(seq.Units[i]);
                    }
                    if (!Semanticize.EmptyExpansionExists(seq.Units[i])) {
                        break;
                    }
                }
            } else if (exp is OneOrMore) {
                OneOrMore om = (OneOrMore) exp;
                GenFirstSet(om.Expansion);
            } else if (exp is ZeroOrMore) {
                ZeroOrMore zm = (ZeroOrMore) exp;
                GenFirstSet(zm.Expansion);
            } else if (exp is ZeroOrOne) {
                ZeroOrOne zo = (ZeroOrOne) exp;
                GenFirstSet(zo.Expansion);
            } else if (exp is TryBlock) {
                TryBlock tb = (TryBlock) exp;
                GenFirstSet(tb.Expansion);
            }
        }

        private const int NOOPENSTM = 0;
        private const int OPENIF = 1;
        private const int OPENSWITCH = 2;

        private static String buildLookaheadChecker(Lookahead[] conds, String[] actions) {

            // The state variables.
            int state = NOOPENSTM;
            int indentAmt = 0;
            bool[] casedValues = new bool[CSharpCCGlobals.tokenCount];
            String retval = "";
            Lookahead la;
            Token t = null;
            int tokenMaskSize = (CSharpCCGlobals.tokenCount - 1)/32 + 1;
            int[] tokenMask = null;

            // Iterate over all the conditions.
            int index = 0;
            while (index < conds.Length) {

                la = conds[index];
                cc2LA = false;

                if ((la.Amount == 0) ||
                    Semanticize.EmptyExpansionExists(la.Expansion) ||
                    CodeCheck(la.Expansion)) {

                    // This handles the following cases:
                    // . If syntactic lookahead is not wanted (and hence explicitly specified
                    //   as 0).
                    // . If it is possible for the lookahead expansion to recognize the empty
                    //   string - in which case the lookahead trivially passes.
                    // . If the lookahead expansion has a JAVACODE production that it directly
                    //   expands to - in which case the lookahead trivially passes.
                    if (la.ActionTokens.Count == 0) {
                        // In addition, if there is no semantic lookahead, then the
                        // lookahead trivially succeeds.  So break the main loop and
                        // treat this case as the default last action.
                        break;
                    } else {
                        // This case is when there is only semantic lookahead
                        // (without any preceding syntactic lookahead).  In this
                        // case, an "if" statement is generated.
                        switch (state) {
                            case NOOPENSTM:
                                retval += "\n" + "if (";
                                indentAmt++;
                                break;
                            case OPENIF:
                                retval += "\u0002\n" + "} else if (";
                                break;
                            case OPENSWITCH:
                                retval += "\u0002\n" + "default:" + "\u0001";
                                if (Options.getErrorReporting()) {
                                    retval += "\ncc_la1[" + CSharpCCGlobals.maskindex + "] = cc_gen;";
                                    CSharpCCGlobals.maskindex++;
                                }
                                CSharpCCGlobals.maskVals.Add(tokenMask);
                                retval += "\n" + "if (";
                                indentAmt++;
                                break;
                        }

                        CSharpCCGlobals.PrintTokenSetup(la.ActionTokens[0]);
                        foreach (var token in la.ActionTokens) {
                            t = token;
                            retval += CSharpCCGlobals.PrintToken(t);
                        }

                        retval += CSharpCCGlobals.PrintTrailingComments(t);
                        retval += ") {\u0001" + actions[index];
                        state = OPENIF;
                    }

                } else if (la.Amount == 1 && la.ActionTokens.Count == 0) {
                    // Special optimal processing when the lookahead is exactly 1, and there
                    // is no semantic lookahead.

                    if (firstSet == null) {
                        firstSet = new bool[CSharpCCGlobals.tokenCount];
                    }
                    for (int i = 0; i < CSharpCCGlobals.tokenCount; i++) {
                        firstSet[i] = false;
                    }
                    // cc2LA is set to false at the beginning of the containing "if" statement.
                    // It is checked immediately after the end of the same statement to determine
                    // if lookaheads are to be performed using calls to the cc2 methods.
                    GenFirstSet(la.Expansion);
                    // GenFirstSet may find that semantic attributes are appropriate for the next
                    // token.  In which case, it sets cc2LA to true.
                    if (!cc2LA) {

                        // This case is if there is no applicable semantic lookahead and the lookahead
                        // is one (excluding the earlier cases such as JAVACODE, etc.).
                        switch (state) {
                            case OPENIF:
                                retval += "\u0002\n" + "} else {\u0001";
                                // Control flows through to next case.
                                goto case NOOPENSTM;
                            case NOOPENSTM:
                                retval += "\n" + "switch (";
                                if (Options.getCacheTokens()) {
                                    retval += "cc_nt.Kind) {\u0001";
                                } else {
                                    retval += "(cc_ntk==-1)?cc_ntk():cc_ntk) {\u0001";
                                }
                                for (int i = 0; i < CSharpCCGlobals.tokenCount; i++) {
                                    casedValues[i] = false;
                                }
                                indentAmt++;
                                tokenMask = new int[tokenMaskSize];
                                for (int i = 0; i < tokenMaskSize; i++) {
                                    tokenMask[i] = 0;
                                }
                                // Don't need to do anything if state is OPENSWITCH.
                                break;
                        }
                        for (int i = 0; i < CSharpCCGlobals.tokenCount; i++) {
                            if (firstSet[i]) {
                                if (!casedValues[i]) {
                                    casedValues[i] = true;
                                    retval += "\u0002\ncase ";
                                    int j1 = i/32;
                                    int j2 = i%32;
                                    tokenMask[j1] |= 1 << j2;
                                    string s;
                                    if (!CSharpCCGlobals.names_of_tokens.TryGetValue(i, out s)) {
                                        retval += i;
                                    } else {
                                        retval += s;
                                    }
                                    retval += ":\u0001";
                                }
                            }
                        }
                        retval += actions[index];
                        retval += "\nbreak;";
                        state = OPENSWITCH;
                    }

                } else {
                    // This is the case when lookahead is determined through calls to
                    // jj2 methods.  The other case is when lookahead is 1, but semantic
                    // attributes need to be evaluated.  Hence this crazy control structure.

                    cc2LA = true;

                }

                if (cc2LA) {
                    // In this case lookahead is determined by the jj2 methods.

                    switch (state) {
                        case NOOPENSTM:
                            retval += "\n" + "if (";
                            indentAmt++;
                            break;
                        case OPENIF:
                            retval += "\u0002\n" + "} else if (";
                            break;
                        case OPENSWITCH:
                            retval += "\u0002\n" + "default:" + "\u0001";
                            if (Options.getErrorReporting()) {
                                retval += "\ncc_la1[" + CSharpCCGlobals.maskindex + "] = cc_gen;";
                                CSharpCCGlobals.maskindex++;
                            }
                            CSharpCCGlobals.maskVals.Add(tokenMask);
                            retval += "\n" + "if (";
                            indentAmt++;
                            break;
                    }
                    CSharpCCGlobals.cc2index++;
                    // At this point, la.la_expansion.InternalName must be "".
                    la.Expansion.InternalName = "_" + CSharpCCGlobals.cc2index;
                    phase2list.Add(la);
                    retval += "cc_2" + la.Expansion.InternalName + "(" + la.Amount + ")";
                    if (la.ActionTokens.Count != 0) {
                        // In addition, there is also a semantic lookahead.  So concatenate
                        // the semantic check with the syntactic one.
                        retval += " && (";
                        CSharpCCGlobals.PrintTokenSetup(la.ActionTokens[0]);
                        foreach (var token in la.ActionTokens) {
                            t = token;
                            retval += CSharpCCGlobals.PrintToken(t);
                        }

                        retval += CSharpCCGlobals.PrintTrailingComments(t);
                        retval += ")";
                    }

                    retval += ") {\u0001" + actions[index];
                    state = OPENIF;
                }

                index++;
            }

            // Generate code for the default case.  Note this may not
            // be the last entry of "actions" if any condition can be
            // statically determined to be always "true".

            switch (state) {
                case NOOPENSTM:
                    retval += actions[index];
                    break;
                case OPENIF:
                    retval += "\u0002\n" + "} else {\u0001" + actions[index];
                    break;
                case OPENSWITCH:
                    retval += "\u0002\n" + "default:" + "\u0001";
                    if (Options.getErrorReporting()) {
                        retval += "\ncc_la1[" + CSharpCCGlobals.maskindex + "] = cc_gen;";
                        CSharpCCGlobals.maskVals.Add(tokenMask);
                        CSharpCCGlobals.maskindex++;
                    }
                    retval += actions[index];
                    break;
            }

            for (int i = 0; i < indentAmt; i++) {
                retval += "\u0002\n}";
            }

            return retval;
        }

        internal static void dumpFormattedString(String str) {
            char ch = ' ';
            char prevChar;
            bool indentOn = true;
            for (int i = 0; i < str.Length; i++) {
                prevChar = ch;
                ch = str[i];
                if (ch == '\n' && prevChar == '\r') {
                    // do nothing - we've already printed a new line for the '\r'
                    // during the previous iteration.
                } else if (ch == '\n' || ch == '\r') {
                    if (indentOn) {
                        phase1NewLine();
                    } else {
                        ostr.WriteLine();
                    }
                } else if (ch == '\u0001') {
                    indentamt += 2;
                } else if (ch == '\u0002') {
                    indentamt -= 2;
                } else if (ch == '\u0003') {
                    indentOn = false;
                } else if (ch == '\u0004') {
                    indentOn = true;
                } else {
                    ostr.Write(ch);
                }
            }
        }

        private static void buildPhase1Routine(BnfProduction p) {
            Token t = p.ReturnTypeTokens[0];
            bool voidReturn = t.kind == CSharpCCParserConstants.VOID;

            CSharpCCGlobals.PrintTokenSetup(t);
            CSharpCCGlobals.ccol = 1;
            CSharpCCGlobals.PrintLeadingComments(t, ostr);
            ostr.Write("  " + CSharpCCGlobals.staticOpt() + " " + (p.AccessModifier ?? "public") + " ");
            CSharpCCGlobals.cline = t.beginLine;
            CSharpCCGlobals.ccol = t.beginColumn;
            CSharpCCGlobals.PrintTokenOnly(t, ostr);
            for (int i = 1; i < p.ReturnTypeTokens.Count; i++) {
                t = p.ReturnTypeTokens[i];
                CSharpCCGlobals.PrintToken(t, ostr);
            }
            CSharpCCGlobals.PrintTrailingComments(t, ostr);
            ostr.Write(" " + p.Lhs + "(");
            if (p.ParameterTokens.Count != 0) {
                CSharpCCGlobals.PrintTokenSetup(p.ParameterTokens[0]);
                foreach (var token in p.ParameterTokens) {
                    t = token;
                    CSharpCCGlobals.PrintToken(t, ostr);
                }
                CSharpCCGlobals.PrintTrailingComments(t, ostr);
            }
            ostr.Write(") {");
            indentamt = 4;
            if (Options.getDebugParser()) {
                ostr.WriteLine("");
                ostr.WriteLine("    trace_call(\"" + p.Lhs + "\");");
                ostr.Write("    try {");
                indentamt = 6;
            }
            if (p.DeclarationTokens.Count != 0) {
                CSharpCCGlobals.PrintTokenSetup(p.DeclarationTokens[0]);
                CSharpCCGlobals.cline--;
                foreach (var token in p.DeclarationTokens) {
                    t = token;
                    CSharpCCGlobals.PrintToken(t, ostr);
                }
                CSharpCCGlobals.PrintTrailingComments(t, ostr);
            }
            String code = phase1ExpansionGen(p.Expansion);
            dumpFormattedString(code);
            ostr.WriteLine("");
            if (p.IsJumpPatched && !voidReturn) {
                ostr.WriteLine("    throw new InvalidOperationException(\"Missing return statement in function\");");
            }
            if (Options.getDebugParser()) {
                ostr.WriteLine("    } finally {");
                ostr.WriteLine("      trace_return(\"" + p.Lhs + "\");");
                ostr.WriteLine("    }");
            }
            ostr.WriteLine("  }");
            ostr.WriteLine("");
        }

        private static void phase1NewLine() {
            ostr.WriteLine("");
            for (int i = 0; i < indentamt; i++) {
                ostr.Write(" ");
            }
        }

        private static String phase1ExpansionGen(Expansion e) {
            String retval = "";
            Token t = null;
            Lookahead[] conds;
            String[] actions;
            if (e is RegularExpression) {
                RegularExpression e_nrw = (RegularExpression) e;
                retval += "\n";
                if (e_nrw.LhsTokens.Count != 0) {
                    CSharpCCGlobals.PrintTokenSetup(e_nrw.LhsTokens[0]);
                    foreach (var token in e_nrw.LhsTokens) {
                        t = token;
                        retval += CSharpCCGlobals.PrintToken(t);
                    }
                    retval += CSharpCCGlobals.PrintTrailingComments(t);
                    retval += " = ";
                }
                String tail = e_nrw.RhsToken == null ? ");" : ")." + e_nrw.RhsToken.image + ";";
                if (e_nrw.Label.Equals("")) {
                    string label;
                    if (CSharpCCGlobals.names_of_tokens.TryGetValue(e_nrw.Ordinal, out label)) {
                        retval += "cc_consume_token(" + label + tail;
                    } else {
                        retval += "cc_consume_token(" + e_nrw.Ordinal + tail;
                    }
                } else {
                    retval += "cc_consume_token(" + e_nrw.Label + tail;
                }
            } else if (e is NonTerminal) {
                NonTerminal e_nrw = (NonTerminal) e;
                retval += "\n";
                if (e_nrw.LhsTokens.Count != 0) {
                    CSharpCCGlobals.PrintTokenSetup(e_nrw.LhsTokens[0]);
                    foreach (var token in e_nrw.LhsTokens) {
                        t = token;
                        retval += CSharpCCGlobals.PrintToken(t);
                    }
                    retval += CSharpCCGlobals.PrintTrailingComments(t);
                    retval += " = ";
                }
                retval += e_nrw.Name + "(";
                if (e_nrw.ArgumentTokens.Count != 0) {
                    CSharpCCGlobals.PrintTokenSetup(e_nrw.ArgumentTokens[0]);
                    foreach (var token in e_nrw.ArgumentTokens) {
                        t = token;
                        retval += CSharpCCGlobals.PrintToken(t);
                    }
                    retval += CSharpCCGlobals.PrintTrailingComments(t);
                }
                retval += ");";
            } else if (e is Action) {
                Action e_nrw = (Action) e;
                retval += "\u0003\n";
                if (e_nrw.ActionTokens.Count != 0) {
                    CSharpCCGlobals.PrintTokenSetup(e_nrw.ActionTokens[0]);
                    CSharpCCGlobals.ccol = 1;
                    foreach (var token in e_nrw.ActionTokens) {
                        t = token;
                        retval += CSharpCCGlobals.PrintToken(t);
                    }
                    retval += CSharpCCGlobals.PrintTrailingComments(t);
                }
                retval += "\u0004";
            } else if (e is Choice) {
                Choice e_nrw = (Choice) e;
                conds = new Lookahead[e_nrw.Choices.Count];
                actions = new String[e_nrw.Choices.Count + 1];
                actions[e_nrw.Choices.Count] = "\n" + "cc_consume_token(-1);\n" + "throw new ParseException();";
                // In previous line, the "throw" never throws an exception since the
                // evaluation of jj_consume_token(-1) causes ParseException to be
                // thrown first.
                Sequence nestedSeq;
                for (int i = 0; i < e_nrw.Choices.Count; i++) {
                    nestedSeq = (Sequence) (e_nrw.Choices[i]);
                    actions[i] = phase1ExpansionGen(nestedSeq);
                    conds[i] = (Lookahead) (nestedSeq.Units[0]);
                }
                retval = buildLookaheadChecker(conds, actions);
            } else if (e is Sequence) {
                Sequence e_nrw = (Sequence) e;
                // We skip the first element in the following iteration since it is the
                // Lookahead object.
                foreach (var unit in e_nrw.Units) {
                    retval += phase1ExpansionGen(unit);
                }
            } else if (e is OneOrMore) {
                OneOrMore e_nrw = (OneOrMore) e;
                Expansion nested_e = e_nrw.Expansion;
                Lookahead la;
                if (nested_e is Sequence) {
                    la = (Lookahead) (((Sequence) nested_e).Units[0]);
                } else {
                    la = new Lookahead();
                    la.Amount = Options.getLookahead();
                    la.Expansion = nested_e;
                }
                retval += "\n";
                int labelIndex = ++gensymindex;
                retval += "while (true) {\u0001";
                retval += phase1ExpansionGen(nested_e);
                conds = new Lookahead[1];
                conds[0] = la;
                actions = new String[2];
                actions[0] = "\n;";
                actions[1] = "\ngoto label_" + labelIndex + ";";
                retval += buildLookaheadChecker(conds, actions);
                retval += "\u0002\n" + "}";
                retval += "label_" + labelIndex + ":;\n";
            } else if (e is ZeroOrMore) {
                ZeroOrMore e_nrw = (ZeroOrMore) e;
                Expansion nested_e = e_nrw.Expansion;
                Lookahead la;
                if (nested_e is Sequence) {
                    la = (Lookahead) (((Sequence) nested_e).Units[0]);
                } else {
                    la = new Lookahead();
                    la.Amount = Options.getLookahead();
                    la.Expansion = nested_e;
                }
                retval += "\n";
                int labelIndex = ++gensymindex;
                retval += "while (true) {\u0001";
                conds = new Lookahead[1];
                conds[0] = la;
                actions = new String[2];
                actions[0] = "\n;";
                actions[1] = "\ngoto label_" + labelIndex + ";";
                retval += buildLookaheadChecker(conds, actions);
                retval += phase1ExpansionGen(nested_e);
                retval += "\u0002\n" + "}";
                retval += "label_" + labelIndex + ":;\n";
            } else if (e is ZeroOrOne) {
                ZeroOrOne e_nrw = (ZeroOrOne) e;
                Expansion nested_e = e_nrw.Expansion;
                Lookahead la;
                if (nested_e is Sequence) {
                    la = (Lookahead) (((Sequence) nested_e).Units[0]);
                } else {
                    la = new Lookahead();
                    la.Amount = Options.getLookahead();
                    la.Expansion = nested_e;
                }
                conds = new Lookahead[1];
                conds[0] = la;
                actions = new String[2];
                actions[0] = phase1ExpansionGen(nested_e);
                actions[1] = "\n;";
                retval += buildLookaheadChecker(conds, actions);
            } else if (e is TryBlock) {
                TryBlock e_nrw = (TryBlock) e;
                Expansion nested_e = e_nrw.Expansion;
                IList<Token> list;
                retval += "\n";
                retval += "try {\u0001";
                retval += phase1ExpansionGen(nested_e);
                retval += "\u0002\n" + "}";
                for (int i = 0; i < e_nrw.CatchBlocks.Count; i++) {
                    retval += " catch (";
                    list = e_nrw.Types[i];
                    if (list.Count != 0) {
                        CSharpCCGlobals.PrintTokenSetup(list[0]);
                        foreach (var token in list) {
                            t = token;
                            retval += CSharpCCGlobals.PrintToken(t);
                        }
                        retval += CSharpCCGlobals.PrintTrailingComments(t);
                    }
                    retval += " ";
                    t = e_nrw.Ids[i];
                    CSharpCCGlobals.PrintTokenSetup(t);
                    retval += CSharpCCGlobals.PrintToken(t);
                    retval += CSharpCCGlobals.PrintTrailingComments(t);
                    retval += ") {\u0003\n";
                    list = e_nrw.CatchBlocks[i];
                    if (list.Count != 0) {
                        CSharpCCGlobals.PrintTokenSetup(list[0]);
                        CSharpCCGlobals.ccol = 1;
                        foreach (var token in list) {
                            t = token;
                            retval += CSharpCCGlobals.PrintToken(t);
                        }
                        retval += CSharpCCGlobals.PrintTrailingComments(t);
                    }
                    retval += "\u0004\n" + "}";
                }
                if (e_nrw.FinallyBlocks != null) {
                    retval += " finally {\u0003\n";
                    if (e_nrw.FinallyBlocks.Count != 0) {
                        CSharpCCGlobals.PrintTokenSetup(e_nrw.FinallyBlocks[0]);
                        CSharpCCGlobals.ccol = 1;
                        foreach (var token in e_nrw.FinallyBlocks) {
                            t = token;
                            retval += CSharpCCGlobals.PrintToken(t);
                        }
                        retval += CSharpCCGlobals.PrintTrailingComments(t);
                    }
                    retval += "\u0004\n" + "}";
                }
            }
            return retval;
        }

        private static void buildPhase2Routine(Lookahead la) {
            Expansion e = la.Expansion;
            ostr.WriteLine("  private " + CSharpCCGlobals.staticOpt() + " bool cc_2" + e.InternalName + "(int xla) {");
            ostr.WriteLine("    cc_la = xla; cc_lastpos = cc_scanpos = token;");
            ostr.WriteLine("    try { return !cc_3" + e.InternalName + "(); }");
            ostr.WriteLine("    catch(LookaheadSuccess) { return true; }");
            if (Options.getErrorReporting())
                ostr.WriteLine("    finally { cc_save(" + (Int32.Parse(e.InternalName.Substring(1)) - 1) + ", xla); }");
            ostr.WriteLine("  }");
            ostr.WriteLine("");
            Phase3Data p3d = new Phase3Data(e, la.Amount);
            phase3list.Add(p3d);
            phase3table[e] = p3d;
        }

        private static bool xsp_declared;

        private static Expansion cc3_expansion;

        private static String genReturn(bool value) {
            String retval = (value ? "true" : "false");
            if (Options.getDebugLookahead() && cc3_expansion != null) {
                String tracecode = "trace_return(\"" + ((NormalProduction) cc3_expansion.Parent).Lhs + "(LOOKAHEAD " +
                                   (value ? "FAILED" : "SUCCEEDED") + ")\");";
                if (Options.getErrorReporting()) {
                    tracecode = "if (!cc_rescan) " + tracecode;
                }
                return "{ " + tracecode + " return " + retval + "; }";
            } else {
                return "return " + retval + ";";
            }
        }

        private static void generate3R(Expansion e, Phase3Data inf) {
            Expansion seq = e;
            if (e.InternalName.Equals("")) {
                while (true) {
                    if (seq is Sequence && ((Sequence) seq).Units.Count == 2) {
                        seq = ((Sequence) seq).Units[1];
                    } else if (seq is NonTerminal) {
                        NonTerminal e_nrw = (NonTerminal) seq;
                        NormalProduction ntprod = productionTable[e_nrw.Name];
                        if (ntprod is CodeProduction) {
                            break; // nothing to do here
                        } else {
                            seq = ntprod.Expansion;
                        }
                    } else
                        break;
                }

                if (seq is RegularExpression) {
                    e.InternalName = "cc_scan_token(" + seq.Ordinal + ")";
                    return;
                }

                gensymindex++;
                e.InternalName = "R_" + gensymindex;
            }
            Phase3Data p3d;
            if (!phase3table.TryGetValue(e, out p3d) ||
                p3d.Count < inf.Count) {
                p3d = new Phase3Data(e, inf.Count);
                phase3list.Add(p3d);
                phase3table[e] = p3d;
            }
        }

        private static void setupPhase3Builds(Phase3Data inf) {
            Expansion e = inf.Expansion;
            if (e is RegularExpression) {
                ; // nothing to here
            } else if (e is NonTerminal) {
                // All expansions of non-terminals have the "name" fields set.  So
                // there's no need to check it below for "e_nrw" and "ntexp".  In
                // fact, we rely here on the fact that the "name" fields of both these
                // variables are the same.
                NonTerminal e_nrw = (NonTerminal) e;
                NormalProduction ntprod = productionTable[e_nrw.Name];
                if (ntprod is CodeProduction) {
                    ; // nothing to do here
                } else {
                    generate3R(ntprod.Expansion, inf);
                }
            } else if (e is Choice) {
                Choice e_nrw = (Choice) e;
                for (int i = 0; i < e_nrw.Choices.Count; i++) {
                    generate3R(e_nrw.Choices[i], inf);
                }
            } else if (e is Sequence) {
                Sequence e_nrw = (Sequence) e;
                // We skip the first element in the following iteration since it is the
                // Lookahead object.
                int cnt = inf.Count;
                for (int i = 1; i < e_nrw.Units.Count; i++) {
                    Expansion eseq = e_nrw.Units[i];
                    setupPhase3Builds(new Phase3Data(eseq, cnt));
                    cnt -= minimumSize(eseq);
                    if (cnt <= 0)
                        break;
                }
            } else if (e is TryBlock) {
                TryBlock e_nrw = (TryBlock) e;
                setupPhase3Builds(new Phase3Data(e_nrw.Expansion, inf.Count));
            } else if (e is OneOrMore) {
                OneOrMore e_nrw = (OneOrMore) e;
                generate3R(e_nrw.Expansion, inf);
            } else if (e is ZeroOrMore) {
                ZeroOrMore e_nrw = (ZeroOrMore) e;
                generate3R(e_nrw.Expansion, inf);
            } else if (e is ZeroOrOne) {
                ZeroOrOne e_nrw = (ZeroOrOne) e;
                generate3R(e_nrw.Expansion, inf);
            }
        }

        private static String gencc_3Call(Expansion e) {
            return e.InternalName.StartsWith("cc_scan_token") ? e.InternalName : "cc_3" + e.InternalName + "()";
        }

        private static void buildPhase3Routine(Phase3Data inf, bool recursive_call) {
            Expansion e = inf.Expansion;
            Token t = null;
            if (e.InternalName.StartsWith("cc_scan_token"))
                return;

            if (!recursive_call) {
                ostr.WriteLine("  private " + CSharpCCGlobals.staticOpt() + "bool cc_3" + e.InternalName + "() {");
                xsp_declared = false;
                if (Options.getDebugLookahead() && e.Parent is NormalProduction) {
                    ostr.Write("    ");
                    if (Options.getErrorReporting()) {
                        ostr.Write("if (!cc_rescan) ");
                    }
                    ostr.WriteLine("trace_call(\"" + ((NormalProduction) e.Parent).Lhs + "(LOOKING AHEAD...)\");");
                    cc3_expansion = e;
                } else {
                    cc3_expansion = null;
                }
            }
            if (e is RegularExpression) {
                RegularExpression e_nrw = (RegularExpression) e;
                if (e_nrw.Label.Equals("")) {
                    string label;
                    if (CSharpCCGlobals.names_of_tokens.TryGetValue(e_nrw.Ordinal, out label)) {
                        ostr.WriteLine("    if (cc_scan_token(" + label + ")) " + genReturn(true));
                    } else {
                        ostr.WriteLine("    if (cc_scan_token(" + e_nrw.Ordinal + ")) " + genReturn(true));
                    }
                } else {
                    ostr.WriteLine("    if (cc_scan_token(" + e_nrw.Label + ")) " + genReturn(true));
                }
            } else if (e is NonTerminal) {
                // All expansions of non-terminals have the "name" fields set.  So
                // there's no need to check it below for "e_nrw" and "ntexp".  In
                // fact, we rely here on the fact that the "name" fields of both these
                // variables are the same.
                NonTerminal e_nrw = (NonTerminal) e;
                NormalProduction ntprod = productionTable[e_nrw.Name];
                if (ntprod is CodeProduction) {
                    ostr.WriteLine("    if (true) { cc_la = 0; cc_scanpos = cc_lastpos; " + genReturn(false) + "}");
                } else {
                    Expansion ntexp = ntprod.Expansion;
                    ostr.WriteLine("    if (" + gencc_3Call(ntexp) + ") " + genReturn(true));
                }
            } else if (e is Choice) {
                Sequence nested_seq;
                Choice e_nrw = (Choice) e;
                if (e_nrw.Choices.Count != 1) {
                    if (!xsp_declared) {
                        xsp_declared = true;
                        ostr.WriteLine("    Token xsp;");
                    }
                    ostr.WriteLine("    xsp = cc_scanpos;");
                }
                for (int i = 0; i < e_nrw.Choices.Count; i++) {
                    nested_seq = (Sequence) (e_nrw.Choices[i]);
                    Lookahead la = (Lookahead) (nested_seq.Units[0]);
                    if (la.ActionTokens.Count != 0) {
                        // We have semantic lookahead that must be evaluated.
                        CSharpCCGlobals.lookaheadNeeded = true;
                        ostr.WriteLine("    cc_lookingAhead = true;");
                        ostr.Write("    cc_semLA = ");
                        CSharpCCGlobals.PrintTokenSetup(la.ActionTokens[0]);
                        foreach (var token in la.ActionTokens) {
                            t = token;
                            CSharpCCGlobals.PrintToken(t, ostr);
                        }
                        CSharpCCGlobals.PrintTrailingComments(t, ostr);
                        ostr.WriteLine(";");
                        ostr.WriteLine("    cc_lookingAhead = false;");
                    }
                    ostr.Write("    if (");
                    if (la.ActionTokens.Count != 0) {
                        ostr.Write("!cc_semLA || ");
                    }
                    if (i != e_nrw.Choices.Count - 1) {
                        ostr.WriteLine(gencc_3Call(nested_seq) + ") {");
                        ostr.WriteLine("    cc_scanpos = xsp;");
                    } else {
                        ostr.WriteLine(gencc_3Call(nested_seq) + ") " + genReturn(true));
                    }
                }
                for (int i = 1; i < e_nrw.Choices.Count; i++) {
                    ostr.WriteLine("    }");
                }
            } else if (e is Sequence) {
                Sequence e_nrw = (Sequence) e;
                // We skip the first element in the following iteration since it is the
                // Lookahead object.
                int cnt = inf.Count;
                for (int i = 1; i < e_nrw.Units.Count; i++) {
                    Expansion eseq = (Expansion) (e_nrw.Units[i]);
                    buildPhase3Routine(new Phase3Data(eseq, cnt), true);

                    cnt -= minimumSize(eseq);
                    if (cnt <= 0)
                        break;
                }
            } else if (e is TryBlock) {
                TryBlock e_nrw = (TryBlock) e;
                buildPhase3Routine(new Phase3Data(e_nrw.Expansion, inf.Count), true);
            } else if (e is OneOrMore) {
                if (!xsp_declared) {
                    xsp_declared = true;
                    ostr.WriteLine("    Token xsp;");
                }
                OneOrMore e_nrw = (OneOrMore) e;
                Expansion nested_e = e_nrw.Expansion;
                ostr.WriteLine("    if (" + gencc_3Call(nested_e) + ") " + genReturn(true));
                ostr.WriteLine("    while (true) {");
                ostr.WriteLine("      xsp = cc_scanpos;");
                ostr.WriteLine("      if (" + gencc_3Call(nested_e) + ") { cc_scanpos = xsp; break; }");
                ostr.WriteLine("    }");
            } else if (e is ZeroOrMore) {
                if (!xsp_declared) {
                    xsp_declared = true;
                    ostr.WriteLine("    Token xsp;");
                }
                ZeroOrMore e_nrw = (ZeroOrMore) e;
                Expansion nested_e = e_nrw.Expansion;
                ostr.WriteLine("    while (true) {");
                ostr.WriteLine("      xsp = cc_scanpos;");
                ostr.WriteLine("      if (" + gencc_3Call(nested_e) + ") { cc_scanpos = xsp; break; }");
                ostr.WriteLine("    }");
            } else if (e is ZeroOrOne) {
                if (!xsp_declared) {
                    xsp_declared = true;
                    ostr.WriteLine("    Token xsp;");
                }
                ZeroOrOne e_nrw = (ZeroOrOne) e;
                Expansion nested_e = e_nrw.Expansion;
                ostr.WriteLine("    xsp = cc_scanpos;");
                ostr.WriteLine("    if (" + gencc_3Call(nested_e) + ") cc_scanpos = xsp;");
            }
            if (!recursive_call) {
                ostr.WriteLine("    " + genReturn(false));
                ostr.WriteLine("  }");
                ostr.WriteLine("");
            }
        }

        private static int minimumSize(Expansion e) {
            return minimumSize(e, Int32.MaxValue);
        }

        private static int minimumSize(Expansion e, int oldMin) {
            int retval = 0; // should never be used.  Will be bad if it is.
            if (e.IsMinimumSize) {
                // recursive search for minimum size unnecessary.
                return Int32.MaxValue;
            }

            e.IsMinimumSize = true;
            if (e is RegularExpression) {
                retval = 1;
            } else if (e is NonTerminal) {
                NonTerminal e_nrw = (NonTerminal) e;
                NormalProduction ntprod = productionTable[e_nrw.Name];
                if (ntprod is CodeProduction) {
                    retval = Int32.MaxValue;
                    // Make caller think this is unending (for we do not go beyond JAVACODE during
                    // phase3 execution).
                } else {
                    Expansion ntexp = ntprod.Expansion;
                    retval = minimumSize(ntexp);
                }
            } else if (e is Choice) {
                int min = oldMin;
                Expansion nested_e;
                Choice e_nrw = (Choice) e;
                for (int i = 0; min > 1 && i < e_nrw.Choices.Count; i++) {
                    nested_e = e_nrw.Choices[i];
                    int min1 = minimumSize(nested_e, min);
                    if (min > min1)
                        min = min1;
                }
                retval = min;
            } else if (e is Sequence) {
                int min = 0;
                Sequence e_nrw = (Sequence) e;
                // We skip the first element in the following iteration since it is the
                // Lookahead object.
                for (int i = 1; i < e_nrw.Units.Count; i++) {
                    Expansion eseq = e_nrw.Units[i];
                    int mineseq = minimumSize(eseq);
                    if (min == Int32.MaxValue || 
                        mineseq == Int32.MaxValue) {
                        min = Int32.MaxValue; // Adding infinity to something results in infinity.
                    } else {
                        min += mineseq;
                        if (min > oldMin)
                            break;
                    }
                }
                retval = min;
            } else if (e is TryBlock) {
                TryBlock e_nrw = (TryBlock) e;
                retval = minimumSize(e_nrw.Expansion);
            } else if (e is OneOrMore) {
                OneOrMore e_nrw = (OneOrMore) e;
                retval = minimumSize(e_nrw.Expansion);
            } else if (e is ZeroOrMore) {
                retval = 0;
            } else if (e is ZeroOrOne) {
                retval = 0;
            } else if (e is Lookahead) {
                retval = 0;
            } else if (e is Action) {
                retval = 0;
            }
            e.IsMinimumSize = false;
            return retval;
        }

        private static void build(TextWriter ps) {
            CodeProduction jp;
            Token t = null;

            ostr = ps;

            foreach (var p in CSharpCCGlobals.bnfproductions) {
                if (p is CodeProduction) {
                    jp = (CodeProduction) p;
                    t = jp.ReturnTypeTokens[0];
                    CSharpCCGlobals.PrintTokenSetup(t);
                    CSharpCCGlobals.ccol = 1;
                    CSharpCCGlobals.PrintLeadingComments(t, ostr);
                    ostr.Write("  " + CSharpCCGlobals.staticOpt() + (p.AccessModifier != null ? p.AccessModifier + " " : ""));
                    CSharpCCGlobals.cline = t.beginLine;
                    CSharpCCGlobals.ccol = t.beginColumn;
                    CSharpCCGlobals.PrintTokenOnly(t, ostr);
                    for (int i = 1; i < jp.ReturnTypeTokens.Count; i++) {
                        t = jp.ReturnTypeTokens[i];
                        CSharpCCGlobals.PrintToken(t, ostr);
                    }
                    CSharpCCGlobals.PrintTrailingComments(t, ostr);
                    ostr.Write(" " + jp.Lhs + "(");
                    if (jp.ParameterTokens.Count != 0) {
                        CSharpCCGlobals.PrintTokenSetup(jp.ParameterTokens[0]);
                        foreach (var token in jp.ParameterTokens) {
                            t = token;
                            CSharpCCGlobals.PrintToken(t, ostr);
                        }
                        CSharpCCGlobals.PrintTrailingComments(t, ostr);
                    }
                    ostr.Write(") {");
                    if (Options.getDebugParser()) {
                        ostr.WriteLine("");
                        ostr.WriteLine("    trace_call(\"" + jp.Lhs + "\");");
                        ostr.Write("    try {");
                    }
                    if (jp.CodeTokens.Count != 0) {
                        CSharpCCGlobals.PrintTokenSetup(jp.CodeTokens[0]);
                        CSharpCCGlobals.cline--;
                        CSharpCCGlobals.PrintTokenList(jp.CodeTokens, ostr);
                    }
                    ostr.WriteLine("");
                    if (Options.getDebugParser()) {
                        ostr.WriteLine("    } finally {");
                        ostr.WriteLine("      trace_return(\"" + jp.Lhs + "\");");
                        ostr.WriteLine("    }");
                    }
                    ostr.WriteLine("  }");
                    ostr.WriteLine("");
                } else {
                    buildPhase1Routine((BnfProduction) p);
                }
            }

            foreach (var lookahead in phase2list) {
                buildPhase2Routine(lookahead);
            }

            foreach (var phase3Data in phase3list) {
                setupPhase3Builds(phase3Data);
            }

            foreach (var phase3Data in phase3table) {
                buildPhase3Routine(phase3Data.Value, false);
            }
        }

        public static void reInit() {
            ostr = null;
            gensymindex = 0;
            indentamt = 0;
            cc2LA = false;
            phase2list = new List<Lookahead>();
            phase3list = new List<Phase3Data>();
            phase3table = new Dictionary<Expansion, Phase3Data>();
            firstSet = null;
            xsp_declared = false;
            cc3_expansion = null;
        }

        private class Phase3Data {
            public Expansion Expansion;
            public int Count;

            public Phase3Data(Expansion expansion, int count) {
                Expansion = expansion;
                Count = count;
            }
        }
    }
}