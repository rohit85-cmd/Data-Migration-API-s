using CsvHelper;
using CSVREADER.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using CSVREADER.Data;
using Microsoft.EntityFrameworkCore;
using CsvHelper.Configuration;
using System.Text;
using System.Diagnostics;
using Swashbuckle.AspNetCore.Annotations;
using System.Data.Common;

[ApiController]
[Route("api/staffAPI")]
public class StaffAPIController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _db;

    List<string> columnList = new List<string>();
    public StaffAPIController(IWebHostEnvironment environment, ApplicationDbContext db)
    {
        _environment = environment;
        _db = db;


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
                columnList.Add(columnName);

            }
        }
        connection.Close();
        foreach(string columnName in columnList)
        {
            Console.WriteLine($"Column Name: {columnName}");
        }
        
    }




    [HttpGet]
    [SwaggerOperation(Summary = "Returns staff records", Description = "Returns all staff records from the database.")]
    [SwaggerResponse(200, "OK", typeof(IEnumerable<Staff>))]
    //[ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Staff>>> GetAllCSVContent()
    {
        var watch = new Stopwatch();
        watch.Start();
        IEnumerable<Staff> StaffList = await _db.StaffData.ToListAsync();
        watch.Stop();
        Console.WriteLine($"Response Time of API: {(float)(watch.ElapsedMilliseconds) / 1000} seconds");
        return Ok(StaffList);
    }




    //Migrating csv content to sql DB
    [HttpPost]
    [SwaggerOperation(Summary = "Uploads Staff CSV to SQL Database", Description = "Uploads Staff CSV to SQL Database with its respective matched column")]
    [SwaggerResponse(200, "OK", typeof(IEnumerable<Staff>))]
    [SwaggerResponse(400,"BadRequest")]
    //[ProducesResponseType (StatusCodes.Status200OK)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]

    public async Task<IActionResult> ReadCSVContent(IFormFile file) 
    {
        var watch = new Stopwatch();
        watch.Start();


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

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        using (var reader = new StreamReader(filePath))
        using (var csv = new CsvReader(reader, configuration))
        {
            
            try
            {
                
                csv.Context.RegisterClassMap<StaffMapByIndex>(); //Registering manual mapping

                var staffRecords = csv.GetRecords<Staff>().ToList();
                //Console.WriteLine(staffRecords.GetType());

                /*
                foreach (var record in staffRecords)
                {
                    Console.WriteLine($"{record.Staff_Id},{record.Staff_FirstName},{record.Staff_LastName},{record.Staff_ContactNo}");
                }
                */

                await _db.StaffData.AddRangeAsync(staffRecords);
                await _db.SaveChangesAsync();


                watch.Stop();
                Console.WriteLine($"Response Time of API: {(float)(watch.ElapsedMilliseconds) / 1000} seconds");

                return Ok(staffRecords);
            }
            catch (Exception)
            {
                //if duplicate records
                //if datatypes and datafields are not same as Staff Model we created
                //then returning badRequest

                return BadRequest("Bad Data Found");
            }   
        }
    }



   
    [HttpDelete ("{id:int}")]

    [SwaggerOperation(Summary = "Deletes staff record by ID", Description = "Deletes staff record with the specified ID from SQL Database.")]

    [SwaggerResponse (204,"OK")]
    [SwaggerResponse (400,"BadRequest")]
    [SwaggerResponse(404, "NotFound")]

    //[ProducesResponseType(StatusCodes.Status204NoContent)]
    //[ProducesResponseType(StatusCodes.Status404NotFound)]
    //[ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteStaffRecord(int id)
    {
        var watch = new Stopwatch();
        watch.Start();

        if (id==0)
        {
            return BadRequest();
        }

        var staff = await _db.StaffData.FirstOrDefaultAsync(s=>s.Staff_Id== id);
        if(staff == null)
        {
            return NotFound();
        }

        _db.StaffData.Remove(staff);
        await _db.SaveChangesAsync();

        watch.Stop();
        Console.WriteLine($"Response Time of API: {(float)(watch.ElapsedMilliseconds) / 1000} seconds");
        return NoContent();
    }
}

public class StaffMapByIndex : ClassMap<Staff>
{
    public StaffMapByIndex()
    {
        Map(s => s.Staff_Id).Name("Id");
        Map(s => s.Staff_FirstName).Name("FirstName");
        Map(s => s.Staff_LastName).Index(2);
        Map(s => s.Staff_ContactNo).Index(3);
    }
}

