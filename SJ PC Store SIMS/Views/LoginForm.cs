using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Utils;
using SJ_PC_Store_SIMS.Controllers;
using System.Collections.Generic;

namespace SJ_PC_Store_SIMS.Views
{
    public partial class LoginForm : Form
    {
        private AuthController _authController = new AuthController();

        private Panel pnlLeftForm;
        private Panel pnlRightBrand;
        private Panel pnlLoginView;
        private Panel pnlRecoveryView;

        private IconButton btnThemeToggle;

        private TextBox txtUsername;
        private TextBox txtPassword;
        private Button btnSignIn;
        private Label lblForgotPassword;

        private TextBox txtRecoveryUsername;
        private TextBox txtPasskey;
        private Button btnVerifyPasskey;
        private Label lblBackToLogin;

        private List<Label> dynamicLabels = new List<Label>();
        private List<Panel> dynamicInputPanels = new List<Panel>();
        private List<TextBox> dynamicTextBoxes = new List<TextBox>();
        private List<IconPictureBox> dynamicIcons = new List<IconPictureBox>();

        private Label lblSystemLogin, lblEnterCreds, lblAccountRec, lblEnterPasskey;

        public LoginForm()
        {
            InitializeProgrammaticUI();
            ApplyTheme();
        }

        private void InitializeProgrammaticUI()
        {
            this.Size = new Size(950, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;

            // --- RIGHT BRANDING PANEL (GRADIENT) ---
            pnlRightBrand = new Panel { Dock = DockStyle.Right, Width = 450 };
            pnlRightBrand.Paint += (s, e) =>
            {
                using (LinearGradientBrush brush = new LinearGradientBrush(pnlRightBrand.ClientRectangle, UITheme.PrimaryDark, UITheme.SecondaryDark, 135F))
                {
                    e.Graphics.FillRectangle(brush, pnlRightBrand.ClientRectangle);
                }
            };

            IconPictureBox logo = new IconPictureBox { IconChar = IconChar.Microchip, IconColor = UITheme.AccentYellow, IconSize = 80, Size = new Size(80, 80), BackColor = Color.Transparent };
            Label lblTitle = new Label { Text = "SJ PC Store", Font = new Font("Segoe UI", 28F, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, BackColor = Color.Transparent };
            Label lblSubTitle = new Label { Text = "Integrated Sales and Inventory\nManagement System", Font = new Font("Segoe UI", 11.5F, FontStyle.Regular), ForeColor = Color.LightGray, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, BackColor = Color.Transparent };
            Label lblVersion = new Label { Text = "Version 1.0.0", Font = new Font("Segoe UI", 9F), ForeColor = Color.LightGray, AutoSize = true, BackColor = Color.Transparent };

            // --- EXIT BUTTON ---
            IconButton btnExit = new IconButton
            {
                IconChar = IconChar.Xmark,
                IconSize = 24,
                Size = new Size(30, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                IconColor = Color.LightGray,
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnExit.FlatAppearance.MouseDownBackColor = Color.Transparent;

            // Hover effect for Exit Button
            btnExit.MouseEnter += (s, e) => btnExit.IconColor = UITheme.AccentYellow;
            btnExit.MouseLeave += (s, e) => btnExit.IconColor = Color.LightGray;

            // Close the entire application when clicked
            btnExit.Click += (s, e) => Application.Exit();

            pnlRightBrand.Controls.AddRange(new Control[] { logo, lblTitle, lblSubTitle, lblVersion, btnExit });

            // Placements
            logo.Location = new Point((pnlRightBrand.Width - logo.Width) / 2, 130);
            lblTitle.Location = new Point((pnlRightBrand.Width - lblTitle.Width) / 2, 230);
            lblSubTitle.Location = new Point((pnlRightBrand.Width - lblSubTitle.Width) / 2, 290);
            lblVersion.Location = new Point(350, 510);
            btnExit.Location = new Point(pnlRightBrand.Width - 40, 15); // Top Right corner

            // --- LEFT FORM PANEL ---
            pnlLeftForm = new Panel { Dock = DockStyle.Fill };

            btnThemeToggle = new IconButton
            {
                IconChar = IconChar.Moon,
                IconSize = 26,
                Size = new Size(40, 40),
                Location = new Point(20, 20),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnThemeToggle.FlatAppearance.BorderSize = 0;
            btnThemeToggle.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnThemeToggle.FlatAppearance.MouseDownBackColor = Color.Transparent;

            btnThemeToggle.MouseEnter += (s, e) => btnThemeToggle.IconColor = UITheme.AccentYellow;
            btnThemeToggle.MouseLeave += (s, e) => btnThemeToggle.IconColor = UITheme.CurrentIcon;
            btnThemeToggle.Click += (s, e) => { UITheme.ToggleTheme(); ApplyTheme(); };

            pnlLeftForm.Controls.Add(btnThemeToggle);

            SetupLoginView();
            SetupRecoveryView();

            pnlLeftForm.Controls.Add(pnlLoginView);
            pnlLeftForm.Controls.Add(pnlRecoveryView);

            this.Controls.Add(pnlLeftForm);
            this.Controls.Add(pnlRightBrand);
        }

        private void SetupLoginView()
        {
            pnlLoginView = new Panel { Dock = DockStyle.Fill };

            lblSystemLogin = new Label { Text = "Login Your Account", Font = UITheme.HeaderFont, AutoSize = true, Location = new Point(60, 80) };
            lblEnterCreds = new Label { Text = "Enter your credentials to access the dashboard.", Font = UITheme.MainFont, AutoSize = true, Location = new Point(65, 125) };

            Label lblUser = new Label { Text = "USERNAME", Font = UITheme.LabelFont, AutoSize = true, Location = new Point(60, 180) };
            Panel pnlUser = CreateIconInput("Enter username", IconChar.UserAlt, 60, 205, out txtUsername);

            Label lblPass = new Label { Text = "PASSWORD", Font = UITheme.LabelFont, AutoSize = true, Location = new Point(60, 275) };
            Panel pnlPass = CreateIconInput("Enter password", IconChar.Lock, 60, 300, out txtPassword);
            txtPassword.UseSystemPasswordChar = true;

            btnSignIn = CreateStyledButton("SIGN IN", 60, 380);
            btnSignIn.Click += BtnSignIn_Click;

            lblForgotPassword = new Label { Text = "Forgot Password?", Font = UITheme.MainFont, AutoSize = true, Cursor = Cursors.Hand };
            pnlLoginView.Controls.Add(lblForgotPassword);
            lblForgotPassword.Location = new Point((pnlLoginView.Width - lblForgotPassword.Width) / 2 + 30, 450);

            lblForgotPassword.MouseEnter += (s, e) => lblForgotPassword.ForeColor = UITheme.AccentYellow;
            lblForgotPassword.MouseLeave += (s, e) => lblForgotPassword.ForeColor = UITheme.MutedText;
            lblForgotPassword.Click += (s, e) => ToggleView(false);

            dynamicLabels.AddRange(new[] { lblUser, lblPass, lblForgotPassword });
            pnlLoginView.Controls.AddRange(new Control[] { lblSystemLogin, lblEnterCreds, lblUser, pnlUser, lblPass, pnlPass, btnSignIn });
        }

        private void SetupRecoveryView()
        {
            pnlRecoveryView = new Panel { Dock = DockStyle.Fill, Visible = false };

            lblAccountRec = new Label { Text = "Account Recovery", Font = UITheme.HeaderFont, AutoSize = true, Location = new Point(60, 80) };
            lblEnterPasskey = new Label { Text = "Enter your 6-character recovery passkey.", Font = UITheme.MainFont, AutoSize = true, Location = new Point(65, 125) };

            Label lblRecUser = new Label { Text = "USERNAME", Font = UITheme.LabelFont, AutoSize = true, Location = new Point(60, 180) };
            Panel pnlRecUser = CreateIconInput("Enter your username", IconChar.UserAlt, 60, 205, out txtRecoveryUsername);

            Label lblKey = new Label { Text = "SECURITY PASSKEY", Font = UITheme.LabelFont, AutoSize = true, Location = new Point(60, 275) };
            Panel pnlKey = CreateIconInput("e.g. A1B2C3", IconChar.Key, 60, 300, out txtPasskey);
            txtPasskey.MaxLength = 6;

            btnVerifyPasskey = CreateStyledButton("VERIFY PASSKEY", 60, 380);

            lblBackToLogin = new Label { Text = "Back to Login", Font = UITheme.MainFont, AutoSize = true, Cursor = Cursors.Hand };
            pnlRecoveryView.Controls.Add(lblBackToLogin);
            lblBackToLogin.Location = new Point((pnlRecoveryView.Width - lblBackToLogin.Width) / 2 + 30, 450);

            lblBackToLogin.MouseEnter += (s, e) => lblBackToLogin.ForeColor = UITheme.AccentYellow;
            lblBackToLogin.MouseLeave += (s, e) => lblBackToLogin.ForeColor = UITheme.MutedText;
            lblBackToLogin.Click += (s, e) => ToggleView(true);

            dynamicLabels.AddRange(new[] { lblRecUser, lblKey, lblBackToLogin });
            pnlRecoveryView.Controls.AddRange(new Control[] { lblAccountRec, lblEnterPasskey, lblRecUser, pnlRecUser, lblKey, pnlKey, btnVerifyPasskey });
        }

        private Panel CreateIconInput(string placeholder, IconChar iconChar, int x, int y, out TextBox txtRef)
        {
            Panel pnlWrapper = new Panel { Location = new Point(x, y), Size = new Size(380, 50) };

            IconPictureBox icon = new IconPictureBox { IconChar = iconChar, IconSize = 24, Size = new Size(24, 24), Location = new Point(15, 13), BackColor = Color.Transparent };

            TextBox txt = new TextBox { Text = placeholder, Location = new Point(55, 14), Width = 310, Font = UITheme.InputFont, BorderStyle = BorderStyle.None };

            txt.Enter += (s, e) => { if (txt.Text == placeholder) txt.Text = ""; };
            txt.Leave += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) txt.Text = placeholder; };

            pnlWrapper.Controls.Add(icon);
            pnlWrapper.Controls.Add(txt);

            dynamicInputPanels.Add(pnlWrapper);
            dynamicTextBoxes.Add(txt);
            dynamicIcons.Add(icon);
            txtRef = txt;

            return pnlWrapper;
        }

        private Button CreateStyledButton(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Width = 380,
                Height = 50,
                BackColor = UITheme.AccentYellow,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
        }

        private void ToggleView(bool showLogin)
        {
            pnlLoginView.Visible = showLogin;
            pnlRecoveryView.Visible = !showLogin;
        }

        private void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace;
            pnlLeftForm.BackColor = UITheme.CurrentPanel;

            btnThemeToggle.IconChar = UITheme.IsDarkMode ? IconChar.Sun : IconChar.Moon;
            btnThemeToggle.IconColor = UITheme.CurrentIcon;

            lblSystemLogin.ForeColor = UITheme.CurrentText;
            lblAccountRec.ForeColor = UITheme.CurrentText;
            lblEnterCreds.ForeColor = UITheme.MutedText;
            lblEnterPasskey.ForeColor = UITheme.MutedText;

            foreach (var lbl in dynamicLabels) lbl.ForeColor = UITheme.MutedText;
            foreach (var icon in dynamicIcons) icon.IconColor = UITheme.CurrentIcon;

            foreach (var pnl in dynamicInputPanels)
            {
                pnl.BackColor = UITheme.CurrentInputBg;
                pnl.Padding = new Padding(1);
                pnl.Paint += (s, e) => { ControlPaint.DrawBorder(e.Graphics, pnl.ClientRectangle, UITheme.CurrentBorder, ButtonBorderStyle.Solid); };
                pnl.Invalidate();
            }

            foreach (var txt in dynamicTextBoxes)
            {
                txt.BackColor = UITheme.CurrentInputBg;
                txt.ForeColor = UITheme.CurrentText;
            }
        }

        private void BtnSignIn_Click(object sender, EventArgs e)
        {
            var user = _authController.Login(txtUsername.Text, txtPassword.Text);
            if (user != null)
            {
                this.Hide(); // Hide the login screen
                txtPassword.Clear(); // Clear password for security

                // Open Dashboard as a dialog
                DashboardForm dashboard = new DashboardForm(user);
                dashboard.ShowDialog();

                // When the dashboard is closed (Logout), this line runs to show the login screen again!
                this.Show();
            }
            else
            {
                MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}