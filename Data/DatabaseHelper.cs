using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using BureauApp.Models;

namespace BureauApp.Data
{
    public static class DatabaseHelper
    {
        private const string DbName = "bureau.db";
        private static string ConnectionString => $"Data Source={DbName}";

        public static void InitializeDatabase()
        {
            using (var connection = new SqliteConnection(ConnectionString))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                // Створюємо всі необхідні таблиці
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT UNIQUE NOT NULL,
                        Password TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS Items (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        ItemType INTEGER, 
                        Date TEXT,
                        Place TEXT,
                        Reward REAL,
                        Category TEXT,
                        IsArchived INTEGER DEFAULT 0,
                        ImagePath TEXT
                    );
                    CREATE TABLE IF NOT EXISTS Logs (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Action TEXT,
                        Timestamp TEXT
                    );";
                cmd.ExecuteNonQuery();

                // Дефолтний адмін
                cmd.CommandText = "INSERT OR IGNORE INTO Users (Username, Password) VALUES ('admin', 'admin')";
                cmd.ExecuteNonQuery();
            }
        }

        // --- БЕЗПЕКА ---
        public static bool RegisterUser(string user, string pass)
        {
            try {
                using var conn = new SqliteConnection(ConnectionString);
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Users (Username, Password) VALUES ($u, $p)";
                cmd.Parameters.AddWithValue("$u", user);
                cmd.Parameters.AddWithValue("$p", pass);
                cmd.ExecuteNonQuery();
                return true;
            } catch { return false; }
        }

        public static bool ValidateUser(string user, string pass)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = $u AND Password = $p";
            cmd.Parameters.AddWithValue("$u", user);
            cmd.Parameters.AddWithValue("$p", pass);
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // --- ФУНКЦІОНАЛ ---
        public static void LogAction(string action)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Logs (Action, Timestamp) VALUES ($a, $t)";
            cmd.Parameters.AddWithValue("$a", action);
            cmd.Parameters.AddWithValue("$t", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.ExecuteNonQuery();
        }

        public static void AddItem(Item item)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Items (Title, Description, ItemType, Date, Place, Reward, Category, IsArchived, ImagePath) 
                                VALUES ($title, $desc, $type, $date, $place, $reward, $cat, $arch, $img)";
            cmd.Parameters.AddWithValue("$title", item.Title);
            cmd.Parameters.AddWithValue("$desc", item.Description);
            cmd.Parameters.AddWithValue("$date", item.EventDate.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$place", item.Place);
            cmd.Parameters.AddWithValue("$cat", item.Category);
            cmd.Parameters.AddWithValue("$arch", item.IsArchived ? 1 : 0);
            cmd.Parameters.AddWithValue("$img", item.ImagePath);
            cmd.Parameters.AddWithValue("$type", (item is FoundItem) ? 1 : 0);
            cmd.Parameters.AddWithValue("$reward", (item is LostItem l) ? (double)l.Reward : 0);
            cmd.ExecuteNonQuery();
        }

        public static List<Item> GetAllItems()
        {
            var items = new List<Item>();
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM Items ORDER BY IsArchived ASC, Id DESC";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                int type = r.GetInt32(3);
                Item item = (type == 1) ? new FoundItem() : new LostItem { Reward = (decimal)r.GetDouble(6) };
                item.Id = r.GetInt32(0);
                item.Title = r.GetString(1);
                item.Description = r.IsDBNull(2) ? "" : r.GetString(2);
                item.EventDate = DateTime.Parse(r.GetString(4));
                item.Place = r.IsDBNull(5) ? "" : r.GetString(5);
                item.Category = r.IsDBNull(7) ? "Інше" : r.GetString(7);
                item.IsArchived = r.GetInt32(8) == 1;
                item.ImagePath = r.IsDBNull(9) ? "" : r.GetString(9);
                items.Add(item);
            }
            return items;
        }

        public static void ArchiveItem(int id)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE Items SET IsArchived = 1 WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }

        public static (int found, int lost, int archived) GetStats()
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT (SELECT COUNT(*) FROM Items WHERE ItemType=1 AND IsArchived=0), (SELECT COUNT(*) FROM Items WHERE ItemType=0 AND IsArchived=0), (SELECT COUNT(*) FROM Items WHERE IsArchived=1)";
            using var r = cmd.ExecuteReader();
            if (r.Read()) return (r.GetInt32(0), r.GetInt32(1), r.GetInt32(2));
            return (0, 0, 0);
        }

        public static void BackupDatabase()
        {
            string bkp = $"backup_{DateTime.Now:yyyyMMdd_HHmm}.db";
            File.Copy(DbName, bkp, true);
        }

        public static void DeleteItem(int id)
        {
            using var conn = new SqliteConnection(ConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM Items WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            cmd.ExecuteNonQuery();
        }
    }
}