// example of using a nuget package
// #r "nuget:Gridify, 2.5.0"

// example of using other csx files
#load "foo.csx"

var message = "Husky supports csharp scripting!";

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine(message);

foreach (var arg in Args)
{
    Console.WriteLine($"arg: {arg}");
}

