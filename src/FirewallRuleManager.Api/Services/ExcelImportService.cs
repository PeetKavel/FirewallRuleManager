using ClosedXML.Excel;
using FirewallRuleManager.Shared.DTOs;
using FirewallRuleManager.Shared.Models;

namespace FirewallRuleManager.Api.Services;

public class ExcelImportService
{
    private static readonly string[] ValidProtocols = { "TCP", "UDP", "ICMP", "ANY" };

    public ImportResult Import(Stream excelStream, out List<FirewallRule> validRules)
    {
        validRules = new List<FirewallRule>();
        var result = new ImportResult();

        using var workbook = new XLWorkbook(excelStream);
        var worksheet = workbook.Worksheets.First();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int row = 2; row <= lastRow; row++)
        {
            var errors = new List<string>();
            var rule = new FirewallRule();

            // From Hostname
            var fromHostname = worksheet.Cell(row, 1).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(fromHostname))
                errors.Add($"Row {row}: From Hostname is required.");
            else if (fromHostname.Length > 255)
                errors.Add($"Row {row}: From Hostname cannot exceed 255 characters.");
            else
                rule.FromHostname = fromHostname;

            // To Hostname
            var toHostname = worksheet.Cell(row, 2).GetString()?.Trim();
            if (string.IsNullOrWhiteSpace(toHostname))
                errors.Add($"Row {row}: To Hostname is required.");
            else if (toHostname.Length > 255)
                errors.Add($"Row {row}: To Hostname cannot exceed 255 characters.");
            else
                rule.ToHostname = toHostname;

            // Port Number
            var portCell = worksheet.Cell(row, 3);
            if (portCell.IsEmpty())
            {
                errors.Add($"Row {row}: Port Number is required.");
            }
            else if (!int.TryParse(portCell.GetString(), out int port) || port < 1 || port > 65535)
            {
                errors.Add($"Row {row}: Port Number must be a valid number between 1 and 65535.");
            }
            else
            {
                rule.PortNumber = port;
            }

            // Description (optional)
            var description = worksheet.Cell(row, 4).GetString()?.Trim();
            if (!string.IsNullOrEmpty(description) && description.Length > 1000)
                errors.Add($"Row {row}: Description cannot exceed 1000 characters.");
            else
                rule.Description = string.IsNullOrEmpty(description) ? null : description;

            // Protocol
            var protocol = worksheet.Cell(row, 5).GetString()?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(protocol))
            {
                errors.Add($"Row {row}: Protocol is required.");
            }
            else if (!ValidProtocols.Contains(protocol))
            {
                errors.Add($"Row {row}: Protocol must be one of: {string.Join(", ", ValidProtocols)}.");
            }
            else
            {
                rule.Protocol = protocol;
            }

            // Registration Date
            var dateCell = worksheet.Cell(row, 6);
            if (dateCell.IsEmpty())
            {
                errors.Add($"Row {row}: Registration Date is required.");
            }
            else
            {
                DateTime regDate;
                if (dateCell.DataType == XLDataType.DateTime)
                {
                    regDate = dateCell.GetDateTime();
                    rule.RegistrationDate = DateTime.SpecifyKind(regDate, DateTimeKind.Utc);
                }
                else if (DateTime.TryParse(dateCell.GetString(), out regDate))
                {
                    rule.RegistrationDate = DateTime.SpecifyKind(regDate, DateTimeKind.Utc);
                }
                else
                {
                    errors.Add($"Row {row}: Registration Date must be a valid date.");
                }
            }

            if (errors.Count > 0)
            {
                result.FailureCount++;
                result.Errors.Add(new ImportRowError { RowNumber = row, Messages = errors });
            }
            else
            {
                result.SuccessCount++;
                validRules.Add(rule);
            }
        }

        return result;
    }
}
