using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace ActionEditor.Nodes.BT
{


    [System.Serializable]
    public class TestBlackBorad : Blackboard
    {
        public int value;
    }
    public class TestBT : BT.BTTree
    {
        public override Blackboard blackBoard => _blackboard;
        [Buffer] private TestBlackBorad _blackboard = new TestBlackBorad();
    }
    class BTTestCondition : BTCondition
    {
        protected override bool Condition()
        {
            return Input.GetKeyDown(KeyCode.Space);
        }


    }
    class BTRset : BTWaitTime
    {
        public BTRset() : base(100)
        {
        }
    }
    class BTWork : BTWaitTime
    {
        public BTWork() : base(10)
        {
        }
    }
    class BTWaitTime : BTAction
    {
        protected override void OnAbort()
        {
            Debug.Log($"{GetType()} OnAbort");
        }
        private float time;
        private float end;
        public BTWaitTime(float time)
        {
            this.time = time;
        }

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
        void Start()
        {
            tree = new TestBT();
            tree.root = new BTRoot()
            {


                child = new BTRepeat()
                {
                    child = new BTSelector()
                    {
                        abortType = BTComposite.AbortType.Both,

                        children = new List<BTNode>()
                        {
                            new BTSeuquence()
                            {
                                abortType = BTComposite.AbortType.Both,
                                children = new List<BTNode>()
                                {
                                    //new BTInvert()
                                    //{
                                    //    child=new BTTestCondition()
                                    //},

                                    new BTTestCondition(),
                                    new BTWork()
                                }
                            },
                            new BTRset()
                        }
                    }

                }

            };
            tree.PrepareForRuntime();
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

