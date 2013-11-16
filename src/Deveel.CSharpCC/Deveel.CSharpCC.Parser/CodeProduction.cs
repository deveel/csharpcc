using System;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
    public class CodeProdution {
        private readonly IList<Token> codeTokens;

        public CodeProdution() {
            codeTokens = new List<Token>();
        }

        public IList<Token> CodeTokens {
            get { return codeTokens; }
        }
    }
}