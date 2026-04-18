using BureauApp.Models;
using BureauApp.Data;
using System.Data;

namespace BureauApp;

public partial class Form1 : Form
{
    // Оголошення елементів інтерфейсу
    private DataGridView dgv;
    private TextBox txtTitle;
    private TextBox txtDesc;
    private TextBox txtExtra;
    private TextBox txtSearch;

    public Form1()
    {
        InitializeComponent();
        
        // Налаштування вікна
        this.Text = "Інформаційна система 'Бюро знахідок'";
        this.Size = new Size(900, 700);
        this.StartPosition = FormStartPosition.CenterScreen;
        
        // Ініціалізація інтерфейсу та завантаження даних
        SetupInterface();
        RefreshGrid();
    }

    private void SetupInterface()
    {
        // 1. Таблиця (DataGridView) для відображення списку речей
        dgv = new DataGridView 
        { 
            Location = new Point(20, 20), 
            Width = 840, 
            Height = 300, 
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            BackgroundColor = Color.White
        };
        this.Controls.Add(dgv);

        // 2. Блок пошуку (Реалізація алгоритму фільтрації)
        Label lblSearch = new Label { Text = "Пошук за назвою:", Location = new Point(20, 340), Width = 120 };
        txtSearch = new TextBox { Location = new Point(150, 340), Width = 200 };
        txtSearch.TextChanged += (s, e) => RefreshGrid(txtSearch.Text); // Пошук під час вводу
        this.Controls.Add(lblSearch);
        this.Controls.Add(txtSearch);

        // 3. Група для додавання нових записів
        GroupBox gbAdd = new GroupBox { Text = "Додати новий запис", Location = new Point(20, 380), Size = new Size(400, 250) };
        
        Label lblTitle = new Label { Text = "Назва:", Location = new Point(20, 30), Width = 100 };
        txtTitle = new TextBox { Location = new Point(130, 30), Width = 230 };
        
        Label lblDesc = new Label { Text = "Опис:", Location = new Point(20, 70), Width = 100 };
        txtDesc = new TextBox { Location = new Point(130, 70), Width = 230, Multiline = true, Height = 60 };

        Label lblExtra = new Label { Text = "Місце / Ціна:", Location = new Point(20, 150), Width = 100 };
        txtExtra = new TextBox { Location = new Point(130, 150), Width = 230 };

        gbAdd.Controls.AddRange(new Control[] { lblTitle, txtTitle, lblDesc, txtDesc, lblExtra, txtExtra });
        this.Controls.Add(gbAdd);

        // 4. Кнопки управління
        Button btnAddFound = new Button 
        { 
            Text = "Зберегти як ЗНАЙДЕНЕ", 
            Location = new Point(440, 400), 
            Width = 220, 
            Height = 45, 
            BackColor = Color.LightGreen,
            FlatStyle = FlatStyle.Flat 
        };
        btnAddFound.Click += (s, e) => SaveItem(true);

        Button btnAddLost = new Button 
        { 
            Text = "Зберегти як ЗАГУБЛЕНЕ", 
            Location = new Point(440, 460), 
            Width = 220, 
            Height = 45, 
            BackColor = Color.LightSalmon,
            FlatStyle = FlatStyle.Flat 
        };
        btnAddLost.Click += (s, e) => SaveItem(false);

        this.Controls.AddRange(new Control[] { btnAddFound, btnAddLost });
    }

    // Метод для збереження даних у базу (Використовує моделі ООП)
    private void SaveItem(bool isFound)
    {
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show("Будь ласка, введіть назву речі!");
            return;
        }

        Item newItem;
        if (isFound)
        {
            // Створення об'єкта FoundItem (Успадкування)
            newItem = new FoundItem { 
                Title = txtTitle.Text, 
                Description = txtDesc.Text, 
                EventDate = DateTime.Now, 
                FoundPlace = txtExtra.Text 
            };
        }
        else
        {
            // Створення об'єкта LostItem (Успадкування)
            decimal.TryParse(txtExtra.Text, out decimal reward);
            newItem = new LostItem { 
                Title = txtTitle.Text, 
                Description = txtDesc.Text, 
                EventDate = DateTime.Now, 
                Reward = reward 
            };
        }

        // Збереження через DatabaseHelper
        DatabaseHelper.AddItem(newItem);
        
        // Очищення полів та оновлення таблиці
        ClearInputs();
        RefreshGrid();
        MessageBox.Show("Запис успішно додано!");
    }

    // Метод для оновлення таблиці та фільтрації (Поліморфізм)
    private void RefreshGrid(string filter = "")
    {
        var items = DatabaseHelper.GetAllItems();
        
        // Фільтрація списку (LINQ)
        if (!string.IsNullOrEmpty(filter))
        {
            items = items.Where(i => i.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // Прив'язка даних до таблиці
        dgv.DataSource = items.Select(i => new {
            ID = i.Id,
            Статус = i is FoundItem ? "Знайдено" : "Загублено",
            Назва = i.Title,
            Опис = i.Description,
            Дата = i.EventDate.ToShortDateString(),
            Інформація = i.GetSummary() // Тут працює ПОЛІМОРФІЗМ
        }).ToList();
    }

    private void ClearInputs()
    {
        txtTitle.Clear();
        txtDesc.Clear();
        txtExtra.Clear();
    }
}
