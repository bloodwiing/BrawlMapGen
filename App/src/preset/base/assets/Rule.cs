using Idle.Serialization;

namespace BMG
{
    public class Rule
    {
        [IdleFlag("CONDITION")]
        string _condition { set => Condition = new Condition(value); }
        public Condition Condition { get; protected set; }

        [IdleProperty("MODIFY_RESULT")]
        string _modifyResult { set => ModifyResult = new Trinary(value); }
        public Trinary ModifyResult { get; protected set; } = null;

        [IdleProperty("SELECT")]
        public string Select { get; protected set; } = null;

        public byte UpdateData(byte data)
        {
            byte updated = 0;

            for (int i = 7; i >= 0; i--)
            {
                updated <<= 1;

                if ((ModifyResult.wild >> i & 1) == 1)
                    updated += (byte)(data >> i & 1);

                else if ((ModifyResult.constant >> i & 1) == 1)
                    updated++;
            }

            return updated;
        }
    }
}
