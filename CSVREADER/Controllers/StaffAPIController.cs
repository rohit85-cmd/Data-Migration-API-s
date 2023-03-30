using CsvHelper;
using CSVREADER.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using CSVREADER.Data;
using Microsoft.EntityFrameworkCore;
using CsvHelper.Configuration;
using System.Text;
using Swashbuckle.AspNetCore.Annotations;
using System.Data.Common;
using AutoMapper;
using Microsoft.AspNetCore.Cors;
using Microsoft.Data.SqlClient;

[ApiController]
[Route("api/staffAPI")]
[EnableCors]
public class StaffAPIController : ControllerBase
{

    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;

    List<string> DbColumnList = new List<string>();





    public StaffAPIController(IWebHostEnvironment environment, ApplicationDbContext db,IMapper mapper)
    {
        _environment = environment;
        _db = db;
        _mapper = mapper;



        DbConnection connection = _db.Database.GetDbConnection();
        connection.Open();
        DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'StaffData'";

        using (DbDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                string columnName = reader["COLUMN_NAME"].ToString();
                // do something with the column name
                DbColumnList.Add(columnName);

            }
        }
        connection.Close();


    }


    [HttpGet("headers")]
    [SwaggerOperation(Summary = "Returns the header columns of database")]

    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(400, "BadRequest")]
    [SwaggerResponse(404, "NotFound")]
    public List<string> GetHeaders()
    {
        return DbColumnList;
    }



    [HttpPost("upload")]

    public async Task<ActionResult> uploadFile(IFormFile file)
    {
        
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file selected");
        }

        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8, // Our file uses UTF-8 encoding.
            Delimiter = "," // The delimiter is a comma.
        };

        var filePath = Path.Combine(_environment.ContentRootPath, "uploads", file.FileName);
        if (System.IO.File.Exists(filePath))
        {
            return BadRequest("File already exists in system change file name.");
        }

        using (var stream = new FileStream(filePath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream);
        }

        using var reader = new StreamReader(filePath);
        List<string> headers = new List<string>();
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();
            headers = csv.HeaderRecord.ToList();
        }

        //Validating csv file headers with csv model
        var CSVModelHeaders = new List<string>();
        var WrongHeaders = new List<string>();

        CSV Csv = new CSV();
        

        foreach (var property in Csv.GetType().GetProperties())
        {
            Console.WriteLine(property.Name);
            CSVModelHeaders.Add(property.Name);
        }

        foreach(var header in headers)
        {
            if (!CSVModelHeaders.Contains(header))
            {
                WrongHeaders.Add(header);
            }
        }

        if(WrongHeaders.Count > 0)
        {
            // Delete the file
            System.IO.File.Delete(filePath);
            return BadRequest("Wrong Headers");
        }

        return Ok("UPLOADED");

    }


    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> DownloadFile(string fileName)
    {
        var filePath = Path.Combine(_environment.ContentRootPath, "uploads", $"{fileName}");
        
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("file not found");
        }

        var memoryStream = new MemoryStream();
        using (var stream = new FileStream(filePath, FileMode.Open))
        {
            await stream.CopyToAsync(memoryStream);
        }
        memoryStream.Position = 0;
        Console.WriteLine(memoryStream.ToString());

        var contentType = "application/octet-stream";
        var fileDownloadName = fileName;
        return File(memoryStream, contentType, fileDownloadName);
    }


    [HttpPost ("migrate/{fileName}")]
    public async Task<ActionResult> MigrateCsvToSql(Dictionary<String,String>mapping, string fileName)
    {
        
        // Check if the file exists in the server's file system
        var filePath = Path.Combine(_environment.ContentRootPath, "uploads", $"{fileName}");
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound("file not found");
        }
        ActionResult file;
        
        // Download the file using the DownloadFile API
        var downloadUrl = $"https://localhost:7096/api/staffAPI/download/{fileName}";
        using (var client = new HttpClient())
        {
            var response =  await client.GetAsync(downloadUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStreamAsync();

                // Return the downloaded file as the HTTP response
                file = File(content, "application/octet-stream", $"{fileName}");

                using var reader = new StreamReader(content);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                
                //csv.Context.RegisterClassMap<StaffMapByDictionary>(); //Registering manual mapping

                //var staffRecords = csv.GetRecords<Staff>().ToList();

                // Map the CSV file records to Staff model using AutoMapper
                var records = csv.GetRecords<CSV>().ToList();
                var staffRecords = records.Select(record => _mapper.Map<Staff>(record));


                try
                {
                    
                    await _db.AddRangeAsync(staffRecords);
                    await _db.SaveChangesAsync();

                    return Ok(staffRecords);
                }
                catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601)
                {
                    // Handle the specific error related to duplicate keys
                    return BadRequest("A record with the same key value already exists in the database.");
                }


            }
        }

        // If the file download failed, return an error response
        return BadRequest("Unable to download file");

       
    }

    

}

public class StaffMapByDictionary : ClassMap<Staff>
{
    public StaffMapByDictionary()
    {
        
        Map(s => s.Staff_Id).Name("Id");
        Map(s => s.Staff_FirstName).Name("FirstName");
        Map(s => s.Staff_LastName).Index(2);
        Map(s => s.Staff_ContactNo).Index(3);
        

       /*/foreach (var kvp in mapping)
        {
            Map(s => $"s.{kvp.Key}").Name($"{kvp.Value}");
        }
       */
    }
}
