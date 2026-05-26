using FontAwesome.Sharp;
using SJ_PC_Store_SIMS.Controllers;
using SJ_PC_Store_SIMS.Models;
using SJ_PC_Store_SIMS.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
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

        private SmoothPanel pnlLanding, pnlProfile, pnlCreate, pnlDetailHeader;
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
        private Label lblDetTitle, lblDetCustomer, lblDetDate, lblDetPayment, lblDetWarranty;
        private Label lblDetAuditCreated, lblDetAuditModified;
        private Label lblTotalSub, lblTotalDisc, lblTotalTax, lblTotalGrand;
        private BadgeLabel badgeStatus;
        private IconButton btnSubmitPayment, btnCancel, btnPDF, btnEdit;
        private FlowLayoutPanel flpLeftDetails;
        private Panel pnlRightItems;
        private RoundedPanel cardOrderDetails, cardAudit, cardAttach;

        // Create/Edit Controls
        private Label lblCreateTitle, lblCreateSub, lblCreateGrand;
        private ThemedDatePicker dtpOrderDate;
        private TextBox txtCustomerName, txtTransactionNumber;
        private NumericUpDown nudWarrantyDays;
        private DarkComboBox cmbPaymentMethod, cmbCreateDiscountType, cmbCreateTaxType, cmbItemSelect, cmbSerialSelect;
        private TextBox txtQty, txtPrice, txtCreateDiscount, txtCreateTax;
        private BufferedFlowLayoutPanel flpCreateItems;
        private Button btnAttach, btnAttachCreate;
        private FlowLayoutPanel flpAttachments;
        private Control wTransactionNumber;

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
            toast.Paint += (s, e) => {
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
            RoundedPanel container = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, BorderSize = 1, Padding = new Padding(1) };
            _borderedContainers.Add(container);

            SmoothPanel toolbar = new SmoothPanel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent, Padding = new Padding(25, 20, 25, 20) };
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
            dgvSalesList.DefaultCellStyle.Padding = new Padding(25, 0, 0, 0);
            dgvSalesList.ColumnHeadersDefaultCellStyle.Padding = new Padding(25, 0, 0, 0);
            dgvSalesList.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            dgvSalesList.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            dgvSalesList.Paint += (s, e) => {
                using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 59, dgvSalesList.Width, 59); }
                if (dgvSalesList.Rows.Count == 0) TextRenderer.DrawText(e.Graphics, "No Sales Orders found in the database.", new Font("Segoe UI", 12F, FontStyle.Italic), dgvSalesList.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            dgvSalesList.CellFormatting += (s, e) => {
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

            container.Controls.Add(dgvSalesList); container.Controls.Add(toolbar);
            pnlLanding.Controls.Add(container);
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
            btnSubmitPayment = new IconButton { Text = " Submit Payment", IconChar = IconChar.MoneyBill, IconSize = 16, Size = new Size(150, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Success", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(10, 0, 0, 0) };
            btnSubmitPayment.Click += (s, e) => OpenModal("SubmitPayment");
            btnCancel = new IconButton { Text = " Cancel", IconChar = IconChar.TimesCircle, IconSize = 16, Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Danger", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(10, 0, 0, 0) };
            btnCancel.Click += (s, e) => OpenModal("CancelOrder");
            flpRight.Controls.AddRange(new Control[] { btnSubmitPayment, btnCancel, btnEdit, btnPDF });
            _buttons.AddRange(new[] { btnEdit, btnPDF }); _dynamicTexts.Add(lblDetTitle);
            pnlDetailHeader.Controls.Add(flpRight); pnlDetailHeader.Controls.Add(flpLeft);

            profileStepper = new StatusStepper { Dock = DockStyle.Top, Height = 100 };

            Panel pnlBody = new Panel { Dock = DockStyle.Fill };
            flpLeftDetails = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 380, AutoScroll = true, Padding = new Padding(25, 20, 10, 20), FlowDirection = FlowDirection.TopDown, WrapContents = false };
            pnlRightItems = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(30) };

            Func<string, RoundedPanel> CreateInfoSection = (title) => {
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
            cardOrderDetails.Controls.AddRange(new Control[] { lblDetCustomer, lblDetDate, lblDetPayment, lblDetWarranty });
            cardOrderDetails.SizeChanged += (s, e) => AdjustCardHeight(cardOrderDetails);

            cardAudit = CreateInfoSection("Audit Trail");
            lblDetAuditCreated = new Label { Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(20, 20) };
            lblDetAuditModified = new Label { Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(20, 60) };
            cardAudit.Controls.AddRange(new Control[] { lblDetAuditCreated, lblDetAuditModified });
            cardAudit.SizeChanged += (s, e) => AdjustCardHeight(cardAudit);

            cardAttach = CreateInfoSection("Attachments");
            btnAttach = new Button { Text = "📝 Attach File", Dock = DockStyle.Top, Size = new Size(110, 35), Location = new Point(20, 20), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            btnAttach.FlatAppearance.BorderSize = 0;
            btnAttach.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog()) { ofd.Multiselect = true; ofd.Filter = "All Files|*.*|Documents|*.pdf;*.docx;*.xlsx|Images|*.jpg;*.png;*.bmp"; if (ofd.ShowDialog() == DialogResult.OK) { foreach (string file in ofd.FileNames) { _attachController.UploadAttachment(null, file, _activeUserId, _selectedTransaction.ReceiptID); } LogAndNotify("Attachment", $"{ofd.FileNames.Length} file(s) attached.", true); LoadAttachments(); } }
            };
            cardAttach.Controls.Add(btnAttach);
            flpAttachments = new FlowLayoutPanel { Location = new Point(20, 65), Size = new Size(280, 150), AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            cardAttach.Controls.Add(flpAttachments);
            cardAttach.SizeChanged += (s, e) => AdjustCardHeight(cardAttach);

            _mutedTexts.AddRange(new[] { lblDetDate, lblDetPayment, lblDetWarranty, lblDetAuditCreated, lblDetAuditModified });

            Panel pnlRightHeader = new Panel { Dock = DockStyle.Top, Height = 60 };
            Label lItemsTitle = new Label { Text = "Ordered Items", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 10) };
            _dynamicTexts.Add(lItemsTitle);
            IconButton btnReturn = new IconButton { Text = " Return Item", IconChar = IconChar.Undo, IconSize = 16, Size = new Size(140, 38), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(pnlRightHeader.Width - 160, 10), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Danger", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
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

            btnSubmitPayment.Visible = _selectedTransaction.Status == "Ordered" || _selectedTransaction.Status == "Quotation";
            btnCancel.Visible = _selectedTransaction.Status != "Paid" && _selectedTransaction.Status != "Completed" && _selectedTransaction.Status != "Returned";
            btnEdit.Visible = _selectedTransaction.Status == "Quotation" || _selectedTransaction.Status == "Ordered";

            lblDetCustomer.Text = $"{_selectedTransaction.CustomerName}";
            lblDetDate.Text = $"Order Date: {_selectedTransaction.OrderDate:MMM dd, yyyy}";
            lblDetPayment.Text = $"Payment: {_selectedTransaction.PaymentMethod}";
            lblDetWarranty.Text = $"Warranty: {_selectedTransaction.WarrantyDays} days";
            if (_selectedTransaction.Status == "Quotation" && (DateTime.Now - _selectedTransaction.OrderDate).TotalDays > 30)
                lblDetWarranty.Text += "\n⚠️ This quotation has expired (30+ days).";

            lblDetAuditCreated.Text = $"Created By:\n{_selectedTransaction.CreatedBy} ({_selectedTransaction.CreatedOn:g})";
            lblDetAuditModified.Text = $"Modified By:\n{_selectedTransaction.ModifiedBy ?? "N/A"}";

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
            RoundedPanel container = new RoundedPanel { Dock = DockStyle.Fill, BorderRadius = 6, BorderSize = 1, Padding = new Padding(1) };
            _borderedContainers.Add(container);

            SmoothPanel pnlCreateHeader = new SmoothPanel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent, Padding = new Padding(25, 20, 25, 20) };
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

            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30), AutoScroll = true };

            // ---- ORDER DETAILS SECTION (BALANCED LAYOUT) ----
            Label lHead1 = new Label { Text = "ORDER DETAILS", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(30, 25) };
            _dynamicTexts.Add(lHead1); pnlBody.Controls.Add(lHead1);

            RoundedPanel pnlDetails = new RoundedPanel { Location = new Point(30, 65), Size = new Size(pnlBody.Width - 60, 210), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BorderRadius = 6, BorderSize = 1, Padding = new Padding(25) };

            // Row 1: Customer Name (left) | Order Date (right)
            Label lCust = new Label { Text = "Customer Name", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(25, 20) };
            txtCustomerName = new TextBox { Font = new Font("Segoe UI", 11F), BorderStyle = BorderStyle.None };
            Control wCust = CreateInputWrapper(txtCustomerName, 350); wCust.Location = new Point(25, 45); _textInputs.Add(txtCustomerName);

            Label lDate = new Label { Text = "Order Date", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(420, 20) };
            dtpOrderDate = new ThemedDatePicker();
            Control wDate = CreateInputWrapper(dtpOrderDate, 180); wDate.Location = new Point(420, 45);

            // Row 2: Payment Method (left) | Transaction Number (right, below payment)
            Label lPay = new Label { Text = "Payment Method", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(25, 100) };
            cmbPaymentMethod = new DarkComboBox { Font = new Font("Segoe UI", 11F), Size = new Size(180, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbPaymentMethod.Items.AddRange(new[] { "Cash Payment", "Online Payment" });
            cmbPaymentMethod.SelectedIndex = 0;
            cmbPaymentMethod.SelectedIndexChanged += (s, e) => { if (wTransactionNumber != null) wTransactionNumber.Visible = cmbPaymentMethod.Text == "Online Payment"; };
            Control wPay = CreateInputWrapper(cmbPaymentMethod, 210); wPay.Location = new Point(25, 125); _comboInputs.Add(cmbPaymentMethod);

            Label lTN = new Label { Text = "Transaction Number", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(25, 165) };
            txtTransactionNumber = new TextBox { Font = new Font("Segoe UI", 11F), BorderStyle = BorderStyle.None };
            wTransactionNumber = CreateInputWrapper(txtTransactionNumber, 210); wTransactionNumber.Location = new Point(25, 188); wTransactionNumber.Visible = false;
            _textInputs.Add(txtTransactionNumber);

            // Row 2 right: Warranty
            Label lWarr = new Label { Text = "Warranty (Days)", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(420, 100) };
            nudWarrantyDays = new NumericUpDown { Font = new Font("Segoe UI", 11F), Minimum = 0, Maximum = 365, Value = 7, BorderStyle = BorderStyle.None };
            Control wWarr = CreateInputWrapper(nudWarrantyDays, 140); wWarr.Location = new Point(420, 125);

            pnlDetails.Controls.AddRange(new Control[] { lCust, wCust, lDate, wDate, lPay, wPay, lTN, wTransactionNumber, lWarr, wWarr });
            _mutedTexts.AddRange(new[] { lCust, lDate, lPay, lTN, lWarr }); _borderedContainers.Add(pnlDetails);
            pnlBody.Controls.Add(pnlDetails);

            // ---- ITEM CART SELECTION SECTION ----
            Label lHead2 = new Label { Text = "ITEM CART SELECTION", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(30, 300) };
            _dynamicTexts.Add(lHead2); pnlBody.Controls.Add(lHead2);

            RoundedPanel pnlItems = new RoundedPanel { Location = new Point(30, 340), Size = new Size(pnlBody.Width - 60 - 370, 420), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BorderRadius = 6, BorderSize = 1, Padding = new Padding(25) };

            Panel pnlItemHeader = new Panel { Dock = DockStyle.Top, Height = 50 };
            Label lblItemCartTitle = new Label { Text = "Item Cart", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(0, 10) };
            IconButton btnAddItem = new IconButton { Text = " Add Item", IconChar = IconChar.Plus, IconSize = 14, Size = new Size(110, 35), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(pnlItems.Width - 160, 5), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "ActionAdd", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0) };
            btnAddItem.Click += (s, e) => AddCartItemRow(); _buttons.Add(btnAddItem);
            pnlItemHeader.Controls.Add(lblItemCartTitle); pnlItemHeader.Controls.Add(btnAddItem);

            Panel pnlItemSearch = new Panel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(0, 15, 0, 0) };
            pnlItemSearch.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlItemSearch.Width, 0); } };

            cmbItemSelect = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(230, 35), Location = new Point(5, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbItemSelect.SelectedIndexChanged += (s, e) => OnItemSelected();
            _comboInputs.Add(cmbItemSelect);

            cmbSerialSelect = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(170, 35), Location = new Point(245, 20), DropDownStyle = ComboBoxStyle.DropDown };
            _comboInputs.Add(cmbSerialSelect);

            txtQty = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "1", Size = new Size(60, 35), Location = new Point(425, 20), TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            _textInputs.Add(txtQty);
            Control wQty = CreateInputWrapper(txtQty, 70); wQty.Location = new Point(425, 20); wQty.Margin = new Padding(0);

            txtPrice = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "0.00", Size = new Size(100, 35), Location = new Point(505, 20), TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None, ReadOnly = true };
            _textInputs.Add(txtPrice);
            Control wPrice = CreateInputWrapper(txtPrice, 110); wPrice.Location = new Point(505, 20); wPrice.Margin = new Padding(0);

            pnlItemSearch.Controls.AddRange(new Control[] { cmbItemSelect, cmbSerialSelect, wQty, wPrice });

            // Data Grid View for cart items
            SmoothGrid dgvCartItems = new SmoothGrid { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, EnableHeadersVisualStyles = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeight = 45, RowTemplate = { Height = 40 }, Margin = new Padding(0, 10, 0, 0) };
            dgvCartItems.Columns.Add("Desc", "ITEM NAME"); dgvCartItems.Columns.Add("Serial", "SERIAL NUMBER");
            dgvCartItems.Columns.Add("Qty", "QTY"); dgvCartItems.Columns.Add("Price", "UNIT PRICE"); dgvCartItems.Columns.Add("Total", "TOTAL");
            dgvCartItems.Columns["Price"].DefaultCellStyle.Format = "C2"; dgvCartItems.Columns["Total"].DefaultCellStyle.Format = "C2";
            dgvCartItems.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvCartItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            dgvCartItems.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            dgvCartItems.DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);

            // Store reference for adding/removing items
            flpCreateItems = new BufferedFlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            // We'll use the grid instead, but keep flpCreateItems for backward compat
            flpCreateItems.Controls.Add(dgvCartItems);

            pnlItems.Controls.Add(flpCreateItems); pnlItems.Controls.Add(pnlItemSearch); pnlItems.Controls.Add(pnlItemHeader);

            // ---- TOTALS + ATTACHMENTS RIGHT PANEL ----
            Panel pnlRightCol = new Panel { Location = new Point(pnlBody.Width - 370, 340), Size = new Size(360, 420), Anchor = AnchorStyles.Top | AnchorStyles.Right };

            RoundedPanel pnlTotals = new RoundedPanel { Location = new Point(0, 0), Size = new Size(360, 260), BorderRadius = 6, BorderSize = 1, Padding = new Padding(20) };

            Label lblTotalsHeader = new Label { Text = "TOTALS", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(20, 15) };
            _dynamicTexts.Add(lblTotalsHeader);

            lblCreateSub = new Label { Text = "Subtotal: ₱ 0.00", Font = new Font("Segoe UI", 10F), AutoSize = true, Location = new Point(20, 50) }; _dynamicTexts.Add(lblCreateSub);

            Label lDisc = new Label { Text = "Discount", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(20, 90) };
            cmbCreateDiscountType = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(50, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCreateDiscountType.Items.AddRange(new[] { "₱", "%" }); cmbCreateDiscountType.SelectedIndex = 0; cmbCreateDiscountType.SelectedIndexChanged += (s, e) => CalculateTotals();
            _comboInputs.Add(cmbCreateDiscountType);
            txtCreateDiscount = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "0.00", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            txtCreateDiscount.TextChanged += (s, e) => CalculateTotals(); _textInputs.Add(txtCreateDiscount);
            Control wDiscType = CreateInputWrapper(cmbCreateDiscountType, 60); wDiscType.Location = new Point(100, 85); wDiscType.Margin = new Padding(0);
            Control wDisc = CreateInputWrapper(txtCreateDiscount, 150); wDisc.Location = new Point(170, 85); wDisc.Margin = new Padding(0);

            Label lTax = new Label { Text = "Tax", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(20, 135) };
            cmbCreateTaxType = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(50, 30), DropDownStyle = ComboBoxStyle.DropDownList };
            cmbCreateTaxType.Items.AddRange(new[] { "₱", "%" }); cmbCreateTaxType.SelectedIndex = 1; cmbCreateTaxType.SelectedIndexChanged += (s, e) => CalculateTotals();
            _comboInputs.Add(cmbCreateTaxType);
            txtCreateTax = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "0.00", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            txtCreateTax.TextChanged += (s, e) => CalculateTotals(); _textInputs.Add(txtCreateTax);
            Control wTaxType = CreateInputWrapper(cmbCreateTaxType, 60); wTaxType.Location = new Point(100, 130); wTaxType.Margin = new Padding(0);
            Control wTax = CreateInputWrapper(txtCreateTax, 150); wTax.Location = new Point(170, 130); wTax.Margin = new Padding(0);

            lblCreateGrand = new Label { Text = "GRAND TOTAL: ₱ 0.00", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 185), ForeColor = Color.FromArgb(16, 185, 129) };
            _dynamicTexts.Add(lblCreateGrand);

            pnlTotals.Controls.AddRange(new Control[] { lblTotalsHeader, lblCreateSub, lDisc, wDiscType, wDisc, lTax, wTaxType, wTax, lblCreateGrand });

            RoundedPanel pnlAttachCreate = new RoundedPanel { Location = new Point(0, 275), Size = new Size(360, 130), BorderRadius = 6, BorderSize = 1, Padding = new Padding(15) };
            btnAttachCreate = new Button { Text = "📝 Attach File", Dock = DockStyle.Top, Size = new Size(110, 35), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            btnAttachCreate.FlatAppearance.BorderSize = 0;
            btnAttachCreate.Click += (s, e) => {
                using (OpenFileDialog ofd = new OpenFileDialog()) { ofd.Multiselect = true; ofd.Filter = "All Files|*.*"; if (ofd.ShowDialog() == DialogResult.OK) { foreach (string file in ofd.FileNames) { _attachController.UploadAttachment(null, file, _activeUserId, _selectedTransaction?.ReceiptID); } ShowToast("Files attached.", true); } }
            };
            FlowLayoutPanel flpCreateAttachments = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
            pnlAttachCreate.Controls.Add(flpCreateAttachments); pnlAttachCreate.Controls.Add(btnAttachCreate);

            pnlRightCol.Controls.Add(pnlTotals); pnlRightCol.Controls.Add(pnlAttachCreate);
            _borderedContainers.Add(pnlItems);

            pnlBody.Controls.Add(pnlRightCol); pnlBody.Controls.Add(pnlItems);
            container.Controls.Add(pnlBody); container.Controls.Add(pnlCreateHeader);
            pnlCreate.Controls.Add(container);

            pnlBody.Resize += (s, e) => {
                pnlDetails.Width = pnlBody.Width - 60;
                pnlItems.Width = pnlBody.Width - 60 - 370;
                pnlRightCol.Location = new Point(pnlBody.Width - 370, 340);
            };
        }

        private void OnItemSelected()
        {
            if (cmbItemSelect.SelectedIndex < 0) return;
            string selected = cmbItemSelect.Text;
            var match = _dbItems.FirstOrDefault(i => $"{i.Category} {i.Specs} ({i.ItemCondition})" == selected);
            if (match != null)
            {
                txtPrice.Text = match.CurrentValue.ToString("0.00");
                cmbSerialSelect.Items.Clear();
                var available = _salesController.GetAvailableStockForItem(match.ItemCode);
                foreach (var s in available) cmbSerialSelect.Items.Add(s.SerialNumber);
                if (cmbSerialSelect.Items.Count > 0) cmbSerialSelect.SelectedIndex = 0;
            }
        }

        private void AddCartItemRow(SalesItemModel existingItem = null)
        {
            if (cmbItemSelect.SelectedIndex < 0 || cmbSerialSelect.SelectedIndex < 0) { ShowToast("Please select an item and serial number.", false); return; }
            string serial = existingItem?.SerialNumber ?? cmbSerialSelect.Text;
            string itemDesc = existingItem?.Description ?? cmbItemSelect.Text;
            decimal price = existingItem?.UnitPrice ?? (decimal.TryParse(txtPrice.Text, out decimal p) ? p : 0);
            int qty = existingItem?.Quantity ?? (int.TryParse(txtQty.Text, out int q) ? q : 1);

            // Find the grid
            SmoothGrid grid = null;
            foreach (Control c in flpCreateItems.Controls)
                if (c is SmoothGrid g) { grid = g; break; }
            if (grid == null) return;

            // Check for duplicate serial
            foreach (DataGridViewRow row in grid.Rows)
                if (row.Cells["Serial"].Value?.ToString() == serial) { ShowToast("This serial number is already in the cart.", false); return; }

            grid.Rows.Add(itemDesc, serial, qty, price, price * qty);
            CalculateTotals();
        }

        private void PrepareCreateForm()
        {
            // Clear the grid
            foreach (Control c in flpCreateItems.Controls)
                if (c is SmoothGrid grid) grid.Rows.Clear();

            // Populate item combo
            cmbItemSelect.Items.Clear();
            foreach (var item in _dbItems)
                cmbItemSelect.Items.Add($"{item.Category} {item.Specs} ({item.ItemCondition})");

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
            CalculateTotals(); ApplyTheme();
        }

        private void CalculateTotals()
        {
            decimal sub = 0;
            foreach (Control c in flpCreateItems.Controls)
                if (c is SmoothGrid grid)
                    foreach (DataGridViewRow row in grid.Rows)
                        if (row.Cells["Total"].Value != null)
                            sub += Convert.ToDecimal(row.Cells["Total"].Value);

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
            foreach (Control c in flpCreateItems.Controls)
                if (c is SmoothGrid grid)
                    foreach (DataGridViewRow row in grid.Rows)
                        if (!row.IsNewRow && row.Cells["Desc"].Value != null)
                            items.Add(new SalesItemModel
                            {
                                SerialNumber = row.Cells["Serial"].Value?.ToString(),
                                Description = row.Cells["Desc"].Value?.ToString(),
                                Quantity = Convert.ToInt32(row.Cells["Qty"].Value ?? 1),
                                UnitPrice = Convert.ToDecimal(row.Cells["Price"].Value ?? 0)
                            });

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

            if (type == "SubmitPayment")
            {
                modal.Size = new Size(450, 300);
                Label lblTitle = new Label { Text = "Submit Payment", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(30, 30) };
                Label lblAmt = new Label { Text = "Amount Received", Font = new Font("Segoe UI", 9.5F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(30, 80) };
                TextBox txtAmtRec = new TextBox { Font = new Font("Segoe UI", 12F), Size = new Size(200, 35), Location = new Point(30, 105), BorderStyle = BorderStyle.FixedSingle };
                Label lblChange = new Label { Text = "Change: ₱ 0.00", Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true, Location = new Point(30, 155), ForeColor = Color.FromArgb(16, 185, 129) };
                txtAmtRec.TextChanged += (s, e) => { if (decimal.TryParse(txtAmtRec.Text, out decimal rec)) lblChange.Text = $"Change: {Math.Max(0, rec - _selectedTransaction.GrandTotal):C2}"; };
                Button btnConfirm = new Button { Text = "Confirm Payment", Size = new Size(140, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(30, 210) }; btnConfirm.FlatAppearance.BorderSize = 0;
                btnConfirm.Click += (s, e) => { if (_salesController.ProcessPayment(_selectedTransaction.ReceiptID, _activeUserId)) { _selectedTransaction.Status = "Paid"; LogAndNotify("Payment Received", _selectedTransaction.ReceiptID, true); LoadData(); SwitchView("Profile"); modal.Close(); } else ShowToast("Payment processing failed.", false); };
                modal.Controls.AddRange(new Control[] { lblTitle, lblAmt, txtAmtRec, lblChange, btnConfirm });
            }
            else if (type == "CancelOrder")
            {
                modal.Size = new Size(400, 220);
                Label lblW = new Label { Text = "Cancel Order?", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(30, 30) };
                Label lblD = new Label { Text = "This action cannot be undone.", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(30, 75) };
                Button btnKeep = new Button { Text = "Keep Order", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(100, 130) }; btnKeep.Click += (s, e) => modal.Close();
                Button btnConf = new Button { Text = "Cancel Order", Size = new Size(120, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(210, 130) };
                btnConf.Click += (s, e) => { if (_salesController.UpdateTransactionStatus(_selectedTransaction.ReceiptID, "Cancelled", _activeUserId)) { _selectedTransaction.Status = "Cancelled"; LogAndNotify("Order Cancelled", _selectedTransaction.ReceiptID, true); LoadData(); SwitchView("Profile"); modal.Close(); } };
                modal.Controls.AddRange(new Control[] { lblW, lblD, btnKeep, btnConf });
            }
            else if (type == "ReturnItem")
            {
                modal.Size = new Size(500, 350);
                Label lblT = new Label { Text = "Return Item", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(30, 30) };
                Label lblS = new Label { Text = "Select item to return:", Font = new Font("Segoe UI", 9.5F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(30, 80) };
                DarkComboBox cmbR = new DarkComboBox { Font = new Font("Segoe UI", 11F), Size = new Size(420, 35), Location = new Point(30, 105), DropDownStyle = ComboBoxStyle.DropDownList };
                foreach (var item in _selectedTransaction.Items) cmbR.Items.Add($"{item.Description} ({item.SerialNumber})");
                if (cmbR.Items.Count > 0) cmbR.SelectedIndex = 0;
                Button btnUp = new Button { Text = "Attach Refund Proof", Size = new Size(160, 35), Location = new Point(30, 160), FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9F) };
                btnUp.Click += (s, e) => { using (OpenFileDialog ofd = new OpenFileDialog()) { if (ofd.ShowDialog() == DialogResult.OK) { _attachController.UploadAttachment(null, ofd.FileName, _activeUserId, _selectedTransaction.ReceiptID); ShowToast("Refund proof attached.", true); } } };
                Button btnRet = new Button { Text = "Confirm Return", Size = new Size(140, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(200, 160) };
                btnRet.Click += (s, e) => { string sel = cmbR.Text; int pIdx = sel.LastIndexOf("("); if (pIdx >= 0) { string ser = sel.Substring(pIdx + 1).TrimEnd(')'); if (_salesController.ProcessReturn(ser, "Customer returned item", _activeUserId)) { _selectedTransaction.Status = "Returned"; _salesController.UpdateTransactionStatus(_selectedTransaction.ReceiptID, "Returned", _activeUserId); LogAndNotify("Item Returned", ser, true); LoadData(); SwitchView("Profile"); modal.Close(); } } };
                modal.Controls.AddRange(new Control[] { lblT, lblS, cmbR, btnUp, btnRet });
            }
            modal.StartPosition = FormStartPosition.CenterParent; modal.ShowDialog();
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
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.HighQuality; int y = 50;
            Font fTitle = new Font("Arial", 24, FontStyle.Bold), fSub = new Font("Arial", 12, FontStyle.Bold), fN = new Font("Arial", 10), fB = new Font("Arial", 10, FontStyle.Bold);
            g.DrawString("SJ PC STORE", fTitle, Brushes.Black, 50, y); y += 40;
            g.DrawString("McArthur Highway, Bocaue, Bulacan", fN, Brushes.DimGray, 50, y); y += 20;
            g.DrawString("SALES INVOICE / RECEIPT", fTitle, Brushes.DarkBlue, 450, 50);
            g.DrawString($"Receipt ID: {_selectedTransaction.ReceiptID}", fSub, Brushes.Black, 450, 90);
            g.DrawString($"Date: {_selectedTransaction.OrderDate:MMM dd, yyyy}", fN, Brushes.Black, 450, 110);
            g.DrawString($"Status: {_selectedTransaction.Status.ToUpper()}", fB, Brushes.DarkBlue, 450, 130);
            y += 50; g.DrawLine(Pens.Black, 50, y, 770, y); y += 30;
            g.DrawString("CUSTOMER:", fSub, Brushes.DarkBlue, 50, y); y += 25;
            g.DrawString(_selectedTransaction.CustomerName, fB, Brushes.Black, 50, y); y += 20;
            g.DrawString($"Payment: {_selectedTransaction.PaymentMethod}", fN, Brushes.Black, 50, y); y += 40;
            g.FillRectangle(Brushes.DarkBlue, 50, y, 720, 30);
            g.DrawString("ITEM NAME", fB, Brushes.White, 60, y + 7); g.DrawString("SERIAL", fB, Brushes.White, 350, y + 7);
            g.DrawString("QTY", fB, Brushes.White, 500, y + 7); g.DrawString("PRICE", fB, Brushes.White, 560, y + 7); g.DrawString("TOTAL", fB, Brushes.White, 680, y + 7); y += 40;
            foreach (var item in _selectedTransaction.Items) { g.DrawString(item.Description.Length > 30 ? item.Description.Substring(0, 30) + "..." : item.Description, fN, Brushes.Black, 60, y); g.DrawString(item.SerialNumber, fN, Brushes.Black, 350, y); g.DrawString(item.Quantity.ToString(), fN, Brushes.Black, 500, y); g.DrawString(item.UnitPrice.ToString("N2"), fN, Brushes.Black, 560, y); g.DrawString(item.TotalAmount.ToString("N2"), fN, Brushes.Black, 680, y); y += 30; }
            g.DrawLine(Pens.Gray, 50, y, 770, y); y += 20;
            g.DrawString("Subtotal:", fN, Brushes.DimGray, 560, y); g.DrawString(_selectedTransaction.SubTotal.ToString("C2"), fN, Brushes.Black, 680, y); y += 25;
            g.DrawString("Discount:", fN, Brushes.DimGray, 560, y); g.DrawString($"-{_selectedTransaction.Discount:C2}", fN, Brushes.Red, 680, y); y += 25;
            g.DrawString("Tax:", fN, Brushes.DimGray, 560, y); g.DrawString($"{_selectedTransaction.Tax:C2}", fN, Brushes.Black, 680, y); y += 25;
            g.DrawString("GRAND TOTAL:", fSub, Brushes.DarkBlue, 520, y); g.DrawString(_selectedTransaction.GrandTotal.ToString("C2"), fSub, Brushes.Black, 680, y);
            y = 1000; g.DrawLine(Pens.Black, 50, y, 250, y); g.DrawString("Authorized Signature", fN, Brushes.DimGray, 90, y + 10);
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

            foreach (DarkComboBox cmb in _comboInputs) { cmb.BackColor = UITheme.CurrentInputBg; cmb.ForeColor = UITheme.CurrentText; }
            foreach (DarkComboBox cmb in _comboFilterInputs) { cmb.BackColor = UITheme.CurrentPanel; cmb.ForeColor = UITheme.CurrentText; }
            foreach (RoundedPanel wrap in _inputWrappers) { wrap.BackColor = UITheme.CurrentInputBg; wrap.BorderColor = UITheme.CurrentBorder; foreach (Control c in wrap.Controls) { if (c is IconPictureBox icon) icon.IconColor = UITheme.CurrentIcon; } }
            foreach (RoundedPanel wrap in _borderedContainers) { wrap.BackColor = UITheme.CurrentPanel; wrap.BorderColor = UITheme.CurrentBorder; }
            foreach (TextBox txt in _textInputs) { txt.BackColor = UITheme.CurrentInputBg; txt.ForeColor = UITheme.CurrentText; }
            foreach (Control c in _dynamicTexts) { c.ForeColor = UITheme.CurrentText; }
            foreach (Control c in _mutedTexts) { c.ForeColor = UITheme.MutedText; }

            foreach (IconButton btn in _buttons)
            {
                string tag = btn.Tag?.ToString() ?? "";
                if (tag == "ActionAdd") { btn.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btn.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : UITheme.SecondaryDark; }
                else if (tag == "Danger") { btn.BackColor = Color.FromArgb(25, 239, 68, 68); btn.ForeColor = Color.FromArgb(239, 68, 68); btn.FlatAppearance.BorderColor = Color.FromArgb(239, 68, 68); btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 68, 68); }
                else if (tag == "Success") { btn.BackColor = Color.FromArgb(25, 16, 185, 129); btn.ForeColor = Color.FromArgb(16, 185, 129); btn.FlatAppearance.BorderColor = Color.FromArgb(16, 185, 129); btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 185, 129); }
                else if (tag == "Secondary") { btn.BackColor = Color.Transparent; btn.ForeColor = UITheme.CurrentText; btn.FlatAppearance.BorderSize = 1; btn.FlatAppearance.BorderColor = UITheme.CurrentBorder; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230); }
                else { btn.BackColor = UITheme.CurrentPanel; btn.ForeColor = UITheme.CurrentText; btn.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(45, 42, 50) : Color.FromArgb(230, 230, 230); }
                btn.IconColor = btn.ForeColor; btn.FlatAppearance.MouseDownBackColor = btn.BackColor;
            }

            // Attachment button theming (both profile and create)
            if (btnAttach != null) { btnAttach.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btnAttach.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; }
            if (btnAttachCreate != null) { btnAttachCreate.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btnAttachCreate.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; }

            if (dgvSalesList != null)
            {
                dgvSalesList.BackgroundColor = UITheme.CurrentPanel; dgvSalesList.GridColor = UITheme.CurrentBorder;
                dgvSalesList.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
                dgvSalesList.DefaultCellStyle.BackColor = UITheme.CurrentPanel; dgvSalesList.DefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvSalesList.DefaultCellStyle.SelectionBackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(220, 230, 240);
                dgvSalesList.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;
                dgvSalesList.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
                dgvSalesList.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
            }
            if (dgvProfileItems != null)
            {
                dgvProfileItems.BackgroundColor = UITheme.CurrentInputBg; dgvProfileItems.GridColor = UITheme.CurrentBorder;
                dgvProfileItems.DefaultCellStyle.BackColor = UITheme.CurrentInputBg; dgvProfileItems.DefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvProfileItems.DefaultCellStyle.SelectionBackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(220, 230, 240);
                dgvProfileItems.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;
                dgvProfileItems.ColumnHeadersDefaultCellStyle.BackColor = UITheme.CurrentInputBg;
                dgvProfileItems.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
            }
            if (profileStepper != null) profileStepper.Invalidate();
            if (dtpOrderDate != null) dtpOrderDate.ApplyTheme(UITheme.IsDarkMode);
            this.Invalidate(true);
        }
    }
}