/* Copyright (c) 2012-2014, Deveel
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 *     * Redistributions of source code must retain the above copyright notice,
 *       this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Sun Microsystems, Inc. nor the names of its
 *       contributors may be used to endorse or promote products derived from
 *       this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections;
using System.Text;

namespace Deveel.CSharpCC.Parser {
    public class Expansion {
        internal static long NextGenerationIndex = 1;

        public int Line { get; internal set; }

        public int Column { get; internal set; }

        internal string InternalName { get; set; }

        public object Parent { get; internal set; }

        internal int Ordinal { get; set; }

        public long MyGeneration { get; internal set; }

        public bool IsMinimumSize { get; internal set; }

        public override int GetHashCode() {
            return Line + Column;
        }

        internal static void ReInit() {
            NextGenerationIndex = -1;
        }

        protected StringBuilder DumpPrefix(int indent) {
            var sb = new StringBuilder(128);
            for (int i = 0; i < indent; i++)
                sb.Append("  ");
            return sb;
        }

        public virtual StringBuilder Dump(int indent, IList alreadyDumped) {
            return DumpPrefix(indent)
                .Append(GetHashCode())
                .Append(" ")
                .Append(GetType().Name);
        }
    }
}