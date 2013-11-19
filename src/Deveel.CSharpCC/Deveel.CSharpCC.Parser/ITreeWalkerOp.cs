using System;

namespace Deveel.CSharpCC.Parser {
    interface ITreeWalkerOp {
        bool GoDeeper(Expansion e);

        void Action(Expansion e);
    }
}