using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Deveel.CSharpCC.Parser {
    public class Options {


        /**
   * Limit subclassing to derived classes.
   */

        protected Options() {
        }

        /**
   * A mapping of option names (Strings) to values (Integer, Boolean, String).
   * This table is initialized by the main program. Its contents defines the
   * set of legal options. Its initial values define the default option
   * values, and the option types can be determined from these values too.
   */
        protected static IDictionary optionValues = null;

        /**
   * Convenience method to retrieve integer options.
   */

        protected static int intValue(String option) {
            return (int) optionValues[option];
        }

        /**
   * Convenience method to retrieve bool options.
   */

        protected static bool booleanValue(String option) {
            return (bool) optionValues[option];
        }

        /**
   * Convenience method to retrieve string options.
   */

        protected static String stringValue(String option) {
            return (String) optionValues[option];
        }

        public static IDictionary getOptions() {
            return new Hashtable(optionValues);
        }

        /**
   * Keep track of what options were set as a command line argument. We use
   * this to see if the options set from the command line and the ones set in
   * the input files clash in any way.
   */
        private static IList cmdLineSetting = null;

        /**
   * Keep track of what options were set from the grammar file. We use this to
   * see if the options set from the command line and the ones set in the
   * input files clash in any way.
   */
        private static IList inputFileSetting = null;

        /**
   * Initialize for JavaCC
   */

        public static void init() {
            optionValues = new Hashtable();
            cmdLineSetting = new ArrayList();
            inputFileSetting = new ArrayList();

            optionValues.Add("LOOKAHEAD", 1);
            optionValues.Add("CHOICE_AMBIGUITY_CHECK", 2);
            optionValues.Add("OTHER_AMBIGUITY_CHECK", 1);

            optionValues.Add("STATIC", true);
            optionValues.Add("DEBUG_PARSER", false);
            optionValues.Add("DEBUG_LOOKAHEAD", false);
            optionValues.Add("DEBUG_TOKEN_MANAGER", false);
            optionValues.Add("ERROR_REPORTING", true);
            optionValues.Add("JAVA_UNICODE_ESCAPE", false);
            optionValues.Add("UNICODE_INPUT", false);
            optionValues.Add("IGNORE_CASE", false);
            optionValues.Add("USER_TOKEN_MANAGER", false);
            optionValues.Add("USER_CHAR_STREAM", false);
            optionValues.Add("BUILD_PARSER", true);
            optionValues.Add("BUILD_TOKEN_MANAGER", true);
            optionValues.Add("TOKEN_MANAGER_USES_PARSER", false);
            optionValues.Add("SANITY_CHECK", true);
            optionValues.Add("FORCE_LA_CHECK", false);
            optionValues.Add("COMMON_TOKEN_ACTION", false);
            optionValues.Add("CACHE_TOKENS", false);
            optionValues.Add("KEEP_LINE_COLUMN", true);

            optionValues.Add("GENERATE_CHAINED_EXCEPTION", false);
            optionValues.Add("GENERATE_GENERICS", false);
            optionValues.Add("GENERATE_STRING_BUILDER", false);
            optionValues.Add("GENERATE_ANNOTATIONS", false);
            optionValues.Add("SUPPORT_CLASS_VISIBILITY_PUBLIC", true);

            optionValues.Add("OUTPUT_DIRECTORY", ".");
            optionValues.Add("JDK_VERSION", "1.5");
            optionValues.Add("TOKEN_EXTENDS", "");
            optionValues.Add("TOKEN_FACTORY", "");
            optionValues.Add("GRAMMAR_ENCODING", "");
        }

        /**
   * Returns a string representation of the specified options of interest.
   * Used when, for example, generating Token.java to record the JavaCC options
   * that were used to generate the file. All of the options must be
   * bool values.
   * @param interestingOptions the options of interest, eg {"STATIC", "CACHE_TOKENS"}
   * @return the string representation of the options, eg "STATIC=true,CACHE_TOKENS=false"
   */

        public static String getOptionsString(String[] interestingOptions) {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < interestingOptions.Length; i++) {
                String key = interestingOptions[i];
                sb.Append(key);
                sb.Append('=');
                sb.Append(optionValues[key]);
                if (i != interestingOptions.Length - 1) {
                    sb.Append(',');
                }
            }

            return sb.ToString();
        }


        /**
   * Determine if a given command line argument might be an option flag.
   * Command line options start with a dash&nbsp;(-).
   *
   * @param opt
   *            The command line argument to examine.
   * @return True when the argument looks like an option flag.
   */

        public static bool isOption(String opt) {
            return opt != null && opt.Length > 1 && opt[0] == '-';
        }

        /**
   * Help function to handle cases where the meaning of an option has changed
   * over time. If the user has supplied an option in the old format, it will
   * be converted to the new format.
   *
   * @param name The name of the option being checked.
   * @param value The option's value.
   * @return The upgraded value.
   */

        public static Object upgradeValue(String name, Object value) {
            if (name.Equals("NODE_FACTORY", StringComparison.OrdinalIgnoreCase) && value is bool) {
                if ((bool) value) {
                    value = "*";
                } else {
                    value = "";
                }
            }

            return value;
        }

        public static void setInputFileOption(Object nameloc,
            Object valueloc,
            String name,
            Object value) {
            String s = name.ToUpper();
            if (!optionValues.Contains(s)) {
                CSharpCCErrors.Warning(nameloc,
                    "Bad option name \"" + name
                    + "\".  Option setting will be ignored.");
                return;
            }
            Object existingValue = optionValues[s];

            value = upgradeValue(name, value);

            if (existingValue != null) {
                if ((existingValue.GetType() != value.GetType()) ||
                    (value is int && ((int) value) <= 0)) {
                    CSharpCCErrors.Warning(valueloc,
                        "Bad option value \"" + value
                        + "\" for \"" + name
                        + "\".  Option setting will be ignored.");
                    return;
                }

                if (inputFileSetting.Contains(s)) {
                    CSharpCCErrors.Warning(nameloc,
                        "Duplicate option setting for \""
                        + name + "\" will be ignored.");
                    return;
                }

                if (cmdLineSetting.Contains(s)) {
                    if (!existingValue.Equals(value)) {
                        CSharpCCErrors.Warning(nameloc,
                            "Command line setting of \""
                            + name + "\" modifies option value in file.");
                    }
                    return;
                }
            }

            optionValues.Add(s, value);
            inputFileSetting.Add(s);
        }



        /**
   * Process a single command-line option.
   * The option is parsed and stored in the optionValues map.
   * @param arg
   */

        public static void setCmdLineOption(String arg) {
            String s;

            if (arg[0] == '-') {
                s = arg.Substring(1);
            } else {
                s = arg;
            }

            String name;
            Object Val;

            // Look for the first ":" or "=", which will separate the option name
            // from its value (if any).
            int index1 = s.IndexOf('=');
            int index2 = s.IndexOf(':');
            int index;

            if (index1 < 0)
                index = index2;
            else if (index2 < 0)
                index = index1;
            else if (index1 < index2)
                index = index1;
            else
                index = index2;

            if (index < 0) {
                name = s.ToUpper();
                if (optionValues.Contains(name)) {
                    Val = true;
                } else if (name.Length > 2 && name[0] == 'N' && name[1] == 'O') {
                    Val = false;
                    name = name.Substring(2);
                } else {
                    Console.Out.WriteLine("Warning: Bad option \"" + arg
                                          + "\" will be ignored.");
                    return;
                }
            } else {
                name = s.Substring(0, index).ToUpper();
                if (s.Substring(index + 1).Equals("TRUE", StringComparison.OrdinalIgnoreCase)) {
                    Val = true;
                } else if (s.Substring(index + 1).Equals("FALSE", StringComparison.OrdinalIgnoreCase)) {
                    Val = false;
                } else {
                    try {
                        int i = Int32.Parse(s.Substring(index + 1));
                        if (i <= 0) {
                            Console.Out.WriteLine("Warning: Bad option value in \""
                                                  + arg + "\" will be ignored.");
                            return;
                        }
                        Val = i;
                    } catch (FormatException e) {
                        Val = s.Substring(index + 1);
                        if (s.Length > index + 2) {
                            // i.e., there is space for two '"'s in value
                            if (s[index + 1] == '"' && s[s.Length - 1] == '"') {
                                // remove the two '"'s.
                                Val = s.Substring(index + 2, s.Length - 1);
                            }
                        }
                    }
                }
            }

            if (!optionValues.Contains(name)) {
                Console.Out.WriteLine("Warning: Bad option \"" + arg
                                      + "\" will be ignored.");
                return;
            }
            Object valOrig = optionValues[name];
            if (Val.GetType() != valOrig.GetType()) {
                Console.Out.WriteLine("Warning: Bad option value in \"" + arg
                                      + "\" will be ignored.");
                return;
            }
            if (cmdLineSetting.Contains(name)) {
                Console.Out.WriteLine("Warning: Duplicate option setting \"" + arg
                                      + "\" will be ignored.");
                return;
            }

            Val = upgradeValue(name, Val);

            optionValues.Add(name, Val);
            cmdLineSetting.Add(name);
        }

        public static void normalize() {
            if (getDebugLookahead() && !getDebugParser()) {
                if (cmdLineSetting.Contains("DEBUG_PARSER")
                    || inputFileSetting.Contains("DEBUG_PARSER")) {
                    CSharpCCErrors.Warning("True setting of option DEBUG_LOOKAHEAD overrides " +
                                           "false setting of option DEBUG_PARSER.");
                }
                optionValues.Add("DEBUG_PARSER", true);
            }

            // Now set the "GENERATE" options from the supplied (or default) JDK version.

            optionValues.Add("GENERATE_CHAINED_EXCEPTION", jdkVersionAtLeast(1.4));
            optionValues.Add("GENERATE_GENERICS", jdkVersionAtLeast(1.5));
            optionValues.Add("GENERATE_STRING_BUILDER", jdkVersionAtLeast(1.5));
            optionValues.Add("GENERATE_ANNOTATIONS", jdkVersionAtLeast(1.5));
        }

        /**
   * Find the lookahead setting.
   *
   * @return The requested lookahead value.
   */

        public static int getLookahead() {
            return intValue("LOOKAHEAD");
        }

        /**
   * Find the choice ambiguity check value.
   *
   * @return The requested choice ambiguity check value.
   */

        public static int getChoiceAmbiguityCheck() {
            return intValue("CHOICE_AMBIGUITY_CHECK");
        }

        /**
   * Find the other ambiguity check value.
   *
   * @return The requested other ambiguity check value.
   */

        public static int getOtherAmbiguityCheck() {
            return intValue("OTHER_AMBIGUITY_CHECK");
        }

        /**
   * Find the static value.
   *
   * @return The requested static value.
   */

        public static bool getStatic() {
            return booleanValue("STATIC");
        }

        /**
   * Find the debug parser value.
   *
   * @return The requested debug parser value.
   */

        public static bool getDebugParser() {
            return booleanValue("DEBUG_PARSER");
        }

        /**
   * Find the debug lookahead value.
   *
   * @return The requested debug lookahead value.
   */

        public static bool getDebugLookahead() {
            return booleanValue("DEBUG_LOOKAHEAD");
        }

        /**
   * Find the debug tokenmanager value.
   *
   * @return The requested debug tokenmanager value.
   */

        public static bool getDebugTokenManager() {
            return booleanValue("DEBUG_TOKEN_MANAGER");
        }

        /**
   * Find the error reporting value.
   *
   * @return The requested error reporting value.
   */

        public static bool getErrorReporting() {
            return booleanValue("ERROR_REPORTING");
        }

        /**
   * Find the Java unicode escape value.
   *
   * @return The requested Java unicode escape value.
   */

        public static bool getJavaUnicodeEscape() {
            return booleanValue("JAVA_UNICODE_ESCAPE");
        }

        /**
   * Find the unicode input value.
   *
   * @return The requested unicode input value.
   */

        public static bool getUnicodeInput() {
            return booleanValue("UNICODE_INPUT");
        }

        /**
   * Find the ignore case value.
   *
   * @return The requested ignore case value.
   */

        public static bool getIgnoreCase() {
            return booleanValue("IGNORE_CASE");
        }

        /**
   * Find the user tokenmanager value.
   *
   * @return The requested user tokenmanager value.
   */

        public static bool getUserTokenManager() {
            return booleanValue("USER_TOKEN_MANAGER");
        }

        /**
   * Find the user charstream value.
   *
   * @return The requested user charstream value.
   */

        public static bool getUserCharStream() {
            return booleanValue("USER_CHAR_STREAM");
        }

        /**
   * Find the build parser value.
   *
   * @return The requested build parser value.
   */

        public static bool getBuildParser() {
            return booleanValue("BUILD_PARSER");
        }

        /**
   * Find the build token manager value.
   *
   * @return The requested build token manager value.
   */

        public static bool getBuildTokenManager() {
            return booleanValue("BUILD_TOKEN_MANAGER");
        }

        /**
   * Find the token manager uses parser value.
   *
   * @return The requested token manager uses parser value;
   */

        public static bool getTokenManagerUsesParser() {
            return booleanValue("TOKEN_MANAGER_USES_PARSER");
        }

        /**
   * Find the sanity check value.
   *
   * @return The requested sanity check value.
   */

        public static bool getSanityCheck() {
            return booleanValue("SANITY_CHECK");
        }

        /**
   * Find the force lookahead check value.
   *
   * @return The requested force lookahead value.
   */

        public static bool getForceLaCheck() {
            return booleanValue("FORCE_LA_CHECK");
        }

        /**
   * Find the common token action value.
   *
   * @return The requested common token action value.
   */

        public static bool getCommonTokenAction() {
            return booleanValue("COMMON_TOKEN_ACTION");
        }

        /**
   * Find the cache tokens value.
   *
   * @return The requested cache tokens value.
   */

        public static bool getCacheTokens() {
            return booleanValue("CACHE_TOKENS");
        }

        /**
   * Find the keep line column value.
   *
   * @return The requested keep line column value.
   */

        public static bool getKeepLineColumn() {
            return booleanValue("KEEP_LINE_COLUMN");
        }

        /**
   * Find the JDK version.
   *
   * @return The requested jdk version.
   */

        public static String getJdkVersion() {
            return stringValue("JDK_VERSION");
        }

        /**
   * Should the generated code create Exceptions using a constructor taking a nested exception?
   * @return
   */

        public static bool getGenerateChainedException() {
            return booleanValue("GENERATE_CHAINED_EXCEPTION");
        }

        /**
   * Should the generated code contain Generics?
   * @return
   */

        public static bool getGenerateGenerics() {
            return booleanValue("GENERATE_GENERICS");
        }

        /**
   * Should the generated code use StringBuilder rather than StringBuilder?
   * @return
   */

        public static bool getGenerateStringBuilder() {
            return booleanValue("GENERATE_STRING_BUILDER");
        }

        /**
   * Should the generated code contain Annotations?
   * @return
   */

        public static bool getGenerateAnnotations() {
            return booleanValue("GENERATE_ANNOTATIONS");
        }

        /**
   * Should the generated code class visibility public?
   * @return
   */

        public static bool getSupportClassVisibilityPublic() {
            return booleanValue("SUPPORT_CLASS_VISIBILITY_PUBLIC");
        }

        /**
   * Determine if the output language is at least the specified
   * version.
   * @param version the version to check against. E.g. <code>1.5</code>
   * @return true if the output version is at least the specified version.
   */

        public static bool jdkVersionAtLeast(double version) {
            double jdkVersion = Double.Parse(getJdkVersion());

            // Comparing doubles is safe here, as it is two simple assignments.
            return jdkVersion >= version;
        }

        /**
   * Return the Token's superclass.
   *
   * @return The required base class for Token.
   */

        public static String getTokenExtends() {
            return stringValue("TOKEN_EXTENDS");
        }

        /**
   * Return the Token's factory class.
   *
   * @return The required factory class for Token.
   */

        public static String getTokenFactory() {
            return stringValue("TOKEN_FACTORY");
        }

        /**
   * Return the file encoding; this will return the file.encoding system property if no value was explicitly set
   *
   * @return The file encoding (e.g., UTF-8, ISO_8859-1, MacRoman)
   */

        public static String getGrammarEncoding() {
            if (stringValue("GRAMMAR_ENCODING").Equals("")) {
                return Encoding.Default.EncodingName;
            } else {
                return stringValue("GRAMMAR_ENCODING");
            }
        }

        /**
   * Find the output directory.
   *
   * @return The requested output directory.
   */

        public static DirectoryInfo getOutputDirectory() {
            return new DirectoryInfo(stringValue("OUTPUT_DIRECTORY"));
        }

        public static String stringBufOrBuild() {
            if (getGenerateStringBuilder()) {
                return "StringBuilder";
            } else {
                return "StringBuilder";
            }
        }

    }

}