using System.Collections.Generic;
using System.Linq;

namespace ActionEditor
{
    internal class Lan
    {
        #region 静态

        public static readonly Dictionary<string, ILanguages> AllLanguages = new Dictionary<string, ILanguages>();
        private static string _lan;
        public static ILanguages ins;
        internal static void Load()
        {
            var lan = Prefs.Lan_key;
            var types = EditorEX.GetImplementationsOf(typeof(ILanguages));
            foreach (var t in types)
            {
                var name = EditorEX.GetTypeName(t);
                AllLanguages[name] = System.Activator.CreateInstance(t) as ILanguages;
                if (lan == name)
                {
                    ins = AllLanguages[name];
                }
            }
            _lan = lan;
            if (ins == null)
            {
                SetLanguage(AllLanguages.Keys.First());
            }
        }

        public static string Language => _lan;

        internal static void SetLanguage(string key)
        {
            if (AllLanguages.TryGetValue(key, out var type))
            {
                _lan = key;
                ins = type;
                Prefs.Lan_key = key;
                //EditorPrefs.SetString("ActionEditor_x", key);
            }
        }


        #endregion



    }
}