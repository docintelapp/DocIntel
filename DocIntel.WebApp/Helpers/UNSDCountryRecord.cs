// 

using CsvHelper.Configuration.Attributes;

namespace DocIntel.WebApp.Helpers
{
    public class UNSDCountryRecord
    {
        [Name("Global Code")] public string GlobalCode { get; set; }
        [Name("Global Name")] public string GlobalName { get; set; }
        [Name("Region Code")] public string RegionCode { get; set; }
        [Name("Region Name")] public string RegionName { get; set; }
        [Name("Sub-region Code")] public string SubregionCode { get; set; }
        [Name("Sub-region Name")] public string SubregionName { get; set; }
        [Name("Intermediate Region Code")] public string IntermediateRegionCode { get; set; }
        [Name("Intermediate Region Name")] public string IntermediateRegionName { get; set; }
        [Name("Country or Area")] public string CountryOrArea { get; set; }
        [Name("M49 Code")] public string M49Code { get; set; }
        [Name("ISO-alpha2 Code")] public string ISO2Code { get; set; }
        [Name("ISO-alpha3 Code")] public string ISO3Code { get; set; }
    }
}