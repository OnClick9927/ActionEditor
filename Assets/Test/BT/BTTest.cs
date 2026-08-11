using ActionAttribute;
using ActionBuffer;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;
namespace ActionEditor.Nodes.BT
{

    public class TestView : BTTreeView<TestBT>
    {

    }


    public class TestBlackBorad : Blackboard
    {
        public float Money;
        public A a;

        public int b, c, d, e, f;
        public enum A
        {
            a,b,c
        }
    }
    public class TestBT : BT.BTTree
    {
        public override Blackboard blackboard => _blackboard;
        [Buffer] private TestBlackBorad _blackboard = new TestBlackBorad();
    }
    [Attachable(typeof(TestBT)), Node(BTNodeTypes.Action)]

    class BTRset : BTWaitTime
    {
        public override float time => 100;
        protected override State OnUpdate(Blackboard blackboard)
        {
            (blackboard as TestBlackBorad).Money -= Time.deltaTime * 5;
            return base.OnUpdate(blackboard);
        }
    }
    [System.Serializable, Attachable(typeof(TestBT)), Node(BTNodeTypes.Action)]

    class BTWork : BTWaitTime
    {
        public override float time => 2;
        protected override State OnUpdate(Blackboard blackboard)
        {
            (blackboard as TestBlackBorad).Money += Time.deltaTime * 10;
            return base.OnUpdate(blackboard);
        }
    }

    class BTWaitTime : BTAction
    {
        [System.Runtime.InteropServices.StructLayout(
            System.Runtime.InteropServices.LayoutKind.Explicit)]
        private struct FloatStatusValue
        {
            [System.Runtime.InteropServices.FieldOffset(0)] public float Float;
            [System.Runtime.InteropServices.FieldOffset(0)] public int Int;
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            Debug.Log($"{GetType()} OnAbort");
        }
        public virtual float time { get; }
        private float GetEnd(Blackboard blackboard) =>
            new FloatStatusValue { Int = GetRuntimeData(blackboard, 0) }.Float;
        private void SetEnd(Blackboard blackboard, float value) =>
            SetRuntimeData(blackboard, 0,
                new FloatStatusValue { Float = value }.Int);

        protected override int RuntimeDataSize => 1;

        protected override void OnStart(Blackboard blackboard)
        {
            base.OnStart(blackboard);
            SetEnd(blackboard, time + Time.time);
        }



        protected override State OnUpdate(Blackboard blackboard)
        {
            return GetEnd(blackboard) > Time.time
                ? State.Running
                : State.Success;
        }

    }
    public class BTTest : MonoBehaviour
    {
        private BTTree tree;
        private TestBlackBorad runtimeBlackboard;
        public TextAsset txt;
        void Start()
        {
            tree = TestBT.FromBytes(typeof(TestBT), txt.bytes) as TestBT;
            BTTree.loader = path =>
            {
                var txt = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                return TestBT.FromBytes(typeof(BTTree), txt.bytes) as BTTree;
            };
            tree.PrepareForRuntime();
            runtimeBlackboard = new TestBlackBorad();
            runtimeBlackboard.CopyFieldsFrom(tree.blackboard);
            runtimeBlackboard.Initialize(tree);
            BTTree.SetAsInstance(tree, runtimeBlackboard);
        }
        // Update is called once per frame
        void Update()
        {
            if (tree == null) return;
            var result = tree.Update(runtimeBlackboard);
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
            {
                var nextBlackboard = new TestBlackBorad();
                nextBlackboard.Initialize(tree);
                nextBlackboard.CopyFieldsFrom(runtimeBlackboard);
                BTTree.SetAsInstance(tree, nextBlackboard);
                runtimeBlackboard = nextBlackboard;
            }
            //if (result == BTNode.State.Success)
            //{
            //    tree = null;
            //}
        }
    }
}

