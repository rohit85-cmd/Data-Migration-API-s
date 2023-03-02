using System.ComponentModel.DataAnnotations;

namespace CSVREADER.Models
{
    public class Staff
    {
        [Key]
        public int Staff_Id { get; set; }

        [Required]
        public string Staff_FirstName { get; set; }
        [Required]
        public string Staff_LastName { get;set; }
        [Required]
        public string Staff_ContactNo { get; set; }
    }
}
