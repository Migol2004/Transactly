using System;
using System.Windows.Forms;
using Transactly;

namespace Transactly
{
    public partial class EditProductForm : Form
    {
        public Product ProductToEdit { get; private set; }
        private DatabaseManager _dbManager;

        public EditProductForm(Product product, DatabaseManager dbManager)
        {
            InitializeComponent();
            ProductToEdit = product;
            _dbManager = dbManager;
            InitializeControls();
            LoadProductData();
            LoadCategories();
        }

        private void InitializeControls()
        {
            // Form setup
            this.Text = "Edit Product";
            this.Size = new System.Drawing.Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Labels and TextBoxes
            int startY = 20;
            int labelWidth = 100;
            int controlWidth = 200;
            int spacing = 30;

            Label lblName = new Label() { Text = "Name:", Location = new System.Drawing.Point(30, startY), Size = new System.Drawing.Size(labelWidth, 20) };
            this.txtName = new TextBox() { Location = new System.Drawing.Point(140, startY), Size = new System.Drawing.Size(controlWidth, 20) };

            Label lblPrice = new Label() { Text = "Price:", Location = new System.Drawing.Point(30, startY + spacing), Size = new System.Drawing.Size(labelWidth, 20) };
            this.txtPrice = new TextBox() { Location = new System.Drawing.Point(140, startY + spacing), Size = new System.Drawing.Size(controlWidth, 20) };

            Label lblCategory = new Label() { Text = "Category:", Location = new System.Drawing.Point(30, startY + spacing * 2), Size = new System.Drawing.Size(labelWidth, 20) };
            this.cmbCategory = new ComboBox() { Location = new System.Drawing.Point(140, startY + spacing * 2), Size = new System.Drawing.Size(controlWidth, 20), DropDownStyle = ComboBoxStyle.DropDownList };

            Label lblImagePath = new Label() { Text = "Image Path:", Location = new System.Drawing.Point(30, startY + spacing * 3), Size = new System.Drawing.Size(labelWidth, 20) };
            this.txtImagePath = new TextBox() { Location = new System.Drawing.Point(140, startY + spacing * 3), Size = new System.Drawing.Size(controlWidth, 20) };

            Label lblStock = new Label() { Text = "Stock:", Location = new System.Drawing.Point(30, startY + spacing * 4), Size = new System.Drawing.Size(labelWidth, 20) };
            this.txtStock = new TextBox() { Location = new System.Drawing.Point(140, startY + spacing * 4), Size = new System.Drawing.Size(controlWidth, 20) };

            // Buttons
            this.btnSave = new Button() { Text = "Save", Location = new System.Drawing.Point(140, startY + spacing * 5 + 10), Size = new System.Drawing.Size(80, 30) };
            this.btnCancel = new Button() { Text = "Cancel", Location = new System.Drawing.Point(230, startY + spacing * 5 + 10), Size = new System.Drawing.Size(80, 30) };

            // Add controls to form
            this.Controls.AddRange(new Control[] { 
                lblName, this.txtName,
                lblPrice, this.txtPrice,
                lblCategory, this.cmbCategory,
                lblImagePath, this.txtImagePath,
                lblStock, this.txtStock,
                this.btnSave, this.btnCancel
            });

            // Event Handlers
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += BtnCancel_Click;
        }

        private void LoadProductData()
        {
            if (ProductToEdit != null)
            {
                txtName.Text = ProductToEdit.Name;
                txtPrice.Text = ProductToEdit.Price.ToString();
                // Category will be set after loading categories
                txtImagePath.Text = ProductToEdit.ImagePath;
                txtStock.Text = ProductToEdit.Stock.ToString();
            }
        }

        private void LoadCategories()
        {
            var categories = _dbManager.GetCategories();
            cmbCategory.Items.AddRange(categories.ToArray());
            // Select the product's current category if it exists in the list
            if (!string.IsNullOrEmpty(ProductToEdit.Category) && cmbCategory.Items.Contains(ProductToEdit.Category))
            {
                cmbCategory.SelectedItem = ProductToEdit.Category;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Validate input (basic validation)
            if (string.IsNullOrWhiteSpace(txtName.Text) || 
                string.IsNullOrWhiteSpace(txtPrice.Text) ||
                cmbCategory.SelectedItem == null ||
                string.IsNullOrWhiteSpace(txtStock.Text))
            {
                MessageBox.Show("Please fill in all fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!decimal.TryParse(txtPrice.Text, out decimal price) || price < 0)
            {
                MessageBox.Show("Please enter a valid price.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(txtStock.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Please enter a valid stock quantity.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Update the Product object
            ProductToEdit.Name = txtName.Text.Trim();
            ProductToEdit.Price = price;
            ProductToEdit.Category = cmbCategory.SelectedItem.ToString(); // Assuming category names are unique
            ProductToEdit.ImagePath = txtImagePath.Text.Trim();
            ProductToEdit.Stock = stock;

            // Call the UpdateProduct method
            if (_dbManager.UpdateProduct(ProductToEdit))
            {
                MessageBox.Show("Product updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK; // Indicate success
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to update product.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; // Indicate cancel
            this.Close();
        }
    }
} 