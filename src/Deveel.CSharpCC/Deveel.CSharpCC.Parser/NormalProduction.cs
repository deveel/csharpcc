using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
    public class NormalProduction {
        private readonly IList<Token> returnTypeTokens;
        private readonly IList<Token> parameterTokens;

        public NormalProduction() {
            LeIndex = 0;
            returnTypeTokens = new List<Token>();
            parameterTokens = new List<Token>();
        }

        public Expansion Expansion { get; internal set; }

        internal bool IsEmptyPossible { get; set; }

        internal NormalProduction[] LeftExpansions { get; set; }

        internal int WalkStatus { get; set; }

        internal Token FirstToken { get; set; }

        internal Token LastToken { get; set; }

        public string AccessModifier { get; internal set; }

        public int Column { get; internal set; }

        public int Line { get; internal set; }

        public string Lhs { get; internal set; }

        internal IList<NonTerminal> Parents { get; set; }

        public IList<Token> ReturnTypeTokens {
            get { return returnTypeTokens; }
        }

        internal int LeIndex { get; set; }

        public IList<Token> ParameterTokens {
            get { return parameterTokens; }
        }

        protected StringBuilder DumpPrefix(int indent)
        {
            var sb = new StringBuilder(128);
            for (int i = 0; i < indent; i++)
                sb.Append("  ");
            return sb;
        }

        public virtual StringBuilder Dump(int indent, IList alreadyDumped) {
            StringBuilder sb = DumpPrefix(indent)
                .Append(GetHashCode())
                .Append(' ')
                .Append(GetType().Name)
                .Append(' ')
                .Append(Lhs);

            if (!alreadyDumped.Contains(this)) {
                alreadyDumped.Add(this);
                if (Expansion != null) {
                    sb.AppendLine()
                        .Append(Expansion.Dump(indent + 1, alreadyDumped));
                }
            }

            return sb;
        }
    }
}