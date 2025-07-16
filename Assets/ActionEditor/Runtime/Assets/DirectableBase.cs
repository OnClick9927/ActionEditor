using System.Collections.Generic;
//using FullSerializer;
using UnityEngine;

namespace ActionEditor
{
    public abstract class DirectableBase : IDirectable
    {
        [SerializeField][HideInInspector] protected bool isLocked;
        [SerializeField][HideInInspector] protected bool active = true;

        public abstract float Length { get; set; }

        public virtual bool IsLocked
        {
            get => Parent != null && (Parent.IsLocked || isLocked);
            set => isLocked = value;
        }
        public virtual bool IsActive
        {
            get => Parent != null && Parent.IsActive && active;
            set
            {
                if (active != value)
                {
                    active = value;
                    if (Root != null) Root.Validate();
                }
            }
        }


        private Asset root;
        private IDirectable parent;

        public Asset Root => root;

        public IDirectable Parent => parent;


        void IDirectable.AfterDeserialize() => OnAfterDeserialize();

        void IDirectable.BeforeSerialize() => OnBeforeSerialize();

        public virtual void Validate(Asset root, IDirectable parent)
        {
            this.root = root;
            this.parent = parent;
        }
        protected virtual void OnAfterDeserialize() { }
        protected virtual void OnBeforeSerialize() { }


        public virtual IEnumerable<IDirectable> Children { get; }

        public abstract bool IsCollapsed { get; set; }

        public abstract float StartTime { get; set; }

        public abstract float EndTime { get; set; }

    }
}