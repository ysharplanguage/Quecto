using System.Collections.Generic;
using System.Linq;
using System.Lisp;
namespace Quecto
{
    class Program {
        static void Main(string[] args) {
            var evaluator = new Lisp2(); var program = @"( /* This demoes various...
                ... well, features */
                var ( DudeFirst ""James"" DudeLast ""Bond""
                    Factorial ( n => ( ( 1 < n ) ? ( n * ( Factorial ( n - 1 ) ) ) : 1 ) )
                    this ( . ) // ('.' is the reflection operator) // Note the literal array constructor '#' below (we bundle the results in an array returned to the host)
                ) ( # ( $ ""12! = { ( Factorial ( 3 * ( 2 ^ 2 ) ) ) : 0,0 }"" )
                      ( $ ""{ ( 2 > 1 ) }! My name is {DudeLast}, {DudeFirst} {DudeLast}..."" )
                      ( $ ""And here's the full program's reflection... { this }"" )
                ) )";
            var tokens = new List<object>(); var at = 0; object read; System.Console.WriteLine($"Token (#1): {evaluator.Print(evaluator.ReadNextToken(program, out var matched, ref at, true))}");
            System.Diagnostics.Debug.Assert(at == 0); while (!Symbol.EOF.Equals(read = evaluator.ReadNextToken(program, out matched, ref at))) { tokens.Add(read); at += matched; }
            System.Console.WriteLine($"All tokens: {string.Join(" ", tokens.Select(evaluator.Print))}");
            var results = (object[])evaluator.Evaluate(null, program);
            System.Console.WriteLine($"\r\n{results[0]}\r\n{results[1]}\r\n{results[2]}");
            System.Console.WriteLine("\r\n(Press a key)"); System.Console.ReadKey(true);
        }
    }
}