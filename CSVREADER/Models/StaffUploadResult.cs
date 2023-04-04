namespace CSVREADER.Models
{
    public class StaffUploadResult
    {
        public List<string> Headers
        {
            get; set;
        }
        public List<CSV> StaffRecords
        {
            get; set;
        }
    }
}
