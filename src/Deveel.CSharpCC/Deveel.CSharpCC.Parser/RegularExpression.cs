using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
    public abstract class RegularExpression : Expansion {
        private readonly IList<Token> lhsTokens;

        protected RegularExpression() {
            lhsTokens = new List<Token>();
        }

        public string Label { get; internal set; }

        internal int Ordinal { get; set; }

        public Token RhsToken { get; internal set; }

        public IList<Token> LhsTokens {
            get { return lhsTokens; }
        }

        public bool IsPrivate { get; internal set; }

        public TokenProduction TokenProductionContext { get; internal set; }

        public virtual bool CanMatchAnyChar {
            get { return false; }
        }

        internal int WalkStatus { get; set; }

        public abstract Nfa GenerateNfa(bool ignoreCase);

        public override StringBuilder Dump(int indent, IList alreadyDumped) {
            var sb = base.Dump(indent, alreadyDumped);
            alreadyDumped.Add(this);
            sb.Append(' ').Append(Label);
            return sb;
        }
    }
}