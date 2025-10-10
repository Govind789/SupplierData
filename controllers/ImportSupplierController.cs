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

        [HttpGet]
        public IActionResult Test()
        {
            var origin = Request.Headers["Origin"].ToString();
            Console.WriteLine($"Incoming Origin: {origin}");
            return Ok("CORS test");
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

                var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    IgnoreBlankLines = true
                };

                using var csv = new CsvReader(reader, csvConfig);
                var records = csv.GetRecords<SupplierDto>().Where(r => r.Supplier_ID != 0).ToList();


                // Normalize & validate data before sending to Oracle
                foreach (var r in records)
                {
                    r.Supplier_Name = r.Supplier_Name?.Trim() ?? "";
                    r.Contact_Name = r.Contact_Name?.Trim() ?? "";
                    r.Contact_Phone = r.Contact_Phone?.Trim() ?? "";
                    r.Contact_Email = r.Contact_Email?.Trim() ?? "";
                    r.Address = r.Address?.Trim() ?? "";
                    r.City = r.City?.Trim() ?? "";
                    r.State = r.State?.Trim() ?? "";
                    r.Country = r.Country?.Trim() ?? "";
                    r.Tax_Identification = r.Tax_Identification?.Trim() ?? "";

                    int fallbackPostalInt = 0;
                    if (string.IsNullOrWhiteSpace(r.Postal_Code) || !int.TryParse(r.Postal_Code.Trim(), out int parsedPostal))
                    {
                        _logger.LogWarning($"Invalid or missing postal code '{r.Postal_Code}' — forcing fallback.");
                        r.Postal_Code = fallbackPostalInt.ToString();
                    }
                    else
                    {
                        r.Postal_Code = parsedPostal.ToString();
                    }
                }

                // Prepare Oracle-compatible array
                var dataList = records.Select(r => string.Join(',',
                    r.Supplier_ID,
                    EscapeCsvField(r.Supplier_Name),
                    EscapeCsvField(r.Contact_Name),
                    EscapeCsvField(r.Contact_Phone),
                    EscapeCsvField(r.Contact_Email),
                    EscapeCsvField(r.Address),
                    EscapeCsvField(r.City),
                    EscapeCsvField(r.State),
                    r.Postal_Code,
                    EscapeCsvField(r.Country),
                    EscapeCsvField(r.Tax_Identification)
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

        private static string EscapeCsvField(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n');
            if (needsQuotes)
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
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
                cmd.Parameters.Add("p_postal_code", OracleDbType.Int32).Value = int.TryParse(supplier.Postal_Code, out int pcode) ? pcode : 0;
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
            return Ok(new { Message = "Data Deleted Successfully" });
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
            return Ok(new { Message = "Data has been fetched successfully", Data = result });
        }

    }
}
