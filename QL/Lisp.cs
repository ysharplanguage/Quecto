using System.Collections.Generic;
using System.Linq;
namespace System.Lisp
{
    public readonly struct Symbol
    {
        public static readonly Symbol Undefined = default, Open = new(-1), Close = new(-2), Quote = new(-3), Let = new(-4), Set = new(-5), Lambda = new(-6), Reflection = new(-7), EOF = new(int.MaxValue); public readonly int Id;
        public static bool operator ==(Symbol left, Symbol right) => left.Equals(right); public static bool operator !=(Symbol left, Symbol right) => !(left == right);
        public Symbol(int id) => Id = id;
        public override bool Equals(object obj) => obj is Symbol symbol && symbol.Id == Id; public override int GetHashCode() => Id;
        public override string ToString() => $"[{nameof(Symbol)}({Id})]";
    }
    public class Symbols : List<string>
    {
        private class Built_inComparer : IComparer<string> { public int Compare(string left, string right) => !ReferenceEquals(left, right) || (string.CompareOrdinal(left, right) != 0) ? (left.Length < right.Length ? 1 : -1) : 0; } private readonly Dictionary<Symbol, object> map = []; private readonly Dictionary<string, Symbol> all; private readonly SortedSet<string> ordered; private object[] built_ins;
        protected internal Symbols(string[] core) { Global = this; all = []; ordered = new(new Built_inComparer()); built_ins = new object[-Lisp.Symbol.Quote.Id]; core ??= [string.Empty, "(", ")", "`", "var", "=", "=>", "."]; for (var at = 0; at < core.Length; at++) Built_in(core[at], out var _); }
        public static readonly object[] Empty = []; public readonly Symbols Global, Parent;
        public Symbols(Symbols outer, object[] locals = null, object[] values = null) { Global = (Parent = outer).Global; locals ??= Empty; values ??= Empty; for (var at = 0; at < locals.Length; at++) map.Add((Symbol)locals[at], values[at]); }
        public Symbol Symbol(string name, bool isBuilt_in = false) { if (!Global.all.TryGetValue(name, out var symbol)) { symbol = new Symbol(isBuilt_in ? -Global.all.Count : Global.all.Count); if (isBuilt_in) Global.ordered.Add(name); Global.all.Add(name, symbol); Global.Add(name); } return symbol; }
        public Symbols Built_in(string name, out Symbol built_in) { built_in = Symbol(name, true); return this; }
        public Symbol Built_in(string literal, int startAt = -1) { static bool Matches(string input, string value, int at) { var from = at; at = 0; while (from < input.Length && at < value.Length && input[from] == value[at]) { from++; at++; } return at == value.Length; } foreach (var name in Global.ordered) { if (name.Length > 0 && ((startAt >= 0 && Matches(literal, name, startAt)) || literal == name) && Global.all[name].Id < 0) { return Global.all[name]; } } return Lisp.Symbol.Undefined; }
        public object Get(string name) => Get(Global.Symbol(name));
        public object Get(Symbol symbol) => symbol.Id <= 0 ? (Math.Abs(symbol.Id) < Global.built_ins.Length ? Global.built_ins[-symbol.Id] : null) : map.TryGetValue(symbol, out var value) ? value : Parent != null && Parent.map.TryGetValue(symbol, out value) ? value : null;
        public Symbols Set(string name, object value, bool outerLookup = false) => Set(Global.Symbol(name), value, outerLookup);
        public Symbols Set(Symbol symbol, object value, bool outerLookup = false) { if (symbol.Id < 0) { var at = -symbol.Id; var to = at + 1; if (to > Global.built_ins.Length) Array.Resize(ref Global.built_ins, to); Global.built_ins[at] = value; } if (outerLookup) { Symbols current = this, found = null; while (found == null) { if (!current.map.TryGetValue(symbol, out var _) && current.Parent != null) current = current.Parent; else { found = current; break; } } found.map[symbol] = value; } else map[symbol] = value; return this; }
        public string NameOf(Symbol symbol) => Global[Math.Abs(symbol.Id)];
    }
    public sealed class Closure
    {
        private readonly Symbols environment; private readonly object[] parameters; private readonly object body;
        public Closure(Symbols environment, object[] parameters, object body) { this.environment = environment; this.parameters = parameters; this.body = body; }
        public object Invoke(params object[] arguments) => Evaluator.Evaluation(new Symbols(environment, parameters, arguments), body);
    }
    public abstract class Evaluator
    {
        internal static object Evaluation(Symbols environment, object expression)
        {
            var list = expression as object[]; int at; if (list != null && list.Length > 0) { var symbol = Symbol.Undefined; var isBuilt_in = (list[at = 0] is Symbol && (symbol = (Symbol)list[at]).Id < 0) || (list.Length > 1 && list[at = 1] is Symbol && (symbol = (Symbol)list[at]).Id < 0); var evaluation = isBuilt_in ? (Evaluation)environment.Get(symbol) : null; object first;
                return evaluation != null ? evaluation(environment, expression) : (first = Evaluation(environment, list[0])) is Closure closure ? closure.Invoke(Actuals(environment, list, 1)) : new object[1] { first }.Concat(Actuals(environment, list, 1)).ToArray();
            } else return list == null ? (expression is Symbol symbol ? environment.Get(symbol) : expression) : Symbols.Empty;
        }
        protected static IEnumerable<object> Sequence(object value, out object sequence, bool required = false) { static bool TryGetSequence(object value, out object sequence, object defaultValue = null) => (sequence = (value as System.Collections.IEnumerable)?.Cast<object>()) != null || (sequence = defaultValue) is IEnumerable<object>; return required ? ((System.Collections.IEnumerable)(sequence = value as System.Collections.IEnumerable ?? throw new ArgumentException("not a sequence", nameof(value)))).Cast<object>() : (TryGetSequence(value, out sequence) ? (IEnumerable<object>)sequence : null); }
        protected static object[] Actuals(Symbols environment, object[] arguments, int startAt = 0) { var result = arguments.Length > startAt ? new object[arguments.Length - startAt] : Symbols.Empty; for (var at = startAt; at < arguments.Length; at++) result[at - startAt] = Evaluation(environment, arguments[at]); return result; }
        protected static object Quotation(Symbols environment, object expression) => ((object[])expression)[1];
        protected static object Definition(Symbols environment, object expression) { var list = (object[])expression; var lets = (object[])list[1]; var at = -1; object result = null; while (++at < lets.Length) { environment.Set((Symbol)lets[at], Evaluation(environment, lets[at + 1])); at++; } at = 1; while (++at < list.Length) result = Evaluation(environment, list[at]); return result; }
        protected static object Assignment(Symbols environment, object expression) { var list = (object[])expression; var symbol = (Symbol)list[0]; var rhs = list[2]; object value; environment.Set(symbol, value = Evaluation(environment, rhs), true); return value; }
        protected static object Abstraction(Symbols environment, object expression) { var list = (object[])expression; return new Closure(environment, (object[])list[0], list[2]); }
        protected virtual object Reflection(Symbols environment, object expression) => environment.Get("\0");
        protected virtual Symbols KnownSymbols() => new(default);
        protected virtual object Token(Symbols symbols, string input, out int matched, ref int at, out int caught, bool skipWhitespace = true) { matched = caught = 0; return Symbol.EOF; }
        protected virtual object Parse(Symbols symbols, string input, object current, int matched, ref int at, int caught)
        {
            if (Symbol.EOF.Equals(current) || Symbol.Undefined.Equals(current)) throw new Exception($"Unexpected {(Symbol.Undefined.Equals(current) ? $"'{input.Substring(at, caught)}'" : "EOF")} at {at}");
            else if (Symbol.Quote.Equals(current)) { at += matched; current = new[] { Symbol.Quote, Parse(symbols, input, Token(symbols, input, out matched, ref at, out caught), matched, ref at, caught) }; }
            else if (Symbol.Open.Equals(current)) { var list = new List<object>(); at += matched; while (!Symbol.EOF.Equals(current = Token(symbols, input, out matched, ref at, out caught)) && !Symbol.Undefined.Equals(current) && !Symbol.Close.Equals(current)) list.Add(Parse(symbols, input, current, matched, ref at, caught));
                if (!Symbol.EOF.Equals(current) && !Symbol.Undefined.Equals(current)) at += matched; else throw new Exception($"Unexpected {(Symbol.Undefined.Equals(current) ? $"'{input.Substring(at, caught)}'" : "EOF")} at {at}"); current = list.ToArray();
            } else at += matched; return current;
        }
        protected virtual object Evaluate(Symbols environment, object expression) { static object Clone(object expression) { static object[] CloneArray(object[] array) { var copy = new object[array.Length]; for (var at = 0; at < array.Length; at++) copy[at] = Clone(array[at]); return copy; } return expression is object[] a ? CloneArray(a) : expression; } return Evaluation(environment, Normalize(environment, Clone(expression))); }
        protected virtual Symbols AsGlobal(Symbols environment) => environment.Set(Symbol.Quote, (Evaluation)Quotation).Set(Symbol.Let, (Evaluation)Definition).Set(Symbol.Set, (Evaluation)Assignment).Set(Symbol.Lambda, (Evaluation)Abstraction).Set(Symbol.Reflection, (Evaluation)Reflection);
        protected virtual object Normalize(Symbols environment, object expression) { if (!(expression is object[])) return expression; var list = (object[])expression; if (list.Length == 3 && Symbol.Lambda.Equals(list[1]) && list[0] is Symbol) return new object[] { new object[] { list[0] }, Symbol.Lambda, list[2] }; for (var at = 0; at < list.Length; at++) { var target = Normalize(environment, list[at]); if (!ReferenceEquals(target, list[at])) list[at] = target; } return expression; }
        public abstract Symbols GetSymbols();
        public object ReadNextToken(string input, out int matched, ref int at, bool peekOnly = false, bool skipWhitespace = true) { var saved = at; var token = Token(GetSymbols(), input, out matched, ref at, out _, skipWhitespace); at = peekOnly ? saved : at; return token; }
        public virtual object Parse(Symbols symbols, string input) { var at = 0; var expression = Parse(symbols, input, Token(symbols, input, out var matched, ref at, out var seen), matched, ref at, seen); return Symbol.EOF.Equals(Token(symbols, input, out _, ref at, out seen)) ? expression : throw new Exception($"Unexpected '{input.Substring(at, seen)}' at {at}"); }
        public virtual string Print(object expression) { var sequence = Sequence(expression, out _); var nonEmpty = sequence != null && sequence.Any(); return sequence != null ? $"( {string.Join(" ", sequence.Select(Print))}{(nonEmpty ? " )" : ")")}" : $"{expression}"; }
        public object Evaluate(Symbols environment, string input) => Evaluate(environment, input, out _, out _);
        public object Evaluate(Symbols environment, string input, out Symbols global, out object parsed) { parsed = Parse(global = AsGlobal(environment ?? new Symbols(GetSymbols())), input); return Evaluate(new Symbols(environment ?? global).Set("\0", input), parsed); }
    }
    public delegate object Evaluation(Symbols environment, object expression);
}