using System.Collections.Generic;


namespace ActionEditor
{
    public abstract class SegmentBase : ISegment
    {
        public abstract bool IsLocked { get;set; }
        public abstract bool IsActive { get; set; }
        public abstract float Length { get; set; }




        private Asset root;
        private ISegment parent;

        public Asset Root => root;

        public ISegment Parent => parent;


        internal virtual void Validate(Asset root, ISegment parent)
        {
            this.root = root;
            this.parent = parent;
        }



        public virtual IEnumerable<ISegment> Children { get; }


        public abstract float StartTime { get; set; }

        public abstract float EndTime { get; set; }

     
    }
}