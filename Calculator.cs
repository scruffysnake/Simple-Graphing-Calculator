namespace Simple_Graphing_Calculator
{
    public enum Operators
    {
        PLUS, MINUS, MULTIPLY, DIVIDE, POWER, OPEN, CLOSE, ERR, NULL
    }
    public enum MathFuncs
    {
        X, PI, E, LN, SIN, COS, TAN, ABS, CEIL, FLOOR
    }
    interface IToken {}
    struct MathFunc(MathFuncs func) : IToken
    {
        public MathFuncs func = func;
    }
    struct Number(double value) : IToken
    {
        public double value = value;
    }
    struct Operator(Operators operatr) : IToken
    {
        public Operators operatr = operatr;
    }

    interface IExpression {}
    internal class Value(double value) : IExpression
    {
        public double value = value;
    }
    internal class BinaryExpression(Operators operatr, IExpression left, IExpression right) : IExpression
    {
        public Operators operatr = operatr; 
        public IExpression left = left, right = right;
    }
    internal class UnaryExpression(Operators operatr, IExpression expr) : IExpression
    {
        public Operators operatr = operatr; 
        public IExpression expr = expr;
    }
    internal class FuncExpression(MathFuncs func) : IExpression 
    {
        public MathFuncs func = func;
    }
    internal class BrokenExpression : IExpression {}

    internal class Calculator
    {
        public static List<IToken> Tokenise(string input)
        {
            List<IToken> tokens = new List<IToken>(); 

            for (int i = 0; i < input.Length; i++)
            {
                switch (input[i])
                {
                    case '\0': case ' ': case '\n': break;

                    case '+': tokens.Add(new Operator(Operators.PLUS)); break;
                    case '-': tokens.Add(new Operator(Operators.MINUS)); break;
                    case '*': tokens.Add(new Operator(Operators.MULTIPLY)); break;
                    case '/': tokens.Add(new Operator(Operators.DIVIDE)); break;
                    case '^': tokens.Add(new Operator(Operators.POWER)); break;
                    case '(': tokens.Add(new Operator(Operators.OPEN)); break;
                    case ')': tokens.Add(new Operator(Operators.CLOSE)); break;

                    case 'X': case 'x': tokens.Add(new MathFunc(MathFuncs.X)); break;

                    default:
                        try { tokens.Add(new Number(Number(input, ref i))); }
                        catch { tokens.Add(new Operator(Operators.ERR)); }
                        break;
                }
            }
            return tokens;
        }

        static double Number(string input, ref int i)
        {
            Func<char, bool> isNumber = c => char.IsDigit(c);
            Func<char, bool> isDot = c => c == '.';
            Func<char, bool> isNumberOrDot = c => isNumber(c) || isDot(c);

            if (!isNumberOrDot(input[i])) throw new Exception("Unexpected char");

            string sNumber = "0";
            bool hasDot = false;
            while (i < input.Length && isNumberOrDot(input[i]))
            {
                if (isDot(input[i])) 
                {
                    if (hasDot) throw new Exception("Can't have multiple decimal places in a number");
                    hasDot = true;
                }
                sNumber += input[i++];
            }
            i--;

            return double.Parse(sNumber);
        }
    }

    class Parser
    {
        List<IToken> tokens;
        int i;
        public bool isBroken = false;

        public Parser(List<IToken> tokens) 
        {
            this.tokens = tokens;
            i = 0;
            if (tokens.Contains(new Operator(Operators.ERR))) isBroken = true;
        }

        bool match(params Operators[] operators) 
        {
            if (i >= tokens.Count) 
            {
                isBroken = true;
                return false;
            }

            if (tokens[i] is Operator r) 
            {
                if (operators.Contains(r.operatr)) 
                {
                    i++;
                    return true;
                }
            }
            return false;
        }

        private IExpression parseBinary(Func<IExpression> next, params Operators[] operatorTokens)
        {
            IExpression expr = next();

            while (match(operatorTokens))
            {
                Operators operatorToken = Operators.NULL;
                if (tokens[i - 1] is Operator r) 
                {
                    operatorToken = r.operatr;
                }
                IExpression right = next();
                expr = new BinaryExpression(operatorToken, expr, right);
            }
            return expr;
        }

        public IExpression parseArithmetic() => parseBinary(parseMultiplicative, Operators.MINUS, Operators.PLUS);
        private IExpression parseMultiplicative() => parseBinary(parseIndices, Operators.DIVIDE, Operators.MULTIPLY);
        private IExpression parseIndices() => parseBinary(parseUnary, Operators.POWER);

        private IExpression parseUnary()
        {
            if (match(Operators.PLUS, Operators.MINUS))
            {
                if (tokens[i - 1] is Operator r) 
                {
                    Operators operatorToken = r.operatr;
                    IExpression expr = parseUnary();
                    return new UnaryExpression(operatorToken, expr);
                }
            }
            return parsePrimary();
        }

        private IExpression parsePrimary()
        {
            if (i >= tokens.Count) 
            {
                isBroken = true;
                return new BrokenExpression();
            }

            if (tokens[i] is Number n) 
            {
                i++;
                return new Value(n.value);
            }
            if (tokens[i] is MathFunc) 
            {
                i++;
                return new FuncExpression(MathFuncs.X);
            }   

            if (match(Operators.OPEN))
            {
                IExpression expr = parseArithmetic();
                if (i >= tokens.Count)
                {
                    isBroken = true;
                    return new BrokenExpression();
                }
                if (tokens[i++] is Operator r) if (r.operatr == Operators.CLOSE) return expr;
                isBroken = true;
            }
            isBroken = true; return new BrokenExpression();
        }
    }

    static class Evaluator
    {
        public static double x;
        public static bool error;

        public static void Reset()
        {
            error = false;
        }

        public static double Interpret(IExpression expr)
        {
            if (expr is Value val) return val.value;
            if (expr is BinaryExpression binary) return InterpretBinary(binary);
            if (expr is UnaryExpression unary) return InterpretUnary(unary);
            if (expr is FuncExpression) return x;
            error = true;
            return 0;
        }
        static double InterpretBinary(BinaryExpression binary)
        {
            double left = Interpret(binary.left);
            double right = Interpret(binary.right);

            switch (binary.operatr)
            {
                case Operators.PLUS: return left + right;
                case Operators.MINUS: return left - right;
                case Operators.MULTIPLY: return left * right;
                case Operators.DIVIDE: 
                    if (right == 0) 
                    {
                        error = true;
                        return 0;
                    }
                    return left / right;
                case Operators.POWER: return Math.Pow(left, right);
            }
            error = true;
            return 0;
        }
        static double InterpretUnary(UnaryExpression unary)
        {
            double val = Interpret(unary.expr);
            switch (unary.operatr)
            {
                case Operators.PLUS: return val;
                case Operators.MINUS: return -val;
            }
            error = true;
            return 0;
        }
    }
}