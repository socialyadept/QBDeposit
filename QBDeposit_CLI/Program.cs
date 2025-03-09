using QBDeposit_LIB;

namespace QB_Deposit
{
    class Program
    {

        static void Main(string[] args)
        {
            Console.WriteLine("\nQuickBooks Desktop Deposit Query Integration");
            Console.WriteLine("==============================================");

            try
            {
                // Query the QuickBooks project
                Console.WriteLine("\nQuerying initial QuickBooks deposits...");
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
    }
}