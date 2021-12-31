/// <summary>
/// a simple regex commit linter example
/// https://www.conventionalcommits.org/en/v1.0.0/
/// </summary>

using System.Text.RegularExpressions;
var pattern = @"^(?=.{1,90}$)(?<type>build|feat|ci|chore|docs|fix|perf|refactor|revert|style|test)(?<scope>\([a-z]+\))*(?<colon>:)(?<subject>[-a-zA-Z0-9._ ])+(?<![\.\s])$";

var msg = File.ReadAllLines(Args[0])[0];

if (Regex.IsMatch(msg, pattern))
   return 0;

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("Invalid commit message");
Console.ResetColor();
Console.WriteLine("e.g: 'feat(scope): subject' or 'fix: subject'");
Console.ForegroundColor = ConsoleColor.Gray;
Console.WriteLine("more info: https://www.conventionalcommits.org/en/v1.0.0/");

return 1;
