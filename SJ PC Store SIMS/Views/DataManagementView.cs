using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace SJ_PC_Store_SIMS.Views
{
    public class DataManagementView : System.Windows.Forms.UserControl
    {
        // =========================================================================
        // STRICT HTML CSS REPLICATION ENGINES
        // =========================================================================
        private class ModalForm : Form { protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } } }
        private class SmoothPanel : Panel { public SmoothPanel() { this.DoubleBuffered = true; this.ResizeRedraw = true; } }
        private class SmoothGrid : DataGridView { public SmoothGrid() { this.DoubleBuffered = true; } }

        private class BufferedFlowLayoutPanel : FlowLayoutPanel
        {
            public BufferedFlowLayoutPanel()
            {
                this.DoubleBuffered = true;
                this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            }
            protected override CreateParams CreateParams
            {
                get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; }
            }
        }

        private class RoundedPanel : Panel
        {
            public int BorderRadius { get; set; } = 6; public int BorderSize { get; set; } = 1; public Color BorderColor { get; set; } = Color.Transparent;
            public RoundedPanel() { this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); this.BackColor = Color.Transparent; this.ResizeRedraw = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (this.Width <= 1 || this.Height <= 1) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = BorderRadius;
                    if (r <= 0) { path.AddRectangle(rect); }
                    else
                    {
                        path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
                        path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90); path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90); path.CloseFigure();
                    }
                    using (SolidBrush brush = new SolidBrush(this.BackColor)) { e.Graphics.FillPath(brush, path); }
                    if (BorderSize > 0) { using (Pen pen = new Pen(BorderColor, BorderSize)) { e.Graphics.DrawPath(pen, path); } }
                }
            }
        }

        private class TabLabel : Label
        {
            public bool IsActive { get; set; } = false;
            public TabLabel() { this.AutoSize = false; this.Size = new Size(200, 45); this.TextAlign = ContentAlignment.MiddleCenter; this.Cursor = Cursors.Hand; this.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold); }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                if (IsActive)
                {
                    Color activeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                    using (Pen p = new Pen(activeColor, 3)) { e.Graphics.DrawLine(p, 0, this.Height - 3, this.Width, this.Height - 3); }
                }
            }
        }

        private class BadgeLabel : Label
        {
            public Color BgTint { get; set; }
            public BadgeLabel() { this.AutoSize = false; this.Size = new Size(70, 26); this.TextAlign = ContentAlignment.MiddleCenter; this.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); this.BackColor = Color.Transparent; }
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    int d = this.Height - 1;
                    path.AddArc(0, 0, d, d, 90, 180); path.AddArc(this.Width - d - 1, 0, d, d, 270, 180); path.CloseFigure();
                    using (SolidBrush b = new SolidBrush(BgTint)) { e.Graphics.FillPath(b, path); }
                }
                TextRenderer.DrawText(e.Graphics, this.Text, this.Font, new Rectangle(0, 0, this.Width, this.Height), this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private class DarkComboBox : ComboBox
        {
            public DarkComboBox() { this.DrawMode = DrawMode.OwnerDrawFixed; this.FlatStyle = FlatStyle.Flat; }
            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                if (e.Index < 0) return;
                e.DrawBackground();
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                Color bg = isSelected ? (UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : UITheme.PrimaryDark) : this.BackColor;
                Color fg = isSelected ? (UITheme.IsDarkMode ? UITheme.AccentYellow : Color.White) : this.ForeColor;
                using (SolidBrush bgBrush = new SolidBrush(bg))
                using (SolidBrush fgBrush = new SolidBrush(fg))
                {
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);
                    e.Graphics.DrawString(this.Items[e.Index].ToString(), this.Font, fgBrush, e.Bounds.X + 5, e.Bounds.Y + 2);
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
                        Rectangle arrowRect = new Rectangle(this.Width - 20, 0, 20, this.Height);
                        using (SolidBrush b = new SolidBrush(this.BackColor)) { g.FillRectangle(b, arrowRect); }
                        using (Pen p = new Pen(this.ForeColor, 2))
                        {
                            int cx = arrowRect.X + 8; int cy = arrowRect.Y + (this.Height / 2) - 1;
                            g.DrawLine(p, cx - 4, cy - 2, cx, cy + 2); g.DrawLine(p, cx, cy + 2, cx + 4, cy - 2);
                        }
                        using (Pen borderPen = new Pen(this.BackColor, 2)) { g.DrawRectangle(borderPen, 0, 0, Width, Height); }
                    }
                }
            }
        }

        // =========================================================================
        // VIEW VARIABLES
        // =========================================================================
        private DataManagementController _dmController;
        private string _activeUserId;
        private SupplierModel _selectedSupplier;
        private bool _sortAsc = true;

        private SmoothPanel pnlMaster, pnlDetail, pnlOverviewTab, pnlTransactionsTab;
        private RoundedPanel pnlDetailHeader;
        private Panel pnlTabRow;
        private BufferedFlowLayoutPanel flpSuppliers;
        private DarkComboBox cmbFilter;
        private TextBox txtSearch;
        private IconButton btnSort, btnAdd, btnEdit, btnDeact, btnActivate;
        private SmoothGrid dgvPOHistory;

        private List<Control> _dynamicTexts = new List<Control>();
        private List<Control> _mutedTexts = new List<Control>();
        private List<RoundedPanel> _inputWrappers = new List<RoundedPanel>();
        private List<RoundedPanel> _borderedContainers = new List<RoundedPanel>();
        private List<TextBox> _textInputs = new List<TextBox>();
        private List<DarkComboBox> _comboInputs = new List<DarkComboBox>(); // FIX: Re-added the missing list declaration
        private List<IconButton> _buttons = new List<IconButton>();
        private List<Panel> _lines = new List<Panel>();

        private Label lblDetName, lblDetID;
        private BadgeLabel badgeStatus;
        private IconPictureBox iconHandshake;
        private TabLabel tabOverview, tabTransactions;
        private POHistoryModel _hoveredPO;

        private List<SupplierModel> _allSuppliers = new List<SupplierModel>();

        public DataManagementView(string currentUserId)
        {
            _activeUserId = currentUserId;
            _dmController = new DataManagementController();

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.Padding = new Padding(35, 20, 35, 35);
            this.Margin = new Padding(0);

            InitializeUI();
            ApplyTheme();
            LoadSuppliers();
        }

        private void LogAndNotify(string title, string message, bool isSuccess)
        {
            _dmController.LogActivity(_activeUserId, $"{title} - {message}", "Data Management");

            if (this.FindForm() is DashboardForm dash)
            {
                dash.AddNotification(title, message, isSuccess);
                var refreshMethod = dash.GetType().GetMethod("LoadDashboardData", BindingFlags.NonPublic | BindingFlags.Instance);
                refreshMethod?.Invoke(dash, null);
            }
            ShowToast(message, isSuccess);
        }

        private void ShowToast(string msg, bool success)
        {
            Form parent = this.FindForm();
            if (parent == null) return;

            // Calculate dynamic size based on text length
            Font msgFont = new Font("Segoe UI", 10F, FontStyle.Bold);
            int textWidth = TextRenderer.MeasureText(msg, msgFont).Width;
            int toastWidth = Math.Max(350, textWidth + 100);

            // Modern color palette
            Color accentColor = success ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
            Color bgColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.White;

            Form toast = new Form
            {
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                BackColor = bgColor,
                Size = new Size(toastWidth, 60),
                TopMost = true,
                ShowInTaskbar = false,
                Opacity = 0 // Starts invisible for the fade-in animation
            };

            // Relocate to the TOP-RIGHT of the workspace
            int xLoc = parent.Right - toastWidth - 30;
            int yLoc = parent.Top + 50;
            toast.Location = new Point(xLoc, yLoc);

            // Custom Paint for a sleek, modern UI (Left accent bar + subtle outer border)
            toast.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Subtle outer border
                using (Pen borderPen = new Pen(UITheme.CurrentBorder, 1))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, toast.Width - 1, toast.Height - 1);
                }

                // Heavy left accent line for quick status recognition
                using (SolidBrush accentBrush = new SolidBrush(accentColor))
                {
                    e.Graphics.FillRectangle(accentBrush, 0, 0, 6, toast.Height);
                }
            };

            // Status Icon
            IconPictureBox icon = new IconPictureBox
            {
                IconChar = success ? IconChar.CheckCircle : IconChar.ExclamationCircle,
                IconColor = accentColor,
                IconSize = 28,
                Size = new Size(28, 28),
                Location = new Point(20, 16),
                BackColor = Color.Transparent
            };

            // Message Label
            Label lbl = new Label
            {
                Text = msg,
                ForeColor = UITheme.CurrentText,
                Font = msgFont,
                AutoSize = true,
                Location = new Point(55, 19),
                BackColor = Color.Transparent
            };

            // Interactive Close Button
            IconPictureBox closeIcon = new IconPictureBox
            {
                IconChar = IconChar.Times,
                IconColor = UITheme.MutedText,
                IconSize = 16,
                Size = new Size(16, 16),
                Location = new Point(toast.Width - 30, 22),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            // Smooth hover effects and click-to-close for the X button
            closeIcon.MouseEnter += (s, e) => closeIcon.IconColor = Color.FromArgb(239, 68, 68);
            closeIcon.MouseLeave += (s, e) => closeIcon.IconColor = UITheme.MutedText;

            toast.Controls.AddRange(new Control[] { icon, lbl, closeIcon });

            // Animation & Lifecycle Timers
            System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer { Interval = 15 };
            System.Windows.Forms.Timer holdTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            bool isFadingIn = true;

            // Manual close logic
            closeIcon.Click += (s, e) =>
            {
                holdTimer.Stop();
                isFadingIn = false;
                fadeTimer.Start();
            };

            // Opacity interpolator
            fadeTimer.Tick += (s, e) =>
            {
                if (isFadingIn)
                {
                    if (toast.Opacity < 1) toast.Opacity += 0.1;
                    else { fadeTimer.Stop(); holdTimer.Start(); }
                }
                else
                {
                    if (toast.Opacity > 0) toast.Opacity -= 0.1;
                    else { fadeTimer.Stop(); toast.Close(); }
                }
            };

            holdTimer.Tick += (s, e) =>
            {
                holdTimer.Stop();
                isFadingIn = false;
                fadeTimer.Start(); // Trigger fade out
            };

            toast.Show();
            fadeTimer.Start(); // Trigger fade in
        }

        protected override void OnParentChanged(EventArgs e) { base.OnParentChanged(e); if (this.Parent != null) { this.Parent.BackColorChanged -= Parent_BackColorChanged; this.Parent.BackColorChanged += Parent_BackColorChanged; } }
        private void Parent_BackColorChanged(object sender, EventArgs e) { ApplyTheme(); }

        private Control CreateSearchInput(string placeholder, int width, out TextBox txtOut, Action clearAction)
        {
            RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(12, 8, 8, 8), Margin = new Padding(0, 0, 10, 0) };
            IconPictureBox icon = new IconPictureBox { IconChar = IconChar.Search, IconSize = 18, Size = new Size(24, 18), Dock = DockStyle.Left, BackColor = Color.Transparent };
            TextBox txt = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5F), Text = placeholder };

            IconPictureBox clearIcon = new IconPictureBox { IconChar = IconChar.Times, IconSize = 16, Size = new Size(20, 18), Dock = DockStyle.Right, BackColor = Color.Transparent, Cursor = Cursors.Hand };

            txt.GotFocus += (s, e) => { if (txt.Text == placeholder) txt.Text = ""; };
            txt.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) txt.Text = placeholder; };
            clearIcon.Click += (s, e) => { txt.Text = placeholder; txt.Parent.Focus(); clearAction(); };
            clearIcon.MouseEnter += (s, e) => clearIcon.IconColor = Color.FromArgb(239, 68, 68);
            clearIcon.MouseLeave += (s, e) => clearIcon.IconColor = UITheme.CurrentIcon;

            wrapper.Controls.Add(clearIcon); wrapper.Controls.Add(txt); wrapper.Controls.Add(icon);
            _inputWrappers.Add(wrapper); _textInputs.Add(txt);
            txtOut = txt; return wrapper;
        }

        private void InitializeUI()
        {
            // --- LEFT PANE (MASTER LIST) ---
            pnlMaster = new SmoothPanel { Dock = DockStyle.Left, Width = 380, Padding = new Padding(0, 0, 20, 0) };
            RoundedPanel masterContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, BorderSize = 1, Padding = new Padding(1) };
            _borderedContainers.Add(masterContainer);

            SmoothPanel pnlMasterToolbar = new SmoothPanel { Dock = DockStyle.Top, Height = 120, BackColor = Color.Transparent };
            pnlMasterToolbar.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 119, pnlMasterToolbar.Width, 119); } };

            cmbFilter = new DarkComboBox { Location = new Point(15, 15), Size = new Size(230, 36), Font = new Font("Segoe UI", 12F, FontStyle.Bold), Cursor = Cursors.Hand };
            cmbFilter.Items.AddRange(new[] { "Active Suppliers", "Inactive Suppliers", "All Suppliers" });
            cmbFilter.SelectedIndex = 2; // Default to All Suppliers
            cmbFilter.SelectedIndexChanged += (s, e) => LoadSuppliers();
            _comboInputs.Add(cmbFilter);

            btnSort = new IconButton { IconChar = IconChar.SortAlphaDown, IconSize = 18, Size = new Size(36, 36), Location = new Point(260, 15), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Secondary" };
            btnSort.Click += (s, e) => { _sortAsc = !_sortAsc; btnSort.IconChar = _sortAsc ? IconChar.SortAlphaDown : IconChar.SortAlphaUp; LoadSuppliers(); };

            btnAdd = new IconButton { IconChar = IconChar.Plus, IconSize = 18, Size = new Size(36, 36), Location = new Point(305, 15), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "ActionAdd" };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) => OpenModal("Create");
            _buttons.Add(btnSort); _buttons.Add(btnAdd);

            Control searchWrapper = CreateSearchInput("Search supplier...", 326, out txtSearch, () => RenderMasterList());
            searchWrapper.Location = new Point(15, 65);
            txtSearch.TextChanged += (s, e) => RenderMasterList();

            pnlMasterToolbar.Controls.AddRange(new Control[] { cmbFilter, btnSort, btnAdd, searchWrapper });

            flpSuppliers = new BufferedFlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0), Margin = new Padding(0) };

            flpSuppliers.Paint += (s, e) =>
            {
                if (_allSuppliers.Count == 0)
                {
                    TextRenderer.DrawText(e.Graphics, "No suppliers in database.\nClick '+' to register one.", new Font("Segoe UI", 10.5F, FontStyle.Italic), flpSuppliers.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else if (flpSuppliers.Controls.Count == 0)
                {
                    TextRenderer.DrawText(e.Graphics, "No matching suppliers found.", new Font("Segoe UI", 10.5F, FontStyle.Italic), flpSuppliers.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            masterContainer.Controls.Add(flpSuppliers); masterContainer.Controls.Add(pnlMasterToolbar);
            pnlMaster.Controls.Add(masterContainer);

            // --- RIGHT PANE (DETAILS) ---
            pnlDetail = new SmoothPanel { Dock = DockStyle.Fill };
            RoundedPanel detailContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, BorderSize = 1, Padding = new Padding(1) };
            _borderedContainers.Add(detailContainer);

            SmoothPanel pnlHeaderWrapper = new SmoothPanel { Dock = DockStyle.Top, Height = 105, BackColor = Color.Transparent };
            pnlHeaderWrapper.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 104, pnlHeaderWrapper.Width, 104); } };
            pnlDetailHeader = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, BorderSize = 0 };

            iconHandshake = new IconPictureBox { IconChar = IconChar.Handshake, IconSize = 55, Size = new Size(60, 60), Location = new Point(30, 25), BackColor = Color.Transparent };
            lblDetName = new Label { Text = "Select a Supplier", Font = new Font("Segoe UI", 20F, FontStyle.Bold), AutoSize = true, Location = new Point(95, 20) };

            badgeStatus = new BadgeLabel { Text = "Active", Location = new Point(100, 58) };
            lblDetID = new Label { Text = "SUP-XXXX", Font = new Font("Consolas", 10.5F, FontStyle.Bold), AutoSize = true, Location = new Point(180, 62) };

            btnEdit = new IconButton { Text = "  Edit", IconChar = IconChar.Pen, IconSize = 18, Size = new Size(100, 38), Location = new Point(detailContainer.Width - 275, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Secondary", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText };

            btnDeact = new IconButton { Text = "  Deactivate", IconChar = IconChar.Ban, IconSize = 18, Size = new Size(130, 38), Location = new Point(detailContainer.Width - 165, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Danger", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText };
            btnDeact.MouseEnter += (s, e) => { btnDeact.ForeColor = Color.White; btnDeact.IconColor = Color.White; };
            btnDeact.MouseLeave += (s, e) => { btnDeact.ForeColor = Color.FromArgb(239, 68, 68); btnDeact.IconColor = Color.FromArgb(239, 68, 68); };

            btnActivate = new IconButton { Text = "  Activate", IconChar = IconChar.Undo, IconSize = 18, Size = new Size(115, 38), Location = new Point(detailContainer.Width - 165, 30), Anchor = AnchorStyles.Top | AnchorStyles.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Success", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText };
            btnActivate.MouseEnter += (s, e) => { btnActivate.ForeColor = Color.White; btnActivate.IconColor = Color.White; };
            btnActivate.MouseLeave += (s, e) => { btnActivate.ForeColor = Color.FromArgb(16, 185, 129); btnActivate.IconColor = Color.FromArgb(16, 185, 129); };

            btnEdit.Click += (s, e) => { if (_selectedSupplier != null) OpenModal("Edit"); };
            btnDeact.Click += (s, e) => { if (_selectedSupplier != null && _selectedSupplier.IsActive) OpenModal("Deactivate"); };
            btnActivate.Click += (s, e) => { if (_selectedSupplier != null && !_selectedSupplier.IsActive) OpenModal("Activate"); };

            pnlDetailHeader.Controls.AddRange(new Control[] { iconHandshake, lblDetName, badgeStatus, lblDetID, btnEdit, btnDeact, btnActivate });
            pnlHeaderWrapper.Controls.Add(pnlDetailHeader);
            _dynamicTexts.Add(lblDetName); _mutedTexts.Add(lblDetID); _buttons.Add(btnEdit); _buttons.Add(btnDeact); _buttons.Add(btnActivate);

            // --- TABS ---
            pnlTabRow = new Panel { Dock = DockStyle.Top, Height = 46, Padding = new Padding(30, 0, 0, 0) };
            pnlTabRow.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 45, pnlTabRow.Width, 45); } };

            tabOverview = new TabLabel { Text = "Overview", IsActive = true, Location = new Point(30, 0) };
            tabTransactions = new TabLabel { Text = "Procurement History", IsActive = false, Location = new Point(230, 0) };

            tabOverview.Click += (s, e) => { SwitchDetailTab("Overview"); };
            tabTransactions.Click += (s, e) => { SwitchDetailTab("Transactions"); };

            pnlTabRow.Controls.AddRange(new Control[] { tabOverview, tabTransactions });

            // --- TAB CONTENT CONTAINERS ---
            pnlOverviewTab = new SmoothPanel { Dock = DockStyle.Fill, AutoScroll = true };
            pnlTransactionsTab = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };

            pnlOverviewTab.Paint += (s, e) =>
            {
                if (_selectedSupplier == null)
                {
                    TextRenderer.DrawText(e.Graphics, "Please select a supplier from the list to view details.", new Font("Segoe UI", 11F, FontStyle.Italic), pnlOverviewTab.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            detailContainer.Controls.Add(pnlOverviewTab);
            detailContainer.Controls.Add(pnlTransactionsTab);
            detailContainer.Controls.Add(pnlTabRow);
            detailContainer.Controls.Add(pnlHeaderWrapper);
            pnlDetail.Controls.Add(detailContainer);

            // --- TRANSACTIONS TAB GRID ---
            dgvPOHistory = new SmoothGrid { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, EnableHeadersVisualStyles = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeight = 50, RowTemplate = { Height = 55 }, Cursor = Cursors.Hand };
            dgvPOHistory.Columns.Add("ColPO", "PURCHASE ORDER #");
            dgvPOHistory.Columns.Add("ColDate", "ORDER DATE");
            dgvPOHistory.Columns.Add("ColItems", "TOTAL ITEMS");
            dgvPOHistory.Columns.Add("ColCost", "TOTAL COST");
            dgvPOHistory.Columns.Add("ColStatus", "STATUS");

            dgvPOHistory.Columns["ColPO"].DefaultCellStyle.Font = new Font("Consolas", 10.5F, FontStyle.Bold);
            dgvPOHistory.Columns["ColPO"].DefaultCellStyle.ForeColor = Color.FromArgb(59, 130, 246);
            dgvPOHistory.Columns["ColCost"].DefaultCellStyle.Format = "C2";
            dgvPOHistory.DefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
            dgvPOHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvPOHistory.ColumnHeadersDefaultCellStyle.Padding = new Padding(15, 0, 0, 0);

            dgvPOHistory.CellMouseClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    string poNumber = dgvPOHistory.Rows[e.RowIndex].Cells[0].Value.ToString();
                    var history = _dmController.GetSupplierPOHistory(_selectedSupplier.SupplierID);
                    _hoveredPO = history.FirstOrDefault(h => h.PO_Number == poNumber);
                    if (_hoveredPO != null) OpenModal("POView");
                }
            };

            dgvPOHistory.Paint += (s, e) =>
            {
                if (dgvPOHistory.Rows.Count == 0 && _selectedSupplier != null) TextRenderer.DrawText(e.Graphics, "No procurement history found for this supplier.", new Font("Segoe UI", 11F, FontStyle.Italic), dgvPOHistory.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            pnlTransactionsTab.Controls.Add(dgvPOHistory);

            this.Controls.Add(pnlDetail);
            this.Controls.Add(pnlMaster);
        }

        private void SwitchDetailTab(string tabName)
        {
            if (pnlOverviewTab != null) pnlOverviewTab.Visible = (tabName == "Overview");
            if (pnlTransactionsTab != null) pnlTransactionsTab.Visible = (tabName == "Transactions");

            if (tabOverview != null)
            {
                tabOverview.IsActive = (tabName == "Overview");
                tabOverview.ForeColor = tabOverview.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText;
                tabOverview.Invalidate();
            }
            if (tabTransactions != null)
            {
                tabTransactions.IsActive = (tabName == "Transactions");
                tabTransactions.ForeColor = tabTransactions.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText;
                tabTransactions.Invalidate();
            }
        }

        private void LoadSuppliers()
        {
            string filter = cmbFilter.SelectedItem?.ToString().Split(' ')[0] ?? "All";
            _allSuppliers = _dmController.GetAllSuppliers(filter);

            if (_sortAsc) _allSuppliers = _allSuppliers.OrderBy(s => s.CompanyName).ToList();
            else _allSuppliers = _allSuppliers.OrderByDescending(s => s.CompanyName).ToList();

            if (_selectedSupplier != null)
            {
                var match = _allSuppliers.FirstOrDefault(s => s.SupplierID == _selectedSupplier.SupplierID);
                _selectedSupplier = match ?? (_allSuppliers.Count > 0 ? _allSuppliers[0] : null);
            }
            else
            {
                _selectedSupplier = _allSuppliers.Count > 0 ? _allSuppliers[0] : null;
            }

            RenderMasterList();
            RenderSupplierDetails();
        }

        private void RenderMasterList()
        {
            flpSuppliers.SuspendLayout();
            flpSuppliers.Controls.Clear();

            string search = txtSearch.Text == "Search supplier..." ? "" : txtSearch.Text.ToLower();

            foreach (var sup in _allSuppliers)
            {
                if (!string.IsNullOrEmpty(search) && !sup.CompanyName.ToLower().Contains(search) && !sup.SupplierID.ToLower().Contains(search)) continue;

                Panel card = new Panel { Width = pnlMaster.Width - 22, Height = 75, Margin = new Padding(0), Cursor = Cursors.Hand };
                card.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 74, card.Width, 74); } };

                Label lblName = new Label { Text = sup.CompanyName, Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 15), BackColor = Color.Transparent };
                Label lblID = new Label { Text = sup.SupplierID, Font = new Font("Consolas", 10.5F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 42), BackColor = Color.Transparent };
                card.Controls.AddRange(new Control[] { lblName, lblID });

                bool isActiveCard = (_selectedSupplier != null && _selectedSupplier.SupplierID == sup.SupplierID);

                Action applyCardTheme = () =>
                {
                    Color activeText = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                    lblName.ForeColor = isActiveCard ? activeText : UITheme.CurrentText;
                    lblID.ForeColor = UITheme.MutedText;
                    card.BackColor = isActiveCard ? (UITheme.IsDarkMode ? Color.FromArgb(20, 255, 210, 74) : Color.FromArgb(15, 10, 36, 64)) : Color.Transparent;
                };

                card.Paint += (s, e) =>
                {
                    if (isActiveCard)
                    {
                        Color indicator = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                        using (SolidBrush b = new SolidBrush(indicator)) { e.Graphics.FillRectangle(b, 0, 0, 4, card.Height); }
                    }
                };

                EventHandler enter = (s, e) => { if (!isActiveCard) card.BackColor = UITheme.IsDarkMode ? Color.FromArgb(15, 255, 255, 255) : Color.FromArgb(10, 0, 0, 0); };
                EventHandler leave = (s, e) => { applyCardTheme(); };
                EventHandler click = (s, e) => { _selectedSupplier = sup; RenderMasterList(); RenderSupplierDetails(); };

                card.MouseEnter += enter; card.MouseLeave += leave; card.Click += click;
                lblName.MouseEnter += enter; lblName.MouseLeave += leave; lblName.Click += click;
                lblID.MouseEnter += enter; lblID.MouseLeave += leave; lblID.Click += click;

                applyCardTheme();
                flpSuppliers.Controls.Add(card);
            }
            flpSuppliers.ResumeLayout();
            flpSuppliers.Invalidate();
        }

        private void RenderSupplierDetails()
        {
            if (_selectedSupplier == null)
            {
                pnlOverviewTab.Controls.Clear();
                dgvPOHistory.Rows.Clear();
                lblDetName.Text = ""; badgeStatus.Visible = false; lblDetID.Text = "";
                btnEdit.Visible = false; btnDeact.Visible = false; btnActivate.Visible = false; iconHandshake.Visible = false;
                pnlOverviewTab.Invalidate();
                return;
            }

            lblDetName.Text = _selectedSupplier.CompanyName;
            lblDetID.Text = _selectedSupplier.SupplierID;
            badgeStatus.Text = _selectedSupplier.IsActive ? "Active" : "Inactive";

            badgeStatus.Visible = true; iconHandshake.Visible = true; btnEdit.Visible = true;
            btnDeact.Visible = _selectedSupplier.IsActive;
            btnActivate.Visible = !_selectedSupplier.IsActive;

            badgeStatus.Invalidate();

            pnlOverviewTab.SuspendLayout();
            pnlOverviewTab.Controls.Clear();
            _dynamicTexts.Clear(); _mutedTexts.Clear(); _lines.Clear();
            _dynamicTexts.Add(lblDetName); _mutedTexts.Add(lblDetID);

            int y = 30;
            Action<string> AddHeader = (title) =>
            {
                Label h = new Label { Text = title.ToUpper(), Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(30, y) };
                pnlOverviewTab.Controls.Add(h); _mutedTexts.Add(h); y += 25;

                Panel line = new Panel { Height = 1, Location = new Point(30, y), BackColor = UITheme.CurrentBorder, Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right };
                line.Width = pnlOverviewTab.Width - 60;
                pnlOverviewTab.Controls.Add(line); _lines.Add(line); y += 20;
            };

            Action<string, string, int, int, bool> AddInfo = (lbl, val, xOffset, width, isItalic) =>
            {
                Label l1 = new Label { Text = lbl, Font = new Font("Segoe UI", 9.5F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(xOffset, y) };

                FontStyle fs = FontStyle.Bold;
                if (isItalic) fs |= FontStyle.Italic;

                Label l2 = new Label { Text = string.IsNullOrEmpty(val) ? "N/A" : val, Font = new Font("Segoe UI", 11.5F, fs), AutoSize = false, Size = new Size(width, 45), Location = new Point(xOffset, y + 22) };

                if (isItalic) { l2.ForeColor = UITheme.MutedText; _mutedTexts.Add(l2); }
                else { l2.ForeColor = UITheme.CurrentText; _dynamicTexts.Add(l2); }

                pnlOverviewTab.Controls.AddRange(new Control[] { l1, l2 }); _mutedTexts.Add(l1);
            };

            AddHeader("Record Information");
            AddInfo("Supplier ID", _selectedSupplier.SupplierID, 30, 300, false);
            AddInfo("Date Registered", _selectedSupplier.DateRegistered.ToString("MMM dd, yyyy"), 350, 300, false);
            y += 70;

            AddHeader("Primary Contact");
            AddInfo("Contact Person", _selectedSupplier.ContactPerson, 30, 300, false);
            AddInfo("Contact Number", _selectedSupplier.ContactNumber, 350, 300, false);
            y += 70;
            AddInfo("Email Address", _selectedSupplier.EmailAddress, 30, 600, false);
            y += 70;

            AddHeader("Location & Additional Details");
            AddInfo("Business Address", _selectedSupplier.Address, 30, 600, false); y += 70;
            AddInfo("Remarks / Notes", _selectedSupplier.Remarks, 30, 600, true);

            pnlOverviewTab.ResumeLayout();
            ApplyTheme();

            dgvPOHistory.Rows.Clear();
            var history = _dmController.GetSupplierPOHistory(_selectedSupplier.SupplierID);
            foreach (var po in history)
            {
                dgvPOHistory.Rows.Add(po.PO_Number, po.OrderDate.ToString("MMM dd, yyyy"), po.TotalItems.ToString(), po.TotalCost, po.Status);
            }
        }

        // =========================================================================
        // MODALS (CREATE, EDIT, DEACTIVATE, ACTIVATE, PO SUMMARY)
        // =========================================================================
        private void OpenModal(string type)
        {
            ModalForm modal = new ModalForm { FormBorderStyle = FormBorderStyle.None, BackColor = UITheme.CurrentPanel, StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = false };
            modal.Paint += (s, e) =>
            {
                if (modal.Width <= 1 || modal.Height <= 1) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 12; path.AddArc(0, 0, r, r, 180, 90); path.AddArc(modal.Width - r - 1, 0, r, r, 270, 90); path.AddArc(modal.Width - r - 1, modal.Height - r - 1, r, r, 0, 90); path.AddArc(0, modal.Height - r - 1, r, r, 90, 90); path.CloseFigure(); modal.Region = new Region(path);
                    if (type != "Deactivate" && type != "Activate") { using (Pen p = new Pen(UITheme.CurrentBorder, 3)) { e.Graphics.DrawPath(p, path); } }
                }
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234) };
            IconChar hIcon = type == "POView" ? IconChar.FileInvoice : IconChar.Dolly;
            IconPictureBox headerIcon = new IconPictureBox { IconChar = hIcon, IconColor = UITheme.CurrentText, IconSize = 22, Size = new Size(24, 24), Location = new Point(20, 18) };
            Label lblTitle = new Label { Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(50, 17) };

            IconButton btnClose = new IconButton { IconChar = IconChar.Times, IconSize = 20, Size = new Size(40, 40), FlatStyle = FlatStyle.Flat, ForeColor = UITheme.MutedText, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0; btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnClose.Click += (s, e) => modal.Close(); btnClose.MouseEnter += (s, e) => btnClose.IconColor = Color.FromArgb(239, 68, 68); btnClose.MouseLeave += (s, e) => btnClose.IconColor = UITheme.MutedText;
            pnlHeader.Controls.AddRange(new Control[] { headerIcon, lblTitle, btnClose });

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = UITheme.CurrentPanel };
            pnlFooter.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); } };
            Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.FlatAppearance.MouseDownBackColor = UITheme.CurrentPanel;
            btnCancel.Click += (s, e) => modal.Close();

            if (type == "Deactivate" || type == "Activate")
            {
                modal.Size = new Size(400, 250); pnlHeader.Visible = false; btnClose.Location = new Point(350, 10); modal.Controls.Add(btnClose);

                bool isActivating = type == "Activate";
                Color themeColor = isActivating ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
                IconChar warnIcon = isActivating ? IconChar.QuestionCircle : IconChar.ExclamationTriangle;

                IconPictureBox iconWarning = new IconPictureBox { IconChar = warnIcon, IconColor = themeColor, IconSize = 60, Size = new Size(60, 60), Location = new Point(170, 30) };

                Label lblWarn = new Label { Text = isActivating ? "Confirm Activation" : "Deactivate Supplier", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 100);

                Label lblDesc = new Label { Text = isActivating ? $"Are you sure you want to restore\n{_selectedSupplier.CompanyName}?" : $"Are you sure you want to mark\n{_selectedSupplier.CompanyName} as Inactive?", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 135);

                Button btnAction = new Button { Text = isActivating ? "Activate" : "Deactivate", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = themeColor, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;

                int totalW = btnCancel.Width + 10 + btnAction.Width;
                int startX = (modal.Width - totalW) / 2;
                btnCancel.Location = new Point(startX, 16);
                btnAction.Location = new Point(startX + btnCancel.Width + 10, 16);

                btnAction.Click += (s, e) =>
                {
                    if (isActivating)
                    {
                        if (_dmController.ActivateSupplier(_selectedSupplier.SupplierID, _activeUserId))
                        {
                            _selectedSupplier.IsActive = true;
                            LogAndNotify("Supplier Restored", $"{_selectedSupplier.CompanyName} is now active.", true);
                        }
                    }
                    else
                    {
                        if (_dmController.DeactivateSupplier(_selectedSupplier.SupplierID, _activeUserId))
                        {
                            _selectedSupplier.IsActive = false;
                            LogAndNotify("Supplier Deactivated", $"{_selectedSupplier.CompanyName} is now inactive.", true);
                        }
                    }
                    LoadSuppliers(); modal.Close();
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.AddRange(new Control[] { iconWarning, lblWarn, lblDesc });
            }
            else if (type == "POView")
            {
                modal.Size = new Size(400, 320); lblTitle.Text = "Purchase Order Overview"; btnClose.Location = new Point(350, 10);

                Label lblPO = new Label { Text = _hoveredPO.PO_Number, Font = new Font("Consolas", 18F, FontStyle.Bold), ForeColor = Color.FromArgb(59, 130, 246), AutoSize = true, Location = new Point(115, 80) };

                RoundedPanel pnlSum = new RoundedPanel { Size = new Size(340, 110), Location = new Point(30, 120), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg };
                Action<string, string, int> AddSum = (l, v, yLoc) =>
                {
                    pnlSum.Controls.Add(new Label { Text = l, Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(15, yLoc) });
                    pnlSum.Controls.Add(new Label { Text = v, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(150, yLoc) });
                };
                AddSum("Order Date:", _hoveredPO.OrderDate.ToString("MMM dd, yyyy"), 15);
                AddSum("Total Items:", $"{_hoveredPO.TotalItems} Units", 45);
                AddSum("Total Cost:", $"₱ {_hoveredPO.TotalCost:N2}", 75);
                modal.Controls.AddRange(new Control[] { lblPO, pnlSum });

                Button btnGo = new Button { Text = "Go to Procurement", Size = new Size(200, 38), FlatStyle = FlatStyle.Flat, BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(100, 16) };
                btnGo.FlatAppearance.BorderSize = 0;
                btnGo.FlatAppearance.BorderSize = 0;
                btnGo.Click += (s, e) =>
                {
                    Form parent = this.FindForm();
                    if (parent != null)
                    {
                        ProcurementView procView = null;

                        // 1. Helper function to deep-search the Dashboard for the Procurement screen
                        Func<ProcurementView> SearchForView = () =>
                        {
                            Stack<Control> stack = new Stack<Control>();
                            stack.Push(parent);
                            while (stack.Count > 0)
                            {
                                Control current = stack.Pop();
                                if (current is ProcurementView pv) return pv;
                                foreach (Control child in current.Controls) stack.Push(child);
                            }
                            return null;
                        };

                        procView = SearchForView();

                        // 2. LAZY-LOAD BYPASS: If the module isn't loaded, trick the Dashboard into loading it!
                        if (procView == null)
                        {
                            Stack<Control> btnStack = new Stack<Control>();
                            btnStack.Push(parent);
                            while (btnStack.Count > 0)
                            {
                                Control current = btnStack.Pop();

                                // Look for the Sidebar button (It inherits from Button and contains "Procurement")
                                if (current is Button btn && btn.Text.Contains("Procurement"))
                                {
                                    btn.PerformClick(); // Simulate a physical mouse click on the sidebar
                                    Application.DoEvents(); // Force the Dashboard to instantly render the new screen
                                    break;
                                }
                                foreach (Control child in current.Controls) btnStack.Push(child);
                            }

                            // Search again now that the Dashboard has officially built the screen
                            procView = SearchForView();
                        }

                        // 3. Send the command to open the exact PO
                        if (procView != null)
                        {
                            // IMPORTANT: Replace 'poNumberFromRow' with the actual variable holding your clicked PO Number!
                            // (e.g., string poNumberFromRow = dgvPOHistory.Rows[e.RowIndex].Cells[0].Value.ToString(); )
                            if (dgvPOHistory.CurrentRow != null)
                            {
                                string selectedPO = dgvPOHistory.CurrentRow.Cells[0].Value.ToString();
                                procView.OpenExternalPO(selectedPO);
                            }

                            else
                            {
                                ShowToast("Please select a procurement record from the history to view.", false);
                            }
                        }
                        else
                        {
                            ShowToast("Could not auto-load Procurement. Please click the sidebar manually.", false);
                        }
                    }
                    modal.Close();
                };


                pnlFooter.Controls.Add(btnGo);
            }
            else // Create & Edit
            {
                lblTitle.Text = type == "Create" ? "Register Supplier" : "Edit Supplier Details"; btnClose.Location = new Point(600, 10);

                int y = 80;
                TextBox txtCode = new TextBox();
                if (type == "Edit")
                {
                    Label lCode = new Label { Text = "Supplier ID (Read Only)", Font = new Font("Segoe UI", 8.5F), ForeColor = UITheme.MutedText, Location = new Point(35, y), AutoSize = true };
                    RoundedPanel pCode = new RoundedPanel { Size = new Size(580, 38), Location = new Point(35, y + 20), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                    txtCode = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = true, Text = _selectedSupplier.SupplierID };
                    pCode.Controls.Add(txtCode); modal.Controls.AddRange(new Control[] { lCode, pCode }); y += 70;
                }

                Func<string, string, int, int, int, int, bool, TextBox> MakeInput = (lbl, val, xOffset, yLoc, w, height, isMulti) =>
                {
                    Label l = new Label { Text = lbl, Font = new Font("Segoe UI", 8.5F), ForeColor = UITheme.MutedText, Location = new Point(xOffset, yLoc), AutoSize = true };
                    RoundedPanel p = new RoundedPanel { Size = new Size(w, height), Location = new Point(xOffset, yLoc + 20), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                    TextBox t = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), Text = val, Multiline = isMulti };
                    p.Controls.Add(t); modal.Controls.AddRange(new Control[] { l, p }); return t;
                };

                TextBox txtComp = MakeInput("Company / Business Name", type == "Edit" ? _selectedSupplier.CompanyName : "", 35, y, 580, 38, false); y += 70;
                TextBox txtCP = MakeInput("Contact Person", type == "Edit" ? _selectedSupplier.ContactPerson : "", 35, y, 280, 38, false);
                TextBox txtCN = MakeInput("Contact Number", type == "Edit" ? _selectedSupplier.ContactNumber : "", 335, y, 280, 38, false); y += 70;
                TextBox txtEmail = MakeInput("Email Address", type == "Edit" ? _selectedSupplier.EmailAddress : "", 35, y, 580, 38, false); y += 70;
                TextBox txtAddr = MakeInput("Business Address", type == "Edit" ? _selectedSupplier.Address : "", 35, y, 580, 38, false); y += 70;
                TextBox txtRem = MakeInput("Remarks / Notes", type == "Edit" ? _selectedSupplier.Remarks : "", 35, y, 580, 100, true);

                int finalY = y + 100;
                modal.Size = new Size(650, finalY + 30 + 70);

                Button btnAction = new Button { Text = type == "Create" ? "Save Supplier" : "Update Changes", Size = new Size(150, 38), FlatStyle = FlatStyle.Flat, BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;

                btnAction.Location = new Point(modal.Width - 35 - 150, 16);
                btnCancel.Location = new Point(modal.Width - 35 - 150 - 10 - 100, 16);

                btnAction.Click += (s, e) =>
                {
                    if (string.IsNullOrWhiteSpace(txtComp.Text)) { ShowToast("Company Name is required.", false); return; }
                    SupplierModel sup = new SupplierModel { SupplierID = type == "Edit" ? txtCode.Text : "", CompanyName = txtComp.Text, ContactPerson = txtCP.Text, ContactNumber = txtCN.Text, EmailAddress = txtEmail.Text, Address = txtAddr.Text, Remarks = txtRem.Text };

                    if (type == "Create") { if (_dmController.CreateSupplier(sup, _activeUserId)) LogAndNotify("Supplier Registered", $"{sup.CompanyName} was successfully registered.", true); }
                    else { if (_dmController.UpdateSupplier(sup, _activeUserId)) LogAndNotify("Supplier Updated", $"{sup.CompanyName} was modified.", true); }

                    LoadSuppliers(); modal.Close();
                };
                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
            }

            modal.Controls.Add(pnlHeader); modal.Controls.Add(pnlFooter);
            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show(); modal.ShowDialog(overlay); overlay.Dispose();
        }

        // =========================================================================
        // THEME LOGIC 
        // =========================================================================
        public void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace;

            if (pnlMaster != null) pnlMaster.BackColor = UITheme.CurrentWorkspace;
            if (pnlDetail != null) pnlDetail.BackColor = UITheme.CurrentWorkspace;

            if (pnlDetailHeader != null) pnlDetailHeader.BackColor = UITheme.CurrentInputBg;
            if (pnlTabRow != null) pnlTabRow.BackColor = UITheme.CurrentInputBg;

            if (pnlOverviewTab != null) pnlOverviewTab.BackColor = UITheme.CurrentPanel;
            if (pnlTransactionsTab != null) pnlTransactionsTab.BackColor = UITheme.CurrentPanel;

            if (tabOverview != null) tabOverview.ForeColor = tabOverview.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText;
            if (tabTransactions != null) tabTransactions.ForeColor = tabTransactions.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText;

            if (cmbFilter != null) { cmbFilter.BackColor = UITheme.CurrentPanel; cmbFilter.ForeColor = UITheme.CurrentText; }
            if (iconHandshake != null) iconHandshake.IconColor = UITheme.MutedText;

            if (badgeStatus != null)
            {
                badgeStatus.BgTint = _selectedSupplier != null && _selectedSupplier.IsActive ? Color.FromArgb(40, 16, 185, 129) : Color.FromArgb(40, 239, 68, 68);
                badgeStatus.ForeColor = _selectedSupplier != null && _selectedSupplier.IsActive ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);
            }

            foreach (RoundedPanel wrap in _inputWrappers) { wrap.BackColor = UITheme.CurrentInputBg; wrap.BorderColor = UITheme.CurrentBorder; foreach (Control c in wrap.Controls) { if (c is IconPictureBox icon) icon.IconColor = UITheme.CurrentIcon; } }
            foreach (RoundedPanel wrap in _borderedContainers) { wrap.BackColor = UITheme.CurrentPanel; wrap.BorderColor = UITheme.CurrentBorder; }
            foreach (Panel line in _lines) { line.BackColor = UITheme.CurrentBorder; }

            foreach (TextBox txt in _textInputs) { txt.BackColor = UITheme.CurrentInputBg; txt.ForeColor = UITheme.CurrentText; }
            foreach (Control c in _dynamicTexts) { c.ForeColor = UITheme.CurrentText; }
            foreach (Control c in _mutedTexts) { c.ForeColor = UITheme.MutedText; }

            foreach (IconButton btn in _buttons)
            {
                string type = btn.Tag?.ToString() ?? "";
                if (type == "ActionAdd") { btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btn.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : UITheme.SecondaryDark; }
                else if (type == "Danger") { btn.BackColor = Color.FromArgb(25, 239, 68, 68); btn.ForeColor = Color.FromArgb(239, 68, 68); btn.FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68); btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 68, 68); }
                else if (type == "Success") { btn.BackColor = Color.FromArgb(25, 16, 185, 129); btn.ForeColor = Color.FromArgb(16, 185, 129); btn.FlatAppearance.BorderColor = Color.FromArgb(16, 185, 129); btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 185, 129); }
                else if (type == "Secondary") { btn.BackColor = Color.Transparent; btn.ForeColor = UITheme.CurrentText; btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = UITheme.CurrentBorder; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230); }
                else { btn.BackColor = UITheme.CurrentPanel; btn.ForeColor = UITheme.CurrentText; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230); }

                btn.IconColor = btn.ForeColor;
                btn.FlatAppearance.MouseDownBackColor = btn.BackColor;
            }

            if (dgvPOHistory != null)
            {
                dgvPOHistory.BackgroundColor = UITheme.CurrentPanel; dgvPOHistory.GridColor = UITheme.CurrentBorder;
                dgvPOHistory.DefaultCellStyle.BackColor = UITheme.CurrentPanel; dgvPOHistory.DefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvPOHistory.DefaultCellStyle.SelectionBackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(220, 230, 240);
                dgvPOHistory.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;
                dgvPOHistory.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
                dgvPOHistory.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvPOHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgvPOHistory.ColumnHeadersDefaultCellStyle.BackColor;
            }

            if (flpSuppliers != null) flpSuppliers.Invalidate();
            if (pnlOverviewTab != null) pnlOverviewTab.Invalidate();

            RenderMasterList();
            this.Invalidate(true);
        }
    }
}