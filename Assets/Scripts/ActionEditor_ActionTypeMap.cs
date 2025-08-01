namespace ActionEditor { public class ActionEditor_ActionTypeMap {
private static System.Collections.Generic.Dictionary<string,System.Type> map=new System.Collections.Generic.Dictionary<string,System.Type>(){
{ "ActionEditor.BuffAsset",typeof(ActionEditor.BuffAsset) },
{ "ActionEditor.PlayAnimation",typeof(ActionEditor.PlayAnimation) },
{ "ActionEditor.PlayAudio",typeof(ActionEditor.PlayAudio) },
{ "ActionEditor.PlayParticle",typeof(ActionEditor.PlayParticle) },
{ "ActionEditor.TriggerEvent",typeof(ActionEditor.TriggerEvent) },
{ "ActionEditor.TriggerLog",typeof(ActionEditor.TriggerLog) },
{ "ActionEditor.TriggerShake",typeof(ActionEditor.TriggerShake) },
{ "ActionEditor.VisibleTo",typeof(ActionEditor.VisibleTo) },
{ "ActionEditor.MoveBy",typeof(ActionEditor.MoveBy) },
{ "ActionEditor.MoveTo",typeof(ActionEditor.MoveTo) },
{ "ActionEditor.RotateTo",typeof(ActionEditor.RotateTo) },
{ "ActionEditor.ScaleTo",typeof(ActionEditor.ScaleTo) },
{ "ActionEditor.ActionTrack",typeof(ActionEditor.ActionTrack) },
{ "ActionEditor.TestGroup",typeof(ActionEditor.TestGroup) },
{ "ActionEditor.AnimationTrack",typeof(ActionEditor.AnimationTrack) },
{ "ActionEditor.AudioTrack",typeof(ActionEditor.AudioTrack) },
{ "ActionEditor.EffectTrack",typeof(ActionEditor.EffectTrack) },
{ "ActionEditor.SignalTrack",typeof(ActionEditor.SignalTrack) },
{ "ActionEditor.SkillAsset",typeof(ActionEditor.SkillAsset) },
};
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod] 
#endif
public static void Init() => Asset.GetTypeByTypeName += Asset_GetTypeByTypeName;
private static System.Type Asset_GetTypeByTypeName(string name)
{System.Type type = null;map.TryGetValue(name, out type);return type; }
}}