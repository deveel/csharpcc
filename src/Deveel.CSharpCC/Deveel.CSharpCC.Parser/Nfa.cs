using System;

namespace Deveel.CSharpCC.Parser {
    public class Nfa {
        internal Nfa(NfaState start, NfaState end) {
            Start = start;
            End = end;
        }

	    public Nfa() {
			Start = new NfaState();
			End = new NfaState();
	    }

	    internal NfaState Start { get; set; }

        internal NfaState End { get; set; }
    }
}