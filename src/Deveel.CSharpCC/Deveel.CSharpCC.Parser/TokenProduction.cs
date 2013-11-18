using System;
using System.Collections.Generic;

namespace Deveel.CSharpCC.Parser {
    public class TokenProduction {
        private readonly IList<RegExprSpec> respecs;

		  public const int TOKEN  = 0,
                          SKIP   = 1,
                          MORE   = 2,
                          SPECIAL = 3;

		  public static readonly String[] kindImage = {
    "TOKEN", "SKIP", "MORE", "SPECIAL"
  };

        public TokenProduction() {
            respecs = new List<RegExprSpec>();
        }

        public int Column { get; internal set; }

        public int Line { get; internal set; }

        public string[] LexStates { get; internal set; }

        public int Kind { get; internal set; }

        public IList<RegExprSpec> RegexSpecs {
            get { return respecs; }
        }

        public bool IsExplicit { get; internal set; }

        public bool IgnoreCase { get; internal set; }

        public Token FirstToken { get; internal set; }

        public Token LastToken { get; internal set; }
    }
}