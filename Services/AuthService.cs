using System.Linq;

namespace TruthDoctor.Services
{
    public class AuthService
    {
        private readonly UserStore _userStore;

        public AuthService(UserStore userStore)
        {
            _userStore = userStore;
        }

        public User? Authenticate(string username, string password)
        {
            var users = _userStore.LoadUsers();

            return users.FirstOrDefault(u =>
                u.Username == username &&
                u.Password == password);
        }

        public bool ChangeCredentials(string oldUsername, string oldPassword,
                                      string newUsername, string newPassword)
        {
            var users = _userStore.LoadUsers();

            var user = users.FirstOrDefault(u =>
                u.Username == oldUsername &&
                u.Password == oldPassword);

            if (user == null)
                return false;

            user.Username = newUsername;
            user.Password = newPassword;

            _userStore.SaveUsers(users);
            return true;
        }

        public bool IsUsingDefaultCredentials()
        {
            return _userStore.IsUsingDefaultCredentials();
        }
    }
}
