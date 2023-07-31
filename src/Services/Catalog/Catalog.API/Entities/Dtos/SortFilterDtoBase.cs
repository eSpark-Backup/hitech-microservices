using static Catalog.API.Entities.Dtos.CEnums;

namespace Catalog.API.Entities.Dtos
{
    public class SortFilterDtoBase
    {
        protected string GetEnumaValue(string? porductAttrib)
        {
            switch (porductAttrib)
            {
                case "categoryName":
                    return "CategoryName";

                case "subCategoryName":
                    return "SubCategory.SubCategoryName";

                case "manufacturer":
                    return "SubCategory.Product.Manufacturer";

                case "manufacturerPartNo":
                    return "SubCategory.Product.ManufacturerPartNo";

                case "name":
                    return "SubCategory.Product.Name";

                case "price":
                    return "SubCategory.Product.Price";

                case "packaging":
                    return "SubCategory.Product.Packaging";

                case "stock":
                    return "SubCategory.Product.Stock";

                case "series":
                    return "SubCategory.Product.Series";

            }
            return "SubCategory.Product.Name";
        }
    }
}