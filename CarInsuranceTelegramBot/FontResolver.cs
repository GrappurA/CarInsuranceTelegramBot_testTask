using System;
using System.IO;
using PdfSharp.Fonts;

public class FontReseolver : IFontResolver
{
	public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
	{
		if (familyName.Equals("OpenSans", StringComparison.OrdinalIgnoreCase))
		{
			// only regular in this example
			return new FontResolverInfo("OpenSans-Regular#");
		}
		throw new InvalidOperationException($"Font '{familyName}' not registered.");
	}

	public byte[] GetFont(string faceName)
	{
		if (faceName == "OpenSans-Regular#")
		{
			var path = Path.Combine(AppContext.BaseDirectory, "fonts", "OpenSans-Regular.ttf");
			if (!File.Exists(path))
				throw new FileNotFoundException(path);
			return File.ReadAllBytes(path);
		}
		throw new InvalidOperationException($"Unexpected faceName '{faceName}'.");
	}
}
