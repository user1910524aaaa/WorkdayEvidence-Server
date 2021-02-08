using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Models
{
    public class TimeSignatureModel
    {
        public int id { get; set; }

        public DateTime startDate { get; set; }

        public DateTime endDate { get; set; }

        public int typeId { get; set; }

        public string typeCode { get; set; }

        public int stateId { get; set; }

        public string stateCode { get; set; }

        public int wd_typeId { get; set; }

        public string wd_typeCode { get; set; }

        public int aom { get; set; }

        public int? workdayId { get; set; }

        public int userId { get; set; }

        public DateTime lastUpdate { get; set; }

        public string updateBy { get; set; }


        public bool fullDay { get; set; }
        public bool finish { get; set; }

    }
}
