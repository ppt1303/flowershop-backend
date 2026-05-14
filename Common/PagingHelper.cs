namespace FlowerShop.Common
{
    public static class PagingHelper
    {
        public static (int Page, int Limit) Normalize(int page, int limit, int defaultLimit = 10, int maxLimit = 100)
        {
            if (page < 1) page = 1;
            if (limit < 1) limit = defaultLimit;
            if (limit > maxLimit) limit = maxLimit;

            return (page, limit);
        }
    }
}
