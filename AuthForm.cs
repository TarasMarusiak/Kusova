using System;
using System.Drawing;
using System.Windows.Forms;
using BureauApp.Data;

namespace BureauApp
{
    public partial class AuthForm : Form
    {
        public string AuthenticatedUser { get; private set; } = "";
        private TextBox txtUser = null!;
        private TextBox txtPass = null!;

        public AuthForm()
        {
            // Налаштування вікна
            this.Text = "Авторизація | BureauApp";
            this.Size = new Size(400, 350);
            this.BackColor = Color.FromArgb(245, 246, 250); // Світлий фон (f5f6fa)
            this.FormBorderStyle = FormBorderStyle.None; // Сучасний вигляд без стандартних рамок
            this.StartPosition = FormStartPosition.CenterScreen;

            SetupUI();
        }

        private void SetupUI()
        {
            // --- ВЕРХНЯ ПАНЕЛЬ (Header) ---
            Panel pnlHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 60, 
                BackColor = Color.FromArgb(44, 62, 80) // Темно-синій (Ebony Clay)
            };

            Label lblHeader = new Label 
            { 
                Text = "ВХІД У СИСТЕМУ", 
                ForeColor = Color.White, // ВИПРАВЛЕНО: ForeColor замість Color
                Font = new Font("Segoe UI", 12, FontStyle.Bold), 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter 
            };

            // Кнопка закриття програми
            Button btnExit = new Button 
            { 
                Text = "✕", 
                Location = new Point(370, 5), 
                Size = new Size(25, 25), 
                FlatStyle = FlatStyle.Flat, 
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += (s, e) => this.Close();

            pnlHeader.Controls.Add(btnExit);
            pnlHeader.Controls.Add(lblHeader);
            this.Controls.Add(pnlHeader);

            // --- ПОЛЯ ВВОДУ ---
            txtUser = new TextBox 
            { 
                Location = new Point(50, 100), 
                Width = 300, 
                Font = new Font("Segoe UI", 11), 
                PlaceholderText = "Введіть логін..." 
            };

            txtPass = new TextBox 
            { 
                Location = new Point(50, 145), 
                Width = 300, 
                Font = new Font("Segoe UI", 11), 
                PlaceholderText = "Введіть пароль...", 
                PasswordChar = '●' 
            };

            // --- КНОПКИ ---
            Button btnLogin = CreateStyledButton("УВІЙТИ", 50, 200, Color.FromArgb(46, 204, 113)); // Зелена
            btnLogin.Click += (s, e) => 
            {
                if (DatabaseHelper.ValidateUser(txtUser.Text, txtPass.Text)) 
                {
                    AuthenticatedUser = txtUser.Text;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                } 
                else 
                {
                    MessageBox.Show("Невірний логін або пароль!", "Помилка доступу", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Button btnReg = CreateStyledButton("РЕЄСТРАЦІЯ", 50, 255, Color.FromArgb(52, 152, 219)); // Блакитна
            btnReg.Click += (s, e) => 
            {
                if (string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPass.Text))
                {
                    MessageBox.Show("Будь ласка, заповніть усі поля!");
                    return;
                }

                if (DatabaseHelper.RegisterUser(txtUser.Text, txtPass.Text)) 
                {
                    MessageBox.Show("Користувача успішно зареєстровано! Тепер ви можете увійти.");
                } 
                else 
                {
                    MessageBox.Show("Цей логін вже зайнятий. Оберіть інший.");
                }
            };

            this.Controls.AddRange(new Control[] { txtUser, txtPass, btnLogin, btnReg });
        }

        // Хелпер для створення однакових красивих кнопок
        private Button CreateStyledButton(string text, int x, int y, Color backColor)
        {
            Button btn = new Button 
            {
                Text = text, 
                Location = new Point(x, y), 
                Width = 300, 
                Height = 45,
                BackColor = backColor, 
                ForeColor = Color.White, 
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold), 
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }
    }
}