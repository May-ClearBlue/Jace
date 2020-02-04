using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jace.Execution;
using Jace.Operations;
using Jace.Tokenizer;
using Jace.Util;

namespace Jace
{
    public class AstBuilder
    {
        private readonly IFunctionRegistry functionRegistry;
        private readonly IConstantRegistry localConstantRegistry;
        private readonly bool caseSensitive;
        private Dictionary<char, int> operationPrecedence = new Dictionary<char, int>();
        private Stack<Operation> resultStack = new Stack<Operation>();
        private Stack<Token> operatorStack = new Stack<Token>();
        private Stack<int> parameterCount = new Stack<int>();

        public AstBuilder(IFunctionRegistry functionRegistry, bool caseSensitive, IConstantRegistry compiledConstants = null)
        {
            if (functionRegistry == null)
                throw new ArgumentNullException("functionRegistry");

            this.functionRegistry = functionRegistry;
            operationPrecedence.Add('[', 0);
            operationPrecedence.Add('(', 0);
            operationPrecedence.Add('&', 1);
            operationPrecedence.Add('|', 1);
            operationPrecedence.Add('<', 2);
            operationPrecedence.Add('>', 2);
            operationPrecedence.Add('≤', 2);
            operationPrecedence.Add('≥', 2);
            operationPrecedence.Add('≠', 2);
            operationPrecedence.Add('=', 2);
            operationPrecedence.Add('+', 3);
            operationPrecedence.Add('-', 3);
            operationPrecedence.Add('*', 4);
            operationPrecedence.Add('/', 4);
            operationPrecedence.Add('%', 4);
            operationPrecedence.Add('_', 6);
            operationPrecedence.Add('^', 5);
            operationPrecedence.Add('＝', 0);
        }

        public Operation Build(IList<Token> tokens)
        {
            resultStack.Clear();
            operatorStack.Clear();

            parameterCount.Clear();

            foreach (Token token in tokens)
            {
                object value = token.Value;

                switch (token.TokenType)
                {
#if _USE_VARIABLE_
                    case TokenType.Integer:
                        resultStack.Push(new VariableCalcurator((int)token.Value));
                        break;
                    case TokenType.FloatingPoint:
                        resultStack.Push(new VariableCalcurator(DataType.FloatingPoint, token.Value.ToString()));
                        break;
                    case TokenType.Hex:
                        resultStack.Push(new VariableCalcurator((uint)token.Value));
                        break;
                    case TokenType.Literal:
                        resultStack.Push(new VariableCalcurator(DataType.Literal, (string)token.Value));
                        break;
                    case TokenType.Identifier:
                        if ((string)token.Value == "true")
                            resultStack.Push(new VariableCalcurator(true));
                        else if ((string)token.Value == "false")
                            resultStack.Push(new VariableCalcurator(false));
                        else
                            resultStack.Push(new VariableCalcurator(DataType.Identifier, (string)token.Value));
                        break;

#else
                    case TokenType.Integer:
                        resultStack.Push(new IntegerConstant((int)token.Value));
                        break;
                    case TokenType.FloatingPoint:
                        resultStack.Push(new FloatingPointConstant((double)token.Value));
                        break;
                    case TokenType.Text:
                        if (functionRegistry.IsFunctionName((string)token.Value))
                        {
                            operatorStack.Push(token);
                            parameterCount.Push(1);
                        }
                        else
                        {
                            string tokenValue = (string)token.Value;
                            if (localConstantRegistry.IsConstantName(tokenValue))
                            {
                                resultStack.Push(new FloatingPointConstant(localConstantRegistry.GetConstantInfo(tokenValue).Value));
                            }
                            else
                            {
                                if (!caseSensitive)
                                {
                                    tokenValue = tokenValue.ToLowerFast();
                                }
                                resultStack.Push(new Variable(tokenValue));
                            }
                        }
                        break;
#endif
                    case TokenType.OpenParentheses:
                        operatorStack.Push(token);
                        break;
                    case TokenType.CloseParentheses:
                        PopOperations(TokenType.OpenParentheses, token);
                        break;
                    case TokenType.OpenBracket:
                        operatorStack.Push(token);
                        break;
                    case TokenType.CloseBracket:
                        PopOperations(TokenType.OpenBracket, token);
                        break;
                    case TokenType.ArgumentSeparator:
                        PopOperations(TokenType.Null, token);
                        parameterCount.Push(parameterCount.Pop() + 1);
                        break;
                    case TokenType.Operation:
                        Token operation1Token = token;
                        char operation1 = (char)operation1Token.Value;

                        while (operatorStack.Count > 0 && (operatorStack.Peek().TokenType == TokenType.Operation ||
                            operatorStack.Peek().TokenType == TokenType.Identifier))
                        {
                            Token operation2Token = operatorStack.Peek();
                            bool isFunctionOnTopOfStack = operation2Token.TokenType == TokenType.Identifier;

                            if (!isFunctionOnTopOfStack)
                            {
                                char operation2 = (char)operation2Token.Value;

                                if ((IsLeftAssociativeOperation(operation1) &&
                                     operationPrecedence[operation1] <= operationPrecedence[operation2]) ||
                                    (operationPrecedence[operation1] <  operationPrecedence[operation2]))
                                {
                                    operatorStack.Pop();
                                    resultStack.Push(ConvertOperation(operation2Token));
                                }
                                else
                                {
                                    break;
                                }
                            }
                            else
                            {
                                operatorStack.Pop();
                                resultStack.Push(ConvertFunction(operation2Token));
                            }
                        }

                        operatorStack.Push(operation1Token);
                        break;
                }
            }

            PopOperations(TokenType.Null , null);

            VerifyResultStack();

            return resultStack.First();
        }

        private void PopOperations(TokenType pairLeft,  Token? currentToken)
        {
            if (pairLeft != TokenType.Null && !currentToken.HasValue)
                throw new ArgumentNullException("currentToken", "If the parameter \"untillLeftBracket\" is set to true, " +
                    "the parameter \"currentToken\" cannot be null.");

            while (operatorStack.Count > 0 && operatorStack.Peek().TokenType != TokenType.OpenParentheses && operatorStack.Peek().TokenType != TokenType.OpenBracket)
            {
                Token token = operatorStack.Pop();

                switch (token.TokenType)
                {
                    case TokenType.Operation:
                        resultStack.Push(ConvertOperation(token));
                        break;
                    case TokenType.Identifier:
                        resultStack.Push(ConvertFunction(token));
                        break;
                }
            }

            if (pairLeft != TokenType.Null)
            {
                Token token;

                if (operatorStack.Count > 0 && operatorStack.Peek().TokenType == pairLeft)
                    token = operatorStack.Pop();
                else
                    throw new ParseException(string.Format("No matching left bracket found for the right " +
                        "bracket at position {0}.", currentToken.Value.StartPosition));

                if (pairLeft == TokenType.OpenBracket)
                    resultStack.Push(ConvertOperation(token));

            }
            else
            {
                if (operatorStack.Count > 0 && (operatorStack.Peek().TokenType == TokenType.OpenParentheses || operatorStack.Peek().TokenType == TokenType.OpenBracket)
                    && !(currentToken.HasValue && currentToken.Value.TokenType == TokenType.ArgumentSeparator))
                    throw new ParseException(string.Format("No matching right bracket found for the left " +
                        "bracket at position {0}.", operatorStack.Peek().StartPosition));
            }
        }

        private Operation ConvertOperation(Token operationToken)
        {
            try
            {
                DataType dataType;
                Operation argument1;
                Operation argument2;
                Operation divisor;
                Operation divident;

                switch ((char)operationToken.Value)
                {
                    case '+':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new Addition(dataType, argument1, argument2);
                    case '-':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new Subtraction(dataType, argument1, argument2);
                    case '*':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new Multiplication(dataType, argument1, argument2);
                    case '/':
                        divisor = resultStack.Pop();
                        divident = resultStack.Pop();

                        return new Division(DataType.FloatingPoint, divident, divisor);
                    case '%':
                        divisor = resultStack.Pop();
                        divident = resultStack.Pop();

                        return new Modulo(DataType.FloatingPoint, divident, divisor);
                    case '_':
                        argument1 = resultStack.Pop();

                        return new UnaryMinus(argument1.DataType, argument1);
                    case '^':
                        Operation exponent = resultStack.Pop();
                        Operation @base = resultStack.Pop();

                        return new Exponentiation(DataType.FloatingPoint, @base, exponent);
                    case '&':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new And(dataType, argument1, argument2);
                    case '|':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new Or(dataType, argument1, argument2);
                    case '<':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new LessThan(dataType, argument1, argument2);
                    case '≤':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new LessOrEqualThan(dataType, argument1, argument2);
                    case '>':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new GreaterThan(dataType, argument1, argument2);
                    case '≥':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new GreaterOrEqualThan(dataType, argument1, argument2);
                    case '=':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new Equal(dataType, argument1, argument2);
                    case '≠':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new NotEqual(dataType, argument1, argument2);
#if _USE_VARIABLE_
                    case '＝':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new Substitution(dataType, argument1, argument2);
                    case '[':
                        argument2 = resultStack.Pop();
                        argument1 = resultStack.Pop();
                        dataType = RequiredDataType(argument1, argument2);

                        return new Index(dataType, argument1, argument2);
#endif
                    default:
                        throw new ArgumentException(string.Format("Unknown operation \"{0}\".", operationToken), "operation");
                }
            }
            catch (InvalidOperationException)
            {
                // If we encounter a Stack empty issue this means there is a syntax issue in 
                // the mathematical formula
                throw new ParseException(string.Format("There is a syntax issue for the operation \"{0}\" at position {1}. " +
                    "The number of arguments does not match with what is expected.", operationToken.Value, operationToken.StartPosition));
            }
        }

        private Operation ConvertFunction(Token functionToken)
        {
            try
            {
                string functionName = ((string)functionToken.Value).ToLowerInvariant();

                if (functionRegistry.IsFunctionName(functionName))
                {
                    FunctionInfo functionInfo = functionRegistry.GetFunctionInfo(functionName);

                    int numberOfParameters;

                    if (functionInfo.IsDynamicFunc) {
                        numberOfParameters = parameterCount.Pop();
                    }
                    else {
                        parameterCount.Pop();
                        numberOfParameters = functionInfo.NumberOfParameters;
                    }
                    
                    List<Operation> operations = new List<Operation>();
                    for (int i = 0; i < numberOfParameters; i++)
                        operations.Add(resultStack.Pop());
                    operations.Reverse();

                    return new Function(DataType.FloatingPoint, functionName, operations, functionInfo.IsIdempotent);
                }
                else
                {
                    throw new ArgumentException(string.Format("Unknown function \"{0}\".", functionToken.Value), "function");
                }
            }
            catch (InvalidOperationException)
            {
                // If we encounter a Stack empty issue this means there is a syntax issue in 
                // the mathematical formula
                throw new ParseException(string.Format("There is a syntax issue for the function \"{0}\" at position {1}. " +
                    "The number of arguments does not match with what is expected.", functionToken.Value, functionToken.StartPosition));
            }
        }

        private void VerifyResultStack()
        {
            if(resultStack.Count > 1)
            {
                Operation[] operations = resultStack.ToArray();

                for (int i = 1; i < operations.Length; i++)
                {
                    Operation operation = operations[i];

                    if (operation.GetType() == typeof(IntegerConstant))
                    {
                        IntegerConstant constant = (IntegerConstant)operation;
                        throw new ParseException(string.Format("Unexpected integer constant \"{0}\" found.", constant.Value));
                    }
                    else if (operation.GetType() == typeof(FloatingPointConstant))
                    {
                        FloatingPointConstant constant = (FloatingPointConstant)operation;
                        throw new ParseException(string.Format("Unexpected floating point constant \"{0}\" found.", constant.Value)); 
                    }
                }

                throw new ParseException("The syntax of the provided formula is not valid.");
            }
        }

        private bool IsCalcValue(TokenType type)
        {
            return type == TokenType.FloatingPoint || type == TokenType.Hex || type == TokenType.Identifier || type == TokenType.Integer || type == TokenType.Literal;
        }

        private bool IsLeftAssociativeOperation(char character)
        {
            return character == '*' || character == '+' || character == '-' || character == '/';
        }

        private DataType RequiredDataType(Operation argument1, Operation argument2)
        {
            return (argument1.DataType == DataType.FloatingPoint || argument2.DataType == DataType.FloatingPoint) ? DataType.FloatingPoint : DataType.Integer;
        }
    }
}
