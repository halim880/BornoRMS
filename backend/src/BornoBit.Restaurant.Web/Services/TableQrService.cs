using Microsoft.Extensions.Options;
using QRCoder;

namespace BornoBit.Restaurant.Web.Services;

public sealed class TableQrService
{
    private readonly CustomerSiteOptions _options;

    public TableQrService(IOptions<CustomerSiteOptions> options) => _options = options.Value;

    public string BuildTableUrl(Guid tableId)
        => $"{_options.BaseUrl.TrimEnd('/')}/menu?table={tableId}";

    public string BuildQrPngDataUri(string url, int pixelsPerModule = 10)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(data);
        return "data:image/png;base64," + Convert.ToBase64String(png.GetGraphic(pixelsPerModule));
    }
}
