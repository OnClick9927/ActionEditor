using ActionBuffer;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ActionEditor.Nodes.BT
{

    public class TestView : BTTreeView<TestBT>
    {

    }


    public class TestBlackBorad : Blackboard
    {
        public float Money;
        public A a;
        public enum A
        {
            a,b,c
        }
    }
    public class TestBT : BT.BTTree
    {
        protected override Blackboard blackboard => _blackboard;
        [Buffer] private TestBlackBorad _blackboard = new TestBlackBorad();
    }
    [Attachable(typeof(TestBT)), Node(BTNodeTypes.Action)]

    class BTRset : BTWaitTime
    {
        public override float time => 100;
        protected override State OnUpdate()
        {
            (blackboard as TestBlackBorad).Money -= Time.deltaTime * 5;
            return base.OnUpdate();
        }
    }
    [System.Serializable, Attachable(typeof(TestBT)), Node(BTNodeTypes.Action)]

    class BTWork : BTWaitTime
    {
        public override float time => 2;
        protected override State OnUpdate()
        {
            (blackboard as TestBlackBorad).Money += Time.deltaTime * 10;
            return base.OnUpdate();
        }
    }

    class BTWaitTime : BTAction
    {
        protected override void OnAbort()
        {
            Debug.Log($"{GetType()} OnAbort");
        }
        public virtual float time { get; }
        private float end;


        protected override void OnStart()
        {
            base.OnStart();
            end = time + Time.time;
        }



        protected override State OnUpdate()
        {
            return end > Time.time ? State.Running : State.Success;
        }
    }
    public class BTTest : MonoBehaviour
    {
        private BTTree tree;
        public TextAsset txt;
        void Start()
        {
            tree = TestBT.FromBytes(typeof(TestBT), txt.bytes) as TestBT;
            tree.PrepareForRuntime((path) =>
            {
                var txt = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                return TestBT.FromBytes(typeof(BTTree), txt.bytes) as BTTree;
            });
            tree.SetAsInstance();
        }
        // Update is called once per frame
        void Update()
        {
            if (tree == null) return;
            var result = tree.Update();

            //if (result == BTNode.State.Success)
            //{
            //    tree = null;
            //}
        }
    }
}

