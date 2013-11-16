using System;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
    public class BnfProduction : NormalProduction {
        private readonly IList<Token> declarationTokens;

        public BnfProduction() {
            declarationTokens = new List<Token>();
        }

        public IList<Token> DeclarationTokens {
            get { return declarationTokens; }
        }

        public bool IsJumpPatched { get; internal set; }
    }
}