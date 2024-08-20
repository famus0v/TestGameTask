using static TestGameTask.Server.Transform;

namespace TestGameTask
{
    public class Client
    {
        public string UserId { get; private set; }
        public string Login { get; private set; }
        public float Health = 100f;
        private const float MaxHealth = 100f;
        public float WeaponDamage = 1.5f;
        private const int RestIntervalMilliseconds = 1000;
        private const float RestHealCount = 5;
        public bool IsRest = false;
        public bool isBot { get; private set; }

        public Client? fightTargetClient;
        public Server? choosedServer;

        public bool IsFighting() => fightTargetClient != null;
        private bool CanMove()
        {
            if (fightTargetClient != null) return false;
            return true;
        }


        public Client(BotMock? botMock = null)
        {
            if(botMock != null)
            {
                isBot = true;
                WeaponDamage = 1f;

                var botId = ServerManager.LoginClient(botMock.login, botMock.password);
                if (botId == null) return;
                UserId = botId;
                Login = botMock.login;

                var Server = ServerManager.GetServerById(botMock.serverId);
                if (Server == null) return;
                Server.ConnectClient(this);
                choosedServer = Server;

                return;
            }

            string? login, password;
            Console.WriteLine("Введите логин: (ДЛЯ ТЕСТА 'admin') ");
            login = Console.ReadLine();
            Console.WriteLine("Введите пароль: (ДЛЯ ТЕСТА '123')");
            password = Console.ReadLine();

            if (login == null || password == null)
            {
                Console.WriteLine("Данные введены некорректно");
                return;
            }

            var userId = ServerManager.LoginClient(login, password);

            if (userId == null)
            {
                Console.WriteLine("Неверный логин или пароль");
                return;
            }

            UserId = userId;

            Login = login;

            ChooseServer();
        }

        private void ChooseServer()
        {
            Console.WriteLine("Выберите сервер:");
            Console.WriteLine(ServerManager.GetServerIds());

            if (!int.TryParse(Console.ReadLine(), out int serverId))
            {
                Console.WriteLine("Вы неправильно написали номер сервера");
                return;
            }

            var Server = ServerManager.GetServerById(serverId);
            if (Server == null)
            {
                Console.WriteLine("Сервер с таким номером не найден");
                return;
            }

            Server.ConnectClient(this);

            choosedServer = Server;

            GameLoop();
        }

        private void GameLoop()
        {
            while (true)
            {
                if (IsFighting()) continue;
                Console.WriteLine();
                Console.WriteLine("Выберите действие (цифра):");
                Console.WriteLine("1. Движение (north, south, east, west)");
                Console.WriteLine("2. Отдохнуть");
                Console.WriteLine("3. Атака");
                Console.WriteLine("4. Список игроков");
                Console.WriteLine("5. Статистика игроков");
                var action = Console.ReadLine();

                switch (action)
                {
                    case "1":
                        Console.Write("Напишите направление (как в списке): ");
                        var directionName = Console.ReadLine();
                        if(!Enum.TryParse(directionName, true, out Direction direction))
                        {
                            Console.WriteLine("Направление введено неверно");
                            break;
                        }
                        choosedServer.transform.MoveClient(this, direction);
                        break;

                    case "2":
                        IsRest = true;
                        RestLoop();
                        break;

                    case "3":
                        Console.Write("Напишите имя игрока, чтобы начать сражаться: ");
                        var targetId = Console.ReadLine();
                        if(targetId == null)
                        {
                            Console.WriteLine("ID введен неверно");
                            break;
                        }
                        var fightStarted = choosedServer.TryStartFight(this, targetId);
                        
                        if(!fightStarted)
                            Console.WriteLine("Сражение не началось, скорее всего вы не рядом с этим игроком");
                        
                        break;

                    case "4":
                        choosedServer.DisplayWorldState(this);
                        break;

                    case "5":
                        choosedServer.DisplayUsersStats(this);
                        break;

                    default:
                        Console.WriteLine("Неверное действие!");
                        break;
                }
            }
        }

        public void RestLoop()
        {
            Console.WriteLine($"Вы начали отдыхать, генерация {RestHealCount} в {(float)(RestIntervalMilliseconds / 1000)} сек.");

            while(IsRest)
            {
                if(Health >= MaxHealth)
                {
                    Console.WriteLine("У вас полное здоровье");
                    IsRest = false;
                    return;
                }

                Health += RestHealCount;
                if (Health > MaxHealth)
                    Health = 100f;
                Console.WriteLine($"Здоровье пополнено, Ваше здоровье: {Health}");

                Thread.Sleep(RestIntervalMilliseconds);
            }
        }



        // mock для ботиков :)
        public class BotMock
        {
            public string login;
            public string password;
            public int serverId;

            public BotMock(string log, string pass, int server)
            {
                login = log;
                password = pass;
                serverId = server;
            }
        }

    }
}
