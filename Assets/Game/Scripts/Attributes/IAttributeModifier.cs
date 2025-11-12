using System.Collections.Generic;
//---------------------------------

namespace PolyQuest.Attributes
{
    public interface IAttributeModifier
    {
        IEnumerable<float> GetAdditiveModifiers(Stat stat);
        IEnumerable<float> GetPercentageModifiers(Stat stat);
    }
}