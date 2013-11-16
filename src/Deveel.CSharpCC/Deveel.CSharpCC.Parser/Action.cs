using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
    public class Action : Expansion {
        private readonly List<Token> actionTokens;

        public Action() {
            actionTokens = new List<Token>();
        }

        internal IList<Token> ActionTokens {
            get { return actionTokens; }
        }

        public override StringBuilder Dump(int indent, IList alreadyDumped) {
            var sb = base.Dump(indent, alreadyDumped);
            alreadyDumped.Add(this);
            if (actionTokens.Count > 0) {
                sb.Append(' ')
                    .Append(actionTokens[0]);
            }

            return sb;
        }
    }
}