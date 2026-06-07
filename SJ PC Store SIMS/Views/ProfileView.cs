using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System.Drawing.Drawing2D;

namespace SJ_PC_Store_SIMS.Views
{
    public class ProfileView : UserControl
    {
        // =========================================================================
        // CUSTOM ENGINE COMPONENTS (Scraped & Replicated)
        // =========================================================================
        private class ModalForm : Form { protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } } }
        private class SmoothPanel : Panel { public SmoothPanel() { this.DoubleBuffered = true; this.ResizeRedraw = true; } }

        private class RoundedPanel : Panel
        {
            public int BorderRadius { get; set; } = 6;
            public int BorderSize { get; set; } = 1;
            public Color BorderColor { get; set; } = Color.Transparent;
            public RoundedPanel() { this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); this.BackColor = Color.Transparent; this.ResizeRedraw = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); if (this.Width <= 1 || this.Height <= 1) return; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = BorderRadius;
                    if (r <= 0) { path.AddRectangle(rect); }
                    else { path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90); path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90); path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90); path.CloseFigure(); }
                    using (SolidBrush brush = new SolidBrush(this.BackColor)) { e.Graphics.FillPath(brush, path); }
                    if (BorderSize > 0) { using (Pen pen = new Pen(BorderColor, BorderSize)) { e.Graphics.DrawPath(pen, path); } }
                }
            }
        }

        // =========================================================================
        // VIEW VARIABLES
        // =========================================================================
        private UserController _userController;
        private string _activeUserId;
        private UserModel _currentUser;

        private FlowLayoutPanel pnlMainLayout;
        private RoundedPanel pnlProfile, pnlPassword, pnlPasskey;

        // Theme tracking lists
        private List<RoundedPanel> _inputWrappers = new List<RoundedPanel>();
        private List<TextBox> _textInputs = new List<TextBox>();
        private List<IconButton> _buttons = new List<IconButton>();

        private TextBox txtCurrentPassword, txtNewPassword, txtPasskey;

        public ProfileView(string currentUserId)
        {
            _activeUserId = currentUserId;
            _userController = new UserController();
            _currentUser = _userController.GetUserById(currentUserId);

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.Padding = new Padding(35, 35, 35, 35);
            this.Margin = new Padding(0);

            InitializeUI();
            ApplyTheme();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (this.Parent != null) { this.Parent.BackColorChanged -= Parent_BackColorChanged; this.Parent.BackColorChanged += Parent_BackColorChanged; }
        }
        private void Parent_BackColorChanged(object sender, EventArgs e) { ApplyTheme(); }

        // =========================================================================
        // INITIALIZATION
        // =========================================================================
        private void InitializeUI()
        {
            pnlMainLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            BuildProfilePanel();
            BuildPasswordPanel();
            BuildPasskeyPanel();

            this.Controls.Add(pnlMainLayout);
        }

        private void BuildProfilePanel()
        {
            pnlProfile = new RoundedPanel { Width = 700, Height = 300, BorderRadius = 8, Margin = new Padding(0, 0, 0, 20) };

            Label lblHeader = new Label { Text = "Profile Information", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(25, 20) };
            pnlProfile.Controls.Add(lblHeader);

            int y = 65;
            AddControlRow(pnlProfile, "First Name", _currentUser.FirstName, 30, y, 300);
            AddControlRow(pnlProfile, "Last Name", _currentUser.LastName, 350, y, 300); y += 75;

            AddControlRow(pnlProfile, "Username", _currentUser.Username, 30, y, 300);
            AddControlRow(pnlProfile, "Contact Number", _currentUser.ContactNumber ?? "N/A", 350, y, 300); y += 75;

            AddControlRow(pnlProfile, "System Role", _currentUser.Role, 30, y, 300);

            // Hide the actual value inside a Masked TextBox
            txtPasskey = new TextBox { Text = _currentUser.Passkey ?? "N/A", UseSystemPasswordChar = true, ReadOnly = true };
            AddRevealableRow(pnlProfile, "Recovery Passkey", txtPasskey, 350, y, 300);

            pnlMainLayout.Controls.Add(pnlProfile);
        }

        private void BuildPasswordPanel()
        {
            pnlPassword = new RoundedPanel { Width = 700, Height = 220, BorderRadius = 8, Margin = new Padding(0, 0, 0, 20) };

            Label lblHeader = new Label { Text = "Change Password", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(25, 20) };
            pnlPassword.Controls.Add(lblHeader);

            int y = 65;
            txtCurrentPassword = new TextBox { UseSystemPasswordChar = true };
            txtNewPassword = new TextBox { UseSystemPasswordChar = true };

            AddRevealableRow(pnlPassword, "Current Password", txtCurrentPassword, 30, y, 300);
            AddRevealableRow(pnlPassword, "New Password", txtNewPassword, 350, y, 300); y += 75;

            IconButton btnSavePass = CreateButton("Update Password", IconChar.Save, "ActionAdd");
            btnSavePass.Location = new Point(480, y + 5);
            btnSavePass.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCurrentPassword.Text) || string.IsNullOrWhiteSpace(txtNewPassword.Text))
                {
                    ShowToast("Please fill in both password fields.", false);
                    return;
                }
                OpenModal("ConfirmPassword");
            };
            pnlPassword.Controls.Add(btnSavePass);

            pnlMainLayout.Controls.Add(pnlPassword);
        }

        private void BuildPasskeyPanel()
        {
            pnlPasskey = new RoundedPanel { Width = 700, Height = 200, BorderRadius = 8, Margin = new Padding(0, 0, 0, 20) };

            Label lblHeader = new Label { Text = "Account Recovery Passkey", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(25, 20) };
            Label lblDesc = new Label { Text = "Your 6-character alphanumeric passkey allows you to recover your account if you forget your password.\nGenerating a new passkey will permanently invalidate your current one.", Font = new Font("Segoe UI", 9.5F), Location = new Point(25, 55), AutoSize = true };
            pnlPasskey.Controls.Add(lblHeader);
            pnlPasskey.Controls.Add(lblDesc);

            IconButton btnGenPasskey = CreateButton("Generate New Passkey", IconChar.Key, "Danger");
            btnGenPasskey.Location = new Point(30, 115);
            btnGenPasskey.Click += (s, e) => OpenModal("ConfirmPasskey");

            pnlPasskey.Controls.Add(btnGenPasskey);
            pnlMainLayout.Controls.Add(pnlPasskey);
        }

        private void AddRevealableRow(Control parent, string lblText, TextBox txtBox, int xLoc, int yLoc, int w)
        {
            Label l = new Label { Text = lblText, Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(xLoc, yLoc), AutoSize = true };
            RoundedPanel p = new RoundedPanel { Location = new Point(xLoc, yLoc + 20), Size = new Size(w, 38), BorderRadius = 4, BorderSize = 1, Padding = new Padding(10, 8, 10, 8) };

            // 1. Setup the Eye Icon
            IconPictureBox iconEye = new IconPictureBox
            {
                IconChar = IconChar.EyeSlash,
                IconSize = 18,
                Size = new Size(24, 18),
                Dock = DockStyle.Right,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                IconColor = UITheme.MutedText
            };

            // 2. Setup the TextBox
            txtBox.Dock = DockStyle.Fill;
            txtBox.BorderStyle = BorderStyle.None;
            txtBox.Font = new Font("Segoe UI", 10.5F);
            txtBox.UseSystemPasswordChar = true; // Hidden by default

            // 3. Hover Effects for the Icon
            iconEye.MouseEnter += (s, e) => iconEye.IconColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
            iconEye.MouseLeave += (s, e) => iconEye.IconColor = UITheme.MutedText;

            // 4. Toggle Visibility Logic
            iconEye.Click += (s, e) =>
            {
                if (txtBox.UseSystemPasswordChar)
                {
                    txtBox.UseSystemPasswordChar = false;
                    iconEye.IconChar = IconChar.Eye;
                }
                else
                {
                    txtBox.UseSystemPasswordChar = true;
                    iconEye.IconChar = IconChar.EyeSlash;
                }
            };

            // Important: Add the icon FIRST, then the text box, so the Docking calculates correctly
            p.Controls.Add(iconEye);
            p.Controls.Add(txtBox);

            parent.Controls.AddRange(new Control[] { l, p });

            _inputWrappers.Add(p);
            _textInputs.Add(txtBox);
        }

        // =========================================================================
        // UTILITY BUILDERS
        // =========================================================================
        private void AddControlRow(Control parent, string lblText, string valText, int xLoc, int yLoc, int w)
        {
            Label l = new Label { Text = lblText, Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(xLoc, yLoc), AutoSize = true };
            RoundedPanel p = new RoundedPanel { Location = new Point(xLoc, yLoc + 20), Size = new Size(w, 38), BorderRadius = 4, BorderSize = 1, Padding = new Padding(10, 8, 10, 8) };
            TextBox t = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5F), ReadOnly = true, Text = valText };

            p.Controls.Add(t);
            parent.Controls.AddRange(new Control[] { l, p });

            _inputWrappers.Add(p); _textInputs.Add(t);
        }

        private void AddInputRow(Control parent, string lblText, TextBox txtBox, int xLoc, int yLoc, int w)
        {
            Label l = new Label { Text = lblText, Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(xLoc, yLoc), AutoSize = true };
            RoundedPanel p = new RoundedPanel { Location = new Point(xLoc, yLoc + 20), Size = new Size(w, 38), BorderRadius = 4, BorderSize = 1, Padding = new Padding(10, 8, 10, 8) };

            txtBox.Dock = DockStyle.Fill;
            txtBox.BorderStyle = BorderStyle.None;
            txtBox.Font = new Font("Segoe UI", 10.5F);

            p.Controls.Add(txtBox);
            parent.Controls.AddRange(new Control[] { l, p });

            _inputWrappers.Add(p); _textInputs.Add(txtBox);
        }

        private IconButton CreateButton(string text, IconChar icon, string type)
        {
            IconButton btn = new IconButton { Text = "  " + text, IconChar = icon, IconSize = 18, Height = 38, AutoSize = true, Padding = new Padding(10, 0, 10, 0), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText, Tag = type };
            btn.FlatAppearance.BorderSize = 0;
            _buttons.Add(btn);
            return btn;
        }

        // =========================================================================
        // TOAST & MODAL ENGINE
        // =========================================================================

        private void ShowToast(string msg, bool success)
        {
            Form parent = this.FindForm();
            if (parent == null) return;

            Label lbl = new Label { Text = msg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), AutoSize = true };
            int toastWidth = Math.Max(320, lbl.PreferredWidth + 80);

            Form toast = new Form { StartPosition = FormStartPosition.Manual, FormBorderStyle = FormBorderStyle.None, BackColor = UITheme.CurrentPanel, Size = new Size(toastWidth, 60), TopMost = true, ShowInTaskbar = false };
            toast.Location = new Point(parent.Right - toastWidth - 20, parent.Bottom - 80);

            toast.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 8; path.AddArc(0, 0, r, r, 180, 90); path.AddArc(toast.Width - r - 1, 0, r, r, 270, 90); path.AddArc(toast.Width - r - 1, toast.Height - r - 1, r, r, 0, 90); path.AddArc(0, toast.Height - r - 1, r, r, 90, 90); path.CloseFigure(); toast.Region = new Region(path);
                    using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawPath(p, path); }
                }
            };

            IconPictureBox icon = new IconPictureBox { IconChar = success ? IconChar.CheckCircle : IconChar.TimesCircle, IconColor = success ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68), IconSize = 24, Size = new Size(24, 24), Location = new Point(15, 18) };
            lbl.Location = new Point(45, 20);

            toast.Controls.AddRange(new Control[] { icon, lbl });

            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (s, e) => { toast.Close(); t.Stop(); };
            toast.Show(); t.Start();
        }

        private void OpenModal(string type)
        {
            ModalForm modal = new ModalForm { FormBorderStyle = FormBorderStyle.None, BackColor = UITheme.CurrentPanel, StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = false, Size = new Size(400, 250) };

            modal.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 12;
                    path.AddArc(0, 0, r, r, 180, 90); path.AddArc(modal.Width - r - 1, 0, r, r, 270, 90);
                    path.AddArc(modal.Width - r - 1, modal.Height - r - 1, r, r, 0, 90); path.AddArc(0, modal.Height - r - 1, r, r, 90, 90);
                    path.CloseFigure(); modal.Region = new Region(path);
                }
            };

            IconButton btnClose = new IconButton { IconChar = IconChar.Times, IconSize = 20, Size = new Size(40, 40), FlatStyle = FlatStyle.Flat, ForeColor = UITheme.MutedText, BackColor = Color.Transparent, Cursor = Cursors.Hand, Location = new Point(350, 10) };
            btnClose.FlatAppearance.BorderSize = 0; btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnClose.Click += (s, e) => modal.Close();
            btnClose.MouseEnter += (s, e) => btnClose.IconColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseLeave += (s, e) => btnClose.IconColor = UITheme.MutedText;

            IconPictureBox iconWarning = new IconPictureBox { IconChar = IconChar.ExclamationTriangle, IconColor = Color.FromArgb(239, 68, 68), IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 30) };

            string tText = type == "ConfirmPassword" ? "Update Password?" : "Generate Passkey?";
            string dText = type == "ConfirmPassword" ? "Are you sure you want to change your login password?" : "Are you sure you want to generate a new passkey?\nYour old one will be destroyed.";

            Label lblWarn = new Label { Text = tText, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
            Label lblDesc = new Label { Text = dText, Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };

            lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 100);
            lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 135);

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = UITheme.CurrentPanel };
            Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.FlatAppearance.MouseDownBackColor = UITheme.CurrentPanel;
            btnCancel.Click += (s, e) => modal.Close();

            Button btnAction = new Button { Text = "Confirm", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = UITheme.AccentYellow, ForeColor = Color.Black, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAction.FlatAppearance.BorderSize = 0;

            if (type == "ConfirmPasskey") { btnAction.BackColor = Color.FromArgb(239, 68, 68); btnAction.ForeColor = Color.White; }

            int startX = (modal.Width - (btnCancel.Width + 10 + btnAction.Width)) / 2;
            btnCancel.Location = new Point(startX, 15); btnAction.Location = new Point(startX + btnCancel.Width + 10, 15);

            btnAction.Click += (s, e) =>
            {
                if (type == "ConfirmPassword")
                {
                    if (_userController.ChangeUserPassword(_activeUserId, txtCurrentPassword.Text, txtNewPassword.Text))
                    {
                        modal.Close();
                        txtCurrentPassword.Clear();
                        txtNewPassword.Clear();
                        ShowToast("Password changed successfully!", true);
                    }
                    else
                    {
                        modal.Close();
                        ShowToast("Incorrect current password.", false);
                    }
                }
                else if (type == "ConfirmPasskey")
                {
                    string newPasskey = _userController.ResetUserPasskey(_activeUserId, _activeUserId, "Profile");
                    if (!string.IsNullOrEmpty(newPasskey))
                    {
                        modal.Close();
                        ShowPasskeyModal(_currentUser.Username, newPasskey);
                    }
                    else
                    {
                        modal.Close();
                        ShowToast("Database error generating passkey.", false);
                    }
                }
            };

            pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
            modal.Controls.AddRange(new Control[] { btnClose, iconWarning, lblWarn, lblDesc, pnlFooter });

            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show(); modal.ShowDialog(overlay); overlay.Dispose();
        }

        private void ShowPasskeyModal(string username, string passkey)
        {
            ModalForm modal = new ModalForm { FormBorderStyle = FormBorderStyle.None, BackColor = UITheme.CurrentPanel, StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = false, Size = new Size(450, 350) };

            modal.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 12;
                    path.AddArc(0, 0, r, r, 180, 90); path.AddArc(modal.Width - r - 1, 0, r, r, 270, 90);
                    path.AddArc(modal.Width - r - 1, modal.Height - r - 1, r, r, 0, 90); path.AddArc(0, modal.Height - r - 1, r, r, 90, 90);
                    path.CloseFigure(); modal.Region = new Region(path);
                    using (Pen p = new Pen(UITheme.CurrentBorder, 3)) { e.Graphics.DrawPath(p, path); }
                }
            };

            IconPictureBox iconSuccess = new IconPictureBox { IconChar = IconChar.CheckCircle, IconColor = Color.FromArgb(16, 185, 129), IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 30) };
            Label lblTitle = new Label { Text = "Passkey Reset Successfully!", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
            lblTitle.Location = new Point((modal.Width - lblTitle.PreferredWidth) / 2, 100);

            Label lblDesc = new Label { Text = $"Account for '{username}' is ready.\nSecurely store the recovery passkey below:", Font = new Font("Segoe UI", 9.5F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
            lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 135);

            RoundedPanel pnlPasskeyWrap = new RoundedPanel { Size = new Size(250, 50), BorderRadius = 6, BorderSize = 2, BorderColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, BackColor = UITheme.CurrentInputBg, Location = new Point((modal.Width - 250) / 2, 185) };
            Label lblPasskey = new Label { Text = passkey, Font = new Font("Consolas", 18F, FontStyle.Bold), ForeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlPasskeyWrap.Controls.Add(lblPasskey);

            Label lblWarn = new Label { Text = "⚠️ This passkey will not be shown again.", Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.FromArgb(239, 68, 68), AutoSize = true };
            lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 245);

            Button btnGotIt = new Button { Text = "Copy & Close", Size = new Size(150, 40), FlatStyle = FlatStyle.Flat, BackColor = UITheme.AccentYellow, ForeColor = Color.Black, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point((modal.Width - 150) / 2, 285) };
            btnGotIt.FlatAppearance.BorderSize = 0;

            btnGotIt.Click += (s, e) =>
            {
                Clipboard.SetText(passkey);
                modal.Close();
            };

            modal.Controls.AddRange(new Control[] { iconSuccess, lblTitle, lblDesc, pnlPasskeyWrap, lblWarn, btnGotIt });

            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show(); modal.ShowDialog(overlay); overlay.Dispose();
        }

        // =========================================================================
        // THEME ENGINE
        // =========================================================================
        public void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace;

            pnlProfile.BackColor = UITheme.CurrentPanel; pnlProfile.BorderColor = UITheme.CurrentBorder;
            pnlPassword.BackColor = UITheme.CurrentPanel; pnlPassword.BorderColor = UITheme.CurrentBorder;
            pnlPasskey.BackColor = UITheme.CurrentPanel; pnlPasskey.BorderColor = UITheme.CurrentBorder;

            foreach (Control c in pnlProfile.Controls) { if (c is Label l && l.Font.Bold) l.ForeColor = UITheme.CurrentText; }
            foreach (Control c in pnlPassword.Controls) { if (c is Label l && l.Font.Bold) l.ForeColor = UITheme.CurrentText; }
            foreach (Control c in pnlPasskey.Controls) { if (c is Label l && l.Font.Bold) l.ForeColor = UITheme.CurrentText; }
            foreach (Control c in pnlPasskey.Controls) { if (c is Label l && !l.Font.Bold) l.ForeColor = UITheme.MutedText; }

            foreach (RoundedPanel wrap in _inputWrappers)
            {
                wrap.BackColor = UITheme.CurrentInputBg;
                wrap.BorderColor = UITheme.CurrentBorder;

                // Add this inner loop to color the Eye icon dynamically
                foreach (Control c in wrap.Controls)
                {
                    if (c is IconPictureBox icon) icon.IconColor = UITheme.MutedText;
                }
            }
            foreach (TextBox txt in _textInputs) { txt.BackColor = UITheme.CurrentInputBg; txt.ForeColor = UITheme.CurrentText; }

            foreach (IconButton btn in _buttons)
            {
                string type = btn.Tag.ToString();
                if (type == "ActionAdd")
                {
                    btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.SecondaryDark;
                    btn.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White;
                    btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : Color.FromArgb(45, 42, 50);
                }
                else if (type == "Danger")
                {
                    btn.BackColor = Color.FromArgb(239, 68, 68);
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38);
                }
                btn.IconColor = btn.ForeColor;
                btn.FlatAppearance.MouseDownBackColor = btn.BackColor;
            }

            this.Invalidate(true);
        }
    }
}