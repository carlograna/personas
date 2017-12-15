//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PersonLoad
{
    using System;
    using System.Collections.Generic;
    
    public partial class KCPersonFinancial
    {
        public System.Guid KCPersonFinancialID { get; set; }
        public System.Guid KCPersonID { get; set; }
        public string ABA { get; set; }
        public string Account { get; set; }
        public byte KCAccountTypeID { get; set; }
        public Nullable<byte> AcceptChecks { get; set; }
        public Nullable<byte> DirectDeposit { get; set; }
        public Nullable<short> BadCheckCount { get; set; }
        public Nullable<System.DateTime> LastBadCheck { get; set; }
        public Nullable<byte> ACHAuthType { get; set; }
        public Nullable<byte> AutoPayFrequency { get; set; }
        public Nullable<decimal> AutoPayAmount { get; set; }
        public Nullable<System.DateTime> AutoPayTransactionDate { get; set; }
        public Nullable<System.DateTime> AutoPayStartDate { get; set; }
        public Nullable<System.DateTime> AutoPayStopDate { get; set; }
        public Nullable<byte> Prenoted { get; set; }
        public Nullable<System.DateTime> PrenotedDate { get; set; }
        public Nullable<System.DateTime> ModifiedStamp { get; set; }
        public string ModifiedBy { get; set; }
        public Nullable<System.DateTime> EPCStart { get; set; }
        public Nullable<System.DateTime> EPCActivated { get; set; }
        public Nullable<System.DateTime> EPCDeactivated { get; set; }
        public bool EPCCardOrdered { get; set; }
        public bool EPCSpecialHandling { get; set; }
        public string EPCMaidenName { get; set; }
        public bool DisplayOnWeb { get; set; }
        public Nullable<byte> AutoPayDayOfMonth { get; set; }
        public byte PmtAuthType { get; set; }
    
        public virtual KCPerson KCPerson { get; set; }
    }
}