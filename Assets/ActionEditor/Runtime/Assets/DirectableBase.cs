using System.Collections.Generic;


namespace ActionEditor
{
    public abstract class DirectableBase : IDirectable
    {
        //[SerializeField][HideInInspector] protected bool isLocked;
        //[SerializeField][HideInInspector] protected bool active = true;
        public abstract bool IsLocked { get;set; }
        public abstract bool IsActive { get; set; }
        public abstract float Length { get; set; }




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

        //public abstract bool IsCollapsed { get; set; }

        public abstract float StartTime { get; set; }

        public abstract float EndTime { get; set; }

    }
}