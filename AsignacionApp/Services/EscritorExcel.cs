using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace AsignacionApp.Services
{
    public static class EscritorExcel
    {
        // Índices de los estilos definidos en AgregarEstilos
        private const uint EstiloFecha = 1;
        private const uint EstiloImporte = 2;
        private const uint EstiloEncabezado = 3;

        // Límites de Excel: 1,048,576 filas por hoja (una es el encabezado) y 32,767 caracteres por celda
        public const int MaxRegistros = 1_048_575;
        public const int MaxCaracteresCelda = 32_767;

        public static void Escribir(string ruta, string nombreHoja, string[] encabezado, IReadOnlyList<object?[]> filas)
        {
            using var documento = SpreadsheetDocument.Create(ruta, SpreadsheetDocumentType.Workbook);
            var libro = documento.AddWorkbookPart();
            libro.Workbook = new Workbook();
            AgregarEstilos(libro);

            var hoja = libro.AddNewPart<WorksheetPart>();
            var letras = Enumerable.Range(1, encabezado.Length).Select(LetraColumna).ToArray();

            // OpenXmlWriter escribe fila por fila sin cargar todo el XML en memoria (rápido)
            using (var writer = OpenXmlWriter.Create(hoja))
            {
                writer.WriteStartElement(new Worksheet());

                // Encabezado fijo al bajar
                writer.WriteElement(new SheetViews(new SheetView(
                    new Pane { VerticalSplit = 1, TopLeftCell = "A2", ActivePane = PaneValues.BottomLeft, State = PaneStateValues.Frozen })
                { WorkbookViewId = 0 }));

                writer.WriteElement(new Columns(new Column
                { Min = 1, Max = (uint)encabezado.Length, Width = 18, CustomWidth = true }));

                writer.WriteStartElement(new SheetData());

                writer.WriteStartElement(new Row { RowIndex = 1 });
                for (int c = 0; c < encabezado.Length; c++)
                {
                    var celda = CeldaTexto(encabezado[c], letras[c] + 1);
                    celda.StyleIndex = EstiloEncabezado;
                    writer.WriteElement(celda);
                }
                writer.WriteEndElement();

                uint numFila = 2;
                foreach (var fila in filas)
                {
                    writer.WriteStartElement(new Row { RowIndex = numFila });
                    for (int c = 0; c < fila.Length; c++)
                    {
                        var celda = CrearCelda(fila[c], letras[c] + numFila);
                        if (celda is not null)
                            writer.WriteElement(celda);
                    }
                    writer.WriteEndElement();
                    numFila++;
                }

                writer.WriteEndElement(); // SheetData

                // Filtros en el encabezado
                writer.WriteElement(new AutoFilter { Reference = $"A1:{letras[^1]}{numFila - 1}" });

                writer.WriteEndElement(); // Worksheet
            }

            libro.Workbook.AppendChild(new Sheets(new Sheet
            {
                Id = libro.GetIdOfPart(hoja),
                SheetId = 1,
                Name = NombreHojaValido(nombreHoja)
            }));
            libro.Workbook.Save();
        }

        // Las celdas vacías no se escriben
        private static Cell? CrearCelda(object? valor, string referencia) => valor switch
        {
            null => null,
            decimal d => new Cell { CellReference = referencia, CellValue = new CellValue(d), StyleIndex = EstiloImporte },
            DateTime f => new Cell { CellReference = referencia, CellValue = new CellValue(f.ToOADate()), StyleIndex = EstiloFecha },
            _ => CeldaTexto(valor.ToString()!, referencia)
        };

        // Si una celda pasa de este largo, Excel dice que el archivo está dañado
        private static Cell CeldaTexto(string texto, string referencia) => new()
        {
            CellReference = referencia,
            DataType = CellValues.InlineString,
            InlineString = new InlineString(new Text(texto.Length > MaxCaracteresCelda ? texto[..MaxCaracteresCelda] : texto))
        };

        private static void AgregarEstilos(WorkbookPart libro)
        {
            var estilos = libro.AddNewPart<WorkbookStylesPart>();
            estilos.Stylesheet = new Stylesheet(
                new NumberingFormats(new NumberingFormat { NumberFormatId = 164, FormatCode = "dd/mm/yyyy" }) { Count = 1 },
                new Fonts(
                    new Font(new FontSize { Val = 11 }, new FontName { Val = "Calibri" }),
                    new Font(new Bold(), new FontSize { Val = 11 }, new FontName { Val = "Calibri" })) { Count = 2 },
                new Fills(
                    new Fill(new PatternFill { PatternType = PatternValues.None }),
                    new Fill(new PatternFill { PatternType = PatternValues.Gray125 })) { Count = 2 },
                new Borders(new Border()) { Count = 1 },
                new CellFormats(
                    new CellFormat(),                                                // 0 normal
                    new CellFormat { NumberFormatId = 164, ApplyNumberFormat = true },  // 1 fecha
                    new CellFormat { NumberFormatId = 4, ApplyNumberFormat = true },    // 2 importe #,##0.00
                    new CellFormat { FontId = 1, ApplyFont = true }) { Count = 4 });   // 3 encabezado
        }

        // 1 -> A, 27 -> AA, 54 -> BB
        private static string LetraColumna(int numero)
        {
            string letras = "";
            while (numero > 0)
            {
                numero--;
                letras = (char)('A' + numero % 26) + letras;
                numero /= 26;
            }
            return letras;
        }

        // Excel no permite : \ / ? * [ ] ni más de 31 caracteres
        private static string NombreHojaValido(string nombre)
        {
            var limpio = new string(nombre.Where(ch => !@":\/?*[]".Contains(ch)).ToArray()).Trim();
            if (limpio.Length == 0) limpio = "Hoja1";
            return limpio.Length > 31 ? limpio[..31] : limpio;
        }
    }
}
