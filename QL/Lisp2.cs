using System;
using System.Lisp;
namespace Quecto
{
    public class Lisp2 : Lisp1
    {
        private Symbol greater, greaterOrEqual, caret;
        protected object GreaterThan(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) > (int)Evaluation(environment, list[2]); }
        protected object GreaterThanOrEqual(Symbols environment, object expression) { var list = (object[])expression; return (int)Evaluation(environment, list[0]) >= (int)Evaluation(environment, list[2]); }
        protected object Power(Symbols environment, object expression) { var list = (object[])expression; var left = (int)Evaluation(environment, list[0]); var e = Math.Abs((int)Evaluation(environment, list[2])); var p = 1; while (e-- > 0) p *= left; return p; }
        protected override Symbols AsGlobal(Symbols environment) =>
            !((environment = base.AsGlobal(environment)).Get(Caret) is Evaluation) ?
            environment.Set(Greater, (Evaluation)GreaterThan).Set(GreaterOrEqual, (Evaluation)GreaterThanOrEqual).Set(Caret, (Evaluation)Power)
            :
            environment;
        public override Symbols GetSymbols() => Symbol.Undefined.Equals(Caret) ? base.GetSymbols().Built_in(">", out greater).Built_in(">=", out greaterOrEqual).Built_in("^", out caret) : base.GetSymbols();
        public Symbol Greater => greater; public Symbol GreaterOrEqual => greaterOrEqual;
        public Symbol Caret => caret;
    }
}