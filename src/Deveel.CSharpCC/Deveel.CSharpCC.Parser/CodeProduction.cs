using System;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
    public class CodeProduction : NormalProduction {
        private readonly IList<Token> codeTokens;

        public CodeProduction() {
            codeTokens = new List<Token>();
        }

        public IList<Token> CodeTokens {
            get { return codeTokens; }
        }
    }
}