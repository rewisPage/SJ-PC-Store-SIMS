using FontAwesome.Sharp;
using ScottPlot;
using ScottPlot.WinForms;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Color = System.Drawing.Color;
using Font = System.Drawing.Font;
using FontStyle = System.Drawing.FontStyle;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;


namespace SJ_PC_Store_SIMS.Views
{
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
            // Force hardware acceleration and prevent background smearing
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
        }

        // Prevent Windows from doing messy pixel-shifting when scrolling
        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            this.Invalidate(true);
        }
    }

    public class ThemedScrollBar : Control
    {
        private Panel _target;
        private int _thumbHeight = 50;
        private float _thumbY = 0;
        private bool _isDragging = false;
        private int _dragStartY = 0;
        private float _dragStartThumbY = 0;

        public ThemedScrollBar(Panel target)
        {
            _target = target;
            this.Width = 8; // Super thin modern width
            this.Cursor = Cursors.Hand;
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.BackColor = Color.Transparent;

            // Sync interactions to keep the thumb updated
            _target.MouseWheel += (s, e) => UpdateThumb();
            _target.Scroll += (s, e) => UpdateThumb();
            _target.Resize += (s, e) => UpdateThumb();
            _target.ControlAdded += (s, e) => UpdateThumb();
            _target.ControlRemoved += (s, e) => UpdateThumb();
        }

        public void UpdateThumb()
        {
            int maxScroll = _target.DisplayRectangle.Height - _target.ClientSize.Height;
            if (maxScroll <= 0)
            {
                this.Visible = false;
                return;
            }
            this.Visible = true;

            float visibleRatio = (float)_target.ClientSize.Height / _target.DisplayRectangle.Height;
            _thumbHeight = Math.Max(30, (int)(this.Height * visibleRatio));

            float scrollRatio = (float)Math.Abs(_target.AutoScrollPosition.Y) / maxScroll;
            _thumbY = scrollRatio * (this.Height - _thumbHeight);

            this.Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Y >= _thumbY && e.Y <= _thumbY + _thumbHeight)
            {
                _isDragging = true;
                _dragStartY = e.Y;
                _dragStartThumbY = _thumbY;
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isDragging)
            {
                float delta = e.Y - _dragStartY;
                float newThumbY = Math.Max(0, Math.Min(this.Height - _thumbHeight, _dragStartThumbY + delta));

                float scrollRatio = newThumbY / (this.Height - _thumbHeight);
                int maxScroll = _target.DisplayRectangle.Height - _target.ClientSize.Height;
                int newScrollValue = (int)(scrollRatio * maxScroll);

                _target.AutoScrollPosition = new Point(0, newScrollValue);
                _target.Invalidate(true); // <-- ADD THIS LINE to force a clean redraw
                UpdateThumb();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) => _isDragging = false;

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            // Sync with UITheme dynamically
            Color thumbColor = UITheme.IsDarkMode ? Color.FromArgb(80, 75, 85) : Color.FromArgb(190, 195, 200);

            // Add a subtle hover effect
            Point mousePos = this.PointToClient(System.Windows.Forms.Cursor.Position);
            bool isHovering = mousePos.X >= 0 && mousePos.X <= this.Width && mousePos.Y >= _thumbY && mousePos.Y <= _thumbY + _thumbHeight;
            if (isHovering || _isDragging) thumbColor = UITheme.MutedText;

            using (GraphicsPath path = new GraphicsPath())
            {
                int d = this.Width;
                path.AddArc(0, (int)_thumbY, d, d, 180, 180);
                path.AddArc(0, (int)_thumbY + _thumbHeight - d, d, d, 0, 180);
                path.CloseFigure();

                using (SolidBrush thumbBrush = new SolidBrush(thumbColor))
                {
                    e.Graphics.FillPath(thumbBrush, path);
                }
            }
        }

        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); this.Invalidate(); }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); this.Invalidate(); }
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
        private Panel pnlTopItems;
        private Panel pnlLowStockItems;

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

        private FormsPlot _salesBarChart;
        private FormsPlot _inventoryDonutChart;
        private FormsPlot _stockStatusPieChart;
        private FormsPlot _procurementBarChart;

        // Add these near your other Main Layout Panels declarations
        private Panel _loadingOverlay;
        private PictureBox _spinnerBox;

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        private const int WM_SETREDRAW = 11;

        // Add this near your other interactive element declarations
        private IconButton _currentActiveNavButton;

        private Panel pnlDashWrapper;
        private ThemedScrollBar _customScrollBar;

        // =========================================================================
        // HARDWARE ACCELERATION & ANTI-FLICKER ENGINE
        // =========================================================================
        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED - Forces double buffering on all child controls
                return cp;
            }
        }

        public DashboardForm(UserModel user)
        {
            _currentUser = user;
            _dashboardController = new DashboardController();

            this.DoubleBuffered = true;

            InitializeProgrammaticUI();
            InitializeCharts();

            PopulateUserNotifications();
            ApplyTheme();
            ApplyRBAC();
            StartClock();

            LoadDashboardData();
        }

        // Strict UITheme Color Palette Generator for ScottPlot
        private ScottPlot.Color[] GetStrictThemePalette()
        {
            return new[] {
                ScottPlot.Color.FromColor(UITheme.PrimaryDark),
                ScottPlot.Color.FromColor(UITheme.AccentYellow),
                ScottPlot.Color.FromColor(UITheme.SecondaryDark),
                ScottPlot.Color.FromColor(UITheme.CurrentBorder),
                ScottPlot.Color.FromColor(UITheme.MutedText)
            };
        }

        private void InitializeCharts()
        {
            _salesBarChart = new FormsPlot { Location = new Point(350, 0), Size = new Size(800, 280), BackColor = UITheme.CurrentWorkspace };
            ((Panel)pnlSalesSection.Controls[1]).Controls.Add(_salesBarChart);

            _inventoryDonutChart = new FormsPlot { Location = new Point(350, 0), Size = new Size(400, 420), BackColor = UITheme.CurrentWorkspace };
            ((Panel)pnlInvSection.Controls[1]).Controls.Add(_inventoryDonutChart);

            _stockStatusPieChart = new FormsPlot { Location = new Point(770, 0), Size = new Size(400, 420), BackColor = UITheme.CurrentWorkspace };
            ((Panel)pnlInvSection.Controls[1]).Controls.Add(_stockStatusPieChart);

            _procurementBarChart = new FormsPlot { Location = new Point(350, 0), Size = new Size(800, 280), BackColor = UITheme.CurrentWorkspace };
            ((Panel)pnlProcSection.Controls[1]).Controls.Add(_procurementBarChart);
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

            // Add this immediately below it:
            _currentActiveNavButton = btnDash;

            btnPOS = CreateNavButton("Sales POS", IconChar.ShoppingCart, false);
            btnInv = CreateNavButton("Inventory", IconChar.Boxes, false);
            btnProc = CreateNavButton("Procurement", IconChar.TruckLoading, false);
            btnData = CreateNavButton("Supplier Management", IconChar.Database, false);
            btnReports = CreateNavButton("Report Center", IconChar.ChartLine, false);
            btnUsers = CreateNavButton("User Management", IconChar.Users, false);
            btnProfile = CreateNavButton("My Profile", IconChar.UserGear, false);

            btnDash.Click += (s, e) => { if (_currentActiveNavButton == btnDash) return; lblPageTitle.Text = "Master Dashboard"; ShowDashboard(); SetActiveNavButton(btnDash); };
            btnPOS.Click += (s, e) => { if (_currentActiveNavButton == btnPOS) return; lblPageTitle.Text = "Sales Management"; LoadUserControl(new SalesView(_currentUser.UserID)); SetActiveNavButton(btnPOS); };
            btnInv.Click += (s, e) => { if (_currentActiveNavButton == btnInv) return; lblPageTitle.Text = "Inventory Management"; LoadUserControl(new InventoryView(_currentUser.UserID)); SetActiveNavButton(btnInv); };
            btnProc.Click += (s, e) => { if (_currentActiveNavButton == btnProc) return; lblPageTitle.Text = "Procurement Management"; LoadUserControl(new ProcurementView(_currentUser.UserID)); SetActiveNavButton(btnProc); };
            btnData.Click += (s, e) => { if (_currentActiveNavButton == btnData) return; lblPageTitle.Text = "Supplier Management"; LoadUserControl(new DataManagementView(_currentUser.UserID)); SetActiveNavButton(btnData); };
            btnReports.Click += (s, e) => { if (_currentActiveNavButton == btnReports) return; lblPageTitle.Text = "Report Center"; LoadUserControl(new ReportView(_currentUser.UserID, _currentUser.FirstName, _currentUser.LastName)); SetActiveNavButton(btnReports); };
            btnUsers.Click += (s, e) => { if (_currentActiveNavButton == btnUsers) return; lblPageTitle.Text = "User Management"; LoadUserControl(new UserManagementView(_currentUser.UserID)); SetActiveNavButton(btnUsers); };
            btnProfile.Click += (s, e) => { if (_currentActiveNavButton == btnProfile) return; lblPageTitle.Text = "My Profile"; LoadUserControl(new ProfileView(_currentUser.UserID)); SetActiveNavButton(btnProfile); };

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

            // 1. Remove transparency, explicitly use the workspace color
            pnlDashWrapper = new Panel { Dock = DockStyle.Fill, BackColor = UITheme.CurrentWorkspace };

            // 2. Upgrade to BufferedPanel and remove transparency
            pnlDashboardContainer = new BufferedPanel { AutoScroll = true, Padding = new Padding(40, 20, 40, 40), BackColor = UITheme.CurrentWorkspace };

            // 3. Initialize Custom Scrollbar
            _customScrollBar = new ThemedScrollBar(pnlDashboardContainer) { Dock = DockStyle.Right };

            pnlDashWrapper.Controls.Add(_customScrollBar);
            pnlDashWrapper.Controls.Add(pnlDashboardContainer);
            pnlWorkspace.Controls.Add(pnlDashWrapper);

            // 4. Resize Magic: Stretch the container so the native scrollbar renders outside the visible bounds
            pnlDashWrapper.Resize += (s, e) =>
            {
                pnlDashboardContainer.Location = new Point(0, 0);
                // Add 30 pixels to push the native scrollbar off-screen to the right
                pnlDashboardContainer.Size = new Size(pnlDashWrapper.Width + 30, pnlDashWrapper.Height);
                _customScrollBar.UpdateThumb();
            };

            // --- NATIVE WORKSPACE LAYOUT ENGINE (VERTICAL REDESIGN) ---

            pnlSalesSection = CreateModuleSection("SALES OVERVIEW", 350, out Panel salesGrid);
            salesGrid.Controls.Add(CreateStatCard("Today's Revenue", "₱ 0.00", "+0% from yesterday", IconChar.Wallet, true, 0, 0));
            salesGrid.Controls.Add(CreateStatCard("Transactions Today", "0", "All successful", IconChar.Receipt, true, 0, 140)); // Stacked Vertically

            // Add Top 3 Items Sold Panel - Position to the right of Sales History chart
            pnlTopItems = new Panel { Location = new Point(1160, 0), Size = new Size(380, 280), BackColor = Color.Transparent };
            salesGrid.Controls.Add(pnlTopItems);

            pnlInvSection = CreateModuleSection("INVENTORY OVERVIEW", 490, out Panel invGrid);
            invGrid.Controls.Add(CreateStatCard("Total Stock Value", "₱ 0.00", "Current valuation", IconChar.Boxes, false, 0, 0));
            invGrid.Controls.Add(CreateStatCard("Low Stock Alerts", "0 Items", "Needs immediate restocking", IconChar.ExclamationTriangle, false, 0, 140, true)); // Stacked
            invGrid.Controls.Add(CreateStatCard("Registered Products", "0", "Total items in database", IconChar.BoxOpen, false, 0, 280)); // Stacked

            // Add Low Stock Items Panel - Position to the right of inventory cards
            pnlLowStockItems = new Panel { Location = new Point(1160, 0), Size = new Size(380, 280), BackColor = Color.Transparent };
            invGrid.Controls.Add(pnlLowStockItems);

            pnlProcSection = CreateModuleSection("PROCUREMENT OVERVIEW", 350, out Panel procGrid);
            procGrid.Controls.Add(CreateStatCard("Pending Procurements", "0 Batches", "Arriving this week", IconChar.Truck, false, 0, 0));
            procGrid.Controls.Add(CreateStatCard("Total Purchase Orders", "0", "Lifetime POs logged", IconChar.FileInvoice, false, 0, 140)); // Stacked

            pnlDataSection = CreateModuleSection("SUPPLIER MANAGEMENT OVERVIEW", 190, out Panel dataGrid);
            dataGrid.Controls.Add(CreateStatCard("Registered Suppliers", "0", "Active business partners", IconChar.Handshake, false, 0, 0));

            pnlUserSection = CreateModuleSection("USER MANAGEMENT OVERVIEW", 190, out Panel userGrid);
            userGrid.Controls.Add(CreateStatCard("Active Users", "0", "System Admins & Cashiers", IconChar.Users, false, 0, 0));

            Panel pnlWelcomeWrapper = new Panel { Dock = DockStyle.Top, Height = 100, BackColor = Color.Transparent };
            lblWelcome = new Label { Text = $"Welcome Back, {_currentUser.FirstName}!", Font = new Font("Segoe UI", 22F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 0) };
            Label lblSubWelcome = new Label { Text = "Here's what is happening with SJ PC Store today.", Font = UITheme.MainFont, AutoSize = true, Location = new Point(2, 45) };
            pnlWelcomeWrapper.Controls.AddRange(new Control[] { lblWelcome, lblSubWelcome });
            _dynamicTexts.Add(lblWelcome);
            _mutedTexts.Add(lblSubWelcome);

            // Add them to the Workspace Container ONCE (Reverse order because of DockStyle.Top)
            pnlDashboardContainer.Controls.Add(pnlUserSection);
            pnlDashboardContainer.Controls.Add(pnlDataSection);
            pnlDashboardContainer.Controls.Add(pnlProcSection);
            pnlDashboardContainer.Controls.Add(pnlInvSection);
            pnlDashboardContainer.Controls.Add(pnlSalesSection);
            pnlDashboardContainer.Controls.Add(pnlWelcomeWrapper);

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

            // ========================================================
            // LOADING OVERLAY ENGINE
            // ========================================================
            _loadingOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CurrentWorkspace, // Matches your theme perfectly
                Visible = false
            };

            _spinnerBox = new PictureBox
            {
                // Ensure "Copy to Output Directory" is set to "Copy if newer" for Spinner.gif in VS
                Image = System.Drawing.Image.FromFile(Path.Combine(Application.StartupPath, "Utils", "Spinner.gif")),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill
            };

            _loadingOverlay.Controls.Add(_spinnerBox);

            // Add it directly to the workspace so it covers the content area
            pnlWorkspace.Controls.Add(_loadingOverlay);
        }

        private void ShowLoading()
        {
            _loadingOverlay.BackColor = UITheme.CurrentWorkspace; // Adapts if theme changes
            _loadingOverlay.BringToFront();
            _loadingOverlay.Visible = true;
            Application.DoEvents(); // Force WinForms to paint the spinner immediately
        }

        private void HideLoading()
        {
            _loadingOverlay.Visible = false;
            _loadingOverlay.SendToBack();
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
        private async void LoadUserControl(UserControl uc)
        {
            ShowLoading();
            await Task.Delay(500); // Brief pause to ensure the GIF starts animating before thread locks

            SendMessage(pnlWorkspace.Handle, WM_SETREDRAW, false, 0);
            pnlWorkspace.SuspendLayout();

            // Hide the entire dashboard wrapper
            if (pnlDashWrapper != null) pnlDashWrapper.Visible = false;

            for (int i = pnlWorkspace.Controls.Count - 1; i >= 0; i--)
            {
                if (pnlWorkspace.Controls[i] is UserControl oldUc) { pnlWorkspace.Controls.Remove(oldUc); oldUc.Dispose(); }
            }

            uc.Dock = DockStyle.Fill;
            pnlWorkspace.Controls.Add(uc);

            // Ensure the loading overlay stays on top of the newly added UserControl
            _loadingOverlay.BringToFront();
            uc.BringToFront();

            pnlWorkspace.ResumeLayout(true);
            SendMessage(pnlWorkspace.Handle, WM_SETREDRAW, true, 0);
            pnlWorkspace.Refresh();

            HideLoading();
        }

        private async void ShowDashboard()
        {
            ShowLoading();
            await Task.Delay(500); // Brief pause to ensure the GIF starts animating

            SendMessage(pnlWorkspace.Handle, WM_SETREDRAW, false, 0);
            pnlWorkspace.SuspendLayout();

            for (int i = pnlWorkspace.Controls.Count - 1; i >= 0; i--)
            {
                if (pnlWorkspace.Controls[i] is UserControl oldUc) { pnlWorkspace.Controls.Remove(oldUc); oldUc.Dispose(); }
            }

            // Show the wrapper and recalculate the custom thumb
            if (pnlDashWrapper != null)
            {
                pnlDashWrapper.Visible = true;
                pnlDashWrapper.BringToFront();
                _customScrollBar.UpdateThumb();
            }

            pnlWorkspace.ResumeLayout(true);
            SendMessage(pnlWorkspace.Handle, WM_SETREDRAW, true, 0);
            pnlWorkspace.Refresh();

            HideLoading();
        }

        private void SetActiveNavButton(IconButton activeBtn)
        {
            // 1. Lock in the newly selected button
            _currentActiveNavButton = activeBtn;

            // 2. Existing styling loop
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
            // 1. Suspend the painting of the sidebar completely to prevent the flash
            SendMessage(pnlSidebar.Handle, WM_SETREDRAW, false, 0);
            pnlSidebar.SuspendLayout();

            bool isExpanding = pnlSidebar.Width == 70;
            pnlSidebar.Width = isExpanding ? 260 : 70;

            lblBrandText.Visible = isExpanding;
            logoIcon.Location = new Point(isExpanding ? 20 : 15, 19);

            // Update the text on all buttons while painting is frozen
            foreach (var btn in _navButtons)
            {
                btn.Text = isExpanding ? btn.Tag.ToString() : "";
            }

            pnlSidebar.ResumeLayout(true);

            // 2. Resume painting and force a clean, instant redraw
            SendMessage(pnlSidebar.Handle, WM_SETREDRAW, true, 0);
            pnlSidebar.Refresh();
        }

        private void PopulateTopItemsPanel(List<TopItemModel> topItems)
        {
            pnlTopItems.Controls.Clear();

            Label lblTitle = new Label { Text = "Top 3 Items Sold", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(10, 5), AutoSize = true };
            _dynamicTexts.Add(lblTitle);
            pnlTopItems.Controls.Add(lblTitle);

            int yPosition = 35;
            foreach (var item in topItems)
            {
                // Create item container
                BufferedPanel itemPanel = new BufferedPanel { Location = new Point(5, yPosition), Size = new Size(370, 70), BackColor = Color.Transparent };
                itemPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        int radius = 8;
                        Rectangle rect = new Rectangle(0, 0, itemPanel.Width - 1, itemPanel.Height - 1);
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

                // Rank badge
                Label lblRank = new Label { Text = $"#{item.Rank}", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(15, 12), AutoSize = true };
                lblRank.ForeColor = UITheme.AccentYellow;
                _dynamicTexts.Add(lblRank);

                // Item specs
                Label lblSpecs = new Label { Text = item.ItemSpecs.Length > 40 ? item.ItemSpecs.Substring(0, 37) + "..." : item.ItemSpecs, Font = new Font("Segoe UI", 9F, FontStyle.Regular), Location = new Point(60, 12), AutoSize = true, MaximumSize = new Size(320, 30), Size = new Size(320, 30) };
                _dynamicTexts.Add(lblSpecs);

                // Units sold
                Label lblUnits = new Label { Text = $"{item.UnitsSold} units sold", Font = new Font("Segoe UI", 8.5F), Location = new Point(60, 45), AutoSize = true };
                lblUnits.ForeColor = UITheme.MutedText;
                _mutedTexts.Add(lblUnits);

                itemPanel.Controls.AddRange(new Control[] { lblRank, lblSpecs, lblUnits });
                pnlTopItems.Controls.Add(itemPanel);

                yPosition += 75;
            }
        }

        private void PopulateLowStockItemsPanel(List<LowStockItemModel> lowStockItems)
        {
            pnlLowStockItems.Controls.Clear();

            Label lblTitle = new Label { Text = "Low Stock Items", Font = new Font("Segoe UI", 11F, FontStyle.Bold), Location = new Point(10, 5), AutoSize = true };
            _dynamicTexts.Add(lblTitle);
            pnlLowStockItems.Controls.Add(lblTitle);

            int yPosition = 35;
            foreach (var item in lowStockItems)
            {
                // Create item container
                BufferedPanel itemPanel = new BufferedPanel { Location = new Point(5, yPosition), Size = new Size(370, 70), BackColor = Color.Transparent };
                itemPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        int radius = 8;
                        Rectangle rect = new Rectangle(0, 0, itemPanel.Width - 1, itemPanel.Height - 1);
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

                // Rank badge with warning color
                Label lblRank = new Label { Text = $"#{item.Rank}", Font = new Font("Segoe UI", 14F, FontStyle.Bold), Location = new Point(15, 12), AutoSize = true };
                lblRank.ForeColor = Color.FromArgb(239, 68, 68); // Red for warning
                _dynamicTexts.Add(lblRank);

                // Item specs
                Label lblSpecs = new Label { Text = item.ItemSpecs.Length > 40 ? item.ItemSpecs.Substring(0, 37) + "..." : item.ItemSpecs, Font = new Font("Segoe UI", 9F, FontStyle.Regular), Location = new Point(60, 12), AutoSize = true, MaximumSize = new Size(300, 30), Size = new Size(300, 30) };
                _dynamicTexts.Add(lblSpecs);

                // Available stock
                Label lblStock = new Label { Text = $"{item.AvailableStock} in stock", Font = new Font("Segoe UI", 8.5F), Location = new Point(60, 45), AutoSize = true };
                lblStock.ForeColor = Color.FromArgb(239, 68, 68); // Red warning text
                _mutedTexts.Add(lblStock);

                itemPanel.Controls.AddRange(new Control[] { lblRank, lblSpecs, lblStock });
                pnlLowStockItems.Controls.Add(itemPanel);

                yPosition += 75;
            }
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

                    // Populate Top 3 Items Sold
                    if (stats.TopItemsSold.Count > 0)
                    {
                        PopulateTopItemsPanel(stats.TopItemsSold);
                    }

                    // Populate Low Stock Items
                    if (stats.LowStockItems.Count > 0)
                    {
                        PopulateLowStockItemsPanel(stats.LowStockItems);
                    }

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

                // ==========================================
                // CHART STYLING BASE
                // ==========================================
                var charts = new[] { _salesBarChart, _inventoryDonutChart, _stockStatusPieChart, _procurementBarChart };
                foreach (var chart in charts)
                {
                    chart.Plot.Clear();
                    chart.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(UITheme.CurrentWorkspace);
                    chart.Plot.DataBackground.Color = ScottPlot.Color.FromColor(UITheme.CurrentWorkspace);
                    chart.Plot.Axes.Color(ScottPlot.Color.FromColor(UITheme.MutedText));

                    // Apply universal Title styling
                    chart.Plot.Axes.Title.Label.FontSize = 16;
                    chart.Plot.Axes.Title.Label.Bold = true;
                    chart.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromColor(UITheme.CurrentText);
                }

                ScottPlot.Color[] palette = GetStrictThemePalette();

                // ==========================================
                // 1. RENDER SALES BAR CHART 
                // ==========================================
                _salesBarChart.Plot.Title("Sales History"); // Added formal title

                if (stats.WeeklySalesData.Count > 0)
                {
                    int i = 0;
                    List<ScottPlot.Tick> ticks = new List<ScottPlot.Tick>();
                    foreach (var kvp in stats.WeeklySalesData)
                    {
                        var bar = _salesBarChart.Plot.Add.Bar(position: i, value: kvp.Value);
                        // Apply alternating PrimaryDark and SecondaryDark colors
                        bar.Color = (i % 2 == 0) ? ScottPlot.Color.FromColor(UITheme.PrimaryDark) : ScottPlot.Color.FromColor(UITheme.SecondaryDark);
                        bar.Label = kvp.Key;
                        ticks.Add(new ScottPlot.Tick(i, kvp.Key));
                        i++;
                    }
                    _salesBarChart.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks.ToArray());
                    _salesBarChart.Plot.Axes.Margins(bottom: 0);
                    _salesBarChart.Plot.ShowLegend();

                    // Style bar chart axes and labels
                    _salesBarChart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 12;
                    _salesBarChart.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromColor(UITheme.CurrentText);
                    _salesBarChart.Plot.Axes.Left.TickLabelStyle.FontSize = 12;
                    _salesBarChart.Plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromColor(UITheme.CurrentText);
                }
                _salesBarChart.Refresh();

                // ==========================================
                // 2. RENDER INVENTORY PIE (Categories w/ Percentages)
                // ==========================================
                _inventoryDonutChart.Plot.Title("Category Distribution"); // Added formal title
                _inventoryDonutChart.Plot.HideGrid(); // Explicitly removes the grid

                if (stats.InventoryCategoryData.Count > 0)
                {
                    double totalItems = stats.InventoryCategoryData.Values.Sum();
                    List<PieSlice> catSlices = new List<PieSlice>();
                    int colorIndex = 0;

                    foreach (var kvp in stats.InventoryCategoryData)
                    {
                        double percentage = (kvp.Value / totalItems) * 100;
                        ScottPlot.Color sliceColor = palette[colorIndex % palette.Length];
                        // Use black text for AccentYellow, white for others
                        ScottPlot.Color labelColor = sliceColor.Equals(ScottPlot.Color.FromColor(UITheme.AccentYellow))
                            ? ScottPlot.Color.FromColor(System.Drawing.Color.Black)
                            : ScottPlot.Color.FromColor(System.Drawing.Color.Black);

                        catSlices.Add(new PieSlice()
                        {
                            Value = kvp.Value,
                            Label = $"{percentage:0.#}%",
                            FillColor = sliceColor,
                            LegendText = kvp.Key,
                            LabelFontSize = 16,
                            LabelFontColor = labelColor
                        });
                        colorIndex++;
                    }
                    var catPie = _inventoryDonutChart.Plot.Add.Pie(catSlices);
                    catPie.DonutFraction = 0.5;
                    catPie.SliceLabelDistance = 0.35;

                    // Style the pie chart appearance
                    var legend = _inventoryDonutChart.Plot.Legend;
                    legend.FontSize = 12;
                    legend.FontColor = ScottPlot.Color.FromColor(UITheme.CurrentText);
                    legend.BackgroundColor = ScottPlot.Color.FromColor(UITheme.CurrentPanel);
                    legend.OutlineColor = ScottPlot.Color.FromColor(UITheme.CurrentBorder);

                    _inventoryDonutChart.Plot.Axes.Frameless();
                    _inventoryDonutChart.Plot.ShowLegend();
                }
                _inventoryDonutChart.Refresh();

                // ==========================================
                // 3. RENDER STOCK STATUS PIE 
                // ==========================================
                _stockStatusPieChart.Plot.Title("Stock Status Overview"); // Added formal title
                _stockStatusPieChart.Plot.HideGrid(); // Explicitly removes the grid

                if (stats.StockStatusData.Count > 0)
                {
                    List<PieSlice> statusSlices = new List<PieSlice>();
                    int colorIndex = 0;

                    foreach (var kvp in stats.StockStatusData)
                    {
                        ScottPlot.Color sliceColor = palette[colorIndex % palette.Length];
                        // Use black text for AccentYellow, white for others
                        ScottPlot.Color labelColor = sliceColor.Equals(ScottPlot.Color.FromColor(UITheme.AccentYellow))
                            ? ScottPlot.Color.FromColor(System.Drawing.Color.Black)
                            : ScottPlot.Color.FromColor(System.Drawing.Color.White);

                        statusSlices.Add(new PieSlice()
                        {
                            Value = kvp.Value,
                            Label = kvp.Value.ToString(),
                            FillColor = sliceColor,
                            LegendText = kvp.Key,
                            LabelFontSize = 16,
                            LabelFontColor = labelColor
                        });
                        colorIndex++;
                    }
                    var statusPie = _stockStatusPieChart.Plot.Add.Pie(statusSlices);
                    statusPie.SliceLabelDistance = 0.35;

                    // Style the pie chart appearance
                    var statusLegend = _stockStatusPieChart.Plot.Legend;
                    statusLegend.FontSize = 12;
                    statusLegend.FontColor = ScottPlot.Color.FromColor(UITheme.CurrentText);
                    statusLegend.BackgroundColor = ScottPlot.Color.FromColor(UITheme.CurrentPanel);
                    statusLegend.OutlineColor = ScottPlot.Color.FromColor(UITheme.CurrentBorder);

                    _stockStatusPieChart.Plot.Axes.Frameless();
                    _stockStatusPieChart.Plot.ShowLegend();
                }
                _stockStatusPieChart.Refresh();

                // ==========================================
                // 4. RENDER PROCUREMENT EXPENSE BAR 
                // ==========================================
                _procurementBarChart.Plot.Title("Procurement Expenses"); // Added formal title

                if (stats.ProcurementExpenseData.Count > 0)
                {
                    int i = 0;
                    List<ScottPlot.Tick> procTicks = new List<ScottPlot.Tick>();
                    foreach (var kvp in stats.ProcurementExpenseData)
                    {
                        var bar = _procurementBarChart.Plot.Add.Bar(position: i, value: kvp.Value);
                        // Apply alternating PrimaryDark and SecondaryDark colors
                        bar.Color = (i % 2 == 0) ? ScottPlot.Color.FromColor(UITheme.PrimaryDark) : ScottPlot.Color.FromColor(UITheme.SecondaryDark);
                        bar.Label = kvp.Key;
                        procTicks.Add(new ScottPlot.Tick(i, kvp.Key));
                        i++;
                    }
                    _procurementBarChart.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(procTicks.ToArray());
                    _procurementBarChart.Plot.Axes.Margins(bottom: 0);
                    _procurementBarChart.Plot.ShowLegend();

                    // Style bar chart axes and labels
                    _procurementBarChart.Plot.Axes.Bottom.TickLabelStyle.FontSize = 12;
                    _procurementBarChart.Plot.Axes.Bottom.TickLabelStyle.ForeColor = ScottPlot.Color.FromColor(UITheme.CurrentText);
                    _procurementBarChart.Plot.Axes.Left.TickLabelStyle.FontSize = 12;
                    _procurementBarChart.Plot.Axes.Left.TickLabelStyle.ForeColor = ScottPlot.Color.FromColor(UITheme.CurrentText);
                }
                _procurementBarChart.Refresh();
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
                Size = new Size(255, 55),
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

        private Panel CreateModuleSection(string title, int customHeight, out Panel grid)
        {
            // Height is now dynamic based on how many cards are stacked vertically
            BufferedPanel pnl = new BufferedPanel { Dock = DockStyle.Top, Height = customHeight, Padding = new Padding(0, 0, 0, 20) };
            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 10F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 0) };

            pnl.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(pen, 0, 25, pnl.Width, 25); }
            };

            BufferedPanel innerGrid = new BufferedPanel
            {
                Location = new Point(0, 40),
                Size = new Size(pnl.Width, customHeight - 50),
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

            if (pnlDashWrapper != null) pnlDashWrapper.BackColor = UITheme.CurrentWorkspace;
            if (pnlDashboardContainer != null) pnlDashboardContainer.BackColor = UITheme.CurrentWorkspace; // <-- ADD THIS LINE
            if (_customScrollBar != null) _customScrollBar.Invalidate();

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

            // Refresh charts with new theme colors
            RefreshChartThemes();
        }

        private void RefreshChartThemes()
        {
            var charts = new[] { _salesBarChart, _inventoryDonutChart, _stockStatusPieChart, _procurementBarChart };
            foreach (var chart in charts)
            {
                if (chart != null)
                {
                    chart.BackColor = UITheme.CurrentWorkspace;

                    chart.Plot.FigureBackground.Color = ScottPlot.Color.FromColor(UITheme.CurrentWorkspace);
                    chart.Plot.DataBackground.Color = ScottPlot.Color.FromColor(UITheme.CurrentWorkspace);
                    chart.Plot.Axes.Color(ScottPlot.Color.FromColor(UITheme.MutedText));

                    // Apply title styling with theme colors
                    chart.Plot.Axes.Title.Label.FontSize = 16;
                    chart.Plot.Axes.Title.Label.Bold = true;
                    chart.Plot.Axes.Title.Label.ForeColor = ScottPlot.Color.FromColor(UITheme.CurrentText);

                    chart.Refresh();
                }
            }
        }
    }
}