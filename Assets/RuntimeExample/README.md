# 说明
本目录是Runtime使用演示目录，如果只需要Editor编辑Asset资产，则无需关注本目录内容

## 为何Clip的编辑器预览和Runtime逻辑不共用
考虑到编辑器预览其实是初步预览，很多情况下，其实是和Runtime下还是存在一定差异。

然后Editor下可以很轻松实现上一帧,上N帧等任意帧播放。而Runtime下很难实现这点
Editor下比如资源处理可以很简单粗暴来处理，Runtime下自己业务可能存在资源预加载等逻辑。

同时Editor下的预览和Runtime下的播放逻辑是可以自己抽象起来，共用一份代码，只有部分设计资源等内容分开即可。

## 为何不在Clip中直接写逻辑

在我设计中，时间轴编辑出来的是表现的配置，其内部只有一些表现逻辑，不存在具体业务的相关内容。

以技能为例，编辑出来的Clip只是技能的一部分，并不是技能的全部，分清楚主次关系很重要。

同时，如果我们在`PlayParticle`这个Clip中写一些runtime逻辑。比如实例化粒子或者播放粒子。

那么如果我们客户端希望要数据层和表现层是分离的。数据层只做数据判断处理，表现层只做表现。

那我们的clip存在逻辑，这就是典型的职责划分不清晰，不符合单一功能职责化的原则。

基于这种设计理念，我们就把ActionEditor编辑出来的Asset设定为单纯的是表现的配置，可以认为是配置表的一部分。

如此职责分明，虽然脚本数量会增加，但是后续维护成本却是很低的。也方便多人协作。

如果项目是比较重度的，一个人同时维护编辑器，表现层，数据层。那么是很困难的，分开也方便由不同人维护，同时出现问题，也更容易定位是那个部分有问题。