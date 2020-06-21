using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.IO;

namespace VideoDecryption
{

    [Cmdlet(VerbsLifecycle.Invoke, "Decryption")]
    public class InvokeDecryption : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            HelpMessage = "course ID"
            )]
        public CourseItem courseitem;

        [Parameter(
            Mandatory = true,
            Position = 1,
            HelpMessage = "Destination folder"
            )]
        public string outputPath;

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            VideoDecryption dv = new VideoDecryption();
            dv.DecryptCourse(courseitem, outputPath);

            WriteObject(
                "Decryption complete."
                );


        }
    }

    [Cmdlet(VerbsCommon.Get, "Courses")]
    public class GetCourses : PSCmdlet
    {
        public static string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Pluralsight";

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var dbPath = File.Exists(folderPath + @"\pluralsight.db") ? folderPath + @"\pluralsight.db" : "";
            var coursePath = Directory.Exists(folderPath + @"\courses") ? folderPath + @"\courses" : "";

            VideoDecryption dv = new VideoDecryption();
            WriteObject(
                dv.GetCourses(dbPath, coursePath)
                );

        }


    }
}
