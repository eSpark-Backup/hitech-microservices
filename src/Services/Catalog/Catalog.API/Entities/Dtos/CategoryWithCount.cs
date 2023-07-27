namespace Catalog.API.Entities.Dtos
{
    public class ProductsResponse
    {
        public ProductsFilter ProductFilter { get; set; }
        public IEnumerable<ProductStats> ProductsStats { get; set; }
    }
    public class ProductsFilter
    {
        public Dictionary<string, int> Manufacturer { get; set; }
        public Dictionary<string, int> General { get; set; } = new Dictionary<string, int>();
        public IEnumerable<string> Categories { get; set; }
    }
    public class ProductStats
    {
        public string CategoryName { get; set; }
        public IEnumerable<SubCategoryDto> SubCategories { get; set; }
    }
    public class SubCategoryDto
    {
        public string SubCategoryName { get; set; }
        public int ProductCount { get; set; }
    }
    public class CategoryWithCount
    {
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public int ProductsCount { get; set; }
    }
}