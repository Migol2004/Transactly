using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Data.SQLite;

namespace Transactly
{
    public class CartItem
    {
        public required Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice => Product.Price * Quantity;
    }

    public class Receipt
    {
        public int ReceiptId { get; set; }
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal Change { get; set; }
        public List<CartItem> Items { get; set; } = [];
    }

    public partial class MainForm : Form
    {
        // Windows API function to show/hide scrollbars
        [DllImport("user32.dll")]
        private static extern int ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        
        // ScrollBar constants
        private const int SB_HORZ = 0;
        private const int SB_VERT = 1;
        private const int SB_BOTH = 3;
        
        // Form controls - using required modifier with public access
        private Panel headerPanel = null!;
        private Label headerLabel = null!;
        private TextBox searchBox = null!;
        private Panel categoriesPanel = null!;
        private Button allButton = null!;
        private Button beveragesButton = null!;
        private Button snacksButton = null!;
        private Panel productsPanel = null!;
        private Panel checkoutPanel = null!;
        private TableLayoutPanel productsTable = null!;
        private DatabaseManager _dbManager = null!;
        private string _currentCategory = "";
        private ImageList productImages = null!;
        
        // Cart components
        private Panel cartHeaderPanel = null!;
        private ListView cartListView = null!;
        private Panel cartTotalPanel = null!;
        private Label totalLabel = null!;
        private Button checkoutButton = null!;
        private Button clearCartButton = null!;
        private List<CartItem> _cartItems = null!;
        
        // Add a ContextMenuStrip for product editing
        private ContextMenuStrip productContextMenu = null!;
        
        public MainForm()
        {
            try
            {
                InitializeComponent();
                
                // Initialize all required fields
                _dbManager = new DatabaseManager();
                _cartItems = [];
                
                InitializeProductImages();
                
                // Add diagnostic button in debug mode
                #if DEBUG
                AddDiagnosticButton();
                #endif
                
                // Create UI first without focusing on SplitContainer issues
                CreateUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing form: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeProductImages()
        {
            // Create an image list for product icons
            productImages = new ImageList();
            productImages.ImageSize = new Size(64, 64);
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
            Bitmap bmp = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Fill background with gradient for more visual appeal
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    new Rectangle(0, 0, 64, 64),
                    color,
                    Color.FromArgb(Math.Max(color.R - 40, 0), Math.Max(color.G - 40, 0), Math.Max(color.B - 40, 0)),
                    45f))
                {
                    g.FillRectangle(brush, 0, 0, 64, 64);
                }
                
                // Add border with subtle shadow effect
                g.DrawRectangle(new Pen(Color.FromArgb(60, 0, 0, 0), 1), 2, 2, 60, 60);
                g.DrawRectangle(new Pen(Color.Black, 2), 1, 1, 62, 62);
                
                // Add text with a subtle shadow for better visibility
                using (Font font = new Font("Arial", 24, FontStyle.Bold))
                {
                    // Draw shadow
                    SizeF textSize = g.MeasureString(text, font);
                    g.DrawString(text, font, new SolidBrush(Color.FromArgb(100, 0, 0, 0)), 
                        (64 - textSize.Width) / 2 + 1, 
                        (64 - textSize.Height) / 2 + 1);
                    
                    // Draw text
                    g.DrawString(text, font, Brushes.White, 
                        (64 - textSize.Width) / 2, 
                        (64 - textSize.Height) / 2);
                }
            }
            return bmp;
        }

        private void AddDiagnosticButton()
        {
            Button diagButton = new()
            {
                Text = "Diagnostic",
                Size = new(100, 30),
                Location = new(10, 10),
                BackColor = Color.Yellow
            };
            diagButton.Click += DiagnosticButton_Click;
            this.Controls.Add(diagButton);
        }

        private void DiagnosticButton_Click(object? sender, EventArgs e)
        {
            try
            {
                string dbPath = Path.Combine(Environment.CurrentDirectory, "transactly.db");
                string message = $"Database Path: {dbPath}\nExists: {File.Exists(dbPath)}\n\n";
                
                if (File.Exists(dbPath))
                {
                    // Check tables and counts
                    using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                    connection.Open();
                    
                    // Check Users table
                    using (var command = new SQLiteCommand("SELECT COUNT(*) FROM Users", connection))
                    {
                        int userCount = Convert.ToInt32(command.ExecuteScalar());
                        message += $"Users Table: {userCount} records\n";
                    }
                    
                    // Check Categories table
                    using (var command = new SQLiteCommand("SELECT COUNT(*) FROM Categories", connection))
                    {
                        int categoryCount = Convert.ToInt32(command.ExecuteScalar());
                        message += $"Categories Table: {categoryCount} records\n";
                        
                        // List Categories
                        using var catCommand = new SQLiteCommand("SELECT CategoryId, Name FROM Categories", connection);
                        using var reader = catCommand.ExecuteReader();
                        message += "Categories:\n";
                        while (reader.Read())
                        {
                            message += $"  ID: {reader["CategoryId"]}, Name: {reader["Name"]}\n";
                        }
                    }
                    
                    // Check Products table
                    using (var command = new SQLiteCommand("SELECT COUNT(*) FROM Products", connection))
                    {
                        int productCount = Convert.ToInt32(command.ExecuteScalar());
                        message += $"Products Table: {productCount} records\n";
                        
                        // List first 5 products
                        using var prodCommand = new SQLiteCommand("SELECT ProductId, Name, Price, CategoryId FROM Products LIMIT 5", connection);
                        using var reader = prodCommand.ExecuteReader();
                        message += "First 5 Products:\n";
                        while (reader.Read())
                        {
                            message += $"  ID: {reader["ProductId"]}, Name: {reader["Name"]}, Price: {reader["Price"]}, CategoryId: {reader["CategoryId"]}\n";
                        }
                    }
                }
                
                MessageBox.Show(message, "Database Diagnostics", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Force refresh products
                if (string.IsNullOrEmpty(_currentCategory))
                {
                    LoadProducts("");
                }
                else
                {
                    LoadProducts(_currentCategory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Diagnostic Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CreateUI()
        {
            try
            {
                // Form settings
                this.Text = "Transactly";
                this.StartPosition = FormStartPosition.CenterScreen;
                this.Size = new(1200, 768);
                this.WindowState = FormWindowState.Maximized;

                // Create the main layout using TableLayoutPanel instead of SplitContainer
                // This avoids SplitterDistance issues entirely
                TableLayoutPanel mainLayout = new()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 2,
                    RowCount = 1,
                };
                
                // Set column styles (left side 70%, right side 30%)
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
                mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
                mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                
                this.Controls.Add(mainLayout);

                // Left panel (products)
                Panel leftPanel = new()
                {
                    Dock = DockStyle.Fill,
                };
                mainLayout.Controls.Add(leftPanel, 0, 0);

                // Right panel (checkout)
                checkoutPanel = new()
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };
                mainLayout.Controls.Add(checkoutPanel, 1, 0);

                // Header panel
                headerPanel = new()
                {
                    Dock = DockStyle.Top,
                    Height = 50,
                    BackColor = ColorTranslator.FromHtml("#B4E67E") // Light green color as in the mockup
                };
                leftPanel.Controls.Add(headerPanel);

                // Header label
                headerLabel = new()
                {
                    Text = "TRANSACTLY",
                    Font = new Font("Arial", 20, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    ForeColor = Color.Black
                };
                headerPanel.Controls.Add(headerLabel);

                // Search box panel
                Panel searchPanel = new()
                {
                    Dock = DockStyle.Top,
                    Height = 40,
                    Padding = new Padding(10, 5, 10, 5),
                    BackColor = Color.White
                };
                leftPanel.Controls.Add(searchPanel);

                // Search box
                searchBox = new()
                {
                    Dock = DockStyle.Fill,
                    BorderStyle = BorderStyle.FixedSingle,
                    Font = new Font("Arial", 12),
                    PlaceholderText = "Search...",
                };
                searchBox.KeyDown += (s, e) => 
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        SearchProducts();
                        e.SuppressKeyPress = true;
                    }
                };
                searchPanel.Controls.Add(searchBox);

                // Categories panel with equal width buttons
                categoriesPanel = new()
                {
                    Dock = DockStyle.Top,
                    Height = 40,
                    BackColor = Color.White
                };
                leftPanel.Controls.Add(categoriesPanel);
                
                // Use a TableLayoutPanel for categories to ensure equal widths
                TableLayoutPanel categoryTable = new()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3,
                    RowCount = 1,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
                };
                
                // Set equal column widths
                categoryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                categoryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                categoryTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                categoryTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                
                categoriesPanel.Controls.Add(categoryTable);

                // ALL category button
                allButton = new()
                {
                    Text = "ALL",
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(97, 138, 61), // Darker green for selected category
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    ForeColor = Color.White
                };
                allButton.FlatAppearance.BorderSize = 0;
                categoryTable.Controls.Add(allButton, 0, 0);

                // Beverages category button
                beveragesButton = new()
                {
                    Text = "Beverages",
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ColorTranslator.FromHtml("#B4E67E"),
                    Font = new Font("Arial", 12, FontStyle.Bold)
                };
                beveragesButton.FlatAppearance.BorderSize = 0;
                categoryTable.Controls.Add(beveragesButton, 1, 0);

                // Snacks category button
                snacksButton = new()
                {
                    Text = "Snacks",
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ColorTranslator.FromHtml("#B4E67E"),
                    Font = new Font("Arial", 12, FontStyle.Bold)
                };
                snacksButton.FlatAppearance.BorderSize = 0;
                categoryTable.Controls.Add(snacksButton, 2, 0);

                // Products panel (grid layout)
                productsPanel = new()
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    AutoScroll = true,
                    Padding = new Padding(10),
                    BorderStyle = BorderStyle.None
                };

                // Explicitly configure scrolling behavior
                productsPanel.HorizontalScroll.Maximum = 0;
                productsPanel.HorizontalScroll.Visible = false;
                productsPanel.VerticalScroll.Visible = true;
                productsPanel.VerticalScroll.Enabled = true;
                productsPanel.AutoScrollMinSize = new Size(0, 1); // Force vertical scrollbar to be available

                leftPanel.Controls.Add(productsPanel);

                // Force always visible vertical scrollbar after panel is created and shown
                this.Shown += (s, e) => {
                    if (productsPanel.IsHandleCreated)
                    {
                        _ = ShowScrollBar(productsPanel.Handle, SB_VERT, true);
                        _ = ShowScrollBar(productsPanel.Handle, SB_HORZ, false);
                        
                        // Reset scroll position to ensure first items are visible
                        productsPanel.AutoScrollPosition = new Point(0, 0);
                    }
                };

                // Create table layout for products grid (7 columns as in the mockup)
                productsTable = new()
                {
                    Dock = DockStyle.Top, // Using Top instead of Fill to ensure all content is visible
                    ColumnCount = 7, // 7 columns as shown in the mockup
                    RowCount = 0,
                    AutoSize = true,
                    CellBorderStyle = TableLayoutPanelCellBorderStyle.Outset, // Changed to Outset for more spacing
                    Margin = new Padding(5),
                    Padding = new Padding(5)
                };

                // Set equal column widths
                for (int i = 0; i < productsTable.ColumnCount; i++)
                {
                    productsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / productsTable.ColumnCount));
                }

                productsPanel.Controls.Add(productsTable);

                // Initialize the cart UI
                InitializeCartUI();

                // Setup event handlers for category buttons
                allButton.Click += (s, e) => SwitchCategory("");
                beveragesButton.Click += (s, e) => SwitchCategory("Beverages");
                snacksButton.Click += (s, e) => SwitchCategory("Snacks");
                
                // Create the context menu
                productContextMenu = new ContextMenuStrip();
                ToolStripMenuItem editProductMenuItem = new ToolStripMenuItem("Edit Product");
                editProductMenuItem.Click += EditProductMenuItem_Click;
                productContextMenu.Items.Add(editProductMenuItem);

                // Initial category selection
                SwitchCategory("");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CreateUI: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void InitializeCartUI()
        {
            try
            {
                if (checkoutPanel == null)
                    return;
                    
                checkoutPanel.Padding = new Padding(5);

                // Create a cart layout with 3 rows
                TableLayoutPanel cartLayout = new()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 1,
                    RowCount = 3
                };
                
                // Set row styles - header, list view, and totals
                cartLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Header
                cartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // List (fills space)
                cartLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));  // Totals panel
                
                checkoutPanel.Controls.Add(cartLayout);

                // Cart header panel with cart label
                Panel headerPanel = new()
                {
                    Dock = DockStyle.Fill,
                    BackColor = ColorTranslator.FromHtml("#B4E67E")
                };
                
                Label cartLabel = new()
                {
                    Text = "SHOPPING CART",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                
                headerPanel.Controls.Add(cartLabel);
                cartHeaderPanel = headerPanel; // Save reference
                
                // ---- CART LIST VIEW SECTION ----
                // Create container panel for list view
                Panel listContainer = new()
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.Fixed3D, // More visible border
                    Margin = new Padding(5)
                };
                cartLayout.Controls.Add(listContainer, 0, 1); // Add to second cell
                
                // Create the list view with clear styling
                cartListView = new()
                {
                    Dock = DockStyle.Fill,
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    Font = new Font("Arial", 12), // Bigger font
                    HeaderStyle = ColumnHeaderStyle.Nonclickable,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None
                };
                
                // Add columns with fixed widths
                cartListView.Columns.Add("Product", 150);
                cartListView.Columns.Add("Price", 80);
                cartListView.Columns.Add("Qty", 50);
                cartListView.Columns.Add("Total", 100);
                
                listContainer.Controls.Add(cartListView);
                
                // Context menu for right-clicking cart items
                ContextMenuStrip cartContextMenu = new();
                ToolStripMenuItem removeItem = new("Remove Item");
                removeItem.Click += RemoveCartItem;
                
                ToolStripMenuItem increaseQuantity = new("Increase Quantity");
                increaseQuantity.Click += IncreaseItemQuantity;
                
                ToolStripMenuItem decreaseQuantity = new("Decrease Quantity");
                decreaseQuantity.Click += DecreaseItemQuantity;
                
                cartContextMenu.Items.AddRange(
                [
                    removeItem, 
                    increaseQuantity, 
                    decreaseQuantity 
                ]);
                cartListView.ContextMenuStrip = cartContextMenu;
                
                // ---- FOOTER SECTION (TOTAL & BUTTONS) ----
                Panel footerPanel = new()
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Margin = new Padding(0, 5, 0, 0)
                };
                cartLayout.Controls.Add(footerPanel, 0, 2); // Add to third cell
                cartTotalPanel = footerPanel; // Save reference
                
                // Create a layout for the footer
                TableLayoutPanel footerLayout = new()
                {
                    Dock = DockStyle.Fill,
                    RowCount = 2,
                    ColumnCount = 1
                };

                // Set row styles - total label and buttons. Adjusted percentages to move buttons higher.
                footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30)); // Total label - increased from 20 to 30
                footerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70)); // Buttons - decreased from 80 to 70

                footerPanel.Controls.Add(footerLayout);
                
                // Total label
                totalLabel = new()
                {
                    Text = "Total: ₱0.00",
                    Font = new Font("Arial", 14, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleRight,
                    Dock = DockStyle.Fill,
                    Padding = new Padding(0, 5, 15, 0),
                    BackColor = Color.FromArgb(240, 240, 240) // Light gray background
                };
                footerLayout.Controls.Add(totalLabel, 0, 0);
                
                // Button container
                Panel buttonPanel = new()
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(10)
                };
                footerLayout.Controls.Add(buttonPanel, 0, 1);

                // Create a TableLayoutPanel for the buttons to distribute them evenly
                TableLayoutPanel buttonTableLayout = new()
                {
                    Dock = DockStyle.Fill,
                    ColumnCount = 3, // Three columns for three buttons
                    RowCount = 1,
                };
                // Configure column styles: three equal-width columns
                buttonTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                buttonTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
                buttonTableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F)); // Slight adjustment for rounding

                buttonPanel.Controls.Add(buttonTableLayout);

                // Checkout button
                checkoutButton = new()
                {
                    Text = "Checkout",
                    Dock = DockStyle.Fill, // Make button fill its table cell
                    // Remove explicit Size, Margin is enough for spacing
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ColorTranslator.FromHtml("#B4E67E"),
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    Enabled = false,
                    Margin = new Padding(5)
                };
                checkoutButton.FlatAppearance.BorderSize = 1;
                checkoutButton.Click += CheckoutButtonClick;
                // Add to the first column
                buttonTableLayout.Controls.Add(checkoutButton, 0, 0);

                // View Receipts button
                Button receiptsButton = new()
                {
                    Text = "Receipts",
                    Dock = DockStyle.Fill, // Make button fill its table cell
                    // Remove explicit Size, Margin is enough for spacing
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.LightSteelBlue,
                    Font = new Font("Arial", 12),
                    Margin = new Padding(5)
                };
                receiptsButton.FlatAppearance.BorderSize = 1;
                receiptsButton.Click += ViewReceiptsClick;
                // Add to the second (middle) column
                buttonTableLayout.Controls.Add(receiptsButton, 1, 0);

                // Clear cart button
                clearCartButton = new()
                {
                    Text = "Clear Cart",
                    Dock = DockStyle.Fill, // Make button fill its table cell
                    // Remove explicit Size, Margin is enough for spacing
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.LightGray,
                    Font = new Font("Arial", 12),
                    Enabled = false,
                    Margin = new Padding(5)
                };
                clearCartButton.FlatAppearance.BorderSize = 1;
                clearCartButton.Click += ClearCartButtonClick;
                // Add to the third column
                buttonTableLayout.Controls.Add(clearCartButton, 2, 0);

                // Adjust column widths when the checkout panel is resized
                checkoutPanel.Resize += (s, e) => {
                    if (cartListView != null && cartListView.Columns.Count >= 4)
                    {
                        int availWidth = cartListView.ClientSize.Width;
                        cartListView.Columns[0].Width = (int)(availWidth * 0.45); // Product
                        cartListView.Columns[1].Width = (int)(availWidth * 0.20); // Price
                        cartListView.Columns[2].Width = (int)(availWidth * 0.10); // Qty
                        cartListView.Columns[3].Width = (int)(availWidth * 0.25); // Total
                    }
                };

                // Initialize the cart items list if it doesn't exist
                _cartItems = _cartItems ?? [];
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in InitializeCartUI: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SwitchCategory(string category)
        {
            try
            {
                if (allButton == null || beveragesButton == null || snacksButton == null)
                    return;
                    
                // Reset all buttons
                allButton.BackColor = ColorTranslator.FromHtml("#B4E67E");
                beveragesButton.BackColor = ColorTranslator.FromHtml("#B4E67E");
                snacksButton.BackColor = ColorTranslator.FromHtml("#B4E67E");
                
                allButton.ForeColor = Color.Black;
                beveragesButton.ForeColor = Color.Black;
                snacksButton.ForeColor = Color.Black;

                // Highlight selected category
                switch (category)
                {
                    case "":
                        allButton.BackColor = Color.FromArgb(97, 138, 61); // Darker green
                        allButton.ForeColor = Color.White;
                        break;
                    case "Beverages":
                        beveragesButton.BackColor = Color.FromArgb(97, 138, 61); // Darker green
                        beveragesButton.ForeColor = Color.White;
                        break;
                    case "Snacks":
                        snacksButton.BackColor = Color.FromArgb(97, 138, 61); // Darker green
                        snacksButton.ForeColor = Color.White;
                        break;
                }

                _currentCategory = category;
                
                // Only load products if the database manager and product table are initialized
                if (_dbManager != null && productsTable != null)
                {
                    LoadProducts(category);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SwitchCategory: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SearchProducts()
        {
            try
            {
                if (searchBox == null || _dbManager == null || productsTable == null)
                    return;
                    
                string searchTerm = searchBox.Text.Trim();
                if (string.IsNullOrEmpty(searchTerm))
                {
                    LoadProducts(_currentCategory);
                    return;
                }

                List<Product> products = _dbManager.SearchProducts(searchTerm);
                
                // Sort search results by name
                products = products.OrderBy(p => p.Name).ToList();
                
                DisplayProducts(products);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SearchProducts: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadProducts(string category)
        {
            try
            {
                if (_dbManager == null || productsTable == null)
                    return;
                    
                List<Product> products;
                
                if (string.IsNullOrEmpty(category))
                {
                    products = _dbManager.GetAllProducts();
                }
                else
                {
                    products = _dbManager.GetProductsByCategory(category);
                }
                
                // Sort products by name for consistent ordering
                products = products.OrderBy(p => p.Name).ToList();
                
                DisplayProducts(products);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LoadProducts: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void EnsureVerticalScrollbar()
        {
            try
            {
                if (productsPanel != null && productsPanel.IsHandleCreated)
                {
                    // Force the vertical scrollbar to be visible
                    _ = ShowScrollBar(productsPanel.Handle, SB_VERT, true);
                    // Hide the horizontal scrollbar
                    _ = ShowScrollBar(productsPanel.Handle, SB_HORZ, false);
                    
                    // Make sure we're at the top
                    productsPanel.AutoScrollPosition = new Point(0, 0);
                    
                    // Force refresh to ensure scrollbar appears
                    productsPanel.Refresh();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing scrollbar: {ex.Message}");
            }
        }

        private void DisplayProducts(List<Product> products)
        {
            try
            {
                if (productsTable == null || products == null)
                    return;
                    
                // Clear existing products
                productsTable.Controls.Clear();
                productsTable.RowStyles.Clear();
                productsTable.RowCount = 0;

                if (products.Count == 0)
                {
                    Label noProductsLabel = new()
                    {
                        Text = "No products found",
                        Font = new Font("Arial", 14),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Fill
                    };
                    
                    productsTable.RowCount = 1;
                    productsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
                    productsTable.Controls.Add(noProductsLabel, 0, 0);
                    productsTable.SetColumnSpan(noProductsLabel, productsTable.ColumnCount);
                    
                    return;
                }

                int cols = productsTable.ColumnCount;
                
                // Reduced from 5 to 3 empty rows to move products 2 rows higher
                int emptyRowsAtTop = 3;
                
                // Calculate total rows needed including empty space
                int contentRows = (int)Math.Ceiling((double)products.Count / cols);
                int totalRows = contentRows + emptyRowsAtTop;

                // Configure row count and styles
                productsTable.RowCount = totalRows;
                
                // Add empty rows at the top
                for (int i = 0; i < emptyRowsAtTop; i++)
                {
                    productsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // Small height for padding
                }
                
                // Add rows for products with increased row height to accommodate images
                for (int i = emptyRowsAtTop; i < totalRows; i++)
                {
                    productsTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 240)); // Increased from 180 to 240 for image
                }

                // Add product cells with sequential item numbers
                for (int i = 0; i < products.Count; i++)
                {
                    Product product = products[i];
                    
                    // Adjusted row calculation to account for empty space at top
                    int row = (i / cols) + emptyRowsAtTop;
                    int col = i % cols;

                    Panel productCell = CreateProductCell(product, i+1); // Display sequential item number
                    productsTable.Controls.Add(productCell, col, row);
                }
                
                // Reset product table properties to ensure correct layout
                productsTable.Dock = DockStyle.Top;  // Use Top instead of Fill for proper layout with scrollbar
                productsTable.Height = ((totalRows - emptyRowsAtTop) * 240) + (emptyRowsAtTop * 40) + (totalRows + 1) * productsTable.Padding.Vertical;
                productsTable.CellBorderStyle = TableLayoutPanelCellBorderStyle.Outset; // Change border style for more spacing
                productsTable.AutoScroll = false;    // Let the parent panel handle scrolling
                
                // Force visible scrollbar and reset position
                if (productsPanel != null)
                {
                    productsPanel.AutoScrollPosition = new Point(0, 0);
                    EnsureVerticalScrollbar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in DisplayProducts: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Panel CreateProductCell(Product product, int itemNumber)
        {
            try
            {
                if (product == null)
                    return new Panel();
                    
                Panel cell = new()
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Padding = new Padding(3),
                    Margin = new Padding(5) // Increased margin for better spacing
                };

                // Store the product object in the Tag for easy access later
                cell.Tag = product;

                // Assign the context menu to the cell
                cell.ContextMenuStrip = productContextMenu;

                // Add click handler and context menu to all child controls recursively
                AddContextMenuToControls(cell, product);

                // Create a layout for the product cell content
                TableLayoutPanel cellLayout = new()
                {
                    Dock = DockStyle.Fill,
                    RowCount = 6, // Increased row count to 6 for the new Stock label
                    ColumnCount = 1,
                    BackColor = Color.White
                };
                
                // Configure rows - adjusted heights for added Stock label
                cellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // Item number
                cellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70)); // Product image - slightly reduced height
                cellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Product name
                cellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // Price
                cellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25)); // New: Stock Label
                cellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35)); // Add button
                
                // Make sure the layout fills the cell fully
                cellLayout.Dock = DockStyle.Fill;
                
                // Center the content in cells horizontally
                cellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                
                // Item number (as shown in mockup)
                Label itemLabel = new()
                {
                    Text = $"ITEM {itemNumber}",
                    Font = new Font("Arial", 9, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    AutoSize = false
                };
                cellLayout.Controls.Add(itemLabel, 0, 0);

                // Product image - new
                PictureBox productImage = new()
                {
                    Size = new Size(64, 64), // Increased size for better visibility
                    SizeMode = PictureBoxSizeMode.CenterImage,
                    Dock = DockStyle.Fill
                };
                
                // Find the appropriate image based on product name
                string imageKey = "default"; // Default image key
                string productName = product.Name.ToLower();
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
                
                // Set the image
                productImage.Image = productImages.Images[imageKey];
                
                // Create a panel to center the image
                Panel imagePanel = new()
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White
                };
                imagePanel.Controls.Add(productImage);
                
                // Center the image in the panel
                imagePanel.Resize += (s, e) => {
                    productImage.Location = new Point(
                        (imagePanel.ClientSize.Width - productImage.Width) / 2,
                        (imagePanel.ClientSize.Height - productImage.Height) / 2);
                };
                
                cellLayout.Controls.Add(imagePanel, 0, 1);

                // Product name label
                Label nameLabel = new()
                {
                    Text = product.Name,
                    Font = new Font("Arial", 10, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    AutoSize = false
                };
                cellLayout.Controls.Add(nameLabel, 0, 2);

                // Price label
                Label priceLabel = new()
                {
                    Text = $"${product.Price:F2}",
                    Font = new Font("Arial", 10),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    AutoSize = false
                };
                cellLayout.Controls.Add(priceLabel, 0, 3);

                // New: Stock label
                Label stockLabel = new()
                {
                    Text = $"Stock: {product.Stock}",
                    Font = new Font("Arial", 9),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill,
                    AutoSize = false
                };
                cellLayout.Controls.Add(stockLabel, 0, 4); // Add to the new row (index 4)

                // Create a panel to center the button
                Panel buttonPanel = new()
                {
                    Dock = DockStyle.Fill
                };
                cellLayout.Controls.Add(buttonPanel, 0, 5);

                // Add to cart button
                Button addButton = new()
                {
                    Text = "Add",
                    Size = new(60, 22),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ColorTranslator.FromHtml("#B4E67E"),
                    Location = new Point((buttonPanel.Width - 60) / 2, (buttonPanel.Height - 22) / 2)
                };
                addButton.FlatAppearance.BorderSize = 0;
                addButton.Click += (s, e) => AddProductToCart(product);
                
                // Center the button in the panel
                buttonPanel.Controls.Add(addButton);
                buttonPanel.Resize += (s, e) => {
                    addButton.Location = new Point(
                        (buttonPanel.ClientSize.Width - addButton.Width) / 2,
                        (buttonPanel.ClientSize.Height - addButton.Height) / 2);
                };

                // Add the layout to the cell
                cell.Controls.Add(cellLayout);

                // Also assign the product and context menu to the cellLayout
                cellLayout.Tag = product;
                cellLayout.ContextMenuStrip = productContextMenu;

                return cell;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CreateProductCell: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new Panel();
            }
        }

        // Helper method to recursively add context menus and set Tag to controls
        private void AddContextMenuToControls(Control parent, Product product)
        {
            foreach (Control control in parent.Controls)
            {
                // Assign the context menu and set the Tag of each control to the product object
                control.ContextMenuStrip = productContextMenu;
                control.Tag = product;

                // Optionally set cursor to hand for visual feedback
                control.Cursor = Cursors.Hand;

                // Recursively add handlers to child controls
                if (control.HasChildren)
                {
                    // Pass the product object down the recursion
                    AddContextMenuToControls(control, product);
                }
            }
        }

        private void AddProductToCart(Product product)
        {
            try
            {
                if (product == null || _cartItems == null || cartListView == null || 
                    checkoutButton == null || clearCartButton == null || _dbManager == null)
                    return;
                    
                // Get the current stock of the product
                int availableStock = _dbManager.GetProductStock(product.ProductId);

                // Check if product is already in cart
                CartItem? existingItem = _cartItems.FirstOrDefault(item => item.Product.ProductId == product.ProductId);
                
                if (existingItem != null)
                {
                    // Check if adding one more exceeds stock
                    if (existingItem.Quantity + 1 > availableStock)
                    {
                        MessageBox.Show($"Cannot add more {product.Name}. Only {availableStock} left in stock.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Increment quantity if product is already in cart
                    existingItem.Quantity++;
                    
                    // Update the list view item
                    foreach (ListViewItem item in cartListView.Items)
                    {
                        if (item.Tag is CartItem cartItem && cartItem.Product.ProductId == product.ProductId)
                        {
                            // Update quantity and total price 
                            item.SubItems[2].Text = existingItem.Quantity.ToString();
                            item.SubItems[3].Text = $"${existingItem.TotalPrice:F2}";
                            
                            // Make sure this item is visible
                            cartListView.EnsureVisible(item.Index);
                            break;
                        }
                    }
                }
                else
                {
                    // Check if adding the first item exceeds stock (should only be 1, but good practice)
                    if (1 > availableStock)
                    {
                         MessageBox.Show($"Cannot add {product.Name}. Currently out of stock.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                         return;
                    }

                    // Add new item to cart
                    CartItem newItem = new()
                    { 
                        Product = product, 
                        Quantity = 1 
                    };
                    _cartItems.Add(newItem);
                    
                    // Create a distinctive list view item
                    ListViewItem listItem = new()
                    {
                        UseItemStyleForSubItems = false,
                        Text = product.Name,
                        BackColor = Color.White,
                        ForeColor = Color.Black
                    };
                    
                    // Price column
                    ListViewItem.ListViewSubItem priceItem = new()
                    {
                        Text = $"${product.Price:F2}",
                        ForeColor = Color.Black,
                        BackColor = Color.White
                    };
                    listItem.SubItems.Add(priceItem);
                    
                    // Quantity column
                    ListViewItem.ListViewSubItem qtyItem = new()
                    {
                        Text = "1",
                        ForeColor = Color.Black,
                        BackColor = Color.White
                    };
                    listItem.SubItems.Add(qtyItem);
                    
                    // Total column
                    ListViewItem.ListViewSubItem totalItem = new()
                    {
                        Text = $"${product.Price:F2}",
                        ForeColor = Color.Black,
                        BackColor = Color.White
                    };
                    listItem.SubItems.Add(totalItem);
                    
                    // Store reference to the cart item
                    listItem.Tag = newItem;
                    
                    // Add to list and ensure visibility
                    cartListView.Items.Add(listItem);
                    cartListView.EnsureVisible(listItem.Index);
                }
                
                // Update total and enable buttons
                UpdateCartTotal();
                
                // Make sure total panel and list view are visible
                Application.DoEvents();
                cartListView.Invalidate();
                cartTotalPanel.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in AddProductToCart: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemoveCartItem(object? sender, EventArgs e)
        {
            try
            {
                if (cartListView == null || _cartItems == null || 
                    checkoutButton == null || clearCartButton == null)
                    return;
                    
                if (cartListView.SelectedItems.Count == 0)
                    return;
                    
                ListViewItem selectedItem = cartListView.SelectedItems[0];
                if (selectedItem.Tag is CartItem cartItem)
                {
                    _cartItems.Remove(cartItem);
                    cartListView.Items.Remove(selectedItem);
                    UpdateCartTotal();
                    
                    // Disable buttons if cart is empty
                    if (_cartItems.Count == 0)
                    {
                        checkoutButton.Enabled = false;
                        clearCartButton.Enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in RemoveCartItem: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void IncreaseItemQuantity(object? sender, EventArgs e)
        {
            try
            {
                if (cartListView == null)
                    return;
                    
                if (cartListView.SelectedItems.Count == 0)
                    return;
                    
                ListViewItem selectedItem = cartListView.SelectedItems[0];
                if (selectedItem.Tag is CartItem cartItem)
                {
                    cartItem.Quantity++;
                    selectedItem.SubItems[2].Text = cartItem.Quantity.ToString();
                    selectedItem.SubItems[3].Text = $"${cartItem.TotalPrice:F2}";
                    UpdateCartTotal();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in IncreaseItemQuantity: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void DecreaseItemQuantity(object? sender, EventArgs e)
        {
            try
            {
                if (cartListView == null || _cartItems == null || 
                    checkoutButton == null || clearCartButton == null)
                    return;
                    
                if (cartListView.SelectedItems.Count == 0)
                    return;
                    
                ListViewItem selectedItem = cartListView.SelectedItems[0];
                if (selectedItem.Tag is CartItem cartItem)
                {
                    if (cartItem.Quantity > 1)
                    {
                        cartItem.Quantity--;
                        selectedItem.SubItems[2].Text = cartItem.Quantity.ToString();
                        selectedItem.SubItems[3].Text = $"${cartItem.TotalPrice:F2}";
                        UpdateCartTotal();
                    }
                    else
                    {
                        // Remove item if quantity would be 0
                        _cartItems.Remove(cartItem);
                        cartListView.Items.Remove(selectedItem);
                        UpdateCartTotal();
                        
                        // Disable buttons if cart is empty
                        if (_cartItems.Count == 0)
                        {
                            checkoutButton.Enabled = false;
                            clearCartButton.Enabled = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in DecreaseItemQuantity: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void UpdateCartTotal()
        {
            try
            {
                if (_cartItems == null || totalLabel == null || 
                    checkoutButton == null || clearCartButton == null)
                    return;
                    
                decimal total = _cartItems.Sum(item => item.TotalPrice);
                
                // Update total display with clear formatting
                totalLabel.Text = $"Total: ${total:F2}";
                
                // Always update button state based on cart contents
                bool hasItems = _cartItems.Count > 0;
                checkoutButton.Enabled = hasItems;
                clearCartButton.Enabled = hasItems;
                
                // Visual feedback for checkout button
                checkoutButton.BackColor = hasItems 
                    ? ColorTranslator.FromHtml("#B4E67E") 
                    : Color.LightGray;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in UpdateCartTotal: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void CheckoutButtonClick(object? sender, EventArgs e)
        {
            try
            {
                if (_cartItems == null)
                    return;
                    
                // Get the total cost of all items in the cart
                decimal total = _cartItems.Sum(item => item.TotalPrice);

                // Create and show the payment dialog
                using PaymentDialog paymentDialog = new(total);
                DialogResult result = paymentDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    decimal amountPaid = paymentDialog.AmountPaid;
                    decimal change = paymentDialog.Change;
                    
                    // Create a receipt
                    Receipt receipt = new()
                    {
                        Date = DateTime.Now,
                        Total = total,
                        AmountPaid = amountPaid,
                        Change = change,
                        Items = _cartItems.ToList() // Create a copy of the cart items
                    };
                    
                    // Save receipt to database
                    int receiptId = _dbManager.SaveReceipt(receipt);
                    
                    if (receiptId > 0)
                    {
                        // Process the transaction (in a real app, this would interact with payment systems)
                        MessageBox.Show(
                            $"Transaction complete!\n\nTotal: ${total:F2}\nAmount Paid: ${amountPaid:F2}\nChange: ${change:F2}\n\nReceipt #{receiptId} saved.",
                            "Transaction Successful",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        // Update stock for each item in the receipt
                        foreach (var item in receipt.Items)
                        {
                            _dbManager.UpdateProductStock(item.Product.ProductId, item.Quantity);
                        }

                        // Clear the cart after successful transaction
                        ClearCart();
                        
                        // Refresh product display to show updated stock levels
                        SwitchCategory(_currentCategory);
                    }
                    else
                    {
                        MessageBox.Show("Failed to save receipt.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in CheckoutButtonClick: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ViewReceiptsClick(object? sender, EventArgs e)
        {
            try
            {
                ReceiptsForm receiptsForm = new(_dbManager);
                receiptsForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error showing receipts: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ClearCartButtonClick(object? sender, EventArgs e)
        {
            try
            {
                // Ask for confirmation
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to clear the cart?",
                    "Clear Cart",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    ClearCart();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ClearCartButtonClick: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ClearCart()
        {
            try
            {
                if (_cartItems == null || cartListView == null || 
                    checkoutButton == null || clearCartButton == null || 
                    totalLabel == null)
                    return;
                    
                _cartItems.Clear();
                cartListView.Items.Clear();
                UpdateCartTotal();
                checkoutButton.Enabled = false;
                clearCartButton.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in ClearCart: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Event handler for the "Edit Product" context menu item
        private void EditProductMenuItem_Click(object? sender, EventArgs e)
        {
            try
            {
                // Get the ToolStripMenuItem that was clicked
                ToolStripMenuItem? menuItem = sender as ToolStripMenuItem;
                if (menuItem == null) return;

                // Get the ContextMenuStrip that owns the menu item
                ContextMenuStrip? menu = menuItem.Owner as ContextMenuStrip;
                if (menu == null) return;

                // Get the control that the context menu was opened on
                Control? sourceControl = menu.SourceControl;
                if (sourceControl == null) return;

                // Get the Product object from the Tag of the source control
                Product? productToEdit = sourceControl.Tag as Product;

                if (productToEdit != null && _dbManager != null)
                {
                    // Open the EditProductForm with the selected product
                    using EditProductForm editForm = new(productToEdit, _dbManager);
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        // If the user saved changes, refresh the product display
                        SwitchCategory(_currentCategory);
                    }
                }
                else
                {
                    // Optional: Log or show a message if product tag is missing
                    Console.WriteLine("Error: Could not retrieve product information from the clicked item.");
                    // MessageBox.Show("Error: Could not retrieve product information for editing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening edit form from context menu: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 