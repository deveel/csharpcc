using System;
using System.IO;

namespace Deveel.CSharpCC.Parser {
    class LexGen {
		static private TextWriter ostr;
		static private String staticString;
		static private String tokMgrClassName;

		// Hashtable of vectors
		//TODO: static Hashtable allTpsForState = new Hashtable();
		public static int lexStateIndex = 0;
		static int[] kinds;
		public static int maxOrdinal = 1;
		public static String lexStateSuffix;
	    internal static String[] newLexState;
		public static int[] lexStates;
		public static bool[] ignoreCase;
		public static Action[] actions;
		//TODO: public static Hashtable initStates = new Hashtable();
		public static int stateSetSize;
		public static int maxLexStates;
		public static String[] lexStateName;
		static NfaState[] singlesToSkip;
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
		static bool hasSkipActions = false;
		static bool hasMoreActions = false;
		static bool hasTokenActions = false;
		static bool hasSpecial = false;
		static bool hasSkip = false;
		static bool hasMore = false;
		public static RegularExpression curRE;
		public static bool keepLineCol;

		public static void AddCharToSkip(char c, int kind) {
			singlesToSkip[lexStateIndex].AddChar(c);
			singlesToSkip[lexStateIndex].kind = kind;
		}
    }
}