using Catalog.API.Data.Interfaces;
using Catalog.API.Entities;
using Catalog.API.Entities.Dtos;
using Catalog.API.Extensions;
using Catalog.API.Filter;
using Catalog.API.Repositories.Interfaces;
using MassTransit.Internals;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Catalog.API.Repositories
{
    public class ProductRepositoryR : IProductRepositoryR
    {
        private readonly IProductContext _context;
        public ProductRepositoryR(IProductContext context)
        {
            _context = context;
        }
        public async Task<ProductsFilter> GetProductsFilter()
        {
            // exclude id, return only gender and date of birth
            var projection = Builders<Category>.Projection
                .Exclude(e => e.Id)
                .Include(u => u.CategoryName)
                .Include(u => u.SubCategory.Product.Manufacturer)
                .Include(u => u.SubCategory.Product.InStock)
                .Include(u => u.SubCategory.Product.LaunchDate)
                .Include(u => u.SubCategory.Product.CADModelUrl)
                ;

            var filter = new BsonDocument();

            // Get results
            var projectedData = await _context.CategoryList
                .Find(filter)
                .Project(projection) // projection stage
                .ToListAsync()
                ;

            var categories = projectedData.Select(v => BsonSerializer.Deserialize<Category>(v)).ToList();
            ProductsFilter pf = new();

            pf.Manufacturer = categories
                  .Select(a => a.SubCategory.Product.Manufacturer)
                  .ToList()
                  .GroupBy(ii => ii.ToString())
                  .ToDictionary(group => group.Key, group => group.Count());
            
            var instock = categories
                  .Select(a => a.SubCategory.Product.InStock)
                  .Where(a => a == true)
                  .Count();
            pf.General.Add("Instock", instock);
            
            var NewlyAdded = categories
                  .Select(a => a.SubCategory.Product.LaunchDate)
                  .Where(a=>a>=DateTimeOffset.UtcNow.AddMonths(-1))
                  .Count();
            pf.General.Add("NewlyAdded", NewlyAdded);

            var CADModelUrl = categories
                  .Select(a => a.SubCategory.Product.CADModelUrl)
                  .Where(a=>!a.IsNullOrEmpty())
                  .Count();
            pf.General.Add("CADAvailable", CADModelUrl);

            pf.Categories = categories
                .Select(a => a.CategoryName).Distinct()
                .ToList();
            return pf;
        }
        public async Task<ProductsResponse> GetProducts()
        {
            var pp = await _context.CategoryList
                .Aggregate()
                .Group(
                a => new { a.CategoryName, a.SubCategory.SubCategoryName },
                b => new CategoryWithCount
                {
                    CategoryName = b.Key.CategoryName,
                    SubCategoryName = b.Key.SubCategoryName,
                    ProductsCount = b.Count()
                })
                .ToListAsync();
            var cats = pp.GroupBy(p => p.CategoryName, (key, group) => new ProductStats
            {
                CategoryName = key,
                SubCategories = group.Select(a => new SubCategoryDto { SubCategoryName = a.SubCategoryName, ProductCount = a.ProductsCount }).ToList(),
            })
                .OrderBy(a => a.CategoryName)
                .ToList();

            var filters = await GetProductsFilter();
            return new ProductsResponse() 
            {
                ProductsStats = cats,
                ProductFilter = filters,
            };
        }
        public async Task<Category> GetProductById(string _id)
        {
            return await _context
                .CategoryList
                .Find(p => p.Id == _id)
                .FirstOrDefaultAsync();
        }
        public async Task<FilterResult> GetFilteredProducts
            (PaginationFilter pagefilter, FilterSortDto myfilter)
        {
            SortDefinition<Category> sortDefinition = (bool)myfilter.Sortdto.IsAccending
                              ? Builders<Category>.Sort.Ascending(myfilter.Sortdto.Orderbyvalue())
                              : Builders<Category>.Sort.Descending(myfilter.Sortdto.Orderbyvalue());

            var filters = Builders<Category>.Filter.Empty;
            if (myfilter.Filters != null || myfilter.Filters.Count() != 0)
            {
                var filtersdef = new List<FilterDefinition<Category>>();
                foreach (var item in myfilter.Filters)
                {
                    if (item.FilterValue != null)
                        filtersdef.Add(Builders<Category>.Filter.Eq(item.Filetrbyvalue(), item.FilterValue));
                }
                if (filtersdef.Count != 0)
                    filters = Builders<Category>.Filter.And(filtersdef);
            }
            var (a, b) = await _context.CategoryList
                .AggregateByPage(filters, sortDefinition, page: pagefilter.PageNumber, pageSize: pagefilter.PageSize);

            var additionalFieldsgroup = await GetAdditionalData(filters);

            return new FilterResult() { TotalRecords = a, Items = b, FiltersMeta = additionalFieldsgroup };
        }

        private async Task<Dictionary<string, Dictionary<string, int>>> GetAdditionalData(FilterDefinition<Category> filters)
        {
            // exclude id, return only gender and date of birth
            var projection = Builders<Category>.Projection
                .Exclude(e => e.Id)
                .Include(u => u.SubCategory.Product.AdditionalFields)
                .Include(u => u.SubCategory.Product.Series)
                .Include(u => u.SubCategory.Product.Packaging)
                ;

            // Get results
            var projectedData = await _context.CategoryList
                .Find(filters)
                .Project(projection) // projection stage
                .ToListAsync()
                ;

            var categories = projectedData.Select(v => BsonSerializer.Deserialize<Category>(v)).ToList();

            Dictionary<string, Dictionary<string, int>> filtersWithCount = new();

            var additionalFields = categories
               .SelectMany(a => a.SubCategory.Product.AdditionalFields)
               .GroupBy(a => a.Key)
               .ToList()
               ;

            var series = categories
                  .Select(a => a.SubCategory.Product.Series)
                  .ToList()
                  .GroupBy(ii => ii.ToString())
                  .ToDictionary(group => group.Key, group => group.Count());

            var packagings = categories
             .Select(a => a.SubCategory.Product.Packaging)
             .ToList()
             .GroupBy(ii => ii.ToString())
             .ToDictionary(group => group.Key, group => group.Count());

            filtersWithCount.Add("SeriesFilter", series);
            filtersWithCount.Add("PackagingFilter", packagings);

            foreach (var item in additionalFields)
                filtersWithCount.Add(item.Key, item.GroupBy(a => a.Value).ToDictionary(a => a.Key, a => a.Count()));


            return filtersWithCount;
        }

        public async Task<IEnumerable<Category>> GetProductsByMFP(string mfp, string mf)
        {
            var filter1 = Builders<Category>.Filter.Eq(x => x.SubCategory.Product.ManufacturerPartNo, mfp);
            var filter2 = Builders<Category>.Filter.Eq(x => x.SubCategory.Product.Manufacturer, mf);
            var res = await _context.CategoryList.Find(filter1 & filter2).ToListAsync();
            return res;
        }
    }
    public class UnwindData
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public DateTimeOffset ModifiedDate { get; set; }
        public Dictionary<string, string> AdditionalFields { get; set; }
    }
}
