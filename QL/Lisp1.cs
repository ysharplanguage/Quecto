using System;
using System.Lisp;
using System.Text.RegularExpressions;
namespace Quecto
{
    public class Lisp1 : Evaluator
    {
        private Symbols knownSymbols; private Symbol pound, at, query, colon, negation, disjunction, conjunction, equality, inequality, less, lessOrEqual, plus, minus, times, divideBy, percent, dollar;
        protected static readonly Regex Spacing = new("(\\s+)|(/\\*[^*]*\\*+(?:[^/*][^*]*\\*+)*/)|(//.*)", RegexOptions.Compiled);
        protected static readonly Regex Integers = new("\\-?(0|[1-9][0-9]*)[_A-Za-z]?", RegexOptions.Compiled);
        protected static readonly Regex Literal = new("\"(\\\\\"|[^\"])*\"", RegexOptions.Compiled);
        protected static readonly Regex Interpolation = new("\\{([^\\}]|\\\\\\})+\\}", RegexOptions.Compiled);
        protected override object Reflection(Symbols environment, object expression) { var args = (object[])expression; return args.Length > 1 ? (Symbol.Reflection.Equals(args[0]) ? Parse(environment, (string)Evaluation(environment, args[1])) : Evaluate(environment, Evaluation(environment, args[0]))) : base.Reflection(environment, expression); }
        protected object Indexed(Symbols environment, object expression) { var args = (object[])expression; object value; return Pound.Equals(args[0]) ? Actuals(environment, args, 1) : (value = Evaluation(environment, args[0])) is object[] array ? array.Length : value is string s ? s.Length : -1; }
        protected object Element(Symbols environment, object expression) { var list = (object[])expression; object left; return (left = Evaluation(environment, list[0])) is  object[] array ? array[(int)Evaluation(environment, list[2])] : null; }
        protected object IfThenElse(Symbols environment, object expression) { var list = (object[])expression; return (bool)Evaluation(environment, list[0]) ? Evaluation(environment, list[2]) : Evaluation(environment, list[4]); }
        protected object IsNot(Symbols environment, object expression) => !(bool)Evaluation(environment, ((object[])expression)[1]);
        protected object OrElse(Symbols environment, object expression) => (bool)Evaluation(environment, ((object[])expression)[0]) || (bool)Evaluation(environment, ((object[])expression)[2]);
        protected object AndThen(Symbols environment, object expression) => (bool)Evaluation(environment, ((object[])expression)[0]) && (bool)Evaluation(environment, ((object[])expression)[2]);
        protected object Equal(Symbols environment, object expression) { var list = (object[])expression; return Equals(Evaluation(environment, list[0]), Evaluation(environment, list[2])); }
        protected object Unequal(Symbols environment, object expression) => !(bool)Equal(environment, expression);
        protected object LessThan(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) < (int)Evaluation(environment, list[2]); }
        protected object LessThanOrEqual(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) <= (int)Evaluation(environment, list[2]); }
        protected object Add(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) + (int)Evaluation(environment, list[2]); }
        protected object Subtract(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) - (int)Evaluation(environment, list[2]); }
        protected object Multiply(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) * (int)Evaluation(environment, list[2]); }
        protected object Divide(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) / (int)Evaluation(environment, list[2]); }
        protected object Modulo(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) % (int)Evaluation(environment, list[2]); }
        protected object ToInterpolatedString(Symbols environment, object expression)
        {
            string Replacer(Match match) { object exp; Type typ; var exi = match.Value.LastIndexOf(')'); var fmi = match.Value.LastIndexOf(':'); var fmt = exi < fmi ? match.Value[(fmi + 1)..] : null; var src = fmt == null ? match.Value[1..^1] : match.Value[1..fmi]; exp = Evaluate(environment, src); fmt = fmt?[..^1]?.Trim(); typ = !string.IsNullOrEmpty(fmt) ? exp?.GetType() : null; var m = typ?.GetMethod("ToString", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, [ typeof(string) ]); return (m != null ? (string)m.Invoke(exp, [ fmt ]) : fmt != null ? Print(exp) : exp?.ToString()) ?? string.Empty; }
            return Dollar.Equals(((object[])expression)[0]) ? Interpolation.Replace(Evaluation(environment, ((object[])expression)[1])?.ToString() ?? string.Empty, Replacer) : Print(Evaluation(environment, ((object[])expression)[0]));
        }
        protected override object Token(Symbols environment, string input, out int matched, ref int at, out int seen, bool skipWhitespace) // Tokenizer
        {
            Match match; matched = seen = 0;
            while ((match = Spacing.Match(input, at)).Success && match.Index == at) at += match.Length;
            if (at < input.Length) { var position = at; Symbol built_in; char head; string name;
                if ((char.IsDigit(input[position]) || input[position] == '-') && (match = Integers.Match(input, position)).Success && match.Index == position) {
                    seen = matched = match.Value.Length; return char.IsDigit(match.Value[^1]) && int.TryParse(match.Value, out var @int) ? @int : Symbol.Undefined;
                } else if ((match = Literal.Match(input, position)).Success && match.Index == position) {
                    matched = match.Value.Length; return match.Value.Substring(1, match.Value.Length - 2).Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\r", "\r"); // (String)
                } else if ((built_in = environment.Built_in(input, position)).Id < 0 && (char.IsLetter((name = environment.NameOf(built_in))[name.Length - 1]) ? ((position + name.Length < input.Length && !char.IsLetterOrDigit(input[position + name.Length])) || position + name.Length == input.Length) : true) && !Symbol.Undefined.Equals(built_in)) {
                    matched = name.Length; return built_in; // (Built in symbol)
                } else if (char.IsLetter(head = input[position]) || head == '_') {
                    var start = position; while (++position < input.Length && (char.IsLetterOrDigit(input[position]) || input[position] == '_')) ; matched = position - start;
                    var @bool = false; return (input = input.Substring(start, matched)) != "null" ? (input == "false" || (@bool = input == "true") ? @bool : /* (Programmer-defined identifier) */environment.Symbol(input)) : null;
                } else return Symbol.Undefined; // (Undefined/unknown symbol)
            } else return Symbol.EOF; // (End of input)
        }
        protected override Symbols AsGlobal(Symbols environment) => base.AsGlobal(environment).Get(Dollar) is Evaluation ? environment : environment
            .Set(Pound, (Evaluation)Indexed).Set(At, (Evaluation)Element).Set(Query, (Evaluation)IfThenElse)
            .Set(Negation, (Evaluation)IsNot).Set(Disjunction, (Evaluation)OrElse).Set(Conjunction, (Evaluation)AndThen)
            .Set(Equality, (Evaluation)Equal).Set(Inequality, (Evaluation)Unequal).Set(Less, (Evaluation)LessThan).Set(LessOrEqual, (Evaluation)LessThanOrEqual)
            .Set(Plus, (Evaluation)Add).Set(Minus, (Evaluation)Subtract).Set(Times, (Evaluation)Multiply).Set(DivideBy, (Evaluation)Divide).Set(Percent, (Evaluation)Modulo)
            .Set(Dollar, (Evaluation)ToInterpolatedString);
        public override Symbols GetSymbols() => knownSymbols != null && !Symbol.Undefined.Equals(Dollar) ? knownSymbols : (knownSymbols ??= KnownSymbols())
            .Built_in("#", out pound).Built_in("@", out at).Built_in("?", out query).Built_in(":", out colon)
            .Built_in("!", out negation).Built_in("||", out disjunction).Built_in("&&", out conjunction)
            .Built_in("==", out equality).Built_in("!=", out inequality).Built_in("<", out less).Built_in("<=", out lessOrEqual)
            .Built_in("+", out plus).Built_in("-", out minus).Built_in("*", out times).Built_in("/", out divideBy).Built_in("%", out percent)
            .Built_in("$", out dollar);
        public override string Print(object expression) => expression is Symbol ? GetSymbols().NameOf((Symbol)expression) : expression is string ? $"\"{((string)expression).Replace("\r", "\\r").Replace("\n", "\\n").Replace("\"", "\\\"")}\"" : expression is int ? $"{expression}" : expression is bool b ? (b ? "true" : "false") : expression == null ? "null" : base.Print(expression);
        public Symbol Pound => pound; public Symbol At => at; public Symbol Query => query; public Symbol Colon => colon;
        public Symbol Negation => negation; public Symbol Disjunction => disjunction; public Symbol Conjunction => conjunction;
        public Symbol Equality => equality; public Symbol Inequality => inequality; public Symbol Less => less; public Symbol LessOrEqual => lessOrEqual;
        public Symbol Plus => plus; public Symbol Minus => minus; public Symbol Times => times; public Symbol DivideBy => divideBy; public Symbol Percent => percent;
        public Symbol Dollar => dollar;
    }
}