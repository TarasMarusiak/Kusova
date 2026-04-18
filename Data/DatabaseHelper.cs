using Microsoft.Data.Sqlite;
using System;
using System.IO;

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

                var createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS Items (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Title TEXT NOT NULL,
                        Description TEXT,
                        ItemType INTEGER, -- 0 для загублених, 1 для знайдених
                        Date TEXT,
                        ExtraInfo TEXT    -- Тут зберігаємо або місце, або винагороду
                    );";

                var command = connection.CreateCommand();
                command.CommandText = createTableQuery;
                command.ExecuteNonQuery();
            }
        }
    public static void AddItem(BureauApp.Models.Item item)
    {
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
            INSERT INTO Items (Title, Description, ItemType, Date, ExtraInfo)
            VALUES ($title, $desc, $type, $date, $extra)";

            command.Parameters.AddWithValue("$title", item.Title);
            command.Parameters.AddWithValue("$desc", item.Description);
            // Поліморфна перевірка типу для БД
            command.Parameters.AddWithValue("$type", item is BureauApp.Models.FoundItem ? 1 : 0);
            command.Parameters.AddWithValue("$date", item.EventDate.ToString("yyyy-MM-dd"));
            
            command.ExecuteNonQuery();
        }
    }
    public static List<BureauApp.Models.Item> GetAllItems()
    {
        var items = new List<BureauApp.Models.Item>();
        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Items";

            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    int type = reader.GetInt32(3); // Поле ItemType
                    BureauApp.Models.Item item;

                   // Реалізація поліморфізму при завантаженні
                    if (type == 1) 
                    item = new BureauApp.Models.FoundItem { FoundPlace = reader.GetString(5) };
                    else 
                    item = new BureauApp.Models.LostItem { Reward = decimal.Parse(reader.GetString(5)) };

                   item.Id = reader.GetInt32(0);
                   item.Title = reader.GetString(1);
                   item.Description = reader.GetString(2);
                   item.EventDate = DateTime.Parse(reader.GetString(4));
                
                   items.Add(item);
                }
            }
        }
        return items;
    }
}