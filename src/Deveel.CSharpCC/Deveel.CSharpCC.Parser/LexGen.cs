using System;

namespace Deveel.CSharpCC.Parser {
    class LexGen {
        public static int lexStateIndex;
        public static int curKind;
        public static RegularExpression curRE;
        public static NfaState initialState;
        public static int maxLexStates;
        public static string lexStateSuffix;
        public static bool[] mixed;
    }
}