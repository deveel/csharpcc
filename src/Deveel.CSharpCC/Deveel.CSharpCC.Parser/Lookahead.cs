using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	public class Lookahead : Expansion {
		private readonly List<Token> actionTokens = new List<Token>();

		public Lookahead() {
			Amount = Int32.MaxValue;
		}

		public bool IsExplicit { get; internal set; }

		public int Amount { get; internal set; }

		public List<Token> ActionTokens {
			get { return actionTokens; }
		}

		public Expansion Expansion { get; internal set; }

		public override System.Text.StringBuilder Dump(int indent, IList alreadyDumped) {
			StringBuilder sb = base.Dump(indent, alreadyDumped)
				.Append(IsExplicit ? " explicit" : " implicit");
			if (alreadyDumped.Contains(this))
				return sb;

			alreadyDumped.Add(this);
			sb.AppendLine()
				.Append(Expansion.Dump(indent + 1, alreadyDumped));
			return sb;

		}
	}
}