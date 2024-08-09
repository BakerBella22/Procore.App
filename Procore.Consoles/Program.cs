// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");

var myclient = new Procore.Core.Class1();

var result = await myclient.TestConnection();

Console.WriteLine(result.Count);