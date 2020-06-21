using System.Collections.Generic;
using System.Linq;

namespace VideoDecryption
{
     public class AppSetting
    {
        public string CoursePath { get; set; }
        public string DatabasePath { get; set; }
        public string OutputPath { get; set; }
        public bool Decrypt { get; set; }
        public bool CreateSub { get; set; }
        public bool DeleteAfterDecrypt { get; set; }
        public bool ClipIndexAtOne { get; set; }
        public bool ModuleIndexAtOne { get; set; }
        public bool ShowErrorOnly { get; set; }
        public bool CopyImage { get; set; }

        public AppSetting()
        {
            CoursePath = string.Empty;
            DatabasePath = string.Empty;
            OutputPath = string.Empty;
            Decrypt = true;
            CreateSub = true;
            DeleteAfterDecrypt = true;
            ClipIndexAtOne = false;
            ModuleIndexAtOne = false;
            ShowErrorOnly = false;
            CopyImage = false;
        }
    }

    public class CourseItem
    {
        public Course Course { get; set; }
        public string CoursePath { get; set; }
    }
    public class Course
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public bool HasTranscript { get; set; }
        public List<Module> Modules { get; set; }

        public bool IsDownloaded { get { return !Modules.Any(md => !md.IsDownloaded); } }

        public Course()
        {
            Modules = new List<Module>();
        }
    }


    public class Module
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string Title { get; set; }
        public string AuthorHandle { get; set; }
        public int Index { get; set; }
        public List<Clip> Clips { get; set; }

        public bool IsDownloaded { get { return !Clips.Any(clp => !clp.IsDownloaded); } }

        public Module()
        {
            Clips = new List<Clip>();
        }
    }
    public class Clip
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public int Id { get; set; }
        public int Index { get; set; }
        public bool IsDownloaded { get; set; }
        public List<ClipTranscript> Subtitle { get; set; }

        public Clip()
        {
            Subtitle = new List<ClipTranscript>();
        }
    }
    public class ClipTranscript
    {
        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public string Text { get; set; }
    }

}



