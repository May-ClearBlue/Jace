using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Jace.Tokenizer
{
    /// <summary>
    /// A token reader that converts the input string in a list of tokens.
    /// </summary>
    public class TokenReader
    {
        private readonly CultureInfo cultureInfo;
        private readonly char decimalSeparator;
        private readonly char argumentSeparator;

        public TokenReader() 
            : this(CultureInfo.CurrentCulture)
        {
        }

        public TokenReader(CultureInfo cultureInfo)
        {
            this.cultureInfo = cultureInfo;
            this.decimalSeparator = cultureInfo.NumberFormat.NumberDecimalSeparator[0];
            this.argumentSeparator = cultureInfo.TextInfo.ListSeparator[0];
        }

        /// <summary>
        /// Read in the provided formula and convert it into a list of takens that can be processed by the
        /// Abstract Syntax Tree Builder.
        /// </summary>
        /// <param name="formula">The formula that must be converted into a list of tokens.</param>
        /// <returns>The list of tokens for the provided formula.</returns>
        public List<Token> Read(string formula)
        {
            if (string.IsNullOrEmpty(formula))
                throw new ArgumentNullException("formula");

            List<Token> tokens = new List<Token>();

            char[] characters = formula.ToCharArray();

            bool isFormulaSubPart = true;
            bool isScientific = false;

            int i = 0;
            while(i < characters.Length)
            {
                //literal
                if (characters[i] == '\"')
                {
                    int startPosition = i;
                    StringBuilder buffer = new StringBuilder();

                    while (++i < characters.Length && characters[i] != '\"')
                        buffer.Append(characters[i]);

                    tokens.Add(new Token() { TokenType = TokenType.Literal, Value = buffer.ToString(), StartPosition = startPosition, Length = buffer.Length });

                    continue;
                }
                else if (characters[i] == '\'')
                {
                    int startPosition = i;
                    StringBuilder buffer = new StringBuilder();

                    while (++i < characters.Length && characters[i] != '\'')
                        buffer.Append(characters[i]);

                    tokens.Add(new Token() { TokenType = TokenType.Literal, Value = buffer.ToString(), StartPosition = startPosition, Length = buffer.Length });

                    continue;
                }
                else if (characters[i]  == '0' && characters.Length > 3 && (characters[i+1] == 'x' || characters[i+1] == 'X'))
                {
                    int startPosition = i + 2;

                    i++;

                    uint uintValue;
                    StringBuilder buffer = new StringBuilder();

                    while (++i < characters.Length && IsPartOfHex(characters[i]))
                        buffer.Append(characters[i]);

                    if (uint.TryParse(buffer.ToString() , System.Globalization.NumberStyles.AllowHexSpecifier, null, out uintValue))
                        tokens.Add(new Token() { TokenType = TokenType.Hex, Value = uintValue, StartPosition = startPosition, Length = buffer.Length });
                    else
                        throw new Exception("elegal hex format");

                    continue;
                }

                else if (IsPartOfNumeric(characters[i], true, isFormulaSubPart))
                {
                    StringBuilder buffer = new StringBuilder();
                    buffer.Append(characters[i]);
                    int startPosition = i;

                    while (++i < characters.Length && IsPartOfNumeric(characters[i], false, isFormulaSubPart))
                    {
                        if (isScientific && IsScientificNotation(characters[i]))
                            throw new ParseException(string.Format("Invalid token \"{0}\" detected at position {1}.", characters[i], i));

                        if (IsScientificNotation(characters[i]))
                        {
                            isScientific = IsScientificNotation(characters[i]);

                            if (characters[i + 1] == '-')
                            {
                                buffer.Append(characters[i++]);
                            }
                        }
                        buffer.Append(characters[i]);
                    }

                    // Verify if we do not have an int
                    int intValue;
                    if (int.TryParse(buffer.ToString(), out intValue))
                    {
                        tokens.Add(new Token() { TokenType = TokenType.Integer, Value = intValue, StartPosition = startPosition, Length = i - startPosition });
                        isFormulaSubPart = false;
                        continue;
                    }
                    else
                    {
                        double doubleValue;
                        if (double.TryParse(buffer.ToString(), NumberStyles.Float | NumberStyles.AllowThousands,                            cultureInfo, out doubleValue))
                        {
                            tokens.Add(new Token() { TokenType = TokenType.FloatingPoint, Value = doubleValue, StartPosition = startPosition, Length = i - startPosition });
                            isScientific = false;
                            isFormulaSubPart = false;
                        }
                        else if (buffer.ToString() == "-")
                        {
                            // Verify if we have a unary minus, we use the token '_' for a unary minus in the AST builder
                            tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '_', StartPosition = startPosition, Length = 1 });
                            continue;
                        }
                        // Else we skip
                    }

                    if (i == characters.Length)
                    {
                        // Last character read
                        continue;
                    }
                }

                if (IsPartOfVariable(characters[i], true))
                {
                    string buffer = "" + characters[i];
                    int startPosition = i;

                    while (++i < characters.Length && IsPartOfVariable(characters[i], false))
                    {
                        buffer += characters[i];
                    }

                    tokens.Add(new Token() { TokenType = TokenType.Identifier, Value = buffer, StartPosition = startPosition, Length = i -startPosition });
                    isFormulaSubPart = false;

                    if (i == characters.Length)
                    {
                        // Last character read
                        continue;
                    }
                }
                if (characters[i] == this.argumentSeparator)
                {
                    tokens.Add(new Token() { TokenType = Tokenizer.TokenType.ArgumentSeparator, Value = characters[i], StartPosition = i, Length = 1 });
                    isFormulaSubPart = false;
                    i++;
                }
                else
                {
                    switch (characters[i])
                    { 
                        case ' ':
                            continue;
                        case '+':
                        case '-':
                        case '*':
                        case '/':
                        case '^':
                        case '%':
                        case '≤':
                        case '≥':
                        case '≠':
                            if (IsUnaryMinus(characters[i], tokens))
                            {
                                // We use the token '_' for a unary minus in the AST builder
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '_', StartPosition = i, Length = 1 });
                            }
                            else
                            {
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = characters[i], StartPosition = i, Length = 1 });                            
                            }
                            isFormulaSubPart = true;
                            break;
                        case '(':
                            tokens.Add(new Token() { TokenType = TokenType.OpenParentheses, Value = characters[i], StartPosition = i, Length = 1 });
                            isFormulaSubPart = true;
                            break;
                        case ')':
                            tokens.Add(new Token() { TokenType = TokenType.CloseParentheses, Value = characters[i], StartPosition = i, Length = 1 });
                            isFormulaSubPart = false;
                            break;
                        case '[':
                            tokens.Add(new Token() { TokenType = TokenType.OpenBracket, Value = characters[i], StartPosition = i, Length = 1 });
                            isFormulaSubPart = true;
                            break;
                        case ']':
                            tokens.Add(new Token() { TokenType = TokenType.CloseBracket, Value = characters[i], StartPosition = i, Length = 1 });
                            isFormulaSubPart = false;
                            break;
                        case '<':
                            if (i + 1 < characters.Length && characters[i + 1] == '=')
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '≤', StartPosition = i++, Length = 2 });
                            else
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '<', StartPosition = i, Length = 1 });
                            isFormulaSubPart = false;
                            break;
                        case '>':
                            if (i + 1 < characters.Length && characters[i + 1] == '=')
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '≥', StartPosition = i++, Length = 2 });
                            else
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '>', StartPosition = i, Length = 1 });
                            isFormulaSubPart = false;
                            break;
                        case '!':
                            if (i + 1 < characters.Length && characters[i + 1] == '=')
                            {
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '≠', StartPosition = i++, Length = 2 });
                                isFormulaSubPart = false;
                            }
                            else
                                throw new ParseException(string.Format("Invalid token \"{0}\" detected at position {1}.", characters[i], i));
                            break;
                        case '&':
                            if (i + 1 < characters.Length && characters[i + 1] == '&')
                            {
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '&', StartPosition = i++, Length = 2 });
                                isFormulaSubPart = false;
                            }
                            else
                                throw new ParseException(string.Format("Invalid token \"{0}\" detected at position {1}.", characters[i], i));
                            break;
                        case '|':
                            if (i + 1 < characters.Length && characters[i + 1] == '|')
                            {
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '|', StartPosition = i++, Length = 2 });
                                isFormulaSubPart = false;
                            }
                            else
                                throw new ParseException(string.Format("Invalid token \"{0}\" detected at position {1}.", characters[i], i));
                            break;
                        case '=':
                            if (i + 1 < characters.Length && characters[i + 1] == '=')
                            {
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '=', StartPosition = i++, Length = 2 });
                                isFormulaSubPart = false;
                            }
#if _USE_VARIABLE_
                            else
                                tokens.Add(new Token() { TokenType = TokenType.Operation, Value = '＝', StartPosition = i, Length = 1 });
#else
                            else
                                throw new ParseException(string.Format("Invalid token \"{0}\" detected at position {1}.", characters[i], i));
#endif
                            break;
                        default:
                            throw new ParseException(string.Format("Invalid token \"{0}\" detected at position {1}.", characters[i], i));
                    }

                    i++;
                }
            }

            return tokens;
        }

        private bool IsPartOfHex(char character)
        {
            return (character >= 'a' && character <= 'f') || (character >= 'A' && character <= 'F') || ( character >= '0' && character <= '9');
        }

        private bool IsPartOfNumeric(char character, bool isFirstCharacter, bool isFormulaSubPart)
        {
            return character == decimalSeparator || (character >= '0' && character <= '9') || (isFormulaSubPart && isFirstCharacter && character == '-') || (!isFirstCharacter && character == 'e') || (!isFirstCharacter && character == 'E');
        }

        private bool IsPartOfVariable(char character, bool isFirstCharacter)
        {
            return (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z') || (!isFirstCharacter && character >= '0' && character <= '9') || (!isFirstCharacter && character == '_');
        }

        private bool IsUnaryMinus(char currentToken, List<Token> tokens)
        {
            if (currentToken == '-')
            {
                Token previousToken = tokens[tokens.Count - 1];

                return !(previousToken.TokenType == TokenType.FloatingPoint ||
                         previousToken.TokenType == TokenType.Integer ||
                         previousToken.TokenType == TokenType.Identifier ||
                         previousToken.TokenType == TokenType.CloseBracket ||
                         previousToken.TokenType == TokenType.CloseParentheses
                         );
            }
            else
                return false;
        }

        private bool IsScientificNotation(char currentToken)
        {
            return currentToken == 'e' || currentToken == 'E';
        }
    }
}
