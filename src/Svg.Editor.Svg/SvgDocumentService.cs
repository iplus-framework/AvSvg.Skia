using System.IO;
using Svg;
using Svg.Model.Services;
using Svg.Skia;
using SK = SkiaSharp;

namespace Svg.Editor.Svg;

public class SvgDocumentService
{
    public SvgDocument? Open(string path)
        => SvgService.Open(path);

    public SvgDocument? Open(Stream stream)
        => SvgService.Open(stream);

    public SvgDocument? FromSvg(string svg)
        => SvgService.FromSvg(svg);

    public string GetXml(SvgDocument document)
        => document.GetXML();

    public void Save(SvgDocument document, string path)
        => document.Write(path);

    public bool ExportToPng(SK.SKPicture picture, Stream stream, SK.SKColor background, int quality = 100, float scaleX = 1f, float scaleY = 1f)
        => picture.ToImage(stream, background, SK.SKEncodedImageFormat.Png, quality, scaleX, scaleY, SK.SKColorType.Rgba8888, SK.SKAlphaType.Premul, SK.SKColorSpace.CreateSrgb());

    public bool ExportToPdf(SK.SKPicture picture, string path, SK.SKColor background, float scaleX = 1f, float scaleY = 1f)
        => picture.ToPdf(path, background, scaleX, scaleY);

    public bool ExportToXps(SK.SKPicture picture, string path, SK.SKColor background, float scaleX = 1f, float scaleY = 1f)
        => picture.ToXps(path, background, scaleX, scaleY);
}
