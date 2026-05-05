using System.ComponentModel.DataAnnotations;

namespace LotusTeam.Models
{
    public class Gender
    {
        [Required(ErrorMessage = "Giới tính là bắt buộc")]

        public byte GenderID { get; set; }
        public string GenderCode { get; set; } = null!;
        public string GenderName { get; set; } = null!;

        public ICollection<Employees>? Employees { get; set; } = new List<Employees>();
    }

}
    