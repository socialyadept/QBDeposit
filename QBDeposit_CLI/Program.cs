using QBDeposit_LIB;

namespace QB_Deposit
{
    class Program
    {
        private static string csvFilePath = "../credit-nonvendor.csv";

        static void Main(string[] args)
        {
            Console.WriteLine("QuickBooks Desktop Deposit Integration");
            Console.WriteLine("=====================================");

            try
            {
                // 1. Query the empty QuickBooks project
                Console.WriteLine("\n1. Querying initial QuickBooks deposits...");
                QueryDeposits();

                // 2. Add a single test record
                Console.WriteLine("\n2. Adding a single test deposit record...");
                DepositRecord testRecord = CreateTestRecord();
                AddSingleDeposit(testRecord);

                // 3. Query again to show the added record
                Console.WriteLine("\n3. Querying QuickBooks deposits after adding one record...");
                QueryDeposits();

                // 4. Add all records from CSV file
                Console.WriteLine("\n4. Adding all deposits from CSV file...");
                if (File.Exists(csvFilePath))
                {
                    AddAllDepositsFromCsv();
                }
                else
                {
                    throw new Exception("CSV File not found");
                }

                // 5. Query all the results
                Console.WriteLine("\n5. Querying all QuickBooks deposits after adding CSV records...");
                QueryDeposits();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Creates a sample test record for demonstration
        /// </summary>
        /// <returns>A sample deposit record</returns>
        private static DepositRecord CreateTestRecord()
        {
            return new DepositRecord
            {
                ChildID = "0001",
                Amount = 1000.00,
                ChartOfAccount = "test account list", // Checking Account
                AccountRef = "Sales", // Income Account
                Customer = "Misc Income",
            };
        }

        /// <summary>
        /// Queries and displays all deposits from QuickBooks
        /// </summary>
        private static void QueryDeposits()
        {
            DepositQuery depositQuery = new DepositQuery();
            try
            {
                if (depositQuery.ConnectToQuickBooks())
                {
                    depositQuery.PrintAllDeposits();
                }
            }
            finally
            {
                depositQuery.DisconnectFromQuickBooks();
            }
        }

        /// <summary>
        /// Adds a single deposit record to QuickBooks
        /// </summary>
        /// <param name="record">The deposit record to add</param>
        private static void AddSingleDeposit(DepositRecord record)
        {
            DepositAdd depositAdd = new DepositAdd();
            try
            {
                if (depositAdd.ConnectToQuickBooks())
                {
                    bool success = depositAdd.AddDeposit(record);
                    Console.WriteLine(success);
                    if (success)
                    {
                        Console.WriteLine("Successfully added test deposit record.");
                    }
                    else
                    {
                        Console.WriteLine("Failed to add test deposit record.");
                    }
                }
            }
            finally
            {
                depositAdd.DisconnectFromQuickBooks();
            }
        }

        /// <summary>
        /// Adds all deposits from the CSV file to QuickBooks
        /// </summary>
        private static void AddAllDepositsFromCsv()
        {
            List<DepositRecord> records = CsvParser.ParseCsvFile(csvFilePath);

            if (records.Count == 0)
            {
                Console.WriteLine("No records found in CSV file or error parsing file.");
                return;
            }

            Console.WriteLine($"Found {records.Count} records in CSV file.");

            DepositAdd depositAdd = new DepositAdd();
            try
            {
                if (depositAdd.ConnectToQuickBooks())
                {
                    int successCount = depositAdd.AddDeposits(records);
                    Console.WriteLine($"Successfully added {successCount} out of {records.Count} deposit records.");
                }
            }
            finally
            {
                depositAdd.DisconnectFromQuickBooks();
            }
        }
    }
}