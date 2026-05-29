using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Reflection;
using Button = System.Windows.Forms.Button;
using ComboBox = System.Windows.Forms.ComboBox;
using Control = System.Windows.Forms.Control;
using Label = System.Windows.Forms.Label;
using Panel = System.Windows.Forms.Panel;
using TextBox = System.Windows.Forms.TextBox;

namespace SJ_PC_Store_SIMS.Views
{
    public class SalesView : System.Windows.Forms.UserControl
    {
        // =========================================================================
        // UI ENGINES & COMPONENTS
        // =========================================================================
        private class ModalForm : Form { protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } } }
        private class SmoothPanel : Panel { public SmoothPanel() { this.DoubleBuffered = true; this.ResizeRedraw = true; } }
        private class SmoothGrid : DataGridView { public SmoothGrid() { this.DoubleBuffered = true; } }
        private class BufferedFlowLayoutPanel : FlowLayoutPanel { public BufferedFlowLayoutPanel() { this.DoubleBuffered = true; this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true); } protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } } }

        private class RoundedPanel : Panel
        {
            public int BorderRadius { get; set; } = 6; public int BorderSize { get; set; } = 1; public Color BorderColor { get; set; } = Color.Transparent;
            public RoundedPanel() { this.SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true); this.BackColor = Color.Transparent; this.ResizeRedraw = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); if (this.Width <= 1 || this.Height <= 1) return; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = BorderRadius;
                    if (r <= 0) { path.AddRectangle(rect); } else { path.AddArc(rect.X, rect.Y, r, r, 180, 90); path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90); path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90); path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90); path.CloseFigure(); }
                    using (SolidBrush brush = new SolidBrush(this.BackColor)) { e.Graphics.FillPath(brush, path); }
                    if (BorderSize > 0) { using (Pen pen = new Pen(BorderColor, BorderSize)) { e.Graphics.DrawPath(pen, path); } }
                }
            }
        }

        private class BadgeLabel : Label
        {
            public Color BgTint { get; set; }
            public BadgeLabel() { this.AutoSize = false; this.Size = new Size(150, 28); this.TextAlign = ContentAlignment.MiddleCenter; this.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold); this.BackColor = Color.Transparent; }
            protected override void OnPaint(PaintEventArgs e)
            {
                if (this.Width <= 1 || this.Height <= 1) return; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath()) { int d = this.Height - 1; path.AddArc(0, 0, d, d, 90, 180); path.AddArc(this.Width - d - 1, 0, d, d, 270, 180); path.CloseFigure(); using (SolidBrush b = new SolidBrush(BgTint)) { e.Graphics.FillPath(b, path); } }
                Rectangle rect = new Rectangle(0, 0, this.Width, this.Height); TextRenderer.DrawText(e.Graphics, this.Text, this.Font, rect, this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
                    // FIX: Center text vertically
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

        private class StatusStepper : Panel
        {
            public int CurrentStep { get; set; } = 2;
            public bool IsCancelled { get; set; } = false;
            public StatusStepper() { this.DoubleBuffered = true; this.ResizeRedraw = true; }
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e); if (this.Width < 200 || this.Height <= 0) return; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                string[] labels = { "Quotation Created", "Ordered", "Paid", "Completed" };
                int y = this.Height / 2 - 15; int padding = 80; int usableW = this.Width - (padding * 2); if (usableW <= 0) return; int stepGap = usableW / 3;
                using (Pen linePen = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(linePen, padding, y, padding + usableW, y); }
                StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                for (int i = 0; i < 4; i++)
                {
                    int cx = padding + (i * stepGap); bool isPast = (i + 1) < CurrentStep; bool isActive = (i + 1) == CurrentStep;
                    Color circleColor = UITheme.CurrentInputBg; Color borderColor = UITheme.CurrentBorder; Color textColor = UITheme.MutedText; string content = (i + 1).ToString();
                    if (IsCancelled) { circleColor = Color.FromArgb(239, 68, 68); borderColor = circleColor; textColor = Color.White; content = "X"; }
                    else if (isPast || (isActive && i == 3)) { circleColor = Color.FromArgb(16, 185, 129); borderColor = circleColor; textColor = Color.White; content = "✓"; }
                    else if (isActive) { if (i == 1) { circleColor = Color.FromArgb(245, 158, 11); borderColor = circleColor; textColor = Color.White; } else if (i == 2) { circleColor = Color.FromArgb(59, 130, 246); borderColor = circleColor; textColor = Color.White; } }
                    e.Graphics.FillEllipse(new SolidBrush(circleColor), cx - 16, y - 16, 32, 32);
                    if (isActive && !IsCancelled && i != 3) { using (Pen p = new Pen(Color.FromArgb(60, circleColor), 6)) { e.Graphics.DrawEllipse(p, cx - 19, y - 19, 38, 38); } } else { using (Pen p = new Pen(borderColor, 3)) { e.Graphics.DrawEllipse(p, cx - 16, y - 16, 32, 32); } }
                    Rectangle textRect = new Rectangle(cx - 16, y - 15, 32, 32);
                    e.Graphics.DrawString(content, new Font("Segoe UI", 10F, FontStyle.Bold), new SolidBrush(textColor), textRect, sf);
                    Color labelColor = (isPast || isActive) ? (IsCancelled ? Color.FromArgb(239, 68, 68) : UITheme.CurrentText) : UITheme.MutedText;
                    Font lblFont = new Font("Segoe UI", 8.5F, FontStyle.Bold); Size txtSize = TextRenderer.MeasureText(labels[i].ToUpper(), lblFont);
                    TextRenderer.DrawText(e.Graphics, labels[i].ToUpper(), lblFont, new Point(cx - (txtSize.Width / 2), y + 25), labelColor);
                }
            }
        }

        private class ThemedMonthCalendar : MonthCalendar
        {
            private Color _backColor = SystemColors.Window;
            public new Color BackColor { get => _backColor; set { _backColor = value; this.Invalidate(); } }
            protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); WinApi.SetWindowTheme(this.Handle, "", ""); }
        }

        private class ThemedDatePicker : Panel
        {
            public DateTime Value { get => _selectedDate; set { _selectedDate = value; txtDate.Text = value.ToString("MM/dd/yyyy"); } }
            private DateTime _selectedDate = DateTime.Now;
            private TextBox txtDate;
            private Button btnDrop;
            private ThemedMonthCalendar monthCal;
            private Form popup;
            private bool isDarkMode;

            public ThemedDatePicker()
            {
                this.Size = new Size(180, 38); this.Padding = new Padding(0); this.BackColor = UITheme.CurrentInputBg;
                txtDate = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 11F), Text = _selectedDate.ToString("MM/dd/yyyy"), ReadOnly = true, BackColor = this.BackColor, ForeColor = UITheme.CurrentText };
                txtDate.Click += (s, e) => ToggleCalendar();
                btnDrop = new Button { Text = "▼", Dock = DockStyle.Right, Width = 24, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 8F), BackColor = this.BackColor, ForeColor = UITheme.CurrentText, Cursor = Cursors.Hand };
                btnDrop.FlatAppearance.BorderSize = 0; btnDrop.Click += (s, e) => ToggleCalendar();
                btnDrop.MouseEnter += (s, e) => { btnDrop.BackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(200, 200, 200); };
                btnDrop.MouseLeave += (s, e) => { btnDrop.BackColor = this.BackColor; };
                this.Controls.Add(txtDate); this.Controls.Add(btnDrop); txtDate.BringToFront();
            }

            private void ToggleCalendar()
            {
                if (popup == null || popup.IsDisposed)
                {
                    popup = new Form { FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, ShowInTaskbar = false, TopMost = true, BackColor = UITheme.CurrentInputBg, Padding = new Padding(0) };
                    monthCal = new ThemedMonthCalendar { MaxSelectionCount = 1, BoldedDates = new DateTime[] { DateTime.Today }, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, TitleBackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234), TitleForeColor = UITheme.CurrentText, TrailingForeColor = UITheme.MutedText };
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

            public void ApplyTheme(bool darkMode)
            {
                isDarkMode = darkMode; Color bg = UITheme.CurrentInputBg; this.BackColor = bg;
                txtDate.BackColor = bg; txtDate.ForeColor = UITheme.CurrentText;
                btnDrop.BackColor = bg; btnDrop.ForeColor = UITheme.CurrentText;
                if (popup != null && !popup.IsDisposed)
                {
                    popup.BackColor = bg;
                    if (monthCal != null) { monthCal.BackColor = bg; monthCal.ForeColor = UITheme.CurrentText; monthCal.TitleBackColor = darkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234); monthCal.TitleForeColor = UITheme.CurrentText; monthCal.TrailingForeColor = UITheme.MutedText; monthCal.Invalidate(); }
                }
            }
        }

        // =========================================================================
        // VARIABLES
        // =========================================================================
        private SalesController _salesController;
        private InventoryController _inventoryController;
        private AttachmentController _attachController;
        private string _activeUserId;
        private SalesTransactionModel _selectedTransaction;
        private bool _isEditMode = false;

        private SmoothPanel pnlLanding, pnlProfile, pnlCreate, pnlDetailHeader, pnlCreateHeader;
        private DarkComboBox cmbFilter;
        private TextBox txtSearch;
        private SmoothGrid dgvSalesList, dgvProfileItems;
        private StatusStepper profileStepper;

        private List<Control> _dynamicTexts = new List<Control>();
        private List<Control> _mutedTexts = new List<Control>();
        private List<RoundedPanel> _inputWrappers = new List<RoundedPanel>();
        private List<RoundedPanel> _borderedContainers = new List<RoundedPanel>();
        private List<TextBox> _textInputs = new List<TextBox>();
        private List<DarkComboBox> _comboInputs = new List<DarkComboBox>();
        private List<DarkComboBox> _comboFilterInputs = new List<DarkComboBox>();
        private List<IconButton> _buttons = new List<IconButton>();

        // Profile Controls
        private Label lblDetTitle, lblDetCustomer, lblDetDate, lblDetPayment, lblDetWarranty, lblDetTransactionNo;
        private Label lblDetAuditCreated, lblDetAuditModified;
        private Label lblTotalSub, lblTotalDisc, lblTotalTax, lblTotalGrand;
        private BadgeLabel badgeStatus;
        private IconButton btnSubmitPayment, btnCancel, btnPDF, btnEdit, btnCheckoutProfile, btnCompleteOrder, btnReturn;
        private FlowLayoutPanel flpLeftDetails;
        private Panel pnlRightItems;
        private RoundedPanel cardOrderDetails, cardAudit, cardAttach;

        // Create/Edit Controls
        private Label lblCreateTitle, lblCreateSub, lblCreateGrand;
        private ThemedDatePicker dtpOrderDate;
        private TextBox txtCustomerName, txtTransactionNumber;
        private NumericUpDown nudWarrantyDays;
        private DarkComboBox cmbPaymentMethod, cmbCreateDiscountType, cmbCreateTaxType, cmbCategorySelect, cmbItemSelect, cmbSerialSelect;
        private TextBox txtQty, txtPrice, txtCreateDiscount, txtCreateTax;

        private FlowLayoutPanel flpCreateAttachments;
        private List<string> _pendingAttachments = new List<string>();

        private Button btnAttach, btnAttachCreate;
        private FlowLayoutPanel flpAttachments;
        private Control wTransactionNumber;
        private SmoothGrid dgvCartItems;

        private List<SalesTransactionModel> _allTransactions = new List<SalesTransactionModel>();
        private List<ItemMasterModel> _dbItems = new List<ItemMasterModel>();

        public SalesView(string currentUserId)
        {
            _activeUserId = currentUserId;
            _salesController = new SalesController();
            _inventoryController = new InventoryController();
            _attachController = new AttachmentController();
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            this.Padding = new Padding(35, 20, 35, 35); this.Margin = new Padding(0);

            InitializeRoutingPanels();
            InitializeLandingView();
            InitializeProfileView();
            InitializeCreateView();

            LoadDatabaseData();
            LoadData();
            SwitchView("Landing");
            ApplyTheme();
        }

        private void LogAndNotify(string title, string message, bool isSuccess)
        {
            _salesController.LogActivity(_activeUserId, $"{title} - {message}");
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
                using (GraphicsPath path = new GraphicsPath()) { int r = 8; path.AddArc(0, 0, r, r, 180, 90); path.AddArc(toast.Width - r - 1, 0, r, r, 270, 90); path.AddArc(toast.Width - r - 1, toast.Height - r - 1, r, r, 0, 90); path.AddArc(0, toast.Height - r - 1, r, r, 90, 90); path.CloseFigure(); toast.Region = new Region(path); using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawPath(p, path); } }
            };
            IconPictureBox icon = new IconPictureBox { IconChar = success ? IconChar.CheckCircle : IconChar.TimesCircle, IconColor = success ? Color.FromArgb(16, 185, 129) : Color.FromArgb(239, 68, 68), IconSize = 24, Size = new Size(24, 24), Location = new Point(15, 18) };
            lbl.Location = new Point(45, 20); toast.Controls.AddRange(new Control[] { icon, lbl });
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer { Interval = 3000 };
            t.Tick += (s, e) => { toast.Close(); t.Stop(); }; toast.Show(); t.Start();
        }

        protected override void OnParentChanged(EventArgs e) { base.OnParentChanged(e); if (this.Parent != null) { this.Parent.BackColorChanged -= Parent_BackColorChanged; this.Parent.BackColorChanged += Parent_BackColorChanged; } }
        private void Parent_BackColorChanged(object sender, EventArgs e) { ApplyTheme(); }

        private Control CreateInputWrapper(Control innerControl, int width)
        {
            RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(12, 8, 12, 8), Margin = new Padding(0, 0, 10, 0) };
            innerControl.Dock = DockStyle.Fill;
            wrapper.Controls.Add(innerControl);
            _inputWrappers.Add(wrapper);
            return wrapper;
        }

        private Control CreateSearchInput(string placeholder, int width, out TextBox txtOut, Action clearAction)
        {
            RoundedPanel wrapper = new RoundedPanel { Width = width, Height = 38, BorderRadius = 4, BorderSize = 1, Padding = new Padding(12, 8, 8, 8), Margin = new Padding(0, 0, 10, 0) };
            IconPictureBox icon = new IconPictureBox { IconChar = IconChar.Search, IconSize = 18, Size = new Size(24, 18), Dock = DockStyle.Left, BackColor = Color.Transparent };
            TextBox txt = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 10.5F), Text = placeholder };
            IconPictureBox clearIcon = new IconPictureBox { IconChar = IconChar.Times, IconSize = 16, Size = new Size(20, 18), Dock = DockStyle.Right, BackColor = Color.Transparent, Cursor = Cursors.Hand };
            txt.GotFocus += (s, e) => { if (txt.Text == placeholder) txt.Text = ""; };
            txt.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(txt.Text)) txt.Text = placeholder; };
            clearIcon.Click += (s, e) => { txt.Text = placeholder; clearAction(); };
            clearIcon.MouseEnter += (s, e) => clearIcon.IconColor = Color.FromArgb(239, 68, 68);
            clearIcon.MouseLeave += (s, e) => clearIcon.IconColor = UITheme.CurrentIcon;
            wrapper.Controls.Add(clearIcon); wrapper.Controls.Add(txt); wrapper.Controls.Add(icon);
            _inputWrappers.Add(wrapper); _textInputs.Add(txt); txtOut = txt; return wrapper;
        }

        private void InitializeRoutingPanels()
        {
            pnlLanding = new SmoothPanel { Dock = DockStyle.Fill };
            pnlProfile = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            pnlCreate = new SmoothPanel { Dock = DockStyle.Fill, Visible = false };
            this.Controls.Add(pnlProfile); this.Controls.Add(pnlCreate); this.Controls.Add(pnlLanding);
        }

        private void SwitchView(string view)
        {
            pnlLanding.Visible = (view == "Landing");
            pnlProfile.Visible = (view == "Profile");
            pnlCreate.Visible = (view == "Create");
            if (view == "Profile" && _selectedTransaction != null) RenderProfile();
            if (view == "Landing") FilterGrid();
            ApplyTheme();
        }

        // =========================================================================
        // LANDING VIEW
        // =========================================================================
        private void InitializeLandingView()
        {
            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25) };

            // 1. The Toolbar Card (Top)
            RoundedPanel pnlToolbar = new RoundedPanel
            {
                Dock = DockStyle.Top,
                Height = 75,
                BorderRadius = 6,
                BorderSize = 1,
            };

            _borderedContainers.Add(pnlToolbar);

            // 2. The 25-pixel Gap
            Panel pnlSpacer = new Panel { Dock = DockStyle.Top, Height = 25 };

            // 3. The Grid Card (Fills the rest of the screen)
            RoundedPanel pnlGridContainer = new RoundedPanel
            {
                Dock = DockStyle.Fill,
                BorderRadius = 6,
                BorderSize = 1,
                Padding = new Padding(1)
            };
            _borderedContainers.Add(pnlGridContainer); // Hook into Theme Engine

            SmoothPanel toolbar = new SmoothPanel
            {
                Dock = DockStyle.Top,
                Height = 80,
                BackColor = Color.Transparent,
                Padding = new Padding(25, 20, 25, 20)
            };
            toolbar.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 79, toolbar.Width, 79); } };

            cmbFilter = new DarkComboBox { Location = new Point(25, 20), Size = new Size(200, 38), Font = new Font("Segoe UI", 14F, FontStyle.Bold), Cursor = Cursors.Hand };
            cmbFilter.Items.AddRange(new[] { "All Orders", "Quotation", "Ordered", "Paid", "Returned", "Cancelled" });
            cmbFilter.SelectedIndex = 0; cmbFilter.SelectedIndexChanged += (s, e) => FilterGrid();
            _comboFilterInputs.Add(cmbFilter);

            Control searchWrapper = CreateSearchInput("Search Order Number...", 300, out txtSearch, () => { txtSearch.Text = "Search Order Number..."; FilterGrid(); });
            searchWrapper.Location = new Point(240, 20);
            txtSearch.TextChanged += (s, e) => { if (txtSearch.Text != "Search Order Number...") FilterGrid(); };

            IconButton btnCreate = new IconButton { Text = " Create New Order", IconChar = IconChar.Plus, IconSize = 16, Size = new Size(200, 38), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "ActionAdd", Font = new Font("Segoe UI", 10F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 0, 15, 0) };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += (s, e) => { _isEditMode = false; PrepareCreateForm(); SwitchView("Create"); };
            _buttons.Add(btnCreate);

            toolbar.Controls.AddRange(new Control[] { cmbFilter, searchWrapper, btnCreate });

            dgvSalesList = new SmoothGrid { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, EnableHeadersVisualStyles = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeight = 60, RowTemplate = { Height = 60 }, Cursor = Cursors.Hand };
            dgvSalesList.Columns.Add("Rcpt", "ORDER NUMBER"); dgvSalesList.Columns.Add("Cust", "CUSTOMER NAME");
            dgvSalesList.Columns.Add("Date", "ORDER DATE"); dgvSalesList.Columns.Add("Total", "TOTAL AMOUNT"); dgvSalesList.Columns.Add("Status", "STATUS");
            dgvSalesList.Columns["Rcpt"].DefaultCellStyle.Font = new Font("Consolas", 11F, FontStyle.Bold);
            dgvSalesList.Columns["Rcpt"].DefaultCellStyle.ForeColor = Color.FromArgb(59, 130, 246);
            dgvSalesList.Columns["Total"].DefaultCellStyle.Format = "C2";
            dgvSalesList.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvSalesList.Columns.Add("Action", ""); dgvSalesList.Columns["Action"].Width = 50;
            dgvSalesList.Columns["Action"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvSalesList.Columns["Action"].DefaultCellStyle.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
            dgvSalesList.Columns["Action"].DefaultCellStyle.ForeColor = UITheme.MutedText;
            dgvSalesList.Columns["Action"].DefaultCellStyle.Padding = new Padding(0, 0, 20, 5);

            // ADD THIS LOOP: Disables clicking/sorting on all column headers
            foreach (DataGridViewColumn col in dgvSalesList.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            dgvSalesList.DefaultCellStyle.Padding = new Padding(25, 0, 0, 0);
            dgvSalesList.ColumnHeadersDefaultCellStyle.Padding = new Padding(25, 0, 0, 0);
            dgvSalesList.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            dgvSalesList.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);



            dgvSalesList.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 59, dgvSalesList.Width, 59); }
                if (dgvSalesList.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "No Sales Orders found in the database.", new Font("Segoe UI", 12F, FontStyle.Italic), dgvSalesList.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            dgvSalesList.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dgvSalesList.Columns["Status"].Index && e.Value != null)
                {
                    string stat = e.Value.ToString(); Color fgColor;
                    if (stat == "Quotation") fgColor = Color.FromArgb(160, 170, 178);
                    else if (stat == "Ordered") fgColor = Color.FromArgb(245, 158, 11);
                    else if (stat == "Paid") fgColor = Color.FromArgb(59, 130, 246);
                    else if (stat == "Completed") fgColor = Color.FromArgb(16, 185, 129);
                    else fgColor = Color.FromArgb(239, 68, 68);
                    e.CellStyle.ForeColor = fgColor; e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    e.Value = stat.ToUpper(); e.FormattingApplied = true;
                }
            };

            dgvSalesList.CellMouseClick += (s, e) => { if (e.RowIndex >= 0) { string id = dgvSalesList.Rows[e.RowIndex].Cells[0].Value.ToString(); _selectedTransaction = _allTransactions.FirstOrDefault(t => t.ReceiptID == id); SwitchView("Profile"); } };

            pnlGridContainer.Controls.Add(dgvSalesList);

            pnlToolbar.Controls.Add(toolbar);

            // Assembly order is critical for docking!
            pnlBody.Controls.Add(pnlGridContainer);
            pnlBody.Controls.Add(pnlSpacer);
            pnlBody.Controls.Add(pnlToolbar);

            pnlLanding.Controls.Add(pnlBody);
        }

        // =========================================================================
        // PROFILE VIEW
        // =========================================================================
        private void InitializeProfileView()
        {
            RoundedPanel container = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, BorderSize = 1, Padding = new Padding(1) };
            _borderedContainers.Add(container);

            pnlDetailHeader = new SmoothPanel { Dock = DockStyle.Top, Height = 90, BackColor = Color.Transparent, Padding = new Padding(25) };
            pnlDetailHeader.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 89, pnlDetailHeader.Width, 89); } };

            FlowLayoutPanel flpLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            IconButton btnBack = new IconButton { Text = " Back", IconChar = IconChar.ArrowLeft, IconSize = 16, Size = new Size(90, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(0, 0, 15, 0) };
            btnBack.FlatAppearance.BorderSize = 0; btnBack.ForeColor = UITheme.MutedText; btnBack.IconColor = UITheme.MutedText;
            btnBack.MouseEnter += (s, e) => { btnBack.ForeColor = UITheme.CurrentText; btnBack.IconColor = UITheme.CurrentText; };
            btnBack.MouseLeave += (s, e) => { btnBack.ForeColor = UITheme.MutedText; btnBack.IconColor = UITheme.MutedText; };
            btnBack.Click += (s, e) => SwitchView("Landing");

            lblDetTitle = new Label { Font = new Font("Consolas", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 5, 15, 0) };
            badgeStatus = new BadgeLabel { Margin = new Padding(0, 5, 0, 0) };
            flpLeft.Controls.AddRange(new Control[] { btnBack, lblDetTitle, badgeStatus });

            FlowLayoutPanel flpRight = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            btnEdit = new IconButton { Text = " Edit", IconChar = IconChar.Pen, IconSize = 16, Size = new Size(80, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Secondary", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(10, 0, 0, 0) };
            btnEdit.Click += (s, e) => { _isEditMode = true; PrepareCreateForm(); SwitchView("Create"); };
            btnPDF = new IconButton { Text = " Generate PDF", IconChar = IconChar.FilePdf, IconSize = 16, Size = new Size(140, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Secondary", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(0) };
            btnPDF.Click += (s, e) => GeneratePDF();
            btnSubmitPayment = new IconButton
            {
                Text = " Submit Payment",
                IconChar = IconChar.MoneyBill,
                IconSize = 16,
                Size = new Size(150, 38),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "Success",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleRight,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(10, 0, 0, 0)
            };
            btnSubmitPayment.Click += (s, e) => OpenModal("SubmitPayment");

            // NEW: Check Out Order button (Converts Quotation to Ordered)
            btnCheckoutProfile = new IconButton
            {
                Text = " Check Out Order",
                IconChar = IconChar.CheckCircle,
                IconSize = 16,
                Size = new Size(150, 39),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "ActionAdd",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleRight,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(10, 0, 0, 0)
            };
            btnCheckoutProfile.Click += (s, e) =>
            {
                if (_salesController.UpdateTransactionStatus(_selectedTransaction.ReceiptID, "Ordered", _activeUserId))
                {
                    _selectedTransaction.Status = "Ordered";
                    LogAndNotify("Order Checked Out", _selectedTransaction.ReceiptID, true);
                    LoadData();
                    SwitchView("Profile");
                }
            };

            // NEW: Complete Order button (Converts Paid to Completed)
            btnCompleteOrder = new IconButton
            {
                Text = " Complete Order",
                IconChar = IconChar.CheckDouble,
                IconSize = 16,
                Size = new Size(160, 39),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "Success", // Green Button Theme
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleRight,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0),
                Margin = new Padding(10, 0, 0, 0)
            };
            btnCompleteOrder.Click += (s, e) => OpenModal("CompleteOrder");

            btnCancel = new IconButton { Text = " Cancel", IconChar = IconChar.TimesCircle, IconSize = 16, Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Danger", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(10, 0, 0, 0) };
            btnCancel.Click += (s, e) => OpenModal("CancelOrder");

            // CHANGED: Included btnCompleteOrder in the UI arrays
            flpRight.Controls.AddRange(new Control[] { btnCompleteOrder, btnCheckoutProfile, btnSubmitPayment, btnCancel, btnEdit, btnPDF });
            _buttons.AddRange(new[] { btnCompleteOrder, btnCheckoutProfile, btnSubmitPayment, btnCancel, btnEdit, btnPDF });

            _dynamicTexts.Add(lblDetTitle);
            pnlDetailHeader.Controls.Add(flpRight); pnlDetailHeader.Controls.Add(flpLeft);

            profileStepper = new StatusStepper { Dock = DockStyle.Top, Height = 100 };

            Panel pnlBody = new Panel { Dock = DockStyle.Fill };
            flpLeftDetails = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 380, AutoScroll = true, Padding = new Padding(25, 20, 10, 20), FlowDirection = FlowDirection.TopDown, WrapContents = false };

            // ADD THIS LINE: Draws the vertical line separator on the right edge
            flpLeftDetails.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 379, 0, 379, flpLeftDetails.Height); } };

            pnlRightItems = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(30) };

            Func<string, RoundedPanel> CreateInfoSection = (title) =>
            {
                Label lHead = new Label { Text = title.ToUpper(), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = UITheme.MutedText, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
                flpLeftDetails.Controls.Add(lHead); _mutedTexts.Add(lHead);
                RoundedPanel p = new RoundedPanel { Width = 320, Height = 100, BorderRadius = 6, BorderSize = 1, Padding = new Padding(20), Margin = new Padding(0, 0, 0, 25) };
                _borderedContainers.Add(p); flpLeftDetails.Controls.Add(p); return p;
            };

            cardOrderDetails = CreateInfoSection("Order Details");
            lblDetCustomer = new Label { Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 20) };
            lblDetDate = new Label { Font = new Font("Segoe UI", 9.5F), AutoSize = true, Location = new Point(20, 50) };
            lblDetPayment = new Label { Font = new Font("Segoe UI", 9.5F), AutoSize = true, Location = new Point(20, 75) };
            lblDetWarranty = new Label { Font = new Font("Segoe UI", 9.5F), AutoSize = true, Location = new Point(20, 100) };

            // ADDED: Transaction Number label for Online Payments
            lblDetTransactionNo = new Label { Font = new Font("Segoe UI", 9.5F), AutoSize = true, Location = new Point(20, 125), Visible = false };

            cardOrderDetails.Controls.AddRange(new Control[] { lblDetCustomer, lblDetDate, lblDetPayment, lblDetWarranty, lblDetTransactionNo });
            cardOrderDetails.SizeChanged += (s, e) => AdjustCardHeight(cardOrderDetails);

            cardAudit = CreateInfoSection("Audit Trail");
            lblDetAuditCreated = new Label { Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(20, 20) };
            lblDetAuditModified = new Label { Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(20, 60) };
            cardAudit.Controls.AddRange(new Control[] { lblDetAuditCreated, lblDetAuditModified });
            cardAudit.SizeChanged += (s, e) => AdjustCardHeight(cardAudit);

            cardAttach = CreateInfoSection("Attachments");
            btnAttach = new Button { Text = "📝 Attach File", Dock = DockStyle.Top, Size = new Size(110, 35), Location = new Point(20, 20), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            btnAttach.FlatAppearance.BorderSize = 0;
            btnAttach.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog()) { ofd.Multiselect = true; ofd.Filter = "All Files|*.*|Documents|*.pdf;*.docx;*.xlsx|Images|*.jpg;*.png;*.bmp"; if (ofd.ShowDialog() == DialogResult.OK) { foreach (string file in ofd.FileNames) { _attachController.UploadAttachment(null, file, _activeUserId, _selectedTransaction.ReceiptID); } LogAndNotify("Attachment", $"{ofd.FileNames.Length} file(s) attached.", true); LoadAttachments(); } }
            };
            cardAttach.Controls.Add(btnAttach);
            flpAttachments = new FlowLayoutPanel { Location = new Point(20, 65), Size = new Size(280, 150), AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            cardAttach.Controls.Add(flpAttachments);
            cardAttach.SizeChanged += (s, e) => AdjustCardHeight(cardAttach);

            _mutedTexts.AddRange(new[] { lblDetDate, lblDetPayment, lblDetWarranty, lblDetAuditCreated, lblDetAuditModified, lblDetTransactionNo });

            Panel pnlRightHeader = new Panel { Dock = DockStyle.Top, Height = 60 };
            Label lItemsTitle = new Label { Text = "Ordered Items", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 10) };
            _dynamicTexts.Add(lItemsTitle);
            btnReturn = new IconButton
            {
                Text = " Return Item",
                IconChar = IconChar.Undo,
                IconSize = 16,
                Size = new Size(140, 38),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlRightHeader.Width - 140, 10),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "ActionAdd",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),

                // ADDED: Fixes the icon overlap by defining strict image/text relations and padding
                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleRight,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };
            btnReturn.Click += (s, e) => OpenModal("ReturnItem"); _buttons.Add(btnReturn);
            pnlRightHeader.Controls.AddRange(new Control[] { btnReturn, lItemsTitle });

            dgvProfileItems = new SmoothGrid { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, EnableHeadersVisualStyles = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeight = 50, RowTemplate = { Height = 45 }, Margin = new Padding(0, 20, 0, 0) };
            dgvProfileItems.Columns.Add("Code", "ITEM CODE"); dgvProfileItems.Columns.Add("Desc", "ITEM NAME"); dgvProfileItems.Columns.Add("Cond", "CONDITION");
            dgvProfileItems.Columns.Add("Serial", "SERIAL NUMBER"); dgvProfileItems.Columns.Add("Qty", "QTY"); dgvProfileItems.Columns.Add("Price", "UNIT PRICE"); dgvProfileItems.Columns.Add("Total", "TOTAL");
            dgvProfileItems.Columns["Code"].DefaultCellStyle.Font = new Font("Consolas", 10F, FontStyle.Bold);
            dgvProfileItems.Columns["Price"].DefaultCellStyle.Format = "C2"; dgvProfileItems.Columns["Total"].DefaultCellStyle.Format = "C2";
            dgvProfileItems.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvProfileItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvProfileItems.ColumnHeadersDefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
            dgvProfileItems.DefaultCellStyle.Padding = new Padding(15, 0, 0, 0);
            dgvProfileItems.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 49, dgvProfileItems.Width, 49); } };

            Panel pnlTotalsWrapper = new Panel { Dock = DockStyle.Bottom, Height = 170, Padding = new Padding(0, 20, 0, 0) };
            RoundedPanel pnlTotals = new RoundedPanel { Size = new Size(350, 160), Dock = DockStyle.Right, BorderRadius = 6, BorderSize = 1 };
            lblTotalSub = new Label { Font = new Font("Segoe UI", 10F), AutoSize = true, Location = new Point(20, 20) };
            lblTotalDisc = new Label { Font = new Font("Segoe UI", 10F), AutoSize = true, Location = new Point(20, 50), ForeColor = Color.FromArgb(16, 185, 129) };
            lblTotalTax = new Label { Font = new Font("Segoe UI", 10F), AutoSize = true, Location = new Point(20, 80) };
            lblTotalGrand = new Label { Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 115), ForeColor = Color.FromArgb(16, 185, 129) };
            _dynamicTexts.AddRange(new[] { lblTotalSub, lblTotalGrand, lblTotalTax }); _borderedContainers.Add(pnlTotals);
            pnlTotals.Controls.AddRange(new Control[] { lblTotalSub, lblTotalDisc, lblTotalTax, lblTotalGrand });
            pnlTotalsWrapper.Controls.Add(pnlTotals);

            pnlRightItems.Controls.Add(dgvProfileItems); pnlRightItems.Controls.Add(pnlTotalsWrapper); pnlRightItems.Controls.Add(pnlRightHeader);
            pnlBody.Controls.Add(pnlRightItems); pnlBody.Controls.Add(flpLeftDetails);
            container.Controls.Add(pnlBody); container.Controls.Add(profileStepper); container.Controls.Add(pnlDetailHeader);
            pnlProfile.Controls.Add(container);
        }

        private void RenderProfile()
        {
            lblDetTitle.Text = _selectedTransaction.ReceiptID;
            badgeStatus.Text = _selectedTransaction.Status;

            profileStepper.IsCancelled = _selectedTransaction.Status == "Returned" || _selectedTransaction.Status == "Cancelled";
            if (_selectedTransaction.Status == "Quotation") profileStepper.CurrentStep = 1;
            else if (_selectedTransaction.Status == "Ordered") profileStepper.CurrentStep = 2;
            else if (_selectedTransaction.Status == "Paid") profileStepper.CurrentStep = 3;
            else if (_selectedTransaction.Status == "Completed") profileStepper.CurrentStep = 4;
            else profileStepper.CurrentStep = 0;
            profileStepper.Invalidate();

            if (_selectedTransaction.Status == "Quotation") { badgeStatus.BgTint = Color.FromArgb(40, 160, 170, 178); badgeStatus.ForeColor = Color.FromArgb(160, 170, 178); }
            else if (_selectedTransaction.Status == "Ordered") { badgeStatus.BgTint = Color.FromArgb(40, 245, 158, 11); badgeStatus.ForeColor = Color.FromArgb(245, 158, 11); }
            else if (_selectedTransaction.Status == "Paid") { badgeStatus.BgTint = Color.FromArgb(40, 59, 130, 246); badgeStatus.ForeColor = Color.FromArgb(59, 130, 246); }
            else if (_selectedTransaction.Status == "Completed") { badgeStatus.BgTint = Color.FromArgb(40, 16, 185, 129); badgeStatus.ForeColor = Color.FromArgb(16, 185, 129); }
            else { badgeStatus.BgTint = Color.FromArgb(40, 239, 68, 68); badgeStatus.ForeColor = Color.FromArgb(239, 68, 68); }
            badgeStatus.Invalidate();

            // CHANGED: Added logic for Complete Order and Return Item buttons
            btnSubmitPayment.Visible = _selectedTransaction.Status == "Ordered";
            btnCheckoutProfile.Visible = _selectedTransaction.Status == "Quotation";
            btnCompleteOrder.Visible = _selectedTransaction.Status == "Paid";
            btnCancel.Visible = _selectedTransaction.Status == "Quotation" || _selectedTransaction.Status == "Ordered";
            btnEdit.Visible = _selectedTransaction.Status == "Quotation" || _selectedTransaction.Status == "Ordered";

            if (btnReturn != null)
            {
                // Return button is completely hidden until the order is finalized
                btnReturn.Visible = _selectedTransaction.Status == "Completed" || _selectedTransaction.Status == "Returned";
            }

            lblDetCustomer.Text = $"{_selectedTransaction.CustomerName}";
            lblDetDate.Text = $"Order Date: {_selectedTransaction.OrderDate:MMM dd, yyyy}";
            lblDetPayment.Text = $"Payment: {_selectedTransaction.PaymentMethod}";
            lblDetWarranty.Text = $"Warranty: {_selectedTransaction.WarrantyDays} days";
            if (_selectedTransaction.Status == "Quotation" && (DateTime.Now - _selectedTransaction.OrderDate).TotalDays > 30)
                lblDetWarranty.Text += "\n⚠️ This quotation has expired (30+ days).";

            // ADDED: Display Transaction Number safely under Warranty if Online Payment
            if (_selectedTransaction.PaymentMethod == "Online Payment" && !string.IsNullOrEmpty(_selectedTransaction.TransactionNumber))
            {
                lblDetTransactionNo.Text = $"Ref No: {_selectedTransaction.TransactionNumber}";
                lblDetTransactionNo.Visible = true;
            }
            else
            {
                lblDetTransactionNo.Visible = false;
            }

            lblDetAuditCreated.Text = $"Created By:\n{_selectedTransaction.CreatedBy} ({_selectedTransaction.CreatedOn:g})";
            // CHANGED: Now displays the timestamp next to the modifier's name if a modification exists
            lblDetAuditModified.Text = $"Modified By:\n{_selectedTransaction.ModifiedBy ?? "N/A"}" +
                                       (_selectedTransaction.ModifiedOn.HasValue ? $" ({_selectedTransaction.ModifiedOn.Value:g})" : "");

            lblTotalSub.Text = $"Sub Total: {_selectedTransaction.SubTotal:C2}";
            lblTotalDisc.Text = $"Discount Applied: -{_selectedTransaction.Discount:C2}";
            lblTotalTax.Text = $"Tax Applied: {_selectedTransaction.Tax:C2}";
            lblTotalGrand.Text = $"GRAND TOTAL: {_selectedTransaction.GrandTotal:C2}";

            dgvProfileItems.Rows.Clear();
            foreach (var item in _selectedTransaction.Items)
                dgvProfileItems.Rows.Add(item.ItemCode, item.Description, item.ItemCondition, item.SerialNumber, item.Quantity, item.UnitPrice, item.TotalAmount);

            AdjustCardHeight(cardOrderDetails); AdjustCardHeight(cardAudit); AdjustCardHeight(cardAttach);
            LoadAttachments();
        }

        private void AdjustCardHeight(RoundedPanel card)
        {
            if (card == null || card.Controls.Count == 0) return;
            card.SuspendLayout(); int maxBottom = 0;
            foreach (Control ctrl in card.Controls) { int controlBottom = ctrl.Location.Y + ctrl.Height; if (controlBottom > maxBottom) maxBottom = controlBottom; }
            card.Height = maxBottom + card.Padding.Bottom + 10; card.Width = 320; card.ResumeLayout(false);
        }

        // =========================================================================
        // CREATE / EDIT VIEW (FULLY REWORKED)
        // =========================================================================
        private void InitializeCreateView()
        {
            // CHANGED: Use a standard transparent Panel so the workspace gray shows through
            Panel container = new Panel { Dock = DockStyle.Fill };

            // CHANGED: Use the class-level variable and removed Color.Transparent
            pnlCreateHeader = new SmoothPanel { Dock = DockStyle.Top, Height = 80, Padding = new Padding(25, 20, 25, 20) };

            pnlCreateHeader.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 79, pnlCreateHeader.Width, 79); } };

            FlowLayoutPanel flpLeft = new FlowLayoutPanel { Dock = DockStyle.Left, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
            IconButton btnBack = new IconButton { IconChar = IconChar.Times, IconSize = 24, Size = new Size(38, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 15, 0), BackColor = Color.Transparent };
            btnBack.FlatAppearance.BorderSize = 0; btnBack.ForeColor = Color.FromArgb(239, 68, 68); btnBack.IconColor = Color.FromArgb(239, 68, 68);
            btnBack.MouseEnter += (s, e) => { btnBack.BackColor = Color.FromArgb(239, 68, 68); btnBack.IconColor = Color.White; };
            btnBack.MouseLeave += (s, e) => { btnBack.BackColor = Color.Transparent; btnBack.IconColor = Color.FromArgb(239, 68, 68); };
            btnBack.Click += (s, e) => SwitchView("Landing");
            lblCreateTitle = new Label { Text = "Create New Order", Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            flpLeft.Controls.AddRange(new Control[] { btnBack, lblCreateTitle });

            FlowLayoutPanel flpRight = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
            IconButton btnSaveQuotation = new IconButton { Text = " Save as Quotation", IconChar = IconChar.FileAlt, IconSize = 16, Size = new Size(180, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Secondary", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 0, 15, 0), Margin = new Padding(0) };
            btnSaveQuotation.Click += (s, e) => SaveOrder("Quotation");
            IconButton btnCheckout = new IconButton { Name = "btnSubmitSale", Text = " Check Out Order", IconChar = IconChar.CheckCircle, IconSize = 16, Size = new Size(180, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "ActionAdd", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleCenter, ImageAlign = ContentAlignment.MiddleCenter, Padding = new Padding(15, 0, 15, 0), Margin = new Padding(10, 0, 0, 0) };
            btnCheckout.Click += (s, e) => SaveOrder("Ordered");
            flpRight.Controls.AddRange(new Control[] { btnCheckout, btnSaveQuotation });
            _buttons.AddRange(new[] { btnSaveQuotation, btnCheckout }); _dynamicTexts.Add(lblCreateTitle);
            pnlCreateHeader.Controls.Add(flpRight); pnlCreateHeader.Controls.Add(flpLeft);

            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 30, 0, 30), AutoScroll = true };

            // ---- ORDER DETAILS SECTION (BALANCED LAYOUT) ----

            RoundedPanel pnlDetails = new RoundedPanel { Location = new Point(30, 25), Height = 280, Dock = DockStyle.Top, BorderRadius = 6, BorderSize = 1, Padding = new Padding(25) };

            Label lHead1 = new Label { Text = "Order Details", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(25, 20) };
            _dynamicTexts.Add(lHead1);
            pnlDetails.Controls.Add(lHead1);

            // Row 1: Customer Name (left) | Order Date (right)
            Label lCust = new Label { Text = "Customer Name", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(25, 50) };
            txtCustomerName = new TextBox { Font = new Font("Segoe UI", 11F), BorderStyle = BorderStyle.None };
            Control wCust = CreateInputWrapper(txtCustomerName, 350); wCust.Location = new Point(25, 70); _textInputs.Add(txtCustomerName);

            Label lDate = new Label { Text = "Order Date", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(420, 50) };
            dtpOrderDate = new ThemedDatePicker();
            Control wDate = CreateInputWrapper(dtpOrderDate, 180); wDate.Location = new Point(420, 70);

            // Row 2: Payment Method (left) | Transaction Number (right, below payment)
            Label lPay = new Label { Text = "Payment Method", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(25, 125) };
            cmbPaymentMethod = new DarkComboBox { Font = new Font("Segoe UI", 11F), Size = new Size(180, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPaymentMethod.Items.AddRange(new[] { "Cash Payment", "Online Payment" });
            cmbPaymentMethod.SelectedIndex = 0;
            cmbPaymentMethod.SelectedIndexChanged += (s, e) => { if (wTransactionNumber != null) wTransactionNumber.Visible = cmbPaymentMethod.Text == "Online Payment"; };
            Control wPay = CreateInputWrapper(cmbPaymentMethod, 350); wPay.Location = new Point(25, 145); _comboInputs.Add(cmbPaymentMethod);

            Label lTN = new Label { Text = "Transaction Number", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(25, 195) };
            txtTransactionNumber = new TextBox { Font = new Font("Segoe UI", 11F), BorderStyle = BorderStyle.None };
            wTransactionNumber = CreateInputWrapper(txtTransactionNumber, 350); wTransactionNumber.Location = new Point(25, 215); wTransactionNumber.Visible = false;
            _textInputs.Add(txtTransactionNumber);

            // Row 2 right: Warranty
            Label lWarr = new Label { Text = "Warranty (Days)", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(420, 125) };
            nudWarrantyDays = new NumericUpDown { Font = new Font("Segoe UI", 11F), Minimum = 0, Maximum = 365, Value = 7, BorderStyle = BorderStyle.None };
            Control wWarr = CreateInputWrapper(nudWarrantyDays, 180); wWarr.Location = new Point(420, 145);

            pnlDetails.Controls.AddRange(new Control[] { lCust, wCust, lDate, wDate, lPay, wPay, lTN, wTransactionNumber, lWarr, wWarr });
            _mutedTexts.AddRange(new[] { lCust, lDate, lPay, lTN, lWarr }); _borderedContainers.Add(pnlDetails);
            pnlBody.Controls.Add(pnlDetails);

            // ---- ITEM CART SELECTION SECTION ----

            RoundedPanel pnlItems = new RoundedPanel { Location = new Point(0, 340), Size = new Size(pnlBody.Width - 440, 530), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BorderRadius = 6, BorderSize = 1, Padding = new Padding(25) };

            Panel pnlItemHeader = new Panel { Dock = DockStyle.Top, Height = 50 };
            Label lblItemCartTitle = new Label { Text = "Item Cart", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(0, 10) };
            _dynamicTexts.Add(lblItemCartTitle);
            IconButton btnAddItem = new IconButton { Text = " Add Item", IconChar = IconChar.Plus, IconSize = 14, Size = new Size(110, 35), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(85, 5), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "ActionAdd", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0) };
            btnAddItem.Click += (s, e) => AddCartItemRow(); _buttons.Add(btnAddItem);
            pnlItemHeader.Controls.Add(lblItemCartTitle); pnlItemHeader.Controls.Add(btnAddItem);

            Panel pnlItemSearch = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(0, 15, 0, 0) };
            pnlItemSearch.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlItemSearch.Width, 0); } };

            // 1. Setup Category Select with Wrapper
            cmbCategorySelect = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCategorySelect.SelectedIndexChanged += (s, e) => OnCategorySelected();
            _comboInputs.Add(cmbCategorySelect);
            Control wCat = CreateInputWrapper(cmbCategorySelect, 150);
            wCat.Location = new Point(5, 20);
            wCat.Margin = new Padding(0);

            // 2. Setup Item Select with Wrapper (Width reduced to fit Category)
            cmbItemSelect = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbItemSelect.SelectedIndexChanged += (s, e) => OnItemSelected();
            _comboInputs.Add(cmbItemSelect);
            Control wItem = CreateInputWrapper(cmbItemSelect, 250);
            wItem.Location = new Point(165, 20);
            wItem.Margin = new Padding(0);

            // 3. Setup Serial Select with Wrapper
            cmbSerialSelect = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), DropDownStyle = ComboBoxStyle.DropDown };
            _comboInputs.Add(cmbSerialSelect);
            Control wSerial = CreateInputWrapper(cmbSerialSelect, 200);
            wSerial.Location = new Point(425, 20);
            wSerial.Margin = new Padding(0);

            // 4. Setup Qty with Wrapper
            txtQty = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "1", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            _textInputs.Add(txtQty);
            Control wQty = CreateInputWrapper(txtQty, 90);
            wQty.Location = new Point(635, 20);
            wQty.Margin = new Padding(0);

            // 5. Setup Price with Wrapper
            txtPrice = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "0.00", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None, ReadOnly = true };
            _textInputs.Add(txtPrice);
            Control wPrice = CreateInputWrapper(txtPrice, 100);
            wPrice.Location = new Point(735, 20);
            wPrice.Margin = new Padding(0);

            // Add all wrappers to the panel
            pnlItemSearch.Controls.AddRange(new Control[] { wCat, wItem, wSerial, wQty, wPrice });

            dgvCartItems = new SmoothGrid
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,

                // Adapted from dgvPOList
                ColumnHeadersHeight = 50,
                RowTemplate = { Height = 60 },
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 10, 0, 0)
            };

            dgvCartItems.GridColor = UITheme.CurrentBorder;

            dgvCartItems.Columns.Add("Desc", "ITEM NAME");
            dgvCartItems.Columns.Add("Serial", "SERIAL NUMBER");
            dgvCartItems.Columns.Add("Qty", "QTY");
            dgvCartItems.Columns.Add("Price", "UNIT PRICE");
            dgvCartItems.Columns.Add("Total", "TOTAL");

            // NEW: Action Column for the Trash Icon
            dgvCartItems.Columns.Add("Action", "");
            dgvCartItems.Columns["Action"].Width = 50;
            dgvCartItems.Columns["Action"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvCartItems.Columns["Action"].DefaultCellStyle.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            dgvCartItems.Columns["Action"].DefaultCellStyle.ForeColor = Color.FromArgb(239, 68, 68); // Red color
            dgvCartItems.Columns["Action"].DefaultCellStyle.Padding = new Padding(0, 0, 20, 0);

            // Data styling to match dgvPOList aesthetic
            dgvCartItems.Columns["Serial"].DefaultCellStyle.Font = new Font("Consolas", 11F, FontStyle.Bold);
            dgvCartItems.Columns["Serial"].DefaultCellStyle.ForeColor = Color.FromArgb(59, 130, 246);
            dgvCartItems.Columns["Price"].DefaultCellStyle.Format = "C2";
            dgvCartItems.Columns["Total"].DefaultCellStyle.Format = "C2";
            dgvCartItems.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);

            // Adopting Padding and Borders from dgvPOList
            dgvCartItems.DefaultCellStyle.Padding = new Padding(25, 0, 0, 0);
            dgvCartItems.ColumnHeadersDefaultCellStyle.Padding = new Padding(25, 0, 0, 0);
            dgvCartItems.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            dgvCartItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            // Custom Header Line Paint Event
            dgvCartItems.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 49, dgvCartItems.Width, 49); }
                if (dgvCartItems.Rows.Count == 0)
                {
                    TextRenderer.DrawText(e.Graphics, "No items in the cart.", new Font("Segoe UI", 12F, FontStyle.Italic), dgvCartItems.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            // NEW: Delete Row Event
            dgvCartItems.CellMouseClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == dgvCartItems.Columns["Action"].Index)
                {
                    dgvCartItems.Rows.RemoveAt(e.RowIndex);
                    CalculateTotals();
                }
            };

            // ADD THIS LINE to prevent the row from being selected/clicked
            dgvCartItems.SelectionChanged += (s, e) => dgvCartItems.ClearSelection();

            pnlItems.Controls.Add(dgvCartItems);

            pnlItems.Controls.Add(pnlItemSearch);
            pnlItems.Controls.Add(pnlItemHeader);

            // ---- TOTALS + ATTACHMENTS RIGHT PANEL ----
            Panel pnlRightCol = new Panel
            {
                Location = new Point(pnlBody.Width - 380, 340),
                Size = new Size(350, 530),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            RoundedPanel pnlTotals = new RoundedPanel
            {
                Location = new Point(0, 0),
                Size = new Size(350, 260),
                BorderRadius = 6,
                BorderSize = 1,
                Padding = new Padding(20)
            };

            Label lblTotalsHeader = new Label { Text = "TOTALS", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(20, 15) };
            _dynamicTexts.Add(lblTotalsHeader);

            lblCreateSub = new Label { Text = "Subtotal: ₱ 0.00", Font = new Font("Segoe UI", 10F), AutoSize = true, Location = new Point(20, 50) }; _dynamicTexts.Add(lblCreateSub);

            Label lDisc = new Label { Text = "Discount", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(20, 90) };
            cmbCreateDiscountType = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(60, 35), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCreateDiscountType.Items.AddRange(new[] { "₱", "%" }); cmbCreateDiscountType.SelectedIndex = 0; cmbCreateDiscountType.SelectedIndexChanged += (s, e) => CalculateTotals();
            _comboInputs.Add(cmbCreateDiscountType);
            txtCreateDiscount = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "0.00", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            txtCreateDiscount.TextChanged += (s, e) => CalculateTotals(); _textInputs.Add(txtCreateDiscount);
            Control wDiscType = CreateInputWrapper(cmbCreateDiscountType, 70); wDiscType.Location = new Point(100, 85); wDiscType.Margin = new Padding(0);
            Control wDisc = CreateInputWrapper(txtCreateDiscount, 135); wDisc.Location = new Point(190, 85); wDisc.Margin = new Padding(0);

            Label lTax = new Label { Text = "Tax", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(20, 135) };
            cmbCreateTaxType = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(60, 35), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCreateTaxType.Items.AddRange(new[] { "₱", "%" }); cmbCreateTaxType.SelectedIndex = 1; cmbCreateTaxType.SelectedIndexChanged += (s, e) => CalculateTotals();
            _comboInputs.Add(cmbCreateTaxType);
            txtCreateTax = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "0.00", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            txtCreateTax.TextChanged += (s, e) => CalculateTotals(); _textInputs.Add(txtCreateTax);
            Control wTaxType = CreateInputWrapper(cmbCreateTaxType, 70); wTaxType.Location = new Point(100, 130); wTaxType.Margin = new Padding(0);
            Control wTax = CreateInputWrapper(txtCreateTax, 135); wTax.Location = new Point(190, 130); wTax.Margin = new Padding(0);

            lblCreateGrand = new Label { Text = "GRAND TOTAL: ₱ 0.00", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 205), ForeColor = Color.FromArgb(16, 185, 129) };
            _dynamicTexts.Add(lblCreateGrand);

            pnlTotals.Controls.AddRange(new Control[] { lblTotalsHeader, lblCreateSub, lDisc, wDiscType, wDisc, lTax, wTaxType, wTax, lblCreateGrand });

            Panel pnlAttachCreate = new RoundedPanel
            {
                Location = new Point(0, 290),
                Size = new Size(350, 240),
            };

            btnAttachCreate = new Button { Text = "📝 Attach File", Dock = DockStyle.Top, Size = new Size(110, 35), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            btnAttachCreate.FlatAppearance.BorderSize = 0;
            btnAttachCreate.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Multiselect = true;
                    ofd.Filter = "All Files|*.*";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        foreach (string file in ofd.FileNames)
                        {
                            if (_isEditMode && _selectedTransaction != null)
                            {
                                // Instantly upload if editing an existing order
                                _attachController.UploadAttachment(null, file, _activeUserId, _selectedTransaction.ReceiptID);
                            }
                            else
                            {
                                // Queue for upload upon checkout if it's a new order
                                if (!_pendingAttachments.Contains(file)) _pendingAttachments.Add(file);
                            }
                        }
                        ShowToast("Files attached.", true);
                        RenderCreateAttachments();
                    }
                }
            };

            // Note: Removed the "FlowLayoutPanel" type declaration here so it uses the class variable
            flpCreateAttachments = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            pnlAttachCreate.Controls.Add(flpCreateAttachments);
            pnlAttachCreate.Controls.Add(btnAttachCreate);

            pnlRightCol.Controls.Add(pnlTotals); pnlRightCol.Controls.Add(pnlAttachCreate);
            _borderedContainers.Add(pnlItems);

            // ADDED: Hook the right-side panels into the theme engine so they get borders and backgrounds!
            _borderedContainers.Add(pnlTotals);

            pnlBody.Controls.Add(pnlRightCol); pnlBody.Controls.Add(pnlItems);
            container.Controls.Add(pnlBody); container.Controls.Add(pnlCreateHeader);
            pnlCreate.Controls.Add(container);

            pnlBody.Resize += (s, e) =>
            {
                pnlDetails.Width = pnlBody.Width - 60;
                pnlItems.Width = pnlBody.Width - 380;
                pnlRightCol.Location = new Point(pnlBody.Width - 350, 340);
            };
        }

        private void RenderCreateAttachments()
        {
            if (flpCreateAttachments == null) return;
            flpCreateAttachments.Controls.Clear();

            // 1. Render existing database attachments (if in Edit Mode)
            if (_isEditMode && _selectedTransaction != null)
            {
                var existing = _attachController.GetAttachmentsByTransaction(_selectedTransaction.ReceiptID);
                foreach (var att in existing)
                {
                    Panel row = new Panel { Width = flpCreateAttachments.Width - 25, Height = 30 };
                    Label lblFile = new Label { Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(59, 130, 246), AutoSize = false, Width = row.Width - 35, Height = 20, TextAlign = ContentAlignment.MiddleLeft, Location = new Point(5, 6), Text = TruncateText(att.FileName, new Font("Segoe UI", 9.5F), row.Width - 35) };
                    IconButton btnDel = new IconButton { IconChar = IconChar.Trash, IconSize = 16, Size = new Size(25, 25), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.Transparent, IconColor = Color.FromArgb(239, 68, 68), ForeColor = Color.FromArgb(239, 68, 68), Location = new Point(row.Width - 30, 3) };
                    btnDel.FlatAppearance.BorderSize = 0;
                    btnDel.Click += (s, e) => { if (_attachController.DeleteAttachment(att.AttachmentID)) { LogAndNotify("Attachment Deleted", att.FileName, true); RenderCreateAttachments(); } };
                    row.Controls.Add(lblFile); row.Controls.Add(btnDel); flpCreateAttachments.Controls.Add(row);
                }
            }

            // 2. Render locally queued attachments 
            foreach (string path in _pendingAttachments.ToList())
            {
                string fileName = System.IO.Path.GetFileName(path);
                Panel row = new Panel { Width = flpCreateAttachments.Width - 25, Height = 30 };
                // Highlight pending files in green so the user knows they haven't been saved to the DB yet
                Label lblFile = new Label { Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(16, 185, 129), AutoSize = false, Width = row.Width - 35, Height = 20, TextAlign = ContentAlignment.MiddleLeft, Location = new Point(5, 6), Text = TruncateText(fileName, new Font("Segoe UI", 9.5F), row.Width - 35) };
                IconButton btnDel = new IconButton { IconChar = IconChar.Trash, IconSize = 16, Size = new Size(25, 25), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.Transparent, IconColor = Color.FromArgb(239, 68, 68), ForeColor = Color.FromArgb(239, 68, 68), Location = new Point(row.Width - 30, 3) };
                btnDel.FlatAppearance.BorderSize = 0;
                btnDel.Click += (s, e) => { _pendingAttachments.Remove(path); RenderCreateAttachments(); };
                row.Controls.Add(lblFile); row.Controls.Add(btnDel); flpCreateAttachments.Controls.Add(row);
            }
        }

        // NEW: Filters the Item combo box based on the selected category
        private void OnCategorySelected()
        {
            cmbItemSelect.Items.Clear();
            cmbSerialSelect.Items.Clear();
            cmbSerialSelect.Text = ""; // Explicitly clear any lingering typed text
            txtPrice.Text = "0.00";
            if (cmbCategorySelect.SelectedIndex < 0) return;

            string selectedCat = cmbCategorySelect.SelectedItem?.ToString() ?? "";

            // Added IsActive filter to ensure we only load active, sellable blueprints
            var itemsInCat = _dbItems.Where(i => i.Category == selectedCat && i.IsActive).ToList();

            foreach (var item in itemsInCat)
            {
                cmbItemSelect.Items.Add($"{item.Specs} ({item.ItemCondition})");
            }

            if (cmbItemSelect.Items.Count > 0) cmbItemSelect.SelectedIndex = 0;
        }

        private void OnItemSelected()
        {
            if (cmbItemSelect.SelectedIndex < 0 || cmbCategorySelect.SelectedIndex < 0) return;

            // FIX: WinForms .Text property lags behind programmatic SelectedIndex changes.
            // ALWAYS use .SelectedItem.ToString() to get the true current value.
            string selectedCat = cmbCategorySelect.SelectedItem?.ToString() ?? "";
            string selectedItem = cmbItemSelect.SelectedItem?.ToString() ?? "";

            // Find the specific item by matching Category AND Specs, ensuring it is Active
            var match = _dbItems.FirstOrDefault(i => i.Category == selectedCat && $"{i.Specs} ({i.ItemCondition})" == selectedItem && i.IsActive);

            if (match != null)
            {
                txtPrice.Text = match.CurrentValue.ToString("0.00");

                cmbSerialSelect.Items.Clear();
                cmbSerialSelect.Text = ""; // Clear lingering text so it doesn't show old serials

                var available = _salesController.GetAvailableStockForItem(match.ItemCode);
                foreach (var s in available)
                {
                    cmbSerialSelect.Items.Add(s.SerialNumber);
                }

                if (cmbSerialSelect.Items.Count > 0)
                {
                    cmbSerialSelect.SelectedIndex = 0;
                }
                else
                {
                    // Visual feedback if the item exists but physical stock is 0
                    cmbSerialSelect.Text = "No Stock Available";
                }
            }
        }

        private void AddCartItemRow(SalesItemModel existingItem = null)
        {
            // Added validation to prevent adding "No Stock Available" strings to the cart
            if (existingItem == null && (cmbItemSelect.SelectedIndex < 0 || string.IsNullOrWhiteSpace(cmbSerialSelect.Text) || cmbSerialSelect.Text == "No Stock Available"))
            {
                ShowToast("Please select a valid item and serial number.", false);
                return;
            }

            string serial = existingItem?.SerialNumber ?? cmbSerialSelect.Text;

            // Use SelectedItem to perfectly construct the detailed receipt name
            string itemDesc = existingItem?.Description ?? $"{cmbCategorySelect.SelectedItem} {cmbItemSelect.SelectedItem}";

            decimal price = existingItem?.UnitPrice ?? (decimal.TryParse(txtPrice.Text, out decimal p) ? p : 0);
            int qty = existingItem?.Quantity ?? (int.TryParse(txtQty.Text, out int q) ? q : 1);

            foreach (DataGridViewRow row in dgvCartItems.Rows)
                if (row.Cells["Serial"].Value?.ToString() == serial) { ShowToast("This serial number is already in the cart.", false); return; }

            dgvCartItems.Rows.Add(itemDesc, serial, qty, price, price * qty, "🗑");
            CalculateTotals();
        }

        private void PrepareCreateForm()
        {
            dgvCartItems.Rows.Clear();

            // Clear and reset the dropdowns
            cmbCategorySelect.Items.Clear();
            cmbItemSelect.Items.Clear();
            cmbSerialSelect.Items.Clear();
            txtPrice.Text = "0.00";

            // Populate Category combo instead of Item combo
            var categories = _dbItems.Select(i => i.Category).Distinct().ToList();
            foreach (var cat in categories)
            {
                cmbCategorySelect.Items.Add(cat);
            }

            if (_isEditMode && _selectedTransaction != null)
            {
                lblCreateTitle.Text = $"Edit Order ({_selectedTransaction.ReceiptID})";
                txtCustomerName.Text = _selectedTransaction.CustomerName;
                cmbPaymentMethod.SelectedIndex = _selectedTransaction.PaymentMethod == "Online Payment" ? 1 : 0;
                txtTransactionNumber.Text = _selectedTransaction.TransactionNumber ?? "";
                if (wTransactionNumber != null) wTransactionNumber.Visible = cmbPaymentMethod.Text == "Online Payment";
                nudWarrantyDays.Value = _selectedTransaction.WarrantyDays;
                txtCreateDiscount.Text = _selectedTransaction.Discount.ToString("0.00");
                txtCreateTax.Text = _selectedTransaction.Tax.ToString("0.00");
                dtpOrderDate.Value = _selectedTransaction.OrderDate;
                foreach (var item in _selectedTransaction.Items) AddCartItemRow(item);
            }
            else
            {
                lblCreateTitle.Text = "Create New Order";
                txtCustomerName.Text = ""; cmbPaymentMethod.SelectedIndex = 0;
                txtTransactionNumber.Text = ""; if (wTransactionNumber != null) wTransactionNumber.Visible = false;
                nudWarrantyDays.Value = 7; dtpOrderDate.Value = DateTime.Now;
                txtCreateDiscount.Text = "0.00"; txtCreateTax.Text = "0.00";
            }
            CalculateTotals();
            ApplyTheme();

            _pendingAttachments.Clear();
            RenderCreateAttachments();
        }

        private void CalculateTotals()
        {
            decimal sub = 0;
            foreach (DataGridViewRow row in dgvCartItems.Rows)
            {
                if (row.Cells["Total"].Value != null)
                    sub += Convert.ToDecimal(row.Cells["Total"].Value);
            }

            decimal disc = 0; decimal.TryParse(txtCreateDiscount.Text, out disc);
            if (cmbCreateDiscountType.Text == "%") disc = sub * (disc / 100m);
            decimal tax = 0;
            if (txtCreateTax != null) { decimal.TryParse(txtCreateTax.Text, out tax); if (cmbCreateTaxType.Text == "%") tax = (sub - disc) * (tax / 100m); }
            decimal grand = sub - disc + tax;
            lblCreateSub.Text = $"Subtotal: {sub:C2}";
            lblCreateGrand.Text = $"GRAND TOTAL: {grand:C2}";
        }

        private void SaveOrder(string status)
        {
            CalculateTotals();
            if (string.IsNullOrWhiteSpace(txtCustomerName.Text)) { ShowToast("Please enter a customer name.", false); return; }

            string receiptID = _isEditMode ? _selectedTransaction.ReceiptID : _salesController.GenerateNextReceiptID();

            // Collect items from grid
            var items = new List<SalesItemModel>();
            foreach (DataGridViewRow row in dgvCartItems.Rows)
            {
                if (!row.IsNewRow && row.Cells["Desc"].Value != null)
                {
                    items.Add(new SalesItemModel
                    {
                        SerialNumber = row.Cells["Serial"].Value?.ToString(),
                        Description = row.Cells["Desc"].Value?.ToString(),
                        Quantity = Convert.ToInt32(row.Cells["Qty"].Value ?? 1),
                        UnitPrice = Convert.ToDecimal(row.Cells["Price"].Value ?? 0)
                    });
                }
            }

            if (items.Count == 0) { ShowToast("Please add at least one item.", false); return; }

            SalesTransactionModel txn = new SalesTransactionModel
            {
                ReceiptID = receiptID,
                CustomerName = txtCustomerName.Text,
                PaymentMethod = cmbPaymentMethod.Text,
                TransactionNumber = wTransactionNumber != null && wTransactionNumber.Visible ? txtTransactionNumber.Text : null,
                OrderDate = dtpOrderDate.Value,
                SubTotal = decimal.Parse(lblCreateSub.Text.Replace("Subtotal: ₱", "").Trim()),
                GrandTotal = decimal.Parse(lblCreateGrand.Text.Replace("GRAND TOTAL: ₱", "").Trim()),
                Discount = decimal.Parse(txtCreateDiscount.Text),
                Tax = decimal.Parse(txtCreateTax.Text),
                Status = status,
                WarrantyDays = (int)nudWarrantyDays.Value,
                CreatedBy = _activeUserId,
                ModifiedBy = _isEditMode ? _activeUserId : null,
                Items = items
            };

            string result = _isEditMode ? _salesController.UpdateTransaction(txn) : _salesController.SaveTransaction(txn);
            if (result == "SUCCESS")
            {
                // ADD THIS LOOP: Upload all pending files now that we have a valid receipt ID
                foreach (string file in _pendingAttachments)
                {
                    _attachController.UploadAttachment(null, file, _activeUserId, receiptID);
                }
                _pendingAttachments.Clear();

                // ADDED: Empties the cart immediately after the order is successfully saved
                dgvCartItems.Rows.Clear();

                LogAndNotify(_isEditMode ? "Order Updated" : (status == "Quotation" ? "Quotation Saved" : "Order Checked Out"), receiptID, true);
                LoadData(); SwitchView("Landing");
            }
            else MessageBox.Show($"Save Failed:\n{result}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // =========================================================================
        // DATA LOAD & MODALS
        // =========================================================================
        private void LoadDatabaseData() { try { _dbItems = _inventoryController.GetAllBlueprints(); } catch { } }
        private void LoadData() { try { _allTransactions = _salesController.GetAllTransactions(); } catch { } FilterGrid(); }

        private void FilterGrid()
        {
            string filter = cmbFilter.SelectedItem?.ToString() ?? "All Orders";
            string search = txtSearch.Text == "Search Order Number..." ? "" : txtSearch.Text.ToLower();
            dgvSalesList.Rows.Clear();
            foreach (var txn in _allTransactions.OrderByDescending(t => t.OrderDate))
            {
                if (filter != "All Orders" && txn.Status != filter) continue;
                if (!string.IsNullOrEmpty(search) && !txn.ReceiptID.ToLower().Contains(search) && !txn.CustomerName.ToLower().Contains(search)) continue;
                dgvSalesList.Rows.Add(txn.ReceiptID, txn.CustomerName, txn.OrderDate.ToString("MMM dd, yyyy"), txn.GrandTotal, txn.Status, "›");
            }
            dgvSalesList.Invalidate();
        }

        private void OpenModal(string type)
        {
            ModalForm modal = new ModalForm { FormBorderStyle = FormBorderStyle.None, BackColor = UITheme.CurrentPanel, StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = false };
            modal.Paint += (s, e) => { if (modal.Width <= 1 || modal.Height <= 1) return; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; using (GraphicsPath path = new GraphicsPath()) { int r = 12; path.AddArc(0, 0, r, r, 180, 90); path.AddArc(modal.Width - r - 1, 0, r, r, 270, 90); path.AddArc(modal.Width - r - 1, modal.Height - r - 1, r, r, 0, 90); path.AddArc(0, modal.Height - r - 1, r, r, 90, 90); path.CloseFigure(); modal.Region = new Region(path); using (Pen p = new Pen(UITheme.CurrentBorder, 3)) { e.Graphics.DrawPath(p, path); } } };

            // ADDED: The uniform gray footer bar for action buttons
            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = UITheme.CurrentPanel };
            pnlFooter.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); } };

            if (type == "SubmitPayment")
            {
                modal.Size = new Size(450, 360);
                IconPictureBox iconMoney = new IconPictureBox { IconChar = IconChar.MoneyBillWave, IconColor = Color.FromArgb(16, 185, 129), IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 25) };

                Label lblTitle = new Label { Text = "Submit Payment", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                lblTitle.Location = new Point((modal.Width - lblTitle.PreferredWidth) / 2, 95);

                Label lblTotal = new Label { Text = $"Total Due: {_selectedTransaction.GrandTotal:C2}", Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = Color.FromArgb(239, 68, 68), AutoSize = true };
                lblTotal.Location = new Point((modal.Width - lblTotal.PreferredWidth) / 2, 130);

                Label lblAmt = new Label { Text = "Amount Received (₱):", Font = new Font("Segoe UI", 9.5F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(80, 165) };
                TextBox txtAmtRec = new TextBox { Font = new Font("Segoe UI", 14F), Size = new Size(290, 35), Location = new Point(80, 190), BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center };

                Label lblChange = new Label { Text = "Change: ₱ 0.00", Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(16, 185, 129) };
                lblChange.Location = new Point((modal.Width - lblChange.PreferredWidth) / 2, 240);

                // CHANGED: Moved btnAction declaration UP so we can modify its Enabled state instantly
                Button btnAction = new Button { Text = "Confirm Payment", Size = new Size(150, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Gray, ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;
                btnAction.Enabled = false; // Disabled by default to protect checkout

                txtAmtRec.TextChanged += (s, e) =>
                {
                    if (decimal.TryParse(txtAmtRec.Text, out decimal rec))
                    {
                        if (rec >= _selectedTransaction.GrandTotal)
                        {
                            lblChange.Text = $"Change: {rec - _selectedTransaction.GrandTotal:C2}";
                            lblChange.ForeColor = Color.FromArgb(16, 185, 129); // Restores Green text
                            btnAction.Enabled = true;
                            btnAction.BackColor = Color.FromArgb(16, 185, 129); // Highlights the button green
                        }
                        else
                        {
                            lblChange.Text = "⚠️ Insufficient Payment Amount";
                            lblChange.ForeColor = Color.FromArgb(239, 68, 68); // Red warning text
                            btnAction.Enabled = false;
                            btnAction.BackColor = Color.Gray; // Locks the button color
                        }
                    }
                    else
                    {
                        lblChange.Text = "Change: ₱ 0.00";
                        lblChange.ForeColor = Color.FromArgb(16, 185, 129);
                        btnAction.Enabled = false;
                        btnAction.BackColor = Color.Gray;
                    }
                    lblChange.Location = new Point((modal.Width - lblChange.PreferredWidth) / 2, 240);
                };

                Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.Click += (s, e) => modal.Close();

                int totalW = btnCancel.Width + 10 + btnAction.Width;
                btnCancel.Location = new Point((modal.Width - totalW) / 2, 16); btnAction.Location = new Point(((modal.Width - totalW) / 2) + btnCancel.Width + 10, 16);

                btnAction.Click += (s, e) =>
                {
                    if (_salesController.ProcessPayment(_selectedTransaction.ReceiptID, _activeUserId))
                    {
                        _selectedTransaction.Status = "Paid";

                        // CHANGED: Fires a detailed, successful alert text message on completion
                        LogAndNotify("Payment Confirmed", $"Successfully processed payment for order {_selectedTransaction.ReceiptID}.", true);

                        LoadData(); SwitchView("Profile"); modal.Close();
                    }
                    else ShowToast("Payment processing failed.", false);
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.AddRange(new Control[] { iconMoney, lblTitle, lblTotal, lblAmt, txtAmtRec, lblChange });
            }
            else if (type == "CancelOrder")
            {
                modal.Size = new Size(400, 280);
                IconPictureBox iconWarning = new IconPictureBox { IconChar = IconChar.ExclamationTriangle, IconColor = Color.FromArgb(239, 68, 68), IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 30) };

                Label lblWarn = new Label { Text = "Cancel Order?", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 110);
                Label lblDesc = new Label { Text = $"Are you sure you want to cancel\n{_selectedTransaction.ReceiptID}?\nThis action cannot be undone.", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 145);

                Button btnCancel = new Button { Text = "Keep Order", Size = new Size(110, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.Click += (s, e) => modal.Close();

                Button btnAction = new Button { Text = "Cancel Order", Size = new Size(120, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;

                int totalW = btnCancel.Width + 10 + btnAction.Width;
                btnCancel.Location = new Point((modal.Width - totalW) / 2, 16); btnAction.Location = new Point(((modal.Width - totalW) / 2) + btnCancel.Width + 10, 16);

                btnAction.Click += (s, e) =>
                {
                    if (_salesController.UpdateTransactionStatus(_selectedTransaction.ReceiptID, "Cancelled", _activeUserId))
                    {
                        _selectedTransaction.Status = "Cancelled";

                        // ADDED: Instantly update the local model's audit properties so the UI reflects them
                        _selectedTransaction.ModifiedBy = _activeUserId;
                        _selectedTransaction.ModifiedOn = DateTime.Now;

                        // CHANGED: Descriptive alert message upon successful cancellation
                        LogAndNotify("Order Cancelled", $"Order {_selectedTransaction.ReceiptID} has been successfully cancelled.", true);

                        LoadData(); SwitchView("Profile"); modal.Close();
                    }
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.AddRange(new Control[] { iconWarning, lblWarn, lblDesc });
            }

            else if (type == "ReturnItem")
            {
                // Increased height from 440 to 500 to fit the multi-select item list
                modal.Size = new Size(550, 500);

                IconPictureBox iconWarning = new IconPictureBox { IconChar = IconChar.Undo, IconColor = Color.FromArgb(245, 158, 11), IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 25) };

                Label lblT = new Label { Text = "Process Item Return", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                lblT.Location = new Point((modal.Width - lblT.PreferredWidth) / 2, 95);

                Label lblDesc = new Label { Text = "Select the specific items below and attach proof\nof return or refund receipt.", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 130);

                // CHANGED: Replaced the ComboBox with a bordered checklist panel
                RoundedPanel wList = new RoundedPanel { Width = 450, Height = 140, BorderRadius = 4, BorderSize = 1, Padding = new Padding(10), BackColor = UITheme.CurrentInputBg, BorderColor = UITheme.CurrentBorder };
                wList.Location = new Point((modal.Width - 450) / 2, 175);

                FlowLayoutPanel flpItems = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
                wList.Controls.Add(flpItems);

                // Dynamically add a checkbox for every item in this transaction
                List<CheckBox> chkItems = new List<CheckBox>();
                foreach (var item in _selectedTransaction.Items)
                {
                    CheckBox chk = new CheckBox
                    {
                        Text = $"{item.Description} ({item.SerialNumber})",
                        Font = new Font("Segoe UI", 10.5F),
                        ForeColor = UITheme.CurrentText,
                        AutoSize = true,
                        Margin = new Padding(5, 5, 5, 10),
                        Cursor = Cursors.Hand,
                        Tag = item.SerialNumber // Hide the serial number in the tag for backend processing
                    };
                    chkItems.Add(chk);
                    flpItems.Controls.Add(chk);
                }

                string pendingRefundProof = null;

                Label lblFileName = new Label { Text = "No file attached.", Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = UITheme.MutedText, AutoSize = false, Size = new Size(450, 20), TextAlign = ContentAlignment.MiddleCenter };
                lblFileName.Location = new Point((modal.Width - 450) / 2, 380); // Pushed down to accommodate the list

                IconButton btnUp = new IconButton { Text = " Attach Proof Document", IconChar = IconChar.Paperclip, IconSize = 16, Size = new Size(216, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0) };
                btnUp.Location = new Point((modal.Width - 216) / 2, 335); // Pushed down to accommodate the list
                btnUp.BackColor = UITheme.CurrentInputBg;
                btnUp.ForeColor = UITheme.CurrentText;
                btnUp.IconColor = UITheme.CurrentText;
                btnUp.FlatAppearance.BorderColor = UITheme.CurrentBorder;

                btnUp.Click += (s, e) =>
                {
                    using (OpenFileDialog ofd = new OpenFileDialog())
                    {
                        if (ofd.ShowDialog() == DialogResult.OK)
                        {
                            pendingRefundProof = ofd.FileName;
                            lblFileName.Text = "📄 " + System.IO.Path.GetFileName(pendingRefundProof);
                            lblFileName.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                            lblFileName.ForeColor = Color.FromArgb(16, 185, 129);

                            lblFileName.Refresh();
                            modal.Refresh();
                        }
                    }
                };

                Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.Click += (s, e) => modal.Close();

                Button btnRet = new Button { Text = "Confirm Return", Size = new Size(140, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnRet.FlatAppearance.BorderSize = 0;

                int totalW = btnCancel.Width + 10 + btnRet.Width;
                btnCancel.Location = new Point((modal.Width - totalW) / 2, 16); btnRet.Location = new Point(((modal.Width - totalW) / 2) + btnCancel.Width + 10, 16);

                // CHANGED: Process multi-item loop
                btnRet.Click += (s, e) =>
                {
                    // Get all serial numbers where the cashier checked the box
                    var selectedSerials = chkItems.Where(c => c.Checked).Select(c => c.Tag.ToString()).ToList();

                    if (selectedSerials.Count == 0)
                    {
                        ShowToast("Please select at least one item to return.", false);
                        return;
                    }

                    if (string.IsNullOrEmpty(pendingRefundProof))
                    {
                        ShowToast("Please attach a refund proof.", false);
                        return;
                    }

                    bool success = true;

                    // Loop through and return every selected item in the backend
                    foreach (string ser in selectedSerials)
                    {
                        if (!_salesController.ProcessReturn(ser, "Customer returned item", _activeUserId))
                        {
                            success = false;
                        }
                    }

                    if (success)
                    {
                        _attachController.UploadAttachment(null, pendingRefundProof, _activeUserId, _selectedTransaction.ReceiptID);

                        _selectedTransaction.Status = "Returned";
                        _salesController.UpdateTransactionStatus(_selectedTransaction.ReceiptID, "Returned", _activeUserId);

                        // Instantly update local modified tracking
                        _selectedTransaction.ModifiedBy = _activeUserId;
                        _selectedTransaction.ModifiedOn = DateTime.Now;

                        // Alerts the user exactly how many items were successfully returned
                        LogAndNotify("Items Returned", $"Successfully processed return for {selectedSerials.Count} item(s).", true);

                        LoadData(); SwitchView("Profile"); modal.Close();
                    }
                    else
                    {
                        ShowToast("Failed to process return for one or more items.", false);
                    }
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnRet });
                modal.Controls.AddRange(new Control[] { iconWarning, lblT, lblDesc, wList, btnUp, lblFileName });
            }

            else if (type == "CompleteOrder")
            {
                modal.Size = new Size(400, 280);
                IconPictureBox iconInfo = new IconPictureBox { IconChar = IconChar.CheckCircle, IconColor = Color.FromArgb(16, 185, 129), IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 30) };

                Label lblWarn = new Label { Text = "Complete Order?", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 110);
                Label lblDesc = new Label { Text = $"Are you sure you want to complete\n{_selectedTransaction.ReceiptID}?\nThis will finalize the transaction.", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 145);

                Button btnCancel = new Button { Text = "Cancel", Size = new Size(110, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.Click += (s, e) => modal.Close();

                Button btnAction = new Button { Text = "Complete Order", Size = new Size(150, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;

                int totalW = btnCancel.Width + 10 + btnAction.Width;
                btnCancel.Location = new Point((modal.Width - totalW) / 2, 16); btnAction.Location = new Point(((modal.Width - totalW) / 2) + btnCancel.Width + 10, 16);

                btnAction.Click += (s, e) =>
                {
                    if (_salesController.UpdateTransactionStatus(_selectedTransaction.ReceiptID, "Completed", _activeUserId))
                    {
                        _selectedTransaction.Status = "Completed";

                        // Instantly update local modified tracking
                        _selectedTransaction.ModifiedBy = _activeUserId;
                        _selectedTransaction.ModifiedOn = DateTime.Now;

                        LogAndNotify("Order Completed", $"Order {_selectedTransaction.ReceiptID} has been successfully completed.", true);

                        LoadData(); SwitchView("Profile"); modal.Close();
                    }
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.AddRange(new Control[] { iconInfo, lblWarn, lblDesc });
            }


            modal.Controls.Add(pnlFooter);

            // ADDED: The beautiful black transparency overlay from Procurement
            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };

            overlay.Show();
            modal.ShowDialog(overlay);
            overlay.Dispose();
        }

        // =========================================================================
        // PDF GENERATION
        // =========================================================================
        private void GeneratePDF()
        {
            PrintDocument pd = new PrintDocument(); pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            pd.PrintPage += Pd_PrintPage;
            PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd, Width = 800, Height = 1000 }; ppd.ShowDialog();
        }
        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            int y = 50;

            Font fTitle = new Font("Arial", 24, FontStyle.Bold);
            Font fSub = new Font("Arial", 12, FontStyle.Bold);
            Font fN = new Font("Arial", 10);
            Font fB = new Font("Arial", 10, FontStyle.Bold);

            // ---- LEFT SIDE: STORE DETAILS ----
            g.DrawString("SJ PC STORE", fTitle, Brushes.Black, 50, y); y += 40;
            g.DrawString("McArthur Highway, Lolomboy Bocaue, Bulacan", fN, Brushes.DimGray, 50, y); y += 20;
            g.DrawString("Phone: 09391814103", fN, Brushes.DimGray, 50, y);

            // ---- RIGHT SIDE: DOCUMENT DETAILS ----
            // Condition: Changes title to QUOTATION if status is Quotation
            string docTitle = _selectedTransaction.Status.Equals("Quotation", StringComparison.OrdinalIgnoreCase) ? "QUOTATION" : "SALES INVOICE";
            g.DrawString(docTitle, fTitle, Brushes.DarkBlue, 450, 50);

            g.DrawString($"Receipt ID: {_selectedTransaction.ReceiptID}", fSub, Brushes.Black, 450, 90);
            g.DrawString($"Date: {_selectedTransaction.OrderDate:MMM dd, yyyy}", fN, Brushes.Black, 450, 110);
            g.DrawString($"Status: {_selectedTransaction.Status.ToUpper()}", fB, Brushes.DarkBlue, 450, 130);
            g.DrawString($"Warranty: {_selectedTransaction.WarrantyDays} Days", fN, Brushes.Black, 450, 150);

            // Condition: Show Transaction Number only if it's Online Payment
            if (_selectedTransaction.PaymentMethod == "Online Payment" && !string.IsNullOrEmpty(_selectedTransaction.TransactionNumber))
            {
                g.DrawString($"Ref No: {_selectedTransaction.TransactionNumber}", fN, Brushes.Black, 450, 170);
            }

            // ---- CUSTOMER DETAILS ----
            // Move Y down safely below the header block before drawing the line
            y = 210;
            g.DrawLine(Pens.Black, 50, y, 770, y); y += 30;

            g.DrawString("CUSTOMER:", fSub, Brushes.DarkBlue, 50, y); y += 25;
            g.DrawString(_selectedTransaction.CustomerName, fB, Brushes.Black, 50, y); y += 20;
            g.DrawString($"Payment Method: {_selectedTransaction.PaymentMethod}", fN, Brushes.Black, 50, y); y += 40;

            // ---- ITEM TABLE HEADER ----
            g.FillRectangle(Brushes.DarkBlue, 50, y, 720, 30);
            g.DrawString("ITEM NAME", fB, Brushes.White, 60, y + 7);

            // Shifted QTY and PRICE to the right to fill the space left by the removed TOTAL column
            g.DrawString("QTY", fB, Brushes.White, 560, y + 7);
            g.DrawString("UNIT PRICE", fB, Brushes.White, 650, y + 7);
            y += 40;

            // ---- ITEM TABLE ROWS ----
            // Condition: Check if this is a quotation
            bool isQuotation = _selectedTransaction.Status.Equals("Quotation", StringComparison.OrdinalIgnoreCase);

            foreach (var item in _selectedTransaction.Items)
            {
                string desc = item.Description.Length > 45 ? item.Description.Substring(0, 45) + "..." : item.Description;
                g.DrawString(desc, fN, Brushes.Black, 60, y);

                // Show serial number ONLY if it is an actual Order/Sale, hide if Quotation
                if (!isQuotation)
                {
                    g.DrawString($"SN: {item.SerialNumber}", new Font("Arial", 8, FontStyle.Italic), Brushes.DimGray, 60, y + 15);
                }

                g.DrawString(item.Quantity.ToString(), fN, Brushes.Black, 560, y);
                g.DrawString(item.UnitPrice.ToString("N2"), fN, Brushes.Black, 650, y);

                y += 30;
            }

            y += 10;

            g.DrawLine(Pens.Gray, 50, y, 770, y); y += 20;

            // ---- TOTALS ----
            g.DrawString("Subtotal:", fN, Brushes.DimGray, 560, y);
            g.DrawString(_selectedTransaction.SubTotal.ToString("C2"), fN, Brushes.Black, 680, y); y += 25;

            g.DrawString("Discount:", fN, Brushes.DimGray, 560, y);
            g.DrawString($"-{_selectedTransaction.Discount:C2}", fN, Brushes.Red, 680, y); y += 25;

            g.DrawString("Tax:", fN, Brushes.DimGray, 560, y);
            g.DrawString($"{_selectedTransaction.Tax:C2}", fN, Brushes.Black, 680, y); y += 25;

            g.DrawString("GRAND TOTAL:", fSub, Brushes.DarkBlue, 520, y);
            g.DrawString(_selectedTransaction.GrandTotal.ToString("C2"), fSub, Brushes.Black, 680, y);

            // ---- TERMS AND CONDITIONS ----
            y += 50;
            g.DrawString("Terms & Conditions:", fSub, Brushes.DarkBlue, 50, y); y += 25;
            g.DrawString("1. All sales are final. Items can only be returned or exchanged within 7 days with original receipt.", fN, Brushes.Black, 50, y); y += 20;
            g.DrawString("2. Physical damage, liquid damage, or unauthorized repairs void the warranty.", fN, Brushes.Black, 50, y); y += 20;
            g.DrawString("3. Quotations are strictly valid for 30 days from the date of issue.", fN, Brushes.Black, 50, y);

            // ---- SIGNATURES ----
            y = 1000;
            g.DrawLine(Pens.Black, 50, y, 250, y);
            g.DrawString("Authorized Signature", fN, Brushes.DimGray, 90, y + 10);

            // Added a customer signature line for professional symmetry
            g.DrawLine(Pens.Black, 520, y, 720, y);
            g.DrawString("Customer Signature", fN, Brushes.DimGray, 560, y + 10);
        }

        // =========================================================================
        // ATTACHMENTS
        // =========================================================================
        private void LoadAttachments()
        {
            if (flpAttachments == null || _selectedTransaction == null) return;
            flpAttachments.Controls.Clear();
            var attachments = _attachController.GetAttachmentsByTransaction(_selectedTransaction.ReceiptID);
            foreach (var att in attachments)
            {
                Panel attachRow = new Panel { Width = flpAttachments.Width - SystemInformation.VerticalScrollBarWidth - 5, Height = 30 };
                int maxW = attachRow.Width - 30 - 10;
                Label lblFile = new Label { Font = new Font("Segoe UI", 9.5F), ForeColor = Color.FromArgb(59, 130, 246), Cursor = Cursors.Hand, AutoSize = false, Width = maxW, Height = 20, TextAlign = ContentAlignment.MiddleLeft, Location = new Point(5, 6) };
                lblFile.Text = TruncateText(att.FileName, lblFile.Font, maxW);
                lblFile.Click += (s, e) => { try { System.Diagnostics.Process.Start(att.FilePath); } catch { } };
                IconButton btnDel = new IconButton { IconChar = IconChar.Trash, IconSize = 16, Size = new Size(25, 25), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, BackColor = Color.Transparent, IconColor = Color.FromArgb(239, 68, 68), ForeColor = Color.FromArgb(239, 68, 68), Location = new Point(attachRow.Width - 30, 3) };
                btnDel.FlatAppearance.BorderSize = 0; btnDel.Click += (s, e) => { if (_attachController.DeleteAttachment(att.AttachmentID)) { LogAndNotify("Attachment Deleted", att.FileName, true); LoadAttachments(); } };
                attachRow.Controls.Add(lblFile); attachRow.Controls.Add(btnDel); flpAttachments.Controls.Add(attachRow);
            }
            if (flpAttachments.Controls.Count > 0) flpAttachments.Height = flpAttachments.Controls.Count * 35 + 5;
        }

        private string TruncateText(string text, Font font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (TextRenderer.MeasureText(text, font).Width <= maxWidth) return text;
            string ellipsis = "..."; int ellipsisWidth = TextRenderer.MeasureText(ellipsis, font).Width;
            int allowedWidth = maxWidth - ellipsisWidth; if (allowedWidth <= 0) return ellipsis;
            for (int i = text.Length - 1; i > 0; i--) { string trimmed = text.Substring(0, i); if (TextRenderer.MeasureText(trimmed, font).Width <= allowedWidth) return trimmed + ellipsis; }
            return ellipsis;
        }

        // =========================================================================
        // THEME LOGIC
        // =========================================================================
        public void ApplyTheme()
        {
            this.BackColor = UITheme.CurrentWorkspace;
            if (pnlLanding != null) pnlLanding.BackColor = UITheme.CurrentWorkspace;
            if (pnlProfile != null) pnlProfile.BackColor = UITheme.CurrentWorkspace;
            if (pnlCreate != null) pnlCreate.BackColor = UITheme.CurrentWorkspace;

            Color cardBg = UITheme.IsDarkMode ? Color.FromArgb(53, 50, 58) : Color.FromArgb(248, 249, 250);
            if (pnlDetailHeader != null) pnlDetailHeader.BackColor = cardBg;
            if (profileStepper != null) profileStepper.BackColor = cardBg;
            // Ensures the "Create New Order" header is always distinct from the gray workspace
            if (pnlCreateHeader != null) pnlCreateHeader.BackColor = UITheme.CurrentPanel;

            foreach (DarkComboBox cmb in _comboInputs) { cmb.BackColor = UITheme.CurrentInputBg; cmb.ForeColor = UITheme.CurrentText; }
            foreach (DarkComboBox cmb in _comboFilterInputs) { cmb.BackColor = UITheme.CurrentPanel; cmb.ForeColor = UITheme.CurrentText; }
            foreach (RoundedPanel wrap in _inputWrappers) { wrap.BackColor = UITheme.CurrentInputBg; wrap.BorderColor = UITheme.CurrentBorder; foreach (Control c in wrap.Controls) { if (c is IconPictureBox icon) icon.IconColor = UITheme.CurrentIcon; } }
            foreach (RoundedPanel wrap in _borderedContainers) { wrap.BackColor = UITheme.CurrentPanel; wrap.BorderColor = UITheme.CurrentBorder; }
            foreach (TextBox txt in _textInputs) { txt.BackColor = UITheme.CurrentInputBg; txt.ForeColor = UITheme.CurrentText; }
            if (nudWarrantyDays != null) { nudWarrantyDays.BackColor = UITheme.CurrentInputBg; nudWarrantyDays.ForeColor = UITheme.CurrentText; }
            foreach (Control c in _dynamicTexts) { c.ForeColor = UITheme.CurrentText; }
            foreach (Control c in _mutedTexts) { c.ForeColor = UITheme.MutedText; }

            foreach (IconButton btn in _buttons)
            {
                string tag = btn.Tag?.ToString() ?? "";
                if (tag == "ActionAdd") { btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btn.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : UITheme.SecondaryDark; }
                else if (tag == "Danger")
                {
                    btn.BackColor = Color.FromArgb(239, 68, 68); // Solid Red
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68);
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 38, 38); // Darker red hover
                }
                else if (tag == "Success")
                {
                    btn.BackColor = Color.FromArgb(16, 185, 129); // Solid Green
                    btn.ForeColor = Color.White;
                    btn.FlatAppearance.BorderColor = Color.FromArgb(16, 185, 129);
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(5, 150, 105); // Darker green hover
                }
                else if (tag == "Secondary") { btn.BackColor = Color.Transparent; btn.ForeColor = UITheme.CurrentText; btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = UITheme.CurrentBorder; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230); }
                else { btn.BackColor = UITheme.CurrentPanel; btn.ForeColor = UITheme.CurrentText; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230); }
                btn.IconColor = btn.ForeColor; btn.FlatAppearance.MouseDownBackColor = btn.BackColor;
            }

            // Attachment button theming (both profile and create)
            if (btnAttach != null) { btnAttach.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btnAttach.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; }
            if (btnAttachCreate != null) { btnAttachCreate.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btnAttachCreate.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; }

            if (dgvSalesList != null)
            {
                dgvSalesList.BackgroundColor = UITheme.CurrentPanel;
                dgvSalesList.GridColor = UITheme.CurrentBorder;
                dgvSalesList.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

                dgvSalesList.DefaultCellStyle.BackColor = UITheme.CurrentPanel;
                dgvSalesList.DefaultCellStyle.ForeColor = UITheme.CurrentText;

                // CHANGED: Replicate dgvCartItems by hiding the selection colors completely
                dgvSalesList.DefaultCellStyle.SelectionBackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(220, 230, 240);
                dgvSalesList.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;

                dgvSalesList.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
                dgvSalesList.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;

                // CHANGED: Added this to prevent column headers from flashing blue when clicked
                dgvSalesList.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgvSalesList.ColumnHeadersDefaultCellStyle.BackColor;
            }
            if (dgvProfileItems != null)
            {
                // Fixes the dark gray empty background area
                dgvProfileItems.BackgroundColor = UITheme.CurrentPanel;
                dgvProfileItems.GridColor = UITheme.CurrentBorder;

                // Fixes the row colors and explicitly hides selection colors
                dgvProfileItems.DefaultCellStyle.BackColor = UITheme.CurrentPanel;
                dgvProfileItems.DefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvProfileItems.DefaultCellStyle.SelectionBackColor = UITheme.CurrentPanel; // Hides selection
                dgvProfileItems.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;  // Hides selection

                // Fixes the bright blue header to match the clean PO header
                dgvProfileItems.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
                dgvProfileItems.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvProfileItems.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgvProfileItems.ColumnHeadersDefaultCellStyle.BackColor;
            }
            if (profileStepper != null) profileStepper.Invalidate();
            if (dtpOrderDate != null) dtpOrderDate.ApplyTheme(UITheme.IsDarkMode);
            this.Invalidate(true);

            if (dgvCartItems != null)
            {
                // Fixes the dark gray empty background area
                dgvCartItems.BackgroundColor = UITheme.CurrentPanel;
                dgvCartItems.GridColor = UITheme.CurrentBorder;

                // Fixes the row colors and explicitly hides selection colors
                dgvCartItems.DefaultCellStyle.BackColor = UITheme.CurrentPanel;
                dgvCartItems.DefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvCartItems.DefaultCellStyle.SelectionBackColor = UITheme.CurrentPanel; // Hides selection
                dgvCartItems.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;  // Hides selection

                // Fixes the bright blue header to match the clean PO header
                dgvCartItems.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
                dgvCartItems.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvCartItems.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgvCartItems.ColumnHeadersDefaultCellStyle.BackColor;
            }
        }
    }
}