using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestGameTask
{
    public class DataBase
    {
        private List<UserModel> usersCollection = new();

        public DataBase()
        {
            Console.WriteLine("Database successfully connected");

            AddUser("admin", "123");
            AddUser("user1", "123");
            AddUser("user2", "123");
            AddUser("user3", "123");
            AddUser("user4", "123");
            AddUser("user5", "123");
        }

        public void AddUser(string login, string password)
        {
            usersCollection.Add(new UserModel(login, password));
        }

        public string? TryLogin(string login, string password)
        {
            var user = FindByLogin(login);
            return user != null && user.IsCorrectPassword(password) ? user.Id : null;
        }

        public UserStats GetUserStats(string userId)
        {
            var user = FindById(userId);
            return new UserStats
            {
                Id = user?.Id ?? "unknown",
                Deaths = user?.Deaths ?? 0,
                Kills = user?.Kills ?? 0,
            };
        }

        public void UpdateUserStats(string userId, UserActionType type)
        {
            var user = FindById(userId);
            if(user == null)
            {
                Console.WriteLine("User not found");
                return;
            }
            user.UserAction(type);
        }

        private UserModel? FindByLogin(string login)
        {
            return usersCollection.FirstOrDefault(x => x.Login == login);
        }

        private UserModel? FindById(string id)
        {
           return usersCollection.FirstOrDefault(x => x.Id == id);
        }


        public class UserModel
        {
            public string Id { get; private set; }
            public int Kills { get; private set; }
            public int Deaths { get; private set; }

            public string Login { get; }

            private string Password { get; }

            public UserModel(string login, string password)
            {
                Id = Guid.NewGuid().ToString();
                Login = login;
                Password = password;
            }

            public void UserAction(UserActionType type)
            {
                switch (type)
                {
                    case UserActionType.Kill:
                        Kills++;
                        break;

                    case UserActionType.Death:
                        Deaths++;
                        break;
                }
            }

            public bool IsCorrectPassword(string password) => Password == password;
        }

        public enum UserActionType
        {
            Kill,
            Death
        }
    }
}
