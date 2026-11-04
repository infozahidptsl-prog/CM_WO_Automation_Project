using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using WO_CM_Review_Automation.Application.Interfaces;
using WO_CM_Review_Automation.Application.Reports;
using WO_CM_Review_Automation.Domain.Entities;

namespace WO_CM_Review_Automation.Infrastructure.Services.CMReview;

public class CMReviewEvidenceImageGenerator : ICMReviewEvidenceImageGenerator
{
    private static Font GetFont(string familyName, float size, FontStyle style)
    {
        try
        {
            return new Font(familyName, size, style, GraphicsUnit.Point);
        }
        catch
        {
            return new Font(FontFamily.GenericSansSerif, size, style, GraphicsUnit.Point);
        }
    }

    public byte[] GenerateSummaryTableImage(
        string monthYear,
        int totalRawWorkOrders,
        List<ChangeManagementReportItem> items,
        string highlightedApp)
    {
        const int margin = 12;
        const int tableWidth = 980;

        const int titleRowHeight = 44;
        const int totalRowHeight = 34;
        const int headerRowHeight = 54;
        const int dataRowHeight = 36;
        const int rowCount = 6;
        const int totalDataHeight = dataRowHeight * rowCount;
        const int tableHeight = titleRowHeight + totalRowHeight + headerRowHeight + totalDataHeight;

        int imageWidth = tableWidth + (margin * 2);
        int imageHeight = tableHeight + (margin * 2);

        using var bitmap = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        // Clean white background
        g.Clear(Color.White);

        // Predefine standard columns
        // 0: Division (155)
        // 1: Functional Unit (240)
        // 2: Team Leader/POC (220)
        // 3: Implemented Change (140)
        // 4: Implemented Change with backdated CM (145)
        // 5: Total (80)
        int colDivisionWidth = 155;
        int colFuWidth = 240;
        int colTlWidth = 220;
        int colImpWidth = 140;
        int colBackWidth = 145;
        int colTotalWidth = 80;

        int startX = margin;
        int startY = margin;

        using var pen1px = new Pen(Color.Black, 1f);
        using var pen2px = new Pen(Color.Black, 2f);
        using var brushBlack = new SolidBrush(Color.Black);

        using var brushTitleBg = new SolidBrush(Color.FromArgb(244, 176, 132)); // #F4B084
        using var brushTotalBg = new SolidBrush(Color.FromArgb(255, 230, 153)); // #FFE699
        using var brushHeaderBg = new SolidBrush(Color.FromArgb(142, 169, 219)); // #8EA9DB
        using var brushYellow = new SolidBrush(Color.FromArgb(255, 255, 0)); // #FFFF00
        using var brushWhite = new SolidBrush(Color.White);

        using var fontTitle = GetFont("Calibri", 13.5f, FontStyle.Bold);
        using var fontTotal = GetFont("Calibri", 11f, FontStyle.Bold);
        using var fontHeader = GetFont("Calibri", 10.5f, FontStyle.Bold);
        using var fontData = GetFont("Calibri", 10.5f, FontStyle.Regular);
        using var fontDivision = GetFont("Calibri", 10f, FontStyle.Regular);

        using var formatCenter = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        using var formatLeft = new StringFormat
        {
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center
        };

        // 1. Title Banner: "Change Management Summary {MonthYear}"
        var titleRect = new Rectangle(startX, startY, tableWidth, titleRowHeight);
        g.FillRectangle(brushTitleBg, titleRect);
        g.DrawRectangle(pen1px, titleRect);

        string titleText = $"Change Management Summary {monthYear}";
        g.DrawString(titleText, fontTitle, brushBlack, titleRect, formatCenter);

        // 2. Total Change Row: "Total Change :" | "{totalRawWorkOrders}"
        int totalY = startY + titleRowHeight;
        var totalLabelRect = new Rectangle(startX, totalY, colDivisionWidth, totalRowHeight);
        g.FillRectangle(brushTotalBg, totalLabelRect);
        g.DrawRectangle(pen1px, totalLabelRect);

        var totalLabelPadRect = new Rectangle(startX + 10, totalY, colDivisionWidth - 10, totalRowHeight);
        g.DrawString("Total Change :", fontTotal, brushBlack, totalLabelPadRect, formatLeft);

        var totalValRect = new Rectangle(startX + colDivisionWidth, totalY, tableWidth - colDivisionWidth, totalRowHeight);
        g.FillRectangle(brushTotalBg, totalValRect);
        g.DrawRectangle(pen1px, totalValRect);

        var totalValPadRect = new Rectangle(startX + colDivisionWidth + 10, totalY, tableWidth - colDivisionWidth - 10, totalRowHeight);
        g.DrawString(totalRawWorkOrders.ToString(), fontTotal, brushBlack, totalValPadRect, formatLeft);

        // 3. Column Header Row: Division | Functional Unit | Team Leader/POC | Implemented Change | Implemented Change with backdated CM | Total
        int headerY = totalY + totalRowHeight;
        int currentX = startX;

        var headerCols = new (string Text, int Width)[]
        {
            ("Division", colDivisionWidth),
            ("Functional Unit", colFuWidth),
            ("Team Leader/POC", colTlWidth),
            ("Implemented Change", colImpWidth),
            ("Implemented Change with\nbackdated CM", colBackWidth),
            ("Total", colTotalWidth)
        };

        foreach (var col in headerCols)
        {
            var hRect = new Rectangle(currentX, headerY, col.Width, headerRowHeight);
            g.FillRectangle(brushHeaderBg, hRect);
            g.DrawRectangle(pen1px, hRect);
            g.DrawString(col.Text, fontHeader, brushBlack, hRect, formatCenter);
            currentX += col.Width;
        }

        // 4. Data Rows (6 Functional Units)
        int dataStartY = headerY + headerRowHeight;

        // Division cell vertically merged across all 6 rows
        var divRect = new Rectangle(startX, dataStartY, colDivisionWidth, totalDataHeight);
        g.FillRectangle(brushWhite, divRect);
        g.DrawRectangle(pen1px, divRect);

        string divisionText = "Business Support\nService and Operations\nSupport Service";
        g.DrawString(divisionText, fontDivision, brushBlack, divRect, formatCenter);

        // Standard 6 functional units matching the official report template
        var standardUnits = new (string Unit, string DefaultPOC)[]
        {
            ("AMS", "Maksud Saifullah Pulak"),
            ("DMS", "Md Maruf Ul Alam"),
            ("LEA", "Minhaz Uddin Ahmed"),
            ("POS", "Md Maruf Ul Alam"),
            ("Sales Force App / RSO App", "Maksud Saifullah Pulak"),
            ("TechVan", "Md Amirul Islam")
        };

        for (int i = 0; i < standardUnits.Length; i++)
        {
            var std = standardUnits[i];
            int rowY = dataStartY + (i * dataRowHeight);

            // Lookup existing item
            var matchedItem = items?.FirstOrDefault(x =>
                string.Equals(x.FunctionalUnit, std.Unit, StringComparison.OrdinalIgnoreCase) ||
                (std.Unit.Contains("Sales Force", StringComparison.OrdinalIgnoreCase) && x.FunctionalUnit.Contains("Sales Force", StringComparison.OrdinalIgnoreCase)) ||
                (std.Unit.Contains("TechVan", StringComparison.OrdinalIgnoreCase) && x.FunctionalUnit.Contains("Techvan", StringComparison.OrdinalIgnoreCase)));

            string fuName = std.Unit;
            string pocName = !string.IsNullOrWhiteSpace(matchedItem?.TeamLeader) ? matchedItem.TeamLeader : std.DefaultPOC;
            int count = matchedItem?.ActualCount ?? 0;

            bool isHighlighted = MatchesHighlightedApp(fuName, highlightedApp);
            var fillBrush = isHighlighted ? brushYellow : brushWhite;

            // Functional Unit
            int colX = startX + colDivisionWidth;
            var fuRect = new Rectangle(colX, rowY, colFuWidth, dataRowHeight);
            g.FillRectangle(fillBrush, fuRect);
            g.DrawRectangle(pen1px, fuRect);
            var fuPadRect = new Rectangle(colX + 10, rowY, colFuWidth - 10, dataRowHeight);
            g.DrawString(fuName, fontData, brushBlack, fuPadRect, formatLeft);

            // Team Leader/POC
            colX += colFuWidth;
            var tlRect = new Rectangle(colX, rowY, colTlWidth, dataRowHeight);
            g.FillRectangle(fillBrush, tlRect);
            g.DrawRectangle(pen1px, tlRect);
            g.DrawString(pocName, fontData, brushBlack, tlRect, formatCenter);

            // Implemented Change
            colX += colTlWidth;
            var impRect = new Rectangle(colX, rowY, colImpWidth, dataRowHeight);
            g.FillRectangle(fillBrush, impRect);
            g.DrawRectangle(pen1px, impRect);
            g.DrawString(count.ToString(), fontData, brushBlack, impRect, formatCenter);

            // Implemented Change with backdated CM (blank)
            colX += colImpWidth;
            var backRect = new Rectangle(colX, rowY, colBackWidth, dataRowHeight);
            g.FillRectangle(fillBrush, backRect);
            g.DrawRectangle(pen1px, backRect);
            g.DrawString("", fontData, brushBlack, backRect, formatCenter);

            // Total
            colX += colBackWidth;
            var totRect = new Rectangle(colX, rowY, colTotalWidth, dataRowHeight);
            g.FillRectangle(fillBrush, totRect);
            g.DrawRectangle(pen1px, totRect);
            g.DrawString(count.ToString(), fontData, brushBlack, totRect, formatCenter);
        }

        // 5. Outer Table Frame (2px solid black)
        g.DrawRectangle(pen2px, new Rectangle(startX, startY, tableWidth, tableHeight));

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    public byte[] GenerateFilteredExcelImage(
        List<WorkOrder> filteredWorkOrders,
        HashSet<string> appWorkOrderIds)
    {
        const int margin = 8;
        const int headerHeight = 32;
        const int rowHeight = 28;

        var columns = new (string Header, int Width, StringAlignment Align)[]
        {
            ("WO", 75, StringAlignment.Near),
            ("Stat", 60, StringAlignment.Near),
            ("Subject", 410, StringAlignment.Near),
            ("WO Type", 80, StringAlignment.Near),
            ("Change Type", 100, StringAlignment.Near),
            ("Service Name", 115, StringAlignment.Near),
            ("Domain", 65, StringAlignment.Near),
            ("NE Type", 105, StringAlignment.Near),
            ("NE Count", 65, StringAlignment.Center),
            ("Vendors", 110, StringAlignment.Near),
            ("Issuer", 140, StringAlignment.Near),
            ("QA / KPI Checked By", 140, StringAlignment.Near),
            ("Assignee", 140, StringAlignment.Near),
            ("SLA", 55, StringAlignment.Center),
            ("Issue Date/Time", 125, StringAlignment.Near),
            ("Proposed", 215, StringAlignment.Near),
            ("Execution Start", 125, StringAlignment.Near),
            ("Execution End", 125, StringAlignment.Near)
        };

        int tableWidth = columns.Sum(c => c.Width);
        int rowCount = Math.Max(1, filteredWorkOrders?.Count ?? 0);
        int tableHeight = headerHeight + (rowCount * rowHeight);

        int imageWidth = tableWidth + (margin * 2);
        int imageHeight = tableHeight + (margin * 2);

        using var bitmap = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);

        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;

        g.Clear(Color.White);

        using var penGrid = new Pen(Color.FromArgb(212, 212, 212), 1f); // #D4D4D4
        using var penHeaderBottom = new Pen(Color.FromArgb(212, 212, 212), 2f);
        using var brushBlack = new SolidBrush(Color.Black);
        using var brushWhite = new SolidBrush(Color.White);
        using var brushYellow = new SolidBrush(Color.FromArgb(255, 255, 0)); // #FFFF00

        using var fontHeader = GetFont("Calibri", 9.5f, FontStyle.Bold);
        using var fontData = GetFont("Calibri", 9.5f, FontStyle.Regular);

        int startX = margin;
        int startY = margin;

        // Header Row
        int currentX = startX;
        for (int i = 0; i < columns.Length; i++)
        {
            var col = columns[i];
            var hRect = new Rectangle(currentX, startY, col.Width, headerHeight);
            g.FillRectangle(brushWhite, hRect);

            // Right border
            g.DrawLine(penGrid, currentX + col.Width, startY, currentX + col.Width, startY + headerHeight);

            using var sf = new StringFormat
            {
                Alignment = col.Align,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            var textRect = new Rectangle(currentX + 6, startY, col.Width - 12, headerHeight);
            g.DrawString(col.Header, fontHeader, brushBlack, textRect, sf);

            currentX += col.Width;
        }

        // Header bottom border
        g.DrawLine(penHeaderBottom, startX, startY + headerHeight, startX + tableWidth, startY + headerHeight);

        // Data Rows
        if (filteredWorkOrders != null && filteredWorkOrders.Any())
        {
            for (int r = 0; r < filteredWorkOrders.Count; r++)
            {
                var wo = filteredWorkOrders[r];
                int rowY = startY + headerHeight + (r * rowHeight);

                bool isAppRow = appWorkOrderIds != null && appWorkOrderIds.Contains(wo.WoId);
                var rowBrush = isAppRow ? brushYellow : brushWhite;

                string[] rowValues = new string[]
                {
                    wo.WoId ?? "",
                    wo.Status ?? "",
                    wo.Subject ?? "",
                    wo.WoType ?? "",
                    wo.ChangeType ?? "",
                    wo.ServiceName ?? "",
                    wo.Domain ?? "",
                    wo.NeType ?? "",
                    wo.NeCount?.ToString() ?? "",
                    wo.Vendor ?? "",
                    wo.Issuer ?? "",
                    wo.QaKpiCheckedBy ?? "",
                    wo.Assignee ?? "",
                    wo.Sla ?? "",
                    wo.IssueDateTime ?? "",
                    wo.ProposedDateTime ?? "",
                    wo.ExecutionStart ?? "",
                    wo.ExecutionEnd ?? ""
                };

                currentX = startX;
                for (int c = 0; c < columns.Length; c++)
                {
                    var col = columns[c];
                    var cellRect = new Rectangle(currentX, rowY, col.Width, rowHeight);
                    g.FillRectangle(rowBrush, cellRect);

                    // Cell right border & bottom border
                    g.DrawLine(penGrid, currentX + col.Width, rowY, currentX + col.Width, rowY + rowHeight);
                    g.DrawLine(penGrid, currentX, rowY + rowHeight, currentX + col.Width, rowY + rowHeight);

                    using var sf = new StringFormat
                    {
                        Alignment = col.Align,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter,
                        FormatFlags = StringFormatFlags.NoWrap
                    };

                    var textRect = new Rectangle(currentX + 6, rowY, col.Width - 12, rowHeight);
                    g.DrawString(rowValues[c], fontData, brushBlack, textRect, sf);

                    currentX += col.Width;
                }
            }
        }

        // Outer border
        g.DrawRectangle(penGrid, new Rectangle(startX, startY, tableWidth, tableHeight));

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        return ms.ToArray();
    }

    private static bool MatchesHighlightedApp(string functionalUnit, string highlightedApp)
    {
        if (string.IsNullOrWhiteSpace(functionalUnit) || string.IsNullOrWhiteSpace(highlightedApp))
            return false;

        var fu = functionalUnit.Trim();
        var app = highlightedApp.Trim();

        if (app.Equals("Techvan", StringComparison.OrdinalIgnoreCase) || app.Equals("TechVan", StringComparison.OrdinalIgnoreCase))
            return fu.Contains("Techvan", StringComparison.OrdinalIgnoreCase) || fu.Contains("TechVan", StringComparison.OrdinalIgnoreCase);

        if (app.Equals("Sales Force App", StringComparison.OrdinalIgnoreCase) || app.Equals("SFA", StringComparison.OrdinalIgnoreCase))
            return fu.Contains("Sales Force", StringComparison.OrdinalIgnoreCase) || fu.Contains("RSO", StringComparison.OrdinalIgnoreCase);

        return fu.Equals(app, StringComparison.OrdinalIgnoreCase);
    }
}
