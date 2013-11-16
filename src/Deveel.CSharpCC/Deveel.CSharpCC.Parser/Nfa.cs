using System;

namespace Deveel.CSharpCC.Parser {
    public class Nfa {
        internal Nfa(NfaState start, NfaState end) {
            Start = start;
            End = end;
        }

        internal NfaState Start { get; set; }

        internal NfaState End { get; set; }
    }
}