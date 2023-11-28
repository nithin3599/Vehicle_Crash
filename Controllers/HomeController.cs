using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using vehiclecrash.Data;
using vehiclecrash.Models;

namespace vehiclecrash.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly static string Api = "https://data.cityofnewyork.us/resource/h9gi-nx95.json";

        private ApplicationDbContext context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            this.context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> viewdata()
        {
            var results = await context.CrashData
                            .Select(c => new CrashViewModel
                            {
                                CollisionID = c.CrashID,
                                CrashDate = c.CrashDate,
                                CrashTime = c.CrashTime,
                                StreetName = c.Address.StreetName,
                                ContributingFactor = c.ContributingFactor.ContributingFactorName,
                                NumberOfPersonsInjured = c.NumberOfPersonsInjured,
                                NumberOfPersonsKilled  = c.NumberOfPersonsKilled,
                                NumberOfPedestriansInjured = c.NumberOfPedestriansInjured,
                                NumberOfPedestriansKilled = c.NumberOfPedestriansKilled,
                                NumberOfCyclistInjured = c.NumberOfCyclistInjured,
                                NumberOfCyclistKilled = c.NumberOfCyclistKilled,
                                }).ToListAsync();

            // Pass the search results to the view
            return View(results);
        }

        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null || id == 0) return NotFound();
            var results = await context.CrashData
                            .Where(c => c.CrashID == id)
                            .Select(c => new CrashViewModel
                            {
                                CollisionID = c.CrashID,
                                CrashDate = c.CrashDate,
                                CrashTime = c.CrashTime,
                                StreetName = c.Address.StreetName,
                                ContributingFactor = c.ContributingFactor.ContributingFactorName,
                                NumberOfPersonsInjured = c.NumberOfPersonsInjured,
                                NumberOfPersonsKilled = c.NumberOfPersonsKilled,
                                NumberOfPedestriansInjured = c.NumberOfPedestriansInjured,
                                NumberOfPedestriansKilled = c.NumberOfPedestriansKilled,
                                NumberOfCyclistInjured = c.NumberOfCyclistInjured,
                                NumberOfCyclistKilled = c.NumberOfCyclistKilled,
                            }).FirstOrDefaultAsync();
            return View("ViewSingle", results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CrashViewModel p)
        {
            if (p == null || p.CollisionID == 0) return NotFound();
            var crashData = await context.CrashData
                           .Include(c => c.Address)
                           .Include(c => c.ContributingFactor)
                           .FirstOrDefaultAsync(c => c.CrashID == p.CollisionID);
            crashData.NumberOfPersonsInjured = p.NumberOfPersonsInjured;
            crashData.NumberOfPersonsKilled = p.NumberOfPersonsKilled;

            await context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Data Updated Successfully";
            return RedirectToAction("viewdata");
        }

        public ActionResult Delete(int? id)
        {
            Crash data = context.CrashData.Find(id);
            context.CrashData.Remove(data);
            context.SaveChanges();
            TempData["SuccessMessage"] = "Data Deleted Successfully";
            return RedirectToAction("viewdata");
        }

        public ActionResult Add() { 
            var states = context.AddressData.ToList();
            var contributions = context.ContributingFactorData.ToList();
            ViewBag.States = new SelectList(states, "StreetID","StreetName");
            ViewBag.Contributions = new SelectList(contributions, 
                "ContributingFactorID", "ContributingFactorName");
            return View(); 
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Add(Crash p)
        {
            if (p == null) return BadRequest();
            Crash f = p;
            context.CrashData.Add(f);
            context.SaveChanges();
            return RedirectToAction("viewdata");
        }

        public ActionResult graph() {
            var crashDataList = context.CrashData
                                .Select(c => new
                                {
                                    c.CrashDate,
                                    c.NumberOfPersonsInjured
                                })
                                .ToList();

            var combinedData = crashDataList.Select(c => new
            {
                CrashDate1 = c.CrashDate,
                PersonsInjured = c.NumberOfPersonsInjured
            }).ToList();

            ViewBag.CrashDataJson = JsonSerializer.Serialize(combinedData);
            return View(); 
        }

        public ActionResult about()
        {
            return View();
        }
        public async Task<IActionResult> LoadDataFromApi()
        {
            System.Diagnostics.Debug.WriteLine("LoadDataFromApi loading");
            using var transaction = context.Database.BeginTransaction();
            try
            {
                context.CrashData.RemoveRange(context.CrashData);
                context.ContributingFactorData.RemoveRange(context.ContributingFactorData);
                context.AddressData.RemoveRange(context.AddressData);
                await context.SaveChangesAsync();
                context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('AddressData', RESEED, 0);");
                context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('ContributingFactorData', RESEED, 0);");
                context.Database.ExecuteSqlRaw("DBCC CHECKIDENT ('CrashData', RESEED, 0);");
                var client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = await client.GetAsync(Api);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "API request error");
                }
                var jsonString = await response.Content.ReadAsStringAsync();
                var crashDataJson = JsonSerializer.Deserialize<List<dynamic>>(jsonString)?.ToList();
                
                string debugString = JsonSerializer.Serialize(crashDataJson, new JsonSerializerOptions { WriteIndented = true });

                System.Diagnostics.Debug.WriteLine(debugString);
                foreach (var dto in crashDataJson)
                {
                    JsonElement propertyElement;
                    //Contributing Factor Model
                    string contributingFactorName = "Unknown";
                    if (dto.TryGetProperty("contributing_factor_vehicle_1", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        contributingFactorName = propertyElement.GetString();
                    }
                    else if (dto.TryGetProperty("contributing_factor_vehicle_2", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        contributingFactorName = propertyElement.GetString();
                    }

                    /*var contributingFactor = context.ContributingFactorData
        .FirstOrDefault(cf => cf.ContributingFactorName == contributingFactorName)
        ?? new ContributingFactor { ContributingFactorName = contributingFactorName };
                    context.SaveChanges();*/
                    var contributingFactor = context.ContributingFactorData.FirstOrDefault(s 
                        => s.ContributingFactorName == contributingFactorName);
                    if (contributingFactor == null)
                    {
                        contributingFactor = new ContributingFactor { ContributingFactorName = contributingFactorName };
                        context.ContributingFactorData.Add(contributingFactor);
                        context.SaveChanges();
                    }

                    //Address Model
                    string address = "Unknown";
                    if (dto.TryGetProperty("on_street_name", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        address = propertyElement.GetString();
                    }
                    /*var address1 = context.AddressData.FirstOrDefault(cf => 
                    cf.StreetName== address) ?? new Address { StreetName = address};
                    context.SaveChanges();*/
                    var address1 = context.AddressData.FirstOrDefault(s
                        => s.StreetName == address);
                    if (address1== null)
                    {
                        address1 = new Address { StreetName = address};
                        context.AddressData.Add(address1);
                        context.SaveChanges();
                    }

                    //CrashDate Value for Date Time
                    //CrashTime Value for TimeSpan
                    DateTime crashDate = DateTime.MinValue;
                    TimeSpan crashTime = TimeSpan.Zero;
                    //Crash Data Model
                    var crashData = new Crash();

                    //Map CrashDate Column
                    if (dto.TryGetProperty("crash_date", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        crashDate = DateTime.Parse(propertyElement.GetString());
                    }

                    //Map crashTime Column
                    if (dto.TryGetProperty("crash_time", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        crashTime = TimeSpan.Parse(propertyElement.GetString());
                    }

                    //Map numberOfPersonsInjured
                    int numberOfPersonsInjured = 0;
                    if (dto.TryGetProperty("number_of_persons_injured", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        var numberOfPersonsInjuredString = propertyElement.GetString();
                        if (int.TryParse(numberOfPersonsInjuredString, out int parsedCrashID))
                        {
                            numberOfPersonsInjured = parsedCrashID;
                        }
                    }

                    //Map NumberOfPersonsKilled
                    int NumberOfPersonsKilled = 0;
                    if (dto.TryGetProperty("number_of_persons_killed", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        var NumberOfPersonsKilledString = propertyElement.GetString();
                        if (int.TryParse(NumberOfPersonsKilledString, out int parsedCrashID))
                        {
                            NumberOfPersonsKilled = parsedCrashID;
                        }
                    }

                    //Map NumberOfPedestriansInjured
                    int NumberOfPedestriansInjured = 0;
                    if (dto.TryGetProperty("number_of_pedestrians_injured", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        var NumberOfPedestriansInjuredString = propertyElement.GetString();
                        if (int.TryParse(NumberOfPedestriansInjuredString, out int parsedCrashID))
                        {
                            NumberOfPedestriansInjured = parsedCrashID;
                        }
                    }

                    //Map NumberOfPedestriansKilled
                    int NumberOfPedestriansKilled = 0;
                    if (dto.TryGetProperty("number_of_pedestrians_killed", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        var NumberOfPedestriansKilledString = propertyElement.GetString();
                        if (int.TryParse(NumberOfPedestriansKilledString, out int parsedCrashID))
                        {
                            NumberOfPedestriansKilled = parsedCrashID;
                        }
                    }

                    //Map NumberOfCyclistInjured
                    int NumberOfCyclistInjured = 0;
                    if (dto.TryGetProperty("number_of_cyclist_injured", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        var NumberOfCyclistInjuredString = propertyElement.GetString();
                        if (int.TryParse(NumberOfCyclistInjuredString, out int parsedCrashID))
                        {
                            NumberOfCyclistInjured = parsedCrashID;
                        }
                    }

                    //Map NumberOfCyclistKilled
                    int NumberOfCyclistKilled = 0;
                    if (dto.TryGetProperty("number_of_cyclist_killed", out propertyElement) && !string.IsNullOrEmpty(propertyElement.GetString()))
                    {
                        var NumberOfCyclistKilledString = propertyElement.GetString();
                        if (int.TryParse(NumberOfCyclistKilledString, out int parsedCrashID))
                        {
                            NumberOfCyclistKilled = parsedCrashID;
                        }
                    }

                    
                    // Map all the crashData Model Parameters with the JSON data.
                    crashData.CrashDate = crashDate;
                    crashData.CrashTime = crashTime;
                    crashData.ContributingFactorID = contributingFactor.ContributingFactorID;
                    crashData.StreetID = address1.StreetID;
                    crashData.NumberOfPersonsInjured = numberOfPersonsInjured;
                    crashData.NumberOfPersonsKilled = NumberOfPersonsKilled;
                    crashData.NumberOfPedestriansInjured = NumberOfPedestriansInjured;
                    crashData.NumberOfPedestriansKilled = NumberOfPedestriansKilled;
                    crashData.NumberOfCyclistInjured = NumberOfCyclistInjured;
                    crashData.NumberOfCyclistKilled = NumberOfCyclistKilled;
            
                    context.CrashData.Add(crashData);
                    context.SaveChanges();
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Data from API loaded successfully.";

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = ex.Message;
                //"An error occurred while loading data from the API. Please Retry..";
                _logger.LogError(ex, "An error occurred while loading data from the API. Please Retry..");
            }
            return RedirectToAction("Index");
        }

            public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}