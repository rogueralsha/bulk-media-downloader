﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BulkMediaDownloader.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "14.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Users\\sanmadjack.FORTAWESOME\\Desktop\\TEST")]
        public string DownloadDir {
            get {
                return ((string)(this["DownloadDir"]));
            }
            set {
                this["DownloadDir"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int MaxConcurrentDownloads {
            get {
                return ((int)(this["MaxConcurrentDownloads"]));
            }
            set {
                this["MaxConcurrentDownloads"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool DetectAdditionalPages {
            get {
                return ((bool)(this["DetectAdditionalPages"]));
            }
            set {
                this["DetectAdditionalPages"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Users\\sanmadjack.FORTAWESOME\\Desktop\\TEST")]
        public string LastDownloadDir {
            get {
                return ((string)(this["LastDownloadDir"]));
            }
            set {
                this["LastDownloadDir"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public global::BulkMediaDownloader.Download.DownloadManager Downloads {
            get {
                return ((global::BulkMediaDownloader.Download.DownloadManager)(this["Downloads"]));
            }
            set {
                this["Downloads"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>jpg</string>\r\n  <string>jpeg</string>\r\n  <string>gif</string>\r\n  <string>p" +
            "ng</string>\r\n  <string>webm</string>\r\n  <string>3gp</string>\r\n  <string>aa</stri" +
            "ng>\r\n  <string>aac</string>\r\n  <string>aax</string>\r\n  <string>act</string>\r\n  <" +
            "string>ai</string>\r\n  <string>aiff</string>\r\n  <string>amr</string>\r\n  <string>a" +
            "pe</string>\r\n  <string>asf</string>\r\n  <string>au</string>\r\n  <string>avi</strin" +
            "g>\r\n  <string>bmp</string>\r\n  <string>exif</string>\r\n  <string>f4a</string>\r\n  <" +
            "string>f4b</string>\r\n  <string>f4p</string>\r\n  <string>f4v</string>\r\n  <string>f" +
            "lac</string>\r\n  <string>flv</string>\r\n  <string>gifv</string>\r\n  <string>gsm</st" +
            "ring>\r\n  <string>m2v</string>\r\n  <string>m4p</string>\r\n  <string>m4v</string>\r\n " +
            " <string>mkv</string>\r\n  <string>mng</string>\r\n  <string>mov</string>\r\n  <string" +
            ">mp2</string>\r\n  <string>mp3</string>\r\n  <string>mp4</string>\r\n  <string>mpe</st" +
            "ring>\r\n  <string>mpeg</string>\r\n  <string>mpg</string>\r\n  <string>mpv</string>\r\n" +
            "  <string>oga</string>\r\n  <string>ogg</string>\r\n  <string>ogv</string>\r\n  <strin" +
            "g>opus</string>\r\n  <string>pbm</string>\r\n  <string>pgm</string>\r\n  <string>pnm</" +
            "string>\r\n  <string>ppm</string>\r\n  <string>psd</string>\r\n  <string>qt</string>\r\n" +
            "  <string>ra</string>\r\n  <string>raw</string>\r\n  <string>rm</string>\r\n  <string>" +
            "rmvb</string>\r\n  <string>svg</string>\r\n  <string>tiff</string>\r\n  <string>vob</s" +
            "tring>\r\n  <string>vox</string>\r\n  <string>wav</string>\r\n  <string>webp</string>\r" +
            "\n  <string>wma</string>\r\n  <string>wmv</string>\r\n</ArrayOfString>")]
        public global::System.Collections.Specialized.StringCollection MediaExtensions {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["MediaExtensions"]));
            }
            set {
                this["MediaExtensions"] = value;
            }
        }
    }
}
