using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace SJ_PC_Store_SIMS.Views
{
    public class UserManagementView : UserControl
    {
        // =========================================================================
        // CUSTOM ENGINE COMPONENTS (Scraped & Replicated)
        // =========================================================================
        private class ModalForm : Form { protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } } }
        private class SmoothPanel : Panel { public SmoothPanel() { this.DoubleBuffered = true; this.ResizeRedraw = true; } }
        private class SmoothGrid : DataGridView { public SmoothGrid() { this.DoubleBuffered = true; } }

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

        private class DarkComboBox : ComboBox
        {
            public DarkComboBox() { this.DrawMode = DrawMode.OwnerDrawFixed; this.FlatStyle = FlatStyle.Flat; }
            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                if (e.Index < 0) return; e.DrawBackground();
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bg = isSelected ? (UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : UITheme.PrimaryDark) : this.BackColor;
                Color fg = isSelected ? (UITheme.IsDarkMode ? UITheme.AccentYellow : Color.White) : this.ForeColor;
                using (SolidBrush bgBrush = new SolidBrush(bg)) using (SolidBrush fgBrush = new SolidBrush(fg))
                {
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    SizeF textSize = e.Graphics.MeasureString(this.Items[e.Index].ToString(), this.Font);
                    float textY = e.Bounds.Y + (e.Bounds.Height - textSize.Height) / 2;
                    e.Graphics.DrawString(this.Items[e.Index].ToString(), this.Font, fgBrush, e.Bounds.X + 8, textY);
                }
            }
            private const int WM_PAINT = 0xF;
            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if (m.Msg == WM_PAINT)
                {
                    using (Graphics g = Graphics.FromHwnd(Handle))
                    {
                        Rectangle arrowRect = new Rectangle(this.Width - 20, 0, 20, this.Height); using (SolidBrush b = new SolidBrush(this.BackColor)) { g.FillRectangle(b, arrowRect); }
                        using (Pen p = new Pen(this.ForeColor, 2)) { int cx = arrowRect.X + 8; int cy = arrowRect.Y + (this.Height / 2) - 1; g.DrawLine(p, cx - 4, cy - 2, cx, cy + 2); g.DrawLine(p, cx, cy + 2, cx + 4, cy - 2); }
                        using (Pen borderPen = new Pen(this.BackColor, 2)) { g.DrawRectangle(borderPen, 0, 0, Width, Height); }
                    }
                }
            }
        }

        private class TabButton : IconButton
        {
            public bool IsActive { get; set; } = false;
            public TabButton() { this.SetStyle(ControlStyles.SupportsTransparentBackColor, true); this.BackColor = Color.Transparent; this.FlatStyle = FlatStyle.Flat; this.FlatAppearance.BorderSize = 0; this.Cursor = Cursors.Hand; }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (IsActive)
                {
                    Rectangle rect = new Rectangle(0, 0, this.Width, this.Height);
                    using (GraphicsPath path = new GraphicsPath())
                    {
                        int radius = 8; path.AddArc(rect.X, rect.Y, radius, radius, 180, 90); path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90); path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom); path.CloseFigure();
                        using (SolidBrush brush = new SolidBrush(UITheme.CurrentPanel)) { e.Graphics.FillPath(brush, path); }
                        using (Pen pen = new Pen(UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, 3)) { e.Graphics.DrawLine(pen, 0, 1, this.Width, 1); }
                    }
                }
                base.OnPaint(e);
            }
        }

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private class ThemedMonthCalendar : MonthCalendar { private Color _backColor = SystemColors.Window; public new Color BackColor { get => _backColor; set { _backColor = value; this.Invalidate(); } } protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); SetWindowTheme(this.Handle, "", ""); } }

        private class ThemedDatePicker : Panel
        {
            public DateTime Value { get => _selectedDate; set { _selectedDate = value; txtDate.Text = value.ToString("MM/dd/yyyy"); } }
            private DateTime _selectedDate = DateTime.Now;
            private TextBox txtDate; private Button btnDrop; private ThemedMonthCalendar monthCal; private Form popup;

            public ThemedDatePicker()
            {
                this.Size = new Size(130, 38); this.Padding = new Padding(0); this.BackColor = UITheme.CurrentInputBg;
                txtDate = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5F), Text = _selectedDate.ToString("MM/dd/yyyy"), ReadOnly = true, BackColor = this.BackColor, ForeColor = UITheme.CurrentText };
                txtDate.Click += (s, e) => ToggleCalendar();
                btnDrop = new Button { Text = "▼", Dock = DockStyle.Right, Width = 24, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F), BackColor = this.BackColor, ForeColor = UITheme.CurrentText, Cursor = Cursors.Hand };
                btnDrop.FlatAppearance.BorderSize = 0; btnDrop.Click += (s, e) => ToggleCalendar();
                this.Controls.Add(txtDate); this.Controls.Add(btnDrop); txtDate.BringToFront();
            }

            private void ToggleCalendar()
            {
                if (popup == null || popup.IsDisposed)
                {
                    popup = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, TopMost = true, BackColor = UITheme.CurrentInputBg, Padding = new Padding(0) };
                    monthCal = new ThemedMonthCalendar { MaxSelectionCount = 1, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, TitleBackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234), TitleForeColor = UITheme.CurrentText, TrailingForeColor = UITheme.MutedText };
                    monthCal.DateSelected += (s, ev) => { Value = monthCal.SelectionStart; popup.Close(); };
                    popup.Controls.Add(monthCal); popup.Deactivate += (s, e) => popup.Close();
                    Size calSize = monthCal.GetPreferredSize(Size.Empty); popup.ClientSize = new Size(calSize.Width + 40, calSize.Height + 8);
                }
                Point screenLoc = this.PointToScreen(new Point(0, this.Height));
                Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                if (screenLoc.Y + popup.Height > workingArea.Bottom) screenLoc.Y -= this.Height + popup.Height;
                if (screenLoc.X + popup.Width > workingArea.Right) screenLoc.X = workingArea.Right - popup.Width;
                popup.Location = screenLoc; popup.Show(); monthCal.Focus();
            }
            public void ApplyTheme() { Color bg = UITheme.CurrentInputBg; this.BackColor = bg; txtDate.BackColor = bg; txtDate.ForeColor = UITheme.CurrentText; btnDrop.BackColor = bg; btnDrop.ForeColor = UITheme.CurrentText; }
        }

        // =========================================================================
        // VIEW VARIABLES
        // =========================================================================
        private UserController _userController;
        private ActivityLogController _logController;
        private string _activeUserId;

        private SmoothPanel pnlTabs, pnlContent, pnlUsersTab, pnlLogsTab;
        private RoundedPanel pnlUsersToolbar, pnlLogsToolbar;
        private TabButton btnTabUsers, btnTabLogs;
        private SmoothGrid dgvUsers, dgvLogs;

        // Toolbar Filters
        private ThemedDatePicker dtpLogFrom, dtpLogTo;
        private DarkComboBox cmbRoles, cmbModuleCategory;
        private TextBox txtSearchUser, txtSearchLog;

        // Data & Pagination States
        private List<UserModel> _usersData = new List<UserModel>();
        private List<ActivityLogModel> _logsData = new List<ActivityLogModel>(); // Assuming model exists
        private int _userPage = 0, _logPage = 0;
        private const int PAGE_SIZE = 10;
        private Label lblUserPage, lblLogPage;

        private int _userSortIndex = 0;
        private readonly string[] _userSortOptions = { "Newest First", "Oldest First", "Name (A-Z)", "Name (Z-A)" };

        private List<RoundedPanel> _inputWrappers = new List<RoundedPanel>();
        private List<TextBox> _textInputs = new List<TextBox>();
        private List<DarkComboBox> _comboInputs = new List<DarkComboBox>();
        private List<IconButton> _buttons = new List<IconButton>();

        // Action Column Hover States
        private int _hoverRowUser = -1; private string _hoverIconUser = "";

        // PDF Generation State Tracker
        private int _pdfPrintIndex = 0;

        public UserManagementView(string currentUserId)
        {
            _activeUserId = currentUserId;
            _userController = new UserController();
            _logController = new ActivityLogController();

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.Padding = new Padding(35, 15, 35, 35);
            this.Margin = new Padding(0);

            InitializeUI();

            FetchUsersData();
            FetchLogsData();

            ApplyTheme();
            SwitchTab("Users");
        }

        // =========================================================================
        // INITIALIZATION
        // =========================================================================
        private void InitializeUI()
        {
            pnlTabs = new SmoothPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0) };
            pnlTabs.Paint += (s, e) => { using (Pen pen = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(pen, 0, pnlTabs.Height - 1, pnlTabs.Width, pnlTabs.Height - 1); } };

            btnTabUsers = CreateTab("User Control Catalog", IconChar.Users);
            btnTabLogs = CreateTab("Activity Log Catalog", IconChar.History);

            btnTabUsers.Click += (s, e) => SwitchTab("Users");
            btnTabLogs.Click += (s, e) => SwitchTab("Logs");

            pnlTabs.Controls.AddRange(new Control[] { btnTabLogs, btnTabUsers });
            pnlContent = new SmoothPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 20, 0, 0) };

            InitializeUsersTab();
            InitializeLogsTab();

            pnlContent.Controls.AddRange(new Control[] { pnlLogsTab, pnlUsersTab });
            this.Controls.Add(pnlContent); this.Controls.Add(pnlTabs);
        }

        private void InitializeUsersTab()
        {
            pnlUsersTab = new SmoothPanel { Dock = DockStyle.Fill };
            pnlUsersToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };
            Control txtSearchWrapper = CreateSearchInput("Search User...", 250, out txtSearchUser, () => FetchUsersData());
            Control cmbRoleWrapper = CreateComboInput(new[] { "All Roles", "Administrator", "Cashier" }, 150, out cmbRoles);

            cmbRoles.SelectedIndexChanged += (s, e) => FetchUsersData();

            IconButton btnRoleCreation = CreateButton("Create Role", IconChar.UserShield, "Primary");
            btnRoleCreation.Click += (s, e) => OpenModal("Role");

            IconButton btnManageRoles = CreateButton("Manage Roles", IconChar.UserCog, "Secondary");
            btnManageRoles.Click += (s, e) => OpenModal("ManageRole");

            // HOOKED UP: Dynamic Cycle Sorting
            IconButton btnSort = CreateButton("Sort: Newest First", IconChar.CalendarAlt, "ActionAdd");
            btnSort.Click += (s, e) =>
            {
                // Cycle to the next sort option
                _userSortIndex = (_userSortIndex + 1) % _userSortOptions.Length;
                btnSort.Text = "  Sort: " + _userSortOptions[_userSortIndex];

                // Update Icon dynamically
                if (_userSortIndex == 0 || _userSortIndex == 1) btnSort.IconChar = IconChar.CalendarAlt;
                else if (_userSortIndex == 2) btnSort.IconChar = IconChar.SortAlphaDown;
                else if (_userSortIndex == 3) btnSort.IconChar = IconChar.SortAlphaUp;

                ApplyUserSort();
            };

            flpLeft.Controls.AddRange(new Control[] { txtSearchWrapper, cmbRoleWrapper, btnRoleCreation, btnManageRoles, btnSort });

            IconButton btnAddUser = CreateButton("Add New User", IconChar.UserPlus, "ActionAdd");
            btnAddUser.Dock = DockStyle.Right;
            btnAddUser.Click += (s, e) => OpenModal("CreateUser");

            pnlUsersToolbar.Controls.Add(flpLeft); pnlUsersToolbar.Controls.Add(btnAddUser);

            SmoothPanel pnlGridGap = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

            RoundedPanel pnlGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            dgvUsers = CreateDataGridView();
            dgvUsers.Columns.Add("UserID", "USER ID");
            dgvUsers.Columns.Add("FullName", "FULL NAME");
            dgvUsers.Columns.Add("Role", "ROLE");
            dgvUsers.Columns.Add("Status", "STATUS");

            DataGridViewTextBoxColumn colActions = new DataGridViewTextBoxColumn { HeaderText = "ACTIONS", Name = "ColActions", Width = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            dgvUsers.Columns.Add(colActions);

            dgvUsers.CellPainting += DgvUsers_CellPainting;
            dgvUsers.CellMouseMove += DgvUsers_CellMouseMove;
            dgvUsers.CellMouseClick += DgvUsers_CellMouseClick;
            dgvUsers.CellMouseLeave += (s, e) => { _hoverRowUser = -1; _hoverIconUser = ""; dgvUsers.Invalidate(); };

            pnlGridContainer.Controls.Add(dgvUsers);

            Panel pnlPagination = CreatePaginationPanel(ref lblUserPage, () => { if (_userPage > 0) { _userPage--; RenderUsersGrid(); } }, () => { if ((_userPage + 1) * PAGE_SIZE < _usersData.Count) { _userPage++; RenderUsersGrid(); } });

            pnlUsersTab.Controls.AddRange(new Control[] { pnlGridContainer, pnlPagination, pnlGridGap, pnlUsersToolbar });
        }

        private void InitializeLogsTab()
        {
            pnlLogsTab = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            pnlLogsToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            dtpLogFrom = new ThemedDatePicker { Value = DateTime.Now.AddDays(-7) };
            dtpLogTo = new ThemedDatePicker { Value = DateTime.Now };
            Control wFrom = CreateInputWrapper(dtpLogFrom, 130);
            Control wTo = CreateInputWrapper(dtpLogTo, 130);

            Control cmbCatWrapper = CreateComboInput(new[] { "All Modules", "Inventory", "Procurement", "Sales", "Data Management", "User Management" }, 180, out cmbModuleCategory);
            Control txtSearchWrapper = CreateSearchInput("Search Logs...", 250, out txtSearchLog, () => FetchLogsData());

            IconButton btnFilter = CreateButton("Apply Filter", IconChar.Filter, "Primary");
            btnFilter.Click += (s, e) => FetchLogsData();

            flpLeft.Controls.AddRange(new Control[] { wFrom, wTo, cmbCatWrapper, txtSearchWrapper, btnFilter });

            IconButton btnExport = CreateButton("Export PDF", IconChar.FilePdf, "ActionAdd");
            btnExport.Dock = DockStyle.Right;
            // HOOKED UP: Triggers the PDF generation
            btnExport.Click += (s, e) => GeneratePDF();

            pnlLogsToolbar.Controls.Add(flpLeft); pnlLogsToolbar.Controls.Add(btnExport);

            SmoothPanel pnlGridGap = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

            RoundedPanel pnlGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            dgvLogs = CreateDataGridView();
            dgvLogs.Columns.Add("UserID", "USER ID");
            dgvLogs.Columns.Add("FullName", "FULL NAME");
            dgvLogs.Columns.Add("Module", "MODULE CATEGORY");
            dgvLogs.Columns.Add("Desc", "ACTION DESCRIPTION");
            dgvLogs.Columns.Add("Date", "LOG DATE");

            pnlGridContainer.Controls.Add(dgvLogs);

            Panel pnlPagination = CreatePaginationPanel(ref lblLogPage, () => { if (_logPage > 0) { _logPage--; RenderLogsGrid(); } }, () => { if ((_logPage + 1) * PAGE_SIZE < _logsData.Count) { _logPage++; RenderLogsGrid(); } });

            pnlLogsTab.Controls.AddRange(new Control[] { pnlGridContainer, pnlPagination, pnlGridGap, pnlLogsToolbar });
        }

        // =========================================================================
        // DATA GRID ACTION LOGIC
        // =========================================================================
        private void DgvUsers_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvUsers.Columns["ColActions"].Index)
            {
                int iconSize = 18; int gap = 8; int startX = 18;
                bool overFirst = e.X >= startX && e.X <= startX + iconSize;
                bool overSecond = e.X >= startX + iconSize + gap && e.X <= startX + (iconSize * 2) + gap;
                bool overThird = e.X >= startX + (iconSize * 2) + (gap * 2) && e.X <= startX + (iconSize * 3) + (gap * 2);

                string status = dgvUsers.Rows[e.RowIndex].Cells["Status"].Value.ToString();
                string currentHover = "";

                if (status == "Active")
                {
                    if (overFirst) currentHover = "Info";
                    else if (overSecond) currentHover = "Edit";
                    else if (overThird) currentHover = "ToggleOff";
                }
                else
                {
                    if (overFirst) currentHover = "Info";
                    else if (overSecond) currentHover = "ToggleOn";
                }

                if (_hoverRowUser != e.RowIndex || _hoverIconUser != currentHover)
                {
                    _hoverRowUser = e.RowIndex; _hoverIconUser = currentHover;
                    dgvUsers.InvalidateCell(e.ColumnIndex, e.RowIndex);
                }
            }
        }

        private void DgvUsers_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvUsers.Columns["ColActions"].Index && !string.IsNullOrEmpty(_hoverIconUser))
            {
                DataGridViewRow row = dgvUsers.Rows[e.RowIndex];
                if (_hoverIconUser == "Info") OpenModal("UserDetails", row);
                else if (_hoverIconUser == "Edit") OpenModal("EditUser", row);
                else if (_hoverIconUser == "ToggleOff" || _hoverIconUser == "ToggleOn") OpenModal("ToggleStatus", row);
            }
        }

        private void DgvUsers_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvUsers.Columns["ColActions"].Index)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                int iconSize = 18; int gap = 8;
                int startX = e.CellBounds.X + 18;
                int startY = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;

                string status = dgvUsers.Rows[e.RowIndex].Cells["Status"].Value.ToString();

                Color cInfo = (_hoverRowUser == e.RowIndex && _hoverIconUser == "Info") ? UITheme.AccentYellow : UITheme.MutedText;
                using (Bitmap infoIcon = SafeGetIcon(IconChar.InfoCircle, cInfo, iconSize))
                {
                    e.Graphics.DrawImage(infoIcon, startX, startY, iconSize, iconSize);
                }

                if (status == "Active")
                {
                    Color cEdit = (_hoverRowUser == e.RowIndex && _hoverIconUser == "Edit") ? UITheme.AccentYellow : UITheme.MutedText;
                    Color cTrash = (_hoverRowUser == e.RowIndex && _hoverIconUser == "ToggleOff") ? UITheme.AccentYellow : Color.FromArgb(239, 68, 68);

                    using (Bitmap editIcon = SafeGetIcon(IconChar.Pen, cEdit, iconSize))
                    using (Bitmap trashIcon = SafeGetIcon(IconChar.Trash, cTrash, iconSize))
                    {
                        e.Graphics.DrawImage(editIcon, startX + iconSize + gap, startY, iconSize, iconSize);
                        e.Graphics.DrawImage(trashIcon, startX + (iconSize * 2) + (gap * 2), startY, iconSize, iconSize);
                    }
                }
                else
                {
                    Color cUndo = (_hoverRowUser == e.RowIndex && _hoverIconUser == "ToggleOn") ? UITheme.AccentYellow : Color.FromArgb(16, 185, 129);
                    using (Bitmap undoIcon = SafeGetIcon(IconChar.Undo, cUndo, iconSize))
                    {
                        e.Graphics.DrawImage(undoIcon, startX + iconSize + gap, startY, iconSize, iconSize);
                    }
                }
                e.Handled = true;
            }
        }

        private Bitmap SafeGetIcon(IconChar icon, Color color, int size = 24)
        {
            try { return icon.ToBitmap(color, size); }
            catch { int s = size > 0 ? size : 24; Bitmap b = new Bitmap(s, s); using (Graphics g = Graphics.FromImage(b)) { g.Clear(Color.Transparent); } return b; }
        }

        // =========================================================================
        // MODAL ENGINE (Fully Functional Integration)
        // =========================================================================
        private void OpenModal(string type, DataGridViewRow rowData = null)
        {
            ModalForm modal = new ModalForm { FormBorderStyle = FormBorderStyle.None, BackColor = UITheme.CurrentPanel, StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = false };

            modal.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 12;
                    path.AddArc(0, 0, r, r, 180, 90); path.AddArc(modal.Width - r - 1, 0, r, r, 270, 90);
                    path.AddArc(modal.Width - r - 1, modal.Height - r - 1, r, r, 0, 90); path.AddArc(0, modal.Height - r - 1, r, r, 90, 90);
                    path.CloseFigure(); modal.Region = new Region(path);
                    if (type != "ToggleStatus") { using (Pen p = new Pen(UITheme.CurrentBorder, 3)) { e.Graphics.DrawPath(p, path); } }
                }
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234) };
            IconChar headerIconType = type == "CreateUser" ? IconChar.UserPlus :
                                      (type == "UserDetails" ? IconChar.InfoCircle :
                                      (type == "EditUser" ? IconChar.UserEdit :
                                      (type == "ManageRole" ? IconChar.UserCog : IconChar.UserShield)));
            IconPictureBox headerIcon = new IconPictureBox { IconChar = headerIconType, IconColor = UITheme.CurrentText, IconSize = 22, Size = new Size(24, 24), Location = new Point(20, 18) };
            Label lblTitle = new Label { Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(50, 17) };

            IconButton btnClose = new IconButton { IconChar = IconChar.Times, IconSize = 20, Size = new Size(40, 40), FlatStyle = FlatStyle.Flat, ForeColor = UITheme.MutedText, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0; btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnClose.Click += (s, e) => modal.Close();
            btnClose.MouseEnter += (s, e) => btnClose.IconColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseLeave += (s, e) => btnClose.IconColor = UITheme.MutedText;

            pnlHeader.Controls.AddRange(new Control[] { headerIcon, lblTitle, btnClose });

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = UITheme.CurrentPanel };
            Button btnCancel = new Button { Text = type == "UserDetails" ? "Close" : "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.FlatAppearance.MouseDownBackColor = UITheme.CurrentPanel;
            btnCancel.Click += (s, e) => modal.Close();

            if (type == "ToggleStatus")
            {
                modal.Size = new Size(400, 250); pnlHeader.Visible = false; btnClose.Location = new Point(350, 10); modal.Controls.Add(btnClose);
                bool isActive = rowData.Cells["Status"].Value.ToString() == "Active";

                IconPictureBox iconWarning = new IconPictureBox { IconChar = isActive ? IconChar.ExclamationTriangle : IconChar.Undo, IconColor = isActive ? Color.FromArgb(239, 68, 68) : Color.FromArgb(16, 185, 129), IconSize = 60, Size = new Size(60, 60) };
                Label lblWarn = new Label { Text = isActive ? "Deactivate User" : "Activate User", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                Label lblDesc = new Label { Text = isActive ? "Are you sure you want to set this user to inactive?\nThey will not be able to log in." : "Are you sure you want to restore this user?\nThey will regain system access.", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };

                iconWarning.Location = new Point((modal.Width - iconWarning.Width) / 2, 30);
                lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 100);
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 135);

                Button btnAction = new Button { Text = isActive ? "Deactivate" : "Activate", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = isActive ? Color.FromArgb(239, 68, 68) : Color.FromArgb(16, 185, 129), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;

                int startX = (modal.Width - (btnCancel.Width + 10 + btnAction.Width)) / 2;
                btnCancel.Location = new Point(startX, 15); btnAction.Location = new Point(startX + btnCancel.Width + 10, 15);

                // HOOKED UP: Toggle User Status
                btnAction.Click += (s, e) =>
                {
                    string targetUserId = rowData.Cells["UserID"].Value.ToString();
                    string newStatus = isActive ? "Inactive" : "Active";
                    if (_userController.ToggleUserStatus(targetUserId, newStatus, _activeUserId))
                    {
                        FetchUsersData();
                        modal.Close();
                    }
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.AddRange(new Control[] { iconWarning, lblWarn, lblDesc });
            }
            else if (type == "Role" || type == "ManageRole")
            {
                modal.Size = new Size(400, type == "ManageRole" ? 570 : 500);
                lblTitle.Text = type == "ManageRole" ? "Manage Roles" : "Create Dynamic Role";
                btnClose.Location = new Point(350, 10);
                pnlFooter.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); } };

                FlowLayoutPanel flp = new FlowLayoutPanel { Location = new Point(30, 80), Size = new Size(340, type == "ManageRole" ? 400 : 330), FlowDirection = FlowDirection.TopDown, WrapContents = false };

                DarkComboBox cmbSelectRole = null;

                // Add ComboBox dynamically if in ManageMode
                if (type == "ManageRole")
                {
                    flp.Controls.Add(new Label { Text = "Select Role to Edit:", ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9F), AutoSize = true, Margin = new Padding(0, 0, 0, 5) });
                    RoundedPanel pnlSelect = new RoundedPanel { Size = new Size(320, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 7, 10, 7), Margin = new Padding(0, 0, 0, 15) };
                    cmbSelectRole = new DarkComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Cursor = Cursors.Hand };

                    cmbSelectRole.Items.AddRange(_userController.GetAllRoles().ToArray());
                    pnlSelect.Controls.Add(cmbSelectRole);
                    flp.Controls.Add(pnlSelect);
                }

                flp.Controls.Add(new Label { Text = "Role Name:", ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9F), AutoSize = true, Margin = new Padding(0, 0, 0, 5) });
                RoundedPanel pnlName = new RoundedPanel { Size = new Size(320, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8), Margin = new Padding(0, 0, 0, 20) };

                TextBox txtRoleName = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F) };

                // Keep RoleName ReadOnly during editing since it acts as the primary key
                if (type == "ManageRole") txtRoleName.ReadOnly = true;

                pnlName.Controls.Add(txtRoleName); flp.Controls.Add(pnlName);

                flp.Controls.Add(new Label { Text = "Module Permissions:", ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 0, 0, 10) });

                string[] modules = { "User Management", "Inventory Management", "Sales & POS", "Procurement", "Report Center", "Data Management" };
                List<CheckBox> checkBoxes = new List<CheckBox>();

                foreach (var mod in modules)
                {
                    CheckBox chk = new CheckBox { Text = mod, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10F), AutoSize = true, Margin = new Padding(10, 5, 0, 5), Cursor = Cursors.Hand };
                    checkBoxes.Add(chk);
                    flp.Controls.Add(chk);
                }

                // If ManageMode, hook up the logic to auto-populate fields when a role is chosen
                if (type == "ManageRole" && cmbSelectRole != null)
                {
                    cmbSelectRole.SelectedIndexChanged += (s, e) =>
                    {
                        string selected = cmbSelectRole.SelectedItem.ToString();
                        txtRoleName.Text = selected;

                        RolePermissions perms = _userController.GetRolePermissions(selected);
                        if (perms != null)
                        {
                            checkBoxes[0].Checked = perms.CanManageUsers;
                            checkBoxes[1].Checked = perms.CanManageInventory;
                            checkBoxes[2].Checked = perms.CanProcessSales;
                            checkBoxes[3].Checked = perms.CanManageProcurement;
                            checkBoxes[4].Checked = perms.CanViewReports;
                            checkBoxes[5].Checked = perms.CanManageData;
                        }
                    };
                    if (cmbSelectRole.Items.Count > 0) cmbSelectRole.SelectedIndex = 0; // Triggers the above event automatically
                }

                modal.Controls.Add(flp);

                Button btnAction = new Button { Text = type == "ManageRole" ? "Update Role" : "Save Role", Size = new Size(120, 38), FlatStyle = FlatStyle.Flat, BackColor = UITheme.AccentYellow, ForeColor = Color.Black, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(250, 15) };
                btnAction.FlatAppearance.BorderSize = 0; btnCancel.Location = new Point(140, 15);

                btnAction.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtRoleName.Text)) { MessageBox.Show("Please enter a role name.", "Requirement Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

                    RolePermissions perms = new RolePermissions
                    {
                        CanManageUsers = checkBoxes[0].Checked,
                        CanManageInventory = checkBoxes[1].Checked,
                        CanProcessSales = checkBoxes[2].Checked,
                        CanManageProcurement = checkBoxes[3].Checked,
                        CanViewReports = checkBoxes[4].Checked,
                        CanManageData = checkBoxes[5].Checked
                    };

                    if (type == "ManageRole")
                    {
                        if (_userController.UpdateRole(txtRoleName.Text.Trim(), perms, _activeUserId))
                        {
                            MessageBox.Show($"Role '{txtRoleName.Text.Trim()}' updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            modal.Close();
                        }
                        else { MessageBox.Show("Role update failed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    }
                    else
                    {
                        if (_userController.CreateRole(txtRoleName.Text.Trim(), perms, _activeUserId))
                        {
                            MessageBox.Show($"Role '{txtRoleName.Text.Trim()}' created successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            // Refresh Comboboxes
                            string currentSelection = cmbRoles.SelectedItem?.ToString();
                            cmbRoles.Items.Clear(); cmbRoles.Items.Add("All Roles");
                            cmbRoles.Items.AddRange(_userController.GetAllRoles().ToArray());
                            cmbRoles.SelectedItem = currentSelection ?? "All Roles";

                            modal.Close();
                        }
                        else { MessageBox.Show("Role creation failed. The role name might already exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                    }
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
            }
            else // Create, Edit, Details
            {
                // Extended height by 75px to fit the new Contact Number row beautifully
                modal.Size = new Size(650, type == "UserDetails" ? 450 : 425);
                lblTitle.Text = type == "CreateUser" ? "Add New User" : (type == "EditUser" ? "Edit User Details" : "User Account Details");
                btnClose.Location = new Point(600, 10);
                pnlFooter.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); } };

                int y = 80;

                UserModel uData = null;
                if (rowData != null)
                {
                    string targetId = rowData.Cells["UserID"].Value.ToString();
                    uData = _usersData.FirstOrDefault(u => u.UserID == targetId);
                }

                bool ro = type == "UserDetails";

                TextBox txtFirstName = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = ro, Text = uData != null ? uData.FirstName : "" };
                TextBox txtLastName = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = ro, Text = uData != null ? uData.LastName : "" };
                TextBox txtUsername = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = ro || type == "EditUser", Text = uData != null ? uData.Username : "" };

                // NEW: Contact Number Box
                TextBox txtContactNumber = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = ro, Text = uData?.ContactNumber ?? "" };

                TextBox txtPassword = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), UseSystemPasswordChar = true };
                DarkComboBox cmbUserRole = new DarkComboBox { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Cursor = Cursors.Hand };

                Action<string, Control, int, int, int> AddControlRow = (lblText, ctrl, xLoc, yLoc, w) =>
                {
                    Label l = new Label { Text = lblText, Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(xLoc, yLoc), AutoSize = true };
                    RoundedPanel p = new RoundedPanel { Location = new Point(xLoc, yLoc + 20), Size = new Size(w, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                    p.Controls.Add(ctrl); modal.Controls.AddRange(new Control[] { l, p });
                };

                // Row 1: Names
                AddControlRow("First Name", txtFirstName, 35, y, 280);
                AddControlRow("Last Name", txtLastName, 335, y, 280); y += 75;

                // Row 2: Username & Contact Number
                AddControlRow("Username", txtUsername, 35, y, 280);
                AddControlRow("Contact Number", txtContactNumber, 335, y, 280); y += 75;

                // Row 3: Role Setup (Moved to the left column)
                Label lblRole = new Label { Text = "User Role", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(35, y), AutoSize = true };
                RoundedPanel pnlRole = new RoundedPanel { Location = new Point(35, y + 20), Size = new Size(280, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 7, 10, 7) };

                if (ro)
                {
                    TextBox tRole = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = true, Text = uData != null ? uData.Role : "" };
                    pnlRole.Controls.Add(tRole);
                }
                else
                {
                    cmbUserRole.Items.AddRange(_userController.GetAllRoles().ToArray());
                    if (cmbUserRole.Items.Count > 0) cmbUserRole.SelectedIndex = 0;
                    if (type == "EditUser" && uData != null) cmbUserRole.SelectedItem = uData.Role;
                    pnlRole.Controls.Add(cmbUserRole);
                }
                modal.Controls.AddRange(new Control[] { lblRole, pnlRole });

                // Row 3 (Right Column): Password OR Reset Passkey
                if (type == "CreateUser")
                {
                    AddControlRow("Assign Initial Password", txtPassword, 335, y, 280);
                }
                else if (type == "EditUser" && uData != null)
                {
                    Label lblReset = new Label { Text = "Account Recovery", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(335, y), AutoSize = true };

                    // We use a red background to signify a destructive/reset action
                    Button btnResetPasskey = new Button { Text = "Reset Recovery Passkey", Size = new Size(280, 38), Location = new Point(335, y + 20), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                    btnResetPasskey.FlatAppearance.BorderSize = 0;

                    // HOOKED UP: Reset Passkey Logic
                    btnResetPasskey.Click += (s, e) =>
                    {
                        if (MessageBox.Show($"Are you sure you want to reset the passkey for {uData.Username}?\nThe old passkey will be permanently invalidated.", "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        {
                            string newPasskey = _userController.ResetUserPasskey(uData.UserID, _activeUserId);
                            if (!string.IsNullOrEmpty(newPasskey))
                            {
                                modal.Close(); // Close the edit modal
                                ShowPasskeyModal(uData.Username, newPasskey, "Passkey Reset Successfully!"); // Show the success modal
                                FetchUsersData(); // Refresh the grid data
                            }
                            else
                            {
                                MessageBox.Show("Failed to reset the passkey.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    };

                    modal.Controls.AddRange(new Control[] { lblReset, btnResetPasskey });
                }
                y += 75;

                if (type == "UserDetails" && uData != null)
                {
                    Label lblAudit = new Label { Text = "Audit Trail", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = UITheme.CurrentText, Location = new Point(35, y), AutoSize = true };
                    modal.Controls.Add(lblAudit); y += 25;

                    string cTime = uData.CreatedTime.HasValue ? uData.CreatedTime.Value.ToString("MMM dd, yyyy") : "Unknown";
                    string mTime = uData.LastModifiedTime.HasValue ? uData.LastModifiedTime.Value.ToString("MMM dd, yyyy") : "Never Modified";

                    Label lblAuditD1 = new Label { Text = $"Created By: {uData.CreatedBy ?? "System"} on {cTime}", Font = new Font("Segoe UI", 8.5F), ForeColor = UITheme.MutedText, Location = new Point(35, y), AutoSize = true };
                    Label lblAuditD2 = new Label { Text = $"Last Modified By: {uData.ModifiedBy ?? "None"} on {mTime}", Font = new Font("Segoe UI", 8.5F), ForeColor = UITheme.MutedText, Location = new Point(335, y), AutoSize = true };
                    modal.Controls.AddRange(new Control[] { lblAuditD1, lblAuditD2 });

                    btnCancel.Text = "Close";
                    btnCancel.Location = new Point((modal.Width - btnCancel.Width) / 2, 15);
                    pnlFooter.Controls.Add(btnCancel);
                }
                else
                {
                    Button btnAction = new Button { Text = type == "CreateUser" ? "Save User" : "Update User", Size = new Size(150, 38), FlatStyle = FlatStyle.Flat, BackColor = UITheme.AccentYellow, ForeColor = Color.Black, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(465, 15) };
                    btnAction.FlatAppearance.BorderSize = 0; btnCancel.Location = new Point(355, 15);

                    btnAction.Click += (s, e) =>
                    {
                        if (string.IsNullOrWhiteSpace(txtFirstName.Text) || string.IsNullOrWhiteSpace(txtLastName.Text) || string.IsNullOrWhiteSpace(txtUsername.Text))
                        {
                            MessageBox.Show("Please fill out all required fields.", "Requirement Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        if (type == "CreateUser")
                        {
                            if (string.IsNullOrWhiteSpace(txtPassword.Text)) { MessageBox.Show("Please provide an initial password.", "Requirement Missing"); return; }

                            UserModel newUser = new UserModel
                            {
                                UserID = _userController.GenerateNextUserID(),
                                FirstName = txtFirstName.Text.Trim(),
                                LastName = txtLastName.Text.Trim(),
                                ContactNumber = txtContactNumber.Text.Trim(), // Saved from field
                                Username = txtUsername.Text.Trim(),
                                Role = cmbUserRole.SelectedItem.ToString()
                            };

                            if (_userController.CreateUser(newUser, txtPassword.Text, _activeUserId))
                            {
                                // Remove old messagebox and trigger the dedicated passkey modal
                                modal.Close();
                                ShowPasskeyModal(newUser.Username, newUser.Passkey, "User Created Successfully!");
                                FetchUsersData();
                            }
                            else { MessageBox.Show("Failed to create user. The Username might already exist.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                        }
                        else if (type == "EditUser")
                        {
                            uData.FirstName = txtFirstName.Text.Trim();
                            uData.LastName = txtLastName.Text.Trim();
                            uData.ContactNumber = txtContactNumber.Text.Trim(); // Saved from field
                            uData.Role = cmbUserRole.SelectedItem.ToString();

                            if (_userController.UpdateUser(uData, _activeUserId))
                            {
                                MessageBox.Show("User details updated successfully.", "Update Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                FetchUsersData();
                                modal.Close();
                            }
                            else { MessageBox.Show("Failed to update user details.", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                        }
                    };
                    pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                }
            }

            modal.Controls.Add(pnlHeader); modal.Controls.Add(pnlFooter);
            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show(); modal.ShowDialog(overlay); overlay.Dispose();
        }

        // =========================================================================
        // PDF GENERATION LOGIC (Adapted from ReportView)
        // =========================================================================
        private void GeneratePDF()
        {
            if (_logsData == null || _logsData.Count == 0)
            {
                MessageBox.Show("There are no activity logs to export.", "Empty Report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PrintDocument pd = new PrintDocument();
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            pd.DefaultPageSettings.Landscape = true; // Use Landscape for wider columns

            // FIX: Rigidly reset the index exactly when the document starts building 
            // (This perfectly solves the PrintPreviewDialog double-render pagination bug)
            pd.BeginPrint += (s, e) => { _pdfPrintIndex = 0; };
            pd.PrintPage += Pd_PrintPage;

            PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd, Width = 1000, Height = 800 };
            ppd.ShowDialog();
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            int y = 50;

            Font fTitle = new Font("Arial", 20, FontStyle.Bold);
            Font fSub = new Font("Arial", 11, FontStyle.Bold);
            Font fN = new Font("Arial", 10);
            Font fB = new Font("Arial", 10, FontStyle.Bold);

            // Report Header
            g.DrawString("SJ PC STORE - SYSTEM REPORT", fTitle, Brushes.Black, 50, y); y += 35;
            g.DrawString("Report Module: ACTIVITY LOGS", fSub, Brushes.DarkBlue, 50, y); y += 20;
            g.DrawString($"Generated On: {DateTime.Now:MMM dd, yyyy HH:mm:ss}", fN, Brushes.DimGray, 50, y); y += 30;

            g.DrawLine(Pens.Black, 50, y, 1100, y); y += 20;

            PrintLogsRows(g, e, ref y, fB, fN);
        }

        private void PrintLogsRows(Graphics g, PrintPageEventArgs e, ref int y, Font fB, Font fN)
        {
            // Format to strictly prevent vertical text wrapping 
            StringFormat noWrapFormat = new StringFormat { FormatFlags = StringFormatFlags.NoWrap };

            // Table Header (Customly spaced for Log Data)
            g.FillRectangle(Brushes.DarkBlue, 50, y, 1050, 30);
            g.DrawString("USER ID", fB, Brushes.White, 60, y + 7);
            g.DrawString("FULL NAME", fB, Brushes.White, 160, y + 7);
            g.DrawString("CATEGORY", fB, Brushes.White, 350, y + 7);
            g.DrawString("ACTION DESCRIPTION", fB, Brushes.White, 520, y + 7);
            g.DrawString("LOG DATE", fB, Brushes.White, 930, y + 7);
            y += 40;

            // Rows with Pagination logic
            while (_pdfPrintIndex < _logsData.Count)
            {
                var log = _logsData[_pdfPrintIndex];

                // FIX: Strip hidden newline characters to prevent rows from vertically bleeding into each other
                string desc = log.ActionDescription.Replace("\n", " ").Replace("\r", " ");
                if (desc.Length > 60) desc = desc.Substring(0, 57) + "...";

                g.DrawString(log.UserID, fN, Brushes.Black, 60, y, noWrapFormat);
                g.DrawString(log.FullName, fN, Brushes.Black, 160, y, noWrapFormat);
                g.DrawString(log.ModuleCategory, fN, Brushes.Black, 350, y, noWrapFormat);
                g.DrawString(desc, fN, Brushes.Black, 520, y, noWrapFormat);
                g.DrawString(log.LogDate.ToString("MMM dd, yyyy - hh:mm tt"), fN, Brushes.Black, 930, y, noWrapFormat);

                y += 30; _pdfPrintIndex++;

                // FIX: Lowered margin threshold to 710 to ensure it safely triggers before running out of bounds
                if (y > 710) { e.HasMorePages = true; return; }
            }

            e.HasMorePages = false;

            // Print the Log Count Footer
            y += 10;
            g.DrawLine(Pens.Gray, 50, y, 1100, y); y += 20;
            g.DrawString("TOTAL LOGS EXTRACTED:", fB, Brushes.DarkBlue, 700, y);
            g.DrawString(_logsData.Count.ToString(), fB, Brushes.Black, 930, y);
        }

        // =========================================================================
        // UTILITY GENERATORS
        // =========================================================================
        private void SwitchTab(string tabName)
        {
            pnlUsersTab.Visible = tabName == "Users";
            pnlLogsTab.Visible = tabName == "Logs";
            btnTabUsers.IsActive = tabName == "Users";
            btnTabLogs.IsActive = tabName == "Logs";

            if (tabName == "Users") { FetchUsersData(); }
            else if (tabName == "Logs") { FetchLogsData(); }
            ApplyTheme();
        }

        private TabButton CreateTab(string text, IconChar icon) { return new TabButton { Text = "  " + text, IconChar = icon, IconSize = 22, Size = new Size(250, 52), Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleCenter, ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Padding = new Padding(20, 0, 0, 0) }; }
        private Control CreateInputWrapper(Control innerControl, int width) { RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 0, 10, 0) }; innerControl.Dock = DockStyle.Fill; wrapper.Controls.Add(innerControl); _inputWrappers.Add(wrapper); return wrapper; }
        private Control CreateSearchInput(string placeholder, int width, out TextBox txtOut, Action clearAction) { RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(12, 8, 8, 8), Margin = new Padding(0, 0, 10, 0) }; IconPictureBox icon = new IconPictureBox { IconChar = IconChar.Search, IconSize = 18, Size = new Size(24, 18), Dock = DockStyle.Left, BackColor = Color.Transparent }; TextBox txt = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5F), Text = placeholder }; IconPictureBox clearIcon = new IconPictureBox { IconChar = IconChar.Times, IconSize = 16, Size = new Size(20, 18), Dock = DockStyle.Right, BackColor = Color.Transparent, Cursor = Cursors.Hand }; txt.GotFocus += (s, e) => { if (txt.Text == placeholder) txt.Text = ""; }; txt.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) txt.Text = placeholder; }; clearIcon.Click += (s, e) => { txt.Text = placeholder; txt.Parent.Focus(); clearAction(); }; clearIcon.MouseEnter += (s, e) => clearIcon.IconColor = Color.FromArgb(239, 68, 68); clearIcon.MouseLeave += (s, e) => clearIcon.IconColor = UITheme.CurrentIcon; wrapper.Controls.Add(clearIcon); wrapper.Controls.Add(txt); wrapper.Controls.Add(icon); _inputWrappers.Add(wrapper); _textInputs.Add(txt); txtOut = txt; return wrapper; }
        private Control CreateComboInput(string[] items, int width, out DarkComboBox cmbOut) { RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(10, 7, 10, 7), Margin = new Padding(0, 0, 10, 0) }; DarkComboBox cmb = new DarkComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Cursor = Cursors.Hand }; cmb.Items.AddRange(items); if (cmb.Items.Count > 0) cmb.SelectedIndex = 0; wrapper.Controls.Add(cmb); _inputWrappers.Add(wrapper); _comboInputs.Add(cmb); cmbOut = cmb; return wrapper; }
        private IconButton CreateButton(string text, IconChar icon, string type) { IconButton btn = new IconButton { Text = text != "" ? "  " + text : "", IconChar = icon, IconSize = 18, Height = 38, AutoSize = true, Padding = new Padding(15, 0, 15, 0), Margin = new Padding(10, 0, 0, 0), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText, Tag = type }; btn.FlatAppearance.BorderSize = 0; if (type == "ActionAdd") btn.Padding = new Padding(10, 0, 10, 0); _buttons.Add(btn); return btn; }
        private Panel CreatePaginationPanel(ref Label lblState, Action onPrev, Action onNext)
        { Panel pnl = new Panel { Dock = DockStyle.Bottom, Height = 200, Padding = new Padding(20, 10, 20, 0) }; lblState = new Label { Text = "Page 1 of 1", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), AutoSize = true, ForeColor = UITheme.CurrentText, Location = new Point(20, 35) }; IconButton btnPrev = CreateButton("Previous", IconChar.ChevronLeft, "Secondary"); btnPrev.Location = new Point(pnl.Width - 250, 25); btnPrev.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnPrev.Click += (s, e) => onPrev(); IconButton btnNext = CreateButton("Next", IconChar.ChevronRight, "Secondary"); btnNext.TextImageRelation = TextImageRelation.TextBeforeImage; btnNext.Location = new Point(pnl.Width - 120, 25); btnNext.Anchor = AnchorStyles.Top | AnchorStyles.Right; btnNext.Click += (s, e) => onNext(); pnl.Controls.AddRange(new Control[] { lblState, btnPrev, btnNext }); return pnl; }

        private SmoothGrid CreateDataGridView()
        {
            SmoothGrid dgv = new SmoothGrid
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 50,
                RowTemplate = { Height = 55 },
                Cursor = Cursors.Hand,
                RowHeadersVisible = false // Re-enabled explicitly to apply custom RowHeader styling 
            };
            dgv.DefaultCellStyle.Padding = new Padding(20, 0, 0, 0); dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(20, 0, 0, 0);
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.SelectionChanged += (s, e) => dgv.ClearSelection();
            return dgv;
        }

        // =========================================================================
        // DATA FETCHING & RENDERING ENGINE
        // =========================================================================

        private void FetchUsersData()
        {
            // Fetch the raw list from the database
            var allUsers = _userController.GetAllUsers();

            // Get current filter states
            string search = txtSearchUser.Text == "Search User..." ? "" : txtSearchUser.Text.ToLower();
            string roleFilter = cmbRoles.SelectedItem?.ToString() ?? "All Roles";

            // Apply LINQ filtering for search and dropdowns
            _usersData = allUsers.Where(u =>
                (string.IsNullOrEmpty(search) ||
                 u.Username.ToLower().Contains(search) ||
                 $"{u.FirstName} {u.LastName}".ToLower().Contains(search)) &&
                (roleFilter == "All Roles" || u.Role == roleFilter)
            ).ToList();

            // Pass the filtered data to the Sort Engine instead of rendering immediately
            ApplyUserSort();
        }

        private void ApplyUserSort()
        {
            // Perform LINQ Ordering based on the current Sort State
            if (_userSortIndex == 0)      // Newest First
                _usersData = _usersData.OrderByDescending(u => u.CreatedTime ?? DateTime.MinValue).ToList();
            else if (_userSortIndex == 1) // Oldest First
                _usersData = _usersData.OrderBy(u => u.CreatedTime ?? DateTime.MinValue).ToList();
            else if (_userSortIndex == 2) // Name A-Z
                _usersData = _usersData.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
            else if (_userSortIndex == 3) // Name Z-A
                _usersData = _usersData.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName).ToList();

            _userPage = 0; // Reset pagination when sorting changes
            RenderUsersGrid();
        }

        private void RenderUsersGrid()
        {
            dgvUsers.Rows.Clear();

            // LINQ Pagination logic
            var pageData = _usersData.Skip(_userPage * PAGE_SIZE).Take(PAGE_SIZE).ToList();

            foreach (var user in pageData)
            {
                dgvUsers.Rows.Add(
                    user.UserID,
                    $"{user.FirstName} {user.LastName}",
                    user.Role,
                    user.Status
                );
            }

            // Update Pagination Label
            if (_usersData.Count == 0) lblUserPage.Text = "No user accounts found.";
            else
            {
                int totalPages = (int)Math.Ceiling(_usersData.Count / (double)PAGE_SIZE);
                lblUserPage.Text = $"Page {_userPage + 1} of {totalPages} (Total Users: {_usersData.Count})";
            }
        }

        private void FetchLogsData()
        {
            string search = txtSearchLog.Text;
            string category = cmbModuleCategory.SelectedItem?.ToString() ?? "All Modules";

            _logsData = _logController.GetFilteredLogs(dtpLogFrom.Value, dtpLogTo.Value, category, search);
            _logPage = 0; // Reset pagination when new filters apply
            RenderLogsGrid();
        }

        private void RenderLogsGrid()
        {
            dgvLogs.Rows.Clear();

            // LINQ Pagination logic
            var pageData = _logsData.Skip(_logPage * PAGE_SIZE).Take(PAGE_SIZE).ToList();

            foreach (var log in pageData)
            {
                dgvLogs.Rows.Add(
                    log.UserID,
                    log.FullName,
                    log.ModuleCategory,
                    log.ActionDescription,
                    log.LogDate.ToString("MMM dd, yyyy - hh:mm tt")
                );
            }

            // Update Pagination Label
            if (_logsData.Count == 0) lblLogPage.Text = "No activity logs found.";
            else
            {
                int totalPages = (int)Math.Ceiling(_logsData.Count / (double)PAGE_SIZE);
                lblLogPage.Text = $"Page {_logPage + 1} of {totalPages} (Total Logs: {_logsData.Count})";
            }
        }

        // =========================================================================
        // THEME ENGINE
        // =========================================================================
        public void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace; pnlTabs.BackColor = UITheme.CurrentWorkspace; pnlContent.BackColor = UITheme.CurrentWorkspace;

            if (pnlUsersTab != null) { pnlUsersTab.BackColor = UITheme.CurrentWorkspace; pnlUsersToolbar.BackColor = UITheme.CurrentPanel; pnlUsersToolbar.BorderColor = UITheme.CurrentBorder; }
            if (pnlLogsTab != null) { pnlLogsTab.BackColor = UITheme.CurrentWorkspace; pnlLogsToolbar.BackColor = UITheme.CurrentPanel; pnlLogsToolbar.BorderColor = UITheme.CurrentBorder; }

            Color hoverColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : UITheme.CurrentPanel;

            btnTabUsers.BackColor = btnTabUsers.IsActive ? UITheme.CurrentPanel : Color.Transparent;
            btnTabLogs.BackColor = btnTabLogs.IsActive ? UITheme.CurrentPanel : Color.Transparent;

            btnTabUsers.ForeColor = btnTabUsers.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabUsers.IconColor = btnTabUsers.ForeColor;
            btnTabLogs.ForeColor = btnTabLogs.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabLogs.IconColor = btnTabLogs.ForeColor;

            foreach (RoundedPanel wrap in _inputWrappers) { wrap.BackColor = UITheme.CurrentInputBg; wrap.BorderColor = UITheme.CurrentBorder; foreach (Control c in wrap.Controls) { if (c is IconPictureBox icon) icon.IconColor = UITheme.CurrentIcon; } }
            foreach (TextBox txt in _textInputs) { txt.BackColor = UITheme.CurrentInputBg; txt.ForeColor = UITheme.CurrentText; }
            foreach (DarkComboBox cmb in _comboInputs) { cmb.BackColor = UITheme.CurrentInputBg; cmb.ForeColor = UITheme.CurrentText; }

            foreach (IconButton btn in _buttons)
            {
                string type = btn.Tag.ToString();
                if (type == "ActionAdd") { btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.SecondaryDark; btn.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : Color.FromArgb(45, 42, 50); }
                else if (type == "Primary") { btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btn.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : UITheme.SecondaryDark; }
                else if (type == "Secondary") { btn.BackColor = UITheme.IsDarkMode ? UITheme.SecondaryDark : UITheme.CurrentPanel; btn.ForeColor = UITheme.IsDarkMode ? Color.White : UITheme.CurrentText; if (!UITheme.IsDarkMode) { btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = UITheme.CurrentBorder; } else btn.FlatAppearance.BorderSize = 0; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? UITheme.PrimaryDark : Color.FromArgb(230, 230, 230); }
                btn.IconColor = btn.ForeColor; btn.FlatAppearance.MouseDownBackColor = btn.BackColor;
            }

            if (lblUserPage != null) lblUserPage.ForeColor = UITheme.CurrentText;
            if (lblLogPage != null) lblLogPage.ForeColor = UITheme.CurrentText;

            if (dtpLogFrom != null) dtpLogFrom.ApplyTheme();
            if (dtpLogTo != null) dtpLogTo.ApplyTheme();

            StyleGridTheme(dgvUsers); StyleGridTheme(dgvLogs);
            this.Invalidate(true);
        }

        private void StyleGridTheme(DataGridView dgv)
        {
            if (dgv == null) return;
            dgv.BackgroundColor = UITheme.CurrentPanel; dgv.GridColor = UITheme.CurrentBorder;

            dgv.DefaultCellStyle.BackColor = UITheme.CurrentPanel; dgv.DefaultCellStyle.ForeColor = UITheme.CurrentText;
            dgv.DefaultCellStyle.SelectionBackColor = UITheme.CurrentPanel; dgv.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;

            // Integrating Header Preferences
            Color customHeaderColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
            dgv.ColumnHeadersDefaultCellStyle.BackColor = customHeaderColor;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = customHeaderColor;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (dgv.Columns.Contains("Status") && row.Cells["Status"].Value != null)
                {
                    string status = row.Cells["Status"].Value.ToString();
                    if (status == "Active") row.Cells["Status"].Style.ForeColor = Color.FromArgb(16, 185, 129);
                    else if (status == "Inactive") row.Cells["Status"].Style.ForeColor = Color.FromArgb(239, 68, 68);
                }
            }
        }

        private void ShowPasskeyModal(string username, string passkey, string title)
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
            Label lblTitle = new Label { Text = title, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
            lblTitle.Location = new Point((modal.Width - lblTitle.PreferredWidth) / 2, 100);

            Label lblDesc = new Label { Text = $"Account for '{username}' is ready.\nSecurely store the recovery passkey below:", Font = new Font("Segoe UI", 9.5F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
            lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 135);

            // Highlighted Passkey Box
            RoundedPanel pnlPasskey = new RoundedPanel { Size = new Size(250, 50), BorderRadius = 6, BorderSize = 2, BorderColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, BackColor = UITheme.CurrentInputBg, Location = new Point((modal.Width - 250) / 2, 185) };
            Label lblPasskey = new Label { Text = passkey, Font = new Font("Consolas", 18F, FontStyle.Bold), ForeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
            pnlPasskey.Controls.Add(lblPasskey);

            Label lblWarn = new Label { Text = "⚠️ This passkey will not be shown again.", Font = new Font("Segoe UI", 8.5F, FontStyle.Italic), ForeColor = Color.FromArgb(239, 68, 68), AutoSize = true };
            lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 245);

            Button btnGotIt = new Button { Text = "Copy & Close", Size = new Size(150, 40), FlatStyle = FlatStyle.Flat, BackColor = UITheme.AccentYellow, ForeColor = Color.Black, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point((modal.Width - 150) / 2, 285) };
            btnGotIt.FlatAppearance.BorderSize = 0;

            btnGotIt.Click += (s, e) =>
            {
                Clipboard.SetText(passkey); // Adds QoL copying feature
                modal.Close();
            };

            modal.Controls.AddRange(new Control[] { iconSuccess, lblTitle, lblDesc, pnlPasskey, lblWarn, btnGotIt });

            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show(); modal.ShowDialog(overlay); overlay.Dispose();
        }
    }
}