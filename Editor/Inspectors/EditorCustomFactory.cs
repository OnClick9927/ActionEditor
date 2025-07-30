using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionEditor
{
    /*public*/
    static class EditorCustomFactory
    {
        #region Header

        private static bool _initHeadersDic = false;
        private static readonly Dictionary<Type, Type> _headerDic = new Dictionary<Type, Type>();
        private static readonly Dictionary<Type, Type> _footerDic = new Dictionary<Type, Type>();
        private static Dictionary<Type, HeaderFooterBase> head_foot = new Dictionary<Type, HeaderFooterBase>();
        public static HeaderFooterBase GetHeaderFooter(Asset asset, bool header = true)
        {
            if (asset == null) return null;
            if (!_initHeadersDic)
            {
                InitHeaderDic();
                InitFooterDic();
                _initHeadersDic = true;
            }



            var type = asset.GetType();
            var dic = header ? _headerDic : _footerDic;
            if (dic.TryGetValue(type, out var t))
            {

                if (head_foot.TryGetValue(t, out HeaderFooterBase headerFooter))
                    return headerFooter;


                headerFooter = Activator.CreateInstance(t) as HeaderFooterBase;
                head_foot[t] = headerFooter;
                return headerFooter;
            }

            return null;
        }

        static void InitHeaderDic()
        {
            //if (_initHeadersDic) return;

            //_initHeadersDic = true;

            //先获取有绑定关系的所有对象和面板对象映射
            Type type = typeof(HeaderFooterBase);
            var childs = EditorEX.GetTypeMetaDerivedFrom(type);
            foreach (var t in childs)
            {
                var arrs = t.type.GetCustomAttributes(typeof(CustomHeaderAttribute), true);
                foreach (var arr in arrs)
                {
                    if (arr is CustomHeaderAttribute c)
                    {
                        var bindT = c.InspectedType;
                        var iT = t.type;
                        if (!_headerDic.ContainsKey(bindT))
                        {
                            if (!iT.IsAbstract) _headerDic[bindT] = iT;
                        }
                        else
                        {
                            var old = _headerDic[bindT];
                            if (!iT.IsAbstract && iT.IsSubclassOf(old))
                            {
                                _headerDic[bindT] = iT;
                            }
                        }
                    }
                }
            }
        }
        static void InitFooterDic()
        {
            //先获取有绑定关系的所有对象和面板对象映射
            Type type = typeof(HeaderFooterBase);
            var childs = EditorEX.GetTypeMetaDerivedFrom(type);
            foreach (var t in childs)
            {
                var arrs = t.type.GetCustomAttributes(typeof(CustomFooterAttribute), true);
                foreach (var arr in arrs)
                {
                    if (arr is CustomFooterAttribute c)
                    {
                        var bindT = c.InspectedType;
                        var iT = t.type;
                        if (!_footerDic.ContainsKey(bindT))
                        {
                            if (!iT.IsAbstract) _footerDic[bindT] = iT;
                        }
                        else
                        {
                            var old = _headerDic[bindT];
                            if (!iT.IsAbstract && iT.IsSubclassOf(old))
                            {
                                _footerDic[bindT] = iT;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Inspectors

        private static bool _initInspectorsDic = false;

        private static readonly Dictionary<Type, Type> _inspectorsDic = new Dictionary<Type, Type>();

        public static InspectorsBase GetInspector(IAction directable)
        {
            InitInspectorDic();

            var type = directable.GetType();
            InspectorsBase b = null;
            // Debug.Log($"type={type}");
            if (_inspectorsDic.ContainsKey(type))
            {
                var t = _inspectorsDic[type];
                b = Activator.CreateInstance(t) as InspectorsBase;
            }

            if (b == null)
            {
                b = new InspectorsBase();
            }

            b.SetTarget(directable);
            return b;
        }

        public static void InitInspectorDic()
        {
            if (_initInspectorsDic) return;

            _initInspectorsDic = true;

            //先获取有绑定关系的所有对象和面板对象映射
            Type type = typeof(InspectorsBase);
            var childs = EditorEX.GetTypeMetaDerivedFrom(type);
            foreach (var t in childs)
            {
                var arrs = t.type.GetCustomAttributes(typeof(CustomInspectorAttribute), true);
                foreach (var arr in arrs)
                {
                    if (arr is CustomInspectorAttribute c)
                    {
                        var bindT = c.InspectedType;
                        var iT = t.type;
                        if (!_inspectorsDic.ContainsKey(bindT))
                        {
                            if (!iT.IsAbstract) _inspectorsDic[bindT] = iT;
                        }
                        else
                        {
                            var old = _inspectorsDic[bindT];
                            //如果不是抽象类，且是子类就更新
                            if (!iT.IsAbstract && iT.IsSubclassOf(old))
                            {
                                _inspectorsDic[bindT] = iT;
                            }
                        }
                    }
                }
            }

            //找出没有映射关系的对象，并绑定其最近父类的面板对象
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes().Where(t => t.GetInterfaces().Contains(typeof(IDirectable))))
                .ToArray();
            foreach (var t in types)
            {
                if (!t.IsAbstract)
                {
                    var iT = TryAdd(_inspectorsDic, t);
                    if (iT != null)
                    {
                        _inspectorsDic[t] = iT;
                    }
                }
            }
        }

        #endregion

        private static Type TryAdd(Dictionary<Type, Type> dictionary, Type type)
        {
            if (type != null && !dictionary.ContainsKey(type))
            {
                return TryAdd(dictionary, type.BaseType);
            }

            if (type != null && dictionary.ContainsKey(type))
            {
                return dictionary[type];
            }

            return null;
        }
    }
}