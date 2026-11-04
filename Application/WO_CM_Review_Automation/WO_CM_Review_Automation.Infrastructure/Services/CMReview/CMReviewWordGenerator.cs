using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WO_CM_Review_Automation.Application.Interfaces;
using WO_CM_Review_Automation.Application.ViewModels.CMReview;
using System.Drawing;

namespace WO_CM_Review_Automation.Infrastructure.Services.CMReview;

public class CMReviewWordGenerator : ICMReviewWordGenerator
{
    // ── Font / size constants (OpenXML uses half-points) ─────────────────────
    private const string Font      = "Calibri";
    private const string SzBody    = "22"; // 11 pt
    private const string SzSection = "24"; // 12 pt
    private const string SzLarge   = "28"; // 14 pt
    private const string SzTitle   = "32"; // 16 pt
    private const string ColBlack  = "000000";
    private const string IndentOne = "720"; // ~0.5 inch

    // ─────────────────────────────────────────────────────────────────────────
    public Task<byte[]> GenerateWordDocumentAsync(CMReviewDocumentViewModel documentModel)
    {
        using var memoryStream = new MemoryStream();

        using (var wordDocument = WordprocessingDocument.Create(memoryStream, WordprocessingDocumentType.Document))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());

            var monthStr   = System.Globalization.DateTimeFormatInfo.InvariantInfo.GetMonthName(documentModel.ReportingMonth);
            var yearStr    = documentModel.ReportingYear.ToString();
            var periodStr1 = $"{monthStr}'{yearStr}";  // June'2026
            var periodStr2 = $"{monthStr}-{yearStr}";  // June-2026

            var hasAnyChanges = documentModel.ChangeCategories.Any(c => c.HasChanges);

            // ── Set document default styles ──────────────────────────────────
            AddDefaultStyles(mainPart);

            // ── Build repeating header + footer ──────────────────────────────
            var (headerPartId, footerPartId) = AddHeaderFooter(mainPart);

            // ════════════════════════════════════════════════════════════════
            // TITLE BLOCK
            // ════════════════════════════════════════════════════════════════

            // "Process for Change Management Review:" – bold 16 pt
            body.AppendChild(MakePara("Process for Change Management Review:", bold: true, fontSize: SzTitle, spaceBefore: 160, spaceAfter: 0));

            // "Registered Change vs. Implemented Change" – italic 16 pt
            body.AppendChild(MakePara("Registered Change vs. Implemented Change", bold: false, fontSize: SzTitle, italic: true, spaceAfter: 240));

            // Objective paragraph – 14 pt justified
            var objPara = new Paragraph();
            objPara.AppendChild(SpacedProps(spaceAfter: 120, justified: true));
            objPara.AppendChild(Run("The objective of this artifact is to enable verification between ", SzLarge));
            objPara.AppendChild(Run("Available Change Log", SzLarge, underline: true));
            objPara.AppendChild(Run(" and ", SzLarge));
            objPara.AppendChild(Run("Registered Changes", SzLarge, underline: true));
            objPara.AppendChild(Run(" and to certify or identify presence of unauthorized changes.", SzLarge));
            body.AppendChild(objPara);
            body.AppendChild(Blank());

            // "Applicable System: POS" – bold 14 pt, app name underlined
            var appPara = new Paragraph();
            appPara.AppendChild(SpacedProps(spaceAfter: 120));
            appPara.AppendChild(Run("Applicable System: ", SzLarge, bold: true));
            appPara.AppendChild(Run(documentModel.Application, SzLarge, bold: true, underline: true));
            body.AppendChild(appPara);
            body.AppendChild(Blank());

            // "Period: June'2026" – bold 14 pt, date underlined
            var periodPara = new Paragraph();
            periodPara.AppendChild(SpacedProps(spaceAfter: 120));
            periodPara.AppendChild(Run("Period: ", SzLarge, bold: true));
            periodPara.AppendChild(Run(periodStr1, SzLarge, bold: true, underline: true));
            body.AppendChild(periodPara);
            body.AppendChild(Blank());

            // ════════════════════════════════════════════════════════════════
            // SECTION A – Available Change Log / Latest Screenshot
            // ════════════════════════════════════════════════════════════════
            body.AppendChild(MakePara($"(A)  Available Change Log at {documentModel.Application} Application as per Change Management Scope",
                bold: true, fontSize: SzSection, spaceAfter: 60));
            body.AppendChild(MakePara("Latest Screenshot", bold: true, fontSize: SzSection, spaceAfter: 120));

            // Descriptive sentence
            if (!hasAnyChanges)
            {
                body.AppendChild(MakePara(
                    $"As per the screenshot below, there was no change made in the {documentModel.Application} system for the month of {periodStr2}.",
                    bold: false, fontSize: SzBody));
            }
            else
            {
                var allCats = documentModel.ChangeCategories
                    .Where(c => c.HasChanges && c.ChangeDetails.Any())
                    .OrderByDescending(c => c.CategoryName.StartsWith("Implementation of Change Request", StringComparison.OrdinalIgnoreCase) || 
                                            c.CategoryName.StartsWith("Business Logic", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(c => c.DisplayOrder)
                    .ToList();
                if (allCats.Any())
                {
                    var parts = allCats.Select(cat =>
                    {
                        var count    = cat.ChangeDetails.Count;
                        var countWrd = ToWord(count);
                        var orders   = string.Join(", ", cat.ChangeDetails.Select(d => d.ChangeOrder));
                        var keyword  = GetCategoryKeyword(cat, documentModel);
                        return count == 1
                            ? $"{countWrd} {keyword} ({orders}) change"
                            : $"{countWrd} {keyword} changes ({orders})";
                    }).ToList();

                    var summary    = JoinWithAnd(parts);
                    var totalCount = allCats.Sum(c => c.ChangeDetails.Count);
                    var wereOrWas  = totalCount == 1 ? "was" : "were";

                    body.AppendChild(MakePara(
                        $"As per the screenshot below, {summary} {wereOrWas} made to the {documentModel.Application} system for the month of {periodStr2}.",
                        bold: false, fontSize: SzBody));
                }
                else
                {
                    body.AppendChild(MakePara(
                        $"As per the screenshot below, there were changes made in the {documentModel.Application} system for the month of {periodStr2}.",
                        bold: false, fontSize: SzBody));
                }
            }
            body.AppendChild(Blank());

            // Server evidence screenshots
            if (documentModel.Application == "Sales Force App")
            {
                var allShots = documentModel.ServerEvidences
                    .SelectMany(s => s.Screenshots)
                    .OrderBy(s => 
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(s.FileName ?? "", @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?!\d)");
                        return match.Success && Version.TryParse(match.Value, out var v) ? v : new Version(999,999,999,999);
                    })
                    .ThenBy(s => s.FileName)
                    .ToList();
                
                foreach (var shot in allShots)
                {
                    var fileNameNoExt = !string.IsNullOrEmpty(shot.FileName) ? System.IO.Path.GetFileNameWithoutExtension(shot.FileName) : "";
                    var displayTitle = "";
                    if (!string.IsNullOrWhiteSpace(fileNameNoExt))
                    {
                        var hasIp = System.Text.RegularExpressions.Regex.IsMatch(fileNameNoExt, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?!\d)");
                        if (hasIp)
                            displayTitle = $"Server: {fileNameNoExt}";
                        else
                        {
                            var parentServer = documentModel.ServerEvidences.FirstOrDefault(se => se.Screenshots.Contains(shot));
                            displayTitle = $"Server: {parentServer?.ServerIp ?? ""} {fileNameNoExt}".Trim();
                        }
                    }
                    else
                    {
                        var parentServer = documentModel.ServerEvidences.FirstOrDefault(se => se.Screenshots.Contains(shot));
                        if (!string.IsNullOrWhiteSpace(parentServer?.ServerIp)) displayTitle = $"Server: {parentServer.ServerIp}";
                    }

                    if (!string.IsNullOrWhiteSpace(displayTitle))
                        body.AppendChild(IndentedBoldPara(IndentOne, displayTitle));

                    EmbedScreenshot(mainPart, body, shot);
                    body.AppendChild(Blank());
                }

                foreach (var server in documentModel.ServerEvidences.OrderBy(s => s.DisplayOrder))
                {
                    if (!string.IsNullOrWhiteSpace(server.Remarks))
                        body.AppendChild(MakePara($"*Note: {server.Remarks}", bold: false, fontSize: SzBody, italic: true));
                }
            }
            else
            {
                foreach (var server in documentModel.ServerEvidences.OrderBy(s => s.DisplayOrder))
                {
                    if (!server.Screenshots.Any()) continue;

                    // "Server: 172.19.1.5" then server name on next line (only when populated)
                    if (!string.IsNullOrWhiteSpace(server.ServerIp))
                        body.AppendChild(IndentedBoldPara(IndentOne, $"Server: {server.ServerIp}"));
                    if (!string.IsNullOrWhiteSpace(server.ServerName))
                        body.AppendChild(IndentedBoldPara(IndentOne, server.ServerName));

                    foreach (var shot in server.Screenshots.OrderBy(s => s.DisplayOrder))
                    {
                        EmbedScreenshot(mainPart, body, shot);
                        if (!string.IsNullOrWhiteSpace(shot.Caption))
                            body.AppendChild(MakePara(shot.Caption, bold: false, fontSize: SzBody));
                        body.AppendChild(Blank());
                    }
                    
                    if (!string.IsNullOrWhiteSpace(server.Remarks))
                        body.AppendChild(MakePara($"*Note: {server.Remarks}", bold: false, fontSize: SzBody, italic: true));
                }
            }

            // ════════════════════════════════════════════════════════════════
            // SECTION B – Change Categories (numbered 1, 2, 3 …)
            // ════════════════════════════════════════════════════════════════
            int sectionNum = 1;
            foreach (var cat in documentModel.ChangeCategories)
            {
                // "N.   Category Name" – bold 12 pt
                body.AppendChild(NumberedHeading(sectionNum, cat.CategoryName, SzSection));

                if (cat.HasChanges && cat.ChangeDetails.Any())
                {
                    var count     = cat.ChangeDetails.Count;
                    var countWrd  = ToWord(count);
                    var isPlural  = count > 1;
                    
                    var catKeyword = GetCategoryKeyword(cat, documentModel);
                    var changeModifier = "";
                    if (!cat.CategoryName.Equals("Standard Change", StringComparison.OrdinalIgnoreCase) &&
                        (catKeyword.Equals("Normal-minor", StringComparison.OrdinalIgnoreCase) || 
                         catKeyword.Equals("Normal-major", StringComparison.OrdinalIgnoreCase)))
                    {
                        changeModifier = $"{catKeyword} ";
                    }
                                          
                    var changeWrd = isPlural ? $"{changeModifier}changes were" : $"{changeModifier}change was";
                    var numWrd    = isPlural ? "numbers are" : "number is";

                    // Build "(CR 96451, CR 96269 and CR 95883)" style list
                    var orderList = cat.ChangeDetails.Select(d => d.ChangeOrder).ToList();
                    var coText    = "(" + JoinWithAnd(orderList) + ")";

                    body.AppendChild(IndentedPara(IndentOne,
                        $"{countWrd} {changeWrd} made under this change category. Change order {numWrd}:",
                        bold: false, fontSize: SzBody));
                    body.AppendChild(IndentedPara(IndentOne, coText, bold: false, fontSize: SzBody));
                    body.AppendChild(Blank());

                    foreach (var detail in cat.ChangeDetails)
                    {
                        var coNum   = CleanCoNumber(detail.ChangeOrder);
                        var dateStr = detail.ScheduleDate.HasValue
                            ? detail.ScheduleDate.Value.ToString("dd-MM-yyyy hh:mm:ss tt")
                            : "";

                        body.AppendChild(LabelValuePara(IndentOne, "Change Order: ", coNum));
                        body.AppendChild(LabelValuePara(IndentOne, "Change Description: ", detail.Description ?? ""));
                        body.AppendChild(LabelValuePara(IndentOne, "Change schedule Date: ", dateStr));

                        // Deployment screenshots
                        var masterGroups = detail.DeploymentScreenshots
                            .OrderBy(s => s.DisplayOrder)
                            .GroupBy(s => s.Heading ?? "After Deployments:")
                            .ToList();

                        foreach (var grp in masterGroups)
                        {
                            body.AppendChild(Blank());
                            body.AppendChild(IndentedBoldPara(IndentOne, grp.Key));

                            var serverGroups = grp.GroupBy(s => (Ip: s.ServerIp ?? "", Name: s.ServerName ?? ""));
                            foreach (var sg in serverGroups)
                            {
                                if (documentModel.Application == "Sales Force App")
                                {
                                    foreach (var shot in sg)
                                    {
                                        var fileNameNoExt = !string.IsNullOrEmpty(shot.FileName) ? System.IO.Path.GetFileNameWithoutExtension(shot.FileName) : "";
                                        var displayTitle = "";
                                        if (!string.IsNullOrWhiteSpace(fileNameNoExt))
                                        {
                                            var hasIp = System.Text.RegularExpressions.Regex.IsMatch(fileNameNoExt, @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}(?!\d)");
                                            if (hasIp)
                                                displayTitle = $"Server: {fileNameNoExt}";
                                            else
                                                displayTitle = $"Server: {sg.Key.Ip} {fileNameNoExt}".Trim();
                                        }
                                        else if (!string.IsNullOrWhiteSpace(sg.Key.Ip))
                                        {
                                            displayTitle = $"Server: {sg.Key.Ip}";
                                        }

                                        if (!string.IsNullOrWhiteSpace(displayTitle))
                                            body.AppendChild(IndentedBoldPara(IndentOne, displayTitle));

                                        EmbedScreenshot(mainPart, body, shot);
                                        body.AppendChild(Blank());
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrWhiteSpace(sg.Key.Ip))
                                    {
                                        body.AppendChild(IndentedBoldPara(IndentOne, $"Server: {sg.Key.Ip}"));
                                        if (!string.IsNullOrWhiteSpace(sg.Key.Name))
                                            body.AppendChild(IndentedBoldPara(IndentOne, sg.Key.Name + ":"));
                                    }
                                    foreach (var shot in sg)
                                    {
                                        EmbedScreenshot(mainPart, body, shot);
                                        body.AppendChild(Blank());
                                    }
                                }
                            }
                        }
                        body.AppendChild(Blank());
                    }
                }
                else
                {
                    body.AppendChild(IndentedPara(IndentOne, "No change.", bold: false, fontSize: SzBody));
                }
                sectionNum++;
            }

            // ════════════════════════════════════════════════════════════════
            // UAT  (dynamic number continues from change categories)
            // ════════════════════════════════════════════════════════════════
            body.AppendChild(NumberedHeading(sectionNum, "UAT", SzSection));

            string uatText;
            if (!string.IsNullOrWhiteSpace(documentModel.UATStatement))
            {
                uatText = documentModel.UATStatement;
            }
            else
            {
                var hasSpecificNow = documentModel.ChangeCategories.Any(c => c.HasChanges &&
                    (c.CategoryName.Contains("Standard",      StringComparison.OrdinalIgnoreCase) ||
                     c.CategoryName.Contains("Change Request",StringComparison.OrdinalIgnoreCase) ||
                     c.CategoryName.Contains("Normal",        StringComparison.OrdinalIgnoreCase)));

                uatText = hasSpecificNow
                    ? $"UAT was conducted of the above changes as per Banglalink change management policy for the month of {monthStr}-{documentModel.ReportingYear}."
                    : !hasAnyChanges
                        ? $"There was No UAT conducted in {documentModel.Application} for the month of {monthStr}-{documentModel.ReportingYear}, as there was No change."
                        : $"There was No UAT conducted in {documentModel.Application} for the month of {monthStr}-{documentModel.ReportingYear}.";
            }
            body.AppendChild(IndentedPara(IndentOne, uatText, bold: false, fontSize: SzBody));
            body.AppendChild(Blank());
            sectionNum++;

            // ════════════════════════════════════════════════════════════════
            // REGISTERED CHANGES / SUMMARY  (dynamic number)
            // ════════════════════════════════════════════════════════════════
            var hasSpecificChanges = documentModel.ChangeCategories.Any(c => c.HasChanges &&
                (c.CategoryName.Contains("Standard",      StringComparison.OrdinalIgnoreCase) ||
                 c.CategoryName.Contains("Change Request",StringComparison.OrdinalIgnoreCase) ||
                 c.CategoryName.Contains("Normal",        StringComparison.OrdinalIgnoreCase) ||
                 documentModel.Application == "Techvan"));

            var hasFigure2 = documentModel.ExcelScreenshot != null &&
                             (!string.IsNullOrWhiteSpace(documentModel.ExcelScreenshot.Base64Data) ||
                              !string.IsNullOrWhiteSpace(documentModel.ExcelScreenshot.StorageReference));

            // "N. Registered Changes, Change Management Report, June -2026." – 14 pt bold
            body.AppendChild(MakePara(
                $"{sectionNum}. Registered Changes, Change Management Report, {monthStr} -{yearStr}.",
                bold: true, fontSize: SzLarge, spaceBefore: 120, spaceAfter: 60));

            // "IT Change Orders Summary & Details, June-2026" – 12 pt bold
            body.AppendChild(MakePara(
                $"IT Change Orders Summary & Details, {monthStr}-{yearStr}",
                bold: true, fontSize: SzSection, spaceAfter: 120));

            if (!hasAnyChanges)
            {
                body.AppendChild(MakePara(
                    $"As per the Change Order Summary Dump (Figure 1), no change was made in the month of {periodStr2} in {documentModel.Application}.",
                    bold: false, fontSize: SzBody));
            }
            else
            {
                var totalCount  = documentModel.ChangeCategories.Where(c => c.HasChanges).SelectMany(c => c.ChangeDetails).Count();
                if (totalCount == 0) totalCount = documentModel.WOReportItems.Count;
                var countWrd    = ToWord(totalCount);
                var changePlural = totalCount == 1 ? "change was" : "changes were";
                var figureRef   = hasSpecificChanges && hasFigure2
                    ? "(figure 1) & (figure 2)"
                    : "(figure 1)";

                body.AppendChild(MakePara(
                    $"As per the screenshot below {figureRef}, {countWrd} {changePlural} made in {documentModel.Application} system for the month of {periodStr2}.",
                    bold: false, fontSize: SzBody));
            }
            body.AppendChild(Blank());

            // Figure 1 – Summary Table screenshot
            if (documentModel.SummaryTableScreenshot != null &&
                (!string.IsNullOrWhiteSpace(documentModel.SummaryTableScreenshot.Base64Data) ||
                 !string.IsNullOrWhiteSpace(documentModel.SummaryTableScreenshot.StorageReference)))
            {
                EmbedScreenshot(mainPart, body, documentModel.SummaryTableScreenshot);
                body.AppendChild(MakePara("Figure-1", bold: true, fontSize: SzBody, center: true, underline: true));
                body.AppendChild(Blank());
            }

            // Figure 2 – Excel dump screenshot
            if (hasSpecificChanges && hasFigure2)
            {
                EmbedScreenshot(mainPart, body, documentModel.ExcelScreenshot!);
                body.AppendChild(MakePara("Figure-2", bold: true, fontSize: SzBody, center: true, underline: true));
                body.AppendChild(Blank());
            }

            // SIEM attachment (POS only)
            if (documentModel.Application == "POS" &&
                documentModel.SiemScreenshot != null &&
                (!string.IsNullOrWhiteSpace(documentModel.SiemScreenshot.Base64Data) ||
                 !string.IsNullOrWhiteSpace(documentModel.SiemScreenshot.StorageReference)))
            {
                body.AppendChild(MakePara(
                    $"Mentioned attached SIEM is a standard change. It should not be part of the CM review for {monthStr}-{documentModel.ReportingYear}.",
                    bold: true, fontSize: SzBody));

                if (!string.IsNullOrEmpty(documentModel.SiemScreenshot.FileName) &&
                   (documentModel.SiemScreenshot.FileName.EndsWith(".xlsb", StringComparison.OrdinalIgnoreCase) ||
                    documentModel.SiemScreenshot.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                    documentModel.SiemScreenshot.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)))
                {
                    var excelIconBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAABACAYAAACqaXHeAAAABHNCSVQICAgIfAhkiAAAAAlwSFlzAAABuwAAAbsBOuzj4gAAABl0RVh0U29mdHdhcmUAd3d3Lmlua3NjYXBlLm9yZ5vuPBoAAAWBSURBVHic7Zt7TBxFHMe/c8cVCgLHo+FaAn0oB/LqgoEKlvBHNUXtH1WJWqy8pFablmgpjVaxQLUkfUHpP41AKI0aoQ2miSb1TZFqAj4CpCjX9EHsi5ITuCq0BXb8gxZuj+W6u9zuHhyf5JK7387OfOeb2dnfTG4IpRSujEZtAWrjpnSDYflh7l56fSRLWIZQyoAQBhQRADopaFFH6e+/KKlHVgOiS5L9taN3GEIoQylhQMB46n0iKFgdoQBAgMkncA0hxB1AipyabHGIAYQQElOcsFwzOsawGsoQShgAjBYIGe8jAYiAiihWO0KPGIjYSTAsP8zdM8A3ioyxDEs048MYiAXg6whB7aW/CbHKYdgdAdMNYYxRHQUBmQNvEDfAgUN4FkJiiuJSCUgNgIfVFgMo/whoCCWVcJLOq4EbCGLVFmGNecCi1MTSqqGkwJUzwUSW0IOubAAAJLq6AfOLoXkD1BagNi5vgOL7AQ9iXe2zvHHjIiPyEvIQGRTFiXf1nkN1WzVMfSZB5W2ZNSPA1GdCdVv1lDhf5+2Vt0V2A+KXxuGTvDpUZR1FTHC03M2JRlYDjAYjjmRUIGJxOOJCGex8eof0uu4NaVvyEvJgXGQUXN4WwXMAE7ISbz2VD51Wh4pvK9F2+Ve75b09vLE/vQzubu4Tsdsjtx/Yzpc5XwmVBACIDIrCoXXlou6xRvAIKEwrQHRwFMINRhzOKEd8aNy0ZQkIStfvRrBfMCde33ZCslC5EGzAyNjIxPcFWh0OvLQPywOX8ZbNTclGShh3e6++7QS+6/pemkoZEWzAsbPHQa22cH08vFGZUYGAhwI45R5fsQqbUzdxYh1XOlH+zeEZSpUHElsUL3j9nZm0EflPbuXE/rrejdePv4Ghu8Mw+Brw6aY6+C6c3B/tHxrAK1WZuGm5KagNTz9P3rhceYA2KHVxsSBlANqvdMDPyw9RSyInYoHegQg3hKOpuxmVGw4hxD9k4hpLWexo2AlT73mhTUC3UMcbNw+Z0TPQg7XGNE68rKmMNw+Yrrwtol+D+08fRLOphRNLfiQJJ7d8jkgrYwDgaNPHaL3UJrYJRRFtAEtZ7Gp8H13XujjxIJ8gzu+fzregtqVuZuqskCsPEDUHWOPv5YdjuTVYol8y5dq1gWvYWJUFy+1bouv9YfuPUuRIRnIm+M9//djVWMR7rfZsnaTOq4FkAwgIXkvJ4b22bc1WLAtcKlmUkkg2IGd11pRk5z4+Ht6o3FABfy9/ycKUQtIcsGpFIo5kVEBDJv0z/2uekhR1Xf8Tm+u2YHhkWHDdSucBokeAwdeAj54r5XR+YGgAmTW5aLDJ9SMXP4q9L+zhlJWKU+wHLNDqsC99L/Se+onY+GuxCL2WXhz4uhxnups596SErZ7RMlhuRBmwI2273WSHpSze++IDnLPJEdIfex6Zya/OSKjqecAzMWkoXV/MiTWbWlBQX8hZJAH8OQIFRWHDO2jqPmO3HafNA15OfJHz+2r/Vew+VTKl88B4jrDts7dhGbZMxAgIsp/InIFUeRBsgPXOzp3ROyg8+S5u2Ul2esw9KGjYibtW+wg6Lf9CR00ErwYv9F1EdHAULpkvo+TUninPOR83Bm+g/e8OhAaEYnBoEPtPH8QNS6/de7KSsoXIcRiS1wJy4fR5gFo4RR4wF5k1BqieByiF0+YBc5V5A9QWoDbzBqgtQG1c3YBWVzagVUNJgRsoOpzp/8IBeh9l/y1OCc0HcEHJRp0JQimd9sAEgJAHVeBoVDkyQ8cPDl2892m8f5HvyAyACADOt7MhEZc/NCXaAN5KHPgIOdWpMaE47BEiaOGNy4hDRoAYpjk6uxIUPSxl3+z88I+fldSjuAHOhitnggCA/wF6hTqVixI1cwAAAABJRU5ErkJggg==";
                    var iconVm = new ScreenshotViewModel { Base64Data = excelIconBase64, ContentType = "image/png", Id = Guid.NewGuid() };
                    
                    var containerPara = new Paragraph();
                    containerPara.AppendChild(new ParagraphProperties(new Justification() { Val = JustificationValues.Left }));
                    
                    body.AppendChild(containerPara);
                    
                    // Call EmbedScreenshot on the body
                    EmbedScreenshot(mainPart, body, iconVm);
                    
                    body.AppendChild(MakePara(documentModel.SiemScreenshot.FileName, bold: false, fontSize: SzBody, center: false));
                }
                else
                {
                    EmbedScreenshot(mainPart, body, documentModel.SiemScreenshot);
                }
                body.AppendChild(Blank());
            }

            // ════════════════════════════════════════════════════════════════
            // COMPARISON SUMMARY
            // ════════════════════════════════════════════════════════════════
            if (!string.IsNullOrWhiteSpace(documentModel.ComparisonSummary))
            {
                foreach (var line in documentModel.ComparisonSummary.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                    body.AppendChild(MakePara(line, bold: false, fontSize: SzBody));
            }
            else
            {
                var totalCount   = documentModel.ChangeCategories.Where(c => c.HasChanges).SelectMany(c => c.ChangeDetails).Count();
                if (totalCount == 0) totalCount = documentModel.WOReportItems.Count;
                var countWrd     = ToWord(totalCount);
                var changePlural = totalCount == 1 ? "change was" : "changes were";

                var csumPara = new Paragraph();
                csumPara.AppendChild(SpacedProps(spaceAfter: 200));
                if (hasSpecificChanges)
                    csumPara.AppendChild(Run($"{countWrd} {changePlural} made to the {documentModel.Application} System for the month of {monthStr}-{documentModel.ReportingYear}. ", SzBody));
                else
                    csumPara.AppendChild(Run($"No change was made to the {documentModel.Application} System for the month of {monthStr}-{documentModel.ReportingYear}. ", SzBody));
                var unauthChanges = totalCount == 1 ? "change was" : "changes were";
                csumPara.AppendChild(Run($"No unauthorized {unauthChanges} made.", SzBody, bold: true));
                body.AppendChild(csumPara);
            }
            body.AppendChild(Blank());
            body.AppendChild(Blank());

            // ════════════════════════════════════════════════════════════════
            // VERIFIER / APPROVAL
            // ════════════════════════════════════════════════════════════════
            body.AppendChild(MakePara("Verifier/Approval", bold: true, fontSize: SzLarge));
            body.AppendChild(Blank());
            body.AppendChild(MakePara("------------------------------------", bold: false, fontSize: SzBody));
            if (!string.IsNullOrWhiteSpace(documentModel.VerifierName))
                body.AppendChild(MakePara(documentModel.VerifierName,        bold: true,  fontSize: SzBody));
            if (!string.IsNullOrWhiteSpace(documentModel.VerifierDesignation))
                body.AppendChild(MakePara(documentModel.VerifierDesignation, bold: true,  fontSize: SzBody));
            if (!string.IsNullOrWhiteSpace(documentModel.VerifierDepartment))
                body.AppendChild(MakePara(documentModel.VerifierDepartment,  bold: false, fontSize: SzBody));
            if (!string.IsNullOrWhiteSpace(documentModel.VerifierOrganization))
                body.AppendChild(MakePara(documentModel.VerifierOrganization,bold: false, fontSize: SzBody));

            // ════════════════════════════════════════════════════════════════
            // PAGE SETUP (A4)
            // ════════════════════════════════════════════════════════════════
            var sectPr = new SectionProperties();
            sectPr.AppendChild(new PageSize { Width = 11906U, Height = 16838U });
            sectPr.AppendChild(new PageMargin
            {
                Top    = 720,   // 0.5"
                Bottom = 720,
                Left   = 1080,  // 0.75"
                Right  = 1080,
                Header = 360,
                Footer = 360,
                Gutter = 0
            });

            if (headerPartId != null)
                sectPr.AppendChild(new HeaderReference { Type = HeaderFooterValues.Default, Id = headerPartId });
            if (footerPartId != null)
                sectPr.AppendChild(new FooterReference { Type = HeaderFooterValues.Default, Id = footerPartId });

            body.AppendChild(sectPr);
            mainPart.Document.Save();
        }

        return Task.FromResult(memoryStream.ToArray());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Header / Footer (repeating every page)
    // ─────────────────────────────────────────────────────────────────────────
    private (string? headerPartId, string? footerPartId) AddHeaderFooter(MainDocumentPart mainPart)
    {
        // ── HEADER ───────────────────────────────────────────────────────────
        var headerPart = mainPart.AddNewPart<HeaderPart>();
        var hdr = new Header();

        // 2-col table: [text | logo]
        var hTable = new Table();
        hTable.AppendChild(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableLayout { Type = TableLayoutValues.Autofit },
            new TableBorders(
                new TopBorder    { Val = BorderValues.None },
                new BottomBorder { Val = BorderValues.Single, Size = 6, Color = "000000" },
                new LeftBorder   { Val = BorderValues.None },
                new RightBorder  { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder   { Val = BorderValues.None }
            ),
            new TableIndentation { Width = 0, Type = TableWidthUnitValues.Dxa }
        ));

        var hRow = new TableRow();

        // Left: "T15.02.b.1: Change Request Initiation & Control"
        var hcLeft = new TableCell();
        hcLeft.AppendChild(new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "4000" },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Bottom }
        ));
        var hTextPara = new Paragraph();
        hTextPara.AppendChild(SpacedProps(spaceAfter: 0));
        hTextPara.AppendChild(Run("T15.02.b.1: Change Request Initiation & Control", SzBody, bold: true));
        hcLeft.AppendChild(hTextPara);
        hRow.AppendChild(hcLeft);

        // Right: logo
        var hcRight = new TableCell();
        hcRight.AppendChild(new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1000" },
            new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Bottom }
        ));
        var logoPara = new Paragraph();
        logoPara.AppendChild(SpacedProps(spaceAfter: 0, justified: false, right: true));

        var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logo.png");
        if (!File.Exists(logoPath))
            logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "favicon.png");
        if (File.Exists(logoPath))
        {
            var drawing = CreateHeaderImageDrawing(headerPart, logoPath);
            if (drawing != null) logoPara.AppendChild(new Run(drawing));
        }
        hcRight.AppendChild(logoPara);
        hRow.AppendChild(hcRight);
        hTable.AppendChild(hRow);

        hdr.AppendChild(hTable);
        headerPart.Header = hdr;
        hdr.Save();
        var headerPartId = mainPart.GetIdOfPart(headerPart);

        // ── FOOTER ───────────────────────────────────────────────────────────
        var footerPart = mainPart.AddNewPart<FooterPart>();
        var ftr = new Footer();

        // 3-col table: [blank | "Internal" blue center | "N | P a g e" right]
        var fTable = new Table();
        fTable.AppendChild(new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableLayout { Type = TableLayoutValues.Autofit },
            new TableBorders(
                new TopBorder { Val = BorderValues.None }, new BottomBorder { Val = BorderValues.None },
                new LeftBorder { Val = BorderValues.None }, new RightBorder { Val = BorderValues.None },
                new InsideHorizontalBorder { Val = BorderValues.None },
                new InsideVerticalBorder   { Val = BorderValues.None }
            )
        ));

        var fRow = new TableRow();

        // Left blank
        var fcLeft = new TableCell(new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1666" }));
        fcLeft.AppendChild(new Paragraph(SpacedProps(spaceAfter: 0)));
        fRow.AppendChild(fcLeft);

        // Center: "Internal" in blue
        var fcCenter = new TableCell(new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1666" }));
        var internalPara = new Paragraph();
        internalPara.AppendChild(SpacedProps(spaceAfter: 0, centered: true));
        var ir = new Run();
        ir.AppendChild(new RunProperties(
            new RunFonts { Ascii = Font, HighAnsi = Font },
            new FontSize { Val = SzBody },
            new DocumentFormat.OpenXml.Wordprocessing.Color { Val = "0000FF" }));
        ir.AppendChild(new Text("Internal"));
        internalPara.AppendChild(ir);
        fcCenter.AppendChild(internalPara);
        fRow.AppendChild(fcCenter);

        // Right: page number "N | P a g e"
        var fcRight = new TableCell(new TableCellProperties(
            new TableCellWidth { Type = TableWidthUnitValues.Pct, Width = "1668" }));
        var pagePara = new Paragraph();
        pagePara.AppendChild(SpacedProps(spaceAfter: 0, right: true));

        // PAGE field begin
        var r1 = new Run(new RunProperties(new RunFonts { Ascii = Font, HighAnsi = Font }, new FontSize { Val = SzBody }),
            new FieldChar { FieldCharType = FieldCharValues.Begin });
        // PAGE instruction
        var r2 = new Run(new RunProperties(new RunFonts { Ascii = Font, HighAnsi = Font }, new FontSize { Val = SzBody }),
            new FieldCode(" PAGE ") { Space = SpaceProcessingModeValues.Preserve });
        // PAGE field end
        var r3 = new Run(new RunProperties(new RunFonts { Ascii = Font, HighAnsi = Font }, new FontSize { Val = SzBody }),
            new FieldChar { FieldCharType = FieldCharValues.End });
        // " | P a g e"
        var r4 = new Run(new RunProperties(new RunFonts { Ascii = Font, HighAnsi = Font }, new FontSize { Val = SzBody }),
            new Text(" | P a g e") { Space = SpaceProcessingModeValues.Preserve });

        pagePara.AppendChild(r1);
        pagePara.AppendChild(r2);
        pagePara.AppendChild(r3);
        pagePara.AppendChild(r4);
        fcRight.AppendChild(pagePara);
        fRow.AppendChild(fcRight);

        fTable.AppendChild(fRow);
        ftr.AppendChild(fTable);
        footerPart.Footer = ftr;
        ftr.Save();
        var footerPartId = mainPart.GetIdOfPart(footerPart);

        return (headerPartId, footerPartId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Default document styles (Calibri 11pt)
    // ─────────────────────────────────────────────────────────────────────────
    private void AddDefaultStyles(MainDocumentPart mainPart)
    {
        var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
        var styles = new Styles();
        var docDefaults = new DocDefaults();
        var rpDefault = new RunPropertiesDefault(
            new RunPropertiesBaseStyle(
                new RunFonts { Ascii = Font, HighAnsi = Font, ComplexScript = Font },
                new FontSize { Val = SzBody },
                new FontSizeComplexScript { Val = SzBody }
            )
        );
        docDefaults.AppendChild(rpDefault);
        styles.AppendChild(docDefaults);
        stylesPart.Styles = styles;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Paragraph builders
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Simple single-run paragraph.</summary>
    private Paragraph MakePara(string text, bool bold, string fontSize,
        bool italic = false, bool underline = false, bool center = false,
        string color = ColBlack, int spaceAfter = 120, int spaceBefore = 0)
    {
        var p = new Paragraph();
        var pp = new ParagraphProperties();
        var sbl = new SpacingBetweenLines { After = spaceAfter.ToString() };
        if (spaceBefore > 0) sbl.Before = spaceBefore.ToString();
        pp.AppendChild(sbl);
        if (center) pp.AppendChild(new Justification { Val = JustificationValues.Center });
        p.AppendChild(pp);
        p.AppendChild(Run(text, fontSize, bold, italic, underline, color));
        return p;
    }

    /// <summary>Indented paragraph, normal weight.</summary>
    private Paragraph IndentedPara(string indent, string text, bool bold, string fontSize,
        bool italic = false, string color = ColBlack)
    {
        var p = new Paragraph();
        var pp = new ParagraphProperties(new SpacingBetweenLines { After = "80" });
        pp.AppendChild(new Indentation { Left = indent });
        p.AppendChild(pp);
        p.AppendChild(Run(text, fontSize, bold, italic, false, color));
        return p;
    }

    /// <summary>Indented bold paragraph (server headers, deployment headings).</summary>
    private Paragraph IndentedBoldPara(string indent, string text)
    {
        var p = new Paragraph();
        var pp = new ParagraphProperties(new SpacingBetweenLines { After = "60" });
        pp.AppendChild(new Indentation { Left = indent });
        p.AppendChild(pp);
        p.AppendChild(Run(text, SzBody, bold: true));
        return p;
    }

    /// <summary>"N.   Category Name" – bold section heading.</summary>
    private Paragraph NumberedHeading(int num, string name, string fontSize)
    {
        var p = new Paragraph();
        p.AppendChild(new ParagraphProperties(new SpacingBetweenLines { Before = "120", After = "60" }));
        p.AppendChild(Run($"{num}.", fontSize, bold: true));
        p.AppendChild(Run("   ", fontSize));
        p.AppendChild(Run(name, fontSize, bold: true));
        return p;
    }

    /// <summary>"**Label:** value" – bold label, normal value, indented.</summary>
    private Paragraph LabelValuePara(string indent, string label, string value)
    {
        var p = new Paragraph();
        var pp = new ParagraphProperties(new SpacingBetweenLines { After = "60" });
        pp.AppendChild(new Indentation { Left = indent });
        p.AppendChild(pp);
        p.AppendChild(Run(label, SzBody, bold: true));
        p.AppendChild(Run(value, SzBody));
        return p;
    }

    /// <summary>Empty paragraph (blank line).</summary>
    private Paragraph Blank()
    {
        var p = new Paragraph();
        p.AppendChild(new ParagraphProperties(new SpacingBetweenLines { After = "0" }));
        return p;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Run builder
    // ─────────────────────────────────────────────────────────────────────────
    private Run Run(string text, string fontSize, bool bold = false, bool italic = false,
        bool underline = false, string color = ColBlack)
    {
        var rp = new RunProperties();
        rp.AppendChild(new RunFonts { Ascii = Font, HighAnsi = Font });
        if (bold)      rp.AppendChild(new Bold());
        if (italic)    rp.AppendChild(new Italic());
        if (underline) rp.AppendChild(new Underline { Val = UnderlineValues.Single });
        rp.AppendChild(new FontSize { Val = fontSize });
        if (color != ColBlack)
            rp.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Color { Val = color });

        var r = new Run(rp);
        r.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return r;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ParagraphProperties helpers
    // ─────────────────────────────────────────────────────────────────────────
    private ParagraphProperties SpacedProps(int spaceAfter = 120, bool justified = false,
        bool centered = false, bool right = false, int spaceBefore = 0)
    {
        var pp = new ParagraphProperties();
        var sbl = new SpacingBetweenLines { After = spaceAfter.ToString() };
        if (spaceBefore > 0) sbl.Before = spaceBefore.ToString();
        pp.AppendChild(sbl);
        if (justified) pp.AppendChild(new Justification { Val = JustificationValues.Both });
        if (centered)  pp.AppendChild(new Justification { Val = JustificationValues.Center });
        if (right)     pp.AppendChild(new Justification { Val = JustificationValues.Right });
        return pp;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Image embedding
    // ─────────────────────────────────────────────────────────────────────────
    private void EmbedScreenshot(MainDocumentPart mainPart, Body body, ScreenshotViewModel shot)
    {
        if (!string.IsNullOrWhiteSpace(shot.Base64Data))
            EmbedBase64Image(mainPart, body, shot.Base64Data);
        else if (!string.IsNullOrWhiteSpace(shot.StorageReference))
            EmbedImage(mainPart, body, shot.StorageReference);
    }

    private void EmbedImage(MainDocumentPart mainPart, Body body, string storageReference)
    {
        if (string.IsNullOrWhiteSpace(storageReference)) return;
        try
        {
            var normalizedRef = storageReference.Replace('\\', '/').TrimStart('/');
            if (normalizedRef.Contains(".."))
            {
                body.AppendChild(MakePara("[Image Error: Invalid path]", bold: true, fontSize: "16", color: "FF0000"));
                return;
            }
            // User-uploaded images are stored in ContentRootPath (e.g. CMReviewEvidence/...)
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), normalizedRef);
            if (File.Exists(fullPath))
                AddImageToBody(mainPart, body, fullPath);
            else
                body.AppendChild(MakePara("[Image Missing: File not found]", bold: true, fontSize: "16", color: "FF0000"));
        }
        catch
        {
            body.AppendChild(MakePara("[Image Error: Could not load]", bold: true, fontSize: "16", color: "FF0000"));
        }
    }

    private void EmbedBase64Image(MainDocumentPart mainPart, Body body, string base64Data)
    {
        try
        {
            var commaIndex = base64Data.IndexOf(',');
            var base64Part = commaIndex >= 0 ? base64Data.Substring(commaIndex + 1) : base64Data;
            var imageBytes = Convert.FromBase64String(base64Part);
            var compressed = CompressImage(imageBytes);

            long cx, cy;
#pragma warning disable CA1416
            using (var img = Image.FromStream(new MemoryStream(compressed)))
                CalculateExtents(img.Width, img.Height, out cx, out cy);
#pragma warning restore CA1416

            var imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
            using (var stream = new MemoryStream(compressed))
                imagePart.FeedData(stream);

            AddImageElementToBody(body, mainPart.GetIdOfPart(imagePart), cx, cy);
        }
        catch
        {
            body.AppendChild(MakePara("[Image Error: Could not parse base64]", bold: true, fontSize: "16", color: "FF0000"));
        }
    }

    private void AddImageToBody(MainDocumentPart mainPart, Body body, string imagePath)
    {
        try
        {
            var bytes      = File.ReadAllBytes(imagePath);
            var compressed = CompressImage(bytes);

            long cx, cy;
#pragma warning disable CA1416
            using (var img = Image.FromStream(new MemoryStream(compressed)))
                CalculateExtents(img.Width, img.Height, out cx, out cy);
#pragma warning restore CA1416

            var imagePart = mainPart.AddImagePart(ImagePartType.Jpeg);
            using (var stream = new MemoryStream(compressed))
                imagePart.FeedData(stream);

            AddImageElementToBody(body, mainPart.GetIdOfPart(imagePart), cx, cy);
        }
        catch
        {
            body.AppendChild(MakePara("[Image Error: Could not load file]", bold: true, fontSize: "16", color: "FF0000"));
        }
    }

    private Drawing? CreateImageDrawing(MainDocumentPart mainPart, string imagePath)
    {
        long cx, cy;
#pragma warning disable CA1416
        using (var img = Image.FromFile(imagePath))
        {
            const long maxLogoWidth = 1371600L; // ~1.5 inches
            long origCx = (long)(img.Width  * 9525);
            long origCy = (long)(img.Height * 9525);
            if (origCx > maxLogoWidth) { cx = maxLogoWidth; cy = (long)(origCy * ((double)maxLogoWidth / origCx)); }
            else                       { cx = origCx; cy = origCy; }
        }
#pragma warning restore CA1416

        var imagePart = mainPart.AddImagePart(ImagePartType.Png);
        using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            imagePart.FeedData(stream);

        return CreateDrawingElement(mainPart.GetIdOfPart(imagePart), cx, cy);
    }

    private Drawing? CreateHeaderImageDrawing(HeaderPart headerPart, string imagePath)
    {
        long cx, cy;
#pragma warning disable CA1416
        using (var img = Image.FromFile(imagePath))
        {
            const long maxLogoWidth = 1371600L;  // ~1.5 inches
            const long maxLogoHeight = 457200L;  // ~0.5 inches
            long origCx = (long)(img.Width  * 9525);
            long origCy = (long)(img.Height * 9525);
            
            double ratioX = (double)maxLogoWidth / origCx;
            double ratioY = (double)maxLogoHeight / origCy;
            double ratio = Math.Min(ratioX, ratioY);
            
            if (ratio < 1.0)
            {
                cx = (long)(origCx * ratio);
                cy = (long)(origCy * ratio);
            }
            else
            {
                cx = origCx;
                cy = origCy;
            }
        }
#pragma warning restore CA1416

        var imagePart = headerPart.AddImagePart(ImagePartType.Png);
        using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            imagePart.FeedData(stream);

        return CreateDrawingElement(headerPart.GetIdOfPart(imagePart), cx, cy);
    }

    private void CalculateExtents(int width, int height, out long cx, out long cy)
    {
        const long maxWidth = 5900000L; // ~6.5 inches
        long origCx = (long)(width  * 9525);
        long origCy = (long)(height * 9525);
        if (origCx > maxWidth) { cx = maxWidth; cy = (long)(origCy * ((double)maxWidth / origCx)); }
        else                   { cx = origCx; cy = origCy; }
    }

    private void AddImageElementToBody(Body body, string relationshipId, long cx, long cy)
        => body.AppendChild(new Paragraph(new Run(CreateDrawingElement(relationshipId, cx, cy))));

    private Drawing CreateDrawingElement(string relationshipId, long cx, long cy)
        => new Drawing(
            new DocumentFormat.OpenXml.Drawing.Wordprocessing.Inline(
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.Extent { Cx = cx, Cy = cy },
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.DocProperties { Id = 1U, Name = "Picture" },
                new DocumentFormat.OpenXml.Drawing.Wordprocessing.NonVisualGraphicFrameDrawingProperties(
                    new DocumentFormat.OpenXml.Drawing.GraphicFrameLocks { NoChangeAspect = true }),
                new DocumentFormat.OpenXml.Drawing.Graphic(
                    new DocumentFormat.OpenXml.Drawing.GraphicData(
                        new DocumentFormat.OpenXml.Drawing.Pictures.Picture(
                            new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureProperties(
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualDrawingProperties { Id = 0U, Name = "Image" },
                                new DocumentFormat.OpenXml.Drawing.Pictures.NonVisualPictureDrawingProperties()),
                            new DocumentFormat.OpenXml.Drawing.Pictures.BlipFill(
                                new DocumentFormat.OpenXml.Drawing.Blip(
                                    new DocumentFormat.OpenXml.Drawing.BlipExtensionList(
                                        new DocumentFormat.OpenXml.Drawing.BlipExtension { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" }))
                                { Embed = relationshipId, CompressionState = DocumentFormat.OpenXml.Drawing.BlipCompressionValues.Print },
                                new DocumentFormat.OpenXml.Drawing.Stretch(new DocumentFormat.OpenXml.Drawing.FillRectangle())),
                            new DocumentFormat.OpenXml.Drawing.Pictures.ShapeProperties(
                                new DocumentFormat.OpenXml.Drawing.Transform2D(
                                    new DocumentFormat.OpenXml.Drawing.Offset { X = 0L, Y = 0L },
                                    new DocumentFormat.OpenXml.Drawing.Extents { Cx = cx, Cy = cy }),
                                new DocumentFormat.OpenXml.Drawing.PresetGeometry(
                                    new DocumentFormat.OpenXml.Drawing.AdjustValueList())
                                { Preset = DocumentFormat.OpenXml.Drawing.ShapeTypeValues.Rectangle })))
                    { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
            { DistanceFromTop = 0U, DistanceFromBottom = 0U, DistanceFromLeft = 0U, DistanceFromRight = 0U });

    private byte[] CompressImage(byte[] imageBytes)
    {
#pragma warning disable CA1416
        using var ms  = new MemoryStream(imageBytes);
        using var img = Image.FromStream(ms);

        int maxWidth = 1500, newW = img.Width, newH = img.Height;
        if (img.Width > maxWidth)
        {
            double r = (double)maxWidth / img.Width;
            newW = (int)(img.Width  * r);
            newH = (int)(img.Height * r);
        }

        using var newImg = new Bitmap(newW, newH);
        using (var g = Graphics.FromImage(newImg))
        {
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, 0, 0, newW, newH);
        }

        var jpegEncoder = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
            .FirstOrDefault(c => c.FormatID == System.Drawing.Imaging.ImageFormat.Jpeg.Guid);
        var encParams = new System.Drawing.Imaging.EncoderParameters(1);
        encParams.Param[0] = new System.Drawing.Imaging.EncoderParameter(
            System.Drawing.Imaging.Encoder.Quality, 85L);

        using var outMs = new MemoryStream();
        if (jpegEncoder != null)
            newImg.Save(outMs, jpegEncoder, encParams);
        else
            newImg.Save(outMs, System.Drawing.Imaging.ImageFormat.Jpeg);

        return outMs.ToArray();
#pragma warning restore CA1416
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Utilities
    // ─────────────────────────────────────────────────────────────────────────
    private static string ToWord(int n) => n switch
    {
        1  => "One",   2  => "Two",   3  => "Three",
        4  => "Four",  5  => "Five",  6  => "Six",
        7  => "Seven", 8  => "Eight", 9  => "Nine",
        10 => "Ten",   11 => "Eleven",12 => "Twelve",
        _  => n.ToString()
    };

    private static string CleanCoNumber(string co)
        => co.Replace("WO ", "", StringComparison.OrdinalIgnoreCase)
             .Replace("WO",  "", StringComparison.OrdinalIgnoreCase)
             .Replace("CR ", "", StringComparison.OrdinalIgnoreCase)
             .Replace("CR",  "", StringComparison.OrdinalIgnoreCase)
             .Trim();

    private static string JoinWithAnd(IList<string> items)
    {
        if (items.Count == 0) return "";
        if (items.Count == 1) return items[0];
        return string.Join(", ", items.Take(items.Count - 1)) + " and " + items.Last();
    }

    private static string GetCategoryKeyword(ChangeCategoryViewModel cat, CMReviewDocumentViewModel doc)
    {
        var firstId  = cat.ChangeDetails.FirstOrDefault()?.ChangeOrder;
        var woItem   = firstId != null ? doc.WOReportItems.FirstOrDefault(w => w.WoId == CleanCoNumber(firstId)) : null;
        
        if (woItem != null && !string.IsNullOrWhiteSpace(woItem.ChangeType))
        {
            return woItem.ChangeType;
        }

        return cat.CategoryName
            .Replace(" Change", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Change",  "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
}
