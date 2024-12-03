using System.Linq;

namespace ActionEditor
{
    public static class EX
    {
        public static bool CanAddTrack(this Group group, Track track)
        {

            if (track == null) return false;
            var type = track.GetType();
            if (type == null || !type.IsSubclassOf(typeof(Track)) || type.IsAbstract) return false;
            if (type.IsDefined(typeof(UniqueAttribute), true) &&
                group.ExistSameTypeTrack(type))
                return false;
            var attachAtt = type.RTGetAttribute<AttachableAttribute>(true);
            if (attachAtt == null || attachAtt.Types == null || attachAtt.Types.All(t => t != group.GetType())) return false;

            return true;
        }
    }
}