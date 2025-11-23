using System.Collections.Generic;
//---------------------------------

namespace PolyQuest.Attributes
{
    public interface IStatModifier
    {
        IEnumerable<float> GetAdditiveModifiers(Stat stat);
        IEnumerable<float> GetPercentageModifiers(Stat stat);
    }
}