using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using CSVREADER.Data;
using CSVREADER.Models;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.Data.Common;
using System.Globalization;
using System.Text;

[ApiController]
[Route("api/staffAPI")]
[EnableCors]
public class StaffAPIController : ControllerBase
{

    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _db;
    private readonly IMapper _mapper;

    List<string> DbColumnList = new List<string>();





    public StaffAPIController(IWebHostEnvironment environment, ApplicationDbContext db, IMapper mapper)
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
    [SwaggerOperation
    (
        Summary = "Returns the columns of database table",
        Description = "Returns the columns of database table using Information Schema in SQL"
    )
    ]

    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(400, "BadRequest")]
    [SwaggerResponse(404, "NotFound")]
    public List<string> GetHeaders()
    {
        return DbColumnList;
    }



    [HttpPost("upload")]
    [SwaggerOperation
    (
        Summary = "Upload the csv file if it is valid",
        Description = "Only uploaded if file is in provided template format."
    )
    ]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(400, "BadRequest")]

    public async Task<ActionResult> uploadFile(IFormFile file)
    {
        List<CSV> StaffRecords = new List<CSV>();
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
            return BadRequest("File with same name already exist in system. Change file name.");
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



        //----------------Validating csv file headers with csv model-------------------------
        var CSVModelHeaders = new List<string>();
        var WrongHeaders = new List<string>();

        CSV Csv = new CSV();


        foreach (var property in Csv.GetType().GetProperties())
        {

            CSVModelHeaders.Add(property.Name);
        }

        foreach (var header in headers)
        {
            if (!CSVModelHeaders.Contains(header))
            {
                WrongHeaders.Add(header);
            }
        }

        if (WrongHeaders.Count > 0)
        {


            // Delete the file
            System.IO.File.Delete(filePath);
            return BadRequest("Wrong Headers");
        }
        //-----------------Validating csv file headers with csv model-----------------------


        using var reader2 = new StreamReader(filePath);
        using var csv2 = new CsvReader(reader2, CultureInfo.InvariantCulture);
        var invalidRows = new List<int>(); // to store the line numbers of invalid rows
        var lineNumber = 2; // initialize line number. (1 ---> header Row)
        while (csv2.Read())
        {
            try
            {
                var record = csv2.GetRecord<CSV>();
                StaffRecords.Add(record);

                var staff = _mapper.Map<Staff>(record);
                _db.Add(staff);
            }

            catch (CsvHelper.TypeConversion.TypeConverterException ex)
            {
                invalidRows.Add(lineNumber); // store the line number of the invalid row
            }
            lineNumber++;
        }
        try
        {
            if (invalidRows.Any())
            {
                reader2.Close();
                csv2.Dispose();
                System.IO.File.Delete(filePath);
                return BadRequest($"Csv file contains invalid datatype fields on row(s) {string.Join(",", invalidRows)}. Please make sure that you entered data is valid.");
            }
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2627)
        {
            reader2.Close();
            csv2.Dispose();
            // Handle the specific error related to duplicate keys
            System.IO.File.Delete(filePath);

            return BadRequest("A record with the same key value already exists in the database.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2628)
        {
            reader2.Close();
            csv2.Dispose();

            System.IO.File.Delete(filePath);

            return BadRequest("String or binary data would be truncated in table 'CSV.dbo.StaffData'");
        }
        catch (Exception ex)
        {
            reader2.Close();
            csv2.Dispose();
            // Handle the specific error related to duplicate keys
            System.IO.File.Delete(filePath);

            return BadRequest(ex);
        }

        var result = new StaffUploadResult
        {
            Headers = headers,
            StaffRecords = StaffRecords,
        };

        return Ok(result);

    }


    [HttpGet("download/{fileName}")]
    [SwaggerOperation
    (
        Summary = "Downloads csv file by name of file",
        Description = "If file exist in backend, it gets downloaded."
    )
    ]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(400, "BadRequest")]
    [SwaggerResponse(404, "NotFound")]
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


    [HttpPost("migrate/{fileName}")]
    [SwaggerOperation
    (
        Summary = "Migrate csv content to sql table by using file name"
    )
    ]
    [SwaggerResponse(200, "OK")]
    [SwaggerResponse(400, "BadRequest")]
    [SwaggerResponse(404, "NotFound")]
    public async Task<ActionResult> MigrateCsvToSql(Dictionary<String, String> mapping, string fileName)
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
            var response = await client.GetAsync(downloadUrl);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStreamAsync();

                // Return the downloaded file as the HTTP response
                file = File(content, "application/octet-stream", $"{fileName}");

                using var reader = new StreamReader(content);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);



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

