using System;
using System.Collections;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
	public static class CSharpCCParserInternals {
		internal static void initialize() {
			int i = 0;
			CSharpCCGlobals.lexstate_S2I["DEFAULT"] = i;
			CSharpCCGlobals.lexstate_I2S[i] = "DEFAULT";
			CSharpCCGlobals.simple_tokens_table["DEFAULT"] = new Dictionary<string, IDictionary<string, RegularExpression>>();
		}

		public static void addcuname(String id) {
			CSharpCCGlobals.cu_name = id;
		}

		public static void compare(Token t, String id1, String id2) {
			if (!id2.Equals(id1)) {
				CSharpCCErrors.ParseError(t, "Name " + id2 + " must be the same as that used at PARSER_BEGIN (" + id1 + ")");
			}
		}

		private static IList<Token> add_cu_token_here = CSharpCCGlobals.cu_to_insertion_point_1;
		private static Token first_cu_token;
		private static bool insertionpoint1set = false;
		private static bool insertionpoint2set = false;

		public static void setinsertionpoint(Token t, int no) {
			do {
				add_cu_token_here.Add(first_cu_token);
				first_cu_token = first_cu_token.next;
			} while (first_cu_token != t);
			if (no == 1) {
				if (insertionpoint1set) {
					CSharpCCErrors.ParseError(t, "Multiple declaration of parser class.");
				} else {
					insertionpoint1set = true;
					add_cu_token_here = CSharpCCGlobals.cu_to_insertion_point_2;
				}
			} else {
				add_cu_token_here = CSharpCCGlobals.cu_from_insertion_point_2;
				insertionpoint2set = true;
			}
			first_cu_token = t;
		}

		public static void insertionpointerrors(Token t) {
			while (first_cu_token != t) {
				add_cu_token_here.Add(first_cu_token);
				first_cu_token = first_cu_token.next;
			}
			if (!insertionpoint1set || !insertionpoint2set) {
				CSharpCCErrors.ParseError(t, "Parser class has not been defined between PARSER_BEGIN and PARSER_END.");
			}
		}

		public static void set_initial_cu_token(Token t) {
			first_cu_token = t;
		}

		public static void addproduction(NormalProduction p) {
			CSharpCCGlobals.bnfproductions.Add(p);
		}

		public static void production_addexpansion(BnfProduction p, Expansion e) {
			e.Parent = p;
			p.Expansion = e;
		}

		private static int nextFreeLexState = 1;

		public static void addregexpr(TokenProduction p) {
			int ii;
			CSharpCCGlobals.rexprlist.Add(p);
			if (Options.getUserTokenManager()) {
				if (p.LexStates == null ||
				    p.LexStates.Length != 1 ||
				    !p.LexStates[0].Equals("DEFAULT")) {
					CSharpCCErrors.Warning(p, "Ignoring lexical state specifications since option " +
					                          "USER_TOKEN_MANAGER has been set to true.");
				}
			}
			if (p.LexStates == null) {
				return;
			}
			for (int i = 0; i < p.LexStates.Length; i++) {
				for (int j = 0; j < i; j++) {
					if (p.LexStates[i].Equals(p.LexStates[j])) {
						CSharpCCErrors.ParseError(p, "Multiple occurrence of \"" + p.LexStates[i] + "\" in lexical state list.");
					}
				}
				if (CSharpCCGlobals.lexstate_S2I.ContainsKey(p.LexStates[i])) {
					ii = nextFreeLexState++;
					CSharpCCGlobals.lexstate_S2I[p.LexStates[i]] = ii;
					CSharpCCGlobals.lexstate_I2S[ii] = p.LexStates[i];
					CSharpCCGlobals.simple_tokens_table[p.LexStates[i]] = new Dictionary<string, IDictionary<string, RegularExpression>>();
				}
			}
		}

		public static void add_token_manager_decls(Token t, IList<Token> decls) {
			if (CSharpCCGlobals.token_mgr_decls != null) {
				CSharpCCErrors.ParseError(t, "Multiple occurrence of \"TOKEN_MGR_DECLS\".");
			} else {
				CSharpCCGlobals.token_mgr_decls = decls;
				if (Options.getUserTokenManager()) {
					CSharpCCErrors.Warning(t, "Ignoring declarations in \"TOKEN_MGR_DECLS\" since option " +
					                          "USER_TOKEN_MANAGER has been set to true.");
				}
			}
		}

		public static void add_inline_regexpr(RegularExpression r) {
			if (!(r is REndOfFile)) {
				TokenProduction p = new TokenProduction();
				p.IsExplicit = false;
				p.LexStates = new String[] {"DEFAULT"};
				p.Kind = TokenProduction.TOKEN;
				RegExprSpec res = new RegExprSpec();
				res.RegularExpression = r;
				res.RegularExpression.TokenProductionContext = p;
				res.Action = new Action();
				res.NextState = null;
				res.NextStateToken = null;
				p.RegexSpecs.Add(res);
				CSharpCCGlobals.rexprlist.Add(p);
			}
		}

		public static bool hexchar(char ch) {
			if (ch >= '0' && ch <= '9')
				return true;
			if (ch >= 'A' && ch <= 'F')
				return true;
			if (ch >= 'a' && ch <= 'f')
				return true;
			return false;
		}

		public static int hexval(char ch) {
			if (ch >= '0' && ch <= '9')
				return ((int) ch) - ((int) '0');
			if (ch >= 'A' && ch <= 'F')
				return ((int) ch) - ((int) 'A') + 10;
			return ((int) ch) - ((int) 'a') + 10;
		}

		public static String remove_escapes_and_quotes(Token t, String str) {
			String retval = "";
			int index = 1;
			char ch, ch1;
			int ordinal;
			while (index < str.Length - 1) {
				if (str[index] != '\\') {
					retval += str[index];
					index++;
					continue;
				}
				index++;
				ch = str[index];
				if (ch == 'b') {
					retval += '\b';
					index++;
					continue;
				}
				if (ch == 't') {
					retval += '\t';
					index++;
					continue;
				}
				if (ch == 'n') {
					retval += '\n';
					index++;
					continue;
				}
				if (ch == 'f') {
					retval += '\f';
					index++;
					continue;
				}
				if (ch == 'r') {
					retval += '\r';
					index++;
					continue;
				}
				if (ch == '"') {
					retval += '\"';
					index++;
					continue;
				}
				if (ch == '\'') {
					retval += '\'';
					index++;
					continue;
				}
				if (ch == '\\') {
					retval += '\\';
					index++;
					continue;
				}
				if (ch >= '0' && ch <= '7') {
					ordinal = ((int) ch) - ((int) '0');
					index++;
					ch1 = str[index];
					if (ch1 >= '0' && ch1 <= '7') {
						ordinal = ordinal*8 + ((int) ch1) - ((int) '0');
						index++;
						ch1 = str[index];
						if (ch <= '3' && ch1 >= '0' && ch1 <= '7') {
							ordinal = ordinal*8 + ((int) ch1) - ((int) '0');
							index++;
						}
					}
					retval += (char) ordinal;
					continue;
				}
				if (ch == 'u') {
					index++;
					ch = str[index];
					if (hexchar(ch)) {
						ordinal = hexval(ch);
						index++;
						ch = str[index];
						if (hexchar(ch)) {
							ordinal = ordinal*16 + hexval(ch);
							index++;
							ch = str[index];
							if (hexchar(ch)) {
								ordinal = ordinal*16 + hexval(ch);
								index++;
								ch = str[index];
								if (hexchar(ch)) {
									ordinal = ordinal*16 + hexval(ch);
									index++;
									continue;
								}
							}
						}
					}
					CSharpCCErrors.ParseError(t, "Encountered non-hex character '" + ch +
					                             "' at position " + index + " of string " +
					                             "- Unicode escape must have 4 hex digits after it.");
					return retval;
				}
				CSharpCCErrors.ParseError(t, "Illegal escape sequence '\\" + ch +
				                             "' at position " + index + " of string.");
				return retval;
			}
			return retval;
		}

		public static char character_descriptor_assign(Token t, String s) {
			if (s.Length != 1) {
				CSharpCCErrors.ParseError(t, "String in character list may contain only one character.");
				return ' ';
			} else {
				return s[0];
			}
		}

		public static char character_descriptor_assign(Token t, String s, String left) {
			if (s.Length != 1) {
				CSharpCCErrors.ParseError(t, "String in character list may contain only one character.");
				return ' ';
			} else if ((int) (left[0]) > (int) (s[0])) {
				CSharpCCErrors.ParseError(t, "Right end of character range \'" + s +
				                             "\' has a lower ordinal value than the left end of character range \'" + left + "\'.");
				return left[0];
			} else {
				return s[0];
			}
		}

		public static void makeTryBlock(
			Token tryLoc,
			Container result,
			Container nestedExp,
			IList<IList<Token>> types,
			IList<Token> ids,
			IList<IList<Token>> catchblks,
			IList<Token> finallyblk
			) {
			if (catchblks.Count == 0 && finallyblk == null) {
				CSharpCCErrors.ParseError(tryLoc, "Try block must contain at least one catch or finally block.");
				return;
			}
			TryBlock tblk = new TryBlock();
			tblk.Line = tryLoc.beginLine;
			tblk.Column = tryLoc.beginColumn;
			tblk.Expansion = (Expansion) (nestedExp.member);
			tblk.Expansion.Parent = tblk;
			tblk.Expansion.Ordinal = 0;
			tblk.Types = types;
			tblk.Ids = ids;
			tblk.CatchBlocks = catchblks;
			tblk.FinallyBlocks = finallyblk;
			result.member = tblk;
		}

		public static void reInit() {
			add_cu_token_here = CSharpCCGlobals.cu_to_insertion_point_1;
			first_cu_token = null;
			insertionpoint1set = false;
			insertionpoint2set = false;
			nextFreeLexState = 1;
		}
	}
}