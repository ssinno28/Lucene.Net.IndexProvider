namespace Lucene.Net.IndexProvider.Models
{
    public class IndexResult<T>
    {
        public float Score { get; set; }
        public T Hit { get; set; }
    }
}