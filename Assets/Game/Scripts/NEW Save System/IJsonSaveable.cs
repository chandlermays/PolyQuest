using Newtonsoft.Json.Linq;
//---------------------------------

namespace PolyQuest.Saving
{
    public interface IJsonSaveable
    {
        JToken CaptureJToken();
        void RestoreJToken(JToken state);
    }
}
