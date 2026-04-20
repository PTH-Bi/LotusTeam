namespace LotusTeam.Models
{
    public class Training
    {
        public int TrainingID { get; set; }
        public string TrainingName { get; set; } = null!;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Trainer { get; set; }
        public decimal Cost { get; set; }
        public string? Location { get; set; }

        public ICollection<EmployeeTraining>? EmployeeTrainings { get; set; }
    }

}
