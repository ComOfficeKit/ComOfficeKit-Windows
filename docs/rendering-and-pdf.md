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

## Page text

`GetPageText` returns the text a page draws and where it draws it: what an application lays over the
page's image so its user can select, copy and search it, or reads to find where a word is on the page.

```csharp
PageText text = document.GetPageText(index: 0);
foreach (PageTextLine line in text.Lines)
{
    Console.WriteLine($"{line.X:0.#}, {line.Baseline:0.#}: {line.Text}");
}
```

- The pages and the points are `RenderPage`'s: lengths are in points (1/72 inch) from the page's
  top-left corner, y growing down, so a point is `dpi` ÷ 72 pixels of the image drawn at `dpi`.
  `Width` and `Height` are the page's.
- It holds what the page draws as text: headers and footers, footnotes and their marks, a watermark,
  a chart's labels, a turned shape's text. Text a clip hides, such as a cell's text past its cell, is
  not in it.
- `Lines` come in the order the page draws them: a Word page's text in reading order, then what lies
  over it; a slide's shapes from back to front; a sheet's cells row by row, then its charts and
  drawings. `Text` is every line's text, one to a line.
- A line starts at (`X`, `Baseline`) and is turned `Rotation` degrees clockwise about that point: 0
  for a level line, 90 for one that runs down the page. Its box, its spans and their advances are
  given before the turn: lay them out unturned, then turn the whole line about its start.
- A `PageTextSpan` is a stretch of a line in one size at one baseline, a superscript's or a
  subscript's included. `Start` and `Length` index the line's `Text` in UTF-16 code units, and
  `Advances` has one advance for each: the n-th code unit starts at the span's `X` plus the first n
  advances.
- A `PageText` owns its data and outlives its document. It needs a licence that allows rendering,
  as `RenderPage` does, and an overload takes a `CancellationToken`.

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

- It works wherever the rest of ComOffice does, servers included: nothing has to be installed or
  running on the machine.
- Exports of different documents run in parallel, as other calls do ([Errors and
  threading](errors-and-threading.md)). A long document takes a while: export it off your UI thread.
- Text is written as the glyphs the layout measured, in embedded fonts, so it can be searched and
  copied.
- A font the document uses that cannot be read throws `ComOfficeKitException` with
  `ErrorKind.Internal`, naming the font. A file that cannot be written throws it with `ErrorKind.IO`.

Not written: PDF/A, tagged PDF, encryption and passwords, digital signatures, form fields.
