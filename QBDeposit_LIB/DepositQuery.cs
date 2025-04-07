using QBFC16Lib;

namespace QBDeposit_LIB
{
    /// <summary>
    /// Handles querying deposit transactions from QuickBooks Desktop
    /// </summary>
    public class DepositQuery
    {
        private QBSessionManager sessionManager;
        private bool sessionBegun = false;
        private bool connectionOpen = false;

        public DepositQuery()
        {
            // Initialize the QuickBooks session manager
            sessionManager = new QBSessionManager();
        }

        /// <summary>
        /// Connects to QuickBooks Desktop
        /// </summary>
        /// <returns>True if connection successful, false otherwise</returns>
        public bool ConnectToQuickBooks()
        {
            try
            {
                sessionManager.OpenConnection("", "QuickBooks Deposit Integration");
                connectionOpen = true;
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to QuickBooks: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Disconnects from QuickBooks Desktop
        /// </summary>
        public void DisconnectFromQuickBooks()
        {
            try
            {
                if (sessionBegun)
                {
                    sessionManager.EndSession();
                    sessionBegun = false;
                }
                if (connectionOpen)
                {
                    sessionManager.CloseConnection();
                    connectionOpen = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting from QuickBooks: {ex.Message}");
            }
        }

        /// <summary>
        /// Queries all deposits from QuickBooks
        /// </summary>
        /// <returns>A list of queried deposit records</returns>
        public List<DepositRecord> QueryAllDeposits()
        {
            List<DepositRecord> deposits = new List<DepositRecord>();

            try
            {
                // Create the message set request object
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                // Create the deposit query request
                IDepositQuery depositQuery = requestMsgSet.AppendDepositQueryRq();

                // Set max results to return (0 = unlimited)
                depositQuery.ORDepositQuery.DepositFilter.MaxReturned.SetValue(100);

                // Include line items
                depositQuery.IncludeLineItems.SetValue(true);

                // Submit the request and get the response from QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // Process the response
                deposits = ProcessDepositQueryResponse(responseMsgSet);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error querying deposits: {ex.Message}");
            }

            return deposits;
        }

        /// <summary>
        /// Process the deposit query response and convert to DepositRecord objects
        /// </summary>
        private List<DepositRecord> ProcessDepositQueryResponse(IMsgSetResponse responseMsgSet)
        {
            List<DepositRecord> deposits = new List<DepositRecord>();

            if (responseMsgSet == null) return deposits;

            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return deposits;

            // Process the response
            if (responseList.Count > 0)
            {
                IResponse response = responseList.GetAt(0);

                // Check the status code of the response, 0=ok, >0 is warning
                if (response.StatusCode >= 0)
                {
                    // The request-specific response is in the details, make sure we have some
                    if (response.Detail != null)
                    {
                        // Make sure the response is the type we're expecting
                        ENResponseType responseType = (ENResponseType)response.Type.GetValue();
                        if (responseType == ENResponseType.rtDepositQueryRs)
                        {
                            // Upcast to more specific type
                            IDepositRetList depositRetList = (IDepositRetList)response.Detail;

                            if (depositRetList != null)
                            {
                                Console.WriteLine($"Found {depositRetList.Count} deposits in QuickBooks.");

                                for (int i = 0; i < depositRetList.Count; i++)
                                {
                                    IDepositRet depositRet = depositRetList.GetAt(i);

                                    // Log deposit header info
                                    string txnID = depositRet.TxnID.GetValue();
                                    DateTime txnDate = depositRet.TxnDate.GetValue();
                                    string depositAccountName = depositRet.DepositToAccountRef.FullName?.GetValue() ?? "Unknown Account";
                                    double depositTotal = depositRet.DepositTotal?.GetValue() ?? 0.0;

                                    // Process each deposit line
                                    if (depositRet.DepositLineRetList != null)
                                    {
                                        for (int j = 0; j < depositRet.DepositLineRetList.Count; j++)
                                        {
                                            IDepositLineRet depositLineRet = depositRet.DepositLineRetList.GetAt(j);
                                            DepositRecord deposit = ConvertToDepositRecord(depositRet, depositLineRet);
                                            deposits.Add(deposit);

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error querying deposits: {response.StatusMessage}");
                }
            }

            return deposits;
        }

        /// <summary>
        /// Convert QuickBooks deposit objects to our DepositRecord model
        /// </summary>
        private DepositRecord ConvertToDepositRecord(IDepositRet depositRet, IDepositLineRet depositLineRet)
        {
            string customerName = "Unknown";
            if (depositLineRet.EntityRef != null && depositLineRet.EntityRef.FullName != null)
            {
                customerName = depositLineRet.EntityRef.FullName.GetValue();
            }

            string memo = "";
            if (depositLineRet.Memo != null)
            {
                memo = depositLineRet.Memo.GetValue();
            }

            string checkNumber = "";
            if (depositLineRet.CheckNumber != null)
            {
                checkNumber = depositLineRet.CheckNumber.GetValue();
            }

            double amount = 0;
            if (depositLineRet.Amount != null)
            {
                amount = depositLineRet.Amount.GetValue();
            }

            string chartOfAccount = "";
            if (depositRet.DepositToAccountRef.FullName != null)
            {
                chartOfAccount = depositRet.DepositToAccountRef.FullName.GetValue();
            }

            string accountRef = "Unknown";
            if (depositLineRet.AccountRef != null && depositLineRet.AccountRef.FullName != null)
            {
                accountRef = depositLineRet.AccountRef.FullName.GetValue();
            }

            return new DepositRecord
            {
                Customer = customerName,
                ChartOfAccount = chartOfAccount,
                Amount = amount,
                ChildID = memo, // Using Memo as ChildId
                AccountRef = accountRef // Get the account reference
            };
        }

        /// <summary>
        /// Prints all deposits to the console
        /// </summary>
        public void PrintAllDeposits()
        {
            List<DepositRecord> deposits = QueryAllDeposits();

            if (deposits.Count == 0)
            {
                Console.WriteLine("No deposits found in QuickBooks.");
                return;
            }

            Console.WriteLine("\n========================================== DEPOSIT LIST ==========================================");
            Console.WriteLine(String.Format("{0,-20} {1,-30} {2,-25} {3,-15} {4,-20}",
                "Customer", "Chart of Account", "Account Ref", "Amount", "Memo"));
            Console.WriteLine("--------------------------------------------------------------------------------------------------");

            double totalAmount = 0;

            foreach (var deposit in deposits)
            {
                Console.WriteLine(String.Format("{0,-20} {1,-30} {2,-25} {3,-15:C} {4,-20}",
                    TruncateString(deposit.Customer, 19),
                    TruncateString(deposit.ChartOfAccount, 29),
                    TruncateString(deposit.AccountRef, 24),
                    deposit.Amount,
                    TruncateString(deposit.ChildID, 19)));

                totalAmount += deposit.Amount;
            }

            Console.WriteLine("--------------------------------------------------------------------------------------------------");
            Console.WriteLine($"TOTAL: {deposits.Count} deposits       Amount: {totalAmount:C}");
            Console.WriteLine("==================================================================================================");
        }

        /// <summary>
        /// Helper function to truncate strings for display formatting
        /// </summary>
        private string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Length <= maxLength ? input : input.Substring(0, maxLength - 3) + "...";
        }
    }
}