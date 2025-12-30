using System;
using System.Collections.Generic;
using System.Linq;
using BomEtlAddin.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace BomEtlAddin
{
    public class BomExporter
    {
        private readonly ISldWorks _swApp;

        public BomExporter(ISldWorks swApp)
        {
            _swApp = swApp;
        }

        public List<BomRow> GetActiveDocumentBom()
        {
            var model = _swApp?.ActiveDoc as IModelDoc2;
            if (model == null)
            {
                return new List<BomRow>();
            }

            return TryExtractFromDrawing(model) ?? ExtractFromAssembly(model);
        }

        private List<BomRow> TryExtractFromDrawing(IModelDoc2 model)
        {
            if (model.GetType() != (int)swDocumentTypes_e.swDocDRAWING)
            {
                return null;
            }

            var drawing = (IDrawingDoc)model;
            var sheet = drawing.GetCurrentSheet() as ISheet;
            if (sheet == null)
            {
                return null;
            }

            var tables = sheet.GetTableAnnotations() as object[];
            if (tables == null)
            {
                return null;
            }

            foreach (var tableObj in tables)
            {
                var table = tableObj as ITableAnnotation;
                if (table == null || table.Type != (int)swTableAnnotationType_e.swTableAnnotation_BillOfMaterials)
                {
                    continue;
                }

                return ReadBomTable(table);
            }

            return null;
        }

        private static List<BomRow> ReadBomTable(ITableAnnotation table)
        {
            var rows = new List<BomRow>();
            var rowCount = table.RowCount;
            var colCount = table.ColumnCount;

            for (var row = 1; row < rowCount; row++)
            {
                var bomRow = new BomRow
                {
                    ItemNumber = GetCell(table, row, 0),
                    PartNumber = GetCell(table, row, FindColumnIndex(table, colCount, "Part Number")),
                    Description = GetCell(table, row, FindColumnIndex(table, colCount, "Description")),
                    Configuration = GetCell(table, row, FindColumnIndex(table, colCount, "Configuration")),
                    Quantity = ParseQuantity(GetCell(table, row, FindColumnIndex(table, colCount, "Qty")))
                };

                rows.Add(bomRow);
            }

            return rows;
        }

        private List<BomRow> ExtractFromAssembly(IModelDoc2 model)
        {
            if (model.GetType() != (int)swDocumentTypes_e.swDocASSEMBLY)
            {
                return new List<BomRow>();
            }

            var assembly = (IAssemblyDoc)model;
            var components = assembly.GetComponents(true) as object[];
            if (components == null)
            {
                return new List<BomRow>();
            }

            var grouped = new Dictionary<string, BomRow>(StringComparer.OrdinalIgnoreCase);

            foreach (var componentObj in components)
            {
                var component = componentObj as IComponent2;
                if (component == null || component.IsSuppressed())
                {
                    continue;
                }

                var modelDoc = component.GetModelDoc2() as IModelDoc2;
                if (modelDoc == null)
                {
                    continue;
                }

                var key = component.GetPathName();
                if (!grouped.TryGetValue(key, out var row))
                {
                    row = new BomRow
                    {
                        ItemNumber = (grouped.Count + 1).ToString(),
                        PartNumber = GetCustomProperty(modelDoc, "PartNo"),
                        Description = GetCustomProperty(modelDoc, "Description"),
                        Configuration = component.ReferencedConfiguration
                    };

                    grouped[key] = row;
                }

                row.Quantity += 1;
            }

            return grouped.Values.OrderBy(r => r.ItemNumber).ToList();
        }

        private static string GetCustomProperty(IModelDoc2 model, string name)
        {
            var extension = model.Extension;
            var config = model.ConfigurationManager.ActiveConfiguration?.Name ?? string.Empty;
            var customPropManager = extension.CustomPropertyManager[config];
            customPropManager.Get4(name, false, out var value, out var resolved);
            return string.IsNullOrWhiteSpace(resolved) ? value ?? string.Empty : resolved;
        }

        private static int FindColumnIndex(ITableAnnotation table, int colCount, string headerLabel)
        {
            for (var col = 0; col < colCount; col++)
            {
                var header = table.get_Text(0, col);
                if (string.Equals(header, headerLabel, StringComparison.OrdinalIgnoreCase))
                {
                    return col;
                }
            }

            return 0;
        }

        private static string GetCell(ITableAnnotation table, int row, int column)
        {
            if (row < 0 || column < 0 || row >= table.RowCount || column >= table.ColumnCount)
            {
                return string.Empty;
            }

            return table.get_Text(row, column) ?? string.Empty;
        }

        private static int ParseQuantity(string value)
        {
            return int.TryParse(value, out var qty) ? qty : 0;
        }
    }
}
