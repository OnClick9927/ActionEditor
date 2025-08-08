namespace ActionEditor
{
    [Name("English")]
    class LanEN : ILanguages
    {
        public string Language => "Language";

        public string AssetPickListType => "AssetPickListType";


        //**********  Welcome *********
        string ILanguages.Title => "Action Editor";
        string ILanguages.CreateAsset => "New";

        //**********  Crate Window *********
        string ILanguages.CrateAssetType => "Create Type";
        string ILanguages.CrateAssetName => "Asset Name";
        string ILanguages.CreateAssetFileName => "Asset File Name";
        string ILanguages.CreateAssetConfirm => "Create by default Path";
        string ILanguages.CreateAssetConfirmBySelectPath => "Create by select Path";

        string ILanguages.CreateAssetTipsNameNull => "Name cannot be empty!";
        string ILanguages.CreateAssetTipsRepetitive => "Duplicate name!";

        //**********  Preferences Window *********
        string ILanguages.Preferences => "Preferences";
        string ILanguages.StepMode => "Time Step Mode";
        string ILanguages.SnapInterval => "Snap Interval";
        string ILanguages.FrameRate => "Frame Rate";
        string ILanguages.MagnetSnapping => "Magnet Snapping";


        //public string PreferencesScrollWheelZoomsTips => "Turn on the scroll wheel to zoom the timeline area";
        string ILanguages.SavePath => "Asset save path";
        string ILanguages.AutoSaveTime => "auto save time";


        //**********  Commom *********
        string ILanguages.Select => "Select";
        //public string SelectFile => "Select File";
        string ILanguages.SelectFolder => "Select Folder";
        string ILanguages.TipsTitle => "Tips";
        string ILanguages.TipsConfirm => "Confirm";

        string ILanguages.Disable => "Disable";
        string ILanguages.Locked => "Locked";
        string ILanguages.Save => "Save";

        //**********  Header *********
        string ILanguages.HeaderLastSaveTime => "Last save time";

        //**********  Group Menu *********
        string ILanguages.MenuAddTrack => "Add Track";
        string ILanguages.GroupAdd => "Add Group";

        string ILanguages.Copy => "Copy";
        string ILanguages.Cut => "Cut";
        string ILanguages.Delete => "Delete";
        string ILanguages.MatchClipLength => "Match Clip Length";
        string ILanguages.Paste => "Paste ({0})";

        //**********  Inspector *********
        string ILanguages.NotSelectAsset => "not selected asset。";
        string ILanguages.OverflowInvalid => "Clip is outside of playable range";
        string ILanguages.EndTimeOverflowInvalid => "Clip end time is outside of playable range";
        string ILanguages.StartTimeOverflowInvalid => "Clip start time is outside of playable range";

        string ILanguages.ClipInvalid => "Clip  Invalid,check params";

        string ILanguages.ClearCopy => "Clear Copy/Cut";

        string ILanguages.ClearSelect => "select none";

        string ILanguages.NoAssetExtendType => "None Type Sub Class of Asset";


    }
}