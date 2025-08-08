
namespace ActionEditor
{
    interface IDirectableTimePointer
    {
        //ActonEditorBase target { get; }
        //float time { get; }
        void TriggerForward(float currentTime, float previousTime);
        void TriggerBackward(float currentTime, float previousTime);
        void Update(float currentTime, float previousTime);
    }

    struct StartTimePointer : IDirectableTimePointer
    {
        private bool triggered;
        private float lastTargetStartTime;
        public ActonEditorView target { get; private set; }
        //float IDirectableTimePointer.time => directable.StartTime;
        private ActionEditor.IDirectable directable => target.target as IDirectable;
        public StartTimePointer(ActonEditorView target)
        {
            this.target = target;
            triggered = false;
            lastTargetStartTime = (target.target as IDirectable).StartTime;
        }

        void IDirectableTimePointer.TriggerForward(float currentTime, float previousTime)
        {
            if (!directable.IsActive) return;
            if (currentTime >= directable.StartTime)
            {
                if (!triggered)
                {
                    triggered = true;
                    target.OnPreviewEnter();
                    target.OnPreviewUpdate(directable.ToLocalTime(currentTime), 0);
                }
            }
        }

        void IDirectableTimePointer.Update(float currentTime, float previousTime)
        {
            if (!directable.IsActive) return;
            if (currentTime >= directable.StartTime && currentTime < directable.EndTime &&
                currentTime > 0)
            {
                var deltaMoveClip = directable.StartTime - lastTargetStartTime;
                var localCurrentTime = directable.ToLocalTime(currentTime);
                var localPreviousTime = directable.ToLocalTime(previousTime + deltaMoveClip);

                target.OnPreviewUpdate(localCurrentTime, localPreviousTime);
                lastTargetStartTime = directable.StartTime;
            }
        }

        void IDirectableTimePointer.TriggerBackward(float currentTime, float previousTime)
        {
            if (!directable.IsActive) return;
            if (currentTime < directable.StartTime || currentTime <= 0)
            {
                if (triggered)
                {
                    triggered = false;
                    target.OnPreviewUpdate(0, directable.ToLocalTime(previousTime));
                    target.OnPreviewReverse();
                }
            }
        }
    }

    struct EndTimePointer : IDirectableTimePointer
    {
        private bool triggered;
        public ActonEditorView target { get; private set; }
        //float IDirectableTimePointer.time => directable.EndTime;
        private ActionEditor.IDirectable directable => target.target as IDirectable;

        public EndTimePointer(ActonEditorView target)
        {
            this.target = target;
            triggered = false;
        }

        void IDirectableTimePointer.TriggerForward(float currentTime, float previousTime)
        {
            if (!directable.IsActive) return;
            if (currentTime >= directable.EndTime)
            {
                if (!triggered)
                {
                    triggered = true;
                    target.OnPreviewUpdate(directable.Length, directable.ToLocalTime(previousTime));
                    target.OnPreviewExit();
                }
            }
        }


        void IDirectableTimePointer.Update(float currentTime, float previousTime)
        {

        }


        void IDirectableTimePointer.TriggerBackward(float currentTime, float previousTime)
        {
            if (!directable.IsActive) return;
            if (currentTime < directable.EndTime || currentTime <= 0)
            {
                if (triggered)
                {
                    triggered = false;
                    target.OnPreviewReverseEnter();
                    target.OnPreviewUpdate(directable.ToLocalTime(currentTime), directable.Length);
                }
            }
        }
    }
}