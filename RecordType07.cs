using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonLoad
{
    class RecordType07
    {
        public string RecordType { get; set; }
        public string PersonID { get; set; }
        public string Court { get; set; }
        public string CountyNumber { get; set; }
        public string CaseType { get; set; }
        public string YearCode { get; set; }
        public string SeqNum { get; set; }
        public string CaseCaption { get; set; }
        public string OldCaseNumber { get; set; }
        public string FIPSCode { get; set; }
        public DateTime ModifiedStamp { get; set; }
        public string PayorPayee { get; set; }
        public string CourtCaseId { get; set; }
        public string IVDCaseNumber { get; set; }
        public string Filler { get; set; }

        public enum FieldIndex : int
        {
             RecordType = 0
            ,PersonID = 2
            ,Court = 15
            ,CountyNumber = 16
            ,CaseType = 18
            ,YearCode = 20
            ,SeqNum = 22
            ,CaseCaption = 29
            ,OldCaseNumber = 79
            ,FIPSCode = 104
            ,ModifiedStamp = 111
            ,PayorPayee = 137
            ,CourtCaseId = 142
            ,IVDCaseNumber = 155
            ,Filler = 168
        }

        public enum FieldLength : int
        {
             RecordType = 2
            ,PersonID = 13
            ,Court = 1
            ,CountyNumber = 2
            ,CaseType = 2
            ,YearCode = 2
            ,SeqNum = 7
            ,CaseCaption = 50
            ,OldCaseNumber = 25
            ,FIPSCode = 7
            ,ModifiedStamp = 26
            ,PayorPayee = 5
            ,CourtCaseId = 13
            ,IVDCaseNumber = 13
            ,Filler = 382
        }
    }
}
