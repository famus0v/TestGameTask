
using static TestGameTask.DataBase;

namespace TestGameTask
{

    public class Server
    {
        public Server(int id) 
        {
            Console.WriteLine($"Server {id} successfully started");
            Number = id;
        }

        public int Number;
        public List<Client> connectedClients = new();
        public Transform transform = new();

        private const int AttackIntervalMilliseconds = 500;

        public void ConnectClient(Client client)
        {
            connectedClients.Add(client);
            transform.SpawnClient(client);
            DisplayWorldState(client);
        }

        public void DisplayUsersStats(Client client)
        {
            if (client.isBot) return;

            Console.WriteLine();
            Console.WriteLine("Статистика игроков:");

            foreach (var _client in connectedClients)
            {
                var stats = ServerManager.GetUserStatsFromDb(_client.UserId);
                Console.WriteLine((client.UserId == _client.UserId ? "Вы: ": "")
                    +$"{_client.Login}: Kills {stats.Kills}, Deaths {stats.Deaths}");
            }
        }

        public void DisplayWorldState(Client client)
        {
            if (client.isBot) return;

            Console.WriteLine();
            Console.WriteLine("Состояние игроков:");

            foreach (var _client in connectedClients)
            {
                var position = transform.GetServerPosition(_client);
                Console.WriteLine((client.UserId == _client.UserId ? "Вы: " : "")
                    + $"{_client.Login}: Position ({position.x}, {position.y}), Health: {_client.Health}");
            }
        }

        // можно реализовать по разным серверам разная статистика, но это долговато
        public static UserStats GetUserStats(Client client)
        {
           return ServerManager.GetUserStatsFromDb(client.UserId);
        }

        public List<UserStats> GetOtherUsersStats(Client client)
        {
            List<UserStats> usersStats = new();
            foreach(var _client in connectedClients)
            {
                if (_client.UserId == client.UserId) continue;
                usersStats.Add(ServerManager.GetUserStatsFromDb(_client.UserId));
            }
            return usersStats;
        }

        public List<Transform.Position> GetClientsPositions(Client client)
        {
            List<Transform.Position> usersPositions = new();
            foreach (var _client in connectedClients)
            {
                if (_client.UserId == client.UserId) continue;
                var position = transform.GetServerPosition(_client);
                usersPositions.Add(position);
            }
            return usersPositions;
        }

        private Client? GetClientById(string id) => connectedClients.FirstOrDefault(x => x.UserId == id);
        private Client? GetClientByLogin(string login) => connectedClients.FirstOrDefault(x => x.Login == login);

        public bool TryStartFight(Client client, string login)
        {
            var targetClient = GetClientByLogin(login);
            bool canStart = CanStartFight(client, targetClient);
            if (!canStart) return false;

            if (targetClient.IsRest)
            {
                targetClient.IsRest = false;
                Console.WriteLine($"{targetClient.Login} отдыхал, но {client.Login} напал на него");
            }

            client.fightTargetClient = targetClient;
            targetClient.fightTargetClient = client;

            StartFight(client);

            return true;
        }

        public void StartFight(Client client)
        {
            var targetClient = client.fightTargetClient;

            if (targetClient == null)
            {
                Console.WriteLine("Игрок пропал во время драки");
                return;
            }

            Console.WriteLine($"Сражение началось: {client.Login} и {targetClient.Login}");
            while (client.Health > 0 && targetClient.Health > 0)
            {
                client.Health -= targetClient.WeaponDamage;
                targetClient.Health -= client.WeaponDamage;

                Console.WriteLine($"{client.Login}: {client.Health} HP, {targetClient.Login}: {targetClient.Health} HP");

                Thread.Sleep(AttackIntervalMilliseconds);
            }

            Client defeatedClient;
            Client victoriousClient;

            if (client.Health <= 0)
            {
                defeatedClient = client;
                victoriousClient = targetClient;
            }
            else
            {
                defeatedClient = targetClient;
                victoriousClient = client;
            }

            Console.WriteLine($"{defeatedClient.Login} потерпел поражение.");
            UpdateStats(defeatedClient.UserId, UserActionType.Death);
            UpdateStats(victoriousClient.UserId, UserActionType.Kill);
            defeatedClient.Health = 100f;
            transform.SpawnClient(defeatedClient);

            client.fightTargetClient = null;
            targetClient.fightTargetClient = null;
        }

        private static void UpdateStats(string userId, UserActionType type) => ServerManager.UpdateDbUserStats(userId, type);

        public bool CanStartFight(Client client, Client? targetClient)
        {
            if (targetClient == null) return false;
            if (targetClient.IsFighting()) return false;
            if (!transform.IsCloseTo(client, targetClient)) return false;
            return true;
        }

        public class Transform
        {
            // userId : position
            public Dictionary<string, Position> serverPositions = new();
            private const float MaxFightDistance = 2f;
            private const float MoveDistance = 1f;

            public void SpawnClient(Client client)
            {
                var newPosition = new Position();
                var userId = client.UserId;
                if (!serverPositions.ContainsKey(userId)) serverPositions.Add(userId, newPosition);
                Console.WriteLine($"Вы успешно заспавнились: ({newPosition.x}, {newPosition.y})");
            }

            public bool IsCloseTo(Client first, Client second)
            {
                var clientPosition = GetServerPosition(first);
                var targetClientPosition = GetServerPosition(second);

                return IsCloseTo(clientPosition, targetClientPosition);
            }

            public static bool IsCloseTo(Position first, Position second) => CheckDistance(first, second, MaxFightDistance);

            public static bool CheckDistance(Position first, Position second, float maxDistance)
            {
                int deltaX = Math.Abs(first.x - second.x);
                int deltaY = Math.Abs(first.y - second.y);

                return deltaX <= maxDistance && deltaY <= maxDistance;
            }

            public Position GetServerPosition(Client client)
            {
                var userId = client.UserId;
                if (!serverPositions.ContainsKey(userId)) serverPositions.Add(userId, new Position());
                var serverPosition = serverPositions[userId];
                return serverPosition;
            }

            public void MoveClient(Client client, Direction direction)
            {
                var clientPosition = GetServerPosition(client);
                clientPosition.MoveTo(direction);
            }

            public enum Direction
            {
                North,      // cевер
                East,       // восток
                South,      // юг
                West        // запад
            }

            public class Position
            {
                public int x { get; private set; }
                public int y { get; private set; }

                public Position()
                {
                    Random random = new();
                    x = random.Next(-30, 30);
                    y = random.Next(-30, 30);
                }

                public void MoveTo(Direction direction)
                {
                    switch (direction)
                    {
                        case Direction.North:
                            y += 1;
                            break;
                        case Direction.South:
                            y -= 1;
                            break;
                        case Direction.East:
                            x += 1;
                            break;
                        case Direction.West:
                            x -= 1;
                            break;
                        default:
                            Console.WriteLine("Направление введено неверно!");
                            return;
                    }
                    Console.WriteLine($"Ваша новая позиция: ({x},{y})");
                }
            }
        }

    }
}
