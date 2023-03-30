using System.ComponentModel.DataAnnotations;

namespace CSVREADER.Models
{
    public class CSV
    {
        public int Id {get; set;}
        public string FirstName
        { get; set;}
        public string Staff_LastName
        {
            get; set;
        }
        
        public string Staff_ContactNo
        {
            get; set;
        }

    }
}
