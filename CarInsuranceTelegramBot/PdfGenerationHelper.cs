using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfSharpCore.Drawing;
using PdfSharpCore.Fonts;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.Advanced;
using PdfSharpCore.Utils;

namespace CarInsuranceTelegramBot
{
	public class PdfGenerationHelper
	{
		public static string GeneratePolicyPDF(string policyText, string filename = "Policy.pdf")
		{
			PdfDocument policy = new();
			PdfPage page = policy.AddPage();

			var gfx = XGraphics.FromPdfPage(page);
			var font = new XFont("Times New Roman", 14, XFontStyle.Bold);
			var textColor = XBrushes.Black;
			var layout = new XRect(20, 20, page.Width, page.Height);
			var format = XStringFormats.Center;

			gfx.DrawString(policyText, font, XBrushes.Black,
			new XRect(40, 40, page.Width - 80, page.Height - 80),
			XStringFormats.TopLeft);

			Directory.CreateDirectory("GeneratedPolicy");
			var path = Path.Combine("GeneratedPolicy", filename);
			policy.Save(path);
			return path;
		}


	}
}
