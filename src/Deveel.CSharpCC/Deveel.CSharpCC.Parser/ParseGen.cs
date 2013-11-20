using System;
using System.Collections.Generic;
using System.IO;

namespace Deveel.CSharpCC.Parser {
	public class ParseGen {
		private static TextWriter ostr;

		public static void start() {
			Token t = null;

			if (CSharpCCErrors.ErrorCount != 0) throw new MetaParseException();

			if (Options.getBuildParser()) {

				try {
					ostr =
						new StreamWriter(
							new BufferedStream(
								new FileStream(Path.Combine(Options.getOutputDirectory().FullName, CSharpCCGlobals.cu_name + ".cs"),
								               FileMode.CreateNew), 8192));
				} catch (IOException e) {
					CSharpCCErrors.SemanticError("Could not open file " + CSharpCCGlobals.cu_name + ".cs for writing.");
					throw new InvalidOperationException();
				}

				IList<string> tn = new List<string>(CSharpCCGlobals.ToolNames);
				tn.Add(CSharpCCGlobals.ToolName);
				ostr.WriteLine("// " + CSharpCCGlobals.GetIdString(tn, CSharpCCGlobals.cu_name + ".cs"));

				bool implementsExists = false;

				if (CSharpCCGlobals.cu_to_insertion_point_1.Count != 0) {
					CSharpCCGlobals.PrintTokenSetup(CSharpCCGlobals.cu_to_insertion_point_1[0]); 
					CSharpCCGlobals.ccol = 1;
					foreach (Token token in CSharpCCGlobals.cu_to_insertion_point_1) {
						t = token;
						if (t.kind == CSharpCCParserConstants.COLON) {
							implementsExists = true;
						} else if (t.kind == CSharpCCParserConstants.CLASS) {
							implementsExists = false;
						}
						CSharpCCGlobals.PrintToken(t, ostr);
					}
				}

				ostr.Write(" : ");
				ostr.Write(CSharpCCGlobals.cu_name + "Constants ");

				if (implementsExists) {
					ostr.Write(", ");
				}
				
				if (CSharpCCGlobals.cu_to_insertion_point_2.Count != 0) {
					CSharpCCGlobals.PrintTokenSetup(CSharpCCGlobals.cu_to_insertion_point_2[0]);
					foreach (Token token in CSharpCCGlobals.cu_to_insertion_point_2) {
						t = token;
						CSharpCCGlobals.PrintToken(t, ostr);
					}
				}

				ostr.WriteLine("");
				ostr.WriteLine("");

				ParseEngine.build(ostr);

				if (Options.getStatic()) {
					ostr.WriteLine("  private static bool cc_initialized_once = false;");
				}
				if (Options.getUserTokenManager()) {
					ostr.WriteLine("  /** User defined Token Manager. */");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public TokenManager tokenSource;");
				} else {
					ostr.WriteLine("  /** Generated Token Manager. */");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public " + CSharpCCGlobals.cu_name + "TokenManager tokenSource;");
					if (!Options.getUserCharStream()) {
						if (Options.getUnicodeEscape()) {
							ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "UnicodeCharStream cc_inputStream;");
						} else {
							ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "SimpleCharStream cc_inputStream;");
						}
					}
				}
				ostr.WriteLine("  /// <summary>Current token.</summary>");
				ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public Token token;");
				ostr.WriteLine("  /// <summary> Next token.</summary>");
				ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public Token cc_nt;");
				if (!Options.getCacheTokens()) {
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int cc_ntk;");
				}
				if (CSharpCCGlobals.cc2index != 0) {
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private Token cc_scanpos, cc_lastpos;");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int cc_la;");
					if (CSharpCCGlobals.lookaheadNeeded) {
						ostr.WriteLine("  /** Whether we are looking ahead. */");
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private bool cc_lookingAhead = false;");
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private bool cc_semLA;");
					}
				}
				if (Options.getErrorReporting()) {
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int cc_gen;");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + " private readonly int[] cc_la1 = new int[" + CSharpCCGlobals.maskindex + "];");
					int tokenMaskSize = (CSharpCCGlobals.tokenCount - 1) / 32 + 1;
					for (int i = 0; i < tokenMaskSize; i++)
						ostr.WriteLine("  static private int[] cc_la1_" + i + ";");
					ostr.WriteLine("  static " + CSharpCCGlobals.cu_name + "() { ");
					for (int i = 0; i < tokenMaskSize; i++)
						ostr.WriteLine("      cc_la1_init_" + i + "();");
					ostr.WriteLine("   }");
					for (int i = 0; i < tokenMaskSize; i++) {
						ostr.WriteLine("   private static void cc_la1_init_" + i + "() {");
						ostr.Write("      cc_la1_" + i + " = new int[] {");
						foreach (int[] tokenMask in CSharpCCGlobals.maskVals) {
							ostr.Write("0x" + tokenMask[i].ToString("X") + ",");
						}
						ostr.WriteLine("};");
						ostr.WriteLine("   }");
					}
				}
				if (CSharpCCGlobals.cc2index != 0 && Options.getErrorReporting()) {
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "readonly private CCCalls[] cc_2_rtns = new CCCalls[" + CSharpCCGlobals.cc2index + "];");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private bool cc_rescan = false;");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int cc_gc = 0;");
				}
				ostr.WriteLine("");

				if (!Options.getUserTokenManager()) {
					if (Options.getUserCharStream()) {
						ostr.WriteLine("  /// Constructor with user supplied ICharStream.");
						ostr.WriteLine("  public " + CSharpCCGlobals.cu_name + "(ICharStream stream) {");
						if (Options.getStatic()) {
							ostr.WriteLine("    if (cc_initialized_once) {");
							ostr.WriteLine("      Console.Out.WriteLine(\"ERROR: Second call to constructor of static parser.  \");");
							ostr.WriteLine("      Console.Out.WriteLine(\"       You must either use ReInit() " +
									"or set the CSharpCC option STATIC to false\");");
							ostr.WriteLine("      Console.Out.WriteLine(\"       during parser generation.\");");
							ostr.WriteLine("      throw new InvalidOperationException();");
							ostr.WriteLine("    }");
							ostr.WriteLine("    cc_initialized_once = true;");
						}
						if (Options.getTokenManagerUsesParser() && !Options.getStatic()) {
							ostr.WriteLine("    tokenSource = new " + CSharpCCGlobals.cu_name + "TokenManager(this, stream);");
						} else {
							ostr.WriteLine("    tokenSource = new " + CSharpCCGlobals.cu_name + "TokenManager(stream);");
						}
						ostr.WriteLine("    token = new Token();");
						if (Options.getCacheTokens()) {
							ostr.WriteLine("    token.Next = cc_nt = tokenSource.GetNextToken();");
						} else {
							ostr.WriteLine("    cc_ntk = -1;");
						}
						if (Options.getErrorReporting()) {
							ostr.WriteLine("    cc_gen = 0;");
							ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) cc_la1[i] = -1;");
							if (CSharpCCGlobals.cc2index != 0) {
								ostr.WriteLine("    for (int i = 0; i < cc_2_rtns.Length; i++) cc_2_rtns[i] = new CCCalls();");
							}
						}
						ostr.WriteLine("  }");
						ostr.WriteLine("");
						ostr.WriteLine("  /** Reinitialise. */");
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public void ReInit(ICharStream stream) {");
						ostr.WriteLine("    tokenSource.ReInit(stream);");
						ostr.WriteLine("    token = new Token();");
						if (Options.getCacheTokens()) {
							ostr.WriteLine("    token.Next = cc_nt = tokenSource.GetNextToken();");
						} else {
							ostr.WriteLine("    cc_ntk = -1;");
						}
						if (CSharpCCGlobals.lookaheadNeeded) {
							ostr.WriteLine("    cc_lookingAhead = false;");
						}
						if (CSharpCCGlobals.TreeGenerated) {
							ostr.WriteLine("    ccTree.Reset();");
						}
						if (Options.getErrorReporting()) {
							ostr.WriteLine("    cc_gen = 0;");
							ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) cc_la1[i] = -1;");
							if (CSharpCCGlobals.cc2index != 0) {
								ostr.WriteLine("    for (int i = 0; i < cc_2_rtns.Length; i++) cc_2_rtns[i] = new CCCalls();");
							}
						}
						ostr.WriteLine("  }");
					} else {
						ostr.WriteLine("  /// Constructor with Stream.");
						ostr.WriteLine("  public " + CSharpCCGlobals.cu_name + "(System.IO.Stream stream)");
						ostr.WriteLine("     : this(stream, null) {");
						ostr.WriteLine("  }");
						ostr.WriteLine("  /// Constructor with Stream and supplied encoding");
						ostr.WriteLine("  public " + CSharpCCGlobals.cu_name + "(System.IO.Stream stream, System.Text.Encoding encoding) {");
						if (Options.getStatic()) {
							ostr.WriteLine("    if (cc_initialized_once) {");
							ostr.WriteLine("      Console.Out.WriteLine(\"ERROR: Second call to constructor of static parser.  \");");
							ostr.WriteLine("      Console.Out.WriteLine(\"       You must either use ReInit() or " +
									"set the CSharpCC option STATIC to false\");");
							ostr.WriteLine("      Console.Out.WriteLine(\"       during parser generation.\");");
							ostr.WriteLine("      throw new Error();");
							ostr.WriteLine("    }");
							ostr.WriteLine("    cc_initialized_once = true;");
						}
						if (Options.getUnicodeEscape()) {
							ostr.WriteLine("    cc_inputStream = new UnicodeCharStream(stream, encoding, 1, 1);");
						} else {
							ostr.WriteLine("    cc_input_stream = new SimpleCharStream(stream, encoding, 1, 1);");
						}

						if (Options.getTokenManagerUsesParser() && !Options.getStatic()) {
							ostr.WriteLine("    tokenSource = new " + CSharpCCGlobals.cu_name + "TokenManager(this, cc_inputStream);");
						} else {
							ostr.WriteLine("    tokenSource = new " +CSharpCCGlobals.cu_name + "TokenManager(cc_inputStream);");
						}
						ostr.WriteLine("    token = new Token();");
						if (Options.getCacheTokens()) {
							ostr.WriteLine("    token.Next = cc_nt = tokenSource.GetNextToken();");
						} else {
							ostr.WriteLine("    cc_ntk = -1;");
						}
						if (Options.getErrorReporting()) {
							ostr.WriteLine("    cc_gen = 0;");
							ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) cc_la1[i] = -1;");
							if (CSharpCCGlobals.cc2index != 0) {
								ostr.WriteLine("    for (int i = 0; i < cc_2_rtns.Length; i++) cc_2_rtns[i] = new CCCalls();");
							}
						}
						ostr.WriteLine("  }");
						ostr.WriteLine("");
						ostr.WriteLine("  /// Reinitialise.");
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public void ReInit(System.IO.Stream stream) {");
						ostr.WriteLine("     ReInit(stream, null);");
						ostr.WriteLine("  }");
						ostr.WriteLine("  /// Reinitialise.");
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public void ReInit(System.IO.Stream stream, System.Text.Encoding encoding) {");
							ostr.WriteLine("   cc_inputStream.ReInit(stream, encoding, 1, 1);");
						ostr.WriteLine("    tokenSource.ReInit(cc_inputStream);");
						ostr.WriteLine("    token = new Token();");
						if (Options.getCacheTokens()) {
							ostr.WriteLine("    token.Next = cc_nt = tokenSource.GetNextToken();");
						} else {
							ostr.WriteLine("    cc_ntk = -1;");
						}
						if (CSharpCCGlobals.TreeGenerated) {
							ostr.WriteLine("    ccTree.Reset();");
						}
						if (Options.getErrorReporting()) {
							ostr.WriteLine("    cc_gen = 0;");
							ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) cc_la1[i] = -1;");
							if (CSharpCCGlobals.cc2index != 0) {
								ostr.WriteLine("    for (int i = 0; i < cc_2_rtns.Length; i++) cc_2_rtns[i] = new CCCalls();");
							}
						}
						ostr.WriteLine("  }");
						ostr.WriteLine("");
						ostr.WriteLine("  /// Constructor.");
						ostr.WriteLine("  public " + CSharpCCGlobals.cu_name + "(System.IO.TetReader reader) {");
						if (Options.getStatic()) {
							ostr.WriteLine("    if (cc_initialized_once) {");
							ostr.WriteLine("      Console.Out.WriteLine(\"ERROR: Second call to constructor of static parser. \");");
							ostr.WriteLine("      Console.Out.WriteLine(\"       You must either use ReInit() or " +
									"set the CSharpCC option STATIC to false\");");
							ostr.WriteLine("      Console.Out.WriteLine(\"       during parser generation.\");");
							ostr.WriteLine("      throw new InvalidOperationException();");
							ostr.WriteLine("    }");
							ostr.WriteLine("    cc_initialized_once = true;");
						}
						if (Options.getUnicodeEscape()) {
							ostr.WriteLine("    cc_inputStream = new UnicodeCharStream(reader, 1, 1);");
						} else {
							ostr.WriteLine("    cc_inputStream = new SimpleCharStream(reader, 1, 1);");
						}
						if (Options.getTokenManagerUsesParser() && !Options.getStatic()) {
							ostr.WriteLine("    tokenSource = new " + CSharpCCGlobals.cu_name + "TokenManager(this, cc_inputStream);");
						} else {
							ostr.WriteLine("    tokenSource = new " + CSharpCCGlobals.cu_name + "TokenManager(cc_inputStream);");
						}
						ostr.WriteLine("    token = new Token();");
						if (Options.getCacheTokens()) {
							ostr.WriteLine("    token.Next = cc_nt = tokenSource.GetNextToken();");
						} else {
							ostr.WriteLine("    cc_ntk = -1;");
						}
						if (Options.getErrorReporting()) {
							ostr.WriteLine("    cc_gen = 0;");
							ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) cc_la1[i] = -1;");
							if (CSharpCCGlobals.cc2index != 0) {
								ostr.WriteLine("    for (int i = 0; i < cc_2_rtns.Length; i++) cc_2_rtns[i] = new CCCalls();");
							}
						}
						ostr.WriteLine("  }");
						ostr.WriteLine("");
						ostr.WriteLine("  /// Reinitialise.");
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public void ReInit(System.IO.TextReader reader) {");
						if (Options.getUnicodeEscape()) {
							ostr.WriteLine("    cc_inputStream.ReInit(reader, 1, 1);");
						} else {
							ostr.WriteLine("    cc_inputStream.ReInit(reader, 1, 1);");
						}
						ostr.WriteLine("    tokenSource.ReInit(c_inputStream);");
						ostr.WriteLine("    token = new Token();");
						if (Options.getCacheTokens()) {
							ostr.WriteLine("    token.next = cc_nt = tokenSource.GetNextToken();");
						} else {
							ostr.WriteLine("    cc_ntk = -1;");
						}
						if (CSharpCCGlobals.TreeGenerated) {
							ostr.WriteLine("    ccTree.Reset();");
						}
						if (Options.getErrorReporting()) {
							ostr.WriteLine("    cc_gen = 0;");
							ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) cc_la1[i] = -1;");
							if (CSharpCCGlobals.cc2index != 0) {
								ostr.WriteLine("    for (int i = 0; i < cc_2_rtns.Length; i++) cc_2_rtns[i] = new CCCalls();");
							}
						}
						ostr.WriteLine("  }");
					}
				}
				ostr.WriteLine("");
				if (Options.getUserTokenManager()) {
					ostr.WriteLine("  /** Constructor with user supplied Token Manager. */");
					ostr.WriteLine("  public " + CSharpCCGlobals.cu_name + "(ITokenManager tm) {");
				} else {
					ostr.WriteLine("  /** Constructor with generated Token Manager. */");
					ostr.WriteLine("  public " + CSharpCCGlobals.cu_name + "(" + CSharpCCGlobals.cu_name + "TokenManager tm) {");
				}
				if (Options.getStatic()) {
					ostr.WriteLine("    if (cc_initialized_once) {");
					ostr.WriteLine("      Console.Out.WriteLine(\"ERROR: Second call to constructor of static parser. \");");
					ostr.WriteLine("      Console.Out.WriteLine(\"       You must either use ReInit() or " +
							"set the JavaCC option STATIC to false\");");
					ostr.WriteLine("      Console.Out.WriteLine(\"       during parser generation.\");");
					ostr.WriteLine("      throw new InvalidOperationException();");
					ostr.WriteLine("    }");
					ostr.WriteLine("    cc_initialized_once = true;");
				}
				ostr.WriteLine("    tokenSource = tm;");
				ostr.WriteLine("    token = new Token();");
				if (Options.getCacheTokens()) {
					ostr.WriteLine("    token.Next = cc_nt = tokenSource.GetNextToken();");
				} else {
					ostr.WriteLine("    cc_ntk = -1;");
				}
				if (Options.getErrorReporting()) {
					ostr.WriteLine("    cc_gen = 0;");
					ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) cc_la1[i] = -1;");
					if (CSharpCCGlobals.cc2index != 0) {
						ostr.WriteLine("    for (int i = 0; i < cc_2_rtns.Length; i++) cc_2_rtns[i] = new CCCalls();");
					}
				}
				ostr.WriteLine("  }");
				ostr.WriteLine("");
				if (Options.getUserTokenManager()) {
					ostr.WriteLine("  /** Reinitialise. */");
					ostr.WriteLine("  public void ReInit(ITokenManager tm) {");
				} else {
					ostr.WriteLine("  /** Reinitialise. */");
					ostr.WriteLine("  public void ReInit(" + CSharpCCGlobals.cu_name + "TokenManager tm) {");
				}
				ostr.WriteLine("    tokenSource = tm;");
				ostr.WriteLine("    token = new Token();");
				if (Options.getCacheTokens()) {
					ostr.WriteLine("    token.next = cc_nt = tokenSource.GetNextToken();");
				} else {
					ostr.WriteLine("    cc_ntk = -1;");
				}
				if (CSharpCCGlobals.TreeGenerated) {
					ostr.WriteLine("    jjtree.reset();");
				}
				if (Options.getErrorReporting()) {
					ostr.WriteLine("    cc_gen = 0;");
					ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) cc_la1[i] = -1;");
					if (CSharpCCGlobals.cc2index != 0) {
						ostr.WriteLine("    for (int i = 0; i < cc_2_rtns.length; i++) cc_2_rtns[i] = new CCCalls();");
					}
				}
				ostr.WriteLine("  }");
				ostr.WriteLine("");
				ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private Token cc_consume_token(int kind) {");
				if (Options.getCacheTokens()) {
					ostr.WriteLine("    Token oldToken = token;");
					ostr.WriteLine("    if ((token = cc_nt).Next != null) cc_nt = cc_nt.Next;");
					ostr.WriteLine("    else cc_nt = cc_nt.Next = tokenSource.GetNextToken();");
				} else {
					ostr.WriteLine("    Token oldToken;");
					ostr.WriteLine("    if ((oldToken = token).Next != null) token = token.Next;");
					ostr.WriteLine("    else token = token.Next = tokenSource.GetNextToken();");
					ostr.WriteLine("    cc_ntk = -1;");
				}
				ostr.WriteLine("    if (token.kind == kind) {");
				if (Options.getErrorReporting()) {
					ostr.WriteLine("      cc_gen++;");
					if (CSharpCCGlobals.cc2index != 0) {
						ostr.WriteLine("      if (++cc_gc > 100) {");
						ostr.WriteLine("        cc_gc = 0;");
						ostr.WriteLine("        for (int i = 0; i < cc_2_rtns.length; i++) {");
						ostr.WriteLine("          CCCalls c = cc_2_rtns[i];");
						ostr.WriteLine("          while (c != null) {");
						ostr.WriteLine("            if (c.gen < cc_gen) c.first = null;");
						ostr.WriteLine("            c = c.next;");
						ostr.WriteLine("          }");
						ostr.WriteLine("        }");
						ostr.WriteLine("      }");
					}
				}
				if (Options.getDebugParser()) {
					ostr.WriteLine("      trace_token(token, \"\");");
				}
				ostr.WriteLine("      return token;");
				ostr.WriteLine("    }");
				if (Options.getCacheTokens()) {
					ostr.WriteLine("    cc_nt = token;");
				}
				ostr.WriteLine("    token = oldToken;");
				if (Options.getErrorReporting()) {
					ostr.WriteLine("    cc_kind = kind;");
				}
				ostr.WriteLine("    throw GenerateParseException();");
				ostr.WriteLine("  }");
				ostr.WriteLine("");
				if (CSharpCCGlobals.cc2index != 0) {
					ostr.WriteLine("  private sealed class LookaheadSuccess : System.Exception { }");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "readonly private LookaheadSuccess cc_ls = new LookaheadSuccess();");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private bool cc_scan_token(int kind) {");
					ostr.WriteLine("    if (cc_scanpos == cc_lastpos) {");
					ostr.WriteLine("      cc_la--;");
					ostr.WriteLine("      if (cc_scanpos.Next == null) {");
					ostr.WriteLine("        cc_lastpos = cc_scanpos = cc_scanpos.Next = tokenSource.GetNextToken();");
					ostr.WriteLine("      } else {");
					ostr.WriteLine("        cc_lastpos = cc_scanpos = cc_scanpos.Next;");
					ostr.WriteLine("      }");
					ostr.WriteLine("    } else {");
					ostr.WriteLine("      cc_scanpos = cc_scanpos.Next;");
					ostr.WriteLine("    }");
					if (Options.getErrorReporting()) {
						ostr.WriteLine("    if (cc_rescan) {");
						ostr.WriteLine("      int i = 0; Token tok = token;");
						ostr.WriteLine("      while (tok != null && tok != cc_scanpos) { i++; tok = tok.next; }");
						ostr.WriteLine("      if (tok != null) cc_add_error_token(kind, i);");
						if (Options.getDebugLookahead()) {
							ostr.WriteLine("    } else {");
							ostr.WriteLine("      trace_scan(cc_scanpos, kind);");
						}
						ostr.WriteLine("    }");
					} else if (Options.getDebugLookahead()) {
						ostr.WriteLine("    trace_scan(cc_scanpos, kind);");
					}
					ostr.WriteLine("    if (cc_scanpos.kind != kind) return true;");
					ostr.WriteLine("    if (cc_la == 0 && cc_scanpos == cc_lastpos) throw cc_ls;");
					ostr.WriteLine("    return false;");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
				}
				ostr.WriteLine("");
				ostr.WriteLine("/** Get the next Token. */");
				ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + " public Token GetNextToken() {");
				if (Options.getCacheTokens()) {
					ostr.WriteLine("    if ((token = cc_nt).Next != null) cc_nt = cc_nt.next;");
					ostr.WriteLine("    else cc_nt = cc_nt.Next = tokenSource.GetNextToken();");
				} else {
					ostr.WriteLine("    if (token.Next != null) token = token.Next;");
					ostr.WriteLine("    else token = token.Next = tokenSource.GetNextToken();");
					ostr.WriteLine("    cc_ntk = -1;");
				}
				if (Options.getErrorReporting()) {
					ostr.WriteLine("    cc_gen++;");
				}
				if (Options.getDebugParser()) {
					ostr.WriteLine("      trace_token(token, \" (in GetNextToken)\");");
				}
				ostr.WriteLine("    return token;");
				ostr.WriteLine("  }");
				ostr.WriteLine("");
				ostr.WriteLine("/** Get the specific Token. */");
				ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + " public Token GetToken(int index) {");
				if (CSharpCCGlobals.lookaheadNeeded) {
					ostr.WriteLine("    Token t = cc_lookingAhead ? cc_scanpos : token;");
				} else {
					ostr.WriteLine("    Token t = token;");
				}
				ostr.WriteLine("    for (int i = 0; i < index; i++) {");
				ostr.WriteLine("      if (t.Next != null) t = t.Next;");
				ostr.WriteLine("      else t = t.next = tokenSource.GetNextToken();");
				ostr.WriteLine("    }");
				ostr.WriteLine("    return t;");
				ostr.WriteLine("  }");
				ostr.WriteLine("");
				if (!Options.getCacheTokens()) {
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int cc_ntk() {");
					ostr.WriteLine("    if ((cc_nt=token.next) == null)");
					ostr.WriteLine("      return (cc_ntk = (token.Next = tokenSource.GetNextToken()).Kind);");
					ostr.WriteLine("    else");
					ostr.WriteLine("      return (cc_ntk = cc_nt.kind);");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
				}
				if (Options.getErrorReporting()) {
					if (!Options.getGenerateGenerics())
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private System.Collections.IList cc_expentries = new System.Collections.ArrayList();");
					else
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private System.Collections.Generic.IList<int[]> cc_expentries = new System.Collections.Generic.List<int[]>();");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int[] cc_expentry;");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int cc_kind = -1;");
					if (CSharpCCGlobals.cc2index != 0) {
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int[] cc_lasttokens = new int[100];");
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int cc_endpos;");
						ostr.WriteLine("");
						ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private void cc_add_error_token(int kind, int pos) {");
						ostr.WriteLine("    if (pos >= 100) return;");
						ostr.WriteLine("    if (pos == cc_endpos + 1) {");
						ostr.WriteLine("      cc_lasttokens[cc_endpos++] = kind;");
						ostr.WriteLine("    } else if (cc_endpos != 0) {");
						ostr.WriteLine("      cc_expentry = new int[cc_endpos];");
						ostr.WriteLine("      for (int i = 0; i < cc_endpos; i++) {");
						ostr.WriteLine("        cc_expentry[i] = cc_lasttokens[i];");
						ostr.WriteLine("      }");
						ostr.WriteLine("      foreach (int[] oldentry in cc_expentries) {");
						ostr.WriteLine("        if (oldentry.length == cc_expentry.length) {");
						ostr.WriteLine("          for (int i = 0; i < cc_expentry.length; i++) {");
						ostr.WriteLine("            if (oldentry[i] != cc_expentry[i]) {");
						ostr.WriteLine("              goto cc_entries_loop;");
						ostr.WriteLine("            }");
						ostr.WriteLine("          }");
						ostr.WriteLine("          cc_expentries.add(cc_expentry);");
						ostr.WriteLine("          goto cc_entries_loop;");
						ostr.WriteLine("        }");
						ostr.WriteLine("      }");
						ostr.WriteLine("      cc_entries_loop:;");
						ostr.WriteLine("      if (pos != 0) cc_lasttokens[(cc_endpos = pos) - 1] = kind;");
						ostr.WriteLine("    }");
						ostr.WriteLine("  }");
					}
					ostr.WriteLine("");
					ostr.WriteLine("  /** Generate ParseException. */");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public ParseException GenerateParseException() {");
					ostr.WriteLine("    cc_expentries.clear();");
					ostr.WriteLine("    bool[] la1tokens = new bool[" + CSharpCCGlobals.tokenCount + "];");
					ostr.WriteLine("    if (cc_kind >= 0) {");
					ostr.WriteLine("      la1tokens[cc_kind] = true;");
					ostr.WriteLine("      cc_kind = -1;");
					ostr.WriteLine("    }");
					ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.maskindex + "; i++) {");
					ostr.WriteLine("      if (cc_la1[i] == cc_gen) {");
					ostr.WriteLine("        for (int j = 0; j < 32; j++) {");
					for (int i = 0; i < (CSharpCCGlobals.tokenCount - 1) / 32 + 1; i++) {
						ostr.WriteLine("          if ((cc_la1_" + i + "[i] & (1<<j)) != 0) {");
						ostr.Write("            la1tokens[");
						if (i != 0) {
							ostr.Write((32 * i) + "+");
						}
						ostr.WriteLine("j] = true;");
						ostr.WriteLine("          }");
					}
					ostr.WriteLine("        }");
					ostr.WriteLine("      }");
					ostr.WriteLine("    }");
					ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.tokenCount + "; i++) {");
					ostr.WriteLine("      if (la1tokens[i]) {");
					ostr.WriteLine("        cc_expentry = new int[1];");
					ostr.WriteLine("        cc_expentry[0] = i;");
					ostr.WriteLine("        cc_expentries.add(cc_expentry);");
					ostr.WriteLine("      }");
					ostr.WriteLine("    }");
					if (CSharpCCGlobals.cc2index != 0) {
						ostr.WriteLine("    cc_endpos = 0;");
						ostr.WriteLine("    cc_rescan_token();");
						ostr.WriteLine("    cc_add_error_token(0, 0);");
					}
					ostr.WriteLine("    int[][] exptokseq = new int[cc_expentries.Count][];");
					ostr.WriteLine("    for (int i = 0; i < cc_expentries.Count; i++) {");
					if (!Options.getGenerateGenerics())
						ostr.WriteLine("      exptokseq[i] = (int[])cc_expentries[i];");
					else
						ostr.WriteLine("      exptokseq[i] = cc_expentries[i];");
					ostr.WriteLine("    }");
					ostr.WriteLine("    return new ParseException(token, exptokseq, tokenImage);");
					ostr.WriteLine("  }");
				} else {
					ostr.WriteLine("  /** Generate ParseException. */");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public ParseException GenerateParseException() {");
					ostr.WriteLine("    Token errortok = token.Next;");
					if (Options.getKeepLineColumn())
						ostr.WriteLine("    int line = errortok.BeginLine, column = errortok.BeginColumn;");
					ostr.WriteLine("    string mess = (errortok.Kind == 0) ? tokenImage[0] : errortok.Image;");
					if (Options.getKeepLineColumn())
						ostr.WriteLine("    return new ParseException(" +
							"\"Parse error at line \" + line + \", column \" + column + \".  " +
							"Encountered: \" + mess);");
					else
						ostr.WriteLine("    return new ParseException(\"Parse error at <unknown location>.  " +
								"Encountered: \" + mess);");
					ostr.WriteLine("  }");
				}
				ostr.WriteLine("");

				if (Options.getDebugParser()) {
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private int trace_indent = 0;");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private bool trace_enabled = true;");
					ostr.WriteLine("");
					ostr.WriteLine("/** Enable tracing. */");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + " public void enable_tracing() {");
					ostr.WriteLine("    trace_enabled = true;");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
					ostr.WriteLine("/** Disable tracing. */");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + " public void disable_tracing() {");
					ostr.WriteLine("    trace_enabled = false;");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private void trace_call(string s) {");
					ostr.WriteLine("    if (trace_enabled) {");
					ostr.WriteLine("      for (int i = 0; i < trace_indent; i++) { Console.Out.Write(\" \"); }");
					ostr.WriteLine("      Console.Out.WriteLine(\"Call:   \" + s);");
					ostr.WriteLine("    }");
					ostr.WriteLine("    trace_indent = trace_indent + 2;");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private void trace_return(string s) {");
					ostr.WriteLine("    trace_indent = trace_indent - 2;");
					ostr.WriteLine("    if (trace_enabled) {");
					ostr.WriteLine("      for (int i = 0; i < trace_indent; i++) { Console.Out.Write(\" \"); }");
					ostr.WriteLine("      Console.Out.WriteLine(\"Return: \" + s);");
					ostr.WriteLine("    }");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private void trace_token(Token t, string loc) {");
					ostr.WriteLine("    if (trace_enabled) {");
					ostr.WriteLine("      for (int i = 0; i < trace_indent; i++) { Console.Out.Write(\" \"); }");
					ostr.WriteLine("      Console.Out.Write(\"Consumed token: <\" + tokenImage[t.Kind]);");
					ostr.WriteLine("      if (t.kind != 0 && !tokenImage[t.Kind].Equals(\"\\\"\" + t.Image + \"\\\"\")) {");
					ostr.WriteLine("        Console.Out.Write(\": \\\"\" + t.Image + \"\\\"\");");
					ostr.WriteLine("      }");
					ostr.WriteLine("      Console.Out.WriteLine(\" at line \" + t.BeginLine + " +
							"\" column \" + t.BeginColumn + \">\" + loc);");
					ostr.WriteLine("    }");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private void trace_scan(Token t1, int t2) {");
					ostr.WriteLine("    if (trace_enabled) {");
					ostr.WriteLine("      for (int i = 0; i < trace_indent; i++) { Console.Out.Write(\" \"); }");
					ostr.WriteLine("      Console.Out.Write(\"Visited token: <\" + tokenImage[t1.kind]);");
					ostr.WriteLine("      if (t1.Kind != 0 && !tokenImage[t1.Kind].Equals(\"\\\"\" + t1.Image + \"\\\"\")) {");
					ostr.WriteLine("        Console.Out.Write(\": \\\"\" + t1.Image + \"\\\"\");");
					ostr.WriteLine("      }");
					ostr.WriteLine("      Console.Out.WriteLine(\" at line \" + t1.BeginLine + \"" +
							" column \" + t1.BeginColumn + \">; Expected token: <\" + tokenImage[t2] + \">\");");
					ostr.WriteLine("    }");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
				} else {
					ostr.WriteLine("  /** Enable tracing. */");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public void enable_tracing() {");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
					ostr.WriteLine("  /** Disable tracing. */");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "public void disable_tracing() {");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
				}

				if (CSharpCCGlobals.cc2index != 0 && Options.getErrorReporting()) {
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private void cc_rescan_token() {");
					ostr.WriteLine("    cc_rescan = true;");
					ostr.WriteLine("    for (int i = 0; i < " + CSharpCCGlobals.cc2index + "; i++) {");
					ostr.WriteLine("    try {");
					ostr.WriteLine("      CCCalls p = cc_2_rtns[i];");
					ostr.WriteLine("      do {");
					ostr.WriteLine("        if (p.gen > cc_gen) {");
					ostr.WriteLine("          cc_la = p.arg; cc_lastpos = cc_scanpos = p.first;");
					ostr.WriteLine("          switch (i) {");
					for (int i = 0; i < CSharpCCGlobals.cc2index; i++) {
						ostr.WriteLine("            case " + i + ": cc_3_" + (i + 1) + "(); break;");
					}
					ostr.WriteLine("          }");
					ostr.WriteLine("        }");
					ostr.WriteLine("        p = p.next;");
					ostr.WriteLine("      } while (p != null);");
					ostr.WriteLine("      } catch(LookaheadSuccess) { }");
					ostr.WriteLine("    }");
					ostr.WriteLine("    cc_rescan = false;");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
					ostr.WriteLine("  " + CSharpCCGlobals.staticOpt() + "private void cc_save(int index, int xla) {");
					ostr.WriteLine("    CCCalls p = cc_2_rtns[index];");
					ostr.WriteLine("    while (p.gen > cc_gen) {");
					ostr.WriteLine("      if (p.next == null) { p = p.next = new CCCalls(); break; }");
					ostr.WriteLine("      p = p.next;");
					ostr.WriteLine("    }");
					ostr.WriteLine("    p.gen = cc_gen + xla - cc_la; p.first = token; p.arg = xla;");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
				}

				if (CSharpCCGlobals.cc2index != 0 && Options.getErrorReporting()) {
					ostr.WriteLine("  sealed class CCCalls {");
					ostr.WriteLine("    public int gen;");
					ostr.WriteLine("    public  Token first;");
					ostr.WriteLine("    public int arg;");
					ostr.WriteLine("    public CCCalls next;");
					ostr.WriteLine("  }");
					ostr.WriteLine("");
				}

				if (CSharpCCGlobals.cu_from_insertion_point_2.Count != 0) {
					CSharpCCGlobals.PrintTokenSetup(CSharpCCGlobals.cu_from_insertion_point_2[0]); 
					CSharpCCGlobals.ccol = 1;
					foreach (Token token in CSharpCCGlobals.cu_from_insertion_point_2) {
						t = token;
						CSharpCCGlobals.PrintToken(t, ostr);
					}
					CSharpCCGlobals.PrintTrailingComments(t, ostr);
				}
				ostr.WriteLine("");

				ostr.Close();

			} // matches "if (Options.getBuildParser())"

		}

		public static void reInit() {
			ostr = null;
			CSharpCCGlobals.lookaheadNeeded = false;
		}
	}
}