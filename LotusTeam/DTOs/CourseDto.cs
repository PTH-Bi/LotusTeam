namespace LotusTeam.DTOs
{
    public class CourseDto
    {
        public int CourseId { get; set; }

        public string CourseName { get; set; } = null!;

        public string? Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public string? Trainer { get; set; }

        public decimal Cost { get; set; }

        public string? Location { get; set; }
    }
}