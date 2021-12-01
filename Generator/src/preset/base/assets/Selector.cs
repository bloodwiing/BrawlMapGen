using BMG.Preset;
using Idle.Serialization;
using System;

namespace BMG
{
    public class Selector
    {
        [IdleShortFlag]
        public SelectorType RunType { get; protected set; }

        [IdleProperty("DEFAULT")]
        public string DefaultId { get; protected set; }

        [IdleProperty("RULESET")]
        public RuleSet[] RuleSets { get; protected set; }


        public string Choose(byte neighbors)
        {
            switch (RunType)
            {
                case SelectorType.SINGLE:
                    return "Main";

                case SelectorType.ADAPTIVE:
                    return RunAdaptive(neighbors);

                default:
                    throw new NotImplementedException();
            }
        }

        protected string RunAdaptive(byte neighbors)
        {
            string asset = DefaultId;

            foreach (RuleSet set in RuleSets)
            {
                foreach (Rule rule in set.Rules)
                {
                    if (rule.Condition.Check(neighbors))
                    {
                        if (rule.ModifyResult != null)
                            neighbors = rule.UpdateData(neighbors);

                        if (rule.Select != null)
                            asset = rule.Select;

                        if (!set.IsStrict)
                            break;
                    }
                }
            }

            return asset;
        }
    }
}
