using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonLoad
{
    class RecordType05
    {
        public string RecordType {get; set;}
        public string PersonID {get; set;}
        public string SSN_FTIN {get; set;}
        public string LastName {get; set;}
        public string FirstName {get; set;}
        public string MiddleName {get; set;}
        public string Suffix {get; set;}
        public string Address1 {get; set;}
        public string Address2 {get; set;}
        public string Address3 {get; set;}
        public string City {get; set;}
        public string State {get; set;}
        public string Zip {get; set;}
        public string HomePhone {get; set;}
        public string CellPhone {get; set;}
        public string WorkPhone {get; set;}
        public string WorkExtension {get; set;}
        public string PagerNumber {get; set;}
        public DateTime? DateofBirth {get; set;}
        public DateTime? DateofDeath {get; set;}
        public string Country {get; set;}
        public string FamilyViolenceInd {get; set;}
        public string EmailAddress {get; set;}
        public string NotificationEmailAddress {get; set;}
        public string DeliveryMethod {get; set;}
        public string IVDStatus {get; set;}
        public string SDUInd {get; set;}
        public string Filler {get; set;}

        public enum FieldIndex : int
        {
             RecordType = 0
            ,PersonID = 2
            ,Ssn_ftin = 15
            ,LastName = 30
            ,FirstName = 80
            ,MiddleName = 100
            ,Suffix = 120
            ,Address1 = 123
            ,Address2 = 173
            ,Address3 = 223
            ,City = 273
            ,State = 298
            ,Zip = 300
            ,HomePhone = 311
            ,CellPhone = 321
            ,WorkPhone = 331
            ,WorkExtension = 341
            ,PagerNumber = 346
            ,DateofBirth = 356
            ,DateofDeath = 364
            ,Country = 372
            ,FamilyViolenceInd = 402
            ,EmailAddress = 403
            ,NotificationEmailAddress = 453
            ,DeliveryMethod = 503
            ,IvdStatus = 504
            ,SduInd = 505
            ,Filler = 506
        }

        public enum FieldLength : int
        {
             RecordType = 2
            ,PersonID = 13
            ,Ssn_ftin = 15
            ,LastName = 50
            ,FirstName = 20
            ,MiddleName = 20
            ,Suffix = 3
            ,Address1 = 50
            ,Address2 = 50
            ,Address3 = 50
            ,City = 25
            ,State = 2
            ,Zip = 11
            ,HomePhone = 10
            ,CellPhone = 10
            ,WorkPhone = 10
            ,WorkExtension = 5
            ,PagerNumber = 10
            ,DateofBirth = 8
            ,DateofDeath = 8
            ,Country = 30
            ,FamilyViolenceInd = 1
            ,EmailAddress = 50
            ,NotificationEmailAddress = 50
            ,DeliveryMethod = 1
            ,IvdStatus = 1
            ,SduInd = 1
            ,Filler = 44
        } 
    }
}
