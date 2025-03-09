namespace QBDeposit_LIB
{
    /// <summary>
    /// Represents a deposit record to be added to QuickBooks
    /// </summary>
    public class DepositRecord
    {
        // Required fields for QuickBooks deposit
        public string Customer { get; set; } = "Misc Income"; // Default customer name
        public string ChartOfAccount { get; set; } // Bank account to deposit to
        public double Amount { get; set; }
        public string ChildID { get; set; } // Will be mapped to Memo field
        public string AccountRef { get; set; } // Account reference ID

        public override string ToString()
        {
            return $"Deposit: Customer={Customer}, Account={ChartOfAccount}, AccountRef={AccountRef}, Amount={Amount:C}, Memo={ChildID}";
        }
    }
}