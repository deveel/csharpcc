using System;
using System.Collections;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	public class OneOrMore : Expansion {
		public OneOrMore(Token token, Expansion expansion) {
			Column = token.beginColumn;
			Line = token.beginLine;
			Expansion = expansion;
			Expansion.Parent = this;
		}

		public Expansion Expansion { get; internal set; }

		public override StringBuilder Dump(int indent, IList alreadyDumped) {
			StringBuilder sb = base.Dump(indent, alreadyDumped);
			if (alreadyDumped.Contains(this))
				return sb;

			alreadyDumped.Add(this);
			sb.AppendLine()
				.Append(Expansion.Dump(indent + 1, alreadyDumped));
			return sb;
		}
	}
}