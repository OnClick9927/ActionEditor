using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.IMGUI.Controls;

namespace ActionEditor.Nodes.BT.Tests
{
    public sealed class BehaviorTreeEditorTests
    {
        private sealed class EmptyBlackboard : Blackboard { }

        private sealed class EmptyTree : BTTree
        {
            private readonly Blackboard value = new EmptyBlackboard();
            public override Blackboard blackboard => value;
        }

        [Test]
        public void EmptyNodeHierarchyReloadsWithoutException()
        {
            Type openViewType = Type.GetType(
                "ActionEditor.Nodes.BT.BTTreeView`1, " +
                "ActionEditor.Nodes.BT.Editor");
            Assert.That(openViewType, Is.Not.Null);

            Type viewType = openViewType.MakeGenericType(typeof(EmptyTree));
            object owner = Activator.CreateInstance(viewType);
            Type nodeTreeType = FindNodeTreeType(viewType);
            if (nodeTreeType.ContainsGenericParameters)
                nodeTreeType = nodeTreeType.MakeGenericType(typeof(EmptyTree));

            object nodeTree = Activator.CreateInstance(nodeTreeType,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic, null,
                new object[] { new TreeViewState(), owner }, null);
            MethodInfo buildRoot = nodeTreeType.GetMethod("BuildRoot",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var root = (TreeViewItem)buildRoot.Invoke(nodeTree, null);

            Assert.That(root.children, Is.Not.Null.And.Empty);
            MethodInfo reload = nodeTreeType.GetMethod("Reload",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.DoesNotThrow(() => reload.Invoke(nodeTree, null));
        }

        private static Type FindNodeTreeType(Type viewType)
        {
            Type[] nestedTypes = viewType.GetNestedTypes(
                BindingFlags.NonPublic);
            for (int i = 0; i < nestedTypes.Length; i++)
            {
                if (nestedTypes[i].Name.StartsWith("NodeTreeIMGUI",
                    StringComparison.Ordinal)) return nestedTypes[i];
            }
            Assert.Fail("找不到行为树层级控件类型。");
            return null;
        }
    }
}
