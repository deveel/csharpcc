using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using Deveel.CSharpCC.Util;

namespace Deveel.CSharpCC.Parser {
	public class NfaState {
        public static bool unicodeWarningGiven = false;
        public static int generatedStates = 0;

        private static int idCnt = 0;
        private static int lohiByteCnt;
        private static int dummyStateIndex = -1;
        private static bool done;
        private static bool[] mark;
        private static bool[] stateDone;

        private static IList<NfaState> allStates = new List<NfaState>();
        private static IList<NfaState> indexedAllStates = new List<NfaState>();
        private static IList<NfaState> nonAsciiTableForMethod = new List<NfaState>();
        private static IDictionary<string, NfaState> equivStatesTable = new Dictionary<string, NfaState>();
        private static IDictionary<string, int[]> allNextStates = new Dictionary<string, int[]>();
        private static IDictionary<string, int> lohiByteTab = new Dictionary<string, int>();
        private static IDictionary<string, int> stateNameForComposite = new Dictionary<string, int>();
        private static IDictionary<string, int[]> compositeStateTable = new Dictionary<string, int[]>();
        private static Hashtable stateBlockTable = new Hashtable();
        private static IDictionary<string, int[]> stateSetsToFix = new Dictionary<string, int[]>();

        private static bool jjCheckNAddStatesUnaryNeeded = false;
        private static bool jjCheckNAddStatesDualNeeded = false;

        public static void ReInit() {
            generatedStates = 0;
            idCnt = 0;
            dummyStateIndex = -1;
            done = false;
            mark = null;
            stateDone = null;

            allStates.Clear();
            indexedAllStates.Clear();
            equivStatesTable.Clear();
            allNextStates.Clear();
            compositeStateTable.Clear();
            stateBlockTable.Clear();
            stateNameForComposite.Clear();
            stateSetsToFix.Clear();
        }

        internal long[] asciiMoves = new long[2];
	    internal char[] charMoves = null;
        private char[] rangeMoves = null;
	    internal NfaState next = null;
        private NfaState stateForCase;
	    internal IList<NfaState> epsilonMoves = new List<NfaState>();
        private String epsilonMovesString;
        private NfaState[] epsilonMoveArray;

        private int id;
	    internal int stateName = -1;
	    internal int kind = Int32.MaxValue;
        private int lookingFor;
        private int usefulEpsilonMoves = 0;
	    internal int inNextOf;
        private int lexState;
        private int nonAsciiMethod = -1;
        private int kindToPrint = Int32.MaxValue;
        internal bool dummy;
        private bool isComposite;
        private int[] compositeStates;
        internal bool isFinal;
        private IList<int> loByteVec;
        private int[] nonAsciiMoveIndices;
        private int round ;
        private int onlyChar;
        private char matchSingleChar;

        public NfaState() {
            id = idCnt++;
            allStates.Add(this);
            lexState = LexGen.lexStateIndex;
            lookingFor = LexGen.curKind;
        }

        private NfaState CreateClone() {
            NfaState retVal = new NfaState();

            retVal.isFinal = isFinal;
            retVal.kind = kind;
            retVal.lookingFor = lookingFor;
            retVal.lexState = lexState;
            retVal.inNextOf = inNextOf;

            retVal.MergeMoves(this);

            return retVal;
        }

        private static void InsertInOrder(IList<NfaState> v, NfaState s) {
            int j;

            for (j = 0; j < v.Count; j++)
                if (v[j].id > s.id)
                    break;
                else if (v[j].id == s.id)
                    return;

            v.Insert(j, s);
        }

        private static char[] ExpandCharArr(char[] oldArr, int incr) {
            var ret = new char[oldArr.Length + incr];
            Array.Copy(oldArr, 0, ret, 0, oldArr.Length);
            return ret;
        }

	    internal void AddMove(NfaState newState) {
            if (!epsilonMoves.Contains(newState))
                InsertInOrder(epsilonMoves, newState);
        }

        private void AddASCIIMove(char c) {
            asciiMoves[c/64] |= (1L << (c%64));
        }

	    internal void AddChar(char c) {
            onlyChar++;
            matchSingleChar = c;
            int i;
            char temp;
            char temp1;

            if ((int) c < 128) // ASCII char
            {
                AddASCIIMove(c);
                return;
            }

            if (charMoves == null)
                charMoves = new char[10];

            int len = charMoves.Length;

            if (charMoves[len - 1] != 0) {
                charMoves = ExpandCharArr(charMoves, 10);
                len += 10;
            }

            for (i = 0; i < len; i++)
                if (charMoves[i] == 0 || charMoves[i] > c)
                    break;

            if (!unicodeWarningGiven && c > 0xff &&
                !Options.getUnicodeEscape() &&
                !Options.getUserCharStream()) {
                unicodeWarningGiven = true;
                CSharpCCErrors.Warning(LexGen.curRE,
                    "Non-ASCII characters used in regular expression.\n" +
                    "Please make sure you use the correct TextReader when you create the parser, " +
                    "one that can handle your character set.");
            }

            temp = charMoves[i];
            charMoves[i] = c;

            for (i++; i < len; i++) {
                if (temp == 0)
                    break;

                temp1 = charMoves[i];
                charMoves[i] = temp;
                temp = temp1;
            }
        }

	    internal void AddRange(char left, char right) {
            onlyChar = 2;
            int i;
            char tempLeft1, tempLeft2, tempRight1, tempRight2;

            if (left < 128) {
                if (right < 128) {
                    for (; left <= right; left++)
                        AddASCIIMove(left);

                    return;
                }

                for (; left < 128; left++)
                    AddASCIIMove(left);
            }

            if (!unicodeWarningGiven && (left > 0xff || right > 0xff) &&
                !Options.getUnicodeEscape() &&
                !Options.getUserCharStream()) {
                unicodeWarningGiven = true;
                CSharpCCErrors.Warning(LexGen.curRE,
                    "Non-ASCII characters used in regular expression.\n" +
                    "Please make sure you use the correct Reader when you create the parser, " +
                    "one that can handle your character set.");
            }

            if (rangeMoves == null)
                rangeMoves = new char[20];

            int len = rangeMoves.Length;

            if (rangeMoves[len - 1] != 0) {
                rangeMoves = ExpandCharArr(rangeMoves, 20);
                len += 20;
            }

            for (i = 0; i < len; i += 2)
                if (rangeMoves[i] == 0 ||
                    (rangeMoves[i] > left) ||
                    ((rangeMoves[i] == left) && (rangeMoves[i + 1] > right)))
                    break;

            tempLeft1 = rangeMoves[i];
            tempRight1 = rangeMoves[i + 1];
            rangeMoves[i] = left;
            rangeMoves[i + 1] = right;

            for (i += 2; i < len; i += 2) {
                if (tempLeft1 == 0)
                    break;

                tempLeft2 = rangeMoves[i];
                tempRight2 = rangeMoves[i + 1];
                rangeMoves[i] = tempLeft1;
                rangeMoves[i + 1] = tempRight1;
                tempLeft1 = tempLeft2;
                tempRight1 = tempRight2;
            }
        }

        // From hereon down all the functions are used for code generation

        private static bool EqualCharArr(char[] arr1, char[] arr2) {
            if (arr1 == arr2)
                return true;

            if (arr1 != null &&
                arr2 != null &&
                arr1.Length == arr2.Length) {
                for (int i = arr1.Length; i-- > 0;)
                    if (arr1[i] != arr2[i])
                        return false;

                return true;
            }

            return false;
        }

        private bool closureDone = false;

        /** This function computes the closure and also updates the kind so that
     * any time there is a move to this state, it can go on epsilon to a
     * new state in the epsilon moves that might have a lower kind of token
     * number for the same length.
   */

        private void EpsilonClosure() {
            int i = 0;

            if (closureDone || mark[id])
                return;

            mark[id] = true;

            // Recursively do closure
            for (i = 0; i < epsilonMoves.Count; i++)
                epsilonMoves[i].EpsilonClosure();

	        for (int j = 0; j < epsilonMoves.Count; j++) {
		        var tmp = epsilonMoves[j];

                for (i = 0; i < tmp.epsilonMoves.Count; i++) {
                    var tmp1 = tmp.epsilonMoves[i];
                    if (tmp1.UsefulState() && !epsilonMoves.Contains(tmp1)) {
                        InsertInOrder(epsilonMoves, tmp1);
                        done = false;
                    }
                }

                if (kind > tmp.kind)
                    kind = tmp.kind;
            }

            if (HasTransitions() && !epsilonMoves.Contains(this))
                InsertInOrder(epsilonMoves, this);
        }

        private bool UsefulState() {
            return isFinal || HasTransitions();
        }

        public bool HasTransitions() {
            return (asciiMoves[0] != 0L || asciiMoves[1] != 0L ||
                    (charMoves != null && charMoves[0] != 0) ||
                    (rangeMoves != null && rangeMoves[0] != 0));
        }

        private void MergeMoves(NfaState other) {
            // Warning : This function does not merge epsilon moves
            if (asciiMoves == other.asciiMoves) {
                CSharpCCErrors.SemanticError("Bug in CSharpCC : Please send " +
                                             "a report along with the input that caused this. Thank you.");
                throw new InvalidOperationException();
            }

            asciiMoves[0] = asciiMoves[0] | other.asciiMoves[0];
            asciiMoves[1] = asciiMoves[1] | other.asciiMoves[1];

            if (other.charMoves != null) {
                if (charMoves == null)
                    charMoves = other.charMoves;
                else {
                    char[] tmpCharMoves = new char[charMoves.Length +
                                                   other.charMoves.Length];
                    Array.Copy(charMoves, 0, tmpCharMoves, 0, charMoves.Length);
                    charMoves = tmpCharMoves;

                    for (int i = 0; i < other.charMoves.Length; i++)
                        AddChar(other.charMoves[i]);
                }
            }

            if (other.rangeMoves != null) {
                if (rangeMoves == null)
                    rangeMoves = other.rangeMoves;
                else {
                    char[] tmpRangeMoves = new char[rangeMoves.Length +
                                                    other.rangeMoves.Length];
                    Array.Copy(rangeMoves, 0, tmpRangeMoves, 0, rangeMoves.Length);
                    rangeMoves = tmpRangeMoves;
                    for (int i = 0; i < other.rangeMoves.Length; i += 2)
                        AddRange(other.rangeMoves[i], other.rangeMoves[i + 1]);
                }
            }

            if (other.kind < kind)
                kind = other.kind;

            if (other.kindToPrint < kindToPrint)
                kindToPrint = other.kindToPrint;

            isFinal |= other.isFinal;
        }

        private NfaState CreateEquivState(IList<NfaState> states) {
            var newState = states[0].CreateClone();

            newState.next = new NfaState();

            InsertInOrder(newState.next.epsilonMoves, states[0].next);

            for (int i = 1; i < states.Count; i++) {
                var tmp2 = states[i];

                if (tmp2.kind < newState.kind)
                    newState.kind = tmp2.kind;

                newState.isFinal |= tmp2.isFinal;

                InsertInOrder(newState.next.epsilonMoves, tmp2.next);
            }

            return newState;
        }

        private NfaState GetEquivalentRunTimeState() {
            for (int i = allStates.Count; i-- > 0;) {
                var other = (NfaState) allStates[i];

                if (this != other && other.stateName != -1 &&
                    kindToPrint == other.kindToPrint &&
                    asciiMoves[0] == other.asciiMoves[0] &&
                    asciiMoves[1] == other.asciiMoves[1] &&
                    EqualCharArr(charMoves, other.charMoves) &&
                    EqualCharArr(rangeMoves, other.rangeMoves)) {
                    if (next == other.next)
                        return other;
                    else if (next != null && other.next != null) {
                        if (next.epsilonMoves.Count == other.next.epsilonMoves.Count) {
                            for (int j = 0; j < next.epsilonMoves.Count; j++)
                                if (next.epsilonMoves[j] != other.next.epsilonMoves[j])
                                    goto Outer;

                            return other;
                        }
                    }
                }
            }

            Outer:
            ;

            return null;
        }

        // generates code (without outputting it) and returns the name used.
        internal void GenerateCode() {
            if (stateName != -1)
                return;

            if (next != null) {
                next.GenerateCode();
                if (next.kind != Int32.MaxValue)
                    kindToPrint = next.kind;
            }

            if (stateName == -1 && HasTransitions()) {
                NfaState tmp = GetEquivalentRunTimeState();

                if (tmp != null) {
                    stateName = tmp.stateName;
//????
                    //tmp.inNextOf += inNextOf;
//????
                    dummy = true;
                    return;
                }

                stateName = generatedStates++;
                indexedAllStates.Add(this);
                GenerateNextStatesCode();
            }
        }

        public static void ComputeClosures() {
            for (int i = allStates.Count; i-- > 0;) {
                var tmp = allStates[i];

                if (!tmp.closureDone)
                    tmp.OptimizeEpsilonMoves(true);
            }

            for (int i = 0; i < allStates.Count; i++) {
                var tmp = allStates[i];

                if (!tmp.closureDone)
                    tmp.OptimizeEpsilonMoves(false);
            }

            for (int i = 0; i < allStates.Count; i++) {
                var tmp = allStates[i];
                tmp.epsilonMoveArray = new NfaState[tmp.epsilonMoves.Count];
                tmp.epsilonMoves.CopyTo(tmp.epsilonMoveArray, 0);
            }
        }

        private void OptimizeEpsilonMoves(bool optReqd) {
            int i;

            // First do epsilon closure
            done = false;
            while (!done) {
                if (mark == null || mark.Length < allStates.Count)
                    mark = new bool[allStates.Count];

                for (i = allStates.Count; i-- > 0;)
                    mark[i] = false;

                done = true;
                EpsilonClosure();
            }

            for (i = allStates.Count; i-- > 0;)
                allStates[i].closureDone = mark[allStates[i].id];

            // Warning : The following piece of code is just an optimization.
            // in case of trouble, just remove this piece.

            bool sometingOptimized = true;

            NfaState newState = null;
            NfaState tmp1, tmp2;
            int j;
            IList<NfaState> equivStates = null;

            while (sometingOptimized) {
                sometingOptimized = false;
                for (i = 0; optReqd && i < epsilonMoves.Count; i++) {
                    if ((tmp1 = epsilonMoves[i]).HasTransitions()) {
                        for (j = i + 1; j < epsilonMoves.Count; j++) {
                            if ((tmp2 = epsilonMoves[j]).
                                HasTransitions() &&
                                (tmp1.asciiMoves[0] == tmp2.asciiMoves[0] &&
                                 tmp1.asciiMoves[1] == tmp2.asciiMoves[1] &&
                                 EqualCharArr(tmp1.charMoves, tmp2.charMoves) &&
                                 EqualCharArr(tmp1.rangeMoves, tmp2.rangeMoves))) {
                                if (equivStates == null) {
                                    equivStates = new List<NfaState>();
                                    equivStates.Add(tmp1);
                                }

                                InsertInOrder(equivStates, tmp2);
                                epsilonMoves.RemoveAt(j--);
                            }
                        }
                    }

                    if (equivStates != null) {
                        sometingOptimized = true;
                        String tmp = "";
                        for (int l = 0; l < equivStates.Count; l++)
                            tmp += equivStates[l].id + ", ";

                        if (!equivStatesTable.TryGetValue(tmp, out  newState)) {
                            newState = CreateEquivState(equivStates);
                            equivStatesTable[tmp] = newState;
                        }

                        epsilonMoves.RemoveAt(i--);
                        epsilonMoves.Add(newState);
                        equivStates = null;
                        newState = null;
                    }
                }

                for (i = 0; i < epsilonMoves.Count; i++) {
                    //if ((tmp1 = (NfaState)epsilonMoves.elementAt(i)).next == null)
                    //continue;
                    tmp1 = epsilonMoves[i];

                    for (j = i + 1; j < epsilonMoves.Count; j++) {
                        tmp2 = epsilonMoves[j];

                        if (tmp1.next == tmp2.next) {
                            if (newState == null) {
                                newState = tmp1.CreateClone();
                                newState.next = tmp1.next;
                                sometingOptimized = true;
                            }

                            newState.MergeMoves(tmp2);
                            epsilonMoves.RemoveAt(j--);
                        }
                    }

                    if (newState != null) {
                        epsilonMoves.RemoveAt(i--);
                        epsilonMoves.Add(newState);
                        newState = null;
                    }
                }
            }

            // End Warning

            // Generate an array of states for epsilon moves (not vector)
            if (epsilonMoves.Count > 0) {
                for (i = 0; i < epsilonMoves.Count; i++)
                    // Since we are doing a closure, just epsilon moves are unncessary
                    if (epsilonMoves[i].HasTransitions())
                        usefulEpsilonMoves++;
                    else
                        epsilonMoves.RemoveAt(i--);
            }
        }

        private void GenerateNextStatesCode() {
            if (next.usefulEpsilonMoves > 0)
                next.GetEpsilonMovesString();
        }

        private String GetEpsilonMovesString() {
            int[] stateNames = new int[usefulEpsilonMoves];
            int cnt = 0;

            if (epsilonMovesString != null)
                return epsilonMovesString;

            if (usefulEpsilonMoves > 0) {
                NfaState tempState;
                epsilonMovesString = "{ ";
                for (int i = 0; i < epsilonMoves.Count; i++) {
                    if ((tempState = (NfaState) epsilonMoves[i]).
                        HasTransitions()) {
                        if (tempState.stateName == -1)
                            tempState.GenerateCode();

                        ((NfaState) indexedAllStates[tempState.stateName]).inNextOf++;
                        stateNames[cnt] = tempState.stateName;
                        epsilonMovesString += tempState.stateName + ", ";
                        if (cnt++ > 0 && cnt%16 == 0)
                            epsilonMovesString += "\n";
                    }
                }

                epsilonMovesString += "};";
            }

            usefulEpsilonMoves = cnt;
            if (epsilonMovesString != null &&
                !allNextStates.ContainsKey(epsilonMovesString)) {
                int[] statesToPut = new int[usefulEpsilonMoves];

                Array.Copy(stateNames, 0, statesToPut, 0, cnt);
                allNextStates[epsilonMovesString] = statesToPut;
            }

            return epsilonMovesString;
        }

        public static bool CanStartNfaUsingAscii(char c) {
            if (c >= 128)
                throw new InvalidOperationException("CSharpCC Bug: Please send mail to sankar@cs.stanford.edu");

            String s = LexGen.initialState.GetEpsilonMovesString();

            if (s == null || s.Equals("null;"))
                return false;

            int[] states = allNextStates[s];

            for (int i = 0; i < states.Length; i++) {
                var tmp = indexedAllStates[states[i]];

                if ((tmp.asciiMoves[c/64] & (1L << c%64)) != 0L)
                    return true;
            }

            return false;
        }

        private bool CanMoveUsingChar(char c) {
            int i;

            if (onlyChar == 1)
                return c == matchSingleChar;

            if (c < 128)
                return ((asciiMoves[c/64] & (1L << c%64)) != 0L);

            // Just check directly if there is a move for this char
            if (charMoves != null && charMoves[0] != 0) {
                for (i = 0; i < charMoves.Length; i++) {
                    if (c == charMoves[i])
                        return true;
                    else if (c < charMoves[i] || charMoves[i] == 0)
                        break;
                }
            }


            // For ranges, iterate thru the table to see if the current char
            // is in some range
            if (rangeMoves != null && rangeMoves[0] != 0)
                for (i = 0; i < rangeMoves.Length; i += 2)
                    if (c >= rangeMoves[i] && c <= rangeMoves[i + 1])
                        return true;
                    else if (c < rangeMoves[i] || rangeMoves[i] == 0)
                        break;

            //return (nextForNegatedList != null);
            return false;
        }

        public int getFirstValidPos(String s, int i, int len) {
            if (onlyChar == 1) {
                char c = matchSingleChar;
                while (c != s[i] && ++i < len)
                    ;
                return i;
            }

            do {
                if (CanMoveUsingChar(s[i]))
                    return i;
            } while (++i < len);

            return i;
        }

        public int MoveFrom(char c, IList<NfaState> newStates) {
            if (CanMoveUsingChar(c)) {
                for (int i = next.epsilonMoves.Count; i-- > 0;)
                    InsertInOrder(newStates, next.epsilonMoves[i]);

                return kindToPrint;
            }

            return Int32.MaxValue;
        }

        public static int MoveFromSet(char c, IList<NfaState> states, IList<NfaState> newStates) {
            int tmp;
            int retVal = Int32.MaxValue;

            for (int i = states.Count; i-- > 0;)
                if (retVal >
                    (tmp = states[i].MoveFrom(c, newStates)))
                    retVal = tmp;

            return retVal;
        }

        public static int moveFromSetForRegEx(char c, NfaState[] states, NfaState[] newStates, int round) {
            int start = 0;
            int sz = states.Length;

            for (int i = 0; i < sz; i++) {
                NfaState tmp1, tmp2;

                if ((tmp1 = states[i]) == null)
                    break;

                if (tmp1.CanMoveUsingChar(c)) {
                    if (tmp1.kindToPrint != Int32.MaxValue) {
                        newStates[start] = null;
                        return 1;
                    }

                    NfaState[] v = tmp1.next.epsilonMoveArray;
                    for (int j = v.Length; j-- > 0;) {
                        if ((tmp2 = v[j]).round != round) {
                            tmp2.round = round;
                            newStates[start++] = tmp2;
                        }
                    }
                }
            }

            newStates[start] = null;
            return Int32.MaxValue;
        }

        private static IList<string> allBitVectors = new List<string>();

        /* This function generates the bit vectors of low and hi bytes for common
      bit vectors and returns those that are not common with anything (in
      loBytes) and returns an array of indices that can be used to generate
      the function names for char matching using the common bit vectors.
      It also generates code to match a char with the common bit vectors.
      (Need a better comment). */

        private static int[] tmpIndices = new int[512]; // 2 * 256

        private void GenerateNonAsciiMoves(TextWriter ostr) {
            int i = 0, j = 0;
            char hiByte;
            int cnt = 0;
            long[,] loBytes = new long[256,4];

            if ((charMoves == null || charMoves[0] == 0) &&
                (rangeMoves == null || rangeMoves[0] == 0))
                return;

            if (charMoves != null) {
                for (i = 0; i < charMoves.Length; i++) {
                    if (charMoves[i] == 0)
                        break;

                    hiByte = (char) (charMoves[i] >> 8);
                    loBytes[hiByte, (charMoves[i] & 0xff)/64] |= (1L << ((charMoves[i] & 0xff)%64));
                }
            }

            if (rangeMoves != null) {
                for (i = 0; i < rangeMoves.Length; i += 2) {
                    if (rangeMoves[i] == 0)
                        break;

                    char c, r;

                    r = (char) (rangeMoves[i + 1] & 0xff);
                    hiByte = (char) (rangeMoves[i] >> 8);

                    if (hiByte == (char) (rangeMoves[i + 1] >> 8)) {
                        for (c = (char) (rangeMoves[i] & 0xff); c <= r; c++)
                            loBytes[hiByte, c/64] |= (1L << (c%64));

                        continue;
                    }

                    for (c = (char) (rangeMoves[i] & 0xff); c <= 0xff; c++)
                        loBytes[hiByte, c/64] |= (1L << (c%64));

                    while (++hiByte < (char) (rangeMoves[i + 1] >> 8)) {
                        loBytes[hiByte, 0] |= Int64.MaxValue /* 0xffffffffffffffffL */;
                        loBytes[hiByte, 1] |= Int64.MaxValue /* 0xffffffffffffffffL */;
                        loBytes[hiByte, 2] |= Int64.MaxValue /* 0xffffffffffffffffL */;
                        loBytes[hiByte, 3] |= Int64.MaxValue /* 0xffffffffffffffffL */;
                    }

                    for (int x = 0; x <= r;x++)
                        loBytes[hiByte, x/64] |= (1L << (c%64));
                }
            }

            long[] common = null;
            bool[] done = new bool[256];

            for (i = 0; i <= 255; i++) {
                if (done[i] ||
                    (done[i] =
                        loBytes[i,0] == 0 &&
                        loBytes[i,1] == 0 &&
                        loBytes[i,2] == 0 &&
                        loBytes[i,3] == 0))
                    continue;

                for (j = i + 1; j < 256; j++) {
                    if (done[j])
                        continue;

                    if (loBytes[i,0] == loBytes[j, 0] &&
                        loBytes[i,1] == loBytes[j, 1] &&
                        loBytes[i,2] == loBytes[j, 2] &&
                        loBytes[i,3] == loBytes[j, 3]) {
                        done[j] = true;
                        if (common == null) {
                            done[i] = true;
                            common = new long[4];
                            common[i/64] |= (1L << (i%64));
                        }

                        common[j/64] |= (1L << (j%64));
                    }
                }

                if (common != null) {
                    int ind;
                    String tmp;

	                tmp = "{\n   " + common[0] + "L, " + common[1] + "L, " + common[2] + "L, " + common[3] + "L\n};";

                    if (!lohiByteTab.TryGetValue(tmp, out ind)) {
                        allBitVectors.Add(tmp);

                        if (!AllBitsSet(tmp))
                            ostr.WriteLine("static readonly long[] ccBitVec" + lohiByteCnt + " = " + tmp);
                        lohiByteTab[tmp] = ind = lohiByteCnt++;
                    }

                    tmpIndices[cnt++] = ind;

	                tmp = "{\n   " + loBytes[i, 0] + "L, " + loBytes[i, 1] + "L, " + loBytes[i, 2] + "L, " + loBytes[i, 3] + "L\n};";


                    if (!lohiByteTab.TryGetValue(tmp, out ind)) {
                        allBitVectors.Add(tmp);

                        if (!AllBitsSet(tmp))
                            ostr.WriteLine("static readonly long[] ccBitVec" + lohiByteCnt + " = " + tmp);
                        lohiByteTab[tmp] = ind = lohiByteCnt++;
                    }

                    tmpIndices[cnt++] = ind;

                    common = null;
                }
            }

            nonAsciiMoveIndices = new int[cnt];
            Array.Copy(tmpIndices, 0, nonAsciiMoveIndices, 0, cnt);

            for (i = 0; i < 256; i++) {
                if (done[i]) {
                    //TODO: Check this!!!
                    loBytes[i,0] = 0;
                    loBytes[i, 1] = 0;
                    loBytes[i, 2] = 0;
                    loBytes[i, 3] = 0;
                } else {
                    String tmp;
                    int ind;

					tmp = "{\n   " + loBytes[i, 0] + "L, " + loBytes[i, 1] + "L, " + loBytes[i, 2] + "L, " + loBytes[i, 3] + "L\n};";


                    if (!lohiByteTab.TryGetValue(tmp, out ind)) {
                        allBitVectors.Add(tmp);

                        if (!AllBitsSet(tmp))
                            ostr.WriteLine("static readonly long[] ccBitVec" + lohiByteCnt + " = " + tmp);
                        lohiByteTab[tmp] = ind = lohiByteCnt++;
                    }

                    if (loByteVec == null)
                        loByteVec = new List<int>();

                    loByteVec.Add(i);
                    loByteVec.Add(ind);
                }
            }

            //System.out.println("");
            UpdateDuplicateNonAsciiMoves();
        }

        private void UpdateDuplicateNonAsciiMoves() {
            for (int i = 0; i < nonAsciiTableForMethod.Count; i++) {
                var tmp = nonAsciiTableForMethod[i];
                if (EqualLoByteVectors(loByteVec, tmp.loByteVec) &&
                    EqualNonAsciiMoveIndices(nonAsciiMoveIndices, tmp.nonAsciiMoveIndices)) {
                    nonAsciiMethod = i;
                    return;
                }
            }

            nonAsciiMethod = nonAsciiTableForMethod.Count;
            nonAsciiTableForMethod.Add(this);
        }

        private static bool EqualLoByteVectors(IList<int> vec1, IList<int> vec2) {
            if (vec1 == null || vec2 == null)
                return false;

            if (vec1 == vec2)
                return true;

            if (vec1.Count != vec2.Count)
                return false;

            for (int i = 0; i < vec1.Count; i++) {
                if (vec1[i] != vec2[i])
                    return false;
            }

            return true;
        }

        private static bool EqualNonAsciiMoveIndices(int[] moves1, int[] moves2) {
            if (moves1 == moves2)
                return true;

            if (moves1 == null || moves2 == null)
                return false;

            if (moves1.Length != moves2.Length)
                return false;

            for (int i = 0; i < moves1.Length; i++) {
                if (moves1[i] != moves2[i])
                    return false;
            }

            return true;
        }

		/*
		HEX ain't good ...
        private static String allBits = "{\n   0xffffffffffffffffL, " +"0xffffffffffffffffL, " + "0xffffffffffffffffL, " + "0xffffffffffffffffL \n};";
		*/

		private static string allBits = "{\n   Int64.MaxValue, Int64.MaxValue, Int64.MaxValue, Int64.MaxValue \n};";

        private static bool AllBitsSet(String bitVec) {
            return bitVec.Equals(allBits);
        }

	    internal static int AddStartStateSet(String stateSetString) {
            return AddCompositeStateSet(stateSetString, true);
        }

        private static int AddCompositeStateSet(String stateSetString, bool starts) {
            int stateNameToReturn;

            if (stateNameForComposite.TryGetValue(stateSetString, out stateNameToReturn))
                return stateNameToReturn;

            int toRet = 0;
            int[] nameSet;

            if (!starts)
                stateBlockTable[stateSetString] = stateSetString;

            if (!allNextStates.TryGetValue(stateSetString, out nameSet))
                throw new InvalidOperationException("CSharpCC Bug: Please send areport; nameSet null for : " + stateSetString);

            if (nameSet.Length == 1) {
                stateNameToReturn = nameSet[0];
                stateNameForComposite[stateSetString] = stateNameToReturn;
                return nameSet[0];
            }

            for (int i = 0; i < nameSet.Length; i++) {
                if (nameSet[i] == -1)
                    continue;

                var st = indexedAllStates[nameSet[i]];
                st.isComposite = true;
                st.compositeStates = nameSet;
            }

            while (toRet < nameSet.Length &&
                   (starts && indexedAllStates[nameSet[toRet]].inNextOf > 1))
                toRet++;

            foreach (var entry in compositeStateTable) {
                if (!entry.Key.Equals(stateSetString) && Intersect(stateSetString, entry.Key)) {
                    int[] other = entry.Value;

                    while (toRet < nameSet.Length &&
                           ((starts && indexedAllStates[nameSet[toRet]].inNextOf > 1) ||
                            ElemOccurs(nameSet[toRet], other) >= 0))
                        toRet++;
                }
            }

            int tmp;

            if (toRet >= nameSet.Length) {
                if (dummyStateIndex == -1)
                    tmp = dummyStateIndex = generatedStates;
                else
                    tmp = ++dummyStateIndex;
            } else
                tmp = nameSet[toRet];

            stateNameToReturn = tmp;
            stateNameForComposite[stateSetString] = stateNameToReturn;
            compositeStateTable[stateSetString] = nameSet;

            return tmp;
        }

        private static int StateNameForComposite(string stateSetString) {
            return stateNameForComposite[stateSetString];
        }

	    internal static int InitStateName() {
            String s = LexGen.initialState.GetEpsilonMovesString();

            if (LexGen.initialState.usefulEpsilonMoves != 0)
                return StateNameForComposite(s);
            return -1;
        }

        public void GenerateInitMoves(TextWriter ostr) {
            GetEpsilonMovesString();

            if (epsilonMovesString == null)
                epsilonMovesString = "null;";

            AddStartStateSet(epsilonMovesString);
        }

        private static IDictionary<string, int[]> tableToDump = new Dictionary<string, int[]>();
        private static IList<int[]> orderedStateSet = new List<int[]>();

        private static int lastIndex = 0;

        private static int[] GetStateSetIndicesForUse(String arrayString) {
            int[] ret;
            int[] set = allNextStates[arrayString];

            if (!tableToDump.TryGetValue(arrayString, out ret)) {
                ret = new int[2];
                ret[0] = lastIndex;
                ret[1] = lastIndex + set.Length - 1;
                lastIndex += set.Length;
                tableToDump[arrayString] = ret;
                orderedStateSet.Add(set);
            }

            return ret;
        }

        public static void DumpStateSets(TextWriter ostr) {
            int cnt = 0;

            ostr.Write("static readonly int[] ccNextStates = {");
            for (int i = 0; i < orderedStateSet.Count; i++) {
                int[] set = orderedStateSet[i];

                for (int j = 0; j < set.Length; j++) {
                    if (cnt++%16 == 0)
                        ostr.Write("\n   ");

                    ostr.Write(set[j] + ", ");
                }
            }

            ostr.WriteLine("\n};");
        }

        private static String GetStateSetString(int[] states) {
            String retVal = "{ ";
            for (int i = 0; i < states.Length;) {
                retVal += states[i] + ", ";

                if (i++ > 0 && i%16 == 0)
                    retVal += "\n";
            }

            retVal += "};";
            allNextStates[retVal] = states;
            return retVal;
        }

        internal static String GetStateSetString(IList<NfaState> states) {
            if (states == null || states.Count == 0)
                return "null;";

            int[] set = new int[states.Count];
            String retVal = "{ ";
            for (int i = 0; i < states.Count;) {
                int k;
                retVal += (k = states[i].stateName) + ", ";
                set[i] = k;

                if (i++ > 0 && i%16 == 0)
                    retVal += "\n";
            }

            retVal += "};";
            allNextStates[retVal] = set;
            return retVal;
        }

        private static int NumberOfBitsSet(long l) {
            int ret = 0;
            for (int i = 0; i < 63; i++)
                if (((l >> i) & 1L) != 0L)
                    ret++;

            return ret;
        }

        private static int OnlyOneBitSet(long l) {
            int oneSeen = -1;
            for (int i = 0; i < 64; i++)
                if (((l >> i) & 1L) != 0L) {
                    if (oneSeen >= 0)
                        return -1;
                    oneSeen = i;
                }

            return oneSeen;
        }

        private static int ElemOccurs(int elem, int[] arr) {
            for (int i = arr.Length; i-- > 0;)
                if (arr[i] == elem)
                    return i;

            return -1;
        }

        private bool FindCommonBlocks() {
            if (next == null || next.usefulEpsilonMoves <= 1)
                return false;

            if (stateDone == null)
                stateDone = new bool[generatedStates];

            String set = next.epsilonMovesString;

            int[] nameSet = allNextStates[set];

            if (nameSet.Length <= 2 || compositeStateTable.ContainsKey(set))
                return false;

            int i;
            int[] freq = new int[nameSet.Length];
            bool[] live = new bool[nameSet.Length];
            int[] count = new int[allNextStates.Count];

            for (i = 0; i < nameSet.Length; i++) {
                if (nameSet[i] != -1) {
                    //TODO: Check!!!
                    if (live[i] = !stateDone[nameSet[i]])
                        count[0]++;
                }
            }

            int j, blockLen = 0, commonFreq = 0;
            bool needUpdate;

            foreach (var entry in allNextStates) {
                int[] tmpSet = entry.Value;
                if (tmpSet == nameSet)
                    continue;

                needUpdate = false;
                for (j = 0; j < nameSet.Length; j++) {
                    if (nameSet[j] == -1)
                        continue;

                    if (live[j] && ElemOccurs(nameSet[j], tmpSet) >= 0) {
                        if (!needUpdate) {
                            needUpdate = true;
                            commonFreq++;
                        }

                        count[freq[j]]--;
                        count[commonFreq]++;
                        freq[j] = commonFreq;
                    }
                }

                if (needUpdate) {
                    int foundFreq = -1;
                    blockLen = 0;

                    for (j = 0; j <= commonFreq; j++)
                        if (count[j] > blockLen) {
                            foundFreq = j;
                            blockLen = count[j];
                        }

                    if (blockLen <= 1)
                        return false;

                    for (j = 0; j < nameSet.Length; j++)
                        if (nameSet[j] != -1 && freq[j] != foundFreq) {
                            live[j] = false;
                            count[freq[j]]--;
                        }
                }
            }

            if (blockLen <= 1)
                return false;

            int[] commonBlock = new int[blockLen];
            int cnt = 0;
            //System.out.println("Common Block for " + set + " :");
            for (i = 0; i < nameSet.Length; i++) {
                if (live[i]) {
                    if (indexedAllStates[nameSet[i]].isComposite)
                        return false;

                    stateDone[nameSet[i]] = true;
                    commonBlock[cnt++] = nameSet[i];
                    //System.out.print(nameSet[i] + ", ");
                }
            }

            //System.out.println("");

            String s = GetStateSetString(commonBlock);
            foreach (var entry in allNextStates) {
                int at;
                bool firstOne = true;
                String stringToFix = entry.Key;
                int[] setToFix = entry.Value;

                if (setToFix == commonBlock)
                    continue;

                for (int k = 0; k < cnt; k++) {
                    if ((at = ElemOccurs(commonBlock[k], setToFix)) >= 0) {
                        if (!firstOne)
                            setToFix[at] = -1;
                        firstOne = false;
                    } else
                        goto Outer;
                }

                if (!stateSetsToFix.ContainsKey(stringToFix))
                    stateSetsToFix[stringToFix] = setToFix;
            }

            Outer:
            ;

            next.usefulEpsilonMoves -= blockLen - 1;
            AddCompositeStateSet(s, false);
            return true;
        }

        private bool CheckNextOccursTogether() {
            if (next == null || next.usefulEpsilonMoves <= 1)
                return true;

            String set = next.epsilonMovesString;

            int[] nameSet = allNextStates[set];

            if (nameSet.Length == 1 || 
                compositeStateTable.ContainsKey(set) ||
                stateSetsToFix.ContainsKey(set))
                return false;

            int i;
            var occursIn = new Dictionary<string, int[]>();
            NfaState tmp = allStates[nameSet[0]];

            for (i = 1; i < nameSet.Length; i++) {
                var tmp1 = allStates[nameSet[i]];

                if (tmp.inNextOf != tmp1.inNextOf)
                    return false;
            }

            int isPresent, j;
            foreach (var entry in allNextStates) {
                string s = entry.Key;
                int[] tmpSet = entry.Value;

                if (tmpSet == nameSet)
                    continue;

                isPresent = 0;
                for (j = 0; j < nameSet.Length; j++) {
                    if (ElemOccurs(nameSet[j], tmpSet) >= 0)
                        isPresent++;
                    else if (isPresent > 0)
                        return false;
                }

                if (isPresent == j) {
                    if (tmpSet.Length > nameSet.Length)
                        occursIn[s] = tmpSet;

                    //May not need. But safe.
                    if (compositeStateTable.ContainsKey(s) ||
                        stateSetsToFix.ContainsKey(s))
                        return false;
                } else if (isPresent != 0)
                    return false;
            }

            foreach (var entry in allNextStates) {
                String s = entry.Key;
                int[] setToFix = entry.Value;

                if (!stateSetsToFix.ContainsKey(s))
                    stateSetsToFix[s] = setToFix;

                for (int k = 0; k < setToFix.Length; k++)
                    if (ElemOccurs(setToFix[k], nameSet) > 0) // Not >= since need the first one (0)
                        setToFix[k] = -1;
            }

            next.usefulEpsilonMoves = 1;
            AddCompositeStateSet(next.epsilonMovesString, false);
            return true;
        }

        private static void FixStateSets() {
            var fixedSets = new Dictionary<string, int[]>();
            int[] tmp = new int[generatedStates];
            int i;

            foreach (var entry in stateSetsToFix) {
                String s = entry.Key;
                int[] toFix = entry.Value;
                int cnt = 0;

                for (i = 0; i < toFix.Length; i++) {
                    if (toFix[i] != -1)
                        tmp[cnt++] = toFix[i];
                }

                int[] iFixed = new int[cnt];
                Array.Copy(tmp, 0, iFixed, 0, cnt);
                fixedSets[s] = iFixed;
                allNextStates[s] = iFixed;
            }

            for (i = 0; i < allStates.Count; i++) {
                NfaState tmpState = allStates[i];
                int[] newSet;

                if (tmpState.next == null || tmpState.next.usefulEpsilonMoves == 0)
                    continue;

                if (fixedSets.TryGetValue(tmpState.next.epsilonMovesString, out newSet))
                    tmpState.FixNextStates(newSet);
            }
        }

        private void FixNextStates(int[] newSet) {
            next.usefulEpsilonMoves = newSet.Length;
        }

        private static bool Intersect(String set1, String set2) {
            if (set1 == null || set2 == null)
                return false;

            int[] nameSet1;
            int[] nameSet2;

            if (!allNextStates.TryGetValue(set1, out nameSet1) || 
                !allNextStates.TryGetValue(set2, out nameSet2))
                return false;

            if (nameSet1 == nameSet2)
                return true;

            for (int i = nameSet1.Length; i-- > 0;)
                for (int j = nameSet2.Length; j-- > 0;)
                    if (nameSet1[i] == nameSet2[j])
                        return true;

            return false;
        }

        private static void DumpHeadForCase(TextWriter ostr, int byteNum) {
            if (byteNum == 0)
                ostr.WriteLine("         long l = 1L << curChar;");
            else if (byteNum == 1)
                ostr.WriteLine("         long l = 1L << (curChar & 63);");

            else {
                if (Options.getUnicodeEscape() || unicodeWarningGiven) {
                    ostr.WriteLine("         int hiByte = (int)(curChar >> 8);");
                    ostr.WriteLine("         int i1 = hiByte >> 6;");
                    ostr.WriteLine("         long l1 = 1L << (hiByte & 63);");
                }

                ostr.WriteLine("         int i2 = (curChar & 0xff) >> 6;");
                ostr.WriteLine("         long l2 = 1L << (curChar & 63);");
            }

            //ostr.WriteLine("         MatchLoop: do");
            ostr.WriteLine("         do");
            ostr.WriteLine("         {");

            ostr.WriteLine("            switch(ccStateSet[--i])");
            ostr.WriteLine("            {");
        }

        private static IList<IList<NfaState>> PartitionStatesSetForAscii(int[] states, int byteNum) {
            int[] cardinalities = new int[states.Length];
            List<NfaState> original = new List<NfaState>(states.Length);
            var partition = new List<IList<NfaState>>();
            NfaState tmp;

            int cnt = 0;
            for (int i = 0; i < states.Length; i++) {
                tmp = allStates[states[i]];

                if (tmp.asciiMoves[byteNum] != 0L) {
                    int j;
                    int p = NumberOfBitsSet(tmp.asciiMoves[byteNum]);

                    for (j = 0; j < i; j++)
                        if (cardinalities[j] <= p)
                            break;

                    for (int k = i; k > j; k--)
                        cardinalities[k] = cardinalities[k - 1];

                    cardinalities[j] = p;

                    original.Insert(j, tmp);
                    cnt++;
                }
            }

            ListUtil.Resize(original, cnt);

            while (original.Count > 0) {
                tmp = original[0];
                original.Remove(tmp);

                long bitVec = tmp.asciiMoves[byteNum];
                IList<NfaState> subSet = new List<NfaState>();
                subSet.Add(tmp);

                for (int j = 0; j < original.Count; j++) {
                    NfaState tmp1 = original[j];

                    if ((tmp1.asciiMoves[byteNum] & bitVec) == 0L) {
                        bitVec |= tmp1.asciiMoves[byteNum];
                        subSet.Add(tmp1);
                        original.RemoveAt(j--);
                    }
                }

                partition.Add(subSet);
            }

            return partition;
        }

        private String PrintNoBreak(TextWriter ostr, int byteNum, bool[] dumped) {
            if (inNextOf != 1)
                throw new InvalidOperationException("CSharpCC Bug: Please send a report");

            dumped[stateName] = true;

            if (byteNum >= 0) {
                if (asciiMoves[byteNum] != 0L) {
                    ostr.WriteLine("               case " + stateName + ":");
                    DumpAsciiMoveForCompositeState(ostr, byteNum, false);
                    return "";
                }
            } else if (nonAsciiMethod != -1) {
                ostr.WriteLine("               case " + stateName + ":");
                DumpNonAsciiMoveForCompositeState(ostr);
                return "";
            }

            return ("               case " + stateName + ":\n");
        }

        private static void DumpCompositeStatesAsciiMoves(TextWriter ostr, string key, int byteNum, bool[] dumped) {
            int i;

            int[] nameSet = allNextStates[key];

            if (nameSet.Length == 1 || dumped[StateNameForComposite(key)])
                return;

            NfaState toBePrinted = null;
            int neededStates = 0;
            NfaState tmp;
            NfaState stateForCase = null;
            String toPrint = "";
            bool stateBlock = stateBlockTable.ContainsKey(key);

            for (i = 0; i < nameSet.Length; i++) {
                tmp = allStates[nameSet[i]];

                if (tmp.asciiMoves[byteNum] != 0L) {
                    if (neededStates++ == 1)
                        break;
                    else
                        toBePrinted = tmp;
                } else
                    dumped[tmp.stateName] = true;

                if (tmp.stateForCase != null) {
                    if (stateForCase != null)
                        throw new InvalidOperationException("CSharpCC Bug: Please send a report: ");

                    stateForCase = tmp.stateForCase;
                }
            }

            if (stateForCase != null)
                toPrint = stateForCase.PrintNoBreak(ostr, byteNum, dumped);

            if (neededStates == 0) {
                if (stateForCase != null && toPrint.Equals(""))
                    ostr.WriteLine("                  break;");
                return;
            }

            if (neededStates == 1) {
                if (!toPrint.Equals(""))
                    ostr.Write(toPrint);

                ostr.WriteLine("               case " + StateNameForComposite(key) + ":");

                if (!dumped[toBePrinted.stateName] && !stateBlock && toBePrinted.inNextOf > 1)
                    ostr.WriteLine("               case " + toBePrinted.stateName + ":");

                dumped[toBePrinted.stateName] = true;
                toBePrinted.DumpAsciiMove(ostr, byteNum, dumped);
                return;
            }

            var partition = PartitionStatesSetForAscii(nameSet, byteNum);

            if (!toPrint.Equals(""))
                ostr.Write(toPrint);

            int keyState = StateNameForComposite(key);
            ostr.WriteLine("               case " + keyState + ":");
            if (keyState < generatedStates)
                dumped[keyState] = true;

            for (i = 0; i < partition.Count; i++) {
                var subSet = partition[i];

                for (int j = 0; j < subSet.Count; j++) {
                    tmp = subSet[j];

                    if (stateBlock)
                        dumped[tmp.stateName] = true;
                    tmp.DumpAsciiMoveForCompositeState(ostr, byteNum, j != 0);
                }
            }

            if (stateBlock)
                ostr.WriteLine("                  break;");
            else
                ostr.WriteLine("                  break;");
        }

        private bool selfLoop() {
            if (next == null || next.epsilonMovesString == null)
                return false;

            int[] set = allNextStates[next.epsilonMovesString];
            return ElemOccurs(stateName, set) >= 0;
        }

        private void DumpAsciiMoveForCompositeState(TextWriter ostr, int byteNum, bool elseNeeded) {
            bool nextIntersects = selfLoop();

            for (int j = 0; j < allStates.Count; j++) {
                var temp1 = allStates[j];

                if (this == temp1 || temp1.stateName == -1 || temp1.dummy ||
                    stateName == temp1.stateName || temp1.asciiMoves[byteNum] == 0L)
                    continue;

                if (!nextIntersects && Intersect(temp1.next.epsilonMovesString,
                    next.epsilonMovesString)) {
                    nextIntersects = true;
                    break;
                }
            }

            //System.out.println(stateName + " \'s nextIntersects : " + nextIntersects);
            String prefix = "";
            if (asciiMoves[byteNum] != Int64.MaxValue /* 0xffffffffffffffffL */) {
                int oneBit = OnlyOneBitSet(asciiMoves[byteNum]);

                if (oneBit != -1)
                    ostr.WriteLine("                  " + (elseNeeded ? "else " : "") + "if (curChar == " +
                                   (64*byteNum + oneBit) + ")");
                else
                    ostr.WriteLine("                  " + (elseNeeded ? "else " : "") +
                                   "if ((" + asciiMoves[byteNum] + "L & l) != 0L)");
                prefix = "   ";
            }

            if (kindToPrint != Int32.MaxValue) {
                if (asciiMoves[byteNum] != Int64.MaxValue /* 0xffffffffffffffffL */) {
                    ostr.WriteLine("                  {");
                }

                ostr.WriteLine(prefix + "                  if (kind > " + kindToPrint + ")");
                ostr.WriteLine(prefix + "                     kind = " + kindToPrint + ";");
            }

            if (next != null && next.usefulEpsilonMoves > 0) {
                int[] stateNames = allNextStates[next.epsilonMovesString];
                if (next.usefulEpsilonMoves == 1) {
                    int name = stateNames[0];

                    if (nextIntersects)
                        ostr.WriteLine(prefix + "                  ccCheckNAdd(" + name + ");");
                    else
                        ostr.WriteLine(prefix + "                  ccStateSet[ccNewStateCnt++] = " + name + ";");
                } else if (next.usefulEpsilonMoves == 2 && nextIntersects) {
                    ostr.WriteLine(prefix + "                  ccCheckNAddTwoStates(" + stateNames[0] + ", " + stateNames[1] + ");");
                } else {
                    int[] indices = GetStateSetIndicesForUse(next.epsilonMovesString);
                    bool notTwo = (indices[0] + 1 != indices[1]);

                    if (nextIntersects) {
                        ostr.Write(prefix + "                  ccCheckNAddStates(" + indices[0]);
                        if (notTwo) {
                            jjCheckNAddStatesDualNeeded = true;
                            ostr.Write(", " + indices[1]);
                        } else {
                            jjCheckNAddStatesUnaryNeeded = true;
                        }
                        ostr.WriteLine(");");
                    } else
                        ostr.WriteLine(prefix + "                  ccAddStates(" + indices[0] + ", " + indices[1] + ");");
                }
            }

            if (asciiMoves[byteNum] != Int64.MaxValue /* 0xffffffffffffffffL */ && 
                kindToPrint != Int32.MaxValue)
            ostr.WriteLine("                  }");
        }

        private void DumpAsciiMove(TextWriter ostr, int byteNum, bool[] dumped) {
            bool nextIntersects = selfLoop() && isComposite;
            bool onlyState = true;

            for (int j = 0; j < allStates.Count; j++) {
                NfaState temp1 = allStates[j];

                if (this == temp1 || temp1.stateName == -1 || temp1.dummy ||
                    stateName == temp1.stateName || temp1.asciiMoves[byteNum] == 0L)
                    continue;

                if (onlyState && (asciiMoves[byteNum] & temp1.asciiMoves[byteNum]) != 0L)
                    onlyState = false;

                if (!nextIntersects && Intersect(temp1.next.epsilonMovesString,
                    next.epsilonMovesString))
                    nextIntersects = true;

                if (!dumped[temp1.stateName] && !temp1.isComposite &&
                    asciiMoves[byteNum] == temp1.asciiMoves[byteNum] &&
                    kindToPrint == temp1.kindToPrint &&
                    (next.epsilonMovesString == temp1.next.epsilonMovesString ||
                     (next.epsilonMovesString != null &&
                      temp1.next.epsilonMovesString != null &&
                      next.epsilonMovesString.Equals(temp1.next.epsilonMovesString)))) {
                    dumped[temp1.stateName] = true;
                    ostr.WriteLine("               case " + temp1.stateName + ":");
                }
            }

            //if (onlyState)
            //nextIntersects = false;

            int oneBit = OnlyOneBitSet(asciiMoves[byteNum]);
            if (asciiMoves[byteNum] != Int64.MaxValue /* 0xffffffffffffffffL */) {
                if ((next == null || next.usefulEpsilonMoves == 0) &&
                    kindToPrint != Int32.MaxValue) {
                    String kindCheck = "";

                    if (!onlyState)
                        kindCheck = " && kind > " + kindToPrint;

                    if (oneBit != -1)
                        ostr.WriteLine("                  if (curChar == " +
                                       (64*byteNum + oneBit) + kindCheck + ")");
                    else
                        ostr.WriteLine("                  if ((" +asciiMoves[byteNum] + "L & l) != 0L" + kindCheck + ")");

                    ostr.WriteLine("                     kind = " + kindToPrint + ";");

                    if (onlyState)
                        ostr.WriteLine("                  break;");
                    else
                        ostr.WriteLine("                  break;");

                    return;
                }
            }

            String prefix = "";
            if (kindToPrint != Int32.MaxValue) {

                if (oneBit != -1) {
                    ostr.WriteLine("                  if (curChar != " + (64*byteNum + oneBit) + ")");
                    ostr.WriteLine("                     break;");
                } else if (asciiMoves[byteNum] != Int64.MaxValue /* 0xffffffffffffffffL */) {
                    ostr.WriteLine("                  if ((" + asciiMoves[byteNum] + "L & l) == 0L)");
                    ostr.WriteLine("                     break;");
                }

                if (onlyState) {
                    ostr.WriteLine("                  kind = " + kindToPrint + ";");
                } else {
                    ostr.WriteLine("                  if (kind > " + kindToPrint + ")");
                    ostr.WriteLine("                     kind = " + kindToPrint + ";");
                }
            } else {
                if (oneBit != -1) {
                    ostr.WriteLine("                  if (curChar == " +
                                   (64*byteNum + oneBit) + ")");
                    prefix = "   ";
                } else if (asciiMoves[byteNum] != Int64.MaxValue /* 0xffffffffffffffffL */) {
                    ostr.WriteLine("                  if ((" + asciiMoves[byteNum] + "L & l) != 0L)");
                    prefix = "   ";
                }
            }

            if (next != null && next.usefulEpsilonMoves > 0) {
                int[] stateNames = allNextStates[next.epsilonMovesString];
                if (next.usefulEpsilonMoves == 1) {
                    int name = stateNames[0];
                    if (nextIntersects)
                        ostr.WriteLine(prefix + "                  ccCheckNAdd(" + name + ");");
                    else
                        ostr.WriteLine(prefix + "                  ccStateSet[ccNewStateCnt++] = " + name + ";");
                } else if (next.usefulEpsilonMoves == 2 && nextIntersects) {
                    ostr.WriteLine(prefix + "                  ccCheckNAddTwoStates(" + stateNames[0] + ", " + stateNames[1] + ");");
                } else {
                    int[] indices = GetStateSetIndicesForUse(next.epsilonMovesString);
                    bool notTwo = (indices[0] + 1 != indices[1]);

                    if (nextIntersects) {
                        ostr.Write(prefix + "                  ccCheckNAddStates(" + indices[0]);
                        if (notTwo) {
                            jjCheckNAddStatesDualNeeded = true;
                            ostr.Write(", " + indices[1]);
                        } else {
                            jjCheckNAddStatesUnaryNeeded = true;
                        }
                        ostr.WriteLine(");");
                    } else
                        ostr.WriteLine(prefix + "                  ccAddStates(" + indices[0] + ", " + indices[1] + ");");
                }
            }

            if (onlyState)
                ostr.WriteLine("                  break;");
            else
                ostr.WriteLine("                  break;");
        }

        private static void DumpAsciiMoves(TextWriter ostr, int byteNum) {
            bool[] dumped = new bool[Math.Max(generatedStates, dummyStateIndex + 1)];

            DumpHeadForCase(ostr, byteNum);

            foreach (var entry in compositeStateTable) 
                DumpCompositeStatesAsciiMoves(ostr, entry.Key, byteNum, dumped);

            for (int i = 0; i < allStates.Count; i++) {
                NfaState temp = allStates[i];

                if (dumped[temp.stateName] || temp.lexState != LexGen.lexStateIndex ||
                    !temp.HasTransitions() || temp.dummy ||
                    temp.stateName == -1)
                    continue;

                String toPrint = "";

                if (temp.stateForCase != null) {
                    if (temp.inNextOf == 1)
                        continue;

                    if (dumped[temp.stateForCase.stateName])
                        continue;

                    toPrint = (temp.stateForCase.PrintNoBreak(ostr, byteNum, dumped));

                    if (temp.asciiMoves[byteNum] == 0L) {
                        if (toPrint.Equals(""))
                            ostr.WriteLine("                  break;");

                        continue;
                    }
                }

                if (temp.asciiMoves[byteNum] == 0L)
                    continue;

                if (!toPrint.Equals(""))
                    ostr.Write(toPrint);

                dumped[temp.stateName] = true;
                ostr.WriteLine("               case " + temp.stateName + ":");
                temp.DumpAsciiMove(ostr, byteNum, dumped);
            }

            ostr.WriteLine("               default : break;");
            ostr.WriteLine("            }");
            ostr.WriteLine("         } while(i != startsAt);");
        }

        private static void DumpCompositeStatesNonAsciiMoves(TextWriter ostr, string key, bool[] dumped) {
            int i;
            int[] nameSet = allNextStates[key];

            if (nameSet.Length == 1 || dumped[StateNameForComposite(key)])
                return;

            NfaState toBePrinted = null;
            int neededStates = 0;
            NfaState tmp;
            NfaState stateForCase = null;
            String toPrint = "";
            bool stateBlock = stateBlockTable.ContainsKey(key);

            for (i = 0; i < nameSet.Length; i++) {
                tmp = allStates[nameSet[i]];

                if (tmp.nonAsciiMethod != -1) {
                    if (neededStates++ == 1)
                        break;
                    else
                        toBePrinted = tmp;
                } else
                    dumped[tmp.stateName] = true;

                if (tmp.stateForCase != null) {
                    if (stateForCase != null)
                        throw new InvalidOperationException("CSharpCC Bug: Please send a report : ");

                    stateForCase = tmp.stateForCase;
                }
            }

            if (stateForCase != null)
                toPrint = stateForCase.PrintNoBreak(ostr, -1, dumped);

            if (neededStates == 0) {
                if (stateForCase != null && toPrint.Equals(""))
                    ostr.WriteLine("                  break;");

                return;
            }

            if (neededStates == 1) {
                if (!toPrint.Equals(""))
                    ostr.Write(toPrint);

                ostr.WriteLine("               case " + StateNameForComposite(key) + ":");

                if (!dumped[toBePrinted.stateName] && !stateBlock && toBePrinted.inNextOf > 1)
                    ostr.WriteLine("               case " + toBePrinted.stateName + ":");

                dumped[toBePrinted.stateName] = true;
                toBePrinted.DumpNonAsciiMove(ostr, dumped);
                return;
            }

            if (!toPrint.Equals(""))
                ostr.Write(toPrint);

            int keyState = StateNameForComposite(key);
            ostr.WriteLine("               case " + keyState + ":");
            if (keyState < generatedStates)
                dumped[keyState] = true;

            for (i = 0; i < nameSet.Length; i++) {
                tmp = allStates[nameSet[i]];

                if (tmp.nonAsciiMethod != -1) {
                    if (stateBlock)
                        dumped[tmp.stateName] = true;
                    tmp.DumpNonAsciiMoveForCompositeState(ostr);
                }
            }

            if (stateBlock)
                ostr.WriteLine("                  break;");
            else
                ostr.WriteLine("                  break;");
        }

        private void DumpNonAsciiMoveForCompositeState(TextWriter ostr) {
            bool nextIntersects = selfLoop();
            for (int j = 0; j < allStates.Count; j++) {
                NfaState temp1 = allStates[j];

                if (this == temp1 || temp1.stateName == -1 || temp1.dummy ||
                    stateName == temp1.stateName || (temp1.nonAsciiMethod == -1))
                    continue;

                if (!nextIntersects && Intersect(temp1.next.epsilonMovesString,
                    next.epsilonMovesString)) {
                    nextIntersects = true;
                    break;
                }
            }

            if (!Options.getUnicodeEscape() && !unicodeWarningGiven) {
                if (loByteVec != null && loByteVec.Count > 1)
                    ostr.WriteLine("                  if ((ccBitVec" + loByteVec[1] + "[i2" +"] & l2) != 0L)");
            } else {
                ostr.WriteLine("                  if (ccCanMove_" + nonAsciiMethod + "(hiByte, i1, i2, l1, l2))");
            }

            if (kindToPrint != Int32.MaxValue) {
                ostr.WriteLine("                  {");
                ostr.WriteLine("                     if (kind > " + kindToPrint + ")");
                ostr.WriteLine("                        kind = " + kindToPrint + ";");
            }

            if (next != null && next.usefulEpsilonMoves > 0) {
                int[] stateNames = (int[]) allNextStates[next.epsilonMovesString];
                if (next.usefulEpsilonMoves == 1) {
                    int name = stateNames[0];
                    if (nextIntersects)
                        ostr.WriteLine("                     ccCheckNAdd(" + name + ");");
                    else
                        ostr.WriteLine("                     ccStateSet[ccNewStateCnt++] = " + name + ";");
                } else if (next.usefulEpsilonMoves == 2 && nextIntersects) {
                    ostr.WriteLine("                     ccCheckNAddTwoStates(" + stateNames[0] + ", " + stateNames[1] + ");");
                } else {
                    int[] indices = GetStateSetIndicesForUse(next.epsilonMovesString);
                    bool notTwo = (indices[0] + 1 != indices[1]);

                    if (nextIntersects) {
                        ostr.Write("                     ccCheckNAddStates(" + indices[0]);
                        if (notTwo) {
                            jjCheckNAddStatesDualNeeded = true;
                            ostr.Write(", " + indices[1]);
                        } else {
                            jjCheckNAddStatesUnaryNeeded = true;
                        }
                        ostr.WriteLine(");");
                    } else
                        ostr.WriteLine("                     ccAddStates(" + indices[0] + ", " + indices[1] + ");");
                }
            }

            if (kindToPrint != Int32.MaxValue)
                ostr.WriteLine("                  }");
        }

        private void DumpNonAsciiMove(TextWriter ostr, bool[] dumped) {
            bool nextIntersects = selfLoop() && isComposite;

            for (int j = 0; j < allStates.Count; j++) {
                NfaState temp1 = (NfaState) allStates[j];

                if (this == temp1 || temp1.stateName == -1 || temp1.dummy ||
                    stateName == temp1.stateName || (temp1.nonAsciiMethod == -1))
                    continue;

                if (!nextIntersects && Intersect(temp1.next.epsilonMovesString,
                    next.epsilonMovesString))
                    nextIntersects = true;

                if (!dumped[temp1.stateName] && !temp1.isComposite &&
                    nonAsciiMethod == temp1.nonAsciiMethod &&
                    kindToPrint == temp1.kindToPrint &&
                    (next.epsilonMovesString == temp1.next.epsilonMovesString ||
                     (next.epsilonMovesString != null &&
                      temp1.next.epsilonMovesString != null &&
                      next.epsilonMovesString.Equals(temp1.next.epsilonMovesString)))) {
                    dumped[temp1.stateName] = true;
                    ostr.WriteLine("               case " + temp1.stateName + ":");
                }
            }

            if (next == null || next.usefulEpsilonMoves <= 0) {
                String kindCheck = " && kind > " + kindToPrint;

                if (!Options.getUnicodeEscape() && !unicodeWarningGiven) {
                    if (loByteVec != null && loByteVec.Count > 1)
                        ostr.WriteLine("                  if ((ccBitVec" + loByteVec[1] + "[i2" + "] & l2) != 0L" + kindCheck + ")");
                } else {
                    ostr.WriteLine("                  if (ccCanMove_" + nonAsciiMethod + "(hiByte, i1, i2, l1, l2)" + kindCheck + ")");
                }
                ostr.WriteLine("                     kind = " + kindToPrint + ";");
                ostr.WriteLine("                  break;");
                return;
            }

            String prefix = "   ";
            if (kindToPrint != Int32.MaxValue) {
                if (!Options.getUnicodeEscape() && !unicodeWarningGiven) {
                    if (loByteVec != null && loByteVec.Count > 1) {
                        ostr.WriteLine("                  if ((ccBitVec" + loByteVec[1] + "[i2" + "] & l2) == 0L)");
                        ostr.WriteLine("                     break;");
                    }
                } else {
                    ostr.WriteLine("                  if (!ccCanMove_" + nonAsciiMethod + "(hiByte, i1, i2, l1, l2))");
                    ostr.WriteLine("                     break;");
                }

                ostr.WriteLine("                  if (kind > " + kindToPrint + ")");
                ostr.WriteLine("                     kind = " + kindToPrint + ";");
                prefix = "";
            } else if (!Options.getUnicodeEscape() && !unicodeWarningGiven) {
                if (loByteVec != null && loByteVec.Count > 1)
                    ostr.WriteLine("                  if ((ccBitVec" + loByteVec[1] + "[i2" + "] & l2) != 0L)");
            } else {
                ostr.WriteLine("                  if (ccCanMove_" + nonAsciiMethod + "(hiByte, i1, i2, l1, l2))");
            }

            if (next != null && next.usefulEpsilonMoves > 0) {
                int[] stateNames = allNextStates[next.epsilonMovesString];
                if (next.usefulEpsilonMoves == 1) {
                    int name = stateNames[0];
                    if (nextIntersects)
                        ostr.WriteLine(prefix + "                  ccCheckNAdd(" + name + ");");
                    else
                        ostr.WriteLine(prefix + "                  ccStateSet[ccNewStateCnt++] = " + name + ";");
                } else if (next.usefulEpsilonMoves == 2 && nextIntersects) {
                    ostr.WriteLine(prefix + "                  ccCheckNAddTwoStates(" + stateNames[0] + ", " + stateNames[1] + ");");
                } else {
                    int[] indices = GetStateSetIndicesForUse(next.epsilonMovesString);
                    bool notTwo = (indices[0] + 1 != indices[1]);

                    if (nextIntersects) {
                        ostr.Write(prefix + "                  ccCheckNAddStates(" + indices[0]);
                        if (notTwo) {
                            jjCheckNAddStatesDualNeeded = true;
                            ostr.Write(", " + indices[1]);
                        } else {
                            jjCheckNAddStatesUnaryNeeded = true;
                        }
                        ostr.WriteLine(");");
                    } else
                        ostr.WriteLine(prefix + "                  ccAddStates(" + indices[0] + ", " + indices[1] + ");");
                }
            }

            ostr.WriteLine("                  break;");
        }

        public static void DumpCharAndRangeMoves(TextWriter ostr) {
            bool[] dumped = new bool[Math.Max(generatedStates, dummyStateIndex + 1)];
            int i;

            DumpHeadForCase(ostr, -1);

            foreach (var entry in compositeStateTable)
                DumpCompositeStatesNonAsciiMoves(ostr, entry.Key, dumped);

            for (i = 0; i < allStates.Count; i++) {
                NfaState temp = allStates[i];

                if (temp.stateName == -1 || dumped[temp.stateName] || temp.lexState != LexGen.lexStateIndex ||
                    !temp.HasTransitions() || temp.dummy)
                    continue;

                String toPrint = "";

                if (temp.stateForCase != null) {
                    if (temp.inNextOf == 1)
                        continue;

                    if (dumped[temp.stateForCase.stateName])
                        continue;

                    toPrint = (temp.stateForCase.PrintNoBreak(ostr, -1, dumped));

                    if (temp.nonAsciiMethod == -1) {
                        if (toPrint.Equals(""))
                            ostr.WriteLine("                  break;");

                        continue;
                    }
                }

                if (temp.nonAsciiMethod == -1)
                    continue;

                if (!toPrint.Equals(""))
                    ostr.Write(toPrint);

                dumped[temp.stateName] = true;
                //System.out.println("case : " + temp.stateName);
                ostr.WriteLine("               case " + temp.stateName + ":");
                temp.DumpNonAsciiMove(ostr, dumped);
            }

            ostr.WriteLine("               default : break;");
            ostr.WriteLine("            }");
            ostr.WriteLine("         } while(i != startsAt);");
        }

        public static void DumpNonAsciiMoveMethods(TextWriter ostr) {
            if (!Options.getUnicodeEscape() && !unicodeWarningGiven)
                return;

            if (nonAsciiTableForMethod.Count <= 0)
                return;

            for (int i = 0; i < nonAsciiTableForMethod.Count; i++) {
                NfaState tmp = (NfaState) nonAsciiTableForMethod[i];
                tmp.DumpNonAsciiMoveMethod(ostr);
            }
        }

        private void DumpNonAsciiMoveMethod(TextWriter ostr) {
            int j;
            ostr.WriteLine("private static readonly bool ccCanMove_" + nonAsciiMethod +"(int hiByte, int i1, int i2, long l1, long l2)");
            ostr.WriteLine("{");
            ostr.WriteLine("   switch(hiByte)");
            ostr.WriteLine("   {");

            if (loByteVec != null && loByteVec.Count > 0) {
                for (j = 0; j < loByteVec.Count; j += 2) {
                    ostr.WriteLine("      case " + loByteVec[j] + ":");
                    if (!AllBitsSet(allBitVectors[loByteVec[j + 1]])) {
                        ostr.WriteLine("         return ((ccBitVec" + loByteVec[j+ 1] + "[i2" + "] & l2) != 0L);");
                    } else
                        ostr.WriteLine("            return true;");
                }
            }

            ostr.WriteLine("      default :");

            if (nonAsciiMoveIndices != null &&
                (j = nonAsciiMoveIndices.Length) > 0) {
                do {
                    if (!AllBitsSet(allBitVectors[nonAsciiMoveIndices[j - 2]]))
                        ostr.WriteLine("         if ((ccBitVec" + nonAsciiMoveIndices[j - 2] + "[i1] & l1) != 0L)");
                    if (!AllBitsSet(allBitVectors[nonAsciiMoveIndices[j - 1]])) {
                        ostr.WriteLine("            if ((ccBitVec" + nonAsciiMoveIndices[j - 1] + "[i2] & l2) == 0L)");
                        ostr.WriteLine("               return false;");
                        ostr.WriteLine("            else");
                    }
                    ostr.WriteLine("            return true;");
                } while ((j -= 2) > 0);
            }

            ostr.WriteLine("         return false;");
            ostr.WriteLine("   }");
            ostr.WriteLine("}");
        }

        private static void ReArrange() {
            IList<NfaState> v = allStates;
            allStates = new List<NfaState>();
            for (int i = 0; i < generatedStates; i++) {
                allStates.Add(null);
            }

            if (allStates.Count != generatedStates)
                throw new InvalidOperationException("What??");

            for (int j = 0; j < v.Count; j++) {
                NfaState tmp = v[j];
                if (tmp.stateName != -1 && !tmp.dummy)
                    allStates[tmp.stateName] = tmp;
            }
        }

        //private static bool boilerPlateDumped = false;
        internal static void PrintBoilerPlate(TextWriter ostr) {
            ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + "void " +"ccCheckNAdd(int state)");
            ostr.WriteLine("{");
            ostr.WriteLine("   if (ccRounds[state] != ccRound)");
            ostr.WriteLine("   {");
            ostr.WriteLine("      ccStateSet[ccNewStateCnt++] = state;");
            ostr.WriteLine("      ccRounds[state] = ccRound;");
            ostr.WriteLine("   }");
            ostr.WriteLine("}");

            ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + " void " + "ccAddStates(int start, int end)");
            ostr.WriteLine("{");
            ostr.WriteLine("   do {");
            ostr.WriteLine("      ccStateSet[ccNewStateCnt++] = ccNextStates[start];");
            ostr.WriteLine("   } while (start++ != end);");
            ostr.WriteLine("}");

            ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + " void " + "ccCheckNAddTwoStates(int state1, int state2)");
            ostr.WriteLine("{");
            ostr.WriteLine("   ccCheckNAdd(state1);");
            ostr.WriteLine("   ccCheckNAdd(state2);");
            ostr.WriteLine("}");
            ostr.WriteLine("");
            if (jjCheckNAddStatesDualNeeded) {
                ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + "void " + "ccCheckNAddStates(int start, int end)");
                ostr.WriteLine("{");
                ostr.WriteLine("   do {");
                ostr.WriteLine("      ccCheckNAdd(ccNextStates[start]);");
                ostr.WriteLine("   } while (start++ != end);");
                ostr.WriteLine("}");
                ostr.WriteLine("");
            }

            if (jjCheckNAddStatesUnaryNeeded) {
                ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + "void " + "ccCheckNAddStates(int start)");
                ostr.WriteLine("{");
                ostr.WriteLine("   ccCheckNAdd(ccNextStates[start]);");
                ostr.WriteLine("   ccCheckNAdd(ccNextStates[start + 1]);");
                ostr.WriteLine("}");
                ostr.WriteLine("");
            }
        }

        private static void FindStatesWithNoBreak() {
            Dictionary<string, string> printed = new Dictionary<string, string>();
            bool[] put = new bool[generatedStates];
            int cnt = 0;
            int i, j, foundAt = 0;

            for (j = 0; j < allStates.Count; j++) {
                NfaState stateForCase = null;
                NfaState tmpState = allStates[j];

                if (tmpState.stateName == -1 || tmpState.dummy || !tmpState.UsefulState() ||
                    tmpState.next == null || tmpState.next.usefulEpsilonMoves < 1)
                    continue;

                String s = tmpState.next.epsilonMovesString;

                if (compositeStateTable.ContainsKey(s) || 
                    printed.ContainsKey(s))
                    continue;

                printed[s] = s;
                int[] nexts = allNextStates[s];

                if (nexts.Length == 1)
                    continue;

                int state = cnt;
                //System.out.println("State " + tmpState.stateName + " : " + s);
                for (i = 0; i < nexts.Length; i++) {
                    if ((state = nexts[i]) == -1)
                        continue;

                    NfaState tmp = allStates[state];

                    if (!tmp.isComposite && tmp.inNextOf == 1) {
                        if (put[state])
                            throw new InvalidOperationException("CSharpCC Bug: Please send a report");

                        foundAt = i;
                        cnt++;
                        stateForCase = tmp;
                        put[state] = true;

                        //System.out.print(state + " : " + tmp.inNextOf + ", ");
                        break;
                    }
                }
                //System.out.println("");

                if (stateForCase == null)
                    continue;

                for (i = 0; i < nexts.Length; i++) {
                    if ((state = nexts[i]) == -1)
                        continue;

                    NfaState tmp = allStates[state];

                    if (!put[state] && tmp.inNextOf > 1 && !tmp.isComposite && tmp.stateForCase == null) {
                        cnt++;
                        nexts[i] = -1;
                        put[state] = true;

                        int toSwap = nexts[0];
                        nexts[0] = nexts[foundAt];
                        nexts[foundAt] = toSwap;

                        tmp.stateForCase = stateForCase;
                        stateForCase.stateForCase = tmp;
                        stateSetsToFix[s] = nexts;

                        //System.out.println("For : " + s + "; " + stateForCase.stateName +
                        //" and " + tmp.stateName);

                        goto Outer;
                    }
                }

                for (i = 0; i < nexts.Length; i++) {
                    if ((state = nexts[i]) == -1)
                        continue;

                    NfaState tmp = (NfaState) allStates[state];
                    if (tmp.inNextOf <= 1)
                        put[state] = false;
                }
            }

            Outer:
            ;
        }

        private static int[][] kinds;
        private static int[][][] statesForState;

        public static void DumpMoveNfa(TextWriter ostr) {
            //if (!boilerPlateDumped)
            //   PrintBoilerPlate(ostr);

            //boilerPlateDumped = true;
            int i;
            int[] kindsForStates = null;

            if (kinds == null) {
                kinds = new int[LexGen.maxLexStates][];
                statesForState = new int[LexGen.maxLexStates][][];
            }

            ReArrange();

            for (i = 0; i < allStates.Count; i++) {
                NfaState temp = allStates[i];

                if (temp.lexState != LexGen.lexStateIndex ||
                    !temp.HasTransitions() || temp.dummy ||
                    temp.stateName == -1)
                    continue;

                if (kindsForStates == null) {
                    kindsForStates = new int[generatedStates];
                    statesForState[LexGen.lexStateIndex] = new int[Math.Max(generatedStates, dummyStateIndex + 1)][];
                }

                kindsForStates[temp.stateName] = temp.lookingFor;
                statesForState[LexGen.lexStateIndex][temp.stateName] = temp.compositeStates;

                temp.GenerateNonAsciiMoves(ostr);
            }

            foreach (var entry in stateNameForComposite) {
                int state = entry.Value;

                if (state >= generatedStates)
                    statesForState[LexGen.lexStateIndex][state] = allNextStates[entry.Key];
            }

            if (stateSetsToFix.Count != 0)
                FixStateSets();

            kinds[LexGen.lexStateIndex] = kindsForStates;

            ostr.WriteLine("private " + (Options.getStatic() ? "static " : "") + " int " + "ccMoveNfa" + LexGen.lexStateSuffix + "(int startState, int curPos)");
            ostr.WriteLine("{");

            if (generatedStates == 0) {
                ostr.WriteLine("   return curPos;");
                ostr.WriteLine("}");
                return;
            }

            if (LexGen.mixed[LexGen.lexStateIndex]) {
                ostr.WriteLine("   int strKind = ccMatchedKind;");
                ostr.WriteLine("   int strPos = ccMatchedPos;");
                ostr.WriteLine("   int seenUpto;");
                ostr.WriteLine("   inputStream.Backup(seenUpto = curPos + 1);");
                ostr.WriteLine("   try { curChar = inputStream.ReadChar(); }");
                ostr.WriteLine("   catch(System.IO.IOException) { throw new System.InvalidOperationException(\"Internal Error\"); }");
                ostr.WriteLine("   curPos = 0;");
            }

            ostr.WriteLine("   int startsAt = 0;");
            ostr.WriteLine("   ccNewStateCnt = " + generatedStates + ";");
            ostr.WriteLine("   int i = 1;");
            ostr.WriteLine("   ccStateSet[0] = startState;");

            if (Options.getDebugTokenManager())
                ostr.WriteLine("      debugStream.WriteLine(\"   Starting NFA to match one of : \" + " +
                               "ccKindsForStateVector(curLexState, ccStateSet, 0, 1));");

            if (Options.getDebugTokenManager())
                ostr.WriteLine("      debugStream.WriteLine(" + (LexGen.maxLexStates > 1
                    ? "\"<\" + lexStateNames[curLexState] + \">\" + "
                    : "") + "\"Current character : \" + " +
                               "TokenMgrError.AddEscapes(curChar.ToString()) + \" (\" + (int)curChar + \") " +
                               "at line \" + inputStream.EndLine + \" column \" + inputStream.EndColumn);");

            ostr.WriteLine("   int kind = Int32.MaxValue;");
            ostr.WriteLine("   for (;;)");
            ostr.WriteLine("   {");
            ostr.WriteLine("      if (++ccRound == Int32.MaxValue)");
            ostr.WriteLine("         ReInitRounds();");
            ostr.WriteLine("      if (curChar < 64)");
            ostr.WriteLine("      {");

            DumpAsciiMoves(ostr, 0);

            ostr.WriteLine("      }");

            ostr.WriteLine("      else if (curChar < 128)");

            ostr.WriteLine("      {");

            DumpAsciiMoves(ostr, 1);

            ostr.WriteLine("      }");

            ostr.WriteLine("      else");
            ostr.WriteLine("      {");

            DumpCharAndRangeMoves(ostr);

            ostr.WriteLine("      }");

            ostr.WriteLine("      if (kind != Int32.MaxValue)");
            ostr.WriteLine("      {");
            ostr.WriteLine("         ccMatchedKind = kind;");
            ostr.WriteLine("         ccMatchedPos = curPos;");
            ostr.WriteLine("         kind = Int32.MaxValue;");
            ostr.WriteLine("      }");
            ostr.WriteLine("      ++curPos;");

            if (Options.getDebugTokenManager()) {
                ostr.WriteLine("      if (ccMatchedKind != 0 && ccMatchedKind != Int32.MaxValue)");
                ostr.WriteLine("         debugStream.WriteLine(" +
                               "\"   Currently matched the first \" + (ccMatchedPos + 1) + \" characters as" +
                               " a \" + tokenImage[ccMatchedKind] + \" token.\");");
            }

            ostr.WriteLine("      if ((i = ccNewStateCnt) == (startsAt = " + generatedStates + " - (ccNewStateCnt = startsAt)))");
            if (LexGen.mixed[LexGen.lexStateIndex])
                ostr.WriteLine("         break;");
            else
                ostr.WriteLine("         return curPos;");

            if (Options.getDebugTokenManager())
                ostr.WriteLine("      debugStream.WriteLine(\"   Possible kinds of longer matches : \" + " + "ccKindsForStateVector(curLexState, ccStateSet, startsAt, i));");

            ostr.WriteLine("      try { curChar = inputStream.ReadChar(); }");

            if (LexGen.mixed[LexGen.lexStateIndex])
                ostr.WriteLine("      catch(System.IO.IOException) { break; }");
            else
                ostr.WriteLine("      catch(System.IO.IOException) { return curPos; }");

            if (Options.getDebugTokenManager())
                ostr.WriteLine("      debugStream.WriteLine(" + (LexGen.maxLexStates > 1
                    ? "\"<\" + lexStateNames[curLexState] + \">\" + "
                    : "") + "\"Current character : \" + " +
                               "TokenMgrError.AddEscapes(curChar.ToString()) + \" (\" + (int)curChar + \") " +
                               "at line \" + inputStream.EndLine + \" column \" + inputStream.EndColumn);");

            ostr.WriteLine("   }");

            if (LexGen.mixed[LexGen.lexStateIndex]) {
                ostr.WriteLine("   if (ccMatchedPos > strPos)");
                ostr.WriteLine("      return curPos;");
                ostr.WriteLine("");
                ostr.WriteLine("   int toRet = System.Math.Max(curPos, seenUpto);");
                ostr.WriteLine("");
                ostr.WriteLine("   if (curPos < toRet)");
                ostr.WriteLine("      for (i = toRet - System.Math.Min(curPos, seenUpto); i-- > 0; )");
                ostr.WriteLine("         try { curChar = inputStream.ReadChar(); }");
                ostr.WriteLine("         catch(System.IO.IOException e) { " +
                               "throw new InvalidOperationException(\"Internal Error : Please send a bug report.\"); }");
                ostr.WriteLine("");
                ostr.WriteLine("   if (ccMatchedPos < strPos)");
                ostr.WriteLine("   {");
                ostr.WriteLine("      ccMatchedKind = strKind;");
                ostr.WriteLine("      ccMatchedPos = strPos;");
                ostr.WriteLine("   }");
                ostr.WriteLine("   else if (ccMatchedPos == strPos && ccMatchedKind > strKind)");
                ostr.WriteLine("      ccMatchedKind = strKind;");
                ostr.WriteLine("");
                ostr.WriteLine("   return toRet;");
            }

            ostr.WriteLine("}");
            allStates.Clear();
        }

        public static void DumpStatesForState(TextWriter ostr) {
            ostr.Write("internal static readonly int[][][] statesForState = ");

            if (statesForState == null) {
                ostr.WriteLine("null;");
                return;
            } else
                ostr.WriteLine("{");

            for (int i = 0; i < statesForState.Length; i++) {

                if (statesForState[i] == null) {
                    ostr.WriteLine(" null,");
                    continue;
                }

                ostr.WriteLine(" {");

                for (int j = 0; j < statesForState[i].Length; j++) {
                    int[] stateSet = statesForState[i][j];

                    if (stateSet == null) {
                        ostr.WriteLine("   { " + j + " },");
                        continue;
                    }

                    ostr.Write("   { ");

                    for (int k = 0; k < stateSet.Length; k++)
                        ostr.Write(stateSet[k] + ", ");

                    ostr.WriteLine("},");
                }
                ostr.WriteLine(" },");
            }
            ostr.WriteLine("\n};");
        }

        public static void DumpStatesForKind(TextWriter ostr) {
            DumpStatesForState(ostr);
            bool moreThanOne = false;
            int cnt = 0;

            ostr.Write("internal static readonly int[][] kindForState = ");

            if (kinds == null) {
                ostr.WriteLine("null;");
                return;
            } else
                ostr.WriteLine("{");

            for (int i = 0; i < kinds.Length; i++) {
                if (moreThanOne)
                    ostr.WriteLine(",");
                moreThanOne = true;

                if (kinds[i] == null)
                    ostr.WriteLine("null");
                else {
                    cnt = 0;
                    ostr.Write("{ ");
                    for (int j = 0; j < kinds[i].Length; j++) {
                        if (cnt++ > 0)
                            ostr.Write(",");

                        if (cnt%15 == 0)
                            ostr.Write("\n  ");
                        else if (cnt > 1)
                            ostr.Write(" ");

                        ostr.Write(kinds[i][j]);
                    }
                    ostr.Write("}");
                }
            }
            ostr.WriteLine("\n};");
        }

        public static void reInit() {
            unicodeWarningGiven = false;
            generatedStates = 0;
            idCnt = 0;
            lohiByteCnt = 0;
            dummyStateIndex = -1;
            done = false;
            mark = null;
            stateDone = null;
            allStates = new List<NfaState>();
            indexedAllStates = new List<NfaState>();
            nonAsciiTableForMethod = new List<NfaState>();
            equivStatesTable = new Dictionary<string, NfaState>();
            allNextStates = new Dictionary<string, int[]>();
            lohiByteTab = new Dictionary<string, int>();
            stateNameForComposite = new Dictionary<string, int>();
            compositeStateTable = new Dictionary<string, int[]>();
            stateBlockTable = new Hashtable();
            stateSetsToFix = new Dictionary<string, int[]>();
            allBitVectors = new List<string>();
            tmpIndices = new int[512];
            allBits = "{\n   Int64.MaxValue /* 0xffffffffffffffffL */, " +
                      "Int64.MaxValue /* 0xffffffffffffffffL */, " +
                      "Int64.MaxValue /* 0xffffffffffffffffL */, " +
                      "Int64.MaxValue /* 0xffffffffffffffffL */\n};";
            tableToDump = new Dictionary<string, int[]>();
            orderedStateSet = new List<int[]>();
            lastIndex = 0;
            //boilerPlateDumped = false;
            jjCheckNAddStatesUnaryNeeded = false;
            jjCheckNAddStatesDualNeeded = false;
            kinds = null;
            statesForState = null;
        }
    }
}