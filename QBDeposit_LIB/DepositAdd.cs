using QBFC16Lib;

namespace QBDeposit_LIB
{
    /// <summary>
    /// Handles adding deposit transactions to QuickBooks Desktop
    /// </summary>
    public class DepositAdd
    {
        private QBSessionManager sessionManager;
        private bool sessionBegun = false;
        private bool connectionOpen = false;

        public DepositAdd()
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
        /// Adds a single deposit record to QuickBooks
        /// </summary>
        /// <param name="record">The deposit record to add</param>
        /// <returns>True if the deposit was added successfully, false otherwise</returns>
        public bool AddDeposit(DepositRecord record)
        {
            Console.WriteLine(record);
            if (record == null)
            {
                Console.WriteLine("Error: Cannot add null deposit record");
                return false;
            }

            try
            {
                // Create the message set request object to hold our request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                // Build the deposit add request
                IDepositAdd depositAddRq = requestMsgSet.AppendDepositAddRq();

                // Set transaction date
                depositAddRq.TxnDate.SetValue(DateTime.Now);

                // Set the deposit account
                depositAddRq.DepositToAccountRef.FullName.SetValue(record.ChartOfAccount);

                // Set memo with the ChildID
                depositAddRq.Memo.SetValue(record.ChildID);

                // Create and add a deposit line
                IDepositLineAdd depositLine = depositAddRq.DepositLineAddList.Append();

                // We need to explicitly set ORDepositLineAdd to use DepositInfo, not PaymentLine
                string depositInfoType = "DepositInfo";

                if (depositInfoType == "DepositInfo")
                {
                    // Set the entity reference if customer is provided
                    if (!string.IsNullOrEmpty(record.Customer))
                    {
                        // First get the customer ListID
                        string customerListId = GetCustomerListId(record.Customer);

                        if (!string.IsNullOrEmpty(customerListId))
                        {
                            // Set the entity reference to the customer
                            depositLine.ORDepositLineAdd.DepositInfo.EntityRef.ListID.SetValue(customerListId);
                        }
                        else
                        {
                            // Fallback to using FullName if we couldn't get ListID
                            depositLine.ORDepositLineAdd.DepositInfo.EntityRef.FullName.SetValue(record.Customer);
                        }
                    }

                    // Set the deposit amount
                    depositLine.ORDepositLineAdd.DepositInfo.Amount.SetValue(record.Amount);

                    // Set memo to ChildId
                    if (!string.IsNullOrEmpty(record.ChildID))
                    {
                        depositLine.ORDepositLineAdd.DepositInfo.Memo.SetValue(record.ChildID);
                    }

                    // CRITICAL FIX: Set the AccountRef to a valid account in the chart of accounts
                    // Don't use reference number format here - use the actual account name/ID
                    depositLine.ORDepositLineAdd.DepositInfo.AccountRef.FullName.SetValue(record.AccountRef);
                }

                // Send the request and get the response from QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // Process the response
                bool success = ProcessDepositAddResponse(responseMsgSet);
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding deposit: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// Process the deposit add response and return success/failure
        /// </summary>
        /// <param name="responseMsgSet">The response message set from QuickBooks</param>
        /// <returns>True if successful, false otherwise</returns>
        private bool ProcessDepositAddResponse(IMsgSetResponse responseMsgSet)
        {
            if (responseMsgSet == null) return false;

            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return false;

            // Check the response
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
                        if (responseType == ENResponseType.rtDepositAddRs)
                        {
                            // Upcast to more specific type
                            IDepositRet depositRet = (IDepositRet)response.Detail;

                            // Get the TxnID
                            string txnId = depositRet.TxnID.GetValue();
                            Console.WriteLine($"Successfully added deposit with TxnID: {txnId}");
                            return true;
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Error adding deposit: {response.StatusMessage}");
                }
            }

            return false;
        }

        /// <summary>
        /// Adds multiple deposit records to QuickBooks
        /// </summary>
        /// <param name="records">List of deposit records to add</param>
        /// <returns>Number of successfully added deposits</returns>
        public int AddDeposits(List<DepositRecord> records)
        {
            int successCount = 0;

            foreach (var record in records)
            {
                if (AddDeposit(record))
                {
                    successCount++;
                }
            }

            return successCount;
        }

        /// <summary>
        /// Gets the ListID for a customer by name
        /// </summary>
        /// <param name="customerName">Name of the customer</param>
        /// <returns>The customer's ListID in QuickBooks</returns>
        private string GetCustomerListId(string customerName)
        {
            try
            {
                // Create the customer query request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                ICustomerQuery customerQuery = requestMsgSet.AppendCustomerQueryRq();
                customerQuery.ORCustomerListQuery.FullNameList.Add(customerName);

                // Submit the request to QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // Process the response
                IResponse response = responseMsgSet.ResponseList.GetAt(0);
                if (response.StatusCode >= 0 && response.Detail != null)
                {
                    ICustomerRetList customerRetList = (ICustomerRetList)response.Detail;
                    if (customerRetList != null && customerRetList.Count > 0)
                    {
                        ICustomerRet customerRet = customerRetList.GetAt(0);
                        return customerRet.ListID.GetValue();
                    }
                    else
                    {
                        // Customer not found, try to create one
                        return CreateCustomer(customerName);
                    }
                }
                else
                {
                    Console.WriteLine($"Error querying customer: {response.StatusMessage}");
                    return CreateCustomer(customerName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting customer ListID: {ex.Message}");
                // Try to create the customer
                return CreateCustomer(customerName);
            }
        }

        /// <summary>
        /// Creates a new customer in QuickBooks
        /// </summary>
        /// <param name="customerName">Name of the customer to create</param>
        /// <returns>The new customer's ListID in QuickBooks</returns>
        private string CreateCustomer(string customerName)
        {
            try
            {
                // Create the customer add request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                ICustomerAdd customerAdd = requestMsgSet.AppendCustomerAddRq();

                // Set customer properties
                customerAdd.Name.SetValue(customerName);
                customerAdd.CompanyName.SetValue(customerName);

                // Submit the request to QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // Process the response
                IResponse response = responseMsgSet.ResponseList.GetAt(0);
                if (response.StatusCode >= 0)
                {
                    ICustomerRet customerRet = (ICustomerRet)response.Detail;
                    string listID = customerRet.ListID.GetValue();
                    Console.WriteLine($"Successfully created customer '{customerName}' with ListID: {listID}");
                    return listID;
                }
                else
                {
                    Console.WriteLine($"Error creating customer: {response.StatusMessage}");
                    return "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating customer: {ex.Message}");
                return "";
            }
        }
    }
}