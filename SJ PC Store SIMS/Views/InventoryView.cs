using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System.Data;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace SJ_PC_Store_SIMS.Views
{
    public class InventoryView : System.Windows.Forms.UserControl
    {
        // =========================================================================
        // CUSTOM ENGINE COMPONENTS (Flicker-Free, Themed)
        // =========================================================================

        private class ModalForm : Form
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= 0x02000000;
                    return cp;
                }
            }
        }

        private class SmoothPanel : System.Windows.Forms.Panel { public SmoothPanel() { this.DoubleBuffered = true; this.ResizeRedraw = true; } }
        private class SmoothGrid : System.Windows.Forms.DataGridView { public SmoothGrid() { this.DoubleBuffered = true; } }

        private class DarkComboBox : System.Windows.Forms.ComboBox
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
                        Rectangle arrowRect = new Rectangle(this.Width - 18, 0, 18, this.Height);
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

        private class RoundedPanel : System.Windows.Forms.Panel
        {
            public int BorderRadius { get; set; } = 6;
            public int BorderSize { get; set; } = 1;
            public Color BorderColor { get; set; } = Color.Transparent;
            public RoundedPanel() { this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); this.BackColor = Color.Transparent; this.ResizeRedraw = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                using (GraphicsPath path = GetRoundPath(rect, BorderRadius))
                {
                    using (SolidBrush brush = new SolidBrush(this.BackColor)) { e.Graphics.FillPath(brush, path); }
                    if (BorderSize > 0) { using (Pen pen = new Pen(BorderColor, BorderSize)) { e.Graphics.DrawPath(pen, path); } }
                }
            }
            private GraphicsPath GetRoundPath(Rectangle rect, int radius)
            {
                GraphicsPath path = new GraphicsPath();
                if (radius <= 0) { path.AddRectangle(rect); return path; }
                path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
                path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
                path.CloseFigure(); return path;
            }
        }

        private class TabButton : FontAwesome.Sharp.IconButton
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
                        int radius = 8;
                        path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                        path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
                        path.AddLine(rect.Right, rect.Bottom, rect.X, rect.Bottom);
                        path.CloseFigure();
                        using (SolidBrush brush = new SolidBrush(UITheme.CurrentPanel)) { e.Graphics.FillPath(brush, path); }
                        using (Pen pen = new Pen(UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, 3)) { e.Graphics.DrawLine(pen, 0, 1, this.Width, 1); }
                    }
                }
                base.OnPaint(e);
            }
        }

        // =========================================================================
        // VIEW VARIABLES
        // =========================================================================
        private InventoryController _inventoryController;
        private DataManagementController _dataController; // ADDED
        private string _activeUserId;

        private SmoothPanel pnlTabs, pnlContent, pnlCatalogTab, pnlStockTab, pnlArchiveTab;
        private TabButton btnTabCatalog, btnTabStock, btnTabArchive;
        private RoundedPanel pnlCatalogToolbar, pnlStockToolbar, pnlArchiveToolbar, pnlCatalogGridContainer, pnlStockGridContainer, pnlArchiveGridContainer;
        private SmoothGrid dgvCatalog, dgvStock, dgvArchive;

        private TextBox txtSearchCatalog, txtSearchStock, txtSearchArchive;
        private DarkComboBox cmbCategory, cmbCondition, cmbStatus;
        private bool _sortAsc = true;

        private int _hoverRowCat = -1; private string _hoverIconCat = "";
        private int _hoverRowStock = -1; private string _hoverIconStock = "";
        private int _hoverRowArch = -1; private string _hoverIconArch = "";

        private List<RoundedPanel> _inputWrappers = new List<RoundedPanel>();
        private List<TextBox> _textInputs = new List<TextBox>();
        private List<DarkComboBox> _comboInputs = new List<DarkComboBox>();
        private List<FontAwesome.Sharp.IconButton> _buttons = new List<FontAwesome.Sharp.IconButton>();

        private List<ItemMasterModel> _blueprintCache = new List<ItemMasterModel>();

        public InventoryView(string currentUserId)
        {
            _activeUserId = currentUserId;
            _inventoryController = new InventoryController();
            _dataController = new DataManagementController(); // ADDED: Initialize the controller

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.Padding = new Padding(35, 15, 35, 35);
            this.Margin = new Padding(0);

            InitializeUI();
            LoadDynamicCategories();
            ApplyTheme();
            LoadBlueprints();
            LoadStock();
            LoadArchived();
            SwitchTab("Catalog");
        }

        // =========================================================================
        // NOTIFICATION & TOAST ENGINE
        // =========================================================================
        private void LogAndNotify(string title, string message, bool isSuccess = true)
        {
            _inventoryController.LogActivity(_activeUserId, $"{title} - {message}", "Inventory");

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
            toast.Show();
            t.Start();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (this.Parent != null) { this.Parent.BackColorChanged -= Parent_BackColorChanged; this.Parent.BackColorChanged += Parent_BackColorChanged; }
        }
        private void Parent_BackColorChanged(object sender, EventArgs e) { ApplyTheme(); }

        private Bitmap SafeGetIcon(IconChar icon, Color color, int size = 24)
        {
            try { return icon.ToBitmap(color, size); }
            catch { int s = size > 0 ? size : 24; Bitmap b = new Bitmap(s, s); using (Graphics g = Graphics.FromImage(b)) { g.Clear(Color.Transparent); } return b; }
        }

        private void InitializeUI()
        {
            pnlTabs = new SmoothPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0) };
            pnlTabs.Paint += (s, e) => { using (Pen pen = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(pen, 0, pnlTabs.Height - 1, pnlTabs.Width, pnlTabs.Height - 1); } };

            btnTabCatalog = CreateTab("Blueprint Catalog", IconChar.Book);
            btnTabStock = CreateTab("Physical Stock", IconChar.Boxes);
            btnTabArchive = CreateTab("Archived", IconChar.Archive);

            btnTabCatalog.Click += (s, e) => SwitchTab("Catalog");
            btnTabStock.Click += (s, e) => SwitchTab("Stock");
            btnTabArchive.Click += (s, e) => SwitchTab("Archive");

            pnlTabs.Controls.Add(btnTabArchive); pnlTabs.Controls.Add(btnTabStock); pnlTabs.Controls.Add(btnTabCatalog);
            pnlContent = new SmoothPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 20, 0, 0) };

            // ================================= CATALOG TAB =================================
            pnlCatalogTab = new SmoothPanel { Dock = DockStyle.Fill };
            pnlCatalogToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpCatLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };
            FlowLayoutPanel flpCatRight = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            Control txtWrapper = CreateSearchInput("Search Item Code...", 320, out txtSearchCatalog, () => ApplyCatalogFilters());
            txtSearchCatalog.TextChanged += (s, e) => ApplyCatalogFilters();

            Control cmbCatWrapper = CreateComboInput(new string[] { }, 150, out cmbCategory);
            cmbCategory.SelectedIndexChanged += (s, e) => ApplyCatalogFilters();

            Control cmbCondWrapper = CreateComboInput(new[] { "Condition: All", "Brand New", "Second-hand" }, 140, out cmbCondition);
            cmbCondition.SelectedIndexChanged += (s, e) => ApplyCatalogFilters();

            flpCatLeft.Controls.AddRange(new Control[] { txtWrapper, cmbCatWrapper, cmbCondWrapper });

            FontAwesome.Sharp.IconButton btnNewBlueprint = CreateButton("New Blueprint", IconChar.FolderPlus, "ActionAdd");
            btnNewBlueprint.Click += (s, e) => OpenModal("Create");

            FontAwesome.Sharp.IconButton btnCategories = CreateButton("Categories", IconChar.List, "Secondary");
            btnCategories.Click += (s, e) => OpenModal("Category");

            FontAwesome.Sharp.IconButton btnSort = CreateButton("Sort", IconChar.Sort, "SortSecondary");
            btnSort.Click += (s, e) => { _sortAsc = !_sortAsc; dgvCatalog.Sort(dgvCatalog.Columns["ColCode"], _sortAsc ? System.ComponentModel.ListSortDirection.Ascending : System.ComponentModel.ListSortDirection.Descending); };

            flpCatRight.Controls.AddRange(new Control[] { btnNewBlueprint, btnCategories, btnSort });
            pnlCatalogToolbar.Controls.Add(flpCatLeft); pnlCatalogToolbar.Controls.Add(flpCatRight);

            pnlCatalogGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            SmoothPanel pnlGridGap1 = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

            dgvCatalog = CreateDataGridView(); SetupCatalogColumns();
            pnlCatalogGridContainer.Controls.Add(dgvCatalog);
            pnlCatalogTab.Controls.Add(pnlCatalogGridContainer); pnlCatalogTab.Controls.Add(pnlGridGap1); pnlCatalogTab.Controls.Add(pnlCatalogToolbar);

            // ================================= STOCK TAB =================================
            pnlStockTab = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            pnlStockToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpStockLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            Control txtStockWrapper = CreateSearchInput("Scan or Search Serial Number...", 320, out txtSearchStock, () => ApplyStockFilters());
            txtSearchStock.TextChanged += (s, e) => ApplyStockFilters();

            Control cmbStatWrapper = CreateComboInput(new[] { "Status: All", "Available", "Defective", "Returned" }, 150, out cmbStatus);
            cmbStatus.SelectedIndexChanged += (s, e) => ApplyStockFilters();

            flpStockLeft.Controls.AddRange(new Control[] { txtStockWrapper, cmbStatWrapper });
            pnlStockToolbar.Controls.Add(flpStockLeft);

            pnlStockGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            SmoothPanel pnlGridGap2 = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

            dgvStock = CreateDataGridView(); SetupStockColumns();
            pnlStockGridContainer.Controls.Add(dgvStock);
            pnlStockTab.Controls.Add(pnlStockGridContainer); pnlStockTab.Controls.Add(pnlGridGap2); pnlStockTab.Controls.Add(pnlStockToolbar);

            // ================================= ARCHIVE TAB =================================
            pnlArchiveTab = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            pnlArchiveToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };
            FlowLayoutPanel flpArchLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            Control txtArchWrapper = CreateSearchInput("Search Archived Codes...", 320, out txtSearchArchive, () => ApplyArchiveFilters());
            txtSearchArchive.TextChanged += (s, e) => ApplyArchiveFilters();
            flpArchLeft.Controls.Add(txtArchWrapper); pnlArchiveToolbar.Controls.Add(flpArchLeft);

            pnlArchiveGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            SmoothPanel pnlGridGap3 = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

            dgvArchive = CreateDataGridView(); SetupArchiveColumns();
            pnlArchiveGridContainer.Controls.Add(dgvArchive);
            pnlArchiveTab.Controls.Add(pnlArchiveGridContainer); pnlArchiveTab.Controls.Add(pnlGridGap3); pnlArchiveTab.Controls.Add(pnlArchiveToolbar);

            pnlContent.Controls.Add(pnlCatalogTab); pnlContent.Controls.Add(pnlStockTab); pnlContent.Controls.Add(pnlArchiveTab);
            this.Controls.Add(pnlContent); this.Controls.Add(pnlTabs);
        }

        // =========================================================================
        // DATA SYNCHRONIZATION
        // =========================================================================
        private void LoadBlueprints()
        {
            dgvCatalog.Rows.Clear();
            var items = _inventoryController.GetAllBlueprints();

            _blueprintCache.RemoveAll(i => items.Any(newI => newI.ItemCode == i.ItemCode));
            _blueprintCache.AddRange(items);

            foreach (var item in items) dgvCatalog.Rows.Add(item.ItemCode, item.Category, item.Specs, item.ItemCondition, item.CurrentValue, item.PhysicalStockCount, "");
        }

        private void LoadStock()
        {
            dgvStock.Rows.Clear();
            var dt = _inventoryController.GetPhysicalStock();
            foreach (DataRow row in dt.Rows)
            {
                string suppId = row["SupplierID"].ToString();
                if (string.IsNullOrWhiteSpace(suppId)) suppId = "N/A";

                dgvStock.Rows.Add(
                    row["SerialNumber"].ToString(),
                    row["ItemCode"].ToString(),
                    row["PO_Number"].ToString(),
                    suppId,
                    row["Status"].ToString(),
                    ""
                );
            }
        }

        private void LoadArchived()
        {
            dgvArchive.Rows.Clear();
            var items = _inventoryController.GetArchivedBlueprints();

            _blueprintCache.RemoveAll(i => items.Any(newI => newI.ItemCode == i.ItemCode));
            _blueprintCache.AddRange(items);

            foreach (var item in items) dgvArchive.Rows.Add(item.ItemCode, item.Category, item.Specs, item.ItemCondition, "");
        }

        private void LoadDynamicCategories()
        {
            var cats = _inventoryController.GetCategories();
            string current = cmbCategory.SelectedItem?.ToString() ?? "All Categories";
            cmbCategory.Items.Clear(); cmbCategory.Items.Add("All Categories");
            foreach (var c in cats) cmbCategory.Items.Add(c);
            if (cmbCategory.Items.Contains(current)) cmbCategory.SelectedItem = current; else cmbCategory.SelectedIndex = 0;
        }

        private void ApplyCatalogFilters()
        {
            string search = txtSearchCatalog.Text == "Search Item Code..." ? "" : txtSearchCatalog.Text.ToLower();
            string cat = cmbCategory.SelectedItem?.ToString() ?? "All Categories";
            string cond = cmbCondition.SelectedItem?.ToString() ?? "Condition: All";

            foreach (DataGridViewRow row in dgvCatalog.Rows)
            {
                if (row.IsNewRow) continue;
                bool match = true;
                if (!string.IsNullOrEmpty(search) && !row.Cells["ColCode"].Value.ToString().ToLower().Contains(search) && !row.Cells["ColSpecs"].Value.ToString().ToLower().Contains(search)) match = false;
                if (cat != "All Categories" && row.Cells["ColCat"].Value.ToString() != cat) match = false;
                if (cond != "Condition: All" && row.Cells["ColCond"].Value.ToString() != cond) match = false;
                row.Visible = match;
            }
        }

        private void ApplyStockFilters()
        {
            string search = txtSearchStock.Text == "Scan or Search Serial Number..." ? "" : txtSearchStock.Text.ToLower();
            string stat = cmbStatus.SelectedItem?.ToString() ?? "Status: All";

            foreach (DataGridViewRow row in dgvStock.Rows)
            {
                if (row.IsNewRow) continue;
                bool match = true;
                if (!string.IsNullOrEmpty(search) && !row.Cells["ColSerial"].Value.ToString().ToLower().Contains(search)) match = false;
                if (stat != "Status: All" && row.Cells["ColStatus"].Value.ToString() != stat) match = false;
                row.Visible = match;
            }
        }

        private void ApplyArchiveFilters()
        {
            string search = txtSearchArchive.Text == "Search Archived Codes..." ? "" : txtSearchArchive.Text.ToLower();
            foreach (DataGridViewRow row in dgvArchive.Rows)
            {
                if (row.IsNewRow) continue;
                bool match = true;
                if (!string.IsNullOrEmpty(search) && !row.Cells["ColCodeArch"].Value.ToString().ToLower().Contains(search)) match = false;
                row.Visible = match;
            }
        }

        // =========================================================================
        // UI GENERATORS
        // =========================================================================
        private TabButton CreateTab(string text, IconChar icon)
        {
            return new TabButton { Text = "  " + text, IconChar = icon, IconSize = 22, Size = new Size(200, 52), Dock = DockStyle.Left, TextAlign = ContentAlignment.MiddleCenter, ImageAlign = ContentAlignment.MiddleLeft, TextImageRelation = TextImageRelation.ImageBeforeText, Font = new Font("Segoe UI", 11F, FontStyle.Bold), Padding = new Padding(20, 0, 0, 0) };
        }

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

        private Control CreateComboInput(string[] items, int width, out DarkComboBox cmbOut)
        {
            RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(10, 7, 10, 7), Margin = new Padding(0, 0, 10, 0) };
            DarkComboBox cmb = new DarkComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), Cursor = Cursors.Hand };
            cmb.Items.AddRange(items); if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;
            wrapper.Controls.Add(cmb); _inputWrappers.Add(wrapper); _comboInputs.Add(cmb);
            cmbOut = cmb; return wrapper;
        }

        private FontAwesome.Sharp.IconButton CreateButton(string text, IconChar icon, string type)
        {
            FontAwesome.Sharp.IconButton btn = new FontAwesome.Sharp.IconButton { Text = text != "" ? "  " + text : "", IconChar = icon, IconSize = 18, Height = 38, AutoSize = true, Padding = new Padding(15, 0, 15, 0), Margin = new Padding(10, 0, 0, 0), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText, Tag = type };
            btn.FlatAppearance.BorderSize = 0;
            if (type == "Primary" && text == "Search") btn.Margin = new Padding(0, 0, 10, 0);
            if (type == "ActionAdd") btn.Padding = new Padding(10, 0, 10, 0);
            _buttons.Add(btn); return btn;
        }

        // =========================================================================
        // DATAGRIDVIEW SETUP & EMPTY STATES
        // =========================================================================
        private void SetupCatalogColumns()
        {
            dgvCatalog.Columns.Add("ColCode", "ITEM CODE");
            dgvCatalog.Columns.Add("ColCat", "CATEGORY");
            dgvCatalog.Columns.Add("ColSpecs", "HARDWARE SPECS");
            dgvCatalog.Columns.Add("ColCond", "CONDITION");
            dgvCatalog.Columns.Add("ColValue", "VALUE (SELLING)");
            dgvCatalog.Columns.Add("ColStock", "PHYSICAL STOCK");

            DataGridViewTextBoxColumn colActions = new DataGridViewTextBoxColumn { HeaderText = "ACTIONS", Name = "ColActions", Width = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            dgvCatalog.Columns.Add(colActions);

            dgvCatalog.Columns["ColCode"].DefaultCellStyle.Font = new Font("Consolas", 11F, FontStyle.Bold);
            dgvCatalog.Columns["ColStock"].DefaultCellStyle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            dgvCatalog.Columns["ColValue"].DefaultCellStyle.Format = "C2";

            dgvCatalog.DefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
            dgvCatalog.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            dgvCatalog.ColumnHeadersDefaultCellStyle.Padding = new Padding(15, 0, 0, 0);

            dgvCatalog.CellPainting += DgvCatalog_CellPainting;
            dgvCatalog.CellMouseClick += DgvCatalog_CellMouseClick;
            dgvCatalog.CellMouseMove += DgvCatalog_CellMouseMove;
            dgvCatalog.CellMouseLeave += (s, e) => { _hoverRowCat = -1; _hoverIconCat = ""; dgvCatalog.Invalidate(); };

            dgvCatalog.Paint += (s, e) =>
            {
                if (dgvCatalog.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "The Blueprint Catalog is empty.\nClick 'New Blueprint' to create one!", new Font("Segoe UI", 11F, FontStyle.Italic), dgvCatalog.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private SmoothGrid CreateDataGridView()
        {
            return new SmoothGrid
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.Single,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 50,
                RowTemplate = { Height = 55 },
                Cursor = Cursors.Hand
            };
        }

        private void DgvCatalog_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvCatalog.Columns["ColActions"].Index)
            {
                int iconSize = 18; int gap = 8; int startX = 18;
                bool overInfo = e.X >= startX && e.X <= startX + iconSize;
                bool overEdit = e.X >= startX + iconSize + gap && e.X <= startX + (iconSize * 2) + gap;
                bool overDel = e.X >= startX + (iconSize * 2) + (gap * 2) && e.X <= startX + (iconSize * 3) + (gap * 2);

                string currentHover = overInfo ? "Info" : (overEdit ? "Edit" : (overDel ? "Delete" : ""));
                if (_hoverRowCat != e.RowIndex || _hoverIconCat != currentHover)
                {
                    _hoverRowCat = e.RowIndex; _hoverIconCat = currentHover;
                    dgvCatalog.InvalidateCell(e.ColumnIndex, e.RowIndex);
                }
            }
        }

        private void DgvCatalog_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvCatalog.Columns["ColActions"].Index)
            {
                DataGridViewRow row = dgvCatalog.Rows[e.RowIndex];
                if (_hoverIconCat == "Info") OpenModal("Details", row);
                else if (_hoverIconCat == "Edit") OpenModal("Edit", row);
                else if (_hoverIconCat == "Delete") OpenModal("Delete", row);
            }
        }

        private void DgvCatalog_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvCatalog.Columns["ColActions"].Index)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                int iconSize = 18; int gap = 8;
                int startX = e.CellBounds.X + 18;
                int startY = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;

                Color cInfo = (_hoverRowCat == e.RowIndex && _hoverIconCat == "Info") ? UITheme.AccentYellow : UITheme.MutedText;
                Color cEdit = (_hoverRowCat == e.RowIndex && _hoverIconCat == "Edit") ? UITheme.AccentYellow : UITheme.MutedText;
                Color cDel = (_hoverRowCat == e.RowIndex && _hoverIconCat == "Delete") ? UITheme.AccentYellow : Color.FromArgb(239, 68, 68);

                using (Bitmap infoIcon = SafeGetIcon(IconChar.InfoCircle, cInfo, iconSize))
                using (Bitmap editIcon = SafeGetIcon(IconChar.Pen, cEdit, iconSize))
                using (Bitmap delIcon = SafeGetIcon(IconChar.Trash, cDel, iconSize))
                {
                    e.Graphics.DrawImage(infoIcon, startX, startY, iconSize, iconSize);
                    e.Graphics.DrawImage(editIcon, startX + iconSize + gap, startY, iconSize, iconSize);
                    e.Graphics.DrawImage(delIcon, startX + (iconSize * 2) + (gap * 2), startY, iconSize, iconSize);
                }
                e.Handled = true;
            }
        }

        private void SetupStockColumns()
        {
            dgvStock.Columns.Add("ColSerial", "SERIAL NUMBER");
            dgvStock.Columns.Add("ColRef", "ITEM CODE (REF)");
            dgvStock.Columns.Add("ColOrigin", "PO NUMBER (ORIGIN)");
            dgvStock.Columns.Add("ColSupp", "SUPPLIER ID");
            dgvStock.Columns.Add("ColStatus", "STATUS");

            DataGridViewTextBoxColumn colFlag = new DataGridViewTextBoxColumn { HeaderText = "ACTIONS", Name = "ColFlag", Width = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            dgvStock.Columns.Add(colFlag);

            dgvStock.Columns["ColSerial"].DefaultCellStyle.Font = new Font("Consolas", 11F, FontStyle.Bold);
            // ADDED: Underlines the Supplier ID text to indicate it is a clickable reference
            dgvStock.Columns["ColSupp"].DefaultCellStyle.Font = new Font("Segoe UI", 10.5F, FontStyle.Underline);
            dgvStock.DefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
            dgvStock.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            dgvStock.ColumnHeadersDefaultCellStyle.Padding = new Padding(15, 0, 0, 0);

            dgvStock.CellPainting += DgvStock_CellPainting;
            dgvStock.CellMouseMove += DgvStock_CellMouseMove;
            dgvStock.CellMouseClick += DgvStock_CellMouseClick;
            dgvStock.CellMouseLeave += (s, e) => { _hoverRowStock = -1; _hoverIconStock = ""; dgvStock.Invalidate(); };

            dgvStock.Paint += (s, e) =>
            {
                if (dgvStock.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "No physical stock available.", new Font("Segoe UI", 11F, FontStyle.Italic), dgvStock.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private void DgvStock_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvStock.Columns["ColFlag"].Index)
            {
                int iconSize = 18; int gap = 8; int startX = 18;
                bool overFirst = e.X >= startX && e.X <= startX + iconSize;
                bool overSecond = e.X >= startX + iconSize + gap && e.X <= startX + (iconSize * 2) + gap;

                string status = dgvStock.Rows[e.RowIndex].Cells["ColStatus"].Value.ToString();
                string currentHover = "";

                // Determine which icon is being hovered based on the item's status
                if (status == "Available" || status == "Defective")
                {
                    if (overFirst) currentHover = "Flag";
                }
                else if (status == "Returned")
                {
                    if (overFirst) currentHover = "Recover";
                    else if (overSecond) currentHover = "Flag";
                }

                if (_hoverRowStock != e.RowIndex || _hoverIconStock != currentHover)
                {
                    _hoverRowStock = e.RowIndex; _hoverIconStock = currentHover;
                    dgvStock.InvalidateCell(e.ColumnIndex, e.RowIndex);
                }
            }
        }

        private void DgvStock_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                // Handle Icon Actions
                if (e.ColumnIndex == dgvStock.Columns["ColFlag"].Index && !string.IsNullOrEmpty(_hoverIconStock))
                {
                    DataGridViewRow row = dgvStock.Rows[e.RowIndex];
                    if (_hoverIconStock == "Flag") OpenModal("Flag", row);
                    else if (_hoverIconStock == "Recover") OpenModal("RecoverStock", row);
                }
                // Handle Clickable Supplier ID Link
                else if (e.ColumnIndex == dgvStock.Columns["ColSupp"].Index)
                {
                    DataGridViewRow row = dgvStock.Rows[e.RowIndex];
                    string suppId = row.Cells["ColSupp"].Value?.ToString();

                    // Only trigger if a valid supplier ID exists
                    if (!string.IsNullOrWhiteSpace(suppId) && suppId != "N/A")
                    {
                        OpenModal("SupplierDetails", row);
                    }
                }
            }
        }

        private void DgvStock_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvStock.Columns["ColFlag"].Index)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                int iconSize = 18; int gap = 8;
                int startX = e.CellBounds.X + 18;
                int startY = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;

                string status = dgvStock.Rows[e.RowIndex].Cells["ColStatus"].Value.ToString();

                if (status == "Available")
                {
                    Color cFlag = (_hoverRowStock == e.RowIndex && _hoverIconStock == "Flag") ? UITheme.AccentYellow : UITheme.MutedText;
                    using (Bitmap flagIcon = SafeGetIcon(IconChar.Flag, cFlag, iconSize))
                    {
                        e.Graphics.DrawImage(flagIcon, startX, startY, iconSize, iconSize);
                    }
                }
                else if (status == "Defective")
                {
                    using (Bitmap flagIcon = SafeGetIcon(IconChar.Flag, Color.FromArgb(239, 68, 68), iconSize))
                    {
                        e.Graphics.DrawImage(flagIcon, startX, startY, iconSize, iconSize);
                    }
                }
                else if (status == "Returned")
                {
                    // Draw BOTH the Recovery icon and the Flag icon
                    Color cRecover = (_hoverRowStock == e.RowIndex && _hoverIconStock == "Recover") ? UITheme.AccentYellow : Color.FromArgb(59, 130, 246);
                    Color cFlag = (_hoverRowStock == e.RowIndex && _hoverIconStock == "Flag") ? UITheme.AccentYellow : Color.FromArgb(239, 68, 68);

                    using (Bitmap recoverIcon = SafeGetIcon(IconChar.Undo, cRecover, iconSize))
                    using (Bitmap flagIcon = SafeGetIcon(IconChar.Flag, cFlag, iconSize))
                    {
                        e.Graphics.DrawImage(recoverIcon, startX, startY, iconSize, iconSize);
                        e.Graphics.DrawImage(flagIcon, startX + iconSize + gap, startY, iconSize, iconSize);
                    }
                }
                e.Handled = true;
            }
        }

        // ARCHIVE SETUP
        private void SetupArchiveColumns()
        {
            dgvArchive.Columns.Add("ColCodeArch", "ITEM CODE");
            dgvArchive.Columns.Add("ColCatArch", "CATEGORY");
            dgvArchive.Columns.Add("ColSpecsArch", "HARDWARE SPECS");
            dgvArchive.Columns.Add("ColCondArch", "CONDITION");

            // Extended width for 3 icons (Info, Restore, Permanent Delete)
            DataGridViewTextBoxColumn colActions = new DataGridViewTextBoxColumn { HeaderText = "ACTIONS", Name = "ColActionsArch", Width = 140, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            dgvArchive.Columns.Add(colActions);

            dgvArchive.Columns["ColCodeArch"].DefaultCellStyle.Font = new Font("Consolas", 11F, FontStyle.Bold);
            dgvArchive.DefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
            dgvArchive.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            dgvArchive.ColumnHeadersDefaultCellStyle.Padding = new Padding(15, 0, 0, 0);

            dgvArchive.CellPainting += DgvArchive_CellPainting;
            dgvArchive.CellMouseMove += DgvArchive_CellMouseMove;
            dgvArchive.CellMouseClick += DgvArchive_CellMouseClick;
            dgvArchive.CellMouseLeave += (s, e) => { _hoverRowArch = -1; _hoverIconArch = ""; dgvArchive.Invalidate(); };

            dgvArchive.Paint += (s, e) =>
            {
                if (dgvArchive.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "No archived blueprints.", new Font("Segoe UI", 11F, FontStyle.Italic), dgvArchive.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
        }

        private void DgvArchive_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvArchive.Columns["ColActionsArch"].Index)
            {
                int iconSize = 18; int gap = 8; int startX = 18;
                bool overInfo = e.X >= startX && e.X <= startX + iconSize;
                bool overRestore = e.X >= startX + iconSize + gap && e.X <= startX + (iconSize * 2) + gap;
                bool overDel = e.X >= startX + (iconSize * 2) + (gap * 2) && e.X <= startX + (iconSize * 3) + (gap * 2);

                string currentHover = overInfo ? "Info" : (overRestore ? "Restore" : (overDel ? "DeletePermanent" : ""));
                if (_hoverRowArch != e.RowIndex || _hoverIconArch != currentHover)
                {
                    _hoverRowArch = e.RowIndex; _hoverIconArch = currentHover;
                    dgvArchive.InvalidateCell(e.ColumnIndex, e.RowIndex);
                }
            }
        }

        private void DgvArchive_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvArchive.Columns["ColActionsArch"].Index)
            {
                DataGridViewRow row = dgvArchive.Rows[e.RowIndex];
                if (_hoverIconArch == "Info") OpenModal("Details", row);
                else if (_hoverIconArch == "Restore") OpenModal("Restore", row);
                else if (_hoverIconArch == "DeletePermanent")
                {
                    string code = row.Cells["ColCodeArch"].Value.ToString();
                    if (_inventoryController.GetLifetimeStockCount(code) > 0)
                    {
                        OpenModal("Error", null, "Cannot permanently delete this blueprint because\nit has historical stock or sales records.");
                    }
                    else
                    {
                        OpenModal("DeletePermanent", row);
                    }
                }
            }
        }

        private void DgvArchive_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == dgvArchive.Columns["ColActionsArch"].Index)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);
                int iconSize = 18; int gap = 8; int startX = e.CellBounds.X + 18; int startY = e.CellBounds.Y + (e.CellBounds.Height - iconSize) / 2;

                Color cInfo = (_hoverRowArch == e.RowIndex && _hoverIconArch == "Info") ? UITheme.AccentYellow : UITheme.MutedText;
                Color cRestore = (_hoverRowArch == e.RowIndex && _hoverIconArch == "Restore") ? UITheme.AccentYellow : UITheme.MutedText;
                Color cDel = (_hoverRowArch == e.RowIndex && _hoverIconArch == "DeletePermanent") ? UITheme.AccentYellow : Color.FromArgb(239, 68, 68);

                using (Bitmap infoIcon = SafeGetIcon(IconChar.InfoCircle, cInfo, iconSize))
                using (Bitmap restoreIcon = SafeGetIcon(IconChar.Undo, cRestore, iconSize))
                using (Bitmap delIcon = SafeGetIcon(IconChar.Trash, cDel, iconSize))
                {
                    e.Graphics.DrawImage(infoIcon, startX, startY, iconSize, iconSize);
                    e.Graphics.DrawImage(restoreIcon, startX + iconSize + gap, startY, iconSize, iconSize);
                    e.Graphics.DrawImage(delIcon, startX + (iconSize * 2) + (gap * 2), startY, iconSize, iconSize);
                }
                e.Handled = true;
            }
        }

        // =========================================================================
        // MASTER MODAL ENGINE WITH DYNAMIC CENTERING & FLICKER PREVENTION
        // =========================================================================
        private void OpenModal(string type, DataGridViewRow rowData = null, string customError = "")
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

                    if (type != "Delete" && type != "Flag" && type != "Error" && type != "Restore" && type != "DeletePermanent")
                    {
                        using (Pen p = new Pen(UITheme.CurrentBorder, 3)) { e.Graphics.DrawPath(p, path); }
                    }
                }
            };

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234) };
            IconChar headerIconType = type == "Create" ? IconChar.Cube : (type == "Details" ? IconChar.InfoCircle : (type == "Edit" ? IconChar.Pen : IconChar.List));
            IconPictureBox headerIcon = new IconPictureBox { IconChar = headerIconType, IconColor = UITheme.CurrentText, IconSize = 22, Size = new Size(24, 24), Location = new Point(20, 18) };
            Label lblTitle = new Label { Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(50, 17) };

            FontAwesome.Sharp.IconButton btnClose = new FontAwesome.Sharp.IconButton { IconChar = IconChar.Times, IconSize = 20, Size = new Size(40, 40), FlatStyle = FlatStyle.Flat, ForeColor = UITheme.MutedText, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            btnClose.FlatAppearance.BorderSize = 0; btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnClose.Click += (s, e) => modal.Close();
            btnClose.MouseEnter += (s, e) => btnClose.IconColor = Color.FromArgb(239, 68, 68);
            btnClose.MouseLeave += (s, e) => btnClose.IconColor = UITheme.MutedText;

            pnlHeader.Controls.AddRange(new Control[] { headerIcon, lblTitle, btnClose });

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = UITheme.CurrentPanel };
            Button btnCancel = new Button { Text = type == "Details" ? "Close" : "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.FlatAppearance.MouseDownBackColor = UITheme.CurrentPanel;
            btnCancel.MouseEnter += (s, e) => btnCancel.BackColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230);
            btnCancel.MouseLeave += (s, e) => btnCancel.BackColor = Color.Transparent;
            btnCancel.Click += (s, e) => modal.Close();


            // Add RecoverStock to the IF statement
            if (type == "Delete" || type == "Flag" || type == "Error" || type == "Restore" || type == "DeletePermanent" || type == "RecoverStock")
            {
                modal.Size = new Size(400, (type == "Flag" || type == "DeletePermanent") ? 320 : 250); pnlHeader.Visible = false; btnClose.Location = new Point(350, 10); modal.Controls.Add(btnClose);

                // Update the Icon setup
                IconChar warnIcon = type == "Error" ? IconChar.TimesCircle : ((type == "Restore" || type == "RecoverStock") ? IconChar.QuestionCircle : IconChar.ExclamationTriangle);
                Color warnColor = (type == "Restore" || type == "RecoverStock") ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);

                IconPictureBox iconWarning = new IconPictureBox { IconChar = warnIcon, IconColor = warnColor, IconSize = 60, Size = new Size(60, 60) };

                // Update the Title and Description setup
                string titleTxt = type == "Delete" ? "Confirm Archiving" :
                                  (type == "Flag" ? "Mark as Defective" :
                                  (type == "Restore" ? "Confirm Restore" :
                                  (type == "RecoverStock" ? "Recover Item" :
                                  (type == "DeletePermanent" ? "Permanent Deletion" : "Action Blocked"))));
                Label lblWarn = new Label { Text = titleTxt, Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };

                string descTxt = type == "Delete" ? "Are you sure you want to archive this blueprint?\nIt will be moved to the Archived tab." :
                                (type == "Flag" ? "Mark this physical stock as defective?\nIt will be removed from available inventory." :
                                (type == "Restore" ? "Are you sure you want to restore this blueprint?\nIt will be returned to the active catalog." :
                                (type == "RecoverStock" ? "Mark this returned item as functional?\nIt will be restored to 'Available' inventory." :
                                (type == "DeletePermanent" ? "This will erase the blueprint from the database forever.\nEnter your password to confirm:" : customError))));
                Label lblDesc = new Label { Text = descTxt, Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };

                iconWarning.Location = new Point((modal.Width - iconWarning.Width) / 2, 30);
                lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 100);
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 135);

                DarkComboBox cmbReason = new DarkComboBox();
                TextBox txtPass = new TextBox();

                if (type == "Flag")
                {
                    RoundedPanel pnlReason = new RoundedPanel { Size = new Size(300, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 7, 10, 7) };
                    pnlReason.Location = new Point((modal.Width - pnlReason.Width) / 2, 185);
                    cmbReason = new DarkComboBox { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Cursor = Cursors.Hand };
                    cmbReason.Items.AddRange(new[] { "DOA - Factory Defect", "DOA - Transit Damage", "Warehouse Damage", "Customer Return - Faulty" }); cmbReason.SelectedIndex = 0;
                    pnlReason.Controls.Add(cmbReason); modal.Controls.Add(pnlReason);
                }
                else if (type == "DeletePermanent")
                {
                    RoundedPanel pnlPass = new RoundedPanel { Size = new Size(280, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                    pnlPass.Location = new Point((modal.Width - pnlPass.Width) / 2, 185);
                    txtPass = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, UseSystemPasswordChar = true, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F) };
                    pnlPass.Controls.Add(txtPass); modal.Controls.Add(pnlPass);
                }

                if (type == "Error")
                {
                    btnCancel.Text = "Okay";
                    btnCancel.Location = new Point((modal.Width - btnCancel.Width) / 2, 15);
                    pnlFooter.Controls.Add(btnCancel);
                }
                else
                {
                    // Update the Action Button text and color
                    string actionTxt = type == "Delete" ? "Archive" :
                                       (type == "Restore" ? "Restore" :
                                       (type == "RecoverStock" ? "Recover" :
                                       (type == "DeletePermanent" ? "Delete" : "Flag")));

                    Color actionBg = (type == "Restore" || type == "RecoverStock") ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68);

                    Button btnAction = new Button { Text = actionTxt, Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = actionBg, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                    btnAction.FlatAppearance.BorderSize = 0;

                    int totalBtnWidth = btnCancel.Width + 10 + btnAction.Width;
                    int startX = (modal.Width - totalBtnWidth) / 2;
                    btnCancel.Location = new Point(startX, 15);
                    btnAction.Location = new Point(startX + btnCancel.Width + 10, 15);

                    btnAction.Click += (s, e) =>
                    {
                        if (type == "Delete")
                        {
                            string code = rowData.Cells["ColCode"].Value.ToString();
                            if (_inventoryController.GetBlueprintStockCount(code) > 0) { modal.Close(); OpenModal("Error", null, "Cannot archive this blueprint because there\nis active physical stock associated with it."); return; }
                            if (_inventoryController.DeleteBlueprint(code, _activeUserId)) LogAndNotify("Blueprint Archived", $"Item {code} was archived.");
                            LoadBlueprints(); LoadArchived();
                            modal.Close();
                        }
                        else if (type == "Flag")
                        {
                            string sn = rowData.Cells["ColSerial"].Value.ToString();
                            if (_inventoryController.FlagStockDefective(sn, cmbReason.SelectedItem.ToString())) LogAndNotify("Stock Flagged", $"SN {sn} marked as Defective.");
                            LoadStock(); LoadBlueprints();
                            modal.Close();
                        }
                        else if (type == "Restore")
                        {
                            string code = rowData.Cells["ColCodeArch"].Value.ToString();
                            if (_inventoryController.RestoreBlueprint(code, _activeUserId)) LogAndNotify("Blueprint Restored", $"Item {code} successfully restored.");
                            LoadArchived(); LoadBlueprints();
                            modal.Close();
                        }
                        else if (type == "DeletePermanent")
                        {
                            if (string.IsNullOrWhiteSpace(txtPass.Text)) { ShowToast("Password is required.", false); return; }
                            if (!_inventoryController.VerifyUserPassword(_activeUserId, txtPass.Text)) { ShowToast("Incorrect password.", false); return; }

                            string code = rowData.Cells["ColCodeArch"].Value.ToString();
                            if (_inventoryController.HardDeleteBlueprint(code, _activeUserId))
                            {
                                LogAndNotify("Blueprint Deleted", $"Item {code} was permanently deleted.");
                                LoadArchived();
                                modal.Close();
                            }
                        }
                        else if (type == "RecoverStock")
                        {
                            string sn = rowData.Cells["ColSerial"].Value.ToString();
                            if (_inventoryController.RecoverStock(sn, _activeUserId))
                            {
                                LogAndNotify("Stock Recovered", $"SN {sn} marked as Available.");
                            }
                            LoadStock(); LoadBlueprints();
                            modal.Close();
                        }
                    };
                    pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                }
                modal.Controls.AddRange(new Control[] { iconWarning, lblWarn, lblDesc });
            }
            else if (type == "Details")
            {
                modal.Size = new Size(650, 620);
                lblTitle.Text = "Item Blueprint Details";
                btnClose.Location = new Point(600, 10);
                pnlFooter.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); } };

                string code = rowData.Cells[0].Value.ToString();
                ItemMasterModel item = _blueprintCache.Find(x => x.ItemCode == code);
                if (item == null) { modal.Close(); return; }

                int y = 80;

                Action<string, string, int, int, int> AddDetailRow = (lblText, valText, xLoc, yLoc, w) =>
                {
                    Label l = new Label { Text = lblText, Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(xLoc, yLoc), AutoSize = true };
                    RoundedPanel p = new RoundedPanel { Location = new Point(xLoc, yLoc + 20), Size = new Size(w, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                    TextBox t = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = true, Text = valText };
                    p.Controls.Add(t); modal.Controls.AddRange(new Control[] { l, p });
                };

                AddDetailRow("Item Code", item.ItemCode, 35, y, 175);
                AddDetailRow("Item Status", item.IsActive ? "Active" : "Archived", 235, y, 175);
                AddDetailRow("Available Stock", $"{item.PhysicalStockCount} Units", 435, y, 175); y += 70;

                AddDetailRow("Hardware Category", item.Category, 35, y, 280);
                AddDetailRow("Item Condition", item.ItemCondition, 335, y, 280); y += 70;

                AddDetailRow("Full Specifications", item.Specs, 35, y, 580); y += 70;

                AddDetailRow("Baseline Cost", $"₱ {item.BaselineCost:N2}", 35, y, 280);
                AddDetailRow("Current Value (Selling Price)", $"₱ {item.CurrentValue:N2}", 335, y, 280); y += 70;

                AddDetailRow("Lifetime Units Sold", $"{item.TotalSold} Units", 35, y, 280);
                AddDetailRow("Lifetime Units Defective", $"{item.TotalDefective} Units", 335, y, 280); y += 70;

                Label lblAudit = new Label { Text = "Audit Trail", Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = UITheme.CurrentText, Location = new Point(35, y), AutoSize = true };
                modal.Controls.Add(lblAudit); y += 25;

                Label lblAuditD1 = new Label { Text = $"Created By: {item.CreatedBy} on {item.CreatedTime:MMM dd, yyyy}", Font = new Font("Segoe UI", 8.5F), ForeColor = UITheme.MutedText, Location = new Point(35, y), AutoSize = true };
                Label lblAuditD2 = new Label { Text = item.LastModifiedTime.HasValue ? $"Last Modified By: {item.ModifiedBy} on {item.LastModifiedTime.Value:MMM dd, yyyy}" : "Never Modified", Font = new Font("Segoe UI", 8.5F), ForeColor = UITheme.MutedText, Location = new Point(335, y), AutoSize = true };
                modal.Controls.AddRange(new Control[] { lblAuditD1, lblAuditD2 });

                btnCancel.Location = new Point((modal.Width - btnCancel.Width) / 2, 15);
                pnlFooter.Controls.Add(btnCancel);
            }
            else if (type == "Category")
            {
                modal.Size = new Size(450, 420); lblTitle.Text = "Manage Categories"; btnClose.Location = new Point(400, 10);

                RoundedPanel pnlListContainer = new RoundedPanel { Location = new Point(30, 80), Size = new Size(390, 220), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg };
                Panel pnlClip = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
                FlowLayoutPanel flpCategories = new FlowLayoutPanel { Width = pnlListContainer.Width + 25, Height = pnlListContainer.Height, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0) };
                pnlClip.Controls.Add(flpCategories); pnlListContainer.Controls.Add(pnlClip);

                RoundedPanel pnlNewCatWrapper = new RoundedPanel { Location = new Point(30, 320), Size = new Size(280, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                TextBox txtNewCat = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), Text = "New Category Name" };
                txtNewCat.GotFocus += (s, e) => { if (txtNewCat.Text == "New Category Name") txtNewCat.Text = ""; };
                txtNewCat.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtNewCat.Text)) txtNewCat.Text = "New Category Name"; };
                pnlNewCatWrapper.Controls.Add(txtNewCat);

                FontAwesome.Sharp.IconButton btnAdd = new FontAwesome.Sharp.IconButton { Text = "Add", Location = new Point(320, 320), Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = UITheme.AccentYellow, ForeColor = Color.Black, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAdd.FlatAppearance.BorderSize = 0;

                Action loadCategories = null;
                loadCategories = () =>
                {
                    flpCategories.Controls.Clear();
                    var cats = _inventoryController.GetCategories();
                    foreach (string cat in cats)
                    {
                        Panel row = new Panel { Width = 390, Height = 45, Margin = new Padding(0) };
                        row.Paint += (s, ev) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { ev.Graphics.DrawLine(p, 0, 44, row.Width, 44); } };
                        Label lblCat = new Label { Text = cat, AutoSize = true, Location = new Point(15, 12), Font = new Font("Segoe UI", 10F), ForeColor = UITheme.CurrentText };
                        FontAwesome.Sharp.IconButton btnDel = new FontAwesome.Sharp.IconButton { IconChar = IconChar.Times, IconSize = 16, Size = new Size(30, 30), Location = new Point(350, 7), FlatStyle = FlatStyle.Flat, ForeColor = UITheme.MutedText, BackColor = Color.Transparent, Cursor = Cursors.Hand };
                        btnDel.FlatAppearance.BorderSize = 0; btnDel.FlatAppearance.MouseOverBackColor = Color.Transparent; btnDel.FlatAppearance.MouseDownBackColor = Color.Transparent;
                        btnDel.MouseEnter += (s, ev) => { btnDel.ForeColor = Color.FromArgb(239, 68, 68); btnDel.IconColor = btnDel.ForeColor; };
                        btnDel.MouseLeave += (s, ev) => { btnDel.ForeColor = UITheme.MutedText; btnDel.IconColor = btnDel.ForeColor; };

                        btnDel.Click += (s, ev) =>
                        {
                            if (_inventoryController.DeleteCategory(cat))
                            {
                                LogAndNotify("Category Removed", $"{cat} deleted successfully.");
                                loadCategories(); LoadDynamicCategories();
                            }
                            else
                            {
                                modal.Close(); OpenModal("Error", null, $"Cannot delete '{cat}' because it is\ncurrently used by an active blueprint.");
                            }
                        };
                        row.Controls.Add(lblCat); row.Controls.Add(btnDel); flpCategories.Controls.Add(row);
                    }
                };

                btnAdd.Click += (s, ev) =>
                {
                    if (!string.IsNullOrWhiteSpace(txtNewCat.Text) && txtNewCat.Text != "New Category Name")
                    {
                        if (_inventoryController.AddCategory(txtNewCat.Text)) LogAndNotify("Category Added", $"{txtNewCat.Text} created successfully.");
                        txtNewCat.Text = "New Category Name"; loadCategories(); LoadDynamicCategories();
                    }
                };
                loadCategories(); pnlFooter.Visible = false; modal.Controls.AddRange(new Control[] { pnlListContainer, pnlNewCatWrapper, btnAdd });

            }
            else if (type == "SupplierDetails")
            {
                modal.Size = new Size(650, 560);
                lblTitle.Text = "Supplier Details";
                btnClose.Location = new Point(600, 10);
                pnlFooter.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); } };

                string suppId = rowData.Cells["ColSupp"].Value.ToString();

                // Pass "All" to fetch both active and inactive suppliers from the database
                var supplier = _dataController.GetAllSuppliers("All").FirstOrDefault(s => s.SupplierID == suppId);

                if (supplier == null)
                {
                    ShowToast("Supplier details not found.", false);
                    modal.Close();
                    return;
                }

                int y = 85;

                Action<string, string, int, int, int> AddDetailRow = (lblText, valText, xLoc, yLoc, w) =>
                {
                    Label l = new Label { Text = lblText, Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(xLoc, yLoc), AutoSize = true };
                    RoundedPanel p = new RoundedPanel { Location = new Point(xLoc, yLoc + 20), Size = new Size(w, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                    TextBox t = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = true, Text = valText };
                    p.Controls.Add(t); modal.Controls.AddRange(new Control[] { l, p });
                };

                AddDetailRow("Supplier ID", supplier.SupplierID, 35, y, 280);
                AddDetailRow("Status", supplier.IsActive ? "Active" : "Inactive", 335, y, 280); y += 75;

                AddDetailRow("Company Name", supplier.CompanyName, 35, y, 580); y += 75;

                AddDetailRow("Contact Person", string.IsNullOrEmpty(supplier.ContactPerson) ? "N/A" : supplier.ContactPerson, 35, y, 280);
                AddDetailRow("Contact Number", string.IsNullOrEmpty(supplier.ContactNumber) ? "N/A" : supplier.ContactNumber, 335, y, 280); y += 75;

                AddDetailRow("Email Address", string.IsNullOrEmpty(supplier.EmailAddress) ? "N/A" : supplier.EmailAddress, 35, y, 280);
                AddDetailRow("Registered Date", supplier.DateRegistered.ToString("MMM dd, yyyy"), 335, y, 280); y += 75;

                AddDetailRow("Physical Address", string.IsNullOrEmpty(supplier.Address) ? "N/A" : supplier.Address, 35, y, 580);

                btnCancel.Text = "Close";
                btnCancel.Location = new Point((modal.Width - btnCancel.Width) / 2, 15);
                pnlFooter.Controls.Add(btnCancel);
            }
            else // Create & Edit 
            {
                modal.Size = new Size(650, type == "Create" ? 420 : 460);
                lblTitle.Text = type == "Create" ? "Create New Blueprint" : "Edit Blueprint";
                btnClose.Location = new Point(600, 10);
                pnlFooter.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); } };
                int y = 85;

                TextBox txtCode = new TextBox();
                if (type == "Create")
                {
                    // No Item Code input box for Create    
                }
                else
                {
                    Label lblCode = new Label { Text = "Item Code (Read Only)", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(35, y), AutoSize = true };
                    RoundedPanel pnlCode = new RoundedPanel { Location = new Point(35, y + 20), Size = new Size(580, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                    txtCode = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F), ReadOnly = true, Text = rowData.Cells["ColCode"].Value.ToString() };
                    pnlCode.Controls.Add(txtCode); modal.Controls.AddRange(new Control[] { lblCode, pnlCode }); y += 75;
                }

                Label lblCat = new Label { Text = "Hardware Category", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(35, y), AutoSize = true };
                RoundedPanel pnlCat = new RoundedPanel { Location = new Point(35, y + 20), Size = new Size(280, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 7, 10, 7) };
                DarkComboBox cmbCat = new DarkComboBox { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Cursor = Cursors.Hand };

                var cats = _inventoryController.GetCategories();
                foreach (var c in cats) cmbCat.Items.Add(c); if (cmbCat.Items.Count > 0) cmbCat.SelectedIndex = 0;
                if (type == "Edit") cmbCat.SelectedItem = rowData.Cells["ColCat"].Value.ToString(); pnlCat.Controls.Add(cmbCat);

                Label lblCond = new Label { Text = "Item Condition", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(335, y), AutoSize = true };
                RoundedPanel pnlCond = new RoundedPanel { Location = new Point(335, y + 20), Size = new Size(280, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 7, 10, 7) };
                DarkComboBox cmbCond = new DarkComboBox { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F), BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Cursor = Cursors.Hand };
                cmbCond.Items.AddRange(new[] { "Brand New", "Second-hand" }); cmbCond.SelectedIndex = 0;
                if (type == "Edit") cmbCond.SelectedItem = rowData.Cells["ColCond"].Value.ToString(); pnlCond.Controls.Add(cmbCond);
                modal.Controls.AddRange(new Control[] { lblCat, pnlCat, lblCond, pnlCond }); y += 75;

                Label lblSpecs = new Label { Text = "Full Specifications (Visible in POS)", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(35, y), AutoSize = true };
                RoundedPanel pnlSpecs = new RoundedPanel { Location = new Point(35, y + 20), Size = new Size(580, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                TextBox txtSpecs = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F) };
                string sPh = "Enter full specifications..."; txtSpecs.Text = type == "Edit" ? rowData.Cells["ColSpecs"].Value.ToString() : sPh;
                if (type == "Create") { txtSpecs.GotFocus += (s, e) => { if (txtSpecs.Text == sPh) txtSpecs.Text = ""; }; txtSpecs.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtSpecs.Text)) txtSpecs.Text = sPh; }; }
                pnlSpecs.Controls.Add(txtSpecs); modal.Controls.AddRange(new Control[] { lblSpecs, pnlSpecs }); y += 75;

                Label p1 = new Label { Text = "₱", Font = new Font("Segoe UI", 11F, FontStyle.Regular), AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(0, 0, 5, 0), ForeColor = UITheme.CurrentText };
                Label p2 = new Label { Text = "₱", Font = new Font("Segoe UI", 11F, FontStyle.Regular), AutoSize = true, Dock = DockStyle.Left, Padding = new Padding(0, 0, 5, 0), ForeColor = UITheme.CurrentText };

                KeyPressEventHandler numbersOnly = (s, e) =>
                {
                    if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && (e.KeyChar != '.')) e.Handled = true;
                    if ((e.KeyChar == '.') && ((s as TextBox).Text.IndexOf('.') > -1)) e.Handled = true;
                };

                Label lblBase = new Label { Text = "Baseline Cost", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(35, y), AutoSize = true };
                RoundedPanel pnlBase = new RoundedPanel { Location = new Point(35, y + 20), Size = new Size(280, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                TextBox txtBase = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F) };
                txtBase.Text = type == "Edit" ? rowData.Cells["ColValue"].Value.ToString().Replace("₱", "").Trim() : "0.00";
                txtBase.KeyPress += numbersOnly;
                if (type == "Create") { txtBase.GotFocus += (s, e) => { if (txtBase.Text == "0.00") txtBase.Text = ""; }; txtBase.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtBase.Text)) txtBase.Text = "0.00"; }; }
                pnlBase.Controls.Add(txtBase); pnlBase.Controls.Add(p1);

                Label lblCurr = new Label { Text = "Current Value (Selling Price)", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, Location = new Point(335, y), AutoSize = true };
                RoundedPanel pnlCurr = new RoundedPanel { Location = new Point(335, y + 20), Size = new Size(280, 38), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                TextBox txtCurr = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 10.5F) };
                txtCurr.Text = type == "Edit" ? rowData.Cells["ColValue"].Value.ToString().Replace("₱", "").Trim() : "0.00";
                txtCurr.KeyPress += numbersOnly;
                if (type == "Create") { txtCurr.GotFocus += (s, e) => { if (txtCurr.Text == "0.00") txtCurr.Text = ""; }; txtCurr.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txtCurr.Text)) txtCurr.Text = "0.00"; }; }
                pnlCurr.Controls.Add(txtCurr); pnlCurr.Controls.Add(p2);
                modal.Controls.AddRange(new Control[] { lblBase, pnlBase, lblCurr, pnlCurr });

                Color btnBg = type == "Create" ? UITheme.AccentYellow : (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark);
                Color btnFg = type == "Create" ? Color.Black : (UITheme.IsDarkMode ? Color.Black : Color.White);
                Button btnAction = new Button { Text = type == "Create" ? "Save Blueprint" : "Update Changes", Size = new Size(150, 38), FlatStyle = FlatStyle.Flat, BackColor = btnBg, ForeColor = btnFg, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(465, 15) };

                btnAction.FlatAppearance.BorderSize = 0; btnCancel.Location = new Point(355, 15);

                btnAction.Click += (s, e) =>
                {
                    if (cmbCat.SelectedIndex < 0 || cmbCat.SelectedItem.ToString() == "All Categories") { ShowToast("Please select a hardware category.", false); return; }
                    if (string.IsNullOrWhiteSpace(txtSpecs.Text) || txtSpecs.Text == sPh) { ShowToast("Please enter valid specifications.", false); return; }
                    if (!decimal.TryParse(txtBase.Text.Replace("₱", "").Trim(), out decimal bCost) || bCost < 0) { ShowToast("Invalid Baseline Cost format.", false); return; }
                    if (!decimal.TryParse(txtCurr.Text.Replace("₱", "").Trim(), out decimal cVal) || cVal < 0) { ShowToast("Invalid Current Value format.", false); return; }

                    if (type == "Create")
                    {
                        string generatedCode = _inventoryController.GenerateNextItemCode();
                        if (_inventoryController.CreateBlueprint(generatedCode, cmbCat.SelectedItem.ToString(), txtSpecs.Text, cmbCond.SelectedItem.ToString(), bCost, cVal, _activeUserId))
                            LogAndNotify("Blueprint Created", $"Item {generatedCode} was successfully created.");
                    }
                    else
                    {
                        if (_inventoryController.UpdateBlueprint(txtCode.Text, cmbCat.SelectedItem.ToString(), txtSpecs.Text, cmbCond.SelectedItem.ToString(), bCost, cVal, _activeUserId))
                            LogAndNotify("Blueprint Updated", $"Item {txtCode.Text} was modified.");
                    }
                    LoadBlueprints(); modal.Close();
                };
                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
            }

            modal.Controls.Add(pnlHeader);
            modal.Controls.Add(pnlFooter);
            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show(); modal.ShowDialog(overlay); overlay.Dispose();
        }

        // =========================================================================
        // THEME LOGIC 
        // =========================================================================
        private void SwitchTab(string tabName)
        {
            pnlCatalogTab.Visible = tabName == "Catalog";
            pnlStockTab.Visible = tabName == "Stock";
            pnlArchiveTab.Visible = tabName == "Archive";

            btnTabCatalog.IsActive = tabName == "Catalog";
            btnTabStock.IsActive = tabName == "Stock";
            btnTabArchive.IsActive = tabName == "Archive";

            ApplyTheme();
        }

        public void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace; pnlTabs.BackColor = UITheme.CurrentWorkspace; pnlContent.BackColor = UITheme.CurrentWorkspace;
            pnlCatalogTab.BackColor = UITheme.CurrentWorkspace; pnlStockTab.BackColor = UITheme.CurrentWorkspace; pnlArchiveTab.BackColor = UITheme.CurrentWorkspace;

            pnlCatalogToolbar.BackColor = UITheme.CurrentPanel; pnlStockToolbar.BackColor = UITheme.CurrentPanel; pnlArchiveToolbar.BackColor = UITheme.CurrentPanel;
            pnlCatalogToolbar.BorderColor = UITheme.CurrentBorder; pnlStockToolbar.BorderColor = UITheme.CurrentBorder; pnlArchiveToolbar.BorderColor = UITheme.CurrentBorder;

            pnlCatalogGridContainer.BackColor = UITheme.CurrentPanel; pnlStockGridContainer.BackColor = UITheme.CurrentPanel; pnlArchiveGridContainer.BackColor = UITheme.CurrentPanel;
            pnlCatalogGridContainer.BorderColor = UITheme.CurrentBorder; pnlStockGridContainer.BorderColor = UITheme.CurrentBorder; pnlArchiveGridContainer.BorderColor = UITheme.CurrentBorder;

            btnTabCatalog.ForeColor = btnTabCatalog.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabCatalog.IconColor = btnTabCatalog.ForeColor;
            btnTabStock.ForeColor = btnTabStock.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabStock.IconColor = btnTabStock.ForeColor;
            btnTabArchive.ForeColor = btnTabArchive.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabArchive.IconColor = btnTabArchive.ForeColor;

            btnTabCatalog.FlatAppearance.MouseDownBackColor = UITheme.CurrentWorkspace; btnTabStock.FlatAppearance.MouseDownBackColor = UITheme.CurrentWorkspace; btnTabArchive.FlatAppearance.MouseDownBackColor = UITheme.CurrentWorkspace;
            Color hoverColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230);
            btnTabCatalog.FlatAppearance.MouseOverBackColor = hoverColor; btnTabStock.FlatAppearance.MouseOverBackColor = hoverColor; btnTabArchive.FlatAppearance.MouseOverBackColor = hoverColor;

            foreach (RoundedPanel wrap in _inputWrappers) { wrap.BackColor = UITheme.CurrentInputBg; wrap.BorderColor = UITheme.CurrentBorder; foreach (Control c in wrap.Controls) { if (c is IconPictureBox icon) icon.IconColor = UITheme.CurrentIcon; } }
            foreach (TextBox txt in _textInputs) { txt.BackColor = UITheme.CurrentInputBg; txt.ForeColor = UITheme.CurrentText; }
            foreach (DarkComboBox cmb in _comboInputs) { cmb.BackColor = UITheme.CurrentInputBg; cmb.ForeColor = UITheme.CurrentText; }

            foreach (FontAwesome.Sharp.IconButton btn in _buttons)
            {

                string type = btn.Tag.ToString();
                if (type == "ActionAdd")
                {
                    btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.SecondaryDark;
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : Color.FromArgb(45, 42, 50);
                }
                else if (type == "SortSecondary") { btn.BackColor = UITheme.SecondaryDark; btn.ForeColor = Color.White; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? UITheme.PrimaryDark : Color.FromArgb(20, 50, 80); }
                else if (type == "Primary") { btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btn.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : UITheme.SecondaryDark; }
                else if (type == "Secondary")
                {
                    btn.BackColor = UITheme.IsDarkMode ? UITheme.SecondaryDark : UITheme.CurrentPanel;
                    btn.ForeColor = UITheme.IsDarkMode ? Color.White : UITheme.CurrentText;
                    if (!UITheme.IsDarkMode) { btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = UITheme.CurrentBorder; } else btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? UITheme.PrimaryDark : Color.FromArgb(230, 230, 230);
                }
                btn.IconColor = btn.ForeColor;
                btn.FlatAppearance.MouseDownBackColor = btn.BackColor;
            }

            StyleGridTheme(dgvCatalog); StyleGridTheme(dgvStock); StyleGridTheme(dgvArchive);
            this.Invalidate(true);
        }

        private void StyleGridTheme(DataGridView dgv)
        {
            dgv.BackgroundColor = UITheme.CurrentPanel; dgv.GridColor = UITheme.CurrentBorder;
            dgv.DefaultCellStyle.BackColor = UITheme.CurrentPanel; dgv.DefaultCellStyle.ForeColor = UITheme.CurrentText;
            dgv.DefaultCellStyle.SelectionBackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(220, 230, 240);
            dgv.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                if (dgv.Columns.Contains("ColCode") && row.Cells["ColCode"] != null) row.Cells["ColCode"].Style.ForeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                if (dgv.Columns.Contains("ColSerial") && row.Cells["ColSerial"] != null) row.Cells["ColSerial"].Style.ForeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                if (dgv.Columns.Contains("ColCodeArch") && row.Cells["ColCodeArch"] != null) row.Cells["ColCodeArch"].Style.ForeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                // ADDED: Theme coloring for the clickable Supplier link
                if (dgv.Columns.Contains("ColSupp") && row.Cells["ColSupp"] != null) row.Cells["ColSupp"].Style.ForeColor = Color.FromArgb(59, 130, 246);

                if (dgv.Columns.Contains("ColStatus") && row.Cells["ColStatus"].Value != null)
                {
                    string status = row.Cells["ColStatus"].Value.ToString();
                    if (status == "Available") row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(16, 185, 129);
                    else if (status == "Defective") row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(239, 68, 68);
                    else if (status == "Returned") row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(239, 68, 68);
                }
            }
        }
    }
}