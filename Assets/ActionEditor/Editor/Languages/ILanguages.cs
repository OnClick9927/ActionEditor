using System;

namespace ActionEditor
{
    interface ILanguages
    {
        string AssetPickListType { get; }

        string Language { get; }
        //**********  Welcome *********
        string Title { get; }
        string CreateAsset { get; }



        //**********  Crate Window *********
        string CrateAssetType { get; }
        string CrateAssetName { get; }
        string CreateAssetFileName { get; }
        string CreateAssetConfirm { get; }
        string CreateAssetConfirmBySelectPath { get; }

        // string CreateAssetReset { get; }
        string CreateAssetTipsNameNull { get; }
        string CreateAssetTipsRepetitive { get; }


        //**********  Preferences Window *********
        string Preferences { get; }
        string StepMode { get; }
        string SnapInterval { get; }
        string FrameRate { get; }
        string MagnetSnapping { get; }

        string SavePath { get; }
        string AutoSaveTime { get; }


        //**********  Commom *********
        string Select { get; }
        string SelectFolder { get; }
        string TipsTitle { get; }
        string TipsConfirm { get; }

        string Disable { get; }
        string Locked { get; }
        string Save { get; }
        string SaveAs {  get; }

        //**********  Header *********
        string HeaderLastSaveTime { get; }
        //string OpenPreferencesTips { get; }
    

        //**********  Group Menu *********
        string MenuAddTrack { get; }
        //string MenuPasteTrack { get; }
        string GroupAdd { get; }

        string ClearCopy { get; }

        string Copy { get; }
        string Cut { get; }
        string Delete { get; }
        string MatchClipLength { get; }

        string Paste { get; }
        //**********  Inspector *********
        string NotSelectAsset { get; }
        string OverflowInvalid { get; }
        string EndTimeOverflowInvalid { get; }
        string StartTimeOverflowInvalid { get; }

        string ClipInvalid { get; }
        string ClearSelect { get; }
        string NoAssetExtendType { get; }

        //string Clip {  get; }
        //string Track { get; }
    }
}