using CarInsuranceTelegramBot;
using Microsoft.Extensions.Options;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using Scriban;
using System;
using System.IO;

public class PolicyPdfGenerator
{
	public static byte[] GeneratePolicyPdf(DrivingLicenseInfo license, VehicleInfo registration)
	{
		Console.WriteLine($"[GeneratePolicyPdf] fullName={license.fullName}, dateOfBirth={license.dateOfBirth}, licenseNumber={license.licenseNumber}, licenseClass={license.licenseClass}, expiryDate={license.expiryDate}, countryCode={license.countryCode}");

		// Define template as verbatim string so "{{...}}" reaches Scriban intact
		var model = new
		{
			policy_number = $"POL-{Guid.NewGuid():N}".Substring(0, 8),
			full_name = license.fullName,
			birth_date = license.dateOfBirth,
			license_number = license.licenseNumber,
			license_class = license.licenseClass,
			expiry_date = license.expiryDate,
			country_code = license.countryCode,

			vin = registration.Vin,
			plate = registration.Plate,
			brand = registration.Brand,
			year = registration.Year,

			start_date = DateTime.Now.ToString("yyyy-MM-dd"),
			end_date = DateTime.Now.AddYears(1).ToString("yyyy-MM-dd")
		};

		string policyText = @"
Insurance Policy Document
-------------------------
License Info:
Policy Number   : {{ policy_number }}
Full Name       : {{ full_name }}
Date of Birth   : {{ birth_date }}
License Number  : {{ license_number }}
License Class   : {{ license_class }}
Expiry Date     : {{ expiry_date }}
Country Code    : {{ country_code }}

-------------------------
Vehicle Info:
VIN             : {{vin}}
Plate           : {{plate}}
Brand           : {{brand}}
Year            : {{year}}

-------------------------
Insurance Start date : {{ start_date }}
Insurance End date   : {{ end_date }}
-------------------------

This document certifies that the above-named individual holds a valid insurance policy
in accordance with the terms and conditions set forth by the issuing authority.
This policy is subject to national and international regulations
regarding driver and vehicle coverage.

Thank you for choosing our insurance service.
";

		var template = Template.Parse(policyText);
		if (template.HasErrors)
		{
			Console.WriteLine("[Scriban] Template parse errors:");
			foreach (var msg in template.Messages)
				Console.WriteLine($"  {msg}");
		}

		string filledText = template.Render(model);
		Console.WriteLine("===== FilledText START =====");
		// Show each line explicitly
		foreach (var line in filledText.Split('\n'))
		{
			Console.WriteLine($"|{line}|");
		}
		Console.WriteLine("===== FilledText END =====");

		// Create PDF
		using var document = new PdfDocument();
		var page = document.AddPage();
		var gfx = XGraphics.FromPdfPage(page);

		GlobalFontSettings.FontResolver = new FontReseolver();
		// Test drawing a simple string first:
		var testFont = new XFont("OpenSans", 12);

		// Draw filledText lines
		double y = 70; // start below DEBUG text
		foreach (var line in filledText.Split('\n'))
		{
			var text = line.Trim();
			Console.WriteLine($"Drawing line at y={y}: '{text}'");
			gfx.DrawString(text, testFont, XBrushes.Black,
						   new XRect(40, y, page.Width - 80, page.Height - y), XStringFormats.TopLeft);
			y += 20;
		}

		using var ms = new MemoryStream();
		document.Save(ms, false);
		Console.WriteLine("[GeneratePolicyPdf] PDF byte length: " + ms.Length);
		return ms.ToArray();
	}
}
