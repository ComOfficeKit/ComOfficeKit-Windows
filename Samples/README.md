# Samples

| | |
|---|---|
| [Sample-Convert](Sample-Convert) | a console app: a document to a PDF and a PNG of each page |
| [Sample-TemplateFill](Sample-TemplateFill) | a console app: fill a Word template's `{{placeholders}}`, build a small invoice workbook, save both |
| [Sample-WpfPreview](Sample-WpfPreview) | a WPF window that shows a document page by page |
| [Sample-ObjectModel](Sample-ObjectModel) | a console app: write a report, a workbook and a deck through the document model, save them, read them back |

To run one:

```powershell
Scripts/fetch-nupkg.ps1 -Tag <release tag>     # the package, into packages/
$env:COMOFFICEKIT_LICENSE_KEY = '<your licence key>'
cd Samples/Sample-Convert
dotnet run -- C:\path\to\report.docx C:\out
```

They take ComOfficeKit from `packages/` (see `nuget.config` and `Directory.Build.props` here) and build
for x64, the engine's only architecture.
