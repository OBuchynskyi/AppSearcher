namespace AppSearcher
{
    public enum Statuses
    {
        Found = 1,
        NotFound = 2,
        Error = 3
    }

    public  class PageSerachResult
    {
        public string Url { get; set; }

        public Statuses SerachStatus { get; set; }

        public string Error { get; set; }
    }
}