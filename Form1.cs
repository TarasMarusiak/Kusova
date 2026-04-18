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
        if (!AuthProcess()) { Environment.Exit(0); return; }

        InitializeComponent();
        this.Text = $"BureauApp Ultimate v6.0 | Оператор: {currentUser}";
        this.Size = new Size(1300, 850);
        this.StartPosition = FormStartPosition.CenterScreen;
        SetupInterface();
        RefreshGrid();
    }

    private bool AuthProcess()
    {
        while (true)
        {
            string m = Microsoft.VisualBasic.Interaction.InputBox("Введіть '1' для Входу або '2' для Реєстрації:", "Авторизація", "1");
            if (string.IsNullOrEmpty(m)) return false;

            string u = Microsoft.VisualBasic.Interaction.InputBox("Логін:", "Користувач", "");
            string p = Microsoft.VisualBasic.Interaction.InputBox("Пароль:", "Безпека", "");
            if (string.IsNullOrEmpty(u)) return false;

            if (m == "1") {
                if (DatabaseHelper.ValidateUser(u, p)) { currentUser = u; DatabaseHelper.LogAction($"Вхід: {u}"); return true; }
                MessageBox.Show("Помилка входу!");
            } else {
                if (DatabaseHelper.RegisterUser(u, p)) MessageBox.Show("Успішно! Тепер увійдіть.");
                else MessageBox.Show("Логін зайнятий!");
            }
        }
    }

    private void SetupInterface()
    {
        dgv = new DataGridView { Location = new Point(20, 20), Width = 900, Height = 350, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, SelectionMode = DataGridViewSelectionMode.FullRowSelect, ReadOnly = true };
        dgv.SelectionChanged += (s, e) => ShowSelectedPhoto();
        this.Controls.Add(dgv);

        pbPhoto = new PictureBox { Location = new Point(940, 20), Width = 320, Height = 350, BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.White };
        this.Controls.Add(pbPhoto);

        lblStats = new Label { Location = new Point(20, 770), Width = 800, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
        this.Controls.Add(lblStats);

        txtSearch = new TextBox { Location = new Point(20, 390), Width = 250, PlaceholderText = "Пошук..." };
        txtSearch.TextChanged += (s, e) => RefreshGrid(txtSearch.Text);
        this.Controls.Add(txtSearch);

        chkShowArchived = new CheckBox { Text = "Показати архів", Location = new Point(280, 390), Checked = true };
        chkShowArchived.CheckedChanged += (s, e) => RefreshGrid(txtSearch.Text);
        this.Controls.Add(chkShowArchived);

        Button btnBkp = new Button { Text = "БЕКАП", Location = new Point(450, 385), Width = 100 };
        btnBkp.Click += (s, e) => { DatabaseHelper.BackupDatabase(); MessageBox.Show("Готово!"); };
        
        Button btnExport = new Button { Text = "ЕКСПОРТ CSV", Location = new Point(560, 385), Width = 120 };
        btnExport.Click += (s, e) => ExportToCSV();

        Button btnMatch = new Button { Text = "MATCHING", Location = new Point(690, 385), Width = 120, BackColor = Color.Gold };
        btnMatch.Click += (s, e) => RunMatchmaking();

        this.Controls.AddRange(new Control[] { btnBkp, btnExport, btnMatch });

        GroupBox gb = new GroupBox { Text = "Панель керування", Location = new Point(20, 430), Size = new Size(550, 330) };
        txtTitle = new TextBox { Location = new Point(150, 30), Width = 370 };
        txtDesc = new TextBox { Location = new Point(150, 70), Width = 370, Multiline = true, Height = 60 };
        cbCategory = new ComboBox { Location = new Point(150, 140), Width = 370, DropDownStyle = ComboBoxStyle.DropDownList };
        cbCategory.Items.AddRange(new string[] { "Електроніка", "Документи", "Ключі", "Одяг", "Тварини", "Гроші", "Інше" });
        cbCategory.SelectedIndex = 0;
        dtPicker = new DateTimePicker { Location = new Point(150, 180), Width = 370 };
        txtPlace = new TextBox { Location = new Point(150, 220), Width = 370 };
        txtReward = new TextBox { Location = new Point(150, 260), Width = 150 };
        Button btnImg = new Button { Text = "ФОТО", Location = new Point(310, 258), Width = 210 };
        btnImg.Click += (s, e) => SelectPhoto();
        gb.Controls.AddRange(new Control[] { txtTitle, txtDesc, cbCategory, dtPicker, txtPlace, txtReward, btnImg });
        this.Controls.Add(gb);

        Button btnF = new Button { Text = "ЗНАЙДЕНО", Location = new Point(600, 440), Width = 300, Height = 60, BackColor = Color.Honeydew };
        btnF.Click += (s, e) => ActionItem(true);
        Button btnL = new Button { Text = "ЗАГУБЛЕНО", Location = new Point(600, 510), Width = 300, Height = 60, BackColor = Color.MistyRose };
        btnL.Click += (s, e) => ActionItem(false);
        Button btnA = new Button { Text = "ПОВЕРНУТО", Location = new Point(600, 590), Width = 300, Height = 60, BackColor = Color.LightSkyBlue };
        btnA.Click += (s, e) => { if (dgv.SelectedRows.Count > 0) DatabaseHelper.ArchiveItem((int)dgv.SelectedRows[0].Cells["ID"].Value); RefreshGrid(); };
        this.Controls.AddRange(new Control[] { btnF, btnL, btnA });
    }

    private void SelectPhoto() {
        using OpenFileDialog ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.png" };
        if (ofd.ShowDialog() == DialogResult.OK) {
            if (!Directory.Exists("photos")) Directory.CreateDirectory("photos");
            string path = Path.Combine("photos", Path.GetFileName(ofd.FileName));
            File.Copy(ofd.FileName, path, true);
            currentImagePath = path;
            pbPhoto.Image = Image.FromFile(path);
        }
    }

    private void ShowSelectedPhoto() {
        if (dgv.SelectedRows.Count > 0) {
            string p = dgv.SelectedRows[0].Cells["Фото"].Value?.ToString() ?? "";
            pbPhoto.Image = (!string.IsNullOrEmpty(p) && File.Exists(p)) ? Image.FromFile(p) : null;
        }
    }

    private void ExportToCSV() {
        var items = DatabaseHelper.GetAllItems();
        using StreamWriter sw = new StreamWriter("report.csv");
        sw.WriteLine("ID;Status;Title;Category");
        foreach(var i in items) sw.WriteLine($"{i.Id};{i.IsArchived};{i.Title};{i.Category}");
        MessageBox.Show("Звіт report.csv створено!");
    }

    private void RunMatchmaking() {
        if (dgv.SelectedRows.Count == 0) return;
        string t = dgv.SelectedRows[0].Cells["Назва"].Value.ToString()!.Split(' ')[0];
        string c = dgv.SelectedRows[0].Cells["Категорія"].Value.ToString()!;
        var matches = DatabaseHelper.GetAllItems().Where(x => x.Category == c && x.Title.Contains(t) && !x.IsArchived).ToList();
        MessageBox.Show(matches.Any() ? "Знайдено: " + string.Join(", ", matches.Select(m => m.Title)) : "Нічого не знайдено");
    }

    private void ActionItem(bool isFound) {
        if (string.IsNullOrWhiteSpace(txtTitle.Text)) return;
        Item item = isFound ? new FoundItem() : new LostItem { Reward = decimal.TryParse(txtReward.Text, out decimal r) ? r : 0 };
        item.Title = txtTitle.Text; item.Description = txtDesc.Text; item.Category = cbCategory.Text; item.EventDate = dtPicker.Value; item.Place = txtPlace.Text; item.ImagePath = currentImagePath;
        DatabaseHelper.AddItem(item);
        DatabaseHelper.LogAction($"Додано: {item.Title} користувачем {currentUser}");
        currentImagePath = ""; pbPhoto.Image = null; RefreshGrid();
    }

    private void RefreshGrid(string filter = "") {
        var list = DatabaseHelper.GetAllItems();
        if (!chkShowArchived.Checked) list = list.Where(x => !x.IsArchived).ToList();
        if (!string.IsNullOrEmpty(filter)) list = list.Where(x => x.Title.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
        dgv.DataSource = list.Select(x => new { ID = x.Id, Статус = x.IsArchived ? "АРХІВ" : (x is FoundItem ? "Знайдено" : "Загублено"), Категорія = x.Category, Назва = x.Title, Дата = x.EventDate.ToShortDateString(), Фото = x.ImagePath }).ToList();
        var s = DatabaseHelper.GetStats();
        lblStats.Text = $"📊 СИСТЕМА: Знайдено ({s.found}) | Загублено ({s.lost}) | Архів ({s.archived})";
    }
}