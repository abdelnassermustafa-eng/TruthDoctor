using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TruthDoctor.Services;

public class User
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string Role { get; set; } = "Admin";
}

public class UserStore
{
    private readonly string _filePath;

    public UserStore()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dir = Path.Combine(home, ".truthdoctor");
        Directory.CreateDirectory(dir);

        _filePath = Path.Combine(dir, "users.json");
    }

    public List<User> LoadUsers()
    {
        if (!File.Exists(_filePath))
        {
            return new List<User>
            {
                new User { Username = "admin", Password = "admin123", Role = "Admin" }
            };
        }

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
    }

    public void SaveUsers(List<User> users)
    {
        var json = JsonSerializer.Serialize(users, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(_filePath, json);
    }

    public bool IsUsingDefaultCredentials()
    {
        var users = LoadUsers();
        return users.Count == 1 &&
               users[0].Username == "admin" &&
               users[0].Password == "admin123";
    }
}
