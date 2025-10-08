using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using backend.DTOs;
using System.Data;
using CsvHelper;
using System.Globalization;

namespace backend.controllers
{
    [ApiController]
    [Route("api")]
    public class ImportSupplierController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly ILogger<ImportSupplierController> _logger;

        public ImportSupplierController(IConfiguration configuration, ILogger<ImportSupplierController> logger)
        {
            _connectionString = configuration.GetConnectionString("OracleDb")
                                ?? throw new InvalidOperationException("Connection String not found");
            _logger = logger;
        }

        [HttpPost("importcsv")]
        public IActionResult ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Cannot upload an empty file");

            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();


                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                var records = csv.GetRecords<SupplierDto>().ToList();

                // Print CSV data for verification
                foreach (var r in records)
                {
                    Console.WriteLine($"Supplier: {r.Supplier_Name}, Contact: {r.Contact_Name}, Phone: {r.Contact_Phone}");
                    _logger.LogInformation("Supplier DTO: {@Record}", r);
                }

                // Prepare array for Oracle procedure
                var dataList = records.Select(r => string.Join(',',
                    r.Supplier_ID,
                    r.Supplier_Name,
                    r.Contact_Name,
                    r.Contact_Phone,
                    r.Contact_Email,
                    r.Address,
                    r.City,
                    r.State,
                    r.Postal_Code,
                    r.Country,
                    r.Tax_Identification
                )).ToArray();


                using var cmd = new OracleCommand("supplier_pkg.sup_import_csv", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.BindByName = true;

                var p_data = new OracleParameter("p_data", OracleDbType.Varchar2)
                {
                    CollectionType = OracleCollectionType.PLSQLAssociativeArray,
                    Value = dataList,
                    Size = dataList.Length,
                    ArrayBindSize = Enumerable.Repeat(4000, dataList.Length).ToArray(),
                    Direction = ParameterDirection.Input
                };
                cmd.Parameters.Add(p_data);
                cmd.ExecuteNonQuery();

                return Ok(new { Message = "CSV imported successfully.", Rows = records.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("submitForm")]
        public IActionResult SubmitForm([FromBody] SupplierDto supplier)
        {
            if (supplier == null)
                return BadRequest("Cannot Submit without form data");

            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();

                using var cmd = new OracleCommand("supplier_pkg.sup_import", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("p_sup_id", OracleDbType.Int32).Value = supplier.Supplier_ID;
                cmd.Parameters.Add("p_sup_name", OracleDbType.Varchar2).Value = supplier.Supplier_Name;
                cmd.Parameters.Add("p_name", OracleDbType.Varchar2).Value = supplier.Contact_Name;
                cmd.Parameters.Add("p_phone", OracleDbType.Varchar2).Value = supplier.Contact_Phone;
                cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = supplier.Contact_Email;
                cmd.Parameters.Add("p_address", OracleDbType.Varchar2).Value = supplier.Address;
                cmd.Parameters.Add("p_city", OracleDbType.Varchar2).Value = supplier.City;
                cmd.Parameters.Add("p_state", OracleDbType.Varchar2).Value = supplier.State;
                cmd.Parameters.Add("p_postal_code", OracleDbType.Int32).Value = supplier.Postal_Code;
                cmd.Parameters.Add("p_country", OracleDbType.Varchar2).Value = supplier.Country;
                cmd.Parameters.Add("p_tax_id", OracleDbType.Varchar2).Value = supplier.Tax_Identification;

                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = ex.Message });
            }
            return Ok(new { recievedId = supplier.Supplier_ID, message = "User saved successfully" });
        }

        [HttpDelete("deletesuppliers")]
        public IActionResult DeleteSuppliers()
        {
            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();

                using var cmd = new OracleCommand("truncate table suppliers_masters", conn);
                cmd.CommandType = CommandType.Text;

                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = ex.Message });
            }
            return Ok(new {Message = "Data Deleted Successfully"});
        }

        [HttpGet("SupplierData")]
        public IActionResult SupplierData()
        {
            var result = new List<object>();

            try
            {
                using var conn = new OracleConnection(_connectionString);
                conn.Open();

                using var cmd = new OracleCommand("supplier_pkg.sup_export", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                using var outParam = cmd.Parameters.Add("p_result", OracleDbType.RefCursor);
                outParam.Direction = ParameterDirection.Output;

                cmd.ExecuteNonQuery();

                using var reader = ((OracleRefCursor)outParam.Value).GetDataReader();

                if (!reader.HasRows)
                {
                    return Ok(new { message = "No Data in the table" });
                }
                while (reader.Read())
                {
                    
                    result.Add(new
                    {
                        Sup_id = reader.GetInt32(reader.GetOrdinal("sup_id")),
                        Sup_Name = reader.GetString(reader.GetOrdinal("sup_name")),
                        Name = reader.GetString(reader.GetOrdinal("contact_name")),
                        Phone = reader.GetString(reader.GetOrdinal("contact_phone")),
                        Email = reader.GetString(reader.GetOrdinal("contact_email")),
                        Address = reader.GetString(reader.GetOrdinal("address")),
                        City = reader.GetString(reader.GetOrdinal("city")),
                        State = reader.GetString(reader.GetOrdinal("state")),
                        Postal_code = reader.GetString(reader.GetOrdinal("postal_code")),
                        Country = reader.GetString(reader.GetOrdinal("country")),
                        Tax_id = reader.GetString(reader.GetOrdinal("tax_id")),
                        Error_msg = reader.GetString(reader.GetOrdinal("Error_msg"))
                    });
                }

            }
            catch (Exception ex)
            {

                return StatusCode(500, new { error = ex.Message });
            }
            return Ok(new { Message = "Data has been fetched successfully" ,Data=result});
        }
    }
}
