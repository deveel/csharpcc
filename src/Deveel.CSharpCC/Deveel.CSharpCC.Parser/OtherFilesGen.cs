using System;
using System.Collections.Generic;
using System.IO;

namespace Deveel.CSharpCC.Parser {
	public class OtherFilesGen {
		private static TextWriter ostr;
		public static bool keepLineCol;

		public static void start() {
			Token t = null;
			keepLineCol = Options.getKeepLineColumn();

			if (CSharpCCErrors.ErrorCount != 0)
				throw new MetaParseException();

			CSharpFiles.GenerateTokenManagerError();
			CSharpFiles.GenerateParseException();
			CSharpFiles.GenerateToken();
			if (Options.getUserTokenManager()) {
				CSharpFiles.GenerateITokenManager();
			} else if (Options.getUserCharStream()) {
				CSharpFiles.GenerateICharStream();
			} else {
				if (Options.getUnicodeEscape()) {
					CSharpFiles.GenerateUnicodeCharStream();
				} else {
					CSharpFiles.GenerateSimpleCharStream();
				}
			}

			try {
				ostr =
					new StreamWriter(
						new BufferedStream(
							new FileStream(Path.Combine(Options.getOutputDirectory().FullName, CSharpCCGlobals.cu_name + "Constants.cs"),
							               FileMode.OpenOrCreate, FileAccess.Write), 8192));
			} catch (IOException) {
				CSharpCCErrors.SemanticError("Could not open file " + CSharpCCGlobals.cu_name + "Constants.cs for writing.");
				throw new InvalidOperationException();
			}

			List<string> tn = new List<string>(CSharpCCGlobals.ToolNames);
			tn.Add(CSharpCCGlobals.ToolName);
			ostr.WriteLine("/* " + CSharpCCGlobals.GetIdString(tn, CSharpCCGlobals.cu_name + "Constants.cs") + " */");

            bool namespaceInserted = false;
			if (CSharpCCGlobals.cu_to_insertion_point_1.Count != 0 &&
			    CSharpCCGlobals. cu_to_insertion_point_1[0].kind == CSharpCCParserConstants.NAMESPACE) {
                namespaceInserted = true;
				for (int i = 1; i < CSharpCCGlobals.cu_to_insertion_point_1.Count; i++) {
					if (CSharpCCGlobals.cu_to_insertion_point_1[i].kind == CSharpCCParserConstants.SEMICOLON) {
						CSharpCCGlobals.PrintTokenSetup(CSharpCCGlobals.cu_to_insertion_point_1[0]);
						for (int j = 0; j <= i; j++) {
							t = (CSharpCCGlobals.cu_to_insertion_point_1[j]);
                            if (t.kind != CSharpCCParserConstants.SEMICOLON)
							    CSharpCCGlobals.PrintToken(t, ostr);
						}
						CSharpCCGlobals.PrintTrailingComments(t, ostr);
						break;
					}
				}

                ostr.WriteLine("{");
			}
			ostr.WriteLine("");
			ostr.WriteLine("/// <summary>");
			ostr.WriteLine("/// Token literal values and constants.");
			ostr.WriteLine("/// <summary>");
			if (Options.getSupportClassVisibilityPublic()) {
				ostr.Write("public ");
			}
			ostr.WriteLine("class " + CSharpCCGlobals.cu_name + "Constants {");
			ostr.WriteLine("");
			ostr.WriteLine("  /// <summary> End of File</summary>");
			ostr.WriteLine("  public const int EOF = 0;");

			foreach (RegularExpression re in CSharpCCGlobals.ordered_named_tokens) {
				ostr.WriteLine("  /// <summary>RegularExpression Id.</summary>");
				ostr.WriteLine("  public const int " + re.Label + " = " + re.Ordinal + ";");
			}
			ostr.WriteLine("");
			if (!Options.getUserTokenManager() && Options.getBuildTokenManager()) {
				for (int i = 0; i < LexGen.lexStateName.Length; i++) {
					ostr.WriteLine("  /// <summary>Lexical state.</summary>");
					ostr.WriteLine("  public const int " + LexGen.lexStateName[i] + " = " + i + ";");
				}
				ostr.WriteLine("");
			}
			ostr.WriteLine("  /// <summary>Literal token values.</summary>");
			ostr.WriteLine("  public static readonly string[] TokenImage = {");
			ostr.WriteLine("    \"<EOF>\",");

			foreach (TokenProduction tp in CSharpCCGlobals.rexprlist) {
				IList<RegExprSpec> respecs = tp.RegexSpecs;
				foreach (RegExprSpec res in respecs) {
					RegularExpression re = res.RegularExpression;
					if (re is RStringLiteral)
					{
						ostr.WriteLine("    \"\\\"" + CSharpCCGlobals.AddEscapes(CSharpCCGlobals.AddEscapes(((RStringLiteral) re).Image)) + "\\\"\",");
					}
				else
					if (!re.Label.Equals("")) {
						ostr.WriteLine("    \"<" + re.Label + ">\",");
					} else {
						if (re.TokenProductionContext.Kind == TokenProduction.TOKEN) {
							CSharpCCErrors.Warning(re, "Consider giving this non-string token a label for better error reporting.");
						}
						ostr.WriteLine("    \"<token of kind " + re.Ordinal + ">\",");
					}

				}
			}
			ostr.WriteLine("  };");
			ostr.WriteLine("");
			ostr.WriteLine("}");
            if (namespaceInserted)
                ostr.WriteLine("}");
			ostr.Close();
		}

		public static void reInit() {
			ostr = null;
		}
	}
}