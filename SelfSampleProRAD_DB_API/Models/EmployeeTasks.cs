using System.ComponentModel.DataAnnotations;

namespace SelfSampleProRAD_DB_API.Models
{
    public class EmployeeTasks
    {
        [Key]
        [Required]
        public Guid ETID { get; set; }

        [Required]
        public Guid TaskId { get; set; }

        [Required]
        public Guid AssignedToId { get; set; }

        [Required]
        public Guid AssignedById { get; set; }

        public virtual Employee AssignedTo { get; set; }
        public virtual Employee AssignedBy { get; set; }
        public virtual Tasks Tasks { get; set; }

        public EmployeeTasks()
        {
            ETID = Guid.NewGuid();
        }
    }

}
