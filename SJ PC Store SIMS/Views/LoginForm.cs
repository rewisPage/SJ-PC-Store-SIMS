using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Utils;
using System.Drawing.Drawing2D;

namespace SJ_PC_Store_SIMS.Views
{
    public partial class LoginForm : Form
    {
        // =========================================================================
        // CUSTOM MODAL ENGINE
        // =========================================================================
        private class ModalForm : Form
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED for flicker-free rendering
                    return cp;
                }
            }
        }

        private void ShowAlertModal(string title, string message, string type = "Error")
        {
            ModalForm modal = new ModalForm
            {
                FormBorderStyle = FormBorderStyle.None,
                BackColor = UITheme.CurrentPanel,
                StartPosition = FormStartPosition.CenterScreen,
                ShowInTaskbar = false,
                Size = new Size(400, 250)
            };

            // Draw Rounded Corners
            modal.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 12;
                    path.AddArc(0, 0, r, r, 180, 90);
                    path.AddArc(modal.Width - r - 1, 0, r, r, 270, 90);
                    path.AddArc(modal.Width - r - 1, modal.Height - r - 1, r, r, 0, 90);
                    path.AddArc(0, modal.Height - r - 1, r, r, 90, 90);
                    path.CloseFigure();
                    modal.Region = new Region(path);
                }
            };

            // Close Button
            IconButton btnClose = new IconButton
            {
                IconChar = IconChar.Times,
                IconSize = 20,
                Size = new Size(40, 40),
                FlatStyle = FlatStyle.Flat,
                ForeColor = UITheme.MutedText,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Location = new Point(350, 10)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnClose.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnClose.MouseEnter += (s, e) => { btnClose.IconColor = Color.FromArgb(239, 68, 68); };
            btnClose.MouseLeave += (s, e) => { btnClose.IconColor = UITheme.MutedText; };
            btnClose.Click += (s, e) => modal.Close();

            // Determine Icon and Colors based on Type
            IconChar warnIcon = type == "Success" ? IconChar.CheckCircle : (type == "Warning" ? IconChar.ExclamationTriangle : IconChar.TimesCircle);
            Color warnColor = type == "Success" ? Color.FromArgb(16, 185, 129) : (type == "Warning" ? Color.FromArgb(245, 158, 11) : Color.FromArgb(239, 68, 68));

            IconPictureBox iconWarning = new IconPictureBox { IconChar = warnIcon, IconColor = warnColor, IconSize = 60, Size = new Size(60, 60) };
            iconWarning.Location = new Point((modal.Width - iconWarning.Width) / 2, 30);

            Label lblWarn = new Label { Text = title, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
            lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 100);

            Label lblDesc = new Label { Text = message, Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
            lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 135);

            // Footer with "Okay" Button
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = UITheme.CurrentPanel };
            Button btnOkay = new Button { Text = "Okay", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnOkay.FlatAppearance.BorderColor = UITheme.CurrentBorder;
            btnOkay.FlatAppearance.MouseDownBackColor = UITheme.CurrentPanel;
            btnOkay.MouseEnter += (s, e) => btnOkay.BackColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230);
            btnOkay.MouseLeave += (s, e) => btnOkay.BackColor = Color.Transparent;
            btnOkay.Click += (s, e) => modal.Close();

            btnOkay.Location = new Point((modal.Width - btnOkay.Width) / 2, 15);
            pnlFooter.Controls.Add(btnOkay);

            modal.Controls.AddRange(new Control[] { btnClose, iconWarning, lblWarn, lblDesc, pnlFooter });

            // Overlay to darken the background
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = this.PointToScreen(Point.Empty), Size = this.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show();
            modal.ShowDialog(overlay);
            overlay.Dispose();
        }

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

        private Panel pnlResetView;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;
        private Button btnSavePassword;
        private Label lblResetTitle, lblResetDesc;

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

            SetupResetView();

            pnlLeftForm.Controls.Add(pnlLoginView);
            pnlLeftForm.Controls.Add(pnlRecoveryView);
            pnlLeftForm.Controls.Add(pnlResetView);

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
            Panel pnlPass = CreateIconInput("Enter password", IconChar.Lock, 60, 300, out txtPassword, true);
            txtPassword.UseSystemPasswordChar = true;

            btnSignIn = CreateStyledButton("SIGN IN", 60, 380);
            btnSignIn.Click += BtnSignIn_Click;

            lblForgotPassword = new Label { Text = "Forgot Password?", Font = UITheme.MainFont, AutoSize = true, Cursor = Cursors.Hand };
            pnlLoginView.Controls.Add(lblForgotPassword);
            lblForgotPassword.Location = new Point((pnlLoginView.Width - lblForgotPassword.Width) / 2 + 30, 450);

            lblForgotPassword.MouseEnter += (s, e) => lblForgotPassword.ForeColor = UITheme.AccentYellow;
            lblForgotPassword.MouseLeave += (s, e) => lblForgotPassword.ForeColor = UITheme.MutedText;
            lblForgotPassword.Click += (s, e) => SwitchView("Recovery");

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

            btnVerifyPasskey.Click += BtnVerifyPasskey_Click;

            lblBackToLogin = new Label { Text = "Back to Login", Font = UITheme.MainFont, AutoSize = true, Cursor = Cursors.Hand };
            pnlRecoveryView.Controls.Add(lblBackToLogin);
            lblBackToLogin.Location = new Point((pnlRecoveryView.Width - lblBackToLogin.Width) / 2 + 30, 450);

            lblBackToLogin.MouseEnter += (s, e) => lblBackToLogin.ForeColor = UITheme.AccentYellow;
            lblBackToLogin.MouseLeave += (s, e) => lblBackToLogin.ForeColor = UITheme.MutedText;
            lblBackToLogin.Click += (s, e) => SwitchView("Login");

            dynamicLabels.AddRange(new[] { lblRecUser, lblKey, lblBackToLogin });
            pnlRecoveryView.Controls.AddRange(new Control[] { lblAccountRec, lblEnterPasskey, lblRecUser, pnlRecUser, lblKey, pnlKey, btnVerifyPasskey });
        }

        private void SetupResetView()
        {
            pnlResetView = new Panel { Dock = DockStyle.Fill, Visible = false };

            lblResetTitle = new Label { Text = "Create New Password", Font = UITheme.HeaderFont, AutoSize = true, Location = new Point(60, 80) };
            lblResetDesc = new Label { Text = "Enter your new secure password below.", Font = UITheme.MainFont, AutoSize = true, Location = new Point(65, 125) };

            Label lblNewPass = new Label { Text = "NEW PASSWORD", Font = UITheme.LabelFont, AutoSize = true, Location = new Point(60, 180) };
            Panel pnlNewPass = CreateIconInput("Enter new password", IconChar.Lock, 60, 205, out txtNewPassword, true);
            txtNewPassword.UseSystemPasswordChar = true;

            Label lblConfirmPass = new Label { Text = "CONFIRM PASSWORD", Font = UITheme.LabelFont, AutoSize = true, Location = new Point(60, 275) };
            Panel pnlConfirmPass = CreateIconInput("Confirm new password", IconChar.CheckCircle, 60, 300, out txtConfirmPassword, true);
            txtConfirmPassword.UseSystemPasswordChar = true;

            btnSavePassword = CreateStyledButton("SAVE PASSWORD", 60, 380);
            btnSavePassword.Click += BtnSavePassword_Click;

            Label lblCancelReset = new Label { Text = "Cancel & Back to Login", Font = UITheme.MainFont, AutoSize = true, Cursor = Cursors.Hand };
            pnlResetView.Controls.Add(lblCancelReset);
            lblCancelReset.Location = new Point((pnlResetView.Width - lblCancelReset.Width) / 2 + 100, 450);

            lblCancelReset.MouseEnter += (s, e) => lblCancelReset.ForeColor = UITheme.AccentYellow;
            lblCancelReset.MouseLeave += (s, e) => lblCancelReset.ForeColor = UITheme.MutedText;
            lblCancelReset.Click += (s, e) => SwitchView("Login");

            dynamicLabels.AddRange(new[] { lblNewPass, lblConfirmPass, lblCancelReset });
            pnlResetView.Controls.AddRange(new Control[] { lblResetTitle, lblResetDesc, lblNewPass, pnlNewPass, lblConfirmPass, pnlConfirmPass, btnSavePassword });
        }

        private Panel CreateIconInput(string placeholder, IconChar iconChar, int x, int y, out TextBox txtRef, bool isPassword = false)
        {
            Panel pnlWrapper = new Panel { Location = new Point(x, y), Size = new Size(380, 50) };

            IconPictureBox icon = new IconPictureBox { IconChar = iconChar, IconSize = 24, Size = new Size(24, 24), Location = new Point(15, 13), BackColor = Color.Transparent };

            // If it's a password field, shorten the textbox to make room for the eye icon
            int txtWidth = isPassword ? 275 : 310;
            TextBox txt = new TextBox { Text = placeholder, Location = new Point(55, 14), Width = txtWidth, Font = UITheme.InputFont, BorderStyle = BorderStyle.None };

            // Unmask the placeholder, mask the text when typing
            txt.Enter += (s, e) =>
            {
                if (txt.Text == placeholder)
                {
                    txt.Text = "";
                    if (isPassword) txt.UseSystemPasswordChar = true;
                }
            };

            txt.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txt.Text))
                {
                    if (isPassword) txt.UseSystemPasswordChar = false;
                    txt.Text = placeholder;
                }
            };

            pnlWrapper.Controls.Add(icon);
            pnlWrapper.Controls.Add(txt);

            // Add the Eye Icon Toggle if it's a password field
            if (isPassword)
            {
                IconPictureBox eyeIcon = new IconPictureBox
                {
                    IconChar = IconChar.EyeSlash,
                    IconSize = 22,
                    Size = new Size(22, 22),
                    Location = new Point(340, 14), // Positioned on the right side
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };

                // Toggle logic
                eyeIcon.Click += (s, e) =>
                {
                    // Only toggle if they are actually typing a password (not the placeholder)
                    if (txt.Text != placeholder && !string.IsNullOrWhiteSpace(txt.Text))
                    {
                        txt.UseSystemPasswordChar = !txt.UseSystemPasswordChar;
                        eyeIcon.IconChar = txt.UseSystemPasswordChar ? IconChar.EyeSlash : IconChar.Eye;
                    }
                };

                pnlWrapper.Controls.Add(eyeIcon);

                // Add to your existing dynamic list so ApplyTheme() handles Dark/Light mode automatically!
                dynamicIcons.Add(eyeIcon);
            }

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
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0, BorderColor = UITheme.AccentYellow } // BorderColor is set to the same as BackColor to create a solid button look

            };
        }

        private void SwitchView(string viewName)
        {
            pnlLoginView.Visible = (viewName == "Login");
            pnlRecoveryView.Visible = (viewName == "Recovery");
            pnlResetView.Visible = (viewName == "Reset");

            // Optional: Clear sensitive fields when navigating away
            if (viewName == "Login")
            {
                txtPasskey.Text = "e.g. A1B2C3";

                txtNewPassword.UseSystemPasswordChar = false;
                txtNewPassword.Text = "Enter new password";

                txtConfirmPassword.UseSystemPasswordChar = false;
                txtConfirmPassword.Text = "Confirm new password";
            }
        }

        private void BtnSignIn_Click(object sender, EventArgs e)
        {
            var user = _authController.Login(txtUsername.Text, txtPassword.Text);
            if (user != null)
            {
                this.Hide();
                txtPassword.UseSystemPasswordChar = false;
                txtPassword.Text = "Enter password";

                DashboardForm dashboard = new DashboardForm(user);
                dashboard.ShowDialog();

                this.Show();
            }
            else
            {
                ShowAlertModal("Login Failed", "Invalid username or password.\nPlease check your credentials and try again.", "Error");
            }
        }

        private void BtnVerifyPasskey_Click(object sender, EventArgs e)
        {
            string username = txtRecoveryUsername.Text.Trim();
            string passkey = txtPasskey.Text.Trim();

            if (string.IsNullOrWhiteSpace(username) || username == "Enter your username" ||
                string.IsNullOrWhiteSpace(passkey) || passkey == "e.g. A1B2C3")
            {
                ShowAlertModal("Required Fields", "Please enter both your Username\nand Security Passkey.", "Warning");
                return;
            }

            if (_authController.VerifyPasskey(username, passkey))
            {
                SwitchView("Reset");
            }
            else
            {
                ShowAlertModal("Verification Failed", "Invalid Username or Passkey.\nPlease try again.", "Error");
            }
        }

        private void BtnSavePassword_Click(object sender, EventArgs e)
        {
            string newPass = txtNewPassword.Text;
            string confirmPass = txtConfirmPassword.Text;

            if (string.IsNullOrWhiteSpace(newPass) || newPass == "Enter new password" ||
                string.IsNullOrWhiteSpace(confirmPass) || confirmPass == "Confirm new password")
            {
                ShowAlertModal("Required Fields", "Please fill out both password fields.", "Warning");
                return;
            }

            if (newPass != confirmPass)
            {
                ShowAlertModal("Mismatch", "Passwords do not match.\nPlease re-enter.", "Error");
                return;
            }

            bool success = _authController.ResetPasswordWithPasskey(txtRecoveryUsername.Text, newPass);
            if (success)
            {
                ShowAlertModal("Success", "Password successfully reset!\nYou can now log in.", "Success");
                SwitchView("Login");
            }
            else
            {
                ShowAlertModal("Error", "Failed to reset password.\nPlease contact an administrator.", "Error");
            }
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

            if (lblResetTitle != null) lblResetTitle.ForeColor = UITheme.CurrentText;
            if (lblResetDesc != null) lblResetDesc.ForeColor = UITheme.MutedText;

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
    }
}