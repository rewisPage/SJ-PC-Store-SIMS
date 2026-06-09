using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System.Drawing.Drawing2D;
using Panel = System.Windows.Forms.Panel;

namespace SJ_PC_Store_SIMS.Views
{
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }
    }

    public partial class DashboardForm : Form
    {
        private UserModel _currentUser;
        private DashboardController _dashboardController;
        private System.Windows.Forms.Timer _clockTimer;

        // Flags to prevent notification spam during Reflection updates
        private bool _isFirstLoad = true;
        private int _lastLowStockCount = -1;

        // Main Layout Panels
        private BufferedPanel pnlSidebar;
        private BufferedPanel pnlHeader;
        private Panel pnlWorkspace;
        private Panel pnlDashboardContainer;

        // Notification UI
        private BufferedPanel pnlNotifDropdown;
        private FlowLayoutPanel flpNotifications;
        private BufferedPanel pnlBadge;
        private Label lblClearNotifs;

        // Interactive Elements
        private IconButton btnHamburger;
        private IconButton btnThemeToggle;
        private IconButton btnNotifications;
        private Label lblClock;
        private Label lblWelcome;
        private Label lblPageTitle;
        private IconPictureBox logoIcon;
        private Label lblBrandText;
        private IconPictureBox avatarIcon;

        // Collections for dynamic updates
        private List<Panel> _roundedCards = new List<Panel>();
        private List<Label> _dynamicTexts = new List<Label>();
        private List<Label> _mutedTexts = new List<Label>();
        private List<IconButton> _navButtons = new List<IconButton>();
        private List<Panel> _moduleSections = new List<Panel>();

        // RBAC Dynamic Controls
        private IconButton btnDash, btnPOS, btnInv, btnProc, btnData, btnReports, btnUsers, btnProfile;
        private Panel pnlSalesSection, pnlInvSection, pnlProcSection, pnlDataSection, pnlUserSection;

        public DashboardForm(UserModel user)
        {
            _currentUser = user;
            _dashboardController = new DashboardController();

            this.DoubleBuffered = true;

            InitializeProgrammaticUI();

            PopulateUserNotifications();
            ApplyTheme();
            ApplyRBAC();
            StartClock();

            LoadDashboardData();
        }

        private void InitializeProgrammaticUI()
        {
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Text = "SJ PC Store - Master Dashboard";

            // --- SIDEBAR (Left) --- 
            pnlSidebar = new BufferedPanel { Dock = DockStyle.Left, Width = 260 };

            BufferedPanel pnlBrand = new BufferedPanel { Dock = DockStyle.Top, Height = 70, BackColor = Color.Transparent };
            logoIcon = new IconPictureBox { IconChar = IconChar.Microchip, IconColor = UITheme.AccentYellow, IconSize = 36, Size = new Size(36, 36), Location = new Point(12, 17), BackColor = Color.Transparent };
            lblBrandText = new Label { Text = "SJ PC Store", Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = UITheme.AccentYellow, AutoSize = true, Location = new Point(55, 20), BackColor = Color.Transparent };

            pnlBrand.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(25, 255, 255, 255), 1)) { e.Graphics.DrawLine(pen, 0, 69, pnlBrand.Width, 69); }
            };
            pnlBrand.Controls.AddRange(new Control[] { logoIcon, lblBrandText });

            FlowLayoutPanel flpNav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 20, 0, 0), BackColor = Color.Transparent };

            btnDash = CreateNavButton("Dashboard", IconChar.ChartPie, true);
            btnPOS = CreateNavButton("Sales POS", IconChar.ShoppingCart, false);
            btnInv = CreateNavButton("Inventory", IconChar.Boxes, false);
            btnProc = CreateNavButton("Procurement", IconChar.TruckLoading, false);
            btnData = CreateNavButton("Data Management", IconChar.Database, false);
            btnReports = CreateNavButton("Report Center", IconChar.ChartLine, false);
            btnUsers = CreateNavButton("User Management", IconChar.Users, false);
            btnProfile = CreateNavButton("My Profile", IconChar.UserGear, false);

            btnDash.Click += (s, e) => { lblPageTitle.Text = "Master Dashboard"; ShowDashboard(); SetActiveNavButton(btnDash); };
            btnInv.Click += (s, e) => { lblPageTitle.Text = "Inventory Management"; LoadUserControl(new InventoryView(_currentUser.UserID)); SetActiveNavButton(btnInv); };
            btnData.Click += (s, e) => { lblPageTitle.Text = "Data Management (Suppliers)"; LoadUserControl(new DataManagementView(_currentUser.UserID)); SetActiveNavButton(btnData); };
            btnProc.Click += (s, e) => { lblPageTitle.Text = "Procurement Management"; LoadUserControl(new ProcurementView(_currentUser.UserID)); SetActiveNavButton(btnProc); };
            btnPOS.Click += (s, e) => { lblPageTitle.Text = "Sales Management"; LoadUserControl(new SalesView(_currentUser.UserID)); SetActiveNavButton(btnPOS); };
            btnReports.Click += (s, e) => { lblPageTitle.Text = "Report Center"; LoadUserControl(new ReportView(_currentUser.UserID)); SetActiveNavButton(btnReports); };
            btnUsers.Click += (s, e) => { lblPageTitle.Text = "User Management"; LoadUserControl(new UserManagementView(_currentUser.UserID)); SetActiveNavButton(btnUsers); };
            btnProfile.Click += (s, e) => { lblPageTitle.Text = "My Profile"; LoadUserControl(new ProfileView(_currentUser.UserID)); SetActiveNavButton(btnProfile); };

            // Add them to the FlowLayoutPanel (Gaps will automatically close when a button is hidden)
            flpNav.Controls.AddRange(new Control[] { btnDash, btnPOS, btnInv, btnProc, btnData, btnReports, btnUsers, btnProfile });

            BufferedPanel pnlFooter = new BufferedPanel { Dock = DockStyle.Bottom, Height = 70, BackColor = Color.Transparent };
            pnlFooter.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(25, 255, 255, 255), 1)) { e.Graphics.DrawLine(pen, 0, 0, pnlFooter.Width, 0); }
            };

            IconButton btnLogout = CreateNavButton("Logout", IconChar.SignOutAlt, false);
            btnLogout.IconColor = Color.FromArgb(255, 107, 107);
            btnLogout.ForeColor = Color.FromArgb(255, 107, 107);
            btnLogout.Click += (s, e) => { this.Close(); };
            pnlFooter.Controls.Add(btnLogout);

            pnlSidebar.Controls.Add(flpNav);
            pnlSidebar.Controls.Add(pnlFooter);
            pnlSidebar.Controls.Add(pnlBrand);

            // --- HEADER (Top) ---
            pnlHeader = new BufferedPanel { Dock = DockStyle.Top, Height = 70 };

            btnHamburger = new IconButton { IconChar = IconChar.Bars, IconFont = FontAwesome.Sharp.IconFont.Solid, IconSize = 24, Size = new Size(40, 40), Location = new Point(20, 15), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnHamburger.FlatAppearance.BorderSize = 0;
            btnHamburger.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnHamburger.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnHamburger.MouseEnter += (s, e) => btnHamburger.IconColor = UITheme.AccentYellow;
            btnHamburger.MouseLeave += (s, e) => btnHamburger.IconColor = UITheme.CurrentIcon;
            btnHamburger.Click += BtnHamburger_Click;

            lblPageTitle = new Label { Text = "Master Dashboard", Font = new Font("Segoe UI", 16F, FontStyle.Bold), AutoSize = true, Location = new Point(70, 20) };

            BufferedPanel pnlHeaderRight = new BufferedPanel { Size = new Size(650, 70), Dock = DockStyle.Right, BackColor = Color.Transparent };
            lblClock = new Label { Text = "Loading time...", Font = UITheme.MainFont, AutoSize = true, Location = new Point(10, 25) };

            btnNotifications = new IconButton { IconChar = IconChar.Bell, IconFont = FontAwesome.Sharp.IconFont.Solid, IconSize = 22, Size = new Size(40, 40), Location = new Point(280, 15), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnNotifications.FlatAppearance.BorderSize = 0;
            btnNotifications.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnNotifications.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnNotifications.MouseEnter += (s, e) => btnNotifications.IconColor = UITheme.AccentYellow;
            btnNotifications.MouseLeave += (s, e) => btnNotifications.IconColor = UITheme.CurrentIcon;
            btnNotifications.Click += (s, e) =>
            {
                pnlNotifDropdown.Visible = !pnlNotifDropdown.Visible;
                if (pnlNotifDropdown.Visible)
                {
                    pnlNotifDropdown.BringToFront();
                    pnlBadge.Visible = false;
                    pnlNotifDropdown.Invalidate(); // Ensures crisp redraw over overlapping controls
                }
            };

            pnlBadge = new BufferedPanel { Size = new Size(12, 12), Location = new Point(306, 14), BackColor = Color.Transparent, Visible = false };
            pnlBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(Color.FromArgb(239, 68, 68))) { e.Graphics.FillEllipse(brush, 1, 1, 10, 10); }
            };

            btnThemeToggle = new IconButton { IconChar = IconChar.Moon, IconFont = FontAwesome.Sharp.IconFont.Solid, IconSize = 24, Size = new Size(40, 40), Location = new Point(330, 15), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnThemeToggle.FlatAppearance.BorderSize = 0;
            btnThemeToggle.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnThemeToggle.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnThemeToggle.MouseEnter += (s, e) => btnThemeToggle.IconColor = UITheme.AccentYellow;
            btnThemeToggle.MouseLeave += (s, e) => btnThemeToggle.IconColor = UITheme.CurrentIcon;
            btnThemeToggle.Click += (s, e) => { UITheme.ToggleTheme(); ApplyTheme(); };

            Label lblUserName = new Label { Text = $"{_currentUser.FirstName} {_currentUser.LastName}", Font = new Font("Segoe UI", 10F, FontStyle.Bold), AutoSize = false, Size = new Size(200, 20), Location = new Point(380, 15), TextAlign = ContentAlignment.MiddleRight };
            Label lblUserRole = new Label { Text = _currentUser.Role, Font = new Font("Segoe UI", 8F), AutoSize = false, Size = new Size(200, 20), Location = new Point(380, 35), TextAlign = ContentAlignment.MiddleRight };

            avatarIcon = new IconPictureBox { IconChar = IconChar.UserCircle, IconColor = UITheme.AccentYellow, IconSize = 40, Size = new Size(40, 40), Location = new Point(590, 15), BackColor = Color.Transparent };

            _dynamicTexts.AddRange(new[] { lblPageTitle, lblUserName });
            _mutedTexts.AddRange(new[] { lblClock, lblUserRole });

            pnlHeaderRight.Controls.AddRange(new Control[] { pnlBadge, btnNotifications, lblClock, btnThemeToggle, lblUserName, lblUserRole, avatarIcon });

            pnlHeader.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(pen, 0, 69, pnlHeader.Width, 69); }
            };
            pnlHeader.Controls.AddRange(new Control[] { btnHamburger, lblPageTitle, pnlHeaderRight });

            // --- NATIVE WORKSPACE LAYOUT ENGINE ---
            pnlWorkspace = new Panel { Dock = DockStyle.Fill };
            pnlDashboardContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(40, 20, 40, 40) };

            pnlSalesSection = CreateModuleSection("SALES OVERVIEW", out Panel salesGrid);
            salesGrid.Controls.Add(CreateStatCard("Today's Revenue", "₱ 0.00", "+0% from yesterday", IconChar.Wallet, true, 0, 0));
            salesGrid.Controls.Add(CreateStatCard("Transactions Today", "0", "All successful", IconChar.Receipt, true, 340, 0));

            pnlInvSection = CreateModuleSection("INVENTORY OVERVIEW", out Panel invGrid);
            invGrid.Controls.Add(CreateStatCard("Total Stock Value", "₱ 0.00", "Current valuation", IconChar.Boxes, false, 0, 0));
            invGrid.Controls.Add(CreateStatCard("Low Stock Alerts", "0 Items", "Needs immediate restocking", IconChar.ExclamationTriangle, false, 340, 0, true));
            invGrid.Controls.Add(CreateStatCard("Registered Products", "0", "Total items in database", IconChar.BoxOpen, false, 680, 0));

            pnlProcSection = CreateModuleSection("PROCUREMENT OVERVIEW", out Panel procGrid);
            procGrid.Controls.Add(CreateStatCard("Pending Procurements", "0 Batches", "Arriving this week", IconChar.Truck, false, 0, 0));
            procGrid.Controls.Add(CreateStatCard("Total Purchase Orders", "0", "Lifetime POs logged", IconChar.FileInvoice, false, 340, 0));

            pnlDataSection = CreateModuleSection("DATA MANAGEMENT OVERVIEW", out Panel dataGrid);
            dataGrid.Controls.Add(CreateStatCard("Registered Suppliers", "0", "Active business partners", IconChar.Handshake, false, 0, 0));

            pnlUserSection = CreateModuleSection("USER MANAGEMENT OVERVIEW", out Panel userGrid);
            userGrid.Controls.Add(CreateStatCard("Active Users", "0", "System Admins & Cashiers", IconChar.Users, false, 0, 0));

            // Add them to the Workspace Container (Top Docking will automatically shift panels up when one is hidden)
            pnlDashboardContainer.Controls.Add(pnlUserSection);
            pnlDashboardContainer.Controls.Add(pnlDataSection);
            pnlDashboardContainer.Controls.Add(pnlProcSection);
            pnlDashboardContainer.Controls.Add(pnlInvSection);
            pnlDashboardContainer.Controls.Add(pnlSalesSection);

            Panel pnlWelcomeWrapper = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.Transparent };
            lblWelcome = new Label { Text = $"Welcome Back, {_currentUser.FirstName}!", Font = new Font("Segoe UI", 22F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 0) };
            Label lblSubWelcome = new Label { Text = "Here's what is happening with SJ PC Store today.", Font = UITheme.MainFont, AutoSize = true, Location = new Point(2, 45) };
            pnlWelcomeWrapper.Controls.AddRange(new Control[] { lblWelcome, lblSubWelcome });
            _dynamicTexts.Add(lblWelcome);
            _mutedTexts.Add(lblSubWelcome);

            pnlDashboardContainer.Controls.Add(pnlUserSection);
            pnlDashboardContainer.Controls.Add(pnlDataSection);
            pnlDashboardContainer.Controls.Add(pnlProcSection);
            pnlDashboardContainer.Controls.Add(pnlInvSection);
            pnlDashboardContainer.Controls.Add(pnlSalesSection);
            pnlDashboardContainer.Controls.Add(pnlWelcomeWrapper);

            pnlWorkspace.Controls.Add(pnlDashboardContainer);

            pnlWorkspace.Resize += (s, e) => { if (pnlNotifDropdown.Visible) pnlNotifDropdown.Visible = false; };

            // ========================================================
            // REFINED NOTIFICATION DROPDOWN (Bug Fix Applied)
            // ========================================================
            pnlNotifDropdown = new BufferedPanel
            {
                Size = new Size(450, 500),
                Visible = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(this.ClientSize.Width - 480, 80),
                BackColor = UITheme.CurrentPanel
            };

            // FIX: Set Region ONCE during initialization to prevent infinite layout invalidation loops
            using (GraphicsPath path = new GraphicsPath())
            {
                int radius = 15;
                Rectangle rect = new Rectangle(0, 0, pnlNotifDropdown.Width, pnlNotifDropdown.Height);
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure();
                pnlNotifDropdown.Region = new Region(path);
            }

            pnlNotifDropdown.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Draw the border without altering the Region property
                using (GraphicsPath path = new GraphicsPath())
                {
                    int radius = 15;
                    Rectangle rect = new Rectangle(0, 0, pnlNotifDropdown.Width - 1, pnlNotifDropdown.Height - 1);
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    using (Pen pen = new Pen(UITheme.CurrentBorder, 3))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            };

            Label lblNotifTitle = new Label { Text = "Notifications", Font = new Font("Segoe UI", 12F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 20) };
            _dynamicTexts.Add(lblNotifTitle);
            pnlNotifDropdown.Controls.Add(lblNotifTitle);

            lblClearNotifs = new Label { Text = "Clear All", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), AutoSize = true, Location = new Point(360, 22), Cursor = Cursors.Hand };
            lblClearNotifs.Click += (s, e) => { flpNotifications.Controls.Clear(); pnlNotifDropdown.Visible = false; };
            pnlNotifDropdown.Controls.Add(lblClearNotifs);

            Panel pnlNotifClip = new Panel { Location = new Point(5, 55), Size = new Size(440, 435), BackColor = Color.Transparent };
            flpNotifications = new FlowLayoutPanel { Size = new Size(465, 435), AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0) };
            pnlNotifClip.Controls.Add(flpNotifications);

            pnlNotifDropdown.Controls.Add(pnlNotifClip);

            this.Controls.Add(pnlNotifDropdown);
            this.Controls.Add(pnlWorkspace);
            this.Controls.Add(pnlHeader);
            this.Controls.Add(pnlSidebar);
        }

        // =========================================================================
        // DYNAMIC NOTIFICATION ENGINE LOGIC
        // =========================================================================
        private string GetRelativeTime(DateTime logDate)
        {
            TimeSpan ts = DateTime.Now - logDate;
            if (ts.TotalMinutes < 1) return "Just now";
            if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes} mins ago";
            if (ts.TotalHours < 24) return $"{(int)ts.TotalHours} hrs ago";
            if (ts.TotalDays < 7) return $"{(int)ts.TotalDays} days ago";
            return logDate.ToString("MMM dd, yyyy");
        }

        private (IconChar, Color) GetNotificationStyle(string category)
        {
            switch (category)
            {
                case "Sales": return (IconChar.ShoppingCart, Color.FromArgb(16, 185, 129));
                case "Inventory": return (IconChar.Boxes, Color.FromArgb(245, 158, 11));
                case "Procurement": return (IconChar.TruckLoading, Color.FromArgb(59, 130, 246));
                case "User Management": return (IconChar.UserShield, UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark);
                case "Data Management": return (IconChar.Database, Color.FromArgb(139, 92, 246));
                default: return (IconChar.InfoCircle, UITheme.MutedText);
            }
        }

        private void PopulateUserNotifications()
        {
            flpNotifications.Controls.Clear();

            var recentLogs = _dashboardController.GetUserRecentActivity(_currentUser.UserID, 15);

            if (recentLogs == null || recentLogs.Count == 0)
            {
                AddDynamicNotification("System Message", "No recent activity found.", IconChar.BellSlash, UITheme.MutedText, DateTime.Now.ToString("MMM d, h:mm tt"));
                pnlBadge.Visible = false; // Turn off red dot if there's no real activity
                return;
            }

            // Iterate backwards so the absolute newest item is pushed to the top last
            for (int i = recentLogs.Count - 1; i >= 0; i--)
            {
                var log = recentLogs[i];
                var style = GetNotificationStyle(log.ModuleCategory);
                string timeStr = GetRelativeTime(log.LogDate);

                // Routes to the NEW 5-parameter method we just created!
                AddDynamicNotification(log.ModuleCategory, log.ActionDescription, style.Item1, style.Item2, timeStr);
            }
        }



        // ========================================================
        // WIRING & NAVIGATION LOGIC
        // ========================================================
        private void LoadUserControl(UserControl uc)
        {
            pnlDashboardContainer.Visible = false;
            for (int i = pnlWorkspace.Controls.Count - 1; i >= 0; i--)
            {
                if (pnlWorkspace.Controls[i] is UserControl oldUc) { pnlWorkspace.Controls.Remove(oldUc); oldUc.Dispose(); }
            }
            uc.Dock = DockStyle.Fill;
            pnlWorkspace.Controls.Add(uc);
            uc.BringToFront();
        }

        private void ShowDashboard()
        {
            for (int i = pnlWorkspace.Controls.Count - 1; i >= 0; i--)
            {
                if (pnlWorkspace.Controls[i] is UserControl oldUc) { pnlWorkspace.Controls.Remove(oldUc); oldUc.Dispose(); }
            }
            pnlDashboardContainer.Visible = true;
            pnlDashboardContainer.BringToFront();
        }

        private void SetActiveNavButton(IconButton activeBtn)
        {
            foreach (var btn in _navButtons)
            {
                btn.ForeColor = Color.FromArgb(209, 213, 219);
                btn.IconColor = Color.FromArgb(209, 213, 219);
                btn.BackColor = Color.Transparent;
            }
            activeBtn.ForeColor = UITheme.AccentYellow;
            activeBtn.IconColor = UITheme.AccentYellow;
            activeBtn.BackColor = Color.FromArgb(25, 255, 255, 255);
        }

        private void BtnHamburger_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();

            bool isExpanding = pnlSidebar.Width == 70;
            pnlSidebar.Width = isExpanding ? 260 : 70;

            lblBrandText.Visible = isExpanding;
            logoIcon.Location = new Point(isExpanding ? 20 : 15, 19);

            foreach (var btn in _navButtons) btn.Text = isExpanding ? btn.Tag.ToString() : "";

            this.ResumeLayout(true);
        }

        private void LoadDashboardData()
        {
            try
            {
                DashboardStatsModel stats = _dashboardController.GetDashboardStatistics();

                if (_roundedCards.Count >= 9)
                {
                    ((Label)_roundedCards[0].Controls[2]).Text = $"₱ {stats.TodaysRevenue:N2}";
                    ((Label)_roundedCards[1].Controls[2]).Text = stats.TransactionsToday.ToString();
                    ((Label)_roundedCards[2].Controls[2]).Text = $"₱ {stats.TotalStockValue:N2}";
                    ((Label)_roundedCards[3].Controls[2]).Text = $"{stats.LowStockAlerts} Items";
                    ((Label)_roundedCards[4].Controls[2]).Text = stats.TotalProducts.ToString();
                    ((Label)_roundedCards[5].Controls[2]).Text = $"{stats.PendingProcurements} Batches";
                    ((Label)_roundedCards[6].Controls[2]).Text = stats.TotalPurchaseOrders.ToString();
                    ((Label)_roundedCards[7].Controls[2]).Text = stats.TotalSuppliers.ToString();
                    ((Label)_roundedCards[8].Controls[2]).Text = stats.TotalActiveUsers.ToString();

                    if (_isFirstLoad)
                    {
                        AddNotification("System Login", "System synchronized with database.", true);
                        _isFirstLoad = false;
                    }

                    // ==========================================================
                    // RBAC FILTER: Only trigger if the user has Inventory rights
                    // ==========================================================
                    if (_currentUser != null && _currentUser.Permissions != null && _currentUser.Permissions.CanManageInventory)
                    {
                        if (stats.LowStockAlerts > 0 && stats.LowStockAlerts != _lastLowStockCount)
                        {
                            AddNotification("Low Stock Alert", $"You have {stats.LowStockAlerts} blueprints running out of stock.", false);
                            _lastLowStockCount = stats.LowStockAlerts;
                        }
                        else if (stats.LowStockAlerts == 0)
                        {
                            _lastLowStockCount = 0;
                        }
                    }
                }
            }
            catch (Exception)
            {
                AddNotification("Database Error", "Failed to load live statistics.", false);
            }
        }

        // =========================================================================
        // NOTIFICATION UI METHODS
        // =========================================================================

        // 1. Backward compatibility for existing system alerts (Login, Low Stock, etc.)
        public void AddNotification(string title, string message, bool isSuccess)
        {
            IconChar iconChar = isSuccess ? IconChar.CheckCircle : IconChar.ExclamationCircle;
            Color iconColor = isSuccess ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
            string timeStr = DateTime.Now.ToString("MMM d, h:mm tt");

            AddDynamicNotification(title, message, iconChar, iconColor, timeStr);
        }

        // 2. The core dynamic engine for Activity Logs
        public void AddDynamicNotification(string title, string message, IconChar iconChar, Color iconColor, string timeStr)
        {
            BufferedPanel pnlItem = new BufferedPanel { Size = new Size(410, 95), Margin = new Padding(15, 5, 10, 5) };

            pnlItem.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(pen, 5, 94, pnlItem.Width - 5, 94); }
            };

            IconPictureBox icon = new IconPictureBox { IconChar = iconChar, IconColor = iconColor, IconSize = 26, Size = new Size(26, 26), Location = new Point(20, 20), BackColor = Color.Transparent };
            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Location = new Point(60, 16), AutoSize = true };

            // Handle long messages by truncating to avoid text overlapping the panel
            string displayMsg = message.Length > 45 ? message.Substring(0, 42) + "..." : message;
            Label lblMsg = new Label { Text = displayMsg, Font = new Font("Segoe UI", 9F), Location = new Point(60, 40), AutoSize = true };

            Label lblTime = new Label { Text = timeStr, Font = new Font("Segoe UI", 8F), Location = new Point(60, 65), AutoSize = true };

            _dynamicTexts.Add(lblTitle);
            _dynamicTexts.Add(lblMsg);
            _mutedTexts.Add(lblTime);

            pnlItem.Controls.AddRange(new Control[] { icon, lblTitle, lblMsg, lblTime });
            flpNotifications.Controls.Add(pnlItem);
            flpNotifications.Controls.SetChildIndex(pnlItem, 0); // Always puts newest at the top

            pnlBadge.Visible = true;
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
            }

            _navButtons.Add(btn);
            return btn;
        }

        private Panel CreateModuleSection(string title, out Panel grid)
        {
            BufferedPanel pnl = new BufferedPanel { Dock = DockStyle.Top, Height = 190, Padding = new Padding(0, 0, 0, 20) };
            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 10F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 0) };

            pnl.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(pen, 0, 25, pnl.Width, 25); }
            };

            BufferedPanel innerGrid = new BufferedPanel
            {
                Location = new Point(0, 40),
                Size = new Size(pnl.Width, 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            grid = innerGrid;

            pnl.Controls.Add(lblTitle);
            pnl.Controls.Add(innerGrid);

            _mutedTexts.Add(lblTitle);
            _moduleSections.Add(pnl);
            return pnl;
        }

        private Panel CreateStatCard(string title, string value, string desc, IconChar icon, bool isPositive, int x, int y, bool isAlert = false)
        {
            BufferedPanel card = new BufferedPanel { Location = new Point(x, y), Size = new Size(320, 130), BackColor = Color.Transparent };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.Clear(UITheme.CurrentWorkspace);

                using (GraphicsPath path = new GraphicsPath())
                {
                    int radius = 15;
                    Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                    path.CloseFigure();

                    using (SolidBrush brush = new SolidBrush(UITheme.CurrentPanel))
                    using (Pen pen = new Pen(UITheme.CurrentBorder, 1))
                    {
                        e.Graphics.FillPath(brush, path);
                        e.Graphics.DrawPath(pen, path);
                    }
                }
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

        private void StartClock()
        {
            _clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _clockTimer.Tick += (s, e) => { lblClock.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy 'at' hh:mm:ss tt"); };
            _clockTimer.Start();
        }

        private void ApplyRBAC()
        {
            if (_currentUser?.Permissions == null) return;

            // 1. Toggle Sidebar Navigation Buttons
            btnUsers.Visible = _currentUser.Permissions.CanManageUsers;
            btnInv.Visible = _currentUser.Permissions.CanManageInventory;
            btnPOS.Visible = _currentUser.Permissions.CanProcessSales;
            btnProc.Visible = _currentUser.Permissions.CanManageProcurement;
            btnReports.Visible = _currentUser.Permissions.CanViewReports;
            btnData.Visible = _currentUser.Permissions.CanManageData;
            btnUsers.Visible = _currentUser.Permissions.CanManageUsers;

            // 2. Toggle Dashboard Overview Sections
            pnlUserSection.Visible = _currentUser.Permissions.CanManageUsers;
            pnlInvSection.Visible = _currentUser.Permissions.CanManageInventory;
            pnlSalesSection.Visible = _currentUser.Permissions.CanProcessSales;
            pnlProcSection.Visible = _currentUser.Permissions.CanManageProcurement;
            pnlDataSection.Visible = _currentUser.Permissions.CanManageData;
        }

        private void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace;

            pnlWorkspace.BackColor = UITheme.CurrentWorkspace;
            pnlHeader.BackColor = UITheme.CurrentPanel;
            pnlSidebar.BackColor = UITheme.CurrentSidebarBg;
            pnlNotifDropdown.BackColor = UITheme.CurrentPanel;

            lblClearNotifs.ForeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.SecondaryDark;

            btnThemeToggle.IconChar = UITheme.IsDarkMode ? IconChar.Sun : IconChar.Moon;
            btnHamburger.IconColor = UITheme.CurrentIcon;
            btnThemeToggle.IconColor = UITheme.CurrentIcon;
            btnNotifications.IconColor = UITheme.CurrentIcon;

            foreach (var lbl in _dynamicTexts) lbl.ForeColor = UITheme.CurrentText;
            foreach (var lbl in _mutedTexts) lbl.ForeColor = UITheme.MutedText;

            foreach (var card in _roundedCards) card.Invalidate();
            pnlHeader.Invalidate();
            pnlNotifDropdown.Invalidate();

            foreach (Control item in flpNotifications.Controls) item.Invalidate();
        }
    }
}