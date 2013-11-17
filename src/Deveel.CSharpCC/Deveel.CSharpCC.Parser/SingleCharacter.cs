using System;

namespace Deveel.CSharpCC.Parser {
	public class SingleCharacter {
		public SingleCharacter(char c) {
			Character = c;
		}

		public SingleCharacter() {
		}

		public int Line { get; internal set; }

		public int Column { get; internal set; }

		public char Character { get; internal set; }
	}
}