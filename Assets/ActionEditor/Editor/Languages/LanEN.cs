namespace ActionEditor
{
    [Name("English")]
    public class LanEN : ILanguages
    {

        //**********  Welcome *********
        public string Title => "Action Editor";
        public string CreateAsset => "Create Asset";
        public string SelectAsset => "Select Asset";
        public string Seeting => "Preferences";

        //**********  Crate Window *********
        public string CrateAssetType => "Create Type";
        public string CrateAssetName => "Asset Name";
        public string CreateAssetFileName => "Asset File Name";
        public string CreateAssetConfirm => "Create";
        public string CreateAssetReset => "Reset";
        public string CreateAssetTipsNameNull => "Name cannot be empty!";
        public string CreateAssetTipsRepetitive => "Duplicate name!";

        //**********  Preferences Window *********
        public string PreferencesTitle => "Editor Preferences";
        public string PreferencesTimeStepMode => "Time Step Mode";
        public string PreferencesSnapInterval => "Snap Interval";
        public string PreferencesFrameRate => "Frame Rate";
        public string PreferencesMagnetSnapping => "Magnet Snapping";

        public string PreferencesMagnetSnappingTips =>
            "Turn on other clips before and after the clip is automatically attached";

        public string PreferencesScrollWheelZooms => "Scroll Wheel Zooms";
        public string PreferencesScrollWheelZoomsTips => "Turn on the scroll wheel to zoom the timeline area";
        public string PreferencesSavePath => "Asset save path";
        public string PreferencesSavePathTips => "Default path on creation and selection";
        public string PreferencesAutoSaveTime => "auto save time";
        public string PreferencesAutoSaveTimeTips => "Auto save interval";
        public string PreferencesHelpDoc => "Help doc";


        //**********  Commom *********
        public string Select => "Select";
        public string SelectFile => "Select File";
        public string SelectFolder => "Select Folder";
        public string TipsTitle => "Tips";
        public string TipsConfirm => "Confirm";
        public string TipsCancel => "Cancel";
        public string CompilingTips => "Compiling\n... please wait...";
        public string Disable => "Disable";
        public string Locked => "Locked";
        public string Save => "Save";
        public string Rename => "Rename";

        //**********  Header *********
        public string HeaderLastSaveTime => "Last save time：{0}";
        public string HeaderSelectAsset => "Select：[{0}]";
        public string OpenPreferencesTips => "Open Preferences";
        public string SelectAssetTips => "Select Asset";
        public string OpenMagnetSnappingTips => "Open Magnet Snapping";
        public string NewAssetTips => "New Asset";
        public string BackMenuTips => "Back menu";
        public string PlayLoopTips => "Loop Play";
        public string PlayForwardTips => "Jump to the end";
        public string StepForwardTips => "Jump to next frame";
        public string PauseTips => "Pause";
        public string PlayTips => "Play";
        public string StopTips => "Stop";
        public string StepBackwardTips => "Jump to previous frame";

        //**********  Group Menu *********
        public string MenuAddTrack => "Add Track";
        public string MenuPasteTrack => "Paste Track";
        public string GroupAdd => "Add Group";
        public string GroupDisable => "Disable Group";
        public string GroupLocked => "Locked Group";
        public string GroupReplica => "Replica Group";
        public string GroupDelete => "Delete Group";
        public string GroupDeleteTips => "confirm delete group?";

        //**********  Track Menu *********
        public string TrackDisable => "Disable Track";
        public string TrackLocked => "Locked Track";
        public string TrackCopy => "Copy Track";
        public string TrackCut => "Cut Track";
        public string TrackDelete => "Delete Track";
        public string TrackDeleteTips => "confirm delete track?";
        public string FirstFrame => "jump to FirstFrame";


        //**********  Clip Menu *********
        public string ClipCopy => "Copy";
        public string ClipCut => "Cut";
        public string ClipDelete => "Delete";
        public string MatchClipLength => "Match Clip Length";
        public string MatchPreviousLoop => "Match Previous Loop";
        public string MatchNextLoop => "Match Next Loop";
        public string ClipPaste => "Paste ({0})";

        //**********  Inspector *********
        public string NotSelectAsset => "not selected asset。";
        public string InsBaseInfo => "{0} base info";
        public string OverflowInvalid => "Clip is outside of playable range";
        public string EndTimeOverflowInvalid => "Clip end time is outside of playable range";
        public string StartTimeOverflowInvalid => "Clip start time is outside of playable range";

        public string ClipInvalid => "Clip  Invalid,check params";

        public string ClearCopy => "Clear Cut or Copy";

        public string EmptyRect => "Empty Rect\nclick to select none";
    }
}