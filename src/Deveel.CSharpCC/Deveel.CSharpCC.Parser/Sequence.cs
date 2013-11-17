using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Deveel.CSharpCC.Parser {
	public class Sequence : Expansion {
		private readonly List<Expansion> units = new List<Expansion>();

		public Sequence() {
		}

		public Sequence(Token token, Lookahead la) {
			Line = token.beginLine;
			Column = token.beginColumn;
			Units.Add(la);
		}

		public List<Expansion> Units {
			get { return units; }
		}

		public override StringBuilder Dump(int indent, IList alreadyDumped) {
			if (alreadyDumped.Contains(this)) {
				return base.Dump(0, alreadyDumped).Insert(0, '[').Append(']').Insert(0, DumpPrefix(indent));
			}

			alreadyDumped.Add(this);
			StringBuilder sb = base.Dump(indent, alreadyDumped);
			foreach (var next in units) {
				sb.AppendLine()
					.Append(next.Dump(indent + 1, alreadyDumped));
			}
			return sb;

		}
	}
}