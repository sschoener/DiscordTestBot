namespace DiscordTestBot {
    [System.AttributeUsage(System.AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class UsageExampleAttribute : System.Attribute
    {
        readonly string _exampleUsage;
        readonly string _description;

        public UsageExampleAttribute(string exampleUsage, string description)
        {
            _exampleUsage = exampleUsage;
            _description = description;
        }
        
        public string Usage { get { return _exampleUsage; } }
        public string Description { get { return _description; } }
    }
}