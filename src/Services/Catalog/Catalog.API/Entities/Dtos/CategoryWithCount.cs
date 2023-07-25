namespace Catalog.API.Entities.Dtos
{
    public class CategoryWithCount
    {
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public int ProductsCount { get; set; }
    }
    public class CategorySubWithCountDto
    {
        public string CategoryName { get; set; }
        public IEnumerable<SubCategoryDto> SubCategories { get; set; }
    }
    public class SubCategoryDto
    {
        public string SubCategoryName { get; set; }
        public int ProductCount { get; set; }
    }

}