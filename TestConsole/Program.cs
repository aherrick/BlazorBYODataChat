using TestConsole.Helpers;

var chatHelper = new ChatHelper();

await chatHelper.ExecuteInMemoryKernel();

Console.Read();