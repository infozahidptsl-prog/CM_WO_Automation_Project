
using System;
using System.Globalization;

string date = "8-24-2026 9:43";
bool success = DateTime.TryParse(date, out var dt);
Console.WriteLine($"Default Culture TryParse: {success}, Date: {dt}");

success = DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out dt);
Console.WriteLine($"Invariant Culture TryParse: {success}, Date: {dt}");

