using System.ComponentModel.DataAnnotations;

namespace CSVREADER.Models
{
    public class Staff
    {
        [Key]
        public long StaffID
        {
            get; set;
        }

        public string StFirstName
        {
            get; set;
        }


        public string StLastName
        {
            get; set;
        }

        [StringLength(10, ErrorMessage = "Contact Number cannot exceed 10 characters. ")]
        public string StContactNo
        {
            get; set;
        }
        public int EmailVerifactionStatus
        {
            get; set;
        }
        public int MobileVerificationStatus
        {
            get; set;
        }


        public string StEmailName
        {
            get; set;
        }

        public DateTime StBirthdate
        {
            get; set;
        }

        public Boolean Active
        {
            get; set;
        }

    }
}
