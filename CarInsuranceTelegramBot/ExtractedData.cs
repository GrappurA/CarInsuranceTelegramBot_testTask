using System.Threading.Tasks;
using Telegram.Bot.Types;

namespace CarInsuranceTelegramBot
{
	public class ExtractedData
	{
		public async Task<DrivingLicenseInfo> GetDriverLicense(PhotoSize? photo, byte[] photoBytes)
		{
			await Task.Delay(50); // simulate async
			return new DrivingLicenseInfo
			{
				fullName = "Volodymyr Velikyi",
				dateOfBirth = "1960-03-10",
				licenseNumber = "UKR0123456",
				licenseClass = "B",
				expiryDate = "2030-01-01",
				countryCode = "UA"
			};
		}

		public static async Task<VehicleInfo> GetVehicleRegistarion(PhotoSize? photo, byte[] photoBytes)
		{
			await Task.Delay(50); // simulate async
			return new VehicleInfo
			{
				Vin = "1HGCM82633A004352",
				Brand = "Honda",
				Year = "2015",
				Plate = "AA1234BB"
			};
		}
	}
}
