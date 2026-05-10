using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Utils;
using SJ_PC_Store_SIMS.Models;
using System.Collections.Generic;

namespace SJ_PC_Store_SIMS.Views
{
    public partial class DashboardForm : Form
    {
        private UserModel _currentUser;
        private System.Windows.Forms.Timer _clockTimer;

        // Main Layout Panels
        private Panel pnlSidebar;
        private Panel pnlHeader;
        private Panel pnlWorkspace;

        // Interactive Elements
        private IconButton btnHamburger;
        private IconButton btnThemeToggle;
        private Label lblClock;
        private Label lblWelcome;
        private Label lblPageTitle;
        private IconPictureBox logoIcon;
        private Label lblBrandText;
        private IconPictureBox avatarIcon;

        // Collections for dynamic theme updates
        private List<Panel> _roundedCards = new List<Panel>();
        private List<Label> _dynamicTexts = new List<Label>();
        private List<Label> _mutedTexts = new List<Label>();
        private List<IconButton> _navButtons = new List<IconButton>();
        private List<Panel> _moduleSections = new List<Panel>();

        // RBAC Tracking
        private List<Control> _adminOnlyControls = new List<Control>();

        public DashboardForm(UserModel user)
        {
            _currentUser = user;
            InitializeProgrammaticUI();
            ApplyTheme();
            ApplyRBAC();
            StartClock();
        }

        private void InitializeProgrammaticUI()
        {
            this.Size = new Size(1366, 768);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "SJ PC Store - Master Dashboard";

            // --- SIDEBAR (Left) --- 
            pnlSidebar = new Panel { Dock = DockStyle.Left, Width = 260 };

            Panel pnlBrand = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.Transparent };

            logoIcon = new IconPictureBox { IconChar = IconChar.Microchip, IconColor = UITheme.AccentYellow, IconSize = 36, Size = new Size(36, 36), Location = new Point(12, 17), BackColor = Color.Transparent };
            lblBrandText = new Label { Text = "SJ PC Store", Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = UITheme.AccentYellow, AutoSize = true, Location = new Point(55, 20), BackColor = Color.Transparent };

            pnlBrand.Paint += (s, e) => { e.Graphics.DrawLine(new Pen(Color.FromArgb(25, 255, 255, 255), 1), 0, 69, pnlBrand.Width, 69); };
            pnlBrand.Controls.AddRange(new Control[] { logoIcon, lblBrandText });

            FlowLayoutPanel flpNav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 20, 0, 0), BackColor = Color.Transparent };

            IconButton btnDash = CreateNavButton("Dashboard", IconChar.ChartPie, true);
            IconButton btnPOS = CreateNavButton("Sales POS", IconChar.CashRegister, false);
            IconButton btnInv = CreateNavButton("Inventory", IconChar.Boxes, false);
            IconButton btnProc = CreateNavButton("Procurement", IconChar.TruckLoading, false);
            IconButton btnData = CreateNavButton("Data Management", IconChar.Database, false);
            IconButton btnReports = CreateNavButton("Reports & Analytics", IconChar.ChartLine, false);
            IconButton btnUsers = CreateNavButton("User Management", IconChar.Users, false);
            IconButton btnProfile = CreateNavButton("My Profile", IconChar.UserGear, false);
            IconButton btnSettings = CreateNavButton("Settings", IconChar.Cog, false);

            _adminOnlyControls.AddRange(new Control[] { btnInv, btnProc, btnData, btnReports, btnUsers, btnSettings });
            flpNav.Controls.AddRange(new Control[] { btnDash, btnPOS, btnInv, btnProc, btnData, btnReports, btnUsers, btnProfile, btnSettings });

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.Transparent };
            pnlFooter.Paint += (s, e) => { e.Graphics.DrawLine(new Pen(Color.FromArgb(25, 255, 255, 255), 1), 0, 0, pnlFooter.Width, 0); };

            IconButton btnLogout = CreateNavButton("Logout", IconChar.SignOutAlt, false);
            btnLogout.IconColor = Color.FromArgb(255, 107, 107);
            btnLogout.ForeColor = Color.FromArgb(255, 107, 107);
            btnLogout.Click += (s, e) => { this.Close(); };
            pnlFooter.Controls.Add(btnLogout);

            pnlSidebar.Controls.Add(flpNav);
            pnlSidebar.Controls.Add(pnlFooter);
            pnlSidebar.Controls.Add(pnlBrand);

            // --- HEADER (Top) ---
            pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70 };

            btnHamburger = new IconButton { IconChar = IconChar.Bars, IconSize = 24, Size = new Size(40, 40), Location = new Point(20, 15), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnHamburger.FlatAppearance.BorderSize = 0;
            btnHamburger.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnHamburger.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnHamburger.MouseEnter += (s, e) => btnHamburger.IconColor = UITheme.AccentYellow;
            btnHamburger.MouseLeave += (s, e) => btnHamburger.IconColor = UITheme.CurrentIcon;
            btnHamburger.Click += BtnHamburger_Click;

            lblPageTitle = new Label { Text = "Master Dashboard", Font = new Font("Segoe UI", 16F, FontStyle.Bold), AutoSize = true, Location = new Point(70, 20) };

            Panel pnlHeaderRight = new Panel { Dock = DockStyle.Right, Width = 500, BackColor = Color.Transparent };

            lblClock = new Label { Text = "Loading time...", Font = UITheme.MainFont, AutoSize = true, Location = new Point(0, 25) };

            btnThemeToggle = new IconButton { IconChar = IconChar.Moon, IconSize = 24, Size = new Size(40, 40), Location = new Point(250, 15), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnThemeToggle.FlatAppearance.BorderSize = 0;
            btnThemeToggle.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnThemeToggle.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnThemeToggle.MouseEnter += (s, e) => btnThemeToggle.IconColor = UITheme.AccentYellow;
            btnThemeToggle.MouseLeave += (s, e) => btnThemeToggle.IconColor = UITheme.CurrentIcon;
            btnThemeToggle.Click += (s, e) => { UITheme.ToggleTheme(); ApplyTheme(); };

            Label lblUserName = new Label { Text = $"{_currentUser.FirstName} {_currentUser.LastName}", Font = new Font("Segoe UI", 10F, FontStyle.Bold), AutoSize = true, Location = new Point(310, 15), TextAlign = ContentAlignment.MiddleRight };
            Label lblUserRole = new Label { Text = _currentUser.Role, Font = new Font("Segoe UI", 8F), AutoSize = true, Location = new Point(310, 35), TextAlign = ContentAlignment.MiddleRight };

            avatarIcon = new IconPictureBox { IconChar = IconChar.UserCircle, IconColor = UITheme.AccentYellow, IconSize = 40, Size = new Size(40, 40), Location = new Point(440, 15), BackColor = Color.Transparent };

            _dynamicTexts.AddRange(new[] { lblPageTitle, lblUserName });
            _mutedTexts.AddRange(new[] { lblClock, lblUserRole });

            pnlHeaderRight.Controls.AddRange(new Control[] { lblClock, btnThemeToggle, lblUserName, lblUserRole, avatarIcon });

            pnlHeader.Paint += (s, e) => { e.Graphics.DrawLine(new Pen(UITheme.CurrentBorder, 1), 0, 69, pnlHeader.Width, 69); };
            pnlHeader.Controls.AddRange(new Control[] { btnHamburger, lblPageTitle, pnlHeaderRight });

            // --- WORKSPACE (Center) ---
            pnlWorkspace = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(40) };

            lblWelcome = new Label { Text = $"Welcome Back, {_currentUser.FirstName}!", Font = new Font("Segoe UI", 22F, FontStyle.Bold), AutoSize = true, Location = new Point(40, 40) };
            Label lblSubWelcome = new Label { Text = "Here's what is happening with SJ PC Store today.", Font = UITheme.MainFont, AutoSize = true, Location = new Point(42, 85) };

            _dynamicTexts.Add(lblWelcome);
            _mutedTexts.Add(lblSubWelcome);

            Panel pnlSalesSection = CreateModuleSection("SALES OVERVIEW", 40, 130, out Panel salesGrid);
            salesGrid.Controls.Add(CreateStatCard("Today's Revenue", "₱ 0.00", "+0% from yesterday", IconChar.Wallet, true, 0, 0));
            salesGrid.Controls.Add(CreateStatCard("Transactions Today", "0", "All successful", IconChar.Receipt, true, 340, 0));

            Panel pnlLogisticsSection = CreateModuleSection("LOGISTICS & INVENTORY OVERVIEW", 40, 330, out Panel logGrid);
            logGrid.Controls.Add(CreateStatCard("Total Stock Value", "₱ 0.00", "Current valuation", IconChar.Boxes, false, 0, 0));
            logGrid.Controls.Add(CreateStatCard("Low Stock Alerts", "0 Items", "Needs immediate restocking", IconChar.ExclamationTriangle, false, 340, 0, true));
            logGrid.Controls.Add(CreateStatCard("Pending Procurements", "0 Batches", "Arriving this week", IconChar.Truck, false, 680, 0));

            _adminOnlyControls.Add(pnlLogisticsSection);

            pnlWorkspace.Controls.AddRange(new Control[] { lblWelcome, lblSubWelcome, pnlSalesSection, pnlLogisticsSection });

            foreach (var section in _moduleSections)
            {
                section.Width = pnlWorkspace.ClientSize.Width - 80;
            }

            pnlWorkspace.Resize += (s, e) =>
            {
                // SAFETY: Suspend layout to prevent flickering during rapid resize
                pnlWorkspace.SuspendLayout();
                foreach (var section in _moduleSections)
                {
                    // Check prevents crash when window is minimized to taskbar
                    if (pnlWorkspace.ClientSize.Width > 100)
                    {
                        section.Width = pnlWorkspace.ClientSize.Width - 80;
                    }
                }
                pnlWorkspace.ResumeLayout();
            };

            this.Controls.Add(pnlWorkspace);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlSidebar);
        }

        private IconButton CreateNavButton(string text, IconChar icon, bool isActive)
        {
            IconButton btn = new IconButton
            {
                Text = "   " + text,
                IconChar = icon,
                IconSize = 30,
                Size = new Size(260, 55),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(209, 213, 219),
                IconColor = Color.FromArgb(209, 213, 219),

                TextAlign = ContentAlignment.MiddleLeft,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Padding = new Padding(10, 0, 0, 0),

                Font = new Font("Segoe UI", 11.5F, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Tag = "   " + text
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 255, 255, 255);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(25, 255, 255, 255);

            if (isActive)
            {
                btn.ForeColor = UITheme.AccentYellow;
                btn.IconColor = UITheme.AccentYellow;
                btn.BackColor = Color.FromArgb(25, 255, 255, 255);

                btn.Paint += (s, e) =>
                {
                    if (pnlSidebar.Width > 70)
                    {
                        e.Graphics.FillRectangle(new SolidBrush(UITheme.AccentYellow), 0, 0, 4, 55);
                    }
                };
            }

            _navButtons.Add(btn);
            return btn;
        }

        private Panel CreateModuleSection(string title, int x, int y, out Panel grid)
        {
            Panel pnl = new Panel { Location = new Point(x, y), Size = new Size(1000, 180) };
            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 10F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 0) };

            pnl.Paint += (s, e) => { e.Graphics.DrawLine(new Pen(UITheme.CurrentBorder, 1), 0, 25, pnl.Width, 25); };
            pnl.Resize += (s, e) => pnl.Invalidate();

            grid = new Panel
            {
                Location = new Point(0, 40),
                Size = new Size(pnl.Width, 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(grid);

            _mutedTexts.Add(lblTitle);
            _moduleSections.Add(pnl);
            return pnl;
        }

        private Panel CreateStatCard(string title, string value, string desc, IconChar icon, bool isPositive, int x, int y, bool isAlert = false)
        {
            Panel card = new Panel { Location = new Point(x, y), Size = new Size(320, 130), BackColor = Color.Transparent };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(UITheme.CurrentWorkspace);

                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                int radius = 15;
                GraphicsPath path = new GraphicsPath();
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();

                e.Graphics.FillPath(new SolidBrush(UITheme.CurrentPanel), path);
                e.Graphics.DrawPath(new Pen(UITheme.CurrentBorder, 1), path);
            };

            Label lblTitle = new Label { Text = title, Font = UITheme.MainFont, Location = new Point(20, 20), AutoSize = true, BackColor = Color.Transparent };
            IconPictureBox icn = new IconPictureBox { IconChar = icon, IconColor = isAlert ? Color.FromArgb(239, 68, 68) : UITheme.AccentYellow, IconSize = 28, Size = new Size(28, 28), Location = new Point(270, 20), BackColor = Color.Transparent };
            Label lblValue = new Label { Text = value, Font = new Font("Segoe UI", 24F, FontStyle.Bold), Location = new Point(18, 45), AutoSize = true, BackColor = Color.Transparent };
            Label lblDesc = new Label { Text = desc, Font = new Font("Segoe UI", 9F), Location = new Point(22, 95), AutoSize = true, ForeColor = isAlert ? Color.FromArgb(239, 68, 68) : (isPositive ? Color.FromArgb(16, 185, 129) : UITheme.MutedText), BackColor = Color.Transparent };

            card.Controls.AddRange(new Control[] { lblTitle, icn, lblValue, lblDesc });

            _roundedCards.Add(card);
            _mutedTexts.Add(lblTitle);
            _dynamicTexts.Add(lblValue);
            if (!isPositive && !isAlert) _mutedTexts.Add(lblDesc);

            return card;
        }

        private void BtnHamburger_Click(object sender, EventArgs e)
        {
            // BUG FIX: Turn off AutoScroll temporarily to prevent WinForms from flashing a horizontal scrollbar!
            pnlWorkspace.AutoScroll = false;

            bool isExpanding = pnlSidebar.Width == 70;
            pnlSidebar.Width = isExpanding ? 260 : 70;
            lblBrandText.Visible = isExpanding;

            logoIcon.Location = new Point(isExpanding ? 20 : 15, 19);

            foreach (var btn in _navButtons)
            {
                if (isExpanding)
                {
                    btn.Text = btn.Tag.ToString();
                }
                else
                {
                    btn.Text = "";
                }
                btn.Invalidate();
            }

            // Immediately force the width update before turning AutoScroll back on
            foreach (var section in _moduleSections)
            {
                section.Width = pnlWorkspace.ClientSize.Width - 80;
            }

            // Turn AutoScroll back on safely
            pnlWorkspace.AutoScroll = true;
        }

        private void StartClock()
        {
            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => { lblClock.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy 'at' hh:mm:ss tt"); };
            _clockTimer.Start();
        }

        private void ApplyRBAC()
        {
            if (_currentUser.Role == "Cashier")
            {
                foreach (Control ctrl in _adminOnlyControls)
                {
                    ctrl.Visible = false;
                }
            }
        }

        private void ApplyTheme()
        {
            pnlWorkspace.BackColor = UITheme.CurrentWorkspace;
            pnlHeader.BackColor = UITheme.CurrentPanel;
            pnlSidebar.BackColor = UITheme.CurrentSidebarBg;
            btnThemeToggle.IconChar = UITheme.IsDarkMode ? IconChar.Sun : IconChar.Moon;

            btnHamburger.IconColor = UITheme.CurrentIcon;
            btnThemeToggle.IconColor = UITheme.CurrentIcon;

            foreach (var lbl in _dynamicTexts) lbl.ForeColor = UITheme.CurrentText;
            foreach (var lbl in _mutedTexts) lbl.ForeColor = UITheme.MutedText;
            foreach (var card in _roundedCards) card.Invalidate();
            pnlHeader.Invalidate();
        }
    }
}