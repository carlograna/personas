using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PersonLoad
{
    class RecordType01
    {
        //private string recordType;
        //private int recordCount;
        //private DateTime creationStamp;
        //private string filler;

        public string RecordType { get; set; }
        public int RecordCount { get; set; }
        public DateTime CreationStamp { get; set; }
        public string Filler { get; set; }

        public enum FieldIndex : int
        {
            RecordType = 0,
            RecordCount = 2,
            CreationStamp = 9,
            Filler = 35
        }

        public enum FieldLength : int
        {
            RecordType = 2,
            RecordCount = 7,
            CreationStamp = 26,
            Filler = 515
        }        
    }
}
