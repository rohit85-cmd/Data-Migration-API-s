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

        DateTime now = DateTime.Now;

        var filePath = Path.Combine(_environment.ContentRootPath, "uploads", DateTime.Now.ToString("yyyyMMdd_hhmmss") + file.FileName);
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

        /*foreach (var header in headers)
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
        */

        foreach (var csvmodelheader in CSVModelHeaders)
        {
            if (!headers.Contains(csvmodelheader))
            {
                // Delete the file
                System.IO.File.Delete(filePath);
                return BadRequest("Template file is tempered");
            }
        }

        //-----------------Validating csv file headers with csv model-----------------------


        using var reader2 = new StreamReader(filePath);
        using var csv2 = new CsvReader(reader2, CultureInfo.InvariantCulture);

        Dictionary<int, string> rowErrors = new Dictionary<int, string>(); // to store the line number and text which is invalid
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

                Console.WriteLine(ex.Text);


                rowErrors.Add(lineNumber, ex.Text);
            }
            lineNumber++;
        }
        try
        {
            var errorBuilder = new StringBuilder();
            foreach (var error in rowErrors)
            {
                errorBuilder.AppendLine($"DataType mismatch on line {error.Key}: Something wrong with text '{error.Value}'");
            }

            if (errorBuilder.Length > 0)
            {
                reader2.Close();
                csv2.Dispose();
                System.IO.File.Delete(filePath);

                return BadRequest(errorBuilder.ToString());
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

}

