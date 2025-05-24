using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Printing;
using System.Text;

namespace Transactly
{
    public class ReceiptsForm : Form
    {
        private ListView receiptsList = null!;
        private ListView receiptItems = null!;
        private Label receiptDetailsLabel = null!;
        private readonly DatabaseManager _dbManager;
        private SplitContainer mainSplit = null!;
        private ImageList productImages = null!;
        private Receipt? currentReceipt = null;
        
        public ReceiptsForm(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            InitializeComponent();
            InitializeImageList();
            this.Load += ReceiptsForm_Load;
        }
        
        private void InitializeImageList()
        {
            // Create an image list for product icons
            productImages = new ImageList();
            productImages.ImageSize = new Size(32, 32);
            productImages.ColorDepth = ColorDepth.Depth32Bit;
            
            try
            {
                // Path to the product images folder
                string imagesFolder = Path.Combine(Application.StartupPath, "ProductImages");
                
                // Create the directory if it doesn't exist
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }

                Console.WriteLine($"Looking for images in: {imagesFolder}");
                var existingFiles = Directory.GetFiles(imagesFolder, "*.png");
                Console.WriteLine($"Found {existingFiles.Length} PNG files in the directory:");
                foreach(var file in existingFiles)
                {
                    Console.WriteLine($" - {Path.GetFileName(file)}");
                }
                
                // Force clear any existing images to ensure reload
                productImages.Images.Clear();
                
                // Add default image first
                string defaultImagePath = Path.Combine(imagesFolder, "default.png");
                if (File.Exists(defaultImagePath))
                {
                    Console.WriteLine($"Loading default image from: {defaultImagePath}");
                    try
                    {
                        using (var img = Image.FromFile(defaultImagePath))
                        {
                            productImages.Images.Add("default", new Bitmap(img));
                            Console.WriteLine("Default image loaded successfully");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading default image: {ex.Message}");
                        productImages.Images.Add("default", CreateColoredIcon(Color.LightGray, "?"));
                    }
                }
                else
                {
                    Console.WriteLine("Default image not found, using colored icon");
                    // If default image doesn't exist, create a basic one
                    productImages.Images.Add("default", CreateColoredIcon(Color.LightGray, "?"));
                }
                
                // List of product types we want to load images for
                string[] productTypes = new[] {
                    "Candy", "Chips", "Chocolate", "Coffee", "Cookies", "Crackers", 
                    "Energy", "Fruit", "Granola", "Juice", "Lemonade", "Nuts", 
                    "Popcorn", "Pretzels", "Protein", "Soda", "Tea", "Trail", "Water"
                };
                
                // Load images for each product type from PNG files
                foreach (string productType in productTypes)
                {
                    string imagePath = Path.Combine(imagesFolder, $"{productType}.png");
                    
                    if (File.Exists(imagePath))
                    {
                        Console.WriteLine($"Found image for {productType} at: {imagePath}");
                        try
                        {
                            // Load the PNG image directly from file rather than using Bitmap constructor
                            using (var img = Image.FromFile(imagePath))
                            {
                                productImages.Images.Add(productType, new Bitmap(img));
                                Console.WriteLine($"{productType} image loaded successfully");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error loading {productType} image: {ex.Message}");
                            // If loading fails, fall back to colored icon
                            Color iconColor = GetColorForProductType(productType);
                            string iconText = productType.Substring(0, 1);
                            productImages.Images.Add(productType, CreateColoredIcon(iconColor, iconText));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"No image file found for {productType}");
                        // Create a fallback colored icon if the PNG doesn't exist
                        Color iconColor = GetColorForProductType(productType);
                        string iconText = productType.Substring(0, 1);
                        productImages.Images.Add(productType, CreateColoredIcon(iconColor, iconText));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading product images: {ex.Message}");
            }
        }
        
        private Color GetColorForProductType(string productType)
        {
            return productType.ToLower() switch
            {
                "candy" => Color.Pink,
                "chips" => Color.Yellow,
                "chocolate" => Color.Brown,
                "coffee" => Color.SaddleBrown,
                "cookies" => Color.Tan,
                "crackers" => Color.Wheat,
                "energy" => Color.Red,
                "fruit" => Color.LightGreen,
                "granola" => Color.Peru,
                "juice" => Color.Orange,
                "lemonade" => Color.Yellow,
                "nuts" => Color.SandyBrown,
                "popcorn" => Color.Ivory,
                "pretzels" => Color.Beige,
                "protein" => Color.Khaki,
                "soda" => Color.Red,
                "tea" => Color.Green,
                "trail" => Color.RosyBrown,
                "water" => Color.Azure,
                _ => Color.LightGray
            };
        }
        
        // Helper method to create colored icons with text
        private Bitmap CreateColoredIcon(Color color, string text)
        {
            Bitmap bmp = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Fill background
                g.FillRectangle(new SolidBrush(color), 0, 0, 32, 32);
                
                // Add border
                g.DrawRectangle(new Pen(Color.Black, 1), 1, 1, 30, 30);
                
                // Add text
                using (Font font = new Font("Arial", 16, FontStyle.Bold))
                {
                    SizeF textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, Brushes.Black, 
                        (32 - textSize.Width) / 2, 
                        (32 - textSize.Height) / 2);
                }
            }
            return bmp;
        }
        
        private void ReceiptsForm_Load(object? sender, EventArgs e)
        {
            try
            {
                // Set splitter distance after form loads to avoid initialization errors
                if (mainSplit != null && mainSplit.Width > 100)
                {
                    mainSplit.SplitterDistance = mainSplit.Width / 3; // Will be ~400px for a 1200px wide window
                }
                
                // Set the image list for the receipt items ListView
                try
                {
                    if (productImages != null && productImages.Images.Count > 0 && receiptItems != null)
                    {
                        receiptItems.SmallImageList = productImages;
                    }
                }
                catch (Exception ex)
                {
                    // Just log the error but continue without images if there's a problem
                    Console.WriteLine($"Error setting image list: {ex.Message}");
                }
                
                LoadReceipts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing receipt form: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void InitializeComponent()
        {
            // Form settings
            this.Text = "Transaction Receipts";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(1200, 600);
            this.MinimumSize = new Size(1000, 500);
            this.WindowState = FormWindowState.Maximized;
            
            // Split layout - receipts list on left, details on right
            mainSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical, 
                Panel1MinSize = 100,
                Panel2MinSize = 100
            };
            
            // Receipts list panel (left side)
            Panel receiptsPanel = new()
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(5, 5, 5, 5)
            };
            mainSplit.Panel1.Controls.Add(receiptsPanel);

            Label receiptsHeaderLabel = new()
            {
                Text = "All Transactions",
                Dock = DockStyle.Bottom,
                Font = new Font("Arial", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(97, 138, 61)
            };

            receiptsPanel.Controls.Add(receiptsHeaderLabel);
            
            receiptsList = new()
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("Arial", 12),
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // Define columns with more explicit widths and clear labels
            receiptsList.Columns.Clear(); // Clear any existing columns
            receiptsList.Columns.Add("ID", 50);
            receiptsList.Columns.Add("Date", 120);
            receiptsList.Columns.Add("Time", 80);
            receiptsList.Columns.Add("Total", 80);
            receiptsList.Columns.Add("Paid", 80);
            receiptsList.Columns.Add("Change", 80);
            
            // Ensure the header is visible
            receiptsList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            
            receiptsList.SelectedIndexChanged += ReceiptsList_SelectedIndexChanged;
            receiptsPanel.Controls.Add(receiptsList);
            
            // Receipt details panel (right side)
            Panel detailsPanel = new()
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            mainSplit.Panel2.Controls.Add(detailsPanel);
            
            // Add a print/export button
            Button printButton = new()
            {
                Text = "Print Receipt",
                Dock = DockStyle.Bottom,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorTranslator.FromHtml("#B4E67E"),
                Font = new Font("Arial", 12, FontStyle.Bold),
                Margin = new Padding(0, 10, 0, 0)
            };
            printButton.Click += PrintButton_Click;
            detailsPanel.Controls.Add(printButton);
            
            // Add delete button
            Button deleteButton = new()
            {
                Text = "Delete Receipt",
                Dock = DockStyle.Bottom,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                Font = new Font("Arial", 12, FontStyle.Bold),
                Margin = new Padding(0, 10, 0, 5)
            };
            deleteButton.Click += DeleteButton_Click;
            detailsPanel.Controls.Add(deleteButton);
            
            // List view for receipt items - Changed to details view instead of LargeIcon
            receiptItems = new()
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("Arial", 12),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White
            };
            
            // Add columns for receipt items
            receiptItems.Columns.Add("", 40); // Image column
            receiptItems.Columns.Add("Item Description", 360);
            receiptItems.Columns.Add("Amount", 100);
            
            detailsPanel.Controls.Add(receiptItems);
            
            // Add padding panel to push content down
            Panel spacerPanel = new()
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White
            };
            detailsPanel.Controls.Add(spacerPanel);
            
            // Header for receipt details
            receiptDetailsLabel = new()
            {
                Text = "Receipt Details",
                Dock = DockStyle.Top,
                Font = new Font("Arial", 14, FontStyle.Bold),
                Height = 30,
                ForeColor = Color.FromArgb(97, 138, 61),
                Padding = new Padding(5, 0, 0, 5)
            };
            detailsPanel.Controls.Add(receiptDetailsLabel);
            
            this.Controls.Add(mainSplit);
        }
        
        private void LoadReceipts()
        {
            try
            {
                receiptsList.Items.Clear();
                receiptItems.Items.Clear();
                
                List<Receipt> receipts = _dbManager.GetAllReceipts();
                
                foreach (Receipt receipt in receipts)
                {
                    string[] row = 
                    {
                        receipt.ReceiptId.ToString(),
                        receipt.Date.ToShortDateString(),
                        receipt.Date.ToShortTimeString(),
                        $"${receipt.Total:F2}",
                        $"${receipt.AmountPaid:F2}",
                        $"${receipt.Change:F2}"
                    };
                    
                    ListViewItem item = new(row);
                    item.Tag = receipt.ReceiptId;
                    receiptsList.Items.Add(item);
                }
                
                // Auto-resize columns
                foreach (ColumnHeader column in receiptsList.Columns)
                {
                    column.Width = -2; // Auto-size to header and content
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading receipts: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ReceiptsList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            try
            {
                receiptItems.Items.Clear();
                
                if (receiptsList.SelectedItems.Count == 0)
                {
                    currentReceipt = null;
                    return;
                }
                
                ListViewItem selected = receiptsList.SelectedItems[0];
                int receiptId = (int)selected.Tag;
                
                currentReceipt = _dbManager.GetReceiptById(receiptId);
                if (currentReceipt != null)
                {
                    // Update header with receipt details
                    receiptDetailsLabel.Text = $"Receipt #{currentReceipt.ReceiptId} - {currentReceipt.Date}";
                    
                    // Add items to the list with images
                    foreach (CartItem item in currentReceipt.Items)
                    {
                        string imageKey = "default"; // Default image key
                        
                        // Find the appropriate image based on product name
                        string productName = item.Product.Name.ToLower();
                        if (productName.Contains("candy")) imageKey = "Candy";
                        else if (productName.Contains("chips")) imageKey = "Chips";
                        else if (productName.Contains("chocolate")) imageKey = "Chocolate";
                        else if (productName.Contains("coffee")) imageKey = "Coffee";
                        else if (productName.Contains("cookies")) imageKey = "Cookies";
                        else if (productName.Contains("crackers")) imageKey = "Crackers";
                        else if (productName.Contains("energy")) imageKey = "Energy";
                        else if (productName.Contains("fruit")) imageKey = "Fruit";
                        else if (productName.Contains("granola")) imageKey = "Granola";
                        else if (productName.Contains("juice")) imageKey = "Juice";
                        else if (productName.Contains("lemonade")) imageKey = "Lemonade";
                        else if (productName.Contains("nuts")) imageKey = "Nuts";
                        else if (productName.Contains("popcorn")) imageKey = "Popcorn";
                        else if (productName.Contains("pretzels")) imageKey = "Pretzels";
                        else if (productName.Contains("protein")) imageKey = "Protein";
                        else if (productName.Contains("soda")) imageKey = "Soda";
                        else if (productName.Contains("tea")) imageKey = "Tea";
                        else if (productName.Contains("trail")) imageKey = "Trail";
                        else if (productName.Contains("water")) imageKey = "Water";
                        
                        // Create list item with image
                        ListViewItem listItem = new ListViewItem("", imageKey);
                        listItem.SubItems.Add($"{item.Product.Name} - ${item.Product.Price:F2} x {item.Quantity}");
                        listItem.SubItems.Add($"${item.TotalPrice:F2}");
                        
                        receiptItems.Items.Add(listItem);
                    }
                    
                    // Add summary items
                    ListViewItem totalItem = new ListViewItem("", "default");
                    totalItem.SubItems.Add("TOTAL");
                    totalItem.SubItems.Add($"${currentReceipt.Total:F2}");
                    totalItem.Font = new Font(receiptItems.Font, FontStyle.Bold);
                    receiptItems.Items.Add(totalItem);
                    
                    ListViewItem paidItem = new ListViewItem("", "default");
                    paidItem.SubItems.Add("Amount Paid");
                    paidItem.SubItems.Add($"${currentReceipt.AmountPaid:F2}");
                    receiptItems.Items.Add(paidItem);
                    
                    ListViewItem changeItem = new ListViewItem("", "default");
                    changeItem.SubItems.Add("Change");
                    changeItem.SubItems.Add($"${currentReceipt.Change:F2}");
                    receiptItems.Items.Add(changeItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading receipt details: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void PrintButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (currentReceipt == null)
                {
                    MessageBox.Show("Please select a receipt to print.", "Information", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                // Create a PrintDocument object
                PrintDocument printDoc = new PrintDocument();
                printDoc.PrintPage += PrintDoc_PrintPage;
                
                // Create a PrintPreviewDialog to show a preview
                using (PrintPreviewDialog previewDlg = new PrintPreviewDialog())
                {
                    previewDlg.Document = printDoc;
                    previewDlg.Width = 800;
                    previewDlg.Height = 600;
                    previewDlg.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing receipt: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            try
            {
                if (currentReceipt == null)
                    return;
                    
                // Set up printing dimensions and positions
                float yPos = 80;
                float leftMargin = 50;
                float topMargin = 50;
                int width = (int)e.PageSettings.PrintableArea.Width;
                
                // Set up fonts
                Font titleFont = new Font("Arial", 14, FontStyle.Bold);
                Font headerFont = new Font("Arial", 12, FontStyle.Bold);
                Font contentFont = new Font("Arial", 10);
                Font itemFont = new Font("Arial", 10);
                
                // Use green brushes for text to save black ink
                Brush textBrush = new SolidBrush(Color.FromArgb(0, 128, 0)); // Dark green
                Pen linePen = new Pen(Color.FromArgb(0, 128, 0), 1);
                
                // Print title
                string title = $"Transactly - Receipt #{currentReceipt.ReceiptId}";
                e.Graphics.DrawString(title, titleFont, textBrush, leftMargin, topMargin);
                yPos += 30;
                
                // Print date
                e.Graphics.DrawString($"Date: {currentReceipt.Date}", headerFont, textBrush, leftMargin, yPos);
                yPos += 25;
                
                // Print a divider line
                e.Graphics.DrawLine(linePen, leftMargin, yPos, leftMargin + width - 100, yPos);
                yPos += 10;
                
                // Print column headers
                e.Graphics.DrawString("Item", headerFont, textBrush, leftMargin, yPos);
                e.Graphics.DrawString("Amount", headerFont, textBrush, leftMargin + 300, yPos);
                yPos += 20;
                
                // Print items
                foreach (CartItem item in currentReceipt.Items)
                {
                    string itemText = $"{item.Product.Name} - ${item.Product.Price:F2} x {item.Quantity}";
                    e.Graphics.DrawString(itemText, itemFont, textBrush, leftMargin, yPos);
                    e.Graphics.DrawString($"${item.TotalPrice:F2}", itemFont, textBrush, leftMargin + 300, yPos);
                    yPos += 20;
                }
                
                // Print a divider line
                yPos += 10;
                e.Graphics.DrawLine(linePen, leftMargin, yPos, leftMargin + width - 100, yPos);
                yPos += 20;
                
                // Print totals
                e.Graphics.DrawString("TOTAL:", headerFont, textBrush, leftMargin, yPos);
                e.Graphics.DrawString($"${currentReceipt.Total:F2}", headerFont, textBrush, leftMargin + 300, yPos);
                yPos += 20;
                
                e.Graphics.DrawString("Amount Paid:", contentFont, textBrush, leftMargin, yPos);
                e.Graphics.DrawString($"${currentReceipt.AmountPaid:F2}", contentFont, textBrush, leftMargin + 300, yPos);
                yPos += 20;
                
                e.Graphics.DrawString("Change:", contentFont, textBrush, leftMargin, yPos);
                e.Graphics.DrawString($"${currentReceipt.Change:F2}", contentFont, textBrush, leftMargin + 300, yPos);
                yPos += 40;
                
                // Thank you message
                e.Graphics.DrawString("Thank you for your purchase!", new Font("Arial", 11, FontStyle.Italic), 
                    textBrush, leftMargin, yPos);
                
                // Indicate that no more pages are needed
                e.HasMorePages = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating print document: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (currentReceipt == null)
                {
                    MessageBox.Show("Please select a receipt to delete.", "Information", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                
                DialogResult result = MessageBox.Show("Are you sure you want to delete this receipt?", "Confirm Deletion", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    _dbManager.DeleteReceipt(currentReceipt.ReceiptId);
                    MessageBox.Show("Receipt deleted successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadReceipts();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting receipt: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 