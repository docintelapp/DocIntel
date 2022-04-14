namespace DocIntel.Core.EmailViews.Shared
{
    public class EmailButtonViewModel
    {
        public EmailButtonViewModel(string text, string url)
        {
            Text = text;
            Url = url;
        }

        public string Text { get; set; }
        public string Url { get; set; }
        public string BackgroundColor { get; set; } = "#fd3995";
    }
}