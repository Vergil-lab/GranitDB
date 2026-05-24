// Forms/ItemsForm.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using StroiSnabApp.Data;
using StroiSnabApp.Models;

namespace StroiSnabApp.Forms
{
    /// <summary>
    /// Форма просмотра и редактирования состава заявки (позиции материалов).
    /// </summary>
    public class ItemsForm : Form
    {
        private DataGridView   dgvItems;
        private ComboBox       cmbMaterial;
        private NumericUpDown  numQty;
        private Label          lblPrice, lblTotal;
        private Button         btnAddItem, btnRemoveItem, btnClose;
        private Label          lblOrderInfo;

        private readonly DatabaseHelper _db;
        private readonly Order          _order;
        private List<OrderItem>         _items = new List<OrderItem>();
        private List<Material>          _materials;

        public ItemsForm(DatabaseHelper db, Order order)
        {
            _db    = db;
            _order = order;
            InitializeUI();
            LoadMaterials();
            LoadItems();
        }

        private void InitializeUI()
        {
            Text            = $"Состав заявки {_order.OrderNumber}";
            Size            = new Size(820, 560);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox     = false;
            StartPosition   = FormStartPosition.CenterParent;
            BackColor       = Color.White;
            Font            = new Font("Segoe UI", 9.5f);

            // ── Шапка заявки ────────────────────────────────────
            lblOrderInfo = new Label
            {
                Text = $"Клиент: {_order.ClientName}   |   Статус: {_order.StatusName}   |   Адрес: {_order.DeliveryAddress}",
                Dock = DockStyle.Top, Height = 32, Padding = new Padding(12, 0, 0, 0),
                BackColor = Color.FromArgb(26, 58, 92), ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f), TextAlign = ContentAlignment.MiddleLeft
            };

            // ── Таблица позиций ──────────────────────────────────
            dgvItems = new DataGridView
            {
                Left = 12, Top = 48, Width = 780, Height = 300,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.WhiteSmoke,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            dgvItems.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 236, 248);
            dgvItems.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 50, 90);
            dgvItems.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold);
            dgvItems.ColumnHeadersHeight = 30;
            dgvItems.RowTemplate.Height  = 28;

            // Итоговая сумма
            lblTotal = new Label
            {
                Left = 12, Top = 358, Width = 780, Height = 28,
                Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 80), TextAlign = ContentAlignment.MiddleRight
            };

            // ── Панель добавления позиции ────────────────────────
            var pnlAdd = new Panel
            {
                Left = 12, Top = 394, Width = 780, Height = 78,
                BackColor = Color.FromArgb(245, 248, 255),
                BorderStyle = BorderStyle.FixedSingle
            };

            new Label { Text = "Материал:", Left = 8, Top = 12, AutoSize = true,
                ForeColor = Color.FromArgb(70, 80, 100), Parent = pnlAdd };

            cmbMaterial = new ComboBox
            {
                Left = 80, Top = 8, Width = 340, DropDownStyle = ComboBoxStyle.DropDownList, Parent = pnlAdd
            };
            cmbMaterial.SelectedIndexChanged += CmbMaterial_Changed;

            new Label { Text = "Кол-во:", Left = 432, Top = 12, AutoSize = true,
                ForeColor = Color.FromArgb(70, 80, 100), Parent = pnlAdd };

            numQty = new NumericUpDown
            {
                Left = 485, Top = 8, Width = 90, Minimum = 1, Maximum = 99999,
                DecimalPlaces = 2, Value = 1, Parent = pnlAdd
            };

            lblPrice = new Label
            { Left = 8, Top = 42, Width = 400, AutoSize = true,
              ForeColor = Color.FromArgb(80, 100, 130), Font = new Font("Segoe UI", 9f),
              Parent = pnlAdd };

            btnAddItem = new Button
            {
                Text = "+ Добавить позицию", Left = 590, Top = 6, Width = 170, Height = 32,
                BackColor = Color.FromArgb(0, 150, 136), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Parent = pnlAdd
            };
            btnAddItem.Click += BtnAddItem_Click;

            // ── Кнопки нижней панели ─────────────────────────────
            btnRemoveItem = new Button
            {
                Text = "✕ Удалить позицию", Left = 12, Top = 486, Width = 170, Height = 30,
                BackColor = Color.FromArgb(220, 53, 69), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            btnRemoveItem.Click += BtnRemoveItem_Click;

            btnClose = new Button
            {
                Text = "Закрыть", Left = 688, Top = 486, Width = 110, Height = 30,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[]
            { lblOrderInfo, dgvItems, lblTotal, pnlAdd, btnRemoveItem, btnClose });
        }

        private void LoadMaterials()
        {
            _materials = _db.GetMaterials();
            cmbMaterial.DisplayMember = "MaterialName";
            cmbMaterial.ValueMember   = "MaterialID";
            cmbMaterial.DataSource    = new System.ComponentModel.BindingList<Material>(_materials);
            if (cmbMaterial.Items.Count > 0) cmbMaterial.SelectedIndex = 0;
        }

        private void LoadItems()
        {
            _items = _db.GetOrderItems(_order.OrderID);
            BindItemsGrid();
        }

        private void BindItemsGrid()
        {
            dgvItems.Columns.Clear();
            dgvItems.AutoGenerateColumns = false;

            void AddCol(string prop, string header, int w) =>
                dgvItems.Columns.Add(new DataGridViewTextBoxColumn
                { DataPropertyName = prop, HeaderText = header, Width = w });

            AddCol("MaterialName", "Материал",       280);
            AddCol("Unit",         "Ед.изм.",          70);
            AddCol("Quantity",     "Количество",      100);
            AddCol("UnitPrice",    "Цена за ед., ₽",  120);
            AddCol("LineTotal",    "Сумма, ₽",         120);

            dgvItems.DataSource = _items;

            // Форматирование денежных столбцов
            dgvItems.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var col = dgvItems.Columns[e.ColumnIndex].DataPropertyName;
                if ((col == "UnitPrice" || col == "LineTotal") && e.Value is decimal d)
                { e.Value = d.ToString("N2"); e.FormattingApplied = true; }
            };

            // Итог
            decimal total = 0;
            foreach (var item in _items) total += item.LineTotal;
            lblTotal.Text = $"Итого по заявке:  {total:N2} ₽";
        }

        private void CmbMaterial_Changed(object sender, EventArgs e)
        {
            if (cmbMaterial.SelectedItem is Material m)
                lblPrice.Text = $"Цена: {m.PricePerUnit:N2} ₽ за {m.Unit}   |   На складе: {m.Stock} {m.Unit}";
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            var mat = cmbMaterial.SelectedItem as Material;
            if (mat == null)
            { MessageBox.Show("Выберите материал.", "Внимание"); return; }

            try
            {
                _db.AddOrderItem(new OrderItem
                {
                    OrderID    = _order.OrderID,
                    MaterialID = mat.MaterialID,
                    Quantity   = numQty.Value,
                    UnitPrice  = mat.PricePerUnit
                });
                LoadItems();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (dgvItems.CurrentRow == null) return;
            int idx = dgvItems.CurrentRow.Index;
            if (idx < 0 || idx >= _items.Count) return;

            var item = _items[idx];
            if (MessageBox.Show($"Удалить позицию «{item.MaterialName}»?",
                    "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try { _db.DeleteOrderItem(item.ItemID, _order.OrderID); LoadItems(); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
            }
        }
    }
}
