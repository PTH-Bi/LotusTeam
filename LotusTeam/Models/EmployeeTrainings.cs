namespace LotusTeam.Models
{
    public class EmployeeTraining
    {
        public int EmployeeTrainingID { get; set; }
        public int EmployeeID { get; set; }
        public int TrainingID { get; set; }
        public short CompletionStatus { get; set; }
        public bool CertificateIssued { get; set; }

        public Employees Employee { get; set; } = null!;
        public Training Training { get; set; } = null!;
    }

}
