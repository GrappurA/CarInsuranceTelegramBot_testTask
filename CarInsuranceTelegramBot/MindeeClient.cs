using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceTelegramBot
{
	public class MindeeClient
	{
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

	}
}
