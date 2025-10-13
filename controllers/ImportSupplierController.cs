using Microsoft.AspNetCore.Mvc;
using backend.DTOs;
using System.Data;
using Microsoft.Data.SqlClient;
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
            _connectionString = configuration.GetConnectionString("SqlServerDB")
                                ?? throw new InvalidOperationException("Connection String not found");
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Welcome()
        {
            var html = @"<!DOCTYPE html>
            <html lang='en'>
            <head>
            <meta charset='UTF-8' />
            <meta name='viewport' content='width=device-width, initial-scale=1.0' />
            <title>Supplier Data API Portal</title>
            <style>
                body {
                margin: 0;
                padding: 0;
                height: 100vh;
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                background: linear-gradient(135deg, #1e3c72, #2a5298);
                display: flex;
                justify-content: center;
                align-items: center;
                color: #fff;
                overflow: hidden;
                }

                .card {
                background: rgba(255, 255, 255, 0.12);
                backdrop-filter: blur(15px);
                box-shadow: 0 10px 35px rgba(0, 0, 0, 0.25);
                border-radius: 20px;
                padding: 2.2rem 2.8rem;
                text-align: center;
                max-width: 700px;
                width: 90%;
                animation: fadeIn 1s ease-in-out;
                }

                h1 {
                font-size: 1.9rem;
                margin-bottom: 0.7rem;
                letter-spacing: 1px;
                }

                p {
                font-size: 1rem;
                line-height: 1.5;
                margin-bottom: 1rem;
                opacity: 0.9;
                }

                .links {
                display: grid;
                grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
                gap: 0.8rem;
                margin-top: 1rem;
                }

                .link-card {
                background: rgba(255, 255, 255, 0.18);
                border-radius: 14px;
                padding: 0.9rem;
                transition: transform 0.2s ease, background 0.3s ease;
                }

                .link-card:hover {
                transform: translateY(-4px);
                background: rgba(255, 255, 255, 0.25);
                }

                a {
                color: #fff;
                text-decoration: none;
                font-weight: 600;
                }

                a:hover {
                text-decoration: underline;
                }

                .icon {
                font-size: 1.3rem;
                margin-right: 0.4rem;
                }

                footer {
                margin-top: 1.2rem;
                font-size: 0.85rem;
                opacity: 0.75;
                }

                @keyframes fadeIn {
                from {
                    opacity: 0;
                    transform: translateY(25px);
                }
                to {
                    opacity: 1;
                    transform: translateY(0);
                }
                }
            </style>
            </head>
            <body>
            <div class='card'>
                <h1>👋 Welcome to <b>Supplier Data API</b></h1>
                <p>
                The backend service for managing suppliers — including CSV import, validation, 
                and integration with the live frontend dashboard.
                </p>

                <div class='links'>
                <div class='link-card'>
                    <span class='icon'>🌐</span>
                    <a href='https://supplierdata1.netlify.app' target='_blank'>
                    Visit Frontend Application
                    </a>
                    <p>Live and interactive supplier dashboard.</p>
                </div>

                <div class='link-card'>
                    <span class='icon'>📘</span>
                    <a href='https://supplierdata.runasp.net/swagger/index.html' target='_blank'>
                    Swagger API Documentation
                    </a>
                    <p>Test all API endpoints directly online.</p>
                </div>

                <div class='link-card'>
                    <span class='icon'>💾</span>
                    <a href='https://github.com/Govind789/SupplierData' target='_blank'>
                    Backend Source Code
                    </a>
                    <p>Built using <b>.NET 8</b> and <b>SQL/Oracle</b>.</p>
                </div>

                <div class='link-card'>
                    <span class='icon'>💻</span>
                    <a href='https://github.com/Govind789/SupplierDataFrontend' target='_blank'>
                    Frontend Source Code
                    </a>
                    <p>Developed using <b>React + Vite</b>.</p>
                </div>

                <div class='link-card'>
                    <span class='icon'>📄</span>
                    <a href='https://drive.google.com/file/d/10_-t12rqIsuMQs-k8HNr-GcLexaEnW-_/view?usp=sharing' target='_blank'>
                    PL/SQL Tables & Procedures
                    </a>
                    <p>View database schema and stored procedure scripts.</p>
                </div>
                </div>

                <footer>© 2025 Supplier Data System. All Rights Reserved.</footer>
            </div>
            </body>
            </html>
            ";
            return new ContentResult
            {
                ContentType = "text/html",
                Content = html,
                StatusCode = 200
            };
        }

        [HttpPost("importcsv")]
        public IActionResult ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Cannot upload an empty file");

            try
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var csvConfig = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    TrimOptions = CsvHelper.Configuration.TrimOptions.Trim,
                    MissingFieldFound = null,
                    HeaderValidated = null,
                    IgnoreBlankLines = true
                };

                using var csv = new CsvReader(reader, csvConfig);
                var records = csv.GetRecords<SupplierDto>().ToList();

                // Convert to DataTable for TVP
                var table = new DataTable();
                table.Columns.Add("sup_id", typeof(int));
                table.Columns.Add("sup_name", typeof(string));
                table.Columns.Add("contact_name", typeof(string));
                table.Columns.Add("contact_phone", typeof(string));
                table.Columns.Add("contact_email", typeof(string));
                table.Columns.Add("address", typeof(string));
                table.Columns.Add("city", typeof(string));
                table.Columns.Add("state", typeof(string));
                table.Columns.Add("postal_code", typeof(string));
                table.Columns.Add("country", typeof(string));
                table.Columns.Add("tax_id", typeof(string));

                foreach (var r in records)
                {
                    table.Rows.Add(
                        r.Supplier_ID,
                        r.Supplier_Name?.Trim() ?? "",
                        r.Contact_Name?.Trim() ?? "",
                        r.Contact_Phone?.Trim() ?? "",
                        r.Contact_Email?.Trim() ?? "",
                        r.Address?.Trim() ?? "",
                        r.City?.Trim() ?? "",
                        r.State?.Trim() ?? "",
                        r.Postal_Code?.Trim() ?? "",
                        r.Country?.Trim() ?? "",
                        r.Tax_Identification?.Trim() ?? ""
                    );
                }

                // Call stored procedure
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                using var cmd = new SqlCommand("dbo.sup_import_csv", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                var param = cmd.Parameters.AddWithValue("@suppliers", table);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = "dbo.SupplierType"; // make sure you created this TVP in SQL

                cmd.ExecuteNonQuery();

                return Ok(new CsvImportResultDto
                {
                    Message = "CSV imported successfully",
                    Rows = records.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CSV");
                return StatusCode(500, new ErrorDto { Error = ex.Message });
            }
        }

        [HttpPost("submitForm")]
        public IActionResult SubmitForm([FromBody] SupplierDto supplier)
        {
            if (supplier == null)
                return BadRequest(new ErrorDto { Error = "Cannot submit empty form" });

            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using var cmd = new SqlCommand("dbo.sup_import_csv_single", conn);
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.AddWithValue("@sup_id", supplier.Supplier_ID);
                cmd.Parameters.AddWithValue("@sup_name", supplier.Supplier_Name ?? "");
                cmd.Parameters.AddWithValue("@contact_name", supplier.Contact_Name ?? "");
                cmd.Parameters.AddWithValue("@contact_phone", supplier.Contact_Phone ?? "");
                cmd.Parameters.AddWithValue("@contact_email", supplier.Contact_Email ?? "");
                cmd.Parameters.AddWithValue("@address", supplier.Address ?? "");
                cmd.Parameters.AddWithValue("@city", supplier.City ?? "");
                cmd.Parameters.AddWithValue("@state", supplier.State ?? "");
                cmd.Parameters.AddWithValue("@postal_code", supplier.Postal_Code ?? "");
                cmd.Parameters.AddWithValue("@country", supplier.Country ?? "");
                cmd.Parameters.AddWithValue("@tax_id", supplier.Tax_Identification ?? "");

                cmd.ExecuteNonQuery();

                return Ok(new SupplierSaveResultDto
                {
                    ReceivedId = supplier.Supplier_ID,
                    Message = "Supplier saved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting form");
                return StatusCode(500, new ErrorDto { Error = ex.Message });
            }
        }

        [HttpDelete("deletesuppliers")]
        public IActionResult DeleteSuppliers()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                using var cmd = new SqlCommand("TRUNCATE TABLE dbo.suppliers_masters", conn);
                cmd.ExecuteNonQuery();

                return Ok(new MessageDto { Message = "All supplier data deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ErrorDto { Error = ex.Message });
            }
        }

        [HttpGet("SupplierData")]
        public IActionResult SupplierData()
        {
            try
            {
                var result = new List<SupplierDataDto>();
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                using var cmd = new SqlCommand("SELECT * FROM dbo.suppliers_masters", conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new SupplierDataDto
                    {
                        Sup_id = reader.GetInt32(reader.GetOrdinal("sup_id")),
                        Sup_Name = reader.GetString(reader.GetOrdinal("sup_name")),
                        Name = reader.GetString(reader.GetOrdinal("contact_name")),
                        Phone = reader.GetString(reader.GetOrdinal("contact_phone")),
                        Email = reader.GetString(reader.GetOrdinal("contact_email")),
                        Address = reader.GetString(reader.GetOrdinal("address")),
                        City = reader.GetString(reader.GetOrdinal("city")),
                        State = reader.GetString(reader.GetOrdinal("state")),
                        Postal_code = reader.GetInt32(reader.GetOrdinal("postal_code")),
                        Country = reader.GetString(reader.GetOrdinal("country")),
                        Tax_id = reader.GetString(reader.GetOrdinal("tax_id")),
                        Error_msg = reader.IsDBNull(reader.GetOrdinal("error_msg")) ? "" : reader.GetString(reader.GetOrdinal("error_msg"))
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching supplier data");
                return StatusCode(500, new ErrorDto { Error = ex.Message });
            }
        }
    }

}
