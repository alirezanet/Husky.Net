using Husky;

var x = await CliActions.Run();
x.ToString().Log();

Console.Read();

await Cli.Start(args);
