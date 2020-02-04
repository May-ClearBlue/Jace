using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Jace.Tokenizer
{
    public enum TokenType
    {
        Null,
        Integer,
        FloatingPoint,
        Hex,
        Identifier,
        Literal,
        Operation,
        OpenParentheses,
        CloseParentheses,
        OpenBracket,
        CloseBracket,
        ArgumentSeparator
    }
}
