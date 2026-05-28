// Forms/MainForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StroiSnabApp.Data;
using StroiSnabApp.Models;
using StroiSnabApp.Services;

namespace StroiSnabApp.Forms
{
    /// <summary>
    /// Главная форма: список заявок на поставку стройматериалов.
    /// Плашки статистики · Фильтрация · Таблица · CRUD · Экспорт JSON
    /// </summary>
    public class MainForm : Form
    {
        // ── Компоненты ──────────────────────────────────────────
        private DataGridView dgvOrders;
        private TextBox      txtSearch;
        private ComboBox     cmbStatusFilter;
        private Button       btnAdd, btnEdit, btnItems, btnDelete, btnExport, btnRefresh;
        private Label        lblTotal, lblNew, lblInProgress, lblDelivered, lblRevenue;
        private Panel        pnlSummary, pnlToolbar;
        private Label        lblTitle, lblSubtitle;

        private readonly DatabaseHelper    _db     = new DatabaseHelper();
        private readonly JsonExportService _export = new JsonExportService();
        private List<Order> _currentOrders = new List<Order>();

        private const string PLACEHOLDER = "Поиск по номеру, клиенту, адресу...";

        public MainForm()
        {
            InitializeUI();
            LoadStatuses();
            LoadData();
        }

        // ════════════════════════════════════════════════════════
        //  ПОСТРОЕНИЕ ИНТЕРФЕЙСА
        // ════════════════════════════════════════════════════════
        private void InitializeUI()
        {
            Text          = "ЦентрТрансГранит — Учёт заявок на поставку стройматериалов";
            Size          = new Size(1280, 740);
            MinimumSize   = new Size(960, 600);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = Color.FromArgb(245, 246, 250);
            Font          = new Font("Segoe UI", 9.5f);

            // ── Шапка ───────────────────────────────────────────
            var pnlHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 56,
                BackColor = Color.FromArgb(26, 58, 92)
            };
            lblTitle = new Label
            {
                Text      = "ООО «ЦентрТрансГранит»",
                Left = 16, Top = 8, AutoSize = true,
                Font      = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            lblSubtitle = new Label
            {
                Text      = "Поставки строительных материалов",
                Left = 16, Top = 32, AutoSize = true,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 210, 240),
                BackColor = Color.Transparent
            };
            pnlHeader.Controls.AddRange(new Control[] { lblTitle, lblSubtitle });

            // ── Плашки-счётчики ─────────────────────────────────
            pnlSummary = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 68,
                BackColor = Color.White,
                Padding   = new Padding(12, 10, 12, 10)
            };
            lblTotal      = MakeSummaryLabel("Всего: 0",       Color.FromArgb(33, 150, 243),  10);
            lblNew        = MakeSummaryLabel("Новые: 0",       Color.FromArgb(255, 152, 0),   175);
            lblInProgress = MakeSummaryLabel("В работе: 0",   Color.FromArgb(156, 39, 176),  340);
            lblDelivered  = MakeSummaryLabel("Доставлено: 0", Color.FromArgb(76, 175, 80),   505);
            lblRevenue    = MakeSummaryLabel("Сумма: 0 ₽",    Color.FromArgb(0, 150, 136),   680);
            pnlSummary.Controls.AddRange(
                new Control[] { lblTotal, lblNew, lblInProgress, lblDelivered, lblRevenue });

            // ── Панель инструментов ─────────────────────────────
            pnlToolbar = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 46,
                BackColor = Color.FromArgb(245, 246, 250),
                Padding   = new Padding(10, 7, 10, 7)
            };

            // Поле поиска с placeholder (.NET 4.8 — вручную)
            txtSearch = new TextBox
            {
                Text      = PLACEHOLDER,
                ForeColor = Color.Gray,
                Width     = 270,
                Left      = 0,
                Top       = 8,
                Height    = 28
            };
            txtSearch.Enter += (s, e) =>
            {
                if (txtSearch.Text == PLACEHOLDER)
                { txtSearch.Text = ""; txtSearch.ForeColor = Color.Black; }
            };
            txtSearch.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearch.Text))
                { txtSearch.Text = PLACEHOLDER; txtSearch.ForeColor = Color.Gray; }
            };
            txtSearch.TextChanged += (s, e) =>
            { if (txtSearch.ForeColor != Color.Gray) LoadData(); };

            var lblFilter = new Label
            {
                Text = "Статус:", Left = 285, Top = 11,
                AutoSize = true, ForeColor = Color.FromArgb(100, 110, 130)
            };
            cmbStatusFilter = new ComboBox
            {
                Left = 340, Top = 7, Width = 165,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.SelectedIndexChanged += (s, e) => LoadData();

            btnAdd      = MakeBtn("+ Добавить",  515, Color.FromArgb(33, 150, 243));
            btnEdit     = MakeBtn("✏ Изменить",  630, Color.FromArgb(100, 100, 120));
            btnItems    = MakeBtn("📋 Состав",    745, Color.FromArgb(0, 150, 136));
            btnDelete   = MakeBtn("✕ Удалить",   860, Color.FromArgb(220, 53, 69));
            btnExport   = MakeBtn("⬇ JSON",      975, Color.FromArgb(40, 167, 69));
            btnRefresh  = MakeBtn("↺",           1080, Color.FromArgb(80, 90, 110));
            btnRefresh.Width = 36;

            btnAdd.Click    += BtnAdd_Click;
            btnEdit.Click   += BtnEdit_Click;
            btnItems.Click  += BtnItems_Click;
            btnDelete.Click += BtnDelete_Click;
            btnExport.Click += BtnExport_Click;
            btnRefresh.Click += (s, e) => LoadData();

            pnlToolbar.Controls.AddRange(new Control[]
            { txtSearch, lblFilter, cmbStatusFilter,
              btnAdd, btnEdit, btnItems, btnDelete, btnExport, btnRefresh });

            // ── Таблица заявок ──────────────────────────────────
            dgvOrders = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible     = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor       = Color.FromArgb(245, 246, 250),
                BorderStyle           = BorderStyle.None,
                GridColor             = Color.FromArgb(220, 225, 235),
                Font                  = new Font("Segoe UI", 9.5f)
            };
            dgvOrders.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(26, 58, 92);
            dgvOrders.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvOrders.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            dgvOrders.ColumnHeadersHeight    = 34;
            dgvOrders.RowTemplate.Height     = 30;
            dgvOrders.CellFormatting        += DgvOrders_CellFormatting;
            dgvOrders.DoubleClick           += (s, e) => BtnEdit_Click(s, e);

            // ── Сборка формы ────────────────────────────────────
            Controls.Add(dgvOrders);
            Controls.Add(pnlToolbar);
            Controls.Add(pnlSummary);
            Controls.Add(pnlHeader);
        }

        // ════════════════════════════════════════════════════════
        //  ЗАГРУЗКА ДАННЫХ
        // ════════════════════════════════════════════════════════
        private void LoadStatuses()
        {
            cmbStatusFilter.Items.Clear();
            cmbStatusFilter.Items.Add(new OrderStatus { StatusID = 0, StatusName = "Все статусы" });
            foreach (var s in _db.GetStatuses())
                cmbStatusFilter.Items.Add(s);
            cmbStatusFilter.SelectedIndex = 0;
        }

        private void LoadData()
        {
            try
            {
                int? statusId = null;
                if (cmbStatusFilter.SelectedItem is OrderStatus st && st.StatusID != 0)
                    statusId = st.StatusID;

                string search = txtSearch.ForeColor == Color.Gray ? null : txtSearch.Text;
                _currentOrders = _db.GetOrders(statusId, search);
                BindGrid(_currentOrders);
                UpdateSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки:\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindGrid(List<Order> orders)
        {
            dgvOrders.Columns.Clear();
            dgvOrders.AutoGenerateColumns = false;

            AddCol("OrderNumber",     "№ Заявки",     90);
            AddCol("ClientName",      "Клиент",        220);
            AddCol("ClientPhone",     "Телефон",       130);
            AddCol("StatusName",      "Статус",        120);
            AddCol("DeliveryAddress", "Адрес доставки",230);
            AddCol("TotalAmount",     "Сумма, ₽",       100);
            AddCol("DeliveryDate",    "Дата доставки", 120);
            AddCol("Notes",           "Примечание",    150);

            dgvOrders.DataSource = orders;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "MainForm";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);

        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void UpdateSummary()
        {
            try
            {
                var dt = _db.GetSummary();
                if (dt.Rows.Count == 0) return;
                var r = dt.Rows[0];
                lblTotal.Text      = $"Всего: {r["TotalOrders"]}";
                lblNew.Text        = $"Новые: {r["NewOrders"]}";
                lblInProgress.Text = $"В работе: {r["InProgress"]}";
                lblDelivered.Text  = $"Доставлено: {r["Delivered"]}";
                lblRevenue.Text    = $"Сумма: {(decimal)r["TotalRevenue"]:N0} ₽";
            }
            catch { }
        }

        // ════════════════════════════════════════════════════════
        //  ОБРАБОТЧИКИ КНОПОК
        // ════════════════════════════════════════════════════════
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using (var f = new OrderForm(_db))
                if (f.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            var o = GetSelected();
            if (o == null) return;
            using (var f = new OrderForm(_db, o))
                if (f.ShowDialog() == DialogResult.OK) LoadData();
        }

        private void BtnItems_Click(object sender, EventArgs e)
        {
            var o = GetSelected();
            if (o == null) return;
            using (var f = new ItemsForm(_db, o))
            {
                f.ShowDialog();
                LoadData(); // пересчитать суммы
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var o = GetSelected();
            if (o == null) return;
            if (MessageBox.Show(
                    $"Удалить заявку {o.OrderNumber}?\nВсе позиции заявки тоже будут удалены.",
                    "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                == DialogResult.Yes)
            {
                try { _db.DeleteOrder(o.OrderID); LoadData(); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            using (var dlg = new SaveFileDialog
            {
                Filter   = "JSON файлы (*.json)|*.json",
                FileName = $"orders_{DateTime.Now:yyyyMMdd}.json"
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _export.ExportOrders(_currentOrders, dlg.FileName);
                        MessageBox.Show(
                            $"Экспортировано {_currentOrders.Count} заявок.\n{dlg.FileName}",
                            "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
                }
            }
        }

        // ════════════════════════════════════════════════════════
        //  ФОРМАТИРОВАНИЕ ЯЧЕЕК
        // ════════════════════════════════════════════════════════
        private void DgvOrders_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _currentOrders.Count) return;
            var o = _currentOrders[e.RowIndex];

            // Столбец «Статус» — цвет из БД
            if (dgvOrders.Columns[e.ColumnIndex].DataPropertyName == "StatusName")
            {
                try
                {
                    var clr = ColorTranslator.FromHtml(o.StatusColor);
                    e.CellStyle.BackColor = Color.FromArgb(45, clr.R, clr.G, clr.B);
                    e.CellStyle.ForeColor = clr;
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
                }
                catch { }
            }

            // Столбец «Сумма» — форматирование числа
            if (dgvOrders.Columns[e.ColumnIndex].DataPropertyName == "TotalAmount"
                && e.Value is decimal amount)
            {
                e.Value = amount.ToString("N2");
                e.FormattingApplied = true;
            }

            // Просрочена дата доставки (не «Доставлена» и не «Отменена»)
            if (o.DeliveryDate.HasValue
                && o.DeliveryDate.Value < DateTime.Today
                && o.StatusID != 5 && o.StatusID != 6)
            {
                e.CellStyle.BackColor = Color.FromArgb(255, 235, 235);
                e.CellStyle.ForeColor = Color.DarkRed;
            }
        }

        // ════════════════════════════════════════════════════════
        //  ВСПОМОГАТЕЛЬНЫЕ
        // ════════════════════════════════════════════════════════
        private Order GetSelected()
        {
            if (dgvOrders.CurrentRow == null) return null;
            int idx = dgvOrders.CurrentRow.Index;
            return (idx >= 0 && idx < _currentOrders.Count) ? _currentOrders[idx] : null;
        }

        private void AddCol(string prop, string header, int weight)
        {
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = prop,
                HeaderText       = header,
                FillWeight       = weight,
                MinimumWidth     = 60
            });
        }

        private Label MakeSummaryLabel(string text, Color color, int left)
        {
            return new Label
            {
                Text = text, Left = left, Top = 12,
                Width = 160, Height = 44,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = color, TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private Button MakeBtn(string text, int left, Color back)
        {
            return new Button
            {
                Text = text, Left = left, Top = 7,
                Width = 110, Height = 30,
                BackColor = back, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }
    }
}
