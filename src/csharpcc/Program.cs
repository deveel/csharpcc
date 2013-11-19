using System;
using System.IO;
using System.Security;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	internal class Program {
		private static void help_message() {
			Console.Out.WriteLine("Usage:");
			Console.Out.WriteLine("    csharpcc option-settings inputfile");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("\"option-settings\" is a sequence of settings separated by spaces.");
			Console.Out.WriteLine("Each option setting must be of one of the following forms:");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("    -optionname=value (e.g., -STATIC=false)");
			Console.Out.WriteLine("    -optionname:value (e.g., -STATIC:false)");
			Console.Out.WriteLine("    -optionname       (equivalent to -optionname=true.  e.g., -STATIC)");
			Console.Out.WriteLine("    -NOoptionname     (equivalent to -optionname=false. e.g., -NOSTATIC)");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("Option settings are not case-sensitive, so one can say \"-nOsTaTiC\" instead");
			Console.Out.WriteLine("of \"-NOSTATIC\".  Option values must be appropriate for the corresponding");
			Console.Out.WriteLine("option, and must be either an integer, a boolean, or a string value.");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("The integer valued options are:");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("    LOOKAHEAD              (default 1)");
			Console.Out.WriteLine("    CHOICE_AMBIGUITY_CHECK (default 2)");
			Console.Out.WriteLine("    OTHER_AMBIGUITY_CHECK  (default 1)");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("The boolean valued options are:");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("    STATIC                 (default true)");
			Console.Out.WriteLine("    SUPPORT_CLASS_VISIBILITY_PUBLIC (default true)");
			Console.Out.WriteLine("    DEBUG_PARSER           (default false)");
			Console.Out.WriteLine("    DEBUG_LOOKAHEAD        (default false)");
			Console.Out.WriteLine("    DEBUG_TOKEN_MANAGER    (default false)");
			Console.Out.WriteLine("    ERROR_REPORTING        (default true)");
			Console.Out.WriteLine("    UNICODE_ESCAPE         (default false)");
			Console.Out.WriteLine("    UNICODE_INPUT          (default false)");
			Console.Out.WriteLine("    IGNORE_CASE            (default false)");
			Console.Out.WriteLine("    COMMON_TOKEN_ACTION    (default false)");
			Console.Out.WriteLine("    USER_TOKEN_MANAGER     (default false)");
			Console.Out.WriteLine("    USER_CHAR_STREAM       (default false)");
			Console.Out.WriteLine("    BUILD_PARSER           (default true)");
			Console.Out.WriteLine("    BUILD_TOKEN_MANAGER    (default true)");
			Console.Out.WriteLine("    TOKEN_MANAGER_USES_PARSER (default false)");
			Console.Out.WriteLine("    SANITY_CHECK           (default true)");
			Console.Out.WriteLine("    FORCE_LA_CHECK         (default false)");
			Console.Out.WriteLine("    CACHE_TOKENS           (default false)");
			Console.Out.WriteLine("    KEEP_LINE_COLUMN       (default true)");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("The string valued options are:");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("    OUTPUT_DIRECTORY       (default Current Directory)");
			Console.Out.WriteLine("    TOKEN_EXTENDS          (default System.Object)");
			Console.Out.WriteLine("    TOKEN_FACTORY          (default none)");
			Console.Out.WriteLine("    CLR_VERSION            (default 2.0)");
			Console.Out.WriteLine("    GRAMMAR_ENCODING       (defaults to platform file encoding)");
			Console.Out.WriteLine("");
			Console.Out.WriteLine("EXAMPLE:");
			Console.Out.WriteLine("    csharpcc -STATIC=false -LOOKAHEAD:2 -debug_parser mygrammar.cc");
			Console.Out.WriteLine("");
		}


		private static void Main(string[] args) {
			int errorcode = MainProgram(args);
			Environment.Exit(errorcode);
		}

		public static int MainProgram(String[] args) {

			// Initialize all static state
			ReInitAll();

			CSharpCCGlobals.BannerLine("Parser Generator", "");

			CSharpCCParser parser = null;
			if (args.Length == 0) {
				Console.Out.WriteLine("");
				help_message();
				return 1;
			} else {
				Console.Out.WriteLine("(type \"javacc\" with no arguments for help)");
			}

			if (Options.IsOption(args[args.Length - 1])) {
				Console.Out.WriteLine("Last argument \"" + args[args.Length - 1] + "\" is not a filename.");
				return 1;
			}
			for (int arg = 0; arg < args.Length - 1; arg++) {
				if (!Options.IsOption(args[arg])) {
					Console.Out.WriteLine("Argument \"" + args[arg] + "\" must be an option setting.");
					return 1;
				}
				Options.SetCmdLineOption(args[arg]);
			}

			try {
				FileInfo fp = new FileInfo(args[args.Length - 1]);
				if (!fp.Exists) {
					Console.Out.WriteLine("File " + args[args.Length - 1] + " not found.");
					return 1;
				}
				parser =
					new CSharpCCParser(
						new StreamReader(new FileStream(args[args.Length - 1], FileMode.Open, FileAccess.Read, FileShare.Read),
						                 Encoding.GetEncoding(Options.getGrammarEncoding())));
			} catch (SecurityException) {
				Console.Out.WriteLine("Security violation while trying to open " + args[args.Length - 1]);
				return 1;
			} catch (FileNotFoundException) {
				Console.Out.WriteLine("File " + args[args.Length - 1] + " not found.");
				return 1;
			}

			try {
				Console.Out.WriteLine("Reading from file " + args[args.Length - 1] + " . . .");
				CSharpCCGlobals.FileName = CSharpCCGlobals.OriginalFileName = args[args.Length - 1];
				CSharpCCGlobals.TreeGenerated = CSharpCCGlobals.IsGeneratedBy("CSTree", args[args.Length - 1]);
				CSharpCCGlobals.ToolNames = CSharpCCGlobals.GetToolNames(args[args.Length - 1]);
				parser.csharpcc_input();
				CSharpCCGlobals.CreateOutputDir(Options.getOutputDirectory().FullName);

				if (Options.getUnicodeInput()) {
					NfaState.unicodeWarningGiven = true;
					Console.Out.WriteLine("Note: UNICODE_INPUT option is specified. " +
					                      "Please make sure you create the parser/lexer using a Reader with the correct character encoding.");
				}

				Semanticize.start();
				ParseGen.start();
				LexGen.start();
				OtherFilesGen.start();

				if ((CSharpCCErrors.ErrorCount == 0) && (Options.getBuildParser() || Options.getBuildTokenManager())) {
					if (CSharpCCErrors.WarningCount == 0) {
						Console.Out.WriteLine("Parser generated successfully.");
					} else {
						Console.Out.WriteLine("Parser generated with 0 errors and "
						                      + CSharpCCErrors.WarningCount + " warnings.");
					}
					return 0;
				} else {
					Console.Out.WriteLine("Detected " + CSharpCCErrors.ErrorCount + " errors and "
					                      + CSharpCCErrors.WarningCount + " warnings.");
					return (CSharpCCErrors.ErrorCount == 0) ? 0 : 1;
				}
			} catch (MetaParseException e) {
				Console.Out.WriteLine("Detected " + CSharpCCErrors.ErrorCount + " errors and "
				                      + CSharpCCErrors.WarningCount + " warnings.");
				return 1;
			} catch (ParseException e) {
				Console.Out.WriteLine(e.ToString());
				Console.Out.WriteLine("Detected " + (CSharpCCErrors.ErrorCount + 1) + " errors and "
				                      + CSharpCCErrors.WarningCount + " warnings.");
				return 1;
			}
		}

		private static void ReInitAll() {
			Expansion.reInit();
			CSharpCCErrors.ReInit();
			CSharpCCGlobals.ReInit();
			Options.init();
			CSharpCCParserInternals.reInit();
			RStringLiteral.reInit();
			// CSharpFiles.reInit();
			LexGen.reInit();
			NfaState.reInit();
			//TODO: MatchInfo.reInit();
			//TODO: LookaheadWalk.reInit();
			Semanticize.reInit();
			ParseGen.reInit();
			OtherFilesGen.reInit();
			//TODO: ParseEngine.reInit();

		}
	}
}