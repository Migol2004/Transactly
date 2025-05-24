using System.Data.SQLite;

namespace Transactly
{
    public partial class Form1 : Form
    {
        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnLogin;
        private Label lblUsername;
        private Label lblPassword;
        private DatabaseManager _dbManager;

        public Form1()
        {
            InitializeComponent();
            InitializeLoginControls();
            _dbManager = new DatabaseManager();
        }

        private void InitializeLoginControls()
        {
            // Form settings
            this.Text = "Transactly POS - Login";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(400, 250);

            // Create controls
            lblUsername = new Label
            {
                Text = "Username:",
                Location = new Point(50, 50),
                Size = new Size(80, 20)
            };

            txtUsername = new TextBox
            {
                Location = new Point(150, 50),
                Size = new Size(180, 20)
            };

            lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(50, 90),
                Size = new Size(80, 20)
            };

            txtPassword = new TextBox
            {
                Location = new Point(150, 90),
                Size = new Size(180, 20),
                PasswordChar = '•'
            };

            btnLogin = new Button
            {
                Text = "Login",
                Location = new Point(150, 130),
                Size = new Size(100, 30)
            };

            // Add event handler
            btnLogin.Click += BtnLogin_Click;

            // Add controls to form
            this.Controls.AddRange(new Control[] { 
                lblUsername, txtUsername,
                lblPassword, txtPassword,
                btnLogin
            });
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.", "Login Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_dbManager.ValidateUser(username, password))
            {
                MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Open main form and hide login form
                MainForm mainForm = new MainForm();
                this.Hide();
                mainForm.FormClosed += (s, args) => this.Close(); // Close application when main form is closed
                mainForm.Show();
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Login Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
