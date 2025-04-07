using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace schedule_2.Models
{
    public class StudentSubgroup
    {
        public int StudentId { get; set; }
        public int SubgroupId { get; set; }

        public virtual Student Student { get; set; } = null!;
        public virtual Subgroup Subgroup { get; set; } = null!;
    }
}