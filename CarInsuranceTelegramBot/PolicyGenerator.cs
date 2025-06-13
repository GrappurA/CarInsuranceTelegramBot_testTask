using Microsoft.Extensions.Options;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using Scriban;
using System;
using System.IO;

public class PolicyPdfGenerator
{
	public static byte[] GeneratePolicyPdf(string fullname, string dateOfBirth, string licenseNumber,
										   string licenseClass, string expiryDate, string countryCode)
	{
		// Dummy data
		var model = new
		{
			PolicyNumber = $"POL-{Guid.NewGuid().ToString().Substring(0, 8)}",
			Fullname = fullname,
			DateofBirth = dateOfBirth,
			LicenseNumber = licenseNumber,
			LicenseClass = licenseClass,
			ExpiryDate = expiryDate,
			CountryCode = countryCode,

			start_date = DateTime.Now.ToString("yyyy-MM-dd"),
			end_date = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd")
		};

		// Load template from file (or define it inline)
		string policyText = $"Insurance Policy Document\n" +
"-------------------------\n\n" +
"Policy Number   : {{ PolicyNumber }}\n" +
"Full Name       : {{ Fullname }}\n" +
"Date of Birth   : {{ DateofBirth }}\n" +
"License Number  : {{ LicenseNumber }}\n" +
"License Class   : {{ LicenseClass }}\n" +
"Expiry Date     : {{ ExpiryDate }}\n" +
"Country Code    : {{ CountryCode }}\n\n" +
"-------------------------\n" +
"Insurance Start date : {{start_date}}\n" +
"Insurance End date   : {{end_date}}\n" +
"-------------------------\n\n" +
"This document certifies that the above-named individual holds a valid insurance policy\n" +
"in accordance with the terms and conditions set forth by the issuing authority. \n" +
"This policy is subject to national and international regulations \n" +
"regarding driver and vehicle coverage.\n\n" +
"Thank you for choosing our insurance service.\n";

		var template = Template.Parse(policyText);
		string filledText = template.Render(model);

		// Create a PDF document
		using var document = new PdfDocument();
		var page = document.AddPage();
		var gfx = XGraphics.FromPdfPage(page);

		GlobalFontSettings.FontResolver = new FontReseolver();
		var font = new XFont("OpenSans", 14);

		// Split text into lines and draw them
		double y = 40;
		foreach (var line in filledText.Split('\n'))
		{
			gfx.DrawString(line.Trim(), font, XBrushes.Black, new XRect(40, y, page.Width - 80, page.Height - y), XStringFormats.TopLeft);
			y += 20;
		}

		// Save to memory stream
		using var ms = new MemoryStream();
		document.Save(ms, false);
		return ms.ToArray();
	}
}
