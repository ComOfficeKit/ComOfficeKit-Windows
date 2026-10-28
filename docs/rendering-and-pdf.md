# Rendering and PDF

## Pages

`PageCount` is what a page means for the kind of document:

| Kind | A page is |
|---|---|
| Word | a page as the document is laid out: sections, margins, headers and footers, footnotes, floating pictures |
| Spreadsheet | a printed page of `ActiveSheet`, from the file's page setup: paper, orientation, margins, scale or fit to page, print area, page breaks. A sheet with nothing to print has 0 |
| Presentation | a slide, hidden slides included |

## Page images

```csharp
PageImage image = document.RenderPage(index: 0, dpi: 144);
image.SavePng("page-1.png");
```

- `index` counts from 0 and must be less than `PageCount`.
- `dpi` is from 24 to 600; 96 is the size Windows shows at 100 % scaling. The image is the page's
  size in points × `dpi` ÷ 72 pixels, rounded up: an A4 page at 96 dpi is 794 × 1123.
- The pixels are BGRA32, premultiplied, the top row first, `Stride` = `Width` × 4 bytes a row. Every
  pixel is opaque: the page is drawn on white.
- A `PageImage` owns a copy of its pixels. It needs no disposal and outlives its document.
- Text is antialiased in grayscale, never ClearType, so an image can be scaled or composited without
  colour fringes, and the same call gives the same pixels on every machine.

### In WPF

```csharp
var image = document.RenderPage(0, dpi: 96 * scale);
var bitmap = BitmapSource.Create(image.Width, image.Height, image.Dpi, image.Dpi,
                                 PixelFormats.Pbgra32, null, image.Pixels.ToArray(), image.Stride);
pageView.Source = bitmap;
```

[Sample-WpfPreview](../Samples/Sample-WpfPreview) shows a document page by page.

### In WinUI 3

```csharp
var image = document.RenderPage(0);
var bitmap = new WriteableBitmap(image.Width, image.Height);
using (var stream = bitmap.PixelBuffer.AsStream())
{
    stream.Write(image.Pixels.Span);
}
bitmap.Invalidate();
```

## PDF

```csharp
document.ExportPdf("out.pdf");          // or a Stream
```

| Kind | The PDF holds |
|---|---|
| Word | every page |
| Spreadsheet | `ActiveSheet`'s printed pages |
| Presentation | the slides that are not hidden, as PowerPoint prints them |

Text stays text, with its fonts embedded. A file already at the path is replaced only once the whole
PDF is written.

**How the PDF is made.** The engine writes the PDF itself, in your process: no printer, no Print
Spooler service, no dialog. So:

- It works wherever the rest of ComOfficeKit does, servers included: nothing has to be installed or
  running on the machine.
- Exports of different documents run in parallel, as other calls do ([Errors and
  threading](errors-and-threading.md)). A long document takes a while: export it off your UI thread.
- Text is written as the glyphs the layout measured, in embedded fonts, so it can be searched and
  copied.
- A font the document uses that cannot be read throws `ComOfficeKitException` with
  `ErrorKind.Internal`, naming the font. A file that cannot be written throws it with `ErrorKind.IO`.

Not written: PDF/A, tagged PDF, encryption and passwords, digital signatures, form fields.
