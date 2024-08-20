using static TestGameTask.DataBase;

namespace TestGameTask
{
    public static class ServerManager
    {
        private static List<Server> activeServers = new();

        //  использую такой подход, потому что разделять всю статистику по разным серверам вообще сейчас не охота
        //  для ToDo можно оставить
        private static DataBase currentDatabase = new DataBase();

        public static void StartServer(int id)
        {
            var newServer = new Server(id);
            activeServers.Add(newServer);
        }

        public static Server? GetServerById(int id) => activeServers.FirstOrDefault(x=>x.Number == id);
        public static string GetServerIds() =>  string.Join(", ", activeServers.Select(o => o.Number.ToString()));
        

        public static string? LoginClient(string username, string password) => currentDatabase.TryLogin(username, password);
        
        public static UserStats GetUserStatsFromDb(string Id)
        {
           return currentDatabase.GetUserStats(Id);
        }

        public static void UpdateDbUserStats(string Id, UserActionType type) => currentDatabase.UpdateUserStats(Id, type);
    }
}
