using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace ActionEditor
{
    class Lan
    {
        #region 静态

        public static readonly Dictionary<string, ILanguages> AllLanguages = new Dictionary<string, ILanguages>();
        private static string _lan;
        public static ILanguages ins;
        internal static void Load()
        {
            var lan = EditorPrefs.GetString("ActionEditor_x", string.Empty);
            var types = Tools.GetImplementationsOf(typeof(ILanguages));
            //ins = new LanEN();
            foreach (var t in types)
            {
                var nameAtt = (NameAttribute)t.GetCustomAttributes(typeof(NameAttribute), false).FirstOrDefault();
                var name = nameAtt != null ? nameAtt.name : t.Name;
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
                EditorPrefs.SetString("ActionEditor_x", key);
            }
        }


        #endregion



    }
}