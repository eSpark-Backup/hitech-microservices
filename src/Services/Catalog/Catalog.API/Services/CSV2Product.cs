using Catalog.API.Entities;
using Catalog.API.Entities.Dtos;
using Microsoft.IdentityModel.Tokens;
using Sylvan.Data;
using Sylvan.Data.Csv;
using System.Data;
using System.Diagnostics;
using System.Globalization;

namespace Catalog.API.Services
{
    public class CSV2Category : ICSV2Category
    {
        enum EColIndex
        {
            Category = 0,
            CategoryImageUrl,
            SubCategory,
            SubCategoryImageUrl,
            ManufacturerPartNo,
            Manufacturer,
            ItemName,
            LaunchDate,
            Description,
            Stock,
            Price,
            Packaging,
            Series,
            CADModelUrl,
            DatasheetUrl,
            ImageUrls,
            AdditionalFields
        }
        public CSVDto Read(string path)
        {
            CSVDto cSVDto = new CSVDto();
            cSVDto.NewProducts = new List<Category>();
            cSVDto.InvalidEntries = new List<string>();
            cSVDto.DuplicatePartNumbers = new List<string>();
            cSVDto.UpdateProducts = new List<Category>();
            var stopWatch = Stopwatch.StartNew();
            int count = 0;
            using (var reader = CsvDataReader.Create(path))
            {

                // Loop through each row in the CSV file
                while (reader.Read())
                {
                    try
                    {
                        // Create a new instance of your custom class
                        Category obj = new Category();

                        // Set the properties of the product object from the data in the CSV file
                        obj.CategoryName = reader.GetString((int)EColIndex.Category);
                        obj.CategoryImageUrl = reader.GetString((int)EColIndex.CategoryImageUrl);

                        obj.SubCategory = new();

                        obj.SubCategory.SubCategoryName = reader.GetString((int)EColIndex.SubCategory);
                        obj.SubCategory.SubCategoryImageUrl = reader.GetString((int)EColIndex.SubCategoryImageUrl);
                        obj.SubCategory.Product = new();

                        obj.SubCategory.Product.ManufacturerPartNo = reader.GetString((int)EColIndex.ManufacturerPartNo);
                        obj.SubCategory.Product.Manufacturer = reader.GetString((int)EColIndex.Manufacturer);
                        if (DateTimeOffset.TryParse(reader.GetString((int)EColIndex.LaunchDate), null, DateTimeStyles.AssumeLocal, out DateTimeOffset parsedDate))
                            obj.SubCategory.Product.LaunchDate = parsedDate;

                        obj.SubCategory.Product.Name = reader.GetString((int)EColIndex.ItemName);
                        obj.SubCategory.Product.ProductNameShortdesc = reader.GetString((int)EColIndex.Description);
                        int intValue = reader.GetInt32((int)EColIndex.Stock);
                        // converting Int32 to Uint32                    
                        uint uintValue = unchecked((uint)intValue);

                        obj.SubCategory.Product.Quantity = uintValue;

                        obj.SubCategory.Product.UnitPrice = reader.GetDecimal((int)EColIndex.Price);
                        obj.SubCategory.Product.Packaging = reader.GetString((int)EColIndex.Packaging);
                        obj.SubCategory.Product.Series = reader.GetString((int)EColIndex.Series);
                        obj.SubCategory.Product.DataSheetUrl = reader.GetString((int)EColIndex.DatasheetUrl);
                        obj.SubCategory.Product.ImageUrl = reader.GetString((int)EColIndex.ImageUrls);

                        string input = reader.GetString((int)EColIndex.AdditionalFields);
                        if (!input.IsNullOrEmpty())
                        {
                            // remove curly braces
                            input = input.Replace("[", "").Replace("]", "");

                            // remove curly braces
                            input = input.Replace("{", "").Replace("}", "");

                            // split key-value pairs using comma delimiter
                            string[] pairs = input.Split(',');
                            obj.SubCategory.Product.AdditionalFields =
                                new Dictionary<string, string>();
                            for (int i = 0; i < pairs.Length; i++)
                            {
                                obj.SubCategory.Product.AdditionalFields
                                    .Add(pairs[i], pairs[++i]);
                            }
                        }
                        if (cSVDto.NewProducts
                            .Where(a => a.SubCategory.Product.ManufacturerPartNo ==
                                obj.SubCategory.Product.ManufacturerPartNo &&
                                a.SubCategory.Product.Manufacturer ==
                                obj.SubCategory.Product.Manufacturer).Count() > 0)
                        {
                            Debug.WriteLine("Duplicate Entry : " + (reader.RowNumber + 1).ToString());
                            cSVDto.DuplicatePartNumbers.Add(reader.GetRawRecordSpan().ToString());
                        }
                        else
                            cSVDto.NewProducts.Add(obj);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine((reader.RowNumber + 1).ToString() + " : " + ex.Message);
                        cSVDto.InvalidEntries.Add(reader.GetRawRecordSpan().ToString());
                    }
                }
                //Console.WriteLine(count);
                Console.WriteLine($"seconds : {stopWatch.Elapsed.TotalSeconds}");
                Console.WriteLine($"Total Products added : {cSVDto.NewProducts.Count}");
                return cSVDto;
            }
        }
    }
}
