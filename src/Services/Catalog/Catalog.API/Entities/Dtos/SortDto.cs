using static Catalog.API.Entities.Dtos.CEnums;
using System.ComponentModel;

namespace Catalog.API.Entities.Dtos
{
    public class SortDto : SortFilterDtoBase
    {
        private string orderby;
        public string Orderby
        {
            get => orderby;
            set
            {
                if (value == null)
                    orderby = "CategoryName";
                orderby = value;
            }
        }
        [DefaultValue(true)]
        public bool IsAccending { get; set; }
        public string Orderbyvalue()
        {
            return GetEnumaValue(Orderby);
        }
    }
}
