using Idle.Serialization;

namespace BMG
{
    public class RuleSet
    {
        [IdleFlag("STRICT")]
        public bool IsStrict { get; protected set; }

        [IdleProperty("RULE")]
        public Rule[] Rules { get; protected set; }
    }
}
