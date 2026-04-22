using System;
using System.Collections.Generic;
using UnityEngine;
namespace ActionEditor.Nodes.BT
{

    public class TestView : BTTreeView<TestBT>
    {

    }


    public class TestBlackBorad : Blackboard
    {
        public float Money;
    }
    public class TestBT : BT.BTTree
    {
        public override Blackboard blackBoard => _blackboard;
        [Buffer] private TestBlackBorad _blackboard = new TestBlackBorad();
    }
    [Attachable(typeof(TestBT)), Node(BTNodeTypes.Condition), Name("≤‚ ‘Ãıº˛")]
    class BTTestCondition : BTCondition
    {
        protected override bool Condition()
        {
            return (blackBoard as TestBlackBorad).Money < 50;
        }


    }
    [Attachable(typeof(TestBT)), Node(BTNodeTypes.Action)]

    class BTRset : BTWaitTime
    {

        protected override State OnUpdate()
        {
            (blackBoard as TestBlackBorad).Money -= Time.deltaTime * 5;
            return base.OnUpdate();
        }
    }
    [System.Serializable, Attachable(typeof(TestBT)), Node(BTNodeTypes.Action)]

    class BTWork : BTWaitTime
    {

        protected override State OnUpdate()
        {
            (blackBoard as TestBlackBorad).Money += Time.deltaTime * 10;
            return base.OnUpdate();
        }
    }
   
    class BTWaitTime : BTAction
    {
        protected override void OnAbort()
        {
            Debug.Log($"{GetType()} OnAbort");
        }
        public float time = 10;
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
            tree.PrepareForRuntime();
            tree.SetAsInstance();
        }
        // Update is called once per frame
        void Update()
        {
            if (tree == null) return;
            var result = tree.Update();
            if (result == BTNode.State.Success)
            {
                tree = null;
            }
        }
    }
}

