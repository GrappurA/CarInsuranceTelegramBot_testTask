using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceTelegramBot
{
	public class DriverLicenseInfo
	{
		public string fullName { get; set; } = "";
		public string dateOfBirth { get; set; } = "";
		public string licenseNumber { get; set; } = "";
		public string licenseClass { get; set; } = "";
		public string expiryDate { get; set; } = "";
		public string countryCode { get; set; } = "";
	}
}

