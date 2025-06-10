using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarInsuranceTelegramBot
{
	public class Mindee
	{
		static string mindeeApiKey = "a9851a63943dab08b4a8bbbb9fc9c313";
		private const string drivingLicenceEndpoint =
			"https://api.mindee.net/v1/products/mindee/driver_license/v1/predict_async";

		private readonly HttpClient _http;

		public Mindee()
		{
			_http = new HttpClient();
			_http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", mindeeApiKey);
		}

		public async Task<ExtractedData> ExtractedDataAsync(string passportImagePath, string VehicleImagePath)
		{
			await Task.Delay(10);//simulating API-call delay here.

			return new ExtractedData
			{
				passportInfo = new PassportInfo()
				{
					FullName = "Volodymyr Velikiiy",
					DateOfBirth = "10.03.1960",
					PassportNumber = "UKR0123456"
				},

				vehicleInfo = new VehicleInfo()
				{
					Vin = "1HGCM82633A004352",
					Brand = "Honda",
					Model = "Civic",
					Year = "2015"
				}

			};
		}

		private async Task<VehicleInfo> ExtractLicenseAsync(string path)
		{
			if (!File.Exists(path))
			{
				throw new FileNotFoundException("License file was't found");
			}
			using var content = new MultipartFormDataContent();
			using var fs = File.OpenRead(path);
			var fileContent = new StreamContent(fs);
			fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
			content.Add(fileContent, "document", Path.GetFileName(path));

			var response = await _http.PostAsync(drivingLicenceEndpoint, content);
			response.EnsureSuccessStatusCode();
			using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

			var fields = doc.RootElement
				.GetProperty("document")
				.GetProperty("inference")
				.GetProperty("pages")[0]
				.GetProperty("inference")
				.GetProperty("fields");

			string GetValue(string key) =>
				fields.TryGetProperty(key, out var p) ?
				p.GetProperty("value").GetString() : "";

			// Mindee uses keys like "vin", "make", "model", "year"
			return new VehicleInfo
			{
				Vin = GetValue("vin"),
				Brand = GetValue("make"),
				Model = GetValue("model"),
				Year = GetValue("year")
			};
		}
	}

}