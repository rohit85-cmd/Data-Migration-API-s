using CsvHelper;
using CSVREADER.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using CSVREADER.Data;
using Microsoft.EntityFrameworkCore;
using CsvHelper.Configuration;
using System.Text;
using System;

[ApiController]
[Route("api/staffAPI")]
public class StaffAPIController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _db;

    public StaffAPIController(IWebHostEnvironment environment, ApplicationDbContext db)
    {
        _environment = environment;
        _db = db;
    }

    //Get All content from csv

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Staff>>> GetAllCSVContent()
    {
        IEnumerable<Staff> StaffList = await _db.StaffData.ToListAsync();
        return Ok(StaffList);
    }




    //Migrating csv content to sql DB
    [HttpPost]
    [ProducesResponseType (StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]

    public async Task<IActionResult> ReadCSVContent(IFormFile file) 
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


                await _db.StaffData.AddRangeAsync(staffRecords);
                await _db.SaveChangesAsync();

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


    [HttpDelete ("id : int")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteStaffRecord(int id)
    {
        if(id==0)
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

        return NoContent();
    }
}

public class StaffMapByIndex : ClassMap<Staff>
{
    public StaffMapByIndex()
    {
        Map(s => s.Staff_Id).Index(0);
        Map(s => s.Staff_FirstName).Index(1);
        Map(s => s.Staff_LastName).Index(2);
        Map(s => s.Staff_ContactNo).Index(3);
    }
}

