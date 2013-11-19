using System;

namespace Deveel.CSharpCC.Parser {
    static class ExpansionTreeWalker {
        public static void PreOrderWalk(Expansion node, ITreeWalkerOp opObj) {
            opObj.Action(node);
            if (opObj.GoDeeper(node)) {
                if (node is Choice) {
                    foreach (var choice in ((Choice) node).Choices)
                        PreOrderWalk(choice, opObj);
                } else if (node is Sequence) {
                    foreach (var expansion in ((Sequence)node).Units) {
                        PreOrderWalk(expansion, opObj);
                    }
                } else if (node is OneOrMore) {
                    PreOrderWalk(((OneOrMore) node).Expansion, opObj);
                } else if (node is ZeroOrMore) {
                    PreOrderWalk(((ZeroOrMore) node).Expansion, opObj);
                } else if (node is ZeroOrOne) {
                    PreOrderWalk(((ZeroOrOne) node).Expansion, opObj);
                } else if (node is Lookahead) {
                    Expansion nestedE = ((Lookahead) node).Expansion;
                    if (!(nestedE is Sequence && ((Sequence) nestedE).Units[0] == node)) {
                        PreOrderWalk(nestedE, opObj);
                    }
                } else if (node is TryBlock) {
                    PreOrderWalk(((TryBlock) node).Expansion, opObj);
                } else if (node is RChoice) {
                    foreach (var choice in ((RChoice)node).Choices) {
                        PreOrderWalk(choice, opObj);
                    }
                } else if (node is RSequence) {
                    foreach (var unit in ((RSequence)node).Units) {
                        PreOrderWalk(unit, opObj);
                    }
                } else if (node is ROneOrMore) {
                    PreOrderWalk(((ROneOrMore) node).RegularExpression, opObj);
                } else if (node is RZeroOrMore) {
                    PreOrderWalk(((RZeroOrMore) node).RegularExpression, opObj);
                } else if (node is RZeroOrOne) {
                    PreOrderWalk(((RZeroOrOne) node).RegularExpression, opObj);
                } else if (node is RRepetitionRange) {
                    PreOrderWalk(((RRepetitionRange) node).RegularExpression, opObj);
                }
            }
        }

        internal static void PostOrderWalk(Expansion node, ITreeWalkerOp opObj) {
            if (opObj.GoDeeper(node)) {
                if (node is Choice) {
                    foreach (var choice in ((Choice)node).Choices) {
                        PostOrderWalk(choice, opObj);
                    }
                } else if (node is Sequence) {
                    foreach (var unit in ((Sequence)node).Units) {
                        PostOrderWalk(unit, opObj);
                    }
                } else if (node is OneOrMore) {
                    PostOrderWalk(((OneOrMore) node).Expansion, opObj);
                } else if (node is ZeroOrMore) {
                    PostOrderWalk(((ZeroOrMore) node).Expansion, opObj);
                } else if (node is ZeroOrOne) {
                    PostOrderWalk(((ZeroOrOne) node).Expansion, opObj);
                } else if (node is Lookahead) {
                    Expansion nestedE = ((Lookahead) node).Expansion;
                    if (!(nestedE is Sequence && ((Sequence) nestedE).Units[0] == node)) {
                        PostOrderWalk(nestedE, opObj);
                    }
                } else if (node is TryBlock) {
                    PostOrderWalk(((TryBlock) node).Expansion, opObj);
                } else if (node is RChoice) {
                    foreach (var choice in ((RChoice)node).Choices) {
                        PostOrderWalk(choice, opObj);
                    }
                } else if (node is RSequence) {
                    foreach (var unit in ((RSequence)node).Units) {
                        PostOrderWalk(unit, opObj);
                    }
                } else if (node is ROneOrMore) {
                    PostOrderWalk(((ROneOrMore) node).RegularExpression, opObj);
                } else if (node is RZeroOrMore) {
                    PostOrderWalk(((RZeroOrMore) node).RegularExpression, opObj);
                } else if (node is RZeroOrOne) {
                    PostOrderWalk(((RZeroOrOne) node).RegularExpression, opObj);
                } else if (node is RRepetitionRange) {
                    PostOrderWalk(((RRepetitionRange) node).RegularExpression, opObj);
                }
            }
            opObj.Action(node);
        }

    }

}