using System.Globalization;
using System.Text;

namespace QBDeposit_LIB
{
    /// <summary>
    /// Handles parsing of CSV deposit data file
    /// </summary>
    public class CsvParser
    {
        /// <summary>
        /// Parses a CSV file and returns a list of DepositRecord objects
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>List of DepositRecord objects</returns>
        public static List<DepositRecord> ParseCsvFile(string filePath)
        {
            List<DepositRecord> records = new List<DepositRecord>();

            try
            {
                // Read all lines from the CSV file
                string[] lines = File.ReadAllLines(filePath);

                if (lines.Length <= 1)
                {
                    Console.WriteLine("CSV file is empty or contains only headers.");
                    return records;
                }

                // Skip header row and process each data row
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var record = ParseCsvLine(line);
                    if (record != null)
                    {
                        records.Add(record);
                        Console.WriteLine($"Parsed record: {record}");
                    }
                }

                Console.WriteLine($"Successfully parsed {records.Count} records from CSV file.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing CSV file: {ex.Message}");
            }

            return records;
        }

        /// <summary>
        /// Parses a single line from the CSV file into a DepositRecord
        /// </summary>
        /// <param name="line">CSV line</param>
        /// <returns>DepositRecord object</returns>
        private static DepositRecord ParseCsvLine(string line)
        {
            try
            {
                // Split the CSV line using commas, but handle quoted values properly
                string[] fields = SplitCsvLine(line);

                // Check if we have at least the minimum number of fields
                if (fields.Length < 14)
                {
                    Console.WriteLine($"Warning: Line does not contain enough fields: {line}");
                    return null;
                }

                // Extract data from CSV fields
                // Field positions based on the CSV header:
                // 0=Parent ID, 1=Child ID, 4=Bank Date, 5=Customer, 6=Check Amount, 
                // 7=Tier 2 - Chart of Account ID, 8=Tier 2 - Chart of Account, 11=Tier 1 - Type

                string childId = fields[1].Trim();

                // Parse amount from Check Amount field
                string amountStr = fields[6].Trim().Replace("$", "").Replace(",", "").Trim('"');
                double amount;
                if (!double.TryParse(amountStr, out amount))
                {
                    Console.WriteLine($"Warning: Could not parse amount: {fields[6]}");
                    return null;
                }

                // Get account information
                string accountId = fields[7].Trim();
                string accountName = fields[8].Trim();
                string accountType = fields[11].Trim();

                // Always use test account list as bank account
                string chartOfAccount = "test account list";

                // Determine account reference based on account type
                string accountRef;
                switch (accountType)
                {
                    case "Income":
                        accountRef = "Sales";
                        break;
                    case "Expense":
                        accountRef = "Automobile Expense";
                        break;
                    case "Equity":
                        accountRef = "Shareholder Distributions";
                        break;
                    case "Other Income":
                        // For Other Income, use the specific account name
                        if (accountName == "Rental")
                            accountRef = "Rental";
                        else
                            accountRef = "Misc Credits";
                        break;
                    default:
                        accountRef = "Sales"; // Default to Sales
                        break;
                }

                // Get customer if available, otherwise use default
                string customer = !string.IsNullOrEmpty(fields[5]) ? fields[5].Trim() : "Misc Income";

                // Create the deposit record
                return new DepositRecord
                {
                    ChildID = childId,
                    Amount = amount,
                    ChartOfAccount = chartOfAccount,
                    AccountRef = accountRef,
                    Customer = customer
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing CSV line: {ex.Message}");
                Console.WriteLine($"Line: {line}");
                return null;
            }
        }

        /// <summary>
        /// Splits a CSV line, handling quoted values correctly
        /// </summary>
        private static string[] SplitCsvLine(string line)
        {
            List<string> result = new List<string>();
            bool inQuotes = false;
            StringBuilder field = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // Toggle in-quotes state
                    inQuotes = !inQuotes;
                    field.Append(c); // Keep quotes in the field value
                }
                else if (c == ',' && !inQuotes)
                {
                    // End of field
                    result.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            // Add the last field
            result.Add(field.ToString());

            return result.ToArray();
        }
    }
}