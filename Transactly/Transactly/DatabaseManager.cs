using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;
using System.Linq;

namespace Transactly
{
    public class Product
    {
        public int ProductId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string ImagePath { get; set; }
        public int Stock { get; set; }
    }

    public class DatabaseManager
    {
        private const string DatabaseName = "transactly.db";
        private readonly string _connectionString;

        public DatabaseManager()
        {
            try
            {
                _connectionString = $"Data Source={DatabaseName};Version=3;";
                InitializeDatabase();
                VerifyDatabaseStructure(); // Add verification step
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database initialization error: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                bool fileExists = File.Exists(DatabaseName);
                bool needsSeeding = !fileExists;
                
                // Ensure database file exists
                if (!fileExists)
                {
                    SQLiteConnection.CreateFile(DatabaseName);
                }
                
                // Open connection and create tables
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Create tables if they don't exist
                    CreateTables(connection);

                    // Check if tables are empty even if the file exists
                    if (!needsSeeding)
                    {
                        // Check if Categories table is empty
                        using (var command = new SQLiteCommand("SELECT COUNT(*) FROM Categories", connection))
                        {
                            int categoryCount = Convert.ToInt32(command.ExecuteScalar());
                            if (categoryCount == 0)
                            {
                                needsSeeding = true;
                            }
                        }
                    }

                    // Seed data if we just created the database or if tables are empty
                    if (needsSeeding)
                    {
                        MessageBox.Show("Database tables are empty. Adding sample data.", "Database Initialization", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        SeedDefaultUser(connection);
                        SeedSampleProducts(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing database: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Delete existing database file and recreate if there's an error
                try
                {
                    if (File.Exists(DatabaseName))
                    {
                        File.Delete(DatabaseName);
                        SQLiteConnection.CreateFile(DatabaseName);
                        
                        using (var connection = new SQLiteConnection(_connectionString))
                        {
                            connection.Open();
                            CreateTables(connection);
                            SeedDefaultUser(connection);
                            SeedSampleProducts(connection);
                        }
                    }
                }
                catch (Exception innerEx)
                {
                    MessageBox.Show($"Error recreating database: {innerEx.Message}", "Database Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void VerifyDatabaseStructure()
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Check if tables exist
                    string[] requiredTables = { "Users", "Categories", "Products", "Receipts", "ReceiptItems" };
                    foreach (string table in requiredTables)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{table}'";
                            var result = command.ExecuteScalar();
                            
                            if (result == null || result.ToString() != table)
                            {
                                // Table doesn't exist, recreate database
                                MessageBox.Show($"Table '{table}' not found. Recreating database.", "Database Structure Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                
                                connection.Close();
                                File.Delete(DatabaseName);
                                SQLiteConnection.CreateFile(DatabaseName);
                                InitializeDatabase();
                                return;
                            }
                        }
                    }
                    
                    // If we get here, all tables exist
                    Console.WriteLine("Database structure verified successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verifying database structure: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void CreateTables(SQLiteConnection connection)
        {
            try
            {
                string createUserTable = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        Password TEXT NOT NULL,
                        FullName TEXT,
                        IsAdmin INTEGER DEFAULT 0,
                        CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
                    );";

                string createCategoryTable = @"
                    CREATE TABLE IF NOT EXISTS Categories (
                        CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE
                    );";

                string createProductTable = @"
                    CREATE TABLE IF NOT EXISTS Products (
                        ProductId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Price REAL NOT NULL,
                        CategoryId INTEGER,
                        ImagePath TEXT,
                        Stock INTEGER DEFAULT 0,
                        FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
                    );";
                    
                string createReceiptsTable = @"
                    CREATE TABLE IF NOT EXISTS Receipts (
                        ReceiptId INTEGER PRIMARY KEY AUTOINCREMENT,
                        Date TEXT NOT NULL,
                        Total REAL NOT NULL,
                        AmountPaid REAL NOT NULL,
                        Change REAL NOT NULL
                    );";
                    
                string createReceiptItemsTable = @"
                    CREATE TABLE IF NOT EXISTS ReceiptItems (
                        ReceiptItemId INTEGER PRIMARY KEY AUTOINCREMENT,
                        ReceiptId INTEGER NOT NULL,
                        ProductId INTEGER NOT NULL,
                        ProductName TEXT NOT NULL,
                        Price REAL NOT NULL,
                        Quantity INTEGER NOT NULL,
                        FOREIGN KEY (ReceiptId) REFERENCES Receipts(ReceiptId),
                        FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
                    );";

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (var command = new SQLiteCommand(createUserTable, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (var command = new SQLiteCommand(createCategoryTable, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (var command = new SQLiteCommand(createProductTable, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (var command = new SQLiteCommand(createReceiptsTable, connection))
                        {
                            command.ExecuteNonQuery();
                        }

                        using (var command = new SQLiteCommand(createReceiptItemsTable, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                        
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating tables: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void SeedDefaultUser(SQLiteConnection connection)
        {
            try
            {
                // Add default admin user
                string insertUser = @"
                    INSERT INTO Users (Username, Password, FullName, IsAdmin)
                    VALUES ('admin', 'admin', 'System Administrator', 1);";

                using (var command = new SQLiteCommand(insertUser, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error seeding default user: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private void SeedSampleProducts(SQLiteConnection connection)
        {
            try
            {
                // First ensure categories don't already exist to avoid duplicates
                using (var checkCommand = new SQLiteCommand("SELECT COUNT(*) FROM Categories", connection))
                {
                    int count = Convert.ToInt32(checkCommand.ExecuteScalar());
                    if (count > 0)
                    {
                        // Clear existing data to avoid duplicates
                        using (var clearCommand = new SQLiteCommand(connection))
                        {
                            clearCommand.CommandText = "DELETE FROM Products";
                            clearCommand.ExecuteNonQuery();
                            
                            clearCommand.CommandText = "DELETE FROM Categories";
                            clearCommand.ExecuteNonQuery();
                            
                            // Reset auto-increment counters
                            clearCommand.CommandText = "DELETE FROM sqlite_sequence WHERE name='Products' OR name='Categories'";
                            clearCommand.ExecuteNonQuery();
                        }
                    }
                }

                // Add categories and products in a single transaction
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Add categories
                        using (var command = new SQLiteCommand(connection))
                        {
                            // Add Beverages category
                            command.CommandText = "INSERT INTO Categories (Name) VALUES ('Beverages'); SELECT last_insert_rowid();";
                            int beveragesCategoryId = Convert.ToInt32(command.ExecuteScalar());

                            // Add Snacks category
                            command.CommandText = "INSERT INTO Categories (Name) VALUES ('Snacks'); SELECT last_insert_rowid();";
                            int snacksCategoryId = Convert.ToInt32(command.ExecuteScalar());

                            // Add sample beverages products
                            InsertProduct(command, "Coffee", 2.50m, beveragesCategoryId, 100);
                            InsertProduct(command, "Tea", 1.75m, beveragesCategoryId, 100);
                            InsertProduct(command, "Soda", 1.50m, beveragesCategoryId, 100);
                            InsertProduct(command, "Water", 1.00m, beveragesCategoryId, 100);
                            InsertProduct(command, "Juice", 2.25m, beveragesCategoryId, 100);
                            InsertProduct(command, "Lemonade", 1.85m, beveragesCategoryId, 100);
                            InsertProduct(command, "Energy Drink", 3.50m, beveragesCategoryId, 100);

                            // Add sample snacks products
                            InsertProduct(command, "Chips", 1.25m, snacksCategoryId, 50);
                            InsertProduct(command, "Chocolate Bar", 1.50m, snacksCategoryId, 50);
                            InsertProduct(command, "Candy", 0.75m, snacksCategoryId, 50);
                            InsertProduct(command, "Cookies", 2.00m, snacksCategoryId, 50);
                            InsertProduct(command, "Nuts", 2.50m, snacksCategoryId, 50);
                            InsertProduct(command, "Popcorn", 1.75m, snacksCategoryId, 50);
                            InsertProduct(command, "Protein Bar", 2.75m, snacksCategoryId, 50);
                            InsertProduct(command, "Trail Mix", 3.00m, snacksCategoryId, 50);
                            InsertProduct(command, "Pretzels", 1.25m, snacksCategoryId, 50);
                            InsertProduct(command, "Crackers", 1.65m, snacksCategoryId, 50);
                            InsertProduct(command, "Granola Bar", 1.45m, snacksCategoryId, 50);
                            InsertProduct(command, "Fruit Snacks", 1.35m, snacksCategoryId, 50);
                        }
                        
                        transaction.Commit();
                        MessageBox.Show("Sample data added successfully!", "Database Initialized", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Error in transaction: {ex.Message}", "Database Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error seeding sample products: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
        
        private void InsertProduct(SQLiteCommand command, string name, decimal price, int categoryId, int stock)
        {
            command.CommandText = "INSERT INTO Products (Name, Price, CategoryId, Stock) VALUES (@Name, @Price, @CategoryId, @Stock)";
            command.Parameters.Clear();
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@Price", price);
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            command.Parameters.AddWithValue("@Stock", stock);
            command.ExecuteNonQuery();
        }

        public bool ValidateUser(string username, string password)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Username", username);
                        command.Parameters.AddWithValue("@Password", password);
                        int count = Convert.ToInt32(command.ExecuteScalar());
                        return count > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error validating user: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public List<string> GetCategories()
        {
            List<string> categories = new List<string>();
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    string query = "SELECT Name FROM Categories ORDER BY CategoryId";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(reader["Name"].ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting categories: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return categories;
        }
        
        public List<Product> GetAllProducts()
        {
            return GetProductsByCategory(null);
        }
        
        public List<Product> GetProductsByCategory(string category)
        {
            List<Product> products = new List<Product>();
            
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    string query;
                    SQLiteCommand command;
                    
                    if (string.IsNullOrEmpty(category))
                    {
                        query = @"
                            SELECT p.ProductId, p.Name, p.Price, c.Name as Category, p.ImagePath, p.Stock
                            FROM Products p
                            LEFT JOIN Categories c ON p.CategoryId = c.CategoryId
                            ORDER BY p.Name";
                        command = new SQLiteCommand(query, connection);
                    }
                    else
                    {
                        query = @"
                            SELECT p.ProductId, p.Name, p.Price, c.Name as Category, p.ImagePath, p.Stock
                            FROM Products p
                            LEFT JOIN Categories c ON p.CategoryId = c.CategoryId
                            WHERE c.Name = @Category
                            ORDER BY p.Name";
                        command = new SQLiteCommand(query, connection);
                        command.Parameters.AddWithValue("@Category", category);
                    }
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductId = Convert.ToInt32(reader["ProductId"]),
                                Name = reader["Name"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                Category = reader["Category"].ToString(),
                                ImagePath = reader["ImagePath"] as string,
                                Stock = Convert.ToInt32(reader["Stock"])
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting products: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            return products;
        }
        
        public List<Product> SearchProducts(string searchTerm)
        {
            List<Product> products = new List<Product>();
            
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    string query = @"
                        SELECT p.ProductId, p.Name, p.Price, c.Name as Category, p.ImagePath, p.Stock
                        FROM Products p
                        LEFT JOIN Categories c ON p.CategoryId = c.CategoryId
                        WHERE p.Name LIKE @SearchTerm
                        ORDER BY p.Name";
                    
                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                products.Add(new Product
                                {
                                    ProductId = Convert.ToInt32(reader["ProductId"]),
                                    Name = reader["Name"].ToString(),
                                    Price = Convert.ToDecimal(reader["Price"]),
                                    Category = reader["Category"].ToString(),
                                    ImagePath = reader["ImagePath"] as string,
                                    Stock = Convert.ToInt32(reader["Stock"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching products: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
            return products;
        }

        public int SaveReceipt(Receipt receipt)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            int receiptId;
                            
                            // Insert receipt header
                            using (var command = new SQLiteCommand(connection))
                            {
                                command.Transaction = transaction;
                                command.CommandText = @"
                                    INSERT INTO Receipts (Date, Total, AmountPaid, Change)
                                    VALUES (@Date, @Total, @AmountPaid, @Change);
                                    SELECT last_insert_rowid();";
                                
                                command.Parameters.AddWithValue("@Date", receipt.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                                command.Parameters.AddWithValue("@Total", receipt.Total);
                                command.Parameters.AddWithValue("@AmountPaid", receipt.AmountPaid);
                                command.Parameters.AddWithValue("@Change", receipt.Change);
                                
                                // Get the new receipt ID
                                receiptId = Convert.ToInt32(command.ExecuteScalar());
                            }
                            
                            // Insert receipt items
                            using (var command = new SQLiteCommand(connection))
                            {
                                command.Transaction = transaction;
                                foreach (CartItem item in receipt.Items)
                                {
                                    command.CommandText = @"
                                        INSERT INTO ReceiptItems (ReceiptId, ProductId, ProductName, Price, Quantity)
                                        VALUES (@ReceiptId, @ProductId, @ProductName, @Price, @Quantity)";
                                    
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("@ReceiptId", receiptId);
                                    command.Parameters.AddWithValue("@ProductId", item.Product.ProductId);
                                    command.Parameters.AddWithValue("@ProductName", item.Product.Name);
                                    command.Parameters.AddWithValue("@Price", item.Product.Price);
                                    command.Parameters.AddWithValue("@Quantity", item.Quantity);
                                    
                                    command.ExecuteNonQuery();
                                }
                            }
                            
                            transaction.Commit();
                            return receiptId;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving receipt: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return -1;
            }
        }
        
        public List<Receipt> GetAllReceipts()
        {
            List<Receipt> receipts = [];
            
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    // Get all receipts
                    using (var command = new SQLiteCommand(@"
                        SELECT ReceiptId, Date, Total, AmountPaid, Change 
                        FROM Receipts 
                        ORDER BY Date DESC", connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Receipt receipt = new()
                                {
                                    ReceiptId = Convert.ToInt32(reader["ReceiptId"]),
                                    Date = DateTime.Parse(reader["Date"].ToString()),
                                    Total = Convert.ToDecimal(reader["Total"]),
                                    AmountPaid = Convert.ToDecimal(reader["AmountPaid"]),
                                    Change = Convert.ToDecimal(reader["Change"]),
                                    Items = []
                                };
                                
                                receipts.Add(receipt);
                            }
                        }
                    }
                }
                return receipts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading receipts: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return [];
            }
        }
        
        public Receipt? GetReceiptById(int receiptId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    Receipt? receipt = null;
                    
                    // Get receipt header
                    using (var command = new SQLiteCommand(@"
                        SELECT ReceiptId, Date, Total, AmountPaid, Change 
                        FROM Receipts 
                        WHERE ReceiptId = @ReceiptId", connection))
                    {
                        command.Parameters.AddWithValue("@ReceiptId", receiptId);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                receipt = new Receipt()
                                {
                                    ReceiptId = Convert.ToInt32(reader["ReceiptId"]),
                                    Date = DateTime.Parse(reader["Date"].ToString()),
                                    Total = Convert.ToDecimal(reader["Total"]),
                                    AmountPaid = Convert.ToDecimal(reader["AmountPaid"]),
                                    Change = Convert.ToDecimal(reader["Change"]),
                                    Items = []
                                };
                            }
                            else
                            {
                                return null; // Receipt not found
                            }
                        }
                    }
                    
                    // Get receipt items
                    if (receipt != null)
                    {
                        using (var command = new SQLiteCommand(@"
                            SELECT ri.ProductId, ri.ProductName, ri.Price, ri.Quantity
                            FROM ReceiptItems ri
                            WHERE ri.ReceiptId = @ReceiptId", connection))
                        {
                            command.Parameters.AddWithValue("@ReceiptId", receiptId);
                            
                            using (var reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Product product = new()
                                    {
                                        ProductId = Convert.ToInt32(reader["ProductId"]),
                                        Name = reader["ProductName"].ToString(),
                                        Price = Convert.ToDecimal(reader["Price"])
                                    };
                                    
                                    CartItem item = new()
                                    {
                                        Product = product,
                                        Quantity = Convert.ToInt32(reader["Quantity"])
                                    };
                                    
                                    receipt.Items.Add(item);
                                }
                            }
                        }
                    }
                    
                    return receipt;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading receipt details: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }
        
        public bool DeleteReceipt(int receiptId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            // Delete receipt items first (foreign key constraint)
                            using (var command = new SQLiteCommand(connection))
                            {
                                command.Transaction = transaction;
                                command.CommandText = "DELETE FROM ReceiptItems WHERE ReceiptId = @ReceiptId";
                                command.Parameters.AddWithValue("@ReceiptId", receiptId);
                                command.ExecuteNonQuery();
                            }
                            
                            // Delete receipt header
                            using (var command = new SQLiteCommand(connection))
                            {
                                command.Transaction = transaction;
                                command.CommandText = "DELETE FROM Receipts WHERE ReceiptId = @ReceiptId";
                                command.Parameters.AddWithValue("@ReceiptId", receiptId);
                                int rowsAffected = command.ExecuteNonQuery();
                                
                                if (rowsAffected == 0)
                                {
                                    transaction.Rollback();
                                    return false; // Receipt doesn't exist
                                }
                            }
                            
                            transaction.Commit();
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting receipt: {ex.Message}", "Database Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // New method to update a product
        public bool UpdateProduct(Product product)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = @"
                        UPDATE Products
                        SET Name = @Name, Price = @Price, CategoryId = @CategoryId, ImagePath = @ImagePath, Stock = @Stock
                        WHERE ProductId = @ProductId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        // Need to get CategoryId from Category Name first
                        int categoryId = GetCategoryId(product.Category, connection);
                        if (categoryId == -1)
                        {
                             MessageBox.Show($"Category '{product.Category}' not found.", "Update Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                             return false;
                        }

                        command.Parameters.AddWithValue("@Name", product.Name);
                        command.Parameters.AddWithValue("@Price", product.Price);
                        command.Parameters.AddWithValue("@CategoryId", categoryId);
                        command.Parameters.AddWithValue("@ImagePath", product.ImagePath ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Stock", product.Stock);
                        command.Parameters.AddWithValue("@ProductId", product.ProductId);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating product: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Helper method to get CategoryId from Category Name
        private int GetCategoryId(string categoryName, SQLiteConnection connection)
        {
            string query = "SELECT CategoryId FROM Categories WHERE Name = @CategoryName";
            using (var command = new SQLiteCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CategoryName", categoryName);
                object? result = command.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                {
                    return Convert.ToInt32(result);
                }
                return -1; // Category not found
            }
        }

        // New method to update a product's stock
        public bool UpdateProductStock(int productId, int quantitySold)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    // Ensure stock does not go below zero
                    string query = @"
                        UPDATE Products
                        SET Stock = MAX(0, Stock - @QuantitySold)
                        WHERE ProductId = @ProductId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@QuantitySold", quantitySold);
                        command.Parameters.AddWithValue("@ProductId", productId);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating product stock: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // New method to get product stock by ID
        public int GetProductStock(int productId)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT Stock FROM Products WHERE ProductId = @ProductId";

                    using (var command = new SQLiteCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@ProductId", productId);
                        object? result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            return Convert.ToInt32(result);
                        }
                        // Return -1 or throw an exception if product not found, depending on desired behavior.
                        // Returning 0 for now as a safe default if product somehow not found or stock is DBNull.
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting product stock: {ex.Message}", "Database Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0; // Return 0 in case of error
            }
        }
    }
} 