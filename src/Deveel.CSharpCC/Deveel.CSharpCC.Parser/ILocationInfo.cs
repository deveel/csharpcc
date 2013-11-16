using System;

namespace Deveel.CSharpCC.Parser {
    public interface ILocationInfo {
        int Column { get; }

        int Line { get; }
    }
}