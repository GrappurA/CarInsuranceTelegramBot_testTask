using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceTelegramBot
{
	public class ExtractedData
	{
		public PassportInfo passportInfo { get; set; } = new PassportInfo();
		public VehicleInfo vehicleInfo { get; set; } = new VehicleInfo();
	}
}
