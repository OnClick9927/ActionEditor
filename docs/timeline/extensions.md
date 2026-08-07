# 自定义 Group、Track 与 Clip

## 定义类型

```csharp
[System.Serializable]
[Name("战斗分组")]
public sealed class CombatGroup : Group { }

[System.Serializable]
[Name("伤害轨道")]
[Attachable(typeof(CombatGroup))]
public sealed class DamageTrack : Track { }

[System.Serializable]
[Name("伤害片段")]
[Attachable(typeof(DamageTrack))]
[TypeInfoBox("在片段进入时应用一次确定伤害。")]
public sealed class DamageClip : Clip, IResizeAble
{
    [Name("伤害值", "使用整数以便帧同步。")]
    public int damage;
    public override bool IsValid => damage >= 0;
}
```

`AttachableAttribute` 决定右键菜单中类型能否挂到父级。业务类型需要可构造且可被 ActionBuffer 序列化；运行时字段使用 `[NonSerialized]` 或不参与协议的未标记属性隔离。

## Clip 能力接口

- `IResizeAble`：允许调整长度。
- `IBlendAble`：提供 BlendIn/BlendOut，编辑器显示混合控件。
- `ILengthMatchAble`：提供可匹配的固有长度。
- `ClipSignal`：零长度信号点。

实现 Blend 时应保证 0 <= BlendIn/BlendOut <= Length，且两者不会产生无意义区间。框架的扩展方法会在时间改变后重新校验。

## 自定义 Inspector 视图

```csharp
[CustomActionView(typeof(DamageClip))]
public sealed class DamageClipView : ClipEditorView<DamageClip>
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        // 只追加必须由代码表达的预览控件。
    }

    public override void OnPreviewUpdate(float time, float previousTime)
    {
        // 编辑器预览，不写运行时持久状态。
    }
}
```

`target` 是当前 Group/Track/Clip，`asset` 始终是顶层 Asset。视图解析会缓存类型映射，并沿继承链选择最近的非抽象 CustomActionView。大多数 Inspector 需求优先使用 ActionAttribute；重写时调用 base，保留 Script、TypeInfoBox、有效性和 In/Out 控件。

## 预览生命周期

`OnPreviewEnter` / `OnPreviewExit` 处理正向进入/离开；Reverse 方法处理反向预览；`OnPreviewUpdate(time, previousTime)` 用于连续更新。预览必须可重复进入、可安全退出，不应创建无法回收的场景对象。修改场景时记录并恢复原值，窗口关闭和脚本编译都可能提前结束预览。

## 扩展注意

- 不在 Editor View 持久化资源数据，数据只能落在 Asset/Segment。
- 修改时间或层级后让框架 Validate，不直接改内部 parent/root 缓存。
- 片段运行逻辑与预览逻辑分离，避免 Editor API 进入 Player。
- 新后缀由 Asset 类型声明，不修改全局 `FileEx`。
- 颜色设置按 Asset 类型保存，业务视图不要直接写 ScriptableSingleton。
