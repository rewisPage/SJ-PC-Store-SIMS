using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Utils;
using SJ_PC_Store_SIMS.Controllers;

namespace SJ_PC_Store_SIMS.Views
{
    public class InventoryView : System.Windows.Forms.UserControl
    {
        // =========================================================================
        // SAFE HTML-EMULATION UI COMPONENTS (Zero Exceptions & Flicker-Free)
        // =========================================================================
        private class SmoothPanel : System.Windows.Forms.Panel
        {
            public SmoothPanel() { this.DoubleBuffered = true; this.ResizeRedraw = true; }
        }

        private class SmoothGrid : System.Windows.Forms.DataGridView
        {
            public SmoothGrid() { this.DoubleBuffered = true; }
        }

        private class RoundedPanel : System.Windows.Forms.Panel
        {
            public int BorderRadius { get; set; } = 6;
            public int BorderSize { get; set; } = 1;
            public Color BorderColor { get; set; } = Color.Transparent;

            public RoundedPanel()
            {
                this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
                this.BackColor = Color.Transparent;
                this.ResizeRedraw = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                using (GraphicsPath path = GetRoundPath(rect, BorderRadius))
                {
                    using (SolidBrush brush = new SolidBrush(this.BackColor)) { e.Graphics.FillPath(brush, path); }
                    if (BorderSize > 0)
                    {
                        using (Pen pen = new Pen(BorderColor, BorderSize)) { e.Graphics.DrawPath(pen, path); }
                    }
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
                path.CloseFigure();
                return path;
            }
        }

        private class TabButton : FontAwesome.Sharp.IconButton
        {
            public bool IsActive { get; set; } = false;
            public TabButton()
            {
                this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
                this.BackColor = Color.Transparent;
                this.FlatStyle = FlatStyle.Flat;
                this.FlatAppearance.BorderSize = 0;
                this.Cursor = Cursors.Hand;
            }

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
        // CONTROLLERS & MAIN VARIABLES
        // =========================================================================
        private InventoryController _inventoryController;
        private SmoothPanel pnlTabs, pnlContent, pnlCatalogTab, pnlStockTab;
        private TabButton btnTabCatalog, btnTabStock;
        private RoundedPanel pnlCatalogToolbar, pnlStockToolbar, pnlCatalogGridContainer, pnlStockGridContainer;
        private SmoothGrid dgvCatalog, dgvStock;

        private List<RoundedPanel> _inputWrappers = new List<RoundedPanel>();
        private List<TextBox> _textInputs = new List<TextBox>();
        private List<ComboBox> _comboInputs = new List<ComboBox>();
        private List<FontAwesome.Sharp.IconButton> _buttons = new List<FontAwesome.Sharp.IconButton>();

        public InventoryView()
        {
            _inventoryController = new InventoryController();

            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);

            InitializeUI();
            ApplyTheme();
            PopulateDummyData();

            SwitchTab("Catalog");
        }

        // =========================================================================
        // CRASH-PROOF ICON GENERATOR
        // =========================================================================
        private Bitmap SafeGetIcon(IconChar icon, Color color, int size = 24)
        {
            try
            {
                return icon.ToBitmap(color, size);
            }
            catch
            {
                int safeSize = size > 0 ? size : 24;
                Bitmap fallback = new Bitmap(safeSize, safeSize);
                using (Graphics g = Graphics.FromImage(fallback)) { g.Clear(Color.Transparent); }
                return fallback;
            }
        }

        private void InitializeUI()
        {
            // 1. TABS PANEL
            pnlTabs = new SmoothPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0) };
            pnlTabs.Paint += (s, e) => {
                using (Pen pen = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(pen, 0, pnlTabs.Height - 1, pnlTabs.Width, pnlTabs.Height - 1); }
            };

            btnTabCatalog = CreateTab("Blueprint Catalog", IconChar.Book);
            btnTabStock = CreateTab("Physical Stock", IconChar.Boxes);

            btnTabCatalog.Click += (s, e) => SwitchTab("Catalog");
            btnTabStock.Click += (s, e) => SwitchTab("Stock");

            pnlTabs.Controls.Add(btnTabStock);
            pnlTabs.Controls.Add(btnTabCatalog);

            // 2. MAIN CONTENT AREA
            pnlContent = new SmoothPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 20, 0, 0) };

            // 3. CATALOG TAB 
            pnlCatalogTab = new SmoothPanel { Dock = DockStyle.Fill };
            pnlCatalogToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpCatLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };
            FlowLayoutPanel flpCatRight = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            Control txtSearchCatalog = CreateSearchInput("Search Item Code or Specs...", 260);
            FontAwesome.Sharp.IconButton btnSearchCatalog = CreateButton("Search", IconChar.Search, "Primary");
            Control cmbCategory = CreateComboInput(new[] { "All Categories", "Motherboard", "Processor", "RAM", "Graphics Card", "Storage" }, 160);
            Control cmbCondition = CreateComboInput(new[] { "Condition: All", "Brand New", "Second-hand" }, 160);
            flpCatLeft.Controls.AddRange(new Control[] { txtSearchCatalog, btnSearchCatalog, cmbCategory, cmbCondition });

            FontAwesome.Sharp.IconButton btnNewBlueprint = CreateButton("New Blueprint", IconChar.Plus, "Primary");
            FontAwesome.Sharp.IconButton btnCategories = CreateButton("Categories", IconChar.List, "Secondary");
            FontAwesome.Sharp.IconButton btnSort = CreateButton("Sort", IconChar.Sort, "Sort");
            flpCatRight.Controls.AddRange(new Control[] { btnNewBlueprint, btnCategories, btnSort });

            pnlCatalogToolbar.Controls.Add(flpCatLeft);
            pnlCatalogToolbar.Controls.Add(flpCatRight);

            pnlCatalogGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            SmoothPanel pnlGridGap1 = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

            dgvCatalog = CreateDataGridView();
            SetupCatalogColumns();
            pnlCatalogGridContainer.Controls.Add(dgvCatalog);

            pnlCatalogTab.Controls.Add(pnlCatalogGridContainer);
            pnlCatalogTab.Controls.Add(pnlGridGap1);
            pnlCatalogTab.Controls.Add(pnlCatalogToolbar);

            // 4. STOCK TAB 
            pnlStockTab = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            pnlStockToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpStockLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            Control txtSearchStock = CreateSearchInput("Scan or Search Serial Number...", 300);
            FontAwesome.Sharp.IconButton btnSearchStock = CreateButton("Search", IconChar.Search, "Primary");
            Control cmbStatus = CreateComboInput(new[] { "Status: All", "Available", "Defective" }, 160);

            flpStockLeft.Controls.AddRange(new Control[] { txtSearchStock, btnSearchStock, cmbStatus });
            pnlStockToolbar.Controls.Add(flpStockLeft);

            pnlStockGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            SmoothPanel pnlGridGap2 = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent };

            dgvStock = CreateDataGridView();
            SetupStockColumns();
            pnlStockGridContainer.Controls.Add(dgvStock);

            pnlStockTab.Controls.Add(pnlStockGridContainer);
            pnlStockTab.Controls.Add(pnlGridGap2);
            pnlStockTab.Controls.Add(pnlStockToolbar);

            pnlContent.Controls.Add(pnlCatalogTab);
            pnlContent.Controls.Add(pnlStockTab);
            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTabs);
        }

        // =========================================================================
        // HTML UI GENERATORS 
        // =========================================================================

        private TabButton CreateTab(string text, IconChar icon)
        {
            return new TabButton
            {
                Text = "  " + text,
                IconChar = icon,
                IconSize = 22,
                Size = new Size(240, 52),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Padding = new Padding(25, 0, 0, 0)
            };
        }

        private Control CreateSearchInput(string placeholder, int width)
        {
            RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 0, 10, 0) };

            IconPictureBox icon = new IconPictureBox { IconChar = IconChar.Search, IconSize = 18, Size = new Size(24, 18), Dock = DockStyle.Left, BackColor = Color.Transparent };
            TextBox txt = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5F), Text = placeholder };

            txt.GotFocus += (s, e) => { if (txt.Text == placeholder) txt.Text = ""; };
            txt.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) txt.Text = placeholder; };

            wrapper.Controls.Add(txt);
            wrapper.Controls.Add(icon);

            _inputWrappers.Add(wrapper);
            _textInputs.Add(txt);
            return wrapper;
        }

        private Control CreateComboInput(string[] items, int width)
        {
            RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(10, 7, 10, 7), Margin = new Padding(0, 0, 10, 0) };

            ComboBox cmb = new ComboBox { Dock = DockStyle.Fill, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10F) };
            cmb.Items.AddRange(items);
            if (cmb.Items.Count > 0) cmb.SelectedIndex = 0;

            wrapper.Controls.Add(cmb);
            _inputWrappers.Add(wrapper);
            _comboInputs.Add(cmb);
            return wrapper;
        }

        private FontAwesome.Sharp.IconButton CreateButton(string text, IconChar icon, string type)
        {
            FontAwesome.Sharp.IconButton btn = new FontAwesome.Sharp.IconButton
            {
                Text = "  " + text,
                IconChar = icon,
                IconSize = 18,
                Height = 38,
                AutoSize = true,
                Padding = new Padding(15, 0, 15, 0),
                Margin = new Padding(10, 0, 0, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Tag = type
            };
            btn.FlatAppearance.BorderSize = 0;

            if (type == "Primary" && text == "Search") btn.Margin = new Padding(0, 0, 10, 0);
            _buttons.Add(btn);
            return btn;
        }

        // =========================================================================
        // DATAGRIDVIEW SETUP
        // =========================================================================

        private SmoothGrid CreateDataGridView()
        {
            return new SmoothGrid
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
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

        private void SetupCatalogColumns()
        {
            dgvCatalog.Columns.Add("ColCode", "ITEM CODE");
            dgvCatalog.Columns.Add("ColCat", "CATEGORY");
            dgvCatalog.Columns.Add("ColSpecs", "HARDWARE SPECS");
            dgvCatalog.Columns.Add("ColCond", "CONDITION");
            dgvCatalog.Columns.Add("ColValue", "VALUE (SELLING)");
            dgvCatalog.Columns.Add("ColStock", "PHYSICAL STOCK");

            DataGridViewImageColumn colEdit = new DataGridViewImageColumn { HeaderText = "ACTIONS", Name = "ColEdit", Image = SafeGetIcon(IconChar.Edit, Color.Gray), Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            DataGridViewImageColumn colDel = new DataGridViewImageColumn { HeaderText = "", Name = "ColDel", Image = SafeGetIcon(IconChar.Trash, Color.FromArgb(239, 68, 68)), Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };

            dgvCatalog.Columns.Add(colEdit);
            dgvCatalog.Columns.Add(colDel);

            dgvCatalog.Columns["ColCode"].DefaultCellStyle.Font = new Font("Consolas", 11F, FontStyle.Bold);
            dgvCatalog.Columns["ColStock"].DefaultCellStyle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            dgvCatalog.Columns["ColValue"].DefaultCellStyle.Format = "C2";
        }

        private void SetupStockColumns()
        {
            dgvStock.Columns.Add("ColSerial", "SERIAL NUMBER");
            dgvStock.Columns.Add("ColRef", "ITEM CODE (REF)");
            dgvStock.Columns.Add("ColOrigin", "PO NUMBER (ORIGIN)");
            dgvStock.Columns.Add("ColSupp", "SUPPLIER ID");
            dgvStock.Columns.Add("ColStatus", "STATUS");

            DataGridViewImageColumn colFlag = new DataGridViewImageColumn { HeaderText = "ACTIONS", Name = "ColFlag", Image = SafeGetIcon(IconChar.Flag, Color.Gray), Width = 60, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
            dgvStock.Columns.Add(colFlag);

            dgvStock.Columns["ColSerial"].DefaultCellStyle.Font = new Font("Consolas", 11F, FontStyle.Bold);
        }

        private void PopulateDummyData()
        {
            dgvCatalog.Rows.Add(new object[] { "ITM-MB-001", "Motherboard", "MSI MAG B650 Tomahawk WiFi (AM5)", "Brand New", 13000.00, "5", null, null });
            dgvCatalog.Rows.Add(new object[] { "ITM-CPU-042", "Processor", "AMD Ryzen 5 8500G (AM5)", "Second-hand", 9500.00, "2", null, null });

            dgvStock.Rows.Add(new object[] { "SN-8821-A90B", "ITM-MB-001", "PO-2026-089", "SUP-TechSource", "Available", null });
            dgvStock.Rows.Add(new object[] { "SN-8821-A90C", "ITM-MB-001", "PO-2026-089", "SUP-TechSource", "Available", null });
            dgvStock.Rows.Add(new object[] { "SN-1102-X44F", "ITM-CPU-042", "PO-2026-012", "SUP-LocalScrap", "Defective", null });
        }

        // =========================================================================
        // THEME & TOGGLE LOGIC 
        // =========================================================================
        private void SwitchTab(string tabName)
        {
            bool isCatalog = tabName == "Catalog";

            pnlCatalogTab.Visible = isCatalog;
            pnlStockTab.Visible = !isCatalog;

            btnTabCatalog.IsActive = isCatalog;
            btnTabStock.IsActive = !isCatalog;

            ApplyTheme();
        }

        public void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace;
            pnlTabs.BackColor = UITheme.CurrentWorkspace;
            pnlContent.BackColor = UITheme.CurrentWorkspace;
            pnlCatalogTab.BackColor = UITheme.CurrentWorkspace;
            pnlStockTab.BackColor = UITheme.CurrentWorkspace;

            pnlCatalogToolbar.BackColor = UITheme.CurrentPanel;
            pnlStockToolbar.BackColor = UITheme.CurrentPanel;
            pnlCatalogToolbar.BorderColor = UITheme.CurrentBorder;
            pnlStockToolbar.BorderColor = UITheme.CurrentBorder;

            pnlCatalogGridContainer.BackColor = UITheme.CurrentPanel;
            pnlStockGridContainer.BackColor = UITheme.CurrentPanel;
            pnlCatalogGridContainer.BorderColor = UITheme.CurrentBorder;
            pnlStockGridContainer.BorderColor = UITheme.CurrentBorder;

            btnTabCatalog.ForeColor = btnTabCatalog.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText;
            btnTabCatalog.IconColor = btnTabCatalog.ForeColor;
            btnTabStock.ForeColor = btnTabStock.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText;
            btnTabStock.IconColor = btnTabStock.ForeColor;

            foreach (RoundedPanel wrap in _inputWrappers)
            {
                wrap.BackColor = UITheme.CurrentInputBg;
                wrap.BorderColor = UITheme.CurrentBorder;
                foreach (Control c in wrap.Controls)
                {
                    if (c is IconPictureBox icon) icon.IconColor = UITheme.CurrentIcon;
                }
            }
            foreach (TextBox txt in _textInputs) { txt.BackColor = UITheme.CurrentInputBg; txt.ForeColor = UITheme.CurrentText; }
            foreach (ComboBox cmb in _comboInputs) { cmb.BackColor = UITheme.CurrentInputBg; cmb.ForeColor = UITheme.CurrentText; }

            foreach (FontAwesome.Sharp.IconButton btn in _buttons)
            {
                string type = btn.Tag.ToString();
                if (type == "Primary")
                {
                    btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                    btn.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White;
                }
                else if (type == "Secondary" || type == "Sort")
                {
                    btn.BackColor = UITheme.IsDarkMode ? (type == "Sort" ? Color.FromArgb(108, 117, 125) : UITheme.SecondaryDark) : Color.Transparent;
                    btn.ForeColor = UITheme.IsDarkMode ? Color.White : UITheme.CurrentText;
                    if (!UITheme.IsDarkMode) { btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = UITheme.CurrentBorder; }
                    else btn.FlatAppearance.BorderSize = 0;
                }
                btn.IconColor = btn.ForeColor;
            }

            StyleGridTheme(dgvCatalog);
            StyleGridTheme(dgvStock);

            this.Invalidate(true);
        }

        private void StyleGridTheme(DataGridView dgv)
        {
            dgv.BackgroundColor = UITheme.CurrentPanel;
            dgv.GridColor = UITheme.CurrentBorder;

            dgv.DefaultCellStyle.BackColor = UITheme.CurrentPanel;
            dgv.DefaultCellStyle.ForeColor = UITheme.CurrentText;
            dgv.DefaultCellStyle.SelectionBackColor = UITheme.IsDarkMode ? Color.FromArgb(20, 255, 255, 255) : Color.FromArgb(20, 10, 36, 64);
            dgv.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;

            foreach (DataGridViewRow row in dgv.Rows)
            {
                // ADDED PROPER NULL/CONTAINS CHECKS HERE TO PREVENT EXCEPTIONS
                if (dgv.Columns.Contains("ColCode") && row.Cells["ColCode"] != null)
                    row.Cells["ColCode"].Style.ForeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;

                if (dgv.Columns.Contains("ColSerial") && row.Cells["ColSerial"] != null)
                    row.Cells["ColSerial"].Style.ForeColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;

                if (dgv.Columns.Contains("ColStatus") && row.Cells["ColStatus"].Value != null)
                {
                    string status = row.Cells["ColStatus"].Value.ToString();
                    if (status == "Available") row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(16, 185, 129);
                    else if (status == "Defective") row.Cells["ColStatus"].Style.ForeColor = Color.FromArgb(239, 68, 68);
                }
            }
        }
    }
}