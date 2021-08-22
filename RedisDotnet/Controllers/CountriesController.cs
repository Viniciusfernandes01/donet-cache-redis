using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using RedisDotnet.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace RedisDotnet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountriesController : ControllerBase
    {
        private readonly IDistributedCache _distributedCache;
        private const string CountriesKey = "Countries";

        public CountriesController(IDistributedCache distributedCache)
        {
            this._distributedCache = distributedCache;
        }

        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            var countriesObject = await _distributedCache.GetStringAsync(CountriesKey);

            if (!string.IsNullOrWhiteSpace(countriesObject))
            {
                return Ok(countriesObject);
            }
            else
            {
                const string restCountriesUrl = "https://restcountries.eu/rest/v2/all";

                using (var httpClient = new HttpClient())
                {
                    var response = await httpClient.GetAsync(restCountriesUrl);
                    var responseData = await response.Content.ReadAsStringAsync();
                    var countries = JsonConvert.DeserializeObject<List<Country>>(responseData);

                    var memoryCacheEntryOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3600),
                        SlidingExpiration = TimeSpan.FromSeconds(1200)
                    };

                    await _distributedCache.SetStringAsync(CountriesKey, responseData, memoryCacheEntryOptions);

                    return Ok(countries);

                }
            }
        }
    }
}
