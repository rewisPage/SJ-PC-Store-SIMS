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
    internal static class WinApi
    {
        [System.Runtime.InteropServices.DllImport("uxtheme.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string appName, string idList);
    }

    public class ProcurementView : System.Windows.Forms.UserControl
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
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.Width - 1, this.Height - 1);
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
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, this.Width, this.Height); TextRenderer.DrawText(e.Graphics, this.Text, this.Font, rect, this.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
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
                        System.Drawing.Rectangle arrowRect = new System.Drawing.Rectangle(this.Width - 20, 0, 20, this.Height); using (SolidBrush b = new SolidBrush(this.BackColor)) { g.FillRectangle(b, arrowRect); }
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
                string[] labels = { "Draft Created", "Pending Approval", "Ordered (Sent)", "Goods Received" };
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
                    System.Drawing.Rectangle textRect = new System.Drawing.Rectangle(cx - 16, y - 15, 32, 32);
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

            public new Color BackColor
            {
                get => _backColor;
                set
                {
                    _backColor = value;
                    this.Invalidate();
                }
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                // Remove visual style so that BackColor works
                WinApi.SetWindowTheme(this.Handle, "", "");
            }
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
                this.Size = new Size(180, 38);
                this.Padding = new Padding(0);
                this.BackColor = UITheme.CurrentInputBg;

                txtDate = new TextBox
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Segoe UI", 11F),
                    Text = _selectedDate.ToString("MM/dd/yyyy"),
                    ReadOnly = true,
                    BackColor = this.BackColor,
                    ForeColor = UITheme.CurrentText
                };
                txtDate.Click += (s, e) => ToggleCalendar();

                btnDrop = new Button
                {
                    Text = "▼",
                    Dock = DockStyle.Right,
                    Width = 24,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8F),
                    BackColor = this.BackColor,
                    ForeColor = UITheme.CurrentText,
                    Cursor = Cursors.Hand
                };
                btnDrop.FlatAppearance.BorderSize = 0;
                btnDrop.Click += (s, e) => ToggleCalendar();
                btnDrop.MouseEnter += (s, e) => { btnDrop.BackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(200, 200, 200); };
                btnDrop.MouseLeave += (s, e) => { btnDrop.BackColor = this.BackColor; };

                this.Controls.Add(txtDate);
                this.Controls.Add(btnDrop);
                txtDate.BringToFront();
            }

            private void ToggleCalendar()
            {
                if (popup == null || popup.IsDisposed)
                {
                    popup = new Form
                    {
                        FormBorderStyle = FormBorderStyle.None,
                        StartPosition = FormStartPosition.Manual,
                        ShowInTaskbar = false,
                        TopMost = true,
                        BackColor = UITheme.CurrentInputBg,
                        Padding = new Padding(0)
                    };

                    monthCal = new ThemedMonthCalendar
                    {
                        MaxSelectionCount = 1,
                        BoldedDates = new DateTime[] { DateTime.Today },
                        BackColor = UITheme.CurrentInputBg,
                        ForeColor = UITheme.CurrentText,
                        TitleBackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234),
                        TitleForeColor = UITheme.CurrentText,
                        TrailingForeColor = UITheme.MutedText
                    };
                    monthCal.DateSelected += (s, ev) =>
                    {
                        Value = monthCal.SelectionStart;
                        popup.Close();
                    };

                    popup.Controls.Add(monthCal);
                    popup.Deactivate += (s, e) => popup.Close();

                    Size calSize = monthCal.GetPreferredSize(Size.Empty);
                    popup.ClientSize = new Size(calSize.Width + 40, calSize.Height + 8);
                }

                // Position the popup, keeping it inside the screen bounds
                Point screenLoc = this.PointToScreen(new Point(0, this.Height));
                Rectangle workingArea = Screen.FromControl(this).WorkingArea;
                if (screenLoc.Y + popup.Height > workingArea.Bottom)
                    screenLoc.Y -= this.Height + popup.Height; // show above instead
                if (screenLoc.X + popup.Width > workingArea.Right)
                    screenLoc.X = workingArea.Right - popup.Width;
                popup.Location = screenLoc;
                popup.Show();
                monthCal.Focus();
            }

            public void ApplyTheme(bool darkMode)
            {
                isDarkMode = darkMode;
                Color bg = UITheme.CurrentInputBg;
                this.BackColor = bg;
                txtDate.BackColor = bg;
                txtDate.ForeColor = UITheme.CurrentText;
                btnDrop.BackColor = bg;
                btnDrop.ForeColor = UITheme.CurrentText;

                if (popup != null && !popup.IsDisposed)
                {
                    popup.BackColor = bg;
                    if (monthCal != null)
                    {
                        monthCal.BackColor = bg;
                        monthCal.ForeColor = UITheme.CurrentText;
                        monthCal.TitleBackColor = darkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
                        monthCal.TitleForeColor = UITheme.CurrentText;
                        monthCal.TrailingForeColor = UITheme.MutedText;
                        monthCal.Invalidate();
                    }
                }
            }
        }

        // =========================================================================
        // VARIABLES
        // =========================================================================
        private ProcurementController _procController;
        private DataManagementController _supplierController;
        private InventoryController _inventoryController;
        private string _activeUserId;
        private ProcurementModel _selectedPO;
        private bool _isEditMode = false;

        private SmoothPanel pnlLanding, pnlProfile, pnlCreate, pnlDetailHeader;
        private DarkComboBox cmbFilter;
        private TextBox txtSearch;
        private SmoothGrid dgvPOList, dgvProfileItems;
        private StatusStepper profileStepper;

        private List<Control> _dynamicTexts = new List<Control>();
        private List<Control> _mutedTexts = new List<Control>();
        private List<RoundedPanel> _inputWrappers = new List<RoundedPanel>();
        private List<RoundedPanel> _borderedContainers = new List<RoundedPanel>();
        private List<TextBox> _textInputs = new List<TextBox>();
        private List<DarkComboBox> _comboInputs = new List<DarkComboBox>();
        private List<DarkComboBox> _comboFilterInputs = new List<DarkComboBox>(); //ADDED specifically for cmbFilter to achieve the correct theming without affecting the main combo inputs in the Create/Edit form. KEEP THIS SEPARATE FROM _comboInputs.
        private List<IconButton> _buttons = new List<IconButton>();

        // Profile Controls
        private Label lblDetTitle, lblDetSupplier, lblDetSupAddress, lblDetContact;
        private Label lblDetDates, lblDetRemarks, lblDetAuditCreated, lblDetAuditApproved;
        private Label lblTotalSub, lblTotalDisc, lblTotalTax, lblTotalGrand;
        private BadgeLabel badgeStatus;
        private IconButton btnApprove, btnCancel, btnGoodsReceipt, btnPDF, btnEdit, btnAddRow;
        private FlowLayoutPanel flpLeftDetails;
        private Panel pnlRightItems;
        private RoundedPanel cardSupplier, cardSchedule, cardAttach, cardAudit;

        // Create/Edit Controls
        private Label lblCreateTitle, lblCreateSub, lblCreateGrand;

        private ThemedDatePicker dtpOrderDate, dtpExpectedDate;
        private DarkComboBox cmbCreateSupplier, cmbCreateDiscountType, cmbCreateTaxType;
        private TextBox txtCreateRemarks, txtCreateDiscount, txtCreateTax;
        private BufferedFlowLayoutPanel flpCreateItems;

        private List<ProcurementModel> _allPOs = new List<ProcurementModel>();
        private List<SupplierModel> _dbSuppliers = new List<SupplierModel>();
        private List<ItemMasterModel> _dbItems = new List<ItemMasterModel>();
        private TableLayoutPanel tlpItemH;

        // ATTACHMENTS
        private AttachmentController _attachController;
        private FlowLayoutPanel flpAttachments;  // inside cardAttach
        private Button btnAttach;

        public ProcurementView(string currentUserId)
        {
            _activeUserId = currentUserId;
            _procController = new ProcurementController();
            _supplierController = new DataManagementController();
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
            _procController.LogActivity(_activeUserId, $"{title} - {message}", "Procurement");
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
            toast.Show(); t.Start();
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
            if (view == "Profile" && _selectedPO != null) RenderProfile();
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
            _borderedContainers.Add(pnlGridContainer);

            SmoothPanel toolbar = new SmoothPanel { Dock = DockStyle.Top, Height = 80, BackColor = Color.Transparent, Padding = new Padding(25, 20, 25, 20) };
            toolbar.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 79, toolbar.Width, 79); } };

            cmbFilter = new DarkComboBox { Location = new Point(25, 20), Size = new Size(200, 38), Font = new Font("Segoe UI", 14F, FontStyle.Bold), Cursor = Cursors.Hand }; // ADJUSTED THE FONT SIZE TO 14F. DO NOT TOUCH IT.
            cmbFilter.Items.AddRange(new[] { "All Purchase Orders", "Draft", "Pending Approval", "Ordered", "Completed", "Cancelled" });
            cmbFilter.SelectedIndex = 0; cmbFilter.SelectedIndexChanged += (s, e) => FilterGrid();
            _comboFilterInputs.Add(cmbFilter); //used specifically for cmbFilter to achieve the correct theming without affecting the main combo inputs in the Create/Edit form. KEEP THIS SEPARATE FROM _comboInputs.

            Control searchWrapper = CreateSearchInput("Search PO Number...", 300, out txtSearch, () => { txtSearch.Text = "Search PO Number..."; FilterGrid(); });
            searchWrapper.Location = new Point(240, 20);
            txtSearch.TextChanged += (s, e) => { if (txtSearch.Text != "Search PO Number...") FilterGrid(); };

            IconButton btnCreate = new IconButton { Text = " Create Purchase Order", IconChar = IconChar.Plus, IconSize = 16, Size = new Size(220, 38), Dock = DockStyle.Right, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "ActionAdd", Font = new Font("Segoe UI", 10F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 0, 15, 0) };
            btnCreate.FlatAppearance.BorderSize = 0;
            btnCreate.Click += (s, e) => { _isEditMode = false; PrepareCreateForm(); SwitchView("Create"); };
            _buttons.Add(btnCreate);

            toolbar.Controls.AddRange(new Control[] { cmbFilter, searchWrapper, btnCreate });

            dgvPOList = new SmoothGrid
            {
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                EnableHeadersVisualStyles = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                AutoSizeColumnsMode =
                DataGridViewAutoSizeColumnsMode.Fill,
                ColumnHeadersHeight = 60,
                RowTemplate = { Height = 60 },
                Cursor = Cursors.Hand
            };


            dgvPOList.Columns.Add("PO", "PO NUMBER"); dgvPOList.Columns.Add("Sup", "SUPPLIER NAME"); dgvPOList.Columns.Add("Date", "ORDER DATE"); dgvPOList.Columns.Add("Total", "TOTAL AMOUNT"); dgvPOList.Columns.Add("Status", "STATUS");

            dgvPOList.Columns["PO"].DefaultCellStyle.Font = new Font("Consolas", 11F, FontStyle.Bold);
            dgvPOList.Columns["PO"].DefaultCellStyle.ForeColor = Color.FromArgb(59, 130, 246);
            dgvPOList.Columns["Total"].DefaultCellStyle.Format = "C2";
            dgvPOList.Columns["Total"].DefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvPOList.Columns.Add("Action", "");

            // ADD THESE LINES to style the arrow column
            dgvPOList.Columns["Action"].Width = 50;
            dgvPOList.Columns["Action"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Reduced to 26F so it physically fits inside the 60px row height without baseline clipping
            dgvPOList.Columns["Action"].DefaultCellStyle.Font = new Font("Segoe UI", 26F, FontStyle.Bold);
            dgvPOList.Columns["Action"].DefaultCellStyle.ForeColor = UITheme.MutedText;

            // Explicit Padding: (Left, Top, Right, Bottom). 
            // This pushes the arrow 20px away from the right edge, and 5px up from the bottom to perfectly center it.
            dgvPOList.Columns["Action"].DefaultCellStyle.Padding = new Padding(0, 0, 20, 5);

            dgvPOList.DefaultCellStyle.Padding = new Padding(25, 0, 0, 0);
            dgvPOList.ColumnHeadersDefaultCellStyle.Padding = new Padding(25, 0, 0, 0);

            // Explicitly force all advanced borders to None to guarantee no lines render
            dgvPOList.AdvancedCellBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;

            dgvPOList.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CurrentBorder, 0)) { e.Graphics.DrawLine(p, 0, 59, dgvPOList.Width, 59); } // Header line
                if (dgvPOList.Rows.Count == 0) // Empty state text
                {
                    TextRenderer.DrawText(e.Graphics, "No Purchase Orders found in the database.", new Font("Segoe UI", 12F, FontStyle.Italic), dgvPOList.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            dgvPOList.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            dgvPOList.Paint += (s, e) =>
            {
                using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 59, dgvPOList.Width, 59); }
                if (dgvPOList.Rows.Count == 0)
                {
                    TextRenderer.DrawText(e.Graphics, "No Purchase Orders found in the database.", new Font("Segoe UI", 12F, FontStyle.Italic), dgvPOList.ClientRectangle, UITheme.MutedText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            };

            // Dynamically colors the text in the Status column without breaking grid borders
            dgvPOList.CellFormatting += (s, e) =>
            {
                // Check if we are in a valid row and looking at the "Status" column
                if (e.RowIndex >= 0 && e.ColumnIndex == dgvPOList.Columns["Status"].Index && e.Value != null)
                {
                    string stat = e.Value.ToString();
                    Color fgColor;

                    // Assign the semantic font colors
                    if (stat == "Draft") fgColor = Color.FromArgb(160, 170, 178); // Gray
                    else if (stat == "Pending Approval") fgColor = Color.FromArgb(245, 158, 11); // Orange
                    else if (stat == "Ordered") fgColor = Color.FromArgb(59, 130, 246); // Blue
                    else if (stat == "Completed" || stat == "Received") fgColor = Color.FromArgb(16, 185, 129); // Green
                    else fgColor = Color.FromArgb(239, 68, 68); // Red for Cancelled

                    // Apply the color and font directly to the cell style
                    e.CellStyle.ForeColor = fgColor;
                    e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

                    // Capitalize the text
                    e.Value = stat.ToUpper();

                    // Tell WinForms we successfully formatted the cell
                    e.FormattingApplied = true;
                }
            };

            dgvPOList.CellMouseClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    string id = dgvPOList.Rows[e.RowIndex].Cells[0].Value.ToString();
                    _selectedPO = _allPOs.FirstOrDefault(p => p.PO_Number == id);
                    SwitchView("Profile");
                }
            };



            pnlGridContainer.Controls.Add(dgvPOList);

            pnlToolbar.Controls.Add(toolbar);


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

            btnApprove = new IconButton { Text = " Approve", IconChar = IconChar.CheckDouble, IconSize = 16, Size = new Size(110, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Success", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(10, 0, 0, 0) };
            btnApprove.Click += (s, e) => OpenModal("ApprovePO");
            btnApprove.MouseEnter += (s, e) => { btnApprove.ForeColor = Color.White; btnApprove.IconColor = Color.White; };
            btnApprove.MouseLeave += (s, e) => { btnApprove.ForeColor = Color.FromArgb(16, 185, 129); btnApprove.IconColor = Color.FromArgb(16, 185, 129); };

            btnCancel = new IconButton { Text = " Cancel", IconChar = IconChar.TimesCircle, IconSize = 16, Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Danger", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(10, 0, 0, 0) };
            btnCancel.Click += (s, e) => OpenModal("CancelPO");
            btnCancel.MouseEnter += (s, e) => { btnCancel.ForeColor = Color.White; btnCancel.IconColor = Color.White; };
            btnCancel.MouseLeave += (s, e) => { btnCancel.ForeColor = Color.FromArgb(239, 68, 68); btnCancel.IconColor = Color.FromArgb(239, 68, 68); };

            btnEdit = new IconButton { Text = " Edit", IconChar = IconChar.Pen, IconSize = 16, Size = new Size(80, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Secondary", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(10, 0, 0, 0) };
            btnEdit.Click += (s, e) => { _isEditMode = true; PrepareCreateForm(); SwitchView("Create"); };

            btnPDF = new IconButton { Text = " Generate PDF", IconChar = IconChar.FilePdf, IconSize = 16, Size = new Size(140, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Secondary", Font = new Font("Segoe UI", 9F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 10, 0), Margin = new Padding(0) };
            btnPDF.MouseEnter += (s, e) => btnPDF.IconColor = Color.FromArgb(239, 68, 68); btnPDF.MouseLeave += (s, e) => btnPDF.IconColor = UITheme.CurrentText;
            btnPDF.Click += (s, e) => GeneratePDF();

            flpRight.Controls.AddRange(new Control[] { btnApprove, btnCancel, btnEdit, btnPDF });
            _buttons.AddRange(new[] { btnEdit, btnPDF }); // Custom hover handled separately for approve/cancel
            _dynamicTexts.Add(lblDetTitle);

            pnlDetailHeader.Controls.Add(flpRight); pnlDetailHeader.Controls.Add(flpLeft);

            profileStepper = new StatusStepper { Dock = DockStyle.Top, Height = 100 };

            Panel pnlBody = new Panel { Dock = DockStyle.Fill };

            flpLeftDetails = new FlowLayoutPanel { Dock = DockStyle.Left, Width = 380, AutoScroll = true, Padding = new Padding(25, 20, 10, 20), FlowDirection = FlowDirection.TopDown, WrapContents = false };
            flpLeftDetails.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 379, 0, 379, flpLeftDetails.Height); } };

            pnlRightItems = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(30) };

            Func<string, RoundedPanel> CreateInfoSection = (title) =>
            {
                Label lHead = new Label { Text = title.ToUpper(), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = UITheme.MutedText, AutoSize = true, Margin = new Padding(0, 0, 0, 10) };
                flpLeftDetails.Controls.Add(lHead); _mutedTexts.Add(lHead);
                RoundedPanel p = new RoundedPanel { Width = 320, Height = 100, BorderRadius = 6, BorderSize = 1, Padding = new Padding(20), Margin = new Padding(0, 0, 0, 25) };
                p.AutoSize = false; // Explicitly disable full AutoSize
                p.AutoSizeMode = AutoSizeMode.GrowAndShrink; // Allow it to grow/shrink
                _borderedContainers.Add(p); flpLeftDetails.Controls.Add(p); return p;
            };

            cardSupplier = CreateInfoSection("Supplier Details");
            lblDetSupplier = new Label { Font = new Font("Segoe UI", 11F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 20), ForeColor = Color.FromArgb(59, 130, 246), Cursor = Cursors.Hand };
            lblDetSupAddress = new Label { Font = new Font("Segoe UI", 9.5F), AutoSize = true, Location = new Point(20, 50), MaximumSize = new Size(280, 0) };
            lblDetContact = new Label { Font = new Font("Segoe UI", 9.5F), AutoSize = true, Location = new Point(20, 90) };
            cardSupplier.Controls.AddRange(new Control[] { lblDetSupplier, lblDetSupAddress, lblDetContact });
            cardSupplier.SizeChanged += (s, e) => AdjustCardHeight(cardSupplier);

            cardSchedule = CreateInfoSection("Schedule & Notes");
            lblDetDates = new Label { Font = new Font("Segoe UI", 9.5F), AutoSize = true, Location = new Point(20, 20) };
            lblDetRemarks = new Label { Font = new Font("Segoe UI", 9.5F, FontStyle.Italic), AutoSize = true, MaximumSize = new Size(280, 0), Location = new Point(20, 70) };
            cardSchedule.Controls.AddRange(new Control[] { lblDetDates, lblDetRemarks });
            cardSchedule.SizeChanged += (s, e) => AdjustCardHeight(cardSchedule);

            cardAttach = CreateInfoSection("Attachments");
            btnAttach = new Button
            {
                Text = "📝 Attach File",
                Dock = DockStyle.Top,
                Size = new Size(110, 35),
                Location = new Point(20, 20),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark,
                ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White
            };
            btnAttach.FlatAppearance.BorderSize = 0;
            btnAttach.Click += (s, e) =>
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Multiselect = true;
                    ofd.Filter = "All Files|*.*|Documents|*.pdf;*.docx;*.xlsx|Images|*.jpg;*.png;*.bmp";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        foreach (string file in ofd.FileNames)
                        {
                            _attachController.UploadAttachment(_selectedPO.PO_Number, file, _activeUserId);
                        }
                        LogAndNotify("Attachment", $"{ofd.FileNames.Length} file(s) attached.", true);
                        LoadAttachments(); // refresh list
                    }
                }
            };
            btnAttach.MouseEnter += (s, e) => { btnAttach.BackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : UITheme.SecondaryDark; };
            btnAttach.MouseLeave += (s, e) => { btnAttach.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; };
            cardAttach.Controls.Add(btnAttach);

            // Drag-and-drop support for the card
            cardAttach.AllowDrop = true;
            cardAttach.DragEnter += (s, e) => { if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy; };
            cardAttach.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    _attachController.UploadAttachment(_selectedPO.PO_Number, file, _activeUserId);
                }
                LogAndNotify("Attachment", $"{files.Length} file(s) attached via drag & drop.", true);
                LoadAttachments();
            };

            // Panel that will hold the attachment list
            flpAttachments = new FlowLayoutPanel
            {
                Location = new Point(20, 65),
                Size = new Size(280, 150),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            cardAttach.Controls.Add(flpAttachments);
            cardAttach.SizeChanged += (s, e) => AdjustCardHeight(cardAttach);

            cardAudit = CreateInfoSection("Audit Trail");
            lblDetAuditCreated = new Label { Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(20, 20) };
            lblDetAuditApproved = new Label { Font = new Font("Segoe UI", 9F), AutoSize = true, Location = new Point(20, 60) };
            cardAudit.Controls.AddRange(new Control[] { lblDetAuditCreated, lblDetAuditApproved });
            cardAudit.SizeChanged += (s, e) => AdjustCardHeight(cardAudit);

            _mutedTexts.AddRange(new[] { lblDetSupAddress, lblDetContact, lblDetDates, lblDetRemarks, lblDetAuditCreated, lblDetAuditApproved });

            // RIGHT ITEMS SECTION
            Panel pnlRightHeader = new Panel { Dock = DockStyle.Top, Height = 60 };
            Label lItemsTitle = new Label { Text = "Ordered Items", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 10) };
            _dynamicTexts.Add(lItemsTitle);

            btnGoodsReceipt = new IconButton { Text = " Process Goods Receipt", IconChar = IconChar.BoxOpen, IconSize = 16, Size = new Size(220, 38), Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(pnlRightHeader.Width - 220, 10), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "ActionAdd", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 0, 15, 0) };
            btnGoodsReceipt.FlatAppearance.BorderSize = 0; btnGoodsReceipt.Click += (s, e) => OpenModal("GoodsReceipt"); _buttons.Add(btnGoodsReceipt);
            pnlRightHeader.Controls.AddRange(new Control[] { btnGoodsReceipt, lItemsTitle });

            dgvProfileItems = new SmoothGrid { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None, EnableHeadersVisualStyles = false, AllowUserToAddRows = false, ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, RowHeadersVisible = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, ColumnHeadersHeight = 50, RowTemplate = { Height = 45 }, Margin = new Padding(0, 20, 0, 0) };
            dgvProfileItems.Columns.Add("Code", "ITEM CODE"); dgvProfileItems.Columns.Add("Desc", "ITEM NAME"); dgvProfileItems.Columns.Add("Cond", "CONDITION"); dgvProfileItems.Columns.Add("Qty", "QTY"); dgvProfileItems.Columns.Add("Price", "UNIT PRICE"); dgvProfileItems.Columns.Add("Total", "TOTAL");
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
            lblTotalGrand = new Label { Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(20, 115) };

            _dynamicTexts.AddRange(new[] { lblTotalSub, lblTotalGrand, lblTotalTax });

            _borderedContainers.Add(pnlTotals);

            pnlTotals.Controls.AddRange(new Control[] { lblTotalSub, lblTotalDisc, lblTotalTax, lblTotalGrand });

            pnlTotalsWrapper.Controls.Add(pnlTotals);

            pnlRightItems.Controls.Add(dgvProfileItems); pnlRightItems.Controls.Add(pnlTotalsWrapper); pnlRightItems.Controls.Add(pnlRightHeader);

            pnlBody.Controls.Add(pnlRightItems); pnlBody.Controls.Add(flpLeftDetails);
            container.Controls.Add(pnlBody); container.Controls.Add(profileStepper); container.Controls.Add(pnlDetailHeader);
            pnlProfile.Controls.Add(container);

            pnlRightHeader.Resize += (s, e) => { btnGoodsReceipt.Location = new Point(pnlRightHeader.Width - 220, 10); };
        }

        private void RenderProfile()
        {
            lblDetTitle.Text = _selectedPO.PO_Number;
            badgeStatus.Text = _selectedPO.Status;

            profileStepper.IsCancelled = _selectedPO.Status == "Cancelled";
            if (_selectedPO.Status == "Draft") profileStepper.CurrentStep = 1;
            else if (_selectedPO.Status == "Pending Approval") profileStepper.CurrentStep = 2;
            else if (_selectedPO.Status == "Ordered") profileStepper.CurrentStep = 3;
            else if (_selectedPO.Status == "Completed" || _selectedPO.Status == "Received") profileStepper.CurrentStep = 4;
            else profileStepper.CurrentStep = 0;
            profileStepper.Invalidate();

            if (_selectedPO.Status == "Draft") { badgeStatus.BgTint = Color.FromArgb(40, 160, 170, 178); badgeStatus.ForeColor = Color.FromArgb(160, 170, 178); }
            else if (_selectedPO.Status == "Pending Approval") { badgeStatus.BgTint = Color.FromArgb(40, 245, 158, 11); badgeStatus.ForeColor = Color.FromArgb(245, 158, 11); }
            else if (_selectedPO.Status == "Ordered") { badgeStatus.BgTint = Color.FromArgb(40, 59, 130, 246); badgeStatus.ForeColor = Color.FromArgb(59, 130, 246); }
            else if (_selectedPO.Status == "Completed") { badgeStatus.BgTint = Color.FromArgb(40, 16, 185, 129); badgeStatus.ForeColor = Color.FromArgb(16, 185, 129); }
            else { badgeStatus.BgTint = Color.FromArgb(40, 239, 68, 68); badgeStatus.ForeColor = Color.FromArgb(239, 68, 68); }
            badgeStatus.Invalidate();

            btnApprove.Visible = _selectedPO.Status == "Pending Approval";
            btnCancel.Visible = _selectedPO.Status == "Draft" || _selectedPO.Status == "Pending Approval";
            btnEdit.Visible = _selectedPO.Status == "Draft" || _selectedPO.Status == "Pending Approval";

            if (_selectedPO.Status != "Ordered")
            {
                btnGoodsReceipt.Enabled = false;
                btnGoodsReceipt.BackColor = UITheme.CurrentBorder;
                btnGoodsReceipt.ForeColor = UITheme.MutedText;
                btnGoodsReceipt.IconColor = UITheme.MutedText;
            }
            else
            {
                btnGoodsReceipt.Enabled = true;
                btnGoodsReceipt.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                btnGoodsReceipt.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White;
                btnGoodsReceipt.IconColor = btnGoodsReceipt.ForeColor;
            }

            var supplierInfo = _dbSuppliers.FirstOrDefault(s => s.SupplierID == _selectedPO.SupplierID);
            lblDetSupplier.Text = $"{_selectedPO.SupplierName}";
            lblDetSupAddress.Text = supplierInfo?.Address ?? "Address not found";
            lblDetContact.Text = $"Contact: {_selectedPO.ContactPerson}\nPhone: {_selectedPO.ContactNumber}";
            lblDetDates.Text = $"Order Date: {_selectedPO.OrderDate:MMM dd, yyyy}\nExpected Delivery: {_selectedPO.ExpectedDate:MMM dd, yyyy}";

            string remarksStr = string.IsNullOrEmpty(_selectedPO.Remarks) ? "N/A" : _selectedPO.Remarks;
            lblDetRemarks.Text = $"Remarks:\n{remarksStr}";

            string approvedStat = (_selectedPO.Status == "Draft" || _selectedPO.Status == "Pending Approval") ? "Pending" : _selectedPO.ApprovedBy;
            lblDetAuditCreated.Text = $"Created By:\n{_selectedPO.CreatedBy} ({_selectedPO.OrderDate:g})";
            lblDetAuditApproved.Text = $"Approved By:\n{approvedStat}";

            lblTotalSub.Text = $"Sub Total: {_selectedPO.SubTotal:C2}";
            lblTotalDisc.Text = $"Discount Applied: -{_selectedPO.Discount:C2}";
            lblTotalTax.Text = $"Tax Applied: {_selectedPO.Tax:C2}";
            lblTotalGrand.Text = $"GRAND TOTAL: {_selectedPO.GrandTotal:C2}";

            dgvProfileItems.Rows.Clear();

            foreach (var item in _selectedPO.Items)
            {
                var dbItem = _dbItems.FirstOrDefault(i => i.ItemCode == item.ItemCode);
                string cond = dbItem?.ItemCondition ?? "Brand New";
                dgvProfileItems.Rows.Add(item.ItemCode, item.Description, cond, item.Quantity, item.UnitPrice, item.TotalAmount);
            }

            // Adjust card heights after all content is loaded
            AdjustCardHeight(cardSupplier);
            AdjustCardHeight(cardSchedule);
            AdjustCardHeight(cardAttach);
            AdjustCardHeight(cardAudit);

            LoadAttachments();
        }

        private void AdjustCardHeight(RoundedPanel card)
        {
            if (card == null || card.Controls.Count == 0) return;

            card.SuspendLayout();
            int maxBottom = 0;

            // Calculate the lowest point of all child controls
            foreach (Control ctrl in card.Controls)
            {
                int controlBottom = ctrl.Location.Y + ctrl.Height;
                if (controlBottom > maxBottom)
                    maxBottom = controlBottom;
            }

            // Set the card height: max bottom + padding bottom + safety margin
            int newHeight = maxBottom + card.Padding.Bottom + 10;
            card.Height = newHeight;
            card.Width = 320; // Ensure width stays fixed at 320

            card.ResumeLayout(false);
        }

        // =========================================================================
        // CREATE / EDIT PO FORM
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
            lblCreateTitle = new Label { Text = "Create New Purchase Order", Font = new Font("Segoe UI", 18F, FontStyle.Bold), AutoSize = true, Margin = new Padding(0, 4, 0, 0) };
            flpLeft.Controls.AddRange(new Control[] { btnBack, lblCreateTitle });

            FlowLayoutPanel flpRight = new FlowLayoutPanel { Dock = DockStyle.Right, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };

            // ADDED: Name = "btnSubmitPO" to uniquely identify it away from the Landing Page button
            IconButton btnSubmit = new IconButton { Name = "btnSubmitPO", Text = " Submit for Approval", IconChar = IconChar.PaperPlane, IconSize = 16, Size = new Size(200, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "ActionAdd", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleCenter, ImageAlign = ContentAlignment.MiddleCenter, Padding = new Padding(15, 0, 15, 0), Margin = new Padding(10, 0, 0, 0) };

            IconButton btnSave = new IconButton { Text = " Save as Draft", IconChar = IconChar.Save, IconSize = 16, Size = new Size(150, 38), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = "Secondary", Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), TextImageRelation = TextImageRelation.ImageBeforeText, TextAlign = ContentAlignment.MiddleRight, ImageAlign = ContentAlignment.MiddleLeft, Padding = new Padding(15, 0, 15, 0), Margin = new Padding(0) };
            btnSave.Click += (s, e) => SavePO("Draft");
            btnSubmit.Click += (s, e) => OpenModal("SubmitPO");
            flpRight.Controls.AddRange(new Control[] { btnSubmit, btnSave });

            _buttons.AddRange(new[] { btnSave, btnSubmit }); _dynamicTexts.Add(lblCreateTitle);
            pnlCreateHeader.Controls.Add(flpRight); pnlCreateHeader.Controls.Add(flpLeft);

            Panel pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(30), AutoScroll = true };

            // ORDER DETAILS SECTION
            Label lHead1 = new Label { Text = "ORDER DETAILS", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(30, 25) }; // MANUALLY POSITIONED CLOSER TO THE TOP FOR BETTER AESTHETICS WITH THE BORDER BELOW
            _dynamicTexts.Add(lHead1); pnlBody.Controls.Add(lHead1);

            RoundedPanel pnlDetails = new RoundedPanel { Location = new Point(30, 65), Size = new Size(pnlBody.Width - 60, 260), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BorderRadius = 6, BorderSize = 1, Padding = new Padding(25) };

            Label l1 = new Label { Text = "Supplier / Vendor", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(25, 20) };
            cmbCreateSupplier = new DarkComboBox { Font = new Font("Segoe UI", 11F), Size = new Size(350, 30) };
            Control w1 = CreateInputWrapper(cmbCreateSupplier, 350); w1.Location = new Point(25, 45); _comboInputs.Add(cmbCreateSupplier);

            Label l2 = new Label { Text = "Order Date", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(400, 20) };
            dtpOrderDate = new ThemedDatePicker();
            Control w2 = CreateInputWrapper(dtpOrderDate, 180);
            w2.Location = new Point(400, 45);

            Label l3 = new Label { Text = "Expected Delivery", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(600, 20) };
            dtpExpectedDate = new ThemedDatePicker();
            Control w3 = CreateInputWrapper(dtpExpectedDate, 180);
            w3.Location = new Point(600, 45);

            Label l4 = new Label { Text = "Remarks / Terms", Font = new Font("Segoe UI", 9F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(25, 95) };
            txtCreateRemarks = new TextBox { Font = new Font("Segoe UI", 11F), Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None };
            Control wrapRemarks = CreateInputWrapper(txtCreateRemarks, 755); wrapRemarks.Location = new Point(25, 120); wrapRemarks.Height = 110; wrapRemarks.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _textInputs.Add(txtCreateRemarks);

            pnlDetails.Controls.AddRange(new Control[] { l1, w1, l2, w2, l3, w3, l4, wrapRemarks });
            _mutedTexts.AddRange(new[] { l1, l2, l3, l4 }); _borderedContainers.Add(pnlDetails);
            pnlBody.Controls.Add(pnlDetails);

            // ITEM SELECTION SECTION
            Label lHead2 = new Label { Text = "ITEM SELECTION", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(30, 350) }; // MANUALLY POSITIONED CLOSER TO THE TOP FOR BETTER AESTHETICS WITH THE BORDER BELOW
            _dynamicTexts.Add(lHead2); pnlBody.Controls.Add(lHead2);

            RoundedPanel pnlItems = new RoundedPanel { Location = new Point(30, 390), Size = new Size(pnlBody.Width - 60, 460), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BorderRadius = 6, BorderSize = 1, Padding = new Padding(25) };

            Panel pnlItemHeader = new Panel { Dock = DockStyle.Top, Height = 50 };
            btnAddRow = new IconButton
            {
                Text = " Add Row",
                IconChar = IconChar.Plus,
                IconSize = 14,
                Size = new Size(110, 35),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(pnlItems.Width - 160, 5),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Tag = "Secondary",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextImageRelation = TextImageRelation.ImageBeforeText,
                TextAlign = ContentAlignment.MiddleRight,
                ImageAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 10, 0)
            };
            btnAddRow.Click += (s, e) => AddCreateItemRow(); _buttons.Add(btnAddRow);
            pnlItemHeader.Controls.Add(btnAddRow);

            tlpItemH = new TableLayoutPanel { Dock = DockStyle.Top, Height = 40, ColumnCount = 6 };
            // --- Add a background color ---
            tlpItemH.BackColor = UITheme.IsDarkMode
                ? Color.FromArgb(34, 32, 38)   // dark header background
                : Color.FromArgb(226, 230, 234); // light header background
            // ------------------------------

            // EXACT PIXEL ALIGNMENT TABLE LAYOUT HEADER. MANUALLY ADJUSTED COLUMN WIDTHS TO PERFECTLY FIT THE ADD ROW BUTTON AND THE DELETE BUTTON IN THE ITEM ROWS. DO NOT CHANGE
            tlpItemH = new TableLayoutPanel { Dock = DockStyle.Top, Height = 40, ColumnCount = 7 };
            tlpItemH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); // item code
            tlpItemH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); // item name
            tlpItemH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // condition
            tlpItemH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F)); // qty
            tlpItemH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); // unit price
            tlpItemH.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F)); // total
            tlpItemH.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F)); // delete button column
            // tlpItemH.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 2)) { e.Graphics.DrawLine(p, 0, 39, tlpItemH.Width, 39); } }; - Removed the bottom border line for the header for better aesthetics since the header already has a distinct background color. DO NOT TOUCH THIS

            string[] hText = { "  ITEM CODE", "ITEM NAME", "CONDITION", "QTY", "UNIT PRICE (₱)", "TOTAL (₱)" };
            for (int i = 0; i < 6; i++)
            {
                Label hl = new Label { Text = hText[i], Font = new Font("Segoe UI", 8.5F, FontStyle.Bold), ForeColor = UITheme.CurrentText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }; // CHANGED Text Alignment to Middle left and Removed Bottom Padding 10 for centered looking column header text. DO NOT TOUCH THIS
                tlpItemH.Controls.Add(hl, i, 0); _dynamicTexts.Add(hl);
            }

            flpCreateItems = new BufferedFlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(0, 15, 0, 0) };

            Panel pnlCreateTotalsWrapper = new Panel { Dock = DockStyle.Bottom, Height = 200, Padding = new Padding(0, 20, 0, 0) };
            Panel pnlCreateTotalsInner = new Panel { Dock = DockStyle.Right, Width = 350 };

            Label l5 = new Label { Text = "Discount Applied:", Font = new Font("Segoe UI", 9.5F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(0, 30) };
            cmbCreateDiscountType = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(50, 30) };
            cmbCreateDiscountType.Items.AddRange(new[] { "₱", "%" }); cmbCreateDiscountType.SelectedIndex = 0; cmbCreateDiscountType.SelectedIndexChanged += (s, e) => CalculateTotals();
            _comboInputs.Add(cmbCreateDiscountType);
            txtCreateDiscount = new TextBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(110, 30), Text = "0.00", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            txtCreateDiscount.TextChanged += (s, e) => CalculateTotals(); _textInputs.Add(txtCreateDiscount);

            Control wType = CreateInputWrapper(cmbCreateDiscountType, 60); wType.Location = new Point(140, 25); wType.Margin = new Padding(0);
            Control wDisc = CreateInputWrapper(txtCreateDiscount, 140); wDisc.Location = new Point(210, 25); wDisc.Margin = new Padding(0);

            Label lTax = new Label { Text = "Tax Applied:", Font = new Font("Segoe UI", 9.5F), ForeColor = UITheme.MutedText, AutoSize = true, Location = new Point(0, 70) };
            cmbCreateTaxType = new DarkComboBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(50, 30) };
            cmbCreateTaxType.Items.AddRange(new[] { "₱", "%" });
            cmbCreateTaxType.SelectedIndex = 1; // Default to %
            cmbCreateTaxType.SelectedIndexChanged += (s, e) => CalculateTotals();
            _comboInputs.Add(cmbCreateTaxType);

            txtCreateTax = new TextBox { Font = new Font("Segoe UI", 10.5F), Size = new Size(110, 30), Text = "0.00", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            txtCreateTax.TextChanged += (s, e) => CalculateTotals();
            _textInputs.Add(txtCreateTax);

            Control wTaxType = CreateInputWrapper(cmbCreateTaxType, 60); wTaxType.Location = new Point(140, 65); wTaxType.Margin = new Padding(0);
            Control wTaxVal = CreateInputWrapper(txtCreateTax, 140); wTaxVal.Location = new Point(210, 65); wTaxVal.Margin = new Padding(0);

            lblCreateSub = new Label { Text = "Subtotal: ₱ 0.00", Font = new Font("Segoe UI", 10F), AutoSize = true, Location = new Point(0, 115) };
            lblCreateGrand = new Label { Text = "GRAND TOTAL: ₱ 0.00", Font = new Font("Segoe UI", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(0, 145), ForeColor = UITheme.PrimaryDark };

            pnlCreateTotalsInner.Controls.AddRange(new Control[] { l5, wType, wDisc, lTax, wTaxType, wTaxVal, lblCreateSub, lblCreateGrand });
            pnlCreateTotalsWrapper.Controls.Add(pnlCreateTotalsInner);
            _borderedContainers.Add(pnlItems); _mutedTexts.Add(l5); _dynamicTexts.AddRange(new[] { lblCreateSub, lblCreateGrand });

            pnlItems.Controls.Add(flpCreateItems); pnlItems.Controls.Add(pnlCreateTotalsWrapper); pnlItems.Controls.Add(tlpItemH); pnlItems.Controls.Add(pnlItemHeader);
            pnlBody.Controls.Add(pnlItems);

            container.Controls.Add(pnlBody); container.Controls.Add(pnlCreateHeader);
            pnlCreate.Controls.Add(container);

            pnlItems.Resize += (s, e) => { btnAddRow.Location = new Point(pnlItems.Width - 160, 5); };
            pnlBody.Resize += (s, e) =>
            {
                pnlDetails.Width = pnlBody.Width - 60;
                pnlItems.Width = pnlBody.Width - 60;
                wrapRemarks.Width = pnlDetails.Width - 50;
            };
        }

        private void AddCreateItemRow(ProcurementItemModel existingItem = null)
        {
            Panel row = new Panel { Width = flpCreateItems.Width - 25, Height = 45, Margin = new Padding(0, 0, 0, 10) };
            TableLayoutPanel tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7 };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F)); tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F)); tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10F)); tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F)); tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 6F)); tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 4F));

            DarkComboBox cCode = new DarkComboBox { Font = new Font("Consolas", 10.5F), Dock = DockStyle.Fill };
            foreach (var item in _dbItems) cCode.Items.Add(item.ItemCode);
            Control wCode = CreateInputWrapper(cCode, 100); wCode.Dock = DockStyle.Fill; wCode.Margin = new Padding(0, 0, 10, 0); _comboInputs.Add(cCode);

            Label lName = new Label { Font = new Font("Segoe UI", 9.5F), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            Label lCond = new Label { Font = new Font("Segoe UI", 9.5F), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };

            TextBox tQty = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "1", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            Control wQty = CreateInputWrapper(tQty, 100); wQty.Dock = DockStyle.Fill; wQty.Margin = new Padding(0, 0, 10, 0); _textInputs.Add(tQty);

            TextBox tPrice = new TextBox { Font = new Font("Segoe UI", 10.5F), Text = "0.00", TextAlign = HorizontalAlignment.Right, BorderStyle = BorderStyle.None };
            Control wPrice = CreateInputWrapper(tPrice, 100); wPrice.Dock = DockStyle.Fill; wPrice.Margin = new Padding(0, 0, 10, 0); _textInputs.Add(tPrice);

            Label lTotal = new Label { Text = "0.00", Font = new Font("Segoe UI", 10.5F, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight };

            IconButton btnDel = new IconButton { IconChar = IconChar.Trash, IconSize = 18, FlatStyle = FlatStyle.Flat, Dock = DockStyle.Fill, Cursor = Cursors.Hand, BackColor = Color.Transparent };
            btnDel.FlatAppearance.BorderSize = 0; btnDel.ForeColor = Color.FromArgb(239, 68, 68); btnDel.IconColor = Color.FromArgb(239, 68, 68);
            btnDel.Click += (s, e) => { flpCreateItems.Controls.Remove(row); CalculateTotals(); };

            cCode.SelectedIndexChanged += (s, e) =>
            {
                var match = _dbItems.FirstOrDefault(m => m.ItemCode == cCode.Text);
                if (match != null) { lName.Text = $"{match.Category} {match.Specs}"; lCond.Text = match.ItemCondition; tPrice.Text = match.BaselineCost.ToString("0.00"); }
                CalculateTotals();
            };

            EventHandler calc = (s, e) =>
            {
                decimal q = 0, p = 0;
                decimal.TryParse(tQty.Text, out q); decimal.TryParse(tPrice.Text, out p);
                lTotal.Text = (q * p).ToString("0.00"); CalculateTotals();
            };
            tQty.TextChanged += calc; tPrice.TextChanged += calc;

            tlp.Controls.Add(wCode, 0, 0); tlp.Controls.Add(lName, 1, 0); tlp.Controls.Add(lCond, 2, 0); tlp.Controls.Add(wQty, 3, 0); tlp.Controls.Add(wPrice, 4, 0); tlp.Controls.Add(lTotal, 5, 0); tlp.Controls.Add(btnDel, 6, 0);
            row.Controls.Add(tlp); _dynamicTexts.AddRange(new[] { lName, lTotal }); _mutedTexts.Add(lCond);

            if (existingItem != null) { cCode.Text = existingItem.ItemCode; tQty.Text = existingItem.Quantity.ToString(); tPrice.Text = existingItem.UnitPrice.ToString("0.00"); }

            flpCreateItems.Controls.Add(row);
            row.Tag = new { Code = cCode, Qty = tQty, Price = tPrice, Name = lName };
            ApplyTheme();
        }

        private void PrepareCreateForm()
        {
            flpCreateItems.Controls.Clear();
            cmbCreateSupplier.Items.Clear();
            foreach (var s in _dbSuppliers) cmbCreateSupplier.Items.Add(s.SupplierID + " - " + s.CompanyName);

            if (_isEditMode && _selectedPO != null)
            {
                lblCreateTitle.Text = $"Edit Purchase Order ({_selectedPO.PO_Number})";

                // Target ONLY the exact Submit button using its unique Name, completely ignoring the Landing Page
                foreach (IconButton btn in _buttons)
                {
                    if (btn.Name == "btnSubmitPO")
                    {
                        btn.Text = " Update Order";
                        btn.Size = new Size(160, 38);
                    }
                }

                cmbCreateSupplier.Text = $"{_selectedPO.SupplierID} - {_selectedPO.SupplierName}";
                dtpOrderDate.Value = _selectedPO.OrderDate;
                dtpExpectedDate.Value = _selectedPO.ExpectedDate;
                txtCreateRemarks.Text = _selectedPO.Remarks;
                txtCreateDiscount.Text = _selectedPO.Discount.ToString("0.00");
                txtCreateTax.Text = _selectedPO.Tax.ToString("0.00");
                foreach (var item in _selectedPO.Items) AddCreateItemRow(item);
            }
            else
            {
                lblCreateTitle.Text = "Create New Purchase Order";

                // Reset button text for new creations
                foreach (IconButton btn in _buttons)
                {
                    if (btn.Name == "btnSubmitPO")
                    {
                        btn.Text = " Submit for Approval";
                        btn.Size = new Size(200, 38);
                    }
                }

                cmbCreateSupplier.SelectedIndex = -1; txtCreateRemarks.Text = ""; txtCreateDiscount.Text = "0.00"; txtCreateTax.Text = "0.00";
                dtpOrderDate.Value = DateTime.Now; dtpExpectedDate.Value = DateTime.Now.AddDays(7);
            }
            CalculateTotals();
            ApplyTheme();
        }

        private void CalculateTotals()
        {
            decimal sub = 0;
            // 1. Sum up all items
            foreach (Control row in flpCreateItems.Controls)
            {
                dynamic tag = row.Tag; if (tag == null) continue;
                decimal q = 0, p = 0;
                decimal.TryParse(((TextBox)tag.Qty).Text, out q);
                decimal.TryParse(((TextBox)tag.Price).Text, out p);
                sub += (q * p);
            }

            // 2. Subtract Discount
            decimal disc = 0;
            decimal.TryParse(txtCreateDiscount.Text, out disc);
            if (cmbCreateDiscountType.Text == "%") disc = sub * (disc / 100m);

            // 3. Add Tax (Calculated against the discounted subtotal)
            decimal tax = 0;
            if (txtCreateTax != null)
            {
                decimal.TryParse(txtCreateTax.Text, out tax);
                if (cmbCreateTaxType.Text == "%") tax = (sub - disc) * (tax / 100m);
            }

            decimal grand = sub - disc + tax;

            lblCreateSub.Text = $"Subtotal: {sub:C2}";
            lblCreateGrand.Text = $"GRAND TOTAL: {grand:C2}";
        }

        private void SavePO(string status)
        {
            CalculateTotals();
            if (cmbCreateSupplier.SelectedIndex == -1) { ShowToast("Please select a valid supplier from the list.", false); return; }

            string selectedText = cmbCreateSupplier.Text;

            // Extract the ID safely by looking for the space-hyphen-space (" - ") separator
            string supId = "";
            int separatorIndex = selectedText.IndexOf(" - ");
            if (separatorIndex > 0)
            {
                supId = selectedText.Substring(0, separatorIndex).Trim();
            }

            if (string.IsNullOrEmpty(supId)) { ShowToast("Invalid Supplier Selection.", false); return; }

            // If editing, keep the same PO Number. If creating new, generate a new one.
            string newPO = _isEditMode ? _selectedPO.PO_Number : _procController.GenerateNextPONumber();

            ProcurementModel po = new ProcurementModel
            {
                PO_Number = newPO,
                SupplierID = supId,
                SupplierName = selectedText,
                OrderDate = dtpOrderDate.Value,
                ExpectedDate = dtpExpectedDate.Value,
                Remarks = txtCreateRemarks.Text,
                Status = status, // Can be "Draft" or "Pending Approval"
                CreatedBy = _activeUserId,
                SubTotal = decimal.Parse(lblCreateSub.Text.Replace("Subtotal: ₱", "").Trim()),
                GrandTotal = decimal.Parse(lblCreateGrand.Text.Replace("GRAND TOTAL: ₱", "").Trim()),
                Discount = decimal.Parse(txtCreateDiscount.Text),
                Tax = decimal.Parse(txtCreateTax.Text)
            };

            foreach (Control row in flpCreateItems.Controls)
            {
                dynamic tag = row.Tag; if (tag == null) continue;
                string code = ((DarkComboBox)tag.Code).Text;
                if (string.IsNullOrEmpty(code)) continue;
                po.Items.Add(new ProcurementItemModel
                {
                    ItemCode = code,
                    Description = ((Label)tag.Name).Text,
                    Quantity = int.Parse(((TextBox)tag.Qty).Text),
                    UnitPrice = decimal.Parse(((TextBox)tag.Price).Text)
                });
            }

            // CRITICAL FIX: Route to Update if editing, otherwise Insert
            string result = _isEditMode ? _procController.UpdatePO(po) : _procController.SavePO(po);

            if (result == "SUCCESS")
            {
                string action = _isEditMode ? "PO Updated" : (status == "Draft" ? "Draft Saved" : "PO Submitted");
                LogAndNotify(action, $"{newPO} saved successfully.", true);
                LoadData();
                SwitchView("Landing");
            }
            else
            {
                MessageBox.Show($"Database Save Failed:\n\n{result}", "SQL Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================================
        // DATA LOAD & MODALS
        // =========================================================================
        private void LoadDatabaseData()
        {
            try
            {
                _dbSuppliers = _supplierController.GetAllSuppliers("Active");
                _dbItems = _inventoryController.GetAllBlueprints();
            }
            catch { }
        }

        private void LoadData()
        {
            try { _allPOs = _procController.GetAllProcurements(); } catch { }
            FilterGrid();
        }

        private void FilterGrid()
        {
            string filter = cmbFilter.SelectedItem?.ToString() ?? "All Purchase Orders";
            string search = txtSearch.Text == "Search PO Number..." ? "" : txtSearch.Text.ToLower();

            dgvPOList.Rows.Clear();
            foreach (var po in _allPOs.OrderByDescending(p => p.OrderDate))
            {
                if (filter != "All Purchase Orders" && po.Status != filter) continue;
                if (!string.IsNullOrEmpty(search) && !po.PO_Number.ToLower().Contains(search) && !po.SupplierName.ToLower().Contains(search)) continue;

                // ADD THE ARROW "➤" AS THE LAST VALUE HERE
                dgvPOList.Rows.Add(po.PO_Number, po.SupplierName, po.OrderDate.ToString("MMM dd, yyyy"), po.GrandTotal, po.Status, "›");
            }
            dgvPOList.Invalidate();
        }

        private void OpenModal(string type)
        {
            ModalForm modal = new ModalForm { FormBorderStyle = FormBorderStyle.None, BackColor = UITheme.CurrentPanel, StartPosition = FormStartPosition.CenterScreen, ShowInTaskbar = false };
            modal.Paint += (s, e) =>
            {
                if (modal.Width <= 1 || modal.Height <= 1) return; e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath path = new GraphicsPath())
                {
                    int r = 12; path.AddArc(0, 0, r, r, 180, 90); path.AddArc(modal.Width - r - 1, 0, r, r, 270, 90); path.AddArc(modal.Width - r - 1, modal.Height - r - 1, r, r, 0, 90); path.AddArc(0, modal.Height - r - 1, r, r, 90, 90); path.CloseFigure(); modal.Region = new Region(path);
                    using (Pen p = new Pen(UITheme.CurrentBorder, 3)) { e.Graphics.DrawPath(p, path); }
                }
            };

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 70, BackColor = UITheme.CurrentPanel };
            pnlFooter.Paint += (s, e) => { using (Pen p = new Pen(UITheme.CurrentBorder, 1)) { e.Graphics.DrawLine(p, 0, 0, pnlFooter.Width, 0); } };

            if (type == "ApprovePO")
            {
                modal.Size = new Size(450, 280);
                IconPictureBox iconWarning = new IconPictureBox { IconChar = IconChar.CheckDouble, IconColor = Color.FromArgb(16, 185, 129), IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 30) };
                Label lblWarn = new Label { Text = "Approve Purchase Order", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 110);
                Label lblDesc = new Label { Text = $"Are you sure you want to approve\n{_selectedPO.PO_Number} and mark it as Ordered?", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 145);

                Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.Click += (s, e) => modal.Close();

                Button btnAction = new Button { Text = "Approve Order", Size = new Size(130, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(16, 185, 129), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;

                int totalW = btnCancel.Width + 10 + btnAction.Width;
                btnCancel.Location = new Point((modal.Width - totalW) / 2, 16); btnAction.Location = new Point(((modal.Width - totalW) / 2) + btnCancel.Width + 10, 16);

                btnAction.Click += (s, e) =>
                {
                    try
                    {
                        if (_procController.UpdatePOStatus(_selectedPO.PO_Number, "Ordered", _activeUserId))
                        {
                            _selectedPO.Status = "Ordered"; LogAndNotify("PO Approved", $"{_selectedPO.PO_Number} approved and ordered.", true); LoadData();
                        }
                    }
                    catch { }
                    SwitchView("Profile"); modal.Close();
                };
                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.AddRange(new Control[] { iconWarning, lblWarn, lblDesc });
            }
            else if (type == "CancelPO")
            {
                modal.Size = new Size(400, 280);
                IconPictureBox iconWarning = new IconPictureBox { IconChar = IconChar.ExclamationTriangle, IconColor = Color.FromArgb(239, 68, 68), IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 30) };

                Label lblWarn = new Label { Text = "Cancel Purchase Order", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 110);
                Label lblDesc = new Label { Text = $"Are you sure you want to cancel\n{_selectedPO.PO_Number}?\nThis action cannot be undone.", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 145);

                Button btnCancel = new Button { Text = "Keep PO", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.Click += (s, e) => modal.Close();

                Button btnAction = new Button { Text = "Cancel Order", Size = new Size(120, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(239, 68, 68), ForeColor = Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;

                int totalW = btnCancel.Width + 10 + btnAction.Width;
                btnCancel.Location = new Point((modal.Width - totalW) / 2, 16); btnAction.Location = new Point(((modal.Width - totalW) / 2) + btnCancel.Width + 10, 16);

                btnAction.Click += (s, e) =>
                {
                    try
                    {
                        if (_procController.UpdatePOStatus(_selectedPO.PO_Number, "Cancelled", _activeUserId))
                        {
                            _selectedPO.Status = "Cancelled"; LogAndNotify("PO Cancelled", $"{_selectedPO.PO_Number} has been halted.", true); LoadData();
                        }
                    }
                    catch { }
                    SwitchView("Profile"); modal.Close();
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.AddRange(new Control[] { iconWarning, lblWarn, lblDesc });
            }
            else if (type == "SubmitPO")
            {
                modal.Size = new Size(400, 280);
                IconPictureBox iconWarning = new IconPictureBox { IconChar = IconChar.PaperPlane, IconColor = UITheme.AccentYellow, IconSize = 60, Size = new Size(60, 60), Location = new Point((modal.Width - 60) / 2, 30) };

                Label lblWarn = new Label { Text = "Submit Purchase Order", Font = new Font("Segoe UI", 14F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true };
                lblWarn.Location = new Point((modal.Width - lblWarn.PreferredWidth) / 2, 110);
                Label lblDesc = new Label { Text = "This PO will be marked as Pending Approval.\nAre you ready to submit?", Font = new Font("Segoe UI", 10F), ForeColor = UITheme.MutedText, AutoSize = true, TextAlign = ContentAlignment.MiddleCenter };
                lblDesc.Location = new Point((modal.Width - lblDesc.PreferredWidth) / 2, 145);

                Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.Click += (s, e) => modal.Close();

                Button btnAction = new Button { Text = "Submit", Size = new Size(120, 38), FlatStyle = FlatStyle.Flat, BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand };
                btnAction.FlatAppearance.BorderSize = 0;

                int totalW = btnCancel.Width + 10 + btnAction.Width;
                btnCancel.Location = new Point((modal.Width - totalW) / 2, 16); btnAction.Location = new Point(((modal.Width - totalW) / 2) + btnCancel.Width + 10, 16);

                btnAction.Click += (s, e) => { modal.Close(); SavePO("Pending Approval"); };
                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.AddRange(new Control[] { iconWarning, lblWarn, lblDesc });
            }
            else if (type == "GoodsReceipt")
            {
                modal.Size = new Size(800, 600);
                Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 60, BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234) };
                Label lblTitle = new Label { Text = "Process Goods Receipt (3-Way Match)", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.CurrentText, AutoSize = true, Location = new Point(50, 17) };
                IconButton btnClose = new IconButton { IconChar = IconChar.Times, IconSize = 20, Size = new Size(40, 40), FlatStyle = FlatStyle.Flat, ForeColor = UITheme.MutedText, BackColor = Color.Transparent, Cursor = Cursors.Hand, Location = new Point(750, 10) };
                btnClose.FlatAppearance.BorderSize = 0; btnClose.Click += (s, e) => modal.Close();
                IconPictureBox hIcon = new IconPictureBox { IconChar = IconChar.BoxOpen, IconColor = UITheme.AccentYellow, IconSize = 22, Size = new Size(24, 24), Location = new Point(20, 18) };
                pnlHeader.Controls.AddRange(new Control[] { hIcon, lblTitle, btnClose });

                Panel body = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(25) };
                Label info = new Label { Text = $"INVENTORY AUTOMATION ACTIVE\nYou are receiving {_selectedPO.PO_Number}.\nPlease enter physical Serial Numbers below to inject them into STOCK_INSTANCE.", Font = new Font("Segoe UI", 10F, FontStyle.Italic), ForeColor = Color.FromArgb(59, 130, 246), AutoSize = false, Size = new Size(750, 80), Location = new Point(25, 20) };
                body.Controls.Add(info);

                int y = 110;
                int count = 1;
                foreach (var item in _selectedPO.Items)
                {
                    for (int i = 0; i < item.Quantity; i++)
                    {
                        Label lNum = new Label { Text = $"#{count}", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = UITheme.MutedText, Location = new Point(25, y), AutoSize = true };
                        Label lName = new Label { Text = item.ItemCode, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = UITheme.CurrentText, Location = new Point(80, y + 3), AutoSize = true };

                        RoundedPanel pCode = new RoundedPanel { Size = new Size(350, 38), Location = new Point(200, y - 5), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 8, 10, 8) };
                        TextBox tCode = new TextBox { Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText, Font = new Font("Consolas", 11F) };
                        pCode.Controls.Add(tCode);

                        RoundedPanel pStat = new RoundedPanel { Size = new Size(120, 38), Location = new Point(560, y - 5), BorderRadius = 4, BorderSize = 1, BorderColor = UITheme.CurrentBorder, BackColor = UITheme.CurrentInputBg, Padding = new Padding(10, 7, 10, 7) };
                        DarkComboBox cStat = new DarkComboBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F), Cursor = Cursors.Hand, BackColor = UITheme.CurrentInputBg, ForeColor = UITheme.CurrentText };
                        cStat.Items.AddRange(new[] { "Available", "Defective" }); cStat.SelectedIndex = 0;
                        pStat.Controls.Add(cStat);

                        body.Controls.AddRange(new Control[] { lNum, lName, pCode, pStat });
                        y += 50; count++;
                    }
                }

                Button btnCancel = new Button { Text = "Cancel", Size = new Size(100, 38), FlatStyle = FlatStyle.Flat, BackColor = Color.Transparent, ForeColor = UITheme.CurrentText, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(modal.Width - 25 - 220 - 10 - 100, 16) };
                btnCancel.FlatAppearance.BorderColor = UITheme.CurrentBorder; btnCancel.Click += (s, e) => modal.Close();

                Button btnAction = new Button { Text = "Save to Inventory", Size = new Size(220, 38), FlatStyle = FlatStyle.Flat, BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark, ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold), Cursor = Cursors.Hand, Location = new Point(modal.Width - 25 - 220, 16) };
                btnAction.FlatAppearance.BorderSize = 0;

                btnAction.Click += (s, e) =>
                {
                    List<StockInstanceModel> physicalItems = new List<StockInstanceModel>();
                    foreach (Control c in body.Controls)
                    {
                        if (c is RoundedPanel rp && rp.Location.X == 200)
                        {
                            TextBox tb = (TextBox)rp.Controls[0];
                            if (!string.IsNullOrWhiteSpace(tb.Text))
                            {
                                string stat = "Available"; string itemCode = "UNKNOWN";
                                foreach (Control peer in body.Controls)
                                {
                                    if (peer is RoundedPanel rp2 && rp2.Location.X == 560 && rp2.Location.Y == rp.Location.Y) stat = ((DarkComboBox)rp2.Controls[0]).Text;
                                    if (peer is Label lbl && lbl.Location.X == 80 && Math.Abs(lbl.Location.Y - rp.Location.Y) < 10) itemCode = lbl.Text;
                                }
                                physicalItems.Add(new StockInstanceModel { SerialNumber = tb.Text, ItemCode = itemCode, Status = stat, SupplierID = _selectedPO.SupplierID, PO_Number = _selectedPO.PO_Number });
                            }
                        }
                    }
                    try
                    {
                        if (_procController.ProcessGoodsReceipt(_selectedPO.PO_Number, physicalItems, _activeUserId))
                        {
                            _selectedPO.Status = "Completed";
                            LogAndNotify("Goods Receipt Processed", $"Inventory Synced for {_selectedPO.PO_Number}.", true);
                            LoadData(); SwitchView("Profile"); modal.Close();
                        }
                        else { ShowToast("Database error syncing inventory.", false); }
                    }
                    catch { }
                };

                pnlFooter.Controls.AddRange(new Control[] { btnCancel, btnAction });
                modal.Controls.Add(body); modal.Controls.Add(pnlHeader);
            }

            modal.Controls.Add(pnlFooter);
            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show(); modal.ShowDialog(overlay); overlay.Dispose();
        }

        // =========================================================================
        // NATIVE PDF GENERATOR ENGINE
        // =========================================================================
        private void GeneratePDF()
        {
            PrintDocument pd = new PrintDocument();
            pd.DefaultPageSettings.PaperSize = new PaperSize("A4", 827, 1169);
            pd.PrintPage += Pd_PrintPage;

            PrintPreviewDialog ppd = new PrintPreviewDialog { Document = pd, Width = 800, Height = 1000 };
            Form parent = this.FindForm();
            Form overlay = new Form { StartPosition = FormStartPosition.Manual, Location = parent.PointToScreen(Point.Empty), Size = parent.ClientSize, BackColor = Color.Black, Opacity = 0.6, FormBorderStyle = FormBorderStyle.None, ShowInTaskbar = false };
            overlay.Show(); ppd.ShowDialog(overlay); overlay.Dispose();
        }

        private void Pd_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.HighQuality;
            int y = 50;

            Font fTitle = new Font("Arial", 24, FontStyle.Bold);
            Font fSubTitle = new Font("Arial", 12, FontStyle.Bold);
            Font fNormal = new Font("Arial", 10, FontStyle.Regular);
            Font fBold = new Font("Arial", 10, FontStyle.Bold);

            g.DrawString("SJ PC STORE", fTitle, Brushes.Black, 50, y);
            y += 40;
            g.DrawString("McArthur Highway, Bocaue, Bulacan", fNormal, Brushes.DimGray, 50, y); y += 20;
            g.DrawString("Phone: 0917-000-0000 | TIN: 000-000-000", fNormal, Brushes.DimGray, 50, y);

            g.DrawString("PURCHASE ORDER", fTitle, Brushes.DarkBlue, 450, 50);
            g.DrawString($"PO NUMBER: {_selectedPO.PO_Number}", fSubTitle, Brushes.Black, 450, 90);
            g.DrawString($"DATE: {_selectedPO.OrderDate:MMM dd, yyyy}", fNormal, Brushes.Black, 450, 110);
            g.DrawString($"STATUS: {_selectedPO.Status.ToUpper()}", fBold, Brushes.DarkBlue, 450, 130);

            y += 50;
            g.DrawLine(Pens.Black, 50, y, 770, y); y += 30;

            g.DrawString("SUPPLIER:", fSubTitle, Brushes.DarkBlue, 50, y); y += 25;
            g.DrawString(_selectedPO.SupplierName, fBold, Brushes.Black, 50, y); y += 20;
            var supplierInfo = _dbSuppliers.FirstOrDefault(s => s.SupplierID == _selectedPO.SupplierID);
            g.DrawString(supplierInfo?.Address ?? "Address Unavailable", fNormal, Brushes.Black, 50, y); y += 20;
            g.DrawString($"Contact: {_selectedPO.ContactPerson}", fNormal, Brushes.Black, 50, y); y += 20;
            g.DrawString($"Phone: {_selectedPO.ContactNumber}", fNormal, Brushes.Black, 50, y); y += 40;

            g.FillRectangle(Brushes.DarkBlue, 50, y, 720, 30);
            g.DrawString("ITEM NAME", fBold, Brushes.White, 60, y + 7);
            g.DrawString("QTY", fBold, Brushes.White, 420, y + 7);
            g.DrawString("UNIT PRICE", fBold, Brushes.White, 500, y + 7);
            g.DrawString("TOTAL", fBold, Brushes.White, 620, y + 7);
            y += 40;

            foreach (var item in _selectedPO.Items)
            {
                g.DrawString(item.Description.Length > 30 ? item.Description.Substring(0, 30) + "..." : item.Description, fNormal, Brushes.Black, 60, y);
                g.DrawString(item.Quantity.ToString(), fNormal, Brushes.Black, 420, y);
                g.DrawString(item.UnitPrice.ToString("N2"), fNormal, Brushes.Black, 500, y);
                g.DrawString(item.TotalAmount.ToString("N2"), fNormal, Brushes.Black, 620, y);
                y += 30;
            }
            g.DrawLine(Pens.Gray, 50, y, 770, y); y += 20;

            g.DrawString("Subtotal:", fNormal, Brushes.DimGray, 560, y); g.DrawString(_selectedPO.SubTotal.ToString("C2"), fNormal, Brushes.Black, 680, y); y += 25;
            g.DrawString("Discount:", fNormal, Brushes.DimGray, 560, y); g.DrawString($"-{_selectedPO.Discount:C2}", fNormal, Brushes.Red, 680, y); y += 25;
            g.DrawString("Tax:", fNormal, Brushes.DimGray, 560, y); g.DrawString($"{_selectedPO.Tax:C2}", fNormal, Brushes.Black, 680, y); y += 25;
            g.DrawString("GRAND TOTAL:", fSubTitle, Brushes.DarkBlue, 520, y); g.DrawString(_selectedPO.GrandTotal.ToString("C2"), fSubTitle, Brushes.Black, 680, y);

            y += 50;
            g.DrawString("Remarks/Terms:", fSubTitle, Brushes.DarkBlue, 50, y); y += 25;
            g.DrawString(_selectedPO.Remarks ?? "N/A", fNormal, Brushes.Black, 50, y);

            y = 1000;
            g.DrawLine(Pens.Black, 50, y, 250, y);
            g.DrawString("Authorized Signature", fNormal, Brushes.DimGray, 90, y + 10);
        }

        // =========================================================================
        // EXTERNAL ROUTING
        // Allows other modules (like Data Management) to force-open a specific PO
        // =========================================================================
        public void OpenExternalPO(string poNumber)
        {
            LoadData(); // Refresh the database to ensure we have the latest status
            _selectedPO = _allPOs.FirstOrDefault(p => p.PO_Number == poNumber);

            if (_selectedPO != null)
            {
                SwitchView("Profile"); // Route to the Profile screen
                this.BringToFront();   // Force the Procurement View to the front of the Dashboard
            }
            else
            {
                ShowToast("Purchase Order not found or access denied.", false);
            }
        }

        private void LoadAttachments()
        {
            if (flpAttachments == null || _selectedPO == null) return;
            flpAttachments.Controls.Clear();
            var attachments = _attachController.GetAttachments(_selectedPO.PO_Number);
            foreach (var att in attachments)
            {
                Panel row = new Panel { Width = flpAttachments.Width - SystemInformation.VerticalScrollBarWidth - 5, Height = 30 };

                // File name label (clickable)
                int maxLabelWidth = row.Width - 30 - 10; // 30 for trash button, 10 for padding
                Label lblFile = new Label
                {
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = Color.FromArgb(59, 130, 246),
                    Cursor = Cursors.Hand,
                    AutoSize = false,
                    Width = maxLabelWidth,
                    Height = 20,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(5, 6)
                };

                lblFile.Text = TruncateText(att.FileName, lblFile.Font, maxLabelWidth);

                lblFile.Click += (s, e) =>
                {
                    try { System.Diagnostics.Process.Start(att.FilePath); } catch { ShowToast("Cannot open file.", false); }
                };
                lblFile.MouseEnter += (s, e) => { lblFile.Font = new Font(lblFile.Font, FontStyle.Underline | FontStyle.Bold); };
                lblFile.MouseLeave += (s, e) => { lblFile.Font = new Font(lblFile.Font, FontStyle.Regular); };

                // Delete button (trash icon)
                IconButton btnDel = new IconButton
                {
                    IconChar = IconChar.Trash,
                    IconSize = 16,
                    Size = new Size(25, 25),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    BackColor = Color.Transparent,
                    IconColor = Color.FromArgb(239, 68, 68),
                    ForeColor = Color.FromArgb(239, 68, 68),
                    Location = new Point(row.Width - 30, 3)
                };
                btnDel.FlatAppearance.BorderSize = 0;
                btnDel.Click += (s, e) =>
                {
                    if (_attachController.DeleteAttachment(att.AttachmentID))
                    {
                        LogAndNotify("Attachment Deleted", att.FileName, true);
                        LoadAttachments();
                    }
                };

                row.Controls.Add(lblFile);
                row.Controls.Add(btnDel);
                flpAttachments.Controls.Add(row);
            }

            // 💡 Right here: after all rows are added, adjust the panel height
            flpAttachments.Height = flpAttachments.Controls.Count * 35 + 5;

            AdjustCardHeight(cardAttach);
        }

        private string TruncateText(string text, Font font, int maxWidth)
        {
            if (string.IsNullOrEmpty(text)) return text;
            if (TextRenderer.MeasureText(text, font).Width <= maxWidth)
                return text;

            string ellipsis = "...";
            int ellipsisWidth = TextRenderer.MeasureText(ellipsis, font).Width;
            int allowedWidth = maxWidth - ellipsisWidth;
            if (allowedWidth <= 0) return ellipsis;

            for (int i = text.Length - 1; i > 0; i--)
            {
                string trimmed = text.Substring(0, i);
                if (TextRenderer.MeasureText(trimmed, font).Width <= allowedWidth)
                    return trimmed + ellipsis;
            }
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

            if (cmbFilter != null) { cmbFilter.BackColor = UITheme.CurrentPanel; cmbFilter.ForeColor = UITheme.CurrentText; }
            foreach (DarkComboBox cmb in _comboInputs) { cmb.BackColor = UITheme.CurrentInputBg; cmb.ForeColor = UITheme.CurrentText; } // KEEP THIS DO NOT TOUCH THIS. MANUALLY ADJUSTED cmbFilter bg input the same color as the panel for transparent view
            foreach (DarkComboBox cmb in _comboFilterInputs) { cmb.BackColor = UITheme.CurrentPanel; cmb.ForeColor = UITheme.CurrentText; } // KEEP THIS DO NOT TOUCH THIS. MANUALLY ADJUSTED cmbFilter bg input the same color as the panel for transparent view

            foreach (RoundedPanel wrap in _inputWrappers) { wrap.BackColor = UITheme.CurrentInputBg; wrap.BorderColor = UITheme.CurrentBorder; foreach (Control c in wrap.Controls) { if (c is IconPictureBox icon) icon.IconColor = UITheme.CurrentIcon; } }
            foreach (RoundedPanel wrap in _borderedContainers) { wrap.BackColor = UITheme.CurrentPanel; wrap.BorderColor = UITheme.CurrentBorder; }

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
                btn.IconColor = btn.ForeColor; btn.FlatAppearance.MouseDownBackColor = btn.BackColor;
            }

            if (btnGoodsReceipt != null)
            {
                if (!btnGoodsReceipt.Enabled)
                {
                    btnGoodsReceipt.BackColor = UITheme.CurrentBorder; btnGoodsReceipt.ForeColor = UITheme.MutedText; btnGoodsReceipt.IconColor = UITheme.MutedText;
                }
            }

            if (btnAddRow != null)
            {
                btnAddRow.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark; btnAddRow.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White; btnAddRow.FlatAppearance.MouseOverBackColor = UITheme.IsDarkMode ? Color.FromArgb(255, 230, 120) : UITheme.SecondaryDark; btnAddRow.IconColor = btnAddRow.ForeColor;
            }

            if (dgvPOList != null)
            {
                dgvPOList.BackgroundColor = UITheme.CurrentPanel;

                dgvPOList.GridColor = UITheme.CurrentBorder;
                dgvPOList.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

                dgvPOList.DefaultCellStyle.BackColor = UITheme.CurrentPanel;
                dgvPOList.DefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvPOList.DefaultCellStyle.SelectionBackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(220, 230, 240);
                dgvPOList.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;
                dgvPOList.ColumnHeadersDefaultCellStyle.BackColor = UITheme.IsDarkMode ? Color.FromArgb(34, 32, 38) : Color.FromArgb(226, 230, 234);
                dgvPOList.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvPOList.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgvPOList.ColumnHeadersDefaultCellStyle.BackColor;
            }

            if (dgvProfileItems != null)
            {
                dgvProfileItems.BackgroundColor = UITheme.CurrentInputBg; dgvProfileItems.GridColor = UITheme.CurrentBorder;
                dgvProfileItems.DefaultCellStyle.BackColor = UITheme.CurrentInputBg; dgvProfileItems.DefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvProfileItems.DefaultCellStyle.SelectionBackColor = UITheme.IsDarkMode ? Color.FromArgb(60, 58, 65) : Color.FromArgb(220, 230, 240);
                dgvProfileItems.DefaultCellStyle.SelectionForeColor = UITheme.CurrentText;
                dgvProfileItems.ColumnHeadersDefaultCellStyle.BackColor = UITheme.CurrentInputBg;
                dgvProfileItems.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.CurrentText;
                dgvProfileItems.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgvProfileItems.ColumnHeadersDefaultCellStyle.BackColor;
            }

            if (profileStepper != null) profileStepper.Invalidate(); this.Invalidate(true);

            if (tlpItemH != null)
            {
                tlpItemH.BackColor = UITheme.IsDarkMode
                    ? Color.FromArgb(34, 32, 38)
                    : Color.FromArgb(226, 230, 234);
            }

            if (btnAttach != null)
            {
                btnAttach.BackColor = UITheme.IsDarkMode ? UITheme.AccentYellow : UITheme.PrimaryDark;
                btnAttach.ForeColor = UITheme.IsDarkMode ? Color.Black : Color.White;
            }

            if (dtpOrderDate != null) dtpOrderDate.ApplyTheme(UITheme.IsDarkMode);
            if (dtpExpectedDate != null) dtpExpectedDate.ApplyTheme(UITheme.IsDarkMode);
        }
    }
}