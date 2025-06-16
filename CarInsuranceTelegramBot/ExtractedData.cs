using Mindee;
using Mindee.Input;
using Mindee.Product.DriverLicense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Google.Cloud.Vision.V1;
using SixLabors.ImageSharp;
using System.Text.RegularExpressions;
using FuzzySharp;

namespace CarInsuranceTelegramBot
{
	public class ExtractedData
	{
		static string mindeeApiKey = File.ReadAllText("G:\\Studying\\telegramBot\\config\\mindee.txt");

		public async Task<DrivingLicenseInfo> GetDriverLicense(PhotoSize? photo, byte[] photoBytes)
		{
			MindeeClient mindeeClient = new(mindeeApiKey);
			var inputSource = new LocalInputSource(photoBytes, $"license_{photo.FileId}.jpg");

			var response = await mindeeClient.EnqueueAndParseAsync<DriverLicenseV1>(inputSource);
			var driverLicense = response.Document.Inference.Prediction;

			var drivingLicenseInfo = new DrivingLicenseInfo
			{
				fullName = driverLicense.FirstName.Value + driverLicense.LastName.Value ?? "Not found",
				dateOfBirth = driverLicense.DateOfBirth?.Value ?? "Not found",
				licenseNumber = driverLicense.Id?.Value ?? "Not found",
				licenseClass = driverLicense.Category?.Value ?? "Not found",
				expiryDate = driverLicense.ExpiryDate?.Value ?? "Not found",
				countryCode = driverLicense.CountryCode?.Value ?? "Not found"
			};

			return drivingLicenseInfo;
		}

		public static async Task<VehicleInfo> GetVehicleRegistarion(PhotoSize? photo, byte[] photoBytes)
		{
			var client = ImageAnnotatorClient.Create();
			var image = Google.Cloud.Vision.V1.Image.FromStream(new MemoryStream(photoBytes));
			var response = await client.DetectTextAsync(image);
			string allText = string.Join("\n", response.Select(a => a.Description)).ToLower();

			string vin = "Not found", brand = "Not found", year = "Not found", plate = "Not found";
			string[] knownBrands = { "toyota", "honda", "ford", "chevrolet", "bmw", "audi",
				"hyundai", "nissan", "kia", "mercedes", "dodge" };

			Console.Write(allText);

			//looking for info
			// Brand
			var brandMatch = Process.ExtractOne(allText, knownBrands);
			if (brandMatch != null && brandMatch.Score > 55)
			{
				brand = brandMatch.Value;
			}

			// VIN
			var vinMatch = Regex.Match(allText, "[A-Za-z0-9]{17}");
			if (vinMatch.Success)
			{
				vin = vinMatch.Value;
			}

			// Year
			var yearMatch = Regex.Matches(allText, @"\b(19|20)\d{2}\b");
			List<int> years = new List<int>();
			foreach (Match item in yearMatch)
			{
				years.Add(int.Parse(item.Value));
			}
			if (years.Count > 0)
			{
				year = years.Min().ToString();
			}

			// Plate
			var plateMatch = Regex.Match(allText, @"\b[A-Z]{2,4}[0-9]{2,4}\b");
			if (plateMatch.Success)
			{
				plate = plateMatch.Value;
			}

			var vehicleInfo = new VehicleInfo()
			{
				Vin = vin,
				Brand = brand,
				Year = year,
				Plate = plate
			};
			return vehicleInfo;
		}

	}
}
