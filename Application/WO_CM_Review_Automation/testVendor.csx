using System;
using System.Linq;
using System.Collections.Generic;

var woVendor = "Primetech BD, Naasbd";
var allowedVendors = new List<string> { "primetech bd" };

bool matched = woVendor.Split(new[] { ',', '&', '|' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(v => v.Trim().ToLower())
                       .Any(v => allowedVendors.Contains(v));

Console.WriteLine($"Matched: {matched}");
