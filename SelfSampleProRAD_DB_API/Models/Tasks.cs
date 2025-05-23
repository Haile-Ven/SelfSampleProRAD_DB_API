﻿using System.ComponentModel.DataAnnotations;

namespace SelfSampleProRAD_DB_API.Models
{
    public class Tasks
    {
        //Auto-Properties
        [Key]
        public Guid TaskId { get; set; }
        [Required]
        public string TaskName { get; set; }
        [Required]
        public char Status { get; set; }

        public virtual ICollection<EmployeeTasks> EmployeeTasks { get; set; }
        //Constructors
        public Tasks() { TaskId = Guid.NewGuid(); }
    }
}
