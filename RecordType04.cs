using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonLoad
{
    class RecordType04
    {
        //private string recordType;
        //private string primaryArpId;
        //private string secondaryArpId;
        //private DateTime modifiedStamp;
        //private string filler;

        public string RecordType { get; set; }
        public string PrimaryArpID { get; set; }
        public string SecondaryArpID { get; set; }
        public DateTime ModifiedStamp { get; set; }
        public string Filler { get; set; }

        public enum FieldIndex : int
        {
            RecordType = 0
            ,
            PrimaryArpID = 2
                ,
            SecondaryArpID = 15
                ,
            ModifiedStamp = 28
                , Filler = 54
        }

        public enum FieldLength : int
        {
            RecordType = 2
            ,
            PrimaryArpID = 13
                ,
            SecondaryArpID = 13
                ,
            ModifiedStamp = 26
                , Filler = 496
        }
    }




}
