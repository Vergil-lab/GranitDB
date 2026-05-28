
using System;
using System.Drawing;
using System.Windows.Forms;
using StroiSnabApp.Data;
using StroiSnabApp.Models;

namespace StroiSnabApp.Forms
{
   
    public class OrderForm : Form
    {
        private ComboBox       cmbClient, cmbStatus;
        private TextBox        txtAddress, txtNotes;
        private DateTimePicker dtpDelivery;
        private CheckBox       chkDelivery;
        private Button         btnSave, btnCancel;
        private Label          lblOrderNum;

        private readonly DatabaseHelper _db;
        private readonly Order _edit;

        public OrderForm(DatabaseHelper db, Order edit = null)
        {
            _db   = db;
            _edit = edit;
            InitializeUI();
            LoadCombos();
            if (_edit != null) FillFields();
        }

        private void InitializeUI()
        {
            Text = _edit == null ? "Новая заявка" : $"Редактировать: {_edit.OrderNumber}";
            Size = new Size(480, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9.5f);

            int y = 20;

            // Номер
            AddLbl("Номер заявки:", y);
            Controls.Add(new Label
            {
                Text = _edit?.OrderNumber ?? "(присваивается автоматически)",
                Left = 155, Top = y, Width = 290,
                ForeColor = Color.FromArgb(37, 99, 168),
                Font = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold)
            });
            y += 34;

            // Клиент
            AddLbl("Клиент *:", y);
            cmbClient = AddCombo(y); y += 34;

            // Статус
            AddLbl("Статус *:", y);
            cmbStatus = AddCombo(y); y += 34;

            // Адрес доставки
            AddLbl("Адрес доставки:", y);
            txtAddress = new TextBox { Left = 155, Top = y, Width = 285, Height = 48, Multiline = true };
            Controls.Add(txtAddress);
            y += 56;

            // Дата доставки
            AddLbl("Дата доставки:", y);
            chkDelivery = new CheckBox { Left = 151, Top = y + 3, Width = 16 };
            Controls.Add(chkDelivery);
            dtpDelivery = new DateTimePicker
            { Left = 172, Top = y, Width = 200, Enabled = false, Format = DateTimePickerFormat.Short };
            chkDelivery.CheckedChanged += (s, e) => dtpDelivery.Enabled = chkDelivery.Checked;
            Controls.Add(dtpDelivery);
            y += 34;

            // Примечания
            AddLbl("Примечания:", y);
            txtNotes = new TextBox
            { Left = 155, Top = y, Width = 285, Height = 48, Multiline = true, ScrollBars = ScrollBars.Vertical };
            Controls.Add(txtNotes);
            y += 58;

            // Кнопки
            btnSave = new Button
            {
                Text = _edit == null ? "Создать" : "Сохранить",
                Left = 155, Top = y, Width = 130, Height = 32,
                BackColor = Color.FromArgb(26, 58, 92),
                ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button
            { Text = "Отмена", Left = 295, Top = y, Width = 130, Height = 32, FlatStyle = FlatStyle.Flat };
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadCombos()
        {
            cmbClient.DisplayMember = "CompanyName";
            cmbClient.ValueMember   = "ClientID";
            cmbClient.DataSource    = _db.GetClients();

            cmbStatus.DisplayMember = "StatusName";
            cmbStatus.ValueMember   = "StatusID";
            cmbStatus.DataSource    = _db.GetStatuses();
            if (cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = 0;
        }

        private void FillFields()
        {
            SelectById(cmbClient, _edit.ClientID);
            SelectById(cmbStatus, _edit.StatusID);
            txtAddress.Text = _edit.DeliveryAddress;
            txtNotes.Text   = _edit.Notes;
            if (_edit.DeliveryDate.HasValue)
            { chkDelivery.Checked = true; dtpDelivery.Value = _edit.DeliveryDate.Value; }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (cmbClient.SelectedItem == null)
            { Warn("Выберите клиента."); return; }

            try
            {
                var o = new Order
                {
                    OrderID         = _edit?.OrderID ?? 0,
                    ClientID        = (int)cmbClient.SelectedValue,
                    StatusID        = (int)cmbStatus.SelectedValue,
                    DeliveryAddress = txtAddress.Text.Trim(),
                    DeliveryDate    = chkDelivery.Checked ? dtpDelivery.Value : (DateTime?)null,
                    Notes           = txtNotes.Text.Trim()
                };

                if (_edit == null) _db.AddOrder(o);
                else               _db.UpdateOrder(o);

                DialogResult = DialogResult.OK;
            }
            catch (Exception ex) { Warn("Ошибка сохранения:\n" + ex.Message); }
        }

       
        private void AddLbl(string text, int top) =>
            Controls.Add(new Label
            {
                Text = text, Left = 12, Top = top + 3, Width = 140,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.FromArgb(70, 80, 100)
            });

        private ComboBox AddCombo(int top)
        {
            var c = new ComboBox { Left = 155, Top = top, Width = 285, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(c);
            return c;
        }

        private void SelectById(ComboBox cmb, int id)
        {
            for (int i = 0; i < cmb.Items.Count; i++)
            {
                var item = cmb.Items[i];
                int v = item is Client cl ? cl.ClientID
                      : item is OrderStatus st ? st.StatusID : 0;
                if (v == id) { cmb.SelectedIndex = i; return; }
            }
        }

        private void Warn(string msg) =>
            MessageBox.Show(msg, "Проверьте данные", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
