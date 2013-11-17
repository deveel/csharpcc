using System;
using System.Collections;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	public class ZeroOrOne : Expansion {
		public ZeroOrOne(Token token, Expansion expansion) {
			Line = token.beginLine;
			Column = token.beginColumn;
			Expansion = expansion;
		}

		public ZeroOrOne() {
		}

		public Expansion Expansion { get; private set; }

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