using System;
using System.Drawing;
using System.Windows.Forms;

namespace Transactly
{
    public class PaymentDialog : Form
    {
        private TextBox amountTextBox = null!;
        private Label changeLabel = null!;
        private Button confirmButton = null!;
        private readonly decimal _totalAmount;
        
        public decimal AmountPaid { get; private set; }
        public decimal Change { get; private set; }
        
        public PaymentDialog(decimal totalAmount)
        {
            _totalAmount = totalAmount;
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            // Form settings
            this.Text = "Payment";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(400, 300);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;
            this.BackColor = Color.White;
            
            // Main container
            TableLayoutPanel mainLayout = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(20)
            };
            
            // Set row styles
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            
            // Total amount label
            Label totalLabel = new()
            {
                Text = $"Total Amount: ${_totalAmount:F2}",
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(97, 138, 61) // Darker green
            };
            mainLayout.Controls.Add(totalLabel, 0, 0);
            
            // Amount paid input panel
            Panel amountPanel = new()
            {
                Dock = DockStyle.Fill
            };
            
            Label amountLabel = new()
            {
                Text = "Amount Received:",
                Font = new Font("Arial", 12),
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(0, 10),
                Size = new Size(150, 25)
            };
            amountPanel.Controls.Add(amountLabel);
            
            amountTextBox = new()
            {
                Font = new Font("Arial", 14),
                Location = new Point(155, 5),
                Size = new Size(180, 35),
                TextAlign = HorizontalAlignment.Right
            };
            amountTextBox.KeyPress += AmountTextBox_KeyPress;
            amountTextBox.TextChanged += AmountTextBox_TextChanged;
            amountPanel.Controls.Add(amountTextBox);
            
            mainLayout.Controls.Add(amountPanel, 0, 1);
            
            // Change display
            Panel changePanel = new()
            {
                Dock = DockStyle.Fill
            };
            
            Label changeTitleLabel = new()
            {
                Text = "Change:",
                Font = new Font("Arial", 12),
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(0, 10),
                Size = new Size(150, 25)
            };
            changePanel.Controls.Add(changeTitleLabel);
            
            changeLabel = new()
            {
                Text = "$0.00",
                Font = new Font("Arial", 14, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(155, 5),
                Size = new Size(180, 35),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.LightYellow
            };
            changePanel.Controls.Add(changeLabel);
            
            mainLayout.Controls.Add(changePanel, 0, 2);
            
            // Buttons panel
            Panel buttonsPanel = new()
            {
                Dock = DockStyle.Fill
            };
            
            confirmButton = new()
            {
                Text = "Confirm",
                DialogResult = DialogResult.OK,
                Enabled = false,
                Size = new Size(120, 40),
                Location = new Point(30, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#B4E67E"),
                Font = new Font("Arial", 12, FontStyle.Bold)
            };
            buttonsPanel.Controls.Add(confirmButton);
            
            Button cancelButton = new()
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Size = new Size(120, 40),
                Location = new Point(170, 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.LightGray,
                Font = new Font("Arial", 12)
            };
            buttonsPanel.Controls.Add(cancelButton);
            
            mainLayout.Controls.Add(buttonsPanel, 0, 3);
            
            this.Controls.Add(mainLayout);
            this.AcceptButton = confirmButton;
            this.CancelButton = cancelButton;
        }
        
        private void AmountTextBox_TextChanged(object? sender, EventArgs e)
        {
            try
            {
                if (decimal.TryParse(amountTextBox.Text, out decimal amount))
                {
                    // Calculate change
                    decimal change = amount - _totalAmount;
                    
                    // Update display
                    changeLabel.Text = $"${change:F2}";
                    changeLabel.ForeColor = change >= 0 ? Color.Black : Color.Red;
                    
                    // Enable/disable confirm button
                    confirmButton.Enabled = amount >= _totalAmount;
                    
                    // Store values
                    AmountPaid = amount;
                    Change = Math.Max(0, change); // Ensure change is not negative
                }
                else
                {
                    changeLabel.Text = "$0.00";
                    changeLabel.ForeColor = Color.Black;
                    confirmButton.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calculating change: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void AmountTextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Allow only digits, decimal point, and control characters
            bool isDigit = char.IsDigit(e.KeyChar);
            bool isControl = char.IsControl(e.KeyChar);
            bool isDecimalPoint = e.KeyChar == '.';
            
            // Check if there's already a decimal point in the text
            bool alreadyHasDecimal = amountTextBox.Text.Contains('.');
            
            if (!isDigit && !isControl && !(isDecimalPoint && !alreadyHasDecimal))
            {
                e.Handled = true; // Block the character
            }
        }
    }
} 