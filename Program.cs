using TestGameTask;

Console.WriteLine("Приветствую в моем тестовом задании, вроде прикольно получилось)");

ServerManager.StartServer(1);

new Client(new Client.BotMock("user1", "123", 1));
new Client(new Client.BotMock("user2", "123", 1));
new Client(new Client.BotMock("user3", "123", 1));
new Client(new Client.BotMock("user4", "123", 1));
new Client(new Client.BotMock("user5", "123", 1));

new Client();
