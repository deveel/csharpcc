using System;
using System.IO;
using System.Text;

using NUnit.Framework;

namespace Deveel.CSharpCC.Parser {
	[TestFixture]
	public class GenerateParserTest {
		[SetUp]
		public void SetUp() {
			DeleteFiles();
		}

		private void DeleteFiles() {
			// Initialize all static state
			ReInitAll();

			DeleteFile("SimpleParser.cs");
			DeleteFile("SimpleParserConstants.cs");
			DeleteFile("SimpleParserTokenManager.cs");
			DeleteFile("TokenManagerError.cs");
			DeleteFile("Token.cs");
			DeleteFile("ParseException.cs");
		}

		private void ReInitAll() {
			Expansion.reInit();
			CSharpCCErrors.ReInit();
			CSharpCCGlobals.ReInit();
			Options.init();
			CSharpCCParserInternals.reInit();
			RStringLiteral.reInit();
			// CSharpFiles.reInit();
			LexGen.reInit();
			NfaState.reInit();
			MatchInfo.reInit();
			LookaheadWalk.reInit();
			Semanticize.reInit();
			ParseGen.reInit();
			OtherFilesGen.reInit();
			ParseEngine.reInit();
		}

		private void DeleteFile(string fileName) {
			var path = Path.Combine(Environment.CurrentDirectory, fileName);
			if (File.Exists(path))
				File.Delete(path);
		}

		[TearDown]
		public void TearDown() {
			
		}

		[Test]
		public void GenerateNoErrors() {
			var input = MakeUpGrammar();
			SetupOptions();

			using (var reader = new StringReader(input)) {
				var parser = new CSharpCCParser(reader);

				CSharpCCGlobals.FileName = CSharpCCGlobals.OriginalFileName = "SimpleParser.cc";
				parser.csharpcc_input();
				CSharpCCGlobals.CreateOutputDir(Options.getOutputDirectory().FullName);

				Semanticize.start();
				ParseGen.start();
				LexGen.start();
				OtherFilesGen.start();
			}

			Assert.AreEqual(0, CSharpCCErrors.ErrorCount);
		}

		private void SetupOptions() {
			Options.SetCmdLineOption("STATIC=false");
		}

		private string MakeUpGrammar() {
			var sb = new StringBuilder();
			sb.AppendLine("PARSER_BEGIN(SimpleParser)");
			sb.AppendLine("namespace Deveel.CSharpCC.Parser;");
			sb.AppendLine();
			sb.AppendLine("using System;");
			sb.AppendLine();
			sb.AppendLine("public class SimpleParser {");
			sb.AppendLine("}");
			sb.AppendLine();
			sb.AppendLine("PARSER_END(SimpleParser)");
			sb.AppendLine();
			sb.AppendLine("TOKEN: {");
			sb.AppendLine("< READ: \"read\" > |");
			sb.AppendLine("< AND: \"and\" > |");
			sb.AppendLine("< PRINT: \"print\" >");
			sb.AppendLine("}");
			sb.AppendLine();
			sb.AppendLine("SKIP: {");
			sb.AppendLine("\" \" |");
			sb.AppendLine("\"\\t\"");
			sb.AppendLine("}");
			sb.AppendLine();
			sb.AppendLine("MORE: {");
			sb.AppendLine("\"/*\" : IN_MULTI_LINE_COMMENT");
			sb.AppendLine("}");
			sb.AppendLine();
			sb.AppendLine("<IN_MULTI_LINE_COMMENT>");
			sb.AppendLine("SPECIAL_TOKEN: {");
			sb.AppendLine("<MULTI_LINE_COMMENT: \"*/\" > : DEFAULT");
			sb.AppendLine("}");
			sb.AppendLine();
			sb.AppendLine("TOKEN: {");
			sb.AppendLine("< STRING_LITERAL: \"'\" ( \"''\" | \"\\\\\" [\"a\"-\"z\", \"\\\\\", \"%\", \"_\", \"'\"] | ~[\"'\",\"\\\\\"] )* \"'\" >");
			sb.AppendLine("}");
			sb.AppendLine();
			sb.AppendLine("void Input() :");
			sb.AppendLine("{ Token t; string line; }");
			sb.AppendLine("{");
			sb.AppendLine("\"READ\" \"AND\" \"PRINT\" t = <STRING_LITERAL> { line = t.Image; }");
			sb.AppendLine("{ Console.Out.WriteLine(line); }");
			sb.AppendLine("}");
			return sb.ToString();
		}
	}
}
