namespace BaGet.Web
{
    public static class RazorExtensions
    {
        public static string ToMetric(this long value)
        {
            if (value < 1000)
                return value.ToString();

            string[] suffixes = { "K", "M", "B", "T" };
            double tempValue = value;
            var suffixIndex = -1;

            while (tempValue >= 1000 && suffixIndex < suffixes.Length - 1)
            {
                tempValue /= 1000;
                suffixIndex++;
            }

            return $"{tempValue:0.#}{suffixes[suffixIndex]}";
        }
    }
}
