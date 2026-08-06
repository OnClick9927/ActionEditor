namespace ActionEditor.Nodes.BT
{
    internal interface IBTEventReceiver
    {
        string EventName { get; }
        void ReceiveEvent();
    }
}
