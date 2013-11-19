using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
    public class NonTerminal : Expansion {
        public string Name { get; internal set; }

        public IList<Token> ArgumentTokens { get; internal set; }

        public IList<Token> LhsTokens { get; internal set; }

        public NormalProduction Production { get; internal set; }

        public override StringBuilder Dump(int indent, IList alreadyDumped) {
            return base.Dump(indent, alreadyDumped).Append(' ').Append(Name);
        }
    }
}