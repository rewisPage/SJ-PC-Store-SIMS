using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Runtime.InteropServices;

namespace SJ_PC_Store_SIMS.Views
{
    public class ReportView : UserControl
    {
        // =========================================================================
        // CUSTOM ENGINE COMPONENTS (Scraped from InventoryView & SalesView)
        // =========================================================================
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

        private class ThemedMonthCalendar : MonthCalendar
        {
            private Color _backColor = SystemColors.Window;
            public new Color BackColor { get => _backColor; set { _backColor = value; this.Invalidate(); } }
            protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); SetWindowTheme(this.Handle, "", ""); }
        }

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

            public void ApplyTheme()
            {
                Color bg = UITheme.CurrentInputBg; this.BackColor = bg;
                txtDate.BackColor = bg; txtDate.ForeColor = UITheme.CurrentText;
                btnDrop.BackColor = bg; btnDrop.ForeColor = UITheme.CurrentText;
                if (popup != null && !popup.IsDisposed) { popup.BackColor = bg; if (monthCal != null) { monthCal.BackColor = bg; monthCal.ForeColor = UITheme.CurrentText; monthCal.TitleBackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234); monthCal.TitleForeColor = UITheme.CurrentText; monthCal.TrailingForeColor = UITheme.MutedText; monthCal.Invalidate(); } }
            }
        }

        // =========================================================================
        // VIEW VARIABLES
        // =========================================================================
        private ReportController _reportController;
        private InventoryController _inventoryController; // ADD THIS
        private string _activeUserId;
        private string userFullName;

        private SmoothPanel pnlTabs, pnlContent, pnlSalesTab, pnlInventoryTab, pnlProcurementTab, pnlStocksTab;
        private RoundedPanel pnlSalesToolbar, pnlInventoryToolbar, pnlProcurementToolbar, pnlStockToolbar;
        private TabButton btnTabSales, btnTabInventory, btnTabProcurement, btnTabStocks;
        private SmoothGrid dgvSales, dgvInventory, dgvProcurement, dgvStocks;

        // Toolbar Filters
        private ThemedDatePicker dtpSalesFrom, dtpSalesTo, dtpProcFrom, dtpProcTo;
        private DarkComboBox cmbSalesStatus, cmbProcStatus, cmbInvCategory, cmbStockStatus;
        private TextBox txtSearchSales, txtSearchProc, txtSearchInv, txtSearchStock;

        // Data Models & Pagination States
        private List<SalesReportModel> _salesData = new List<SalesReportModel>();
        private List<InventoryReportModel> _inventoryData = new List<InventoryReportModel>();
        private List<ProcurementReportModel> _procurementData = new List<ProcurementReportModel>();
        private List<StockReportModel> _stocksData = new List<StockReportModel>();

        private int _salesPage = 0, _invPage = 0, _procPage = 0, _stocksPage = 0;
        private const int PAGE_SIZE = 10;
        private Label lblSalesPage, lblInvPage, lblProcPage, lblStocksPage;
        private List<RoundedPanel> _inputWrappers = new List<RoundedPanel>();
        private List<TextBox> _textInputs = new List<TextBox>();
        private List<DarkComboBox> _comboInputs = new List<DarkComboBox>();
        private List<IconButton> _buttons = new List<IconButton>();

        // PDF Generation State Tracker
        private int _pdfPrintIndex = 0;
        private string _activePrintTab = "";

        private UserModel _currentUser;

        public ReportView(string currentUserId, string firstName, string lastName)
        {
            _activeUserId = currentUserId;
            _reportController = new ReportController();
            _inventoryController = new InventoryController();
            userFullName = firstName + " " + lastName;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.Padding = new Padding(35, 15, 35, 35);
            this.Margin = new Padding(0);

            InitializeUI();
            ApplyTheme();
            SwitchTab("Sales");
        }

        // =========================================================================
        // INITIALIZATION
        // =========================================================================
        private void InitializeUI()
        {
            pnlTabs = new SmoothPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(0) };
            pnlTabs.Paint += (s, e) => { using (Pen pen = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(pen, 0, pnlTabs.Height - 1, pnlTabs.Width, pnlTabs.Height - 1); } };

            btnTabSales = CreateTab("Sales Report", IconChar.ChartLine);
            btnTabInventory = CreateTab("Inventory Report", IconChar.Boxes);
            btnTabProcurement = CreateTab("Procurement Report", IconChar.TruckLoading);
            btnTabStocks = CreateTab("Stocks Report", IconChar.Cubes);


            btnTabSales.Click += (s, e) => SwitchTab("Sales");
            btnTabInventory.Click += (s, e) => SwitchTab("Inventory");
            btnTabProcurement.Click += (s, e) => SwitchTab("Procurement");
            btnTabStocks.Click += (s, e) => SwitchTab("Stocks");

            pnlTabs.Controls.AddRange(new Control[] { btnTabStocks, btnTabProcurement, btnTabInventory, btnTabSales });
            pnlContent = new SmoothPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 20, 0, 0) };

            InitializeSalesTab();
            InitializeInventoryTab();
            InitializeProcurementTab();
            InitializeStocksTab();

            pnlContent.Controls.AddRange(new Control[] { pnlProcurementTab, pnlInventoryTab, pnlSalesTab, pnlStocksTab });
            this.Controls.Add(pnlContent); this.Controls.Add(pnlTabs);
        }

        private void InitializeSalesTab()
        {
            pnlSalesTab = new SmoothPanel { Dock = DockStyle.Fill };
            pnlSalesToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            dtpSalesFrom = new ThemedDatePicker { Value = DateTime.Now.AddDays(-30) };
            dtpSalesTo = new ThemedDatePicker { Value = DateTime.Now };
            Control wFrom = CreateInputWrapper(dtpSalesFrom, 130);
            Control wTo = CreateInputWrapper(dtpSalesTo, 130);

            Control cmbStatWrapper = CreateComboInput(new[] { "All", "Quotation", "Ordered", "Paid", "Completed", "Cancelled" }, 140, out cmbSalesStatus);
            Control txtSearchWrapper = CreateSearchInput("Search Receipt...", 250, out txtSearchSales, () => FetchSalesData());

            flpLeft.Controls.AddRange(new Control[] { wFrom, wTo, cmbStatWrapper, txtSearchWrapper });

            IconButton btnFilter = CreateButton("Apply Filter", IconChar.Filter, "Primary");
            btnFilter.Click += (s, e) => FetchSalesData();
            flpLeft.Controls.Add(btnFilter);

            // ADD THIS NEW BLOCK: Reset Button for Sales
            IconButton btnResetSales = CreateButton("Reset", IconChar.Undo, "Secondary");
            btnResetSales.Click += (s, e) =>
            {
                dtpSalesFrom.Value = DateTime.Now.AddDays(-30);
                dtpSalesTo.Value = DateTime.Now;
                if (cmbSalesStatus.Items.Count > 0) cmbSalesStatus.SelectedIndex = 0;
                txtSearchSales.Text = "Search Receipt...";
                FetchSalesData();
            };
            flpLeft.Controls.Add(btnResetSales);

            IconButton btnExport = CreateButton("Export PDF", IconChar.FilePdf, "ActionAdd");
            btnExport.Dock = DockStyle.Right;
            btnExport.Click += (s, e) => { _activePrintTab = "Sales"; GeneratePDF(); };

            pnlSalesToolbar.Controls.Add(flpLeft); pnlSalesToolbar.Controls.Add(btnExport);

            SmoothPanel pnlGridGap = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent }; // Spacer Panel for visual separation

            RoundedPanel pnlGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            dgvSales = CreateDataGridView();
            dgvSales.Columns.Add("Date", "DATE"); dgvSales.Columns.Add("Receipt", "RECEIPT ID");
            dgvSales.Columns.Add("Customer", "CUSTOMER"); dgvSales.Columns.Add("Status", "STATUS");
            dgvSales.Columns.Add("Total", "GRAND TOTAL");

            // Center align BOTH the header and the data cells
            dgvSales.Columns["Total"].DefaultCellStyle.Format = "C2";
            dgvSales.Columns["Total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvSales.Columns["Total"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Independent Paint Event
            dgvSales.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 49, dgvSales.Width, 49); }
                if (dgvSales.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "No sales records found for the selected filters.", new Font("Segoe UI", 11F, FontStyle.Italic), dgvSales.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            pnlGridContainer.Controls.Add(dgvSales);

            Panel pnlPagination = CreatePaginationPanel(ref lblSalesPage,
                () => { if (_salesPage > 0) { _salesPage--; RenderSalesGrid(); } },
                () => { if ((_salesPage + 1) * PAGE_SIZE < _salesData.Count) { _salesPage++; RenderSalesGrid(); } });

            pnlSalesTab.Controls.Add(pnlGridContainer);
            pnlSalesTab.Controls.Add(pnlPagination);
            pnlSalesTab.Controls.Add(pnlGridGap);
            pnlSalesTab.Controls.Add(pnlSalesToolbar);
        }

        private void InitializeInventoryTab()
        {
            pnlInventoryTab = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            pnlInventoryToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            // Fetch dynamic categories from the database via the Inventory Controller
            List<string> categoryList = new List<string> { "All Categories" };
            categoryList.AddRange(_inventoryController.GetCategories());

            Control cmbCatWrapper = CreateComboInput(categoryList.ToArray(), 160, out cmbInvCategory);
            Control txtSearchWrapper = CreateSearchInput("Search Item Code...", 250, out txtSearchInv, () => FetchInventoryData());
            flpLeft.Controls.AddRange(new Control[] { cmbCatWrapper, txtSearchWrapper });

            IconButton btnFilter = CreateButton("Apply Filter", IconChar.Filter, "Primary");
            btnFilter.Click += (s, e) => FetchInventoryData();
            flpLeft.Controls.Add(btnFilter);

            // ADD THIS NEW BLOCK: Reset Button for Inventory
            IconButton btnResetInv = CreateButton("Reset", IconChar.Undo, "Secondary");
            btnResetInv.Click += (s, e) =>
            {
                if (cmbInvCategory.Items.Count > 0) cmbInvCategory.SelectedIndex = 0;
                txtSearchInv.Text = "Search Item Code...";
                FetchInventoryData();
            };
            flpLeft.Controls.Add(btnResetInv);

            IconButton btnExport = CreateButton("Export PDF", IconChar.FilePdf, "ActionAdd");
            btnExport.Dock = DockStyle.Right;
            btnExport.Click += (s, e) => { _activePrintTab = "Inventory"; GeneratePDF(); };

            pnlInventoryToolbar.Controls.Add(flpLeft); pnlInventoryToolbar.Controls.Add(btnExport);

            SmoothPanel pnlGridGap = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent }; // Spacer Panel for visual separation

            RoundedPanel pnlGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            dgvInventory = CreateDataGridView();
            dgvInventory.Columns.Add("Code", "ITEM CODE"); dgvInventory.Columns.Add("Cat", "CATEGORY");
            dgvInventory.Columns.Add("Specs", "SPECIFICATIONS"); dgvInventory.Columns.Add("Stock", "AVAILABLE STOCK");
            dgvInventory.Columns.Add("UnitVal", "UNIT VALUE"); dgvInventory.Columns.Add("TotalVal", "TOTAL ASSET VALUE");

            dgvInventory.Columns["UnitVal"].DefaultCellStyle.Format = "C2";
            dgvInventory.Columns["TotalVal"].DefaultCellStyle.Format = "C2";
            dgvInventory.Columns["TotalVal"].DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            // Center align BOTH the header and the data cells
            dgvInventory.Columns["TotalVal"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvInventory.Columns["TotalVal"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Independent Paint Event
            dgvInventory.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 49, dgvInventory.Width, 49); }
                if (dgvInventory.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "No inventory records found for the selected filters.", new Font("Segoe UI", 11F, FontStyle.Italic), dgvInventory.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            pnlGridContainer.Controls.Add(dgvInventory);

            Panel pnlPagination = CreatePaginationPanel(ref lblInvPage,
                () => { if (_invPage > 0) { _invPage--; RenderInventoryGrid(); } },
                () => { if ((_invPage + 1) * PAGE_SIZE < _inventoryData.Count) { _invPage++; RenderInventoryGrid(); } });

            pnlInventoryTab.Controls.Add(pnlGridContainer);
            pnlInventoryTab.Controls.Add(pnlPagination);
            pnlInventoryTab.Controls.Add(pnlGridGap);
            pnlInventoryTab.Controls.Add(pnlInventoryToolbar);
        }

        private void InitializeProcurementTab()
        {
            pnlProcurementTab = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            pnlProcurementToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            dtpProcFrom = new ThemedDatePicker { Value = DateTime.Now.AddDays(-30) };
            dtpProcTo = new ThemedDatePicker { Value = DateTime.Now };
            Control wFrom = CreateInputWrapper(dtpProcFrom, 130);
            Control wTo = CreateInputWrapper(dtpProcTo, 130);

            Control cmbStatWrapper = CreateComboInput(new[] { "All", "Draft", "Pending Approval", "Ordered", "Completed", "Cancelled" }, 160, out cmbProcStatus);
            Control txtSearchWrapper = CreateSearchInput("Search PO...", 250, out txtSearchProc, () => FetchProcurementData());

            flpLeft.Controls.AddRange(new Control[] { wFrom, wTo, cmbStatWrapper, txtSearchWrapper });

            IconButton btnFilter = CreateButton("Apply Filter", IconChar.Filter, "Primary");
            btnFilter.Click += (s, e) => FetchProcurementData();
            flpLeft.Controls.Add(btnFilter);

            // ADD THIS NEW BLOCK: Reset Button for Procurement
            IconButton btnResetProc = CreateButton("Reset", IconChar.Undo, "Secondary");
            btnResetProc.Click += (s, e) =>
            {
                dtpProcFrom.Value = DateTime.Now.AddDays(-30);
                dtpProcTo.Value = DateTime.Now;
                if (cmbProcStatus.Items.Count > 0) cmbProcStatus.SelectedIndex = 0;
                txtSearchProc.Text = "Search PO...";
                FetchProcurementData();
            };
            flpLeft.Controls.Add(btnResetProc);

            IconButton btnExport = CreateButton("Export PDF", IconChar.FilePdf, "ActionAdd");
            btnExport.Dock = DockStyle.Right;
            btnExport.Click += (s, e) => { _activePrintTab = "Procurement"; GeneratePDF(); };

            pnlProcurementToolbar.Controls.Add(flpLeft); pnlProcurementToolbar.Controls.Add(btnExport);

            SmoothPanel pnlGridGap = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent }; // Spacer Panel for visual separation

            RoundedPanel pnlGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            dgvProcurement = CreateDataGridView();
            dgvProcurement.Columns.Add("Date", "DATE"); dgvProcurement.Columns.Add("PO", "PO NUMBER");
            dgvProcurement.Columns.Add("Supplier", "SUPPLIER"); dgvProcurement.Columns.Add("Status", "STATUS");
            dgvProcurement.Columns.Add("Total", "GRAND TOTAL");

            // Center align BOTH the header and the data cells
            dgvProcurement.Columns["Total"].DefaultCellStyle.Format = "C2";
            dgvProcurement.Columns["Total"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvProcurement.Columns["Total"].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // Independent Paint Event
            dgvProcurement.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 49, dgvProcurement.Width, 49); }
                if (dgvProcurement.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "No procurement records found for the selected filters.", new Font("Segoe UI", 11F, FontStyle.Italic), dgvProcurement.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            pnlGridContainer.Controls.Add(dgvProcurement);

            Panel pnlPagination = CreatePaginationPanel(ref lblProcPage,
                () => { if (_procPage > 0) { _procPage--; RenderProcurementGrid(); } },
                () => { if ((_procPage + 1) * PAGE_SIZE < _procurementData.Count) { _procPage++; RenderProcurementGrid(); } });

            pnlProcurementTab.Controls.Add(pnlGridContainer);
            pnlProcurementTab.Controls.Add(pnlPagination);
            pnlProcurementTab.Controls.Add(pnlGridGap);
            pnlProcurementTab.Controls.Add(pnlProcurementToolbar);
        }

        private void InitializeStocksTab()
        {
            pnlStocksTab = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            pnlStockToolbar = new RoundedPanel { Dock = DockStyle.Top, Height = 65, Padding = new Padding(15, 12, 15, 12), Margin = new Padding(0, 0, 0, 20), BorderRadius = 6 };

            FlowLayoutPanel flpLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0) };

            Control cmbStatWrapper = CreateComboInput(new[] { "All", "Available", "Sold", "Defective", "Returned", "RMA" }, 160, out cmbStockStatus);
            Control txtSearchWrapper = CreateSearchInput("Search Serial/Code...", 250, out txtSearchStock, () => FetchStocksData());

            flpLeft.Controls.AddRange(new Control[] { cmbStatWrapper, txtSearchWrapper });

            IconButton btnFilter = CreateButton("Apply Filter", IconChar.Filter, "Primary");
            btnFilter.Click += (s, e) => FetchStocksData();
            flpLeft.Controls.Add(btnFilter);

            // ADD THIS NEW BLOCK: Reset Button for Stocks
            IconButton btnResetStocks = CreateButton("Reset", IconChar.Undo, "Secondary");
            btnResetStocks.Click += (s, e) =>
            {
                if (cmbStockStatus.Items.Count > 0) cmbStockStatus.SelectedIndex = 0;
                txtSearchStock.Text = "Search Serial/Code...";
                FetchStocksData();
            };
            flpLeft.Controls.Add(btnResetStocks);

            IconButton btnExport = CreateButton("Export PDF", IconChar.FilePdf, "ActionAdd");
            btnExport.Dock = DockStyle.Right;
            btnExport.Click += (s, e) => { _activePrintTab = "Stocks"; GeneratePDF(); };

            pnlStockToolbar.Controls.Add(flpLeft); pnlStockToolbar.Controls.Add(btnExport);

            SmoothPanel pnlGridGap = new SmoothPanel { Dock = DockStyle.Top, Height = 20, BackColor = Color.Transparent }; // Spacer Panel for visual separation

            RoundedPanel pnlGridContainer = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, Padding = new Padding(1) };
            dgvStocks = CreateDataGridView();
            dgvStocks.Columns.Add("Serial", "SERIAL NUMBER");
            dgvStocks.Columns.Add("Code", "ITEM CODE");
            dgvStocks.Columns.Add("Specs", "SPECIFICATIONS");
            dgvStocks.Columns.Add("Status", "STATUS");
            dgvStocks.Columns.Add("PO", "PO NUMBER");
            dgvStocks.Columns.Add("Supp", "SUPPLIER");

            dgvStocks.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 49, dgvStocks.Width, 49); }
                if (dgvStocks.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "No stock records found for the selected filters.", new Font("Segoe UI", 11F, FontStyle.Italic), dgvStocks.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            pnlGridContainer.Controls.Add(dgvStocks);

            Panel pnlPagination = CreatePaginationPanel(ref lblStocksPage,
                () => { if (_stocksPage > 0) { _stocksPage--; RenderStocksGrid(); } },
                () => { if ((_stocksPage + 1) * PAGE_SIZE < _stocksData.Count) { _stocksPage++; RenderStocksGrid(); } });

            pnlStocksTab.Controls.Add(pnlGridContainer);
            pnlStocksTab.Controls.Add(pnlPagination);
            pnlStocksTab.Controls.Add(pnlGridGap);
            pnlStocksTab.Controls.Add(pnlStockToolbar);
        }

        // =========================================================================
        // DATA FETCHING & PAGINATION
        // =========================================================================
        private void FetchSalesData()
        {
            string search = txtSearchSales.Text == "Search Receipt..." ? "" : txtSearchSales.Text;
            _salesData = _reportController.GetFilteredSales(dtpSalesFrom.Value, dtpSalesTo.Value, cmbSalesStatus.SelectedItem.ToString(), search);
            _salesPage = 0; RenderSalesGrid();
        }

        private void FetchInventoryData()
        {
            string search = txtSearchInv.Text == "Search Item Code..." ? "" : txtSearchInv.Text;
            _inventoryData = _reportController.GetFilteredInventory(cmbInvCategory.SelectedItem.ToString(), search);
            _invPage = 0; RenderInventoryGrid();
        }

        private void FetchProcurementData()
        {
            string search = txtSearchProc.Text == "Search PO..." ? "" : txtSearchProc.Text;
            _procurementData = _reportController.GetFilteredProcurement(dtpProcFrom.Value, dtpProcTo.Value, cmbProcStatus.SelectedItem.ToString(), search);
            _procPage = 0; RenderProcurementGrid();
        }

        private void FetchStocksData()
        {
            string search = txtSearchStock.Text == "Search Serial/Code..." ? "" : txtSearchStock.Text;
            _stocksData = _reportController.GetFilteredStocks(cmbStockStatus.SelectedItem.ToString(), search);
            _stocksPage = 0; RenderStocksGrid();
        }

        private void RenderSalesGrid()
        {
            dgvSales.Rows.Clear();
            var pageData = _salesData.Skip(_salesPage * PAGE_SIZE).Take(PAGE_SIZE).ToList();

            foreach (var item in pageData)
            {
                // FIX: Display a dash instead of the price if the order is Cancelled or Returned
                object displayTotal = (item.Status == "Cancelled" || item.Status == "Returned") ? "-" : (object)item.GrandTotal;

                dgvSales.Rows.Add(item.OrderDate.ToString("MMM dd, yyyy"), item.ReceiptID, item.CustomerName, item.Status, displayTotal);
            }

            UpdatePaginationLabel(lblSalesPage, _salesPage, _salesData.Count);
        }

        private void RenderInventoryGrid()
        {
            dgvInventory.Rows.Clear();
            var pageData = _inventoryData.Skip(_invPage * PAGE_SIZE).Take(PAGE_SIZE).ToList();
            foreach (var item in pageData)
            {
                dgvInventory.Rows.Add(item.ItemCode, item.Category, item.Specs, item.AvailableStock, item.UnitValue, item.TotalAssetValue);
            }
            UpdatePaginationLabel(lblInvPage, _invPage, _inventoryData.Count);
        }

        private void RenderProcurementGrid()
        {
            dgvProcurement.Rows.Clear();
            var pageData = _procurementData.Skip(_procPage * PAGE_SIZE).Take(PAGE_SIZE).ToList();
            foreach (var item in pageData)
            {
                dgvProcurement.Rows.Add(item.OrderDate.ToString("MMM dd, yyyy"), item.PO_Number, item.SupplierName, item.Status, item.GrandTotal);
            }
            UpdatePaginationLabel(lblProcPage, _procPage, _procurementData.Count);
        }

        private void RenderStocksGrid()
        {
            dgvStocks.Rows.Clear();
            var pageData = _stocksData.Skip(_stocksPage * PAGE_SIZE).Take(PAGE_SIZE).ToList();
            foreach (var item in pageData)
            {
                dgvStocks.Rows.Add(item.SerialNumber, item.ItemCode, item.Specs, item.Status, item.PO_Number, item.SupplierName);
            }
            UpdatePaginationLabel(lblStocksPage, _stocksPage, _stocksData.Count);
        }

        private void UpdatePaginationLabel(Label lbl, int currentPage, int totalRecords)
        {
            if (totalRecords == 0) lbl.Text = "No records found.";
            else
            {
                int totalPages = (int)Math.Ceiling(totalRecords / (double)PAGE_SIZE);
                lbl.Text = $"Page {currentPage + 1} of {totalPages} (Total Records: {totalRecords})";
            }
        }

        // =========================================================================
        // PDF GENERATION LOGIC
        // =========================================================================
        private void GeneratePDF()
        {
            _pdfPrintIndex = 0; // Reset pagination tracker for PDF
            PrintDocument pd = new PrintDocument();
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            pd.DefaultPageSettings.Landscape = true; // Use Landscape for Reports
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
            g.DrawString($"Report Module: {_activePrintTab.ToUpper()} REPORT", fSub, Brushes.DarkBlue, 50, y); y += 20;
            g.DrawString($"Generated On: {DateTime.Now:MMM dd, yyyy HH:mm:ss}", fN, Brushes.DimGray, 50, y); y += 20;
            g.DrawString($"Generated By: {userFullName}", fN, Brushes.DimGray, 50, y); y += 30; // ADDED THIS LINE

            g.DrawLine(Pens.Black, 50, y, 1100, y); y += 20;

            // Route to specific report printer logic
            if (_activePrintTab == "Sales") PrintSalesRows(g, e, ref y, fB, fN);
            else if (_activePrintTab == "Inventory") PrintInventoryRows(g, e, ref y, fB, fN);
            else if (_activePrintTab == "Procurement") PrintProcurementRows(g, e, ref y, fB, fN);
            else if (_activePrintTab == "Stocks") PrintStocksRows(g, e, ref y, fB, fN);
        }

        private void PrintSalesRows(Graphics g, PrintPageEventArgs e, ref int y, Font fB, Font fN)
        {
            // Table Header
            g.FillRectangle(Brushes.DarkBlue, 50, y, 1050, 30);
            g.DrawString("DATE", fB, Brushes.White, 60, y + 7);
            g.DrawString("RECEIPT ID", fB, Brushes.White, 200, y + 7);
            g.DrawString("CUSTOMER", fB, Brushes.White, 400, y + 7);
            g.DrawString("STATUS", fB, Brushes.White, 750, y + 7);
            g.DrawString("GRAND TOTAL", fB, Brushes.White, 950, y + 7);
            y += 40;

            // Rows with Pagination
            while (_pdfPrintIndex < _salesData.Count)
            {
                var item = _salesData[_pdfPrintIndex];
                g.DrawString(item.OrderDate.ToString("yyyy-MM-dd"), fN, Brushes.Black, 60, y);
                g.DrawString(item.ReceiptID, fN, Brushes.Black, 200, y);
                g.DrawString(item.CustomerName, fN, Brushes.Black, 400, y);
                g.DrawString(item.Status, fN, Brushes.Black, 750, y);

                // FIX: Hide the price string if the order is Cancelled or Returned
                string totalStr = (item.Status == "Cancelled" || item.Status == "Returned") ? "-" : item.GrandTotal.ToString("C2");
                g.DrawString(totalStr, fN, Brushes.Black, 950, y);

                y += 30; _pdfPrintIndex++;
                if (y > 750) { e.HasMorePages = true; return; }
            }

            e.HasMorePages = false;
            _pdfPrintIndex = 0; // End of Document

            // FIX: Filter out Cancelled AND Returned orders from the final PDF revenue sum
            decimal activeSum = _salesData.Where(x => x.Status != "Cancelled" && x.Status != "Returned").Sum(x => x.GrandTotal);

            PrintReportFooter(g, ref y, "TOTAL REVENUE:", activeSum.ToString("C2"), fB);
        }

        private void PrintInventoryRows(Graphics g, PrintPageEventArgs e, ref int y, Font fB, Font fN)
        {
            g.FillRectangle(Brushes.DarkBlue, 50, y, 1050, 30);
            g.DrawString("ITEM CODE", fB, Brushes.White, 60, y + 7);
            g.DrawString("CATEGORY", fB, Brushes.White, 250, y + 7);
            g.DrawString("SPECIFICATIONS", fB, Brushes.White, 450, y + 7);
            g.DrawString("STOCK", fB, Brushes.White, 800, y + 7);
            g.DrawString("TOTAL VALUE", fB, Brushes.White, 950, y + 7);
            y += 40;

            while (_pdfPrintIndex < _inventoryData.Count)
            {
                var item = _inventoryData[_pdfPrintIndex];
                string specs = item.Specs.Length > 40 ? item.Specs.Substring(0, 40) + "..." : item.Specs;

                g.DrawString(item.ItemCode, fN, Brushes.Black, 60, y);
                g.DrawString(item.Category, fN, Brushes.Black, 250, y);
                g.DrawString(specs, fN, Brushes.Black, 450, y);
                g.DrawString(item.AvailableStock.ToString(), fN, Brushes.Black, 800, y);
                g.DrawString(item.TotalAssetValue.ToString("C2"), fN, Brushes.Black, 950, y);

                y += 30; _pdfPrintIndex++;
                if (y > 750) { e.HasMorePages = true; return; }
            }
            e.HasMorePages = false;
            _pdfPrintIndex = 0;
            decimal totalAsset = _inventoryData.Sum(x => x.TotalAssetValue);
            PrintReportFooter(g, ref y, "TOTAL ASSET VALUE:", totalAsset.ToString("C2"), fB);
        }

        private void PrintProcurementRows(Graphics g, PrintPageEventArgs e, ref int y, Font fB, Font fN)
        {
            g.FillRectangle(Brushes.DarkBlue, 50, y, 1050, 30);
            g.DrawString("DATE", fB, Brushes.White, 60, y + 7);
            g.DrawString("PO NUMBER", fB, Brushes.White, 200, y + 7);
            g.DrawString("SUPPLIER", fB, Brushes.White, 400, y + 7);
            g.DrawString("STATUS", fB, Brushes.White, 750, y + 7);
            g.DrawString("GRAND TOTAL", fB, Brushes.White, 950, y + 7);
            y += 40;

            while (_pdfPrintIndex < _procurementData.Count)
            {
                var item = _procurementData[_pdfPrintIndex];
                g.DrawString(item.OrderDate.ToString("yyyy-MM-dd"), fN, Brushes.Black, 60, y);
                g.DrawString(item.PO_Number, fN, Brushes.Black, 200, y);
                g.DrawString(item.SupplierName, fN, Brushes.Black, 400, y);
                g.DrawString(item.Status, fN, Brushes.Black, 750, y);
                g.DrawString(item.GrandTotal.ToString("C2"), fN, Brushes.Black, 950, y);

                y += 30; _pdfPrintIndex++;
                if (y > 750) { e.HasMorePages = true; return; }
            }
            e.HasMorePages = false;
            _pdfPrintIndex = 0;
            // Filter out Cancelled POs from the expenditure sum
            decimal totalProc = _procurementData.Where(x => x.Status != "Cancelled").Sum(x => x.GrandTotal);

            PrintReportFooter(g, ref y, "TOTAL EXPENDITURE:", totalProc.ToString("C2"), fB);
        }

        private void PrintStocksRows(Graphics g, PrintPageEventArgs e, ref int y, Font fB, Font fN)
        {
            g.FillRectangle(Brushes.DarkBlue, 50, y, 1050, 30);
            g.DrawString("SERIAL NUMBER", fB, Brushes.White, 60, y + 7);
            g.DrawString("ITEM CODE", fB, Brushes.White, 300, y + 7);
            g.DrawString("SPECIFICATIONS", fB, Brushes.White, 450, y + 7);
            g.DrawString("STATUS", fB, Brushes.White, 750, y + 7);
            g.DrawString("SUPPLIER", fB, Brushes.White, 900, y + 7);
            y += 40;

            while (_pdfPrintIndex < _stocksData.Count)
            {
                var item = _stocksData[_pdfPrintIndex];
                string specs = item.Specs.Length > 30 ? item.Specs.Substring(0, 30) + "..." : item.Specs;
                string supp = item.SupplierName.Length > 15 ? item.SupplierName.Substring(0, 15) + "..." : item.SupplierName;

                g.DrawString(item.SerialNumber, fN, Brushes.Black, 60, y);
                g.DrawString(item.ItemCode, fN, Brushes.Black, 300, y);
                g.DrawString(specs, fN, Brushes.Black, 450, y);
                g.DrawString(item.Status, fN, Brushes.Black, 750, y);
                g.DrawString(supp, fN, Brushes.Black, 900, y);

                y += 30; _pdfPrintIndex++;
                if (y > 750) { e.HasMorePages = true; return; }
            }
            e.HasMorePages = false; _pdfPrintIndex = 0;

            // Adjust footer for Stocks since it doesn't have a currency sum
            y += 10;
            g.DrawLine(Pens.Gray, 50, y, 1100, y); y += 20;
            g.DrawString("TOTAL UNITS FOUND:", fB, Brushes.DarkBlue, 750, y);
            g.DrawString(_stocksData.Count.ToString(), fB, Brushes.Black, 950, y);
        }

        private void PrintReportFooter(Graphics g, ref int y, string label, string value, Font fB)
        {
            y += 15;

            // Draw a solid line to separate the table from the totals
            g.DrawLine(Pens.Black, 50, y, 1100, y);
            y += 15;

            // Draw the specific label and the value aligned to the right
            g.DrawString(label, fB, Brushes.DarkBlue, 750, y);
            g.DrawString(value, fB, Brushes.Black, 950, y);

            y += 40;

            // Draw a professional end-of-document marker centered on the page
            g.DrawString("*** END OF REPORT ***", new Font("Arial", 9, FontStyle.Italic), Brushes.DimGray, 500, y);
        }

        // =========================================================================
        // UI GENERATORS & THEME ENGINE
        // =========================================================================
        private void SwitchTab(string tabName)
        {
            pnlSalesTab.Visible = tabName == "Sales";
            pnlInventoryTab.Visible = tabName == "Inventory";
            pnlProcurementTab.Visible = tabName == "Procurement";
            pnlStocksTab.Visible = tabName == "Stocks";

            btnTabSales.IsActive = tabName == "Sales";
            btnTabInventory.IsActive = tabName == "Inventory";
            btnTabProcurement.IsActive = tabName == "Procurement";
            btnTabStocks.IsActive = tabName == "Stocks";

            if (tabName == "Sales" && _salesData.Count == 0) FetchSalesData();
            if (tabName == "Inventory" && _inventoryData.Count == 0) FetchInventoryData();
            if (tabName == "Procurement" && _procurementData.Count == 0) FetchProcurementData();
            if (tabName == "Stocks" && _stocksData.Count == 0) FetchStocksData();

            ApplyTheme();
        }

        private TabButton CreateTab(string text, IconChar icon)
        {
            return new TabButton
            {
                Text = "  " + text,
                IconChar = icon,
                IconSize = 22,
                Size = new Size(250, 52),
                Dock = DockStyle.Left,
                TextAlign = ContentAlignment.MiddleCenter,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Padding = new Padding(20, 0, 0, 0)
            };
        }

        private Control CreateInputWrapper(Control innerControl, int width)
        {
            RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 0, 10, 0) };
            innerControl.Dock = DockStyle.Fill; wrapper.Controls.Add(innerControl);
            _inputWrappers.Add(wrapper); return wrapper;
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

        private IconButton CreateButton(string text, IconChar icon, string type)
        {
            IconButton btn = new IconButton { Text = text != "" ? "  " + text : "", IconChar = icon, IconSize = 18, Height = 38, AutoSize = true, Padding = new Padding(15, 0, 15, 0), Margin = new Padding(10, 0, 0, 0), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, TextImageRelation = TextImageRelation.ImageBeforeText, Tag = type };
            btn.FlatAppearance.BorderSize = 0;
            if (type == "ActionAdd") btn.Padding = new Padding(10, 0, 10, 0);
            _buttons.Add(btn); return btn;
        }

        private Panel CreatePaginationPanel(ref Label lblState, Action onPrev, Action onNext)
        {
            Panel pnl = new Panel { Dock = DockStyle.Bottom, Height = 200, Padding = new Padding(20, 10, 20, 0) };
            lblState = new Label { Text = "Page 1 of 1", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), AutoSize = true, ForeColor = UITheme.CurrentText, Location = new Point(20, 35) };

            IconButton btnPrev = CreateButton("Previous", IconChar.ChevronLeft, "Secondary");
            btnPrev.Location = new Point(pnl.Width - 250, 25); btnPrev.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPrev.Click += (s, e) => onPrev();

            IconButton btnNext = CreateButton("Next", IconChar.ChevronRight, "Secondary");
            btnNext.TextImageRelation = TextImageRelation.TextBeforeImage;
            btnNext.Location = new Point(pnl.Width - 120, 25); btnNext.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNext.Click += (s, e) => onNext();

            pnl.Controls.AddRange(new Control[] { lblState, btnPrev, btnNext });
            return pnl;
        }

        private SmoothGrid CreateDataGridView()
        {
            SmoothGrid dgv = new SmoothGrid
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, // Horizontal lines restored
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 50,
                RowTemplate = { Height = 55 },
                Cursor = Cursors.Hand,
                RowHeadersVisible = false // Removed Row Headers
            };

            dgv.DefaultCellStyle.Padding = new Padding(20, 0, 0, 0);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(20, 0, 0, 0);

            // Notice: AdvancedCellBorderStyle was removed here so it doesn't kill the SingleHorizontal lines.

            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.SelectionChanged += (s, e) => dgv.ClearSelection();

            return dgv;
        }

        public void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace; pnlTabs.BackColor = UITheme.CurrentWorkspace; pnlContent.BackColor = UITheme.CurrentWorkspace;

            if (pnlSalesTab != null)
            {
                pnlSalesTab.BackColor = UITheme.CurrentWorkspace;
                pnlSalesToolbar.BackColor = UITheme.CurrentPanel;
                pnlSalesToolbar.BorderColor = UITheme.CurrentBorder;
            }
            if (pnlInventoryTab != null)
            {
                pnlInventoryTab.BackColor = UITheme.CurrentWorkspace;
                pnlInventoryToolbar.BackColor = UITheme.CurrentPanel;
                pnlInventoryToolbar.BorderColor = UITheme.CurrentBorder;
            }
            if (pnlProcurementTab != null)
            {
                pnlProcurementTab.BackColor = UITheme.CurrentWorkspace;
                pnlProcurementToolbar.BackColor = UITheme.CurrentPanel;
                pnlProcurementToolbar.BorderColor = UITheme.CurrentBorder;
            }

            if (pnlStocksTab != null)
            {
                pnlStocksTab.BackColor = UITheme.CurrentWorkspace;
                pnlStockToolbar.BackColor = UITheme.CurrentPanel;
                pnlStockToolbar.BorderColor = UITheme.CurrentBorder;
            }

            btnTabSales.FlatAppearance.MouseDownBackColor = UITheme.CurrentWorkspace;
            btnTabInventory.FlatAppearance.MouseDownBackColor = UITheme.CurrentWorkspace;
            btnTabProcurement.FlatAppearance.MouseDownBackColor = UITheme.CurrentWorkspace;
            btnTabStocks.FlatAppearance.MouseDownBackColor = UITheme.CurrentWorkspace;

            Color hoverColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : UITheme.CurrentPanel;

            btnTabSales.FlatAppearance.MouseOverBackColor = hoverColor;
            btnTabInventory.FlatAppearance.MouseOverBackColor = hoverColor;
            btnTabProcurement.FlatAppearance.MouseOverBackColor = hoverColor;
            btnTabStocks.FlatAppearance.MouseOverBackColor = hoverColor;

            btnTabSales.BackColor = btnTabSales.IsActive ? UITheme.CurrentPanel : Color.Transparent;
            btnTabInventory.BackColor = btnTabInventory.IsActive ? UITheme.CurrentPanel : Color.Transparent;
            btnTabProcurement.BackColor = btnTabProcurement.IsActive ? UITheme.CurrentPanel : Color.Transparent;
            btnTabStocks.BackColor = btnTabStocks.IsActive ? UITheme.CurrentPanel : Color.Transparent;

            btnTabSales.ForeColor = btnTabSales.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabSales.IconColor = btnTabSales.ForeColor;
            btnTabInventory.ForeColor = btnTabInventory.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabInventory.IconColor = btnTabInventory.ForeColor;
            btnTabProcurement.ForeColor = btnTabProcurement.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabProcurement.IconColor = btnTabProcurement.ForeColor;
            btnTabStocks.ForeColor = btnTabStocks.IsActive ? (UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark) : UITheme.MutedText; btnTabStocks.IconColor = btnTabStocks.ForeColor;

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

            if (lblSalesPage != null) lblSalesPage.ForeColor = UITheme.CurrentText;
            if (lblInvPage != null) lblInvPage.ForeColor = UITheme.CurrentText;
            if (lblProcPage != null) lblProcPage.ForeColor = UITheme.CurrentText;
            if (lblStocksPage != null) lblStocksPage.ForeColor = UITheme.CurrentText;


            if (dtpSalesFrom != null) dtpSalesFrom.ApplyTheme();
            if (dtpSalesTo != null) dtpSalesTo.ApplyTheme();
            if (dtpProcFrom != null) dtpProcFrom.ApplyTheme();
            if (dtpProcTo != null) dtpProcTo.ApplyTheme();


            StyleGridTheme(dgvSales); StyleGridTheme(dgvInventory); StyleGridTheme(dgvProcurement); StyleGridTheme(dgvStocks);
            this.Invalidate(true);
        }

        private void StyleGridTheme(DataGridView dgv)
        {
            if (dgv == null) return;

            dgv.BackgroundColor = UITheme.CurrentPanel;
            dgv.GridColor = UITheme.CurrentBorder;

            dgv.DefaultCellStyle.BackColor = UITheme.CurrentPanel;
            dgv.DefaultCellStyle.ForeColor = UITheme.CurrentText;
            dgv.DefaultCellStyle.SelectionBackColor = UITheme.CurrentPanel;
            dgv.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor;
        }
    }
}