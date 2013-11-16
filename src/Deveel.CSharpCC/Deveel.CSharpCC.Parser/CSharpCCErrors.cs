using System;

namespace Deveel.CSharpCC.Parser {
    public static class CSharpCCErrors {
        private static int warningCount;
        private static int parseErrorCount;
        private static int semanticErrorCount;

        private static void PrintLocationInfo(object node) {
            if (node is ILocationInfo) {
                var locationInfo = (ILocationInfo) node;
                Console.Error.Write("Line {0}, Column {1}: ", locationInfo.Line, locationInfo.Column);
            } else if (node is Token) {
                var t = (Token) node;
                Console.Error.Write("Line {0}, Column {1}: ", t.beginLine, t.beginColumn);
            }
        }


        public static void ParseError(Object node, String mess) {
            Console.Error.Write("Error: ");
            PrintLocationInfo(node);
            Console.Error.WriteLine(mess);
            parseErrorCount++;
        }

        public static void ParseError(String mess) {
            Console.Error.Write("Error: ");
            Console.Error.WriteLine(mess);
            parseErrorCount++;
        }

        public static int ParseErrorCount {
            get { return parseErrorCount; }
        }

        public static void SemanticError(Object node, String mess) {
            Console.Error.Write("Error: ");
            PrintLocationInfo(node);
            Console.Error.WriteLine(mess);
            semanticErrorCount++;
        }

        public static void SemanticError(String mess) {
            Console.Error.Write("Error: ");
            Console.Error.WriteLine(mess);
            semanticErrorCount++;
        }

        public static int SemanticErrorCount {
            get { return semanticErrorCount; }
        }

        public static void Warning(Object node, String mess) {
            Console.Error.Write("Warning: ");
            PrintLocationInfo(node);
            Console.Error.WriteLine(mess);
            warningCount++;
        }

        public static void Warning(String mess) {
            Console.Error.Write("Warning: ");
            Console.Error.WriteLine(mess);
            warningCount++;
        }

        public static int WarningCount {
            get { return warningCount; }
        }

        public static int ErrorCount {
            get { return parseErrorCount + semanticErrorCount; }
        }

        public static void ReInit() {
            parseErrorCount = 0;
            semanticErrorCount = 0;
            warningCount = 0;
        }
    }
}