using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Server.Models
{
    public class WorkdayModel
    {
        public int id { get; set; }

        public DateTime date { get; set; }

        public DateTime startDate { get; set; }

        public DateTime endDate { get; set; }

        public string signatureType { get; set; }

        public int aom { get; set; }

        public string typeCode { get; set; }

        public int userId { get; set; }
    }
}
