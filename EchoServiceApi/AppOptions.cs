namespace EchoServiceApi
{
    public class AppOptions
    {
        [ConfigurationKeyName("diag_name")]
        public string? DiagName { get; set; }
    }
}
