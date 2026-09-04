namespace EchoServiceApi
{
    public class AppOptions
    {
        [ConfigurationKeyName("diag_name")]
        public string? DiagName { get; set; }

        public string? DefaultPath { get; set; }

        public string? NamePath { get; set; }
    }
}
