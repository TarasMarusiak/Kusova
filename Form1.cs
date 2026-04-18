using BureauApp.Models;
using BureauApp.Data;
using System.Data;
using System.IO;

namespace BureauApp;

public partial class Form1 : Form
{
    private DataGridView dgv = null!;
    private TextBox txtTitle = null!, txtDesc = null!, txtPlace = null!, txtReward = null!, txtSearch = null!;
    private ComboBox cbCategory = null!;
    private DateTimePicker dtPicker = null!;
    private CheckBox chkShowArchived = null!;
    private Label lblStats = null!;
    private PictureBox pbPhoto = null!;
    
    private string currentImagePath = "";
    private string currentUser = "";

    public Form1()
    {
        DatabaseHelper.InitializeDatabase();

        using (AuthForm auth = new AuthForm())
        {
            if (auth.ShowDialog() == DialogResult.OK)
            {
                this.currentUser = auth.AuthenticatedUser;
                DatabaseHelper.LogAction($"Вхід у систему: {currentUser}");
            }
            else
            {
                this.Load += (s, e) => this.Close();
                return;
            }
        }

        InitializeComponent();
        
        this.Text = $"BureauApp Ultimate v6.0 | Оператор: {currentUser.ToUpper()}";
        this.Size = new Size(1300, 850);
        this.MinimumSize = new Size(1250, 830); 
        this.BackColor = Color.FromArgb(236, 240, 241);
        this.StartPosition = FormStartPosition.CenterScreen;
        
        SetupInterface();
        RefreshGrid();
    }

    private void SetupInterface()
    {
        Font mainFont = new Font("Segoe UI", 9);

        // --- ВЕРХНЯ ПАНЕЛЬ ---
        Panel pnlTop = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = Color.FromArgb(44, 62, 80) };
        Label lblLogo = new Label { 
            Text = $"🏢 БЮРО ЗНАХІДОК  |  Авторизовано: {currentUser.ToUpper()}", 
            ForeColor = Color.White, Font = new Font("Segoe UI", 12, FontStyle.Bold), 
            Location = new Point(20, 18), AutoSize = true 
        };
        pnlTop.Controls.Add(lblLogo);
        this.Controls.Add(pnlTop);

        // --- ТАБЛИЦЯ ---
        dgv = new DataGridView { 
            Location = new Point(20, 80), Width = 900, Height = 340, 
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
            BackgroundColor = Color.White, BorderStyle = BorderStyle.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true,
            RowHeadersVisible = false, GridColor = Color.FromArgb(236, 240, 241),
            Font = mainFont
        };
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(52, 73, 94);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        dgv.EnableHeadersVisualStyles = false;
        dgv.SelectionChanged += (s, e) => ShowSelectedPhoto();
        this.Controls.Add(dgv);

        // --- ПАНЕЛЬ ФОТО ---
        pbPhoto = new PictureBox { 
            Location = new Point(940, 80), Width = 320, Height = 340, 
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.White, BorderStyle = BorderStyle.FixedSingle, 
            SizeMode = PictureBoxSizeMode.Zoom 
        };
        this.Controls.Add(pbPhoto);

        // --- ПАНЕЛЬ ПОШУКУ ТА КНОПОК (З НОВИМИ НАЗВАМИ) ---
        int searchY = 435;
        txtSearch = new TextBox { 
            Location = new Point(20, searchY), Width = 150, 
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left, 
            PlaceholderText = "🔎 Пошук..." 
        };
        txtSearch.TextChanged += (s, e) => RefreshGrid(txtSearch.Text);
        
        // Змінено: Додано слово "Архів"
        chkShowArchived = new CheckBox { 
            Text = "Показати АРХІВ", Location = new Point(180, searchY), 
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left, Checked = true, Width = 130 
        };
        chkShowArchived.CheckedChanged += (s, e) => RefreshGrid(txtSearch.Text);

        Button btnBkp = new Button { 
            Text = "💾 БЕКАП", Location = new Point(320, searchY - 5), Width = 100, Height = 32, 
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = Color.FromArgb(149, 165, 166), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8, FontStyle.Bold)
        };
        btnBkp.Click += (s, e) => { DatabaseHelper.BackupDatabase(); MessageBox.Show("Резервна копія створена!"); };

        // Змінено: Назва кнопки Експорту
        Button btnExport = new Button { 
            Text = "📄 ЕКСПОРТ ЗВІТУ", Location = new Point(430, searchY - 5), Width = 130, Height = 32, 
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = Color.FromArgb(127, 140, 141), ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8, FontStyle.Bold)
        };
        btnExport.Click += (s, e) => ExportToCSV();

        // Змінено: Назва кнопки Пошуку пари
        Button btnMatch = new Button { 
            Text = "🔍 ПОШУК ПАРИ", Location = new Point(570, searchY - 5), Width = 130, Height = 32, 
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            BackColor = Color.FromArgb(241, 196, 15), ForeColor = Color.Black, FlatStyle = FlatStyle.Flat, 
            Font = new Font("Segoe UI", 8, FontStyle.Bold) 
        };
        btnMatch.Click += (s, e) => RunMatchmaking();

        this.Controls.AddRange(new Control[] { txtSearch, chkShowArchived, btnBkp, btnExport, btnMatch });

        // --- ПАНЕЛЬ РЕЄСТРАЦІЇ ---
        GroupBox gb = new GroupBox { 
            Text = " РЕЄСТРАЦІЯ ОБ'ЄКТА ", Location = new Point(20, 475), 
            Size = new Size(550, 290), Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Font = new Font("Segoe UI", 9, FontStyle.Bold), BackColor = Color.White 
        };
        
        txtTitle = CreateField(gb, "Назва речі:", 30);
        txtDesc = CreateField(gb, "Опис:", 65, true);
        cbCategory = new ComboBox { Location = new Point(150, 130), Width = 370, DropDownStyle = ComboBoxStyle.DropDownList, Font = mainFont };
        cbCategory.Items.AddRange(new string[] { "Електроніка", "Документи", "Ключі", "Одяг", "Тварини", "Гроші", "Інше" });
        cbCategory.SelectedIndex = 0;
        gb.Controls.Add(new Label { Text = "Категорія:", Location = new Point(15, 133), Font = mainFont });
        gb.Controls.Add(cbCategory);

        dtPicker = new DateTimePicker { Location = new Point(150, 165), Width = 370, Font = mainFont };
        gb.Controls.Add(new Label { Text = "Дата події:", Location = new Point(15, 168), Font = mainFont });
        gb.Controls.Add(dtPicker);

        txtPlace = CreateField(gb, "Місце:", 200);
        txtReward = CreateField(gb, "Винагорода:", 235);
        txtReward.Width = 100;

        Button btnImg = new Button { 
            Text = "📁 ОБРАТИ ФОТО", Location = new Point(260, 233), 
            Width = 260, Height = 30, BackColor = Color.FromArgb(52, 73, 94), 
            ForeColor = Color.White, FlatStyle = FlatStyle.Flat 
        };
        btnImg.Click += (s, e) => SelectPhoto();
        gb.Controls.Add(btnImg);
        this.Controls.Add(gb);

        // --- КНОПКИ ДІЙ ---
        Button btnFound = CreateActionButton("✅ ЗНАЙДЕНО", 600, 485, Color.FromArgb(46, 204, 113));
        btnFound.Click += (s, e) => ActionItem(true);
        
        Button btnLost = CreateActionButton("🆘 ЗАГУБЛЕНО", 600, 560, Color.FromArgb(231, 76, 60));
        btnLost.Click += (s, e) => ActionItem(false);
        
        Button btnReturn = CreateActionButton("🤝 ПОВЕРНУТО", 600, 635, Color.FromArgb(52, 152, 219));
        btnReturn.Click += (s, e) => {
            if (dgv.SelectedRows.Count > 0) {
                DatabaseHelper.ArchiveItem((int)dgv.SelectedRows[0].Cells["ID"].Value);
                RefreshGrid();
            }
        };

        Button btnDelete = CreateActionButton("🗑️ ВИДАЛИТИ ЗАПИС", 600, 710, Color.FromArgb(127, 140, 141));
        btnDelete.Height = 45;
        btnDelete.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnDelete.Click += (s, e) => {
            if (dgv.SelectedRows.Count > 0) {
                if (MessageBox.Show("Видалити цей запис назавжди?", "Підтвердження", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                    DatabaseHelper.DeleteItem((int)dgv.SelectedRows[0].Cells["ID"].Value);
                    RefreshGrid();
                }
            }
        };

        btnFound.Anchor = btnLost.Anchor = btnReturn.Anchor = btnDelete.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.Controls.AddRange(new Control[] { btnFound, btnLost, btnReturn, btnDelete });

        // --- СТАТИСТИКА ---
        lblStats = new Label { 
            Dock = DockStyle.Bottom, Height = 35, 
            BackColor = Color.FromArgb(44, 62, 80), ForeColor = Color.White, 
            TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Padding = new Padding(15, 0, 0, 0)
        };
        this.Controls.Add(lblStats);

        dgv.CellFormatting += (s, e) => {
            if (dgv.Columns[e.ColumnIndex].Name == "Статус" && e.Value != null) {
                string val = e.Value.ToString()!;
                if (val == "АРХІВ") dgv.Rows[e.RowIndex].DefaultCellStyle.ForeColor = Color.Gray;
                else if (val == "Знайдено") dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.Honeydew;
                else dgv.Rows[e.RowIndex].DefaultCellStyle.BackColor = Color.MistyRose;
            }
        };
    }

    private TextBox CreateField(GroupBox gb, string label, int y, bool multi = false) {
        gb.Controls.Add(new Label { Text = label, Location = new Point(15, y + 3), Font = new Font("Segoe UI", 9), Width = 120 });
        TextBox tb = new TextBox { Location = new Point(150, y), Width = 370, Font = new Font("Segoe UI", 9) };
        if (multi) { tb.Multiline = true; tb.Height = 60; }
        gb.Controls.Add(tb);
        return tb;
    }

    private Button CreateActionButton(string text, int x, int y, Color color) {
        return new Button {
            Text = text, Location = new Point(x, y), Width = 300, Height = 70,
            BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand
        };
    }

    private void SelectPhoto() {
        using OpenFileDialog ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.jpeg" };
        if (ofd.ShowDialog() == DialogResult.OK) {
            if (!Directory.Exists("photos")) Directory.CreateDirectory("photos");
            string path = Path.Combine("photos", Guid.NewGuid().ToString() + Path.GetExtension(ofd.FileName));
            File.Copy(ofd.FileName, path, true);
            currentImagePath = path;
            pbPhoto.Image = Image.FromFile(path);
        }
    }

    private void ShowSelectedPhoto() {
        if (dgv.SelectedRows.Count > 0) {
            string p = dgv.SelectedRows[0].Cells["Фото"].Value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(p) && File.Exists(p)) pbPhoto.Image = Image.FromFile(p);
            else pbPhoto.Image = null;
        }
    }

    private void ExportToCSV() {
        var items = DatabaseHelper.GetAllItems();
        string file = "Звіт_Бюро_Знахідок.csv";
        using (StreamWriter sw = new StreamWriter(file, false, System.Text.Encoding.UTF8)) {
            sw.WriteLine("ID;Статус;Назва;Категорія;Дата;Місце");
            foreach(var i in items) sw.WriteLine($"{i.Id};{(i.IsArchived?"Архів":"Актив")};{i.Title};{i.Category};{i.EventDate.ToShortDateString()};{i.Place}");
        }
        MessageBox.Show($"Дані успішно експортовано у файл: {file}", "Експорт");
    }

    private void RunMatchmaking() {
        if (dgv.SelectedRows.Count == 0) return;
        string t = dgv.SelectedRows[0].Cells["Назва"].Value.ToString()!.Split(' ')[0];
        string c = dgv.SelectedRows[0].Cells["Категорія"].Value.ToString()!;
        var matches = DatabaseHelper.GetAllItems().Where(x => x.Category == c && x.Title.Contains(t, StringComparison.OrdinalIgnoreCase) && !x.IsArchived).ToList();
        if (matches.Any()) MessageBox.Show("Знайдено потенційні пари:\n" + string.Join("\n", matches.Select(m => "- " + m.Title + " (" + m.Place + ")")), "Розумний пошук");
        else MessageBox.Show("Збігів у базі не знайдено.", "Розумний пошук");
    }

    private void ActionItem(bool isFound) {
        if (string.IsNullOrWhiteSpace(txtTitle.Text)) return;
        Item item = isFound ? new FoundItem() : new LostItem { Reward = decimal.TryParse(txtReward.Text, out decimal r) ? r : 0 };
        item.Title = txtTitle.Text; item.Description = txtDesc.Text; item.Category = cbCategory.Text; item.EventDate = dtPicker.Value; item.Place = txtPlace.Text; item.ImagePath = currentImagePath;
        DatabaseHelper.AddItem(item);
        DatabaseHelper.LogAction($"Додано об'єкт: {item.Title}");
        currentImagePath = ""; pbPhoto.Image = null; txtTitle.Clear(); txtDesc.Clear(); RefreshGrid();
    }

    private void RefreshGrid(string filter = "") {
        var list = DatabaseHelper.GetAllItems();
        if (!chkShowArchived.Checked) list = list.Where(x => !x.IsArchived).ToList();
        if (!string.IsNullOrEmpty(filter)) list = list.Where(x => x.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        
        dgv.DataSource = list.Select(x => new { 
            ID = x.Id, 
            Статус = x.IsArchived ? "АРХІВ" : (x is FoundItem ? "Знайдено" : "Загублено"), 
            Категорія = x.Category, 
            Назва = x.Title, 
            Дата = x.EventDate.ToShortDateString(), 
            Фото = x.ImagePath 
        }).ToList();

        var s = DatabaseHelper.GetStats();
        lblStats.Text = $"📊 СТАТИСТИКА: Активні ({s.found + s.lost})  |  Знайдено ({s.found})  |  Загублено ({s.lost})  |  Архів ({s.archived})";
    }
}