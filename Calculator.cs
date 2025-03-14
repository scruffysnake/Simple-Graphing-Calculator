using System.Linq.Expressions;

namespace Simple_Graphing_Calculator
{
    public enum Operators
    {
        PLUS, MINUS, MULTIPLY, DIVIDE, POWER, OPEN, CLOSE, ABS, ERR, NULL
    }
    public enum MathFuncs
    {
        VAR, PI, E, LN, LOG, SIN, COS, TAN, CEIL, FLOOR,
    }
    interface IToken {}
    struct MathFunc(MathFuncs type) : IToken
    {
        public MathFuncs type = type;
    }
    struct Number(double value) : IToken
    {
        public double value = value;
    }
    struct Operator(Operators operatr) : IToken
    {
        public Operators operatr = operatr;
    }
    struct FN(double id) : IToken
    {
        public int id = (int)id;
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
    internal class VarExpression(MathFuncs type) : IExpression 
    {
        public MathFuncs type = type;
    }
    internal class FuncExpression(MathFuncs type, IExpression func) : IExpression 
    {
        public MathFuncs type = type;
        public IExpression func = func;
    }
    internal class AbsExpression(IExpression expr) : IExpression
    {
        public IExpression expr = expr;
    }
    internal class FNExpression(int id, IExpression expr) : IExpression
    {
        public int id = id;
        public IExpression expr = expr;
    }
    internal class BrokenExpression : IExpression {}

    internal class Calculator
    {
        public static List<IToken> Tokenise(string input)
        {
            List<IToken> tokens = new List<IToken>();
            string correctedInput = input.ToLower();

            bool AddMulticharToken(MathFuncs mathFunc, ref int i, params char[] expectedChars)
            {
                for (int j = 0; j < expectedChars.Length; j++)
                {
                    if (correctedInput.Length <= i + j + 1) return false;
                    if (expectedChars[j] != correctedInput[i + j + 1]) return false;
                }
                i += expectedChars.Length;
                tokens.Add(new MathFunc(mathFunc));
                return true;
            }
            bool IsValidFunctionType(char c) => Enum.IsDefined(typeof(FunctionTypes), (int)c);

            for (int i = 0; i < correctedInput.Length; i++)
            {
                if (IsValidFunctionType(correctedInput[i]))
                {
                    tokens.Add(new MathFunc(MathFuncs.VAR));
                    continue;
                }
                switch (correctedInput[i])
                {
                    case '\0': case ' ': case '\n': break;

                    case '+': tokens.Add(new Operator(Operators.PLUS)); break;
                    case '-': tokens.Add(new Operator(Operators.MINUS)); break;
                    case '*': tokens.Add(new Operator(Operators.MULTIPLY)); break;
                    case '/': tokens.Add(new Operator(Operators.DIVIDE)); break;
                    case '^': tokens.Add(new Operator(Operators.POWER)); break;
                    case '(': tokens.Add(new Operator(Operators.OPEN)); break;
                    case ')': tokens.Add(new Operator(Operators.CLOSE)); break;
                    case '|': tokens.Add(new Operator(Operators.ABS)); break;

                    case 'e': tokens.Add(new MathFunc(MathFuncs.E)); break;
                    case 'π': tokens.Add(new MathFunc(MathFuncs.PI)); break;
                    // logs
                    case 'l':
                        if (!AddMulticharToken(MathFuncs.LN, ref i, 'n') && 
                            !AddMulticharToken(MathFuncs.LOG, ref i, 'o', 'g'))
                            tokens.Add(new Operator(Operators.ERR));
                        break;
                    // SIN
                    case 's':
                        if (!AddMulticharToken(MathFuncs.SIN, ref i, 'i', 'n'))
                            tokens.Add(new Operator(Operators.ERR));
                        break;
                    // COS and Ceil
                    case 'c':
                        if (!AddMulticharToken(MathFuncs.COS, ref i, 'o', 's') &&
                            !AddMulticharToken(MathFuncs.CEIL, ref i, 'e', 'i', 'l'))
                            tokens.Add(new Operator(Operators.ERR));
                        break;
                    // TAN
                    case 't':
                        if (!AddMulticharToken(MathFuncs.TAN, ref i, 'a', 'n'))
                            tokens.Add(new Operator(Operators.ERR));
                        break;
                    // Floor
                    case 'f':
                        if (AddMulticharToken(MathFuncs.FLOOR, ref i, 'l', 'o', 'o', 'r')) break;
                        try 
                        { 
                            i++;
                            tokens.Add(new FN(Number(correctedInput, ref i))); 
                        }
                        catch { tokens.Add(new Operator(Operators.ERR)); }
                        break;

                    default:
                        try { tokens.Add(new Number(Number(correctedInput, ref i))); }
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

        public IExpression parse()
        {
            static bool RequiresImplicitMultiply(IToken a, IToken b)
            {
                bool aIsPrimary = a is Number ||
                                (a is MathFunc mfA && (mfA.type == MathFuncs.VAR || mfA.type == MathFuncs.PI || mfA.type == MathFuncs.E)) ||
                                (a is Operator opA && (opA.operatr == Operators.CLOSE || opA.operatr == Operators.ABS));

                bool bIsPrimary = b is Number ||
                                (b is MathFunc) ||
                                (b is Operator opB && (opB.operatr == Operators.OPEN || opB.operatr == Operators.ABS));

                return aIsPrimary && bIsPrimary;
            }

            for (int i = 1; i < tokens.Count; i++)
            {
                if (RequiresImplicitMultiply(tokens[i - 1], tokens[i])) tokens.Insert(i, new Operator(Operators.MULTIPLY));
            }
            return parseArithmetic();
        }
        private IExpression parseArithmetic() => parseBinary(parseMultiplicative, Operators.MINUS, Operators.PLUS);
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
            if (tokens[i] is MathFunc func) 
            {
                i++;
                return func.type switch
                {
                    MathFuncs.VAR or MathFuncs.E or MathFuncs.PI => new VarExpression(func.type),
                    _ => new FuncExpression(func.type, parsePrimary()),
                };
            }
            if (tokens[i] is FN fn)
            {
                i++;
                return new FNExpression(fn.id, parsePrimary());
            }


            if (match(Operators.OPEN))
            {
                IExpression expr = parse();
                if (i >= tokens.Count)
                {
                    isBroken = true;
                    return new BrokenExpression();
                }
                if (tokens[i++] is Operator r) if (r.operatr == Operators.CLOSE) return expr;
                isBroken = true;
            }
            if (match(Operators.ABS))
            {
                IExpression expr = parse();
                if (i >= tokens.Count)
                {
                    isBroken = true;
                    return new BrokenExpression();
                }
                if (tokens[i++] is Operator r) if (r.operatr == Operators.ABS) return new AbsExpression(expr);
                isBroken = true;
            }
            isBroken = true; return new BrokenExpression();
        }
    }

    class Evaluator(int recursionDepth = 0)
    {
        public double coord;
        public bool error;
        public bool floorOrCeil;
        public List<(int id, IExpression expr)> functionCalls = [];
        public int id;
        public int recursionDepth = recursionDepth;
        const int MAX_RECURSION_DEPTH = 2;

        public void Reset()
        {
            error = false;
            floorOrCeil = false;
        }

        public double Interpret(IExpression expr)
        {
            if (expr is Value val) return val.value;
            if (expr is BinaryExpression binary) return InterpretBinary(binary);
            if (expr is UnaryExpression unary) return InterpretUnary(unary);
            if (expr is AbsExpression abs) return Math.Abs(Interpret(abs.expr));
            if (expr is VarExpression var)
            {
                switch (var.type)
                {
                    case MathFuncs.VAR: return coord;
                    case MathFuncs.E: return Math.E;
                    case MathFuncs.PI: return Math.PI;
                }
            }
            if (expr is FuncExpression func)
            {
                switch (func.type)
                {
                    case MathFuncs.LN: return Math.Log(Interpret(func.func));
                    case MathFuncs.LOG: return Math.Log10(Interpret(func.func));
                    case MathFuncs.SIN: return Math.Sin(Interpret(func.func));
                    case MathFuncs.COS: return Math.Cos(Interpret(func.func));
                    case MathFuncs.TAN: return Math.Tan(Interpret(func.func));
                    case MathFuncs.CEIL: 
                    {
                        floorOrCeil = true;
                        return Math.Ceiling(Interpret(func.func));
                    }
                    case MathFuncs.FLOOR: 
                    {
                        floorOrCeil = true;
                        return Math.Floor(Interpret(func.func));
                    }
                }
            }
            if (expr is FNExpression fn)
            {
                if (recursionDepth > MAX_RECURSION_DEPTH) return Error();

                if (fn.id == id) return Error();
                if (!Program.functions.Any(f => f.ID == fn.id)) return Error();
                
                IExpression internalExpression;
                if (!functionCalls.Any(f => f.id == fn.id))
                {
                    List<IToken> tokens = Calculator.Tokenise(Program.functions.Find(f => f.ID == fn.id).func);
                    if (tokens.Contains(new Operator(Operators.ERR))) return Error();
                    Parser parser = new(tokens);
                    functionCalls.Add((fn.id, expr: parser.parse()));
                }
                internalExpression = functionCalls.Find(f => f.id == fn.id).expr;
                double Var = Interpret(fn.expr);
                Evaluator calculator = new();
                calculator.Reset();
                calculator.coord = Var;
                double output = calculator.Interpret(internalExpression);
                if (double.IsFinite(output)) return output;
                return Error();
            }
            return Error();
        }
        double InterpretBinary(BinaryExpression binary)
        {
            double left = Interpret(binary.left);
            double right = Interpret(binary.right);

            switch (binary.operatr)
            {
                case Operators.PLUS: return left + right;
                case Operators.MINUS: return left - right;
                case Operators.MULTIPLY: return left * right;
                case Operators.DIVIDE: 
                    if (right == 0) return Error();
                    return left / right;
                case Operators.POWER: return Math.Pow(left, right);
            }
            return Error();
        }
        double InterpretUnary(UnaryExpression unary)
        {
            double val = Interpret(unary.expr);
            switch (unary.operatr)
            {
                case Operators.PLUS: return val;
                case Operators.MINUS: return -val;
            }
            return Error();
        }
        double Error()
        {
            error = true;
            return double.NaN;
        }
    }
}