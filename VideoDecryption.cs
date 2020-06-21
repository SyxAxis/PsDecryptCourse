using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VideoDecryption
{
    class VideoDecryption
    {

        private static List<CourseItem> _listCourse;
        private SQLiteConnection _databaseConnection;
        private static readonly char[] _invalidFileCharacters = Path.GetInvalidFileNameChars();
        private AppSetting _appSetting;


        public List<CourseItem> GetCourses(string dbPath, string coursePath)
        {
            _appSetting = new AppSetting
            {
                CoursePath = coursePath,
                DatabasePath = dbPath
            };

            if (File.Exists(dbPath))
            {
                if (Path.GetExtension(dbPath).Equals(".db"))
                {
                    _databaseConnection = new SQLiteConnection($"Data Source={dbPath}; Version=3;FailIfMissing=True");
                    _databaseConnection.Open();

                    List<string> folderList = Directory.GetDirectories(coursePath, "*", SearchOption.TopDirectoryOnly).ToList();
                    folderList = folderList.Where(r => Directory.GetDirectories(r, "*", SearchOption.TopDirectoryOnly).Length > 0).ToList();
                    _listCourse = folderList.Select(r => new CourseItem() { CoursePath = r, Course = this.GetCourseFromDb(r) }).Where(r => r.Course != null).OrderBy(r => r.Course.Title).ToList();
                    _listCourse = _listCourse.Where(c => c.Course.IsDownloaded).ToList();
                }
            }

            return _listCourse;

        }

        public void DecryptCourse(CourseItem courseItem, string outputPath)
        {


            //Create new course path with the output path
            var newCoursePath = Path.Combine(outputPath, CleanName(courseItem.Course.Title));

            DirectoryInfo courseInfo = Directory.Exists(newCoursePath)
                ? new DirectoryInfo(newCoursePath)
                : Directory.CreateDirectory(newCoursePath);

            //Get list all modules in current course
            List<Module> listModules = courseItem.Course.Modules;

            if (listModules.Count > 0)
            {
                // integer to add 1 if index should start at 1
                int startAt1 = 1;
                //Get each module
                foreach (Module module in listModules)
                {
                    //Generate module hash name
                    string moduleHash = ModuleHash(module.Name, module.AuthorHandle);
                    //Generate module path
                    string moduleHashPath = Path.Combine(courseItem.CoursePath, moduleHash);
                    //Create new module path with decryption name
                    string newModulePath = Path.Combine(courseInfo.FullName, $"{(startAt1 + module.Index):00}. {module.Title}");

                    if (Directory.Exists(moduleHashPath))
                    {
                        DirectoryInfo moduleInfo = Directory.Exists(newModulePath)
                            ? new DirectoryInfo(newModulePath)
                            : Directory.CreateDirectory(newModulePath);
                        //Decrypt all videos in current module folder
                        DecryptAllVideos(moduleHashPath, module, moduleInfo.FullName);
                    }
                }
            }

        }

        // ===================================================================================================

        public Course GetCourseFromDb(string folderCoursePath)
        {
            Course course = null;

            string courseName = GetFolderName(folderCoursePath, true).Trim().ToLower();

            var cmd = _databaseConnection.CreateCommand();
            cmd.CommandText = @"SELECT Name, Title, HasTranscript 
                                FROM Course 
                                WHERE Name = @courseName";
            cmd.Parameters.Add(new SQLiteParameter("@courseName", courseName));

            var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                course = new Course
                {
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Title = CleanName(reader.GetString(reader.GetOrdinal("Title"))),
                    HasTranscript = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("HasTranscript")))
                };

                course.Modules = GetModulesFromDb(course.Name);
            }

            reader.Close();

            return course;
        }

        public string GetFolderName(string folderPath, bool checkExisted = false)
        {
            if (checkExisted)
            {
                if (Directory.Exists(folderPath))
                {
                    return folderPath.Substring(folderPath.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
                }
                throw new DirectoryNotFoundException();
            }
            return folderPath.Substring(folderPath.LastIndexOf(@"\", StringComparison.Ordinal) + 1);
        }

        public static string CleanName(string path)
        {
            foreach (var invalidChar in _invalidFileCharacters)
                path = path.Replace(invalidChar, '-');

            return path.Trim();
        }

        public List<Module> GetModulesFromDb(string courseName)
        {
            List<Module> list = new List<Module>();

            var cmd = _databaseConnection.CreateCommand();
            cmd.CommandText = @"SELECT Id, Name, Title, AuthorHandle, ModuleIndex
                                FROM Module 
                                WHERE CourseName = @courseName";
            cmd.Parameters.Add(new SQLiteParameter("@courseName", courseName));

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Module module = new Module
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    AuthorHandle = reader.GetString(reader.GetOrdinal("AuthorHandle")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Title = CleanName(reader.GetString(reader.GetOrdinal("Title"))),
                    Index = reader.GetInt32(reader.GetOrdinal("ModuleIndex"))
                };

                module.Clips = GetClipsFromDb(module.Id, ModuleHash(module.Name, module.AuthorHandle), courseName);
                list.Add(module);
            }
            reader.Close();
            return list;
        }

        public List<Clip> GetClipsFromDb(int moduleId, string moduleName, string courseName)
        {
            List<Clip> list = new List<Clip>();

            var cmd = _databaseConnection.CreateCommand();
            cmd.CommandText = @"SELECT Id, Name, Title, ClipIndex
                                FROM Clip 
                                WHERE ModuleId = @moduleId";
            cmd.Parameters.Add(new SQLiteParameter("@moduleId", moduleId));

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                Clip clip = new Clip
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Title = CleanName(reader.GetString(reader.GetOrdinal("Title"))),
                    Index = reader.GetInt32(reader.GetOrdinal("ClipIndex")),
                    Subtitle = GetTranscriptFromDb(reader.GetInt32(reader.GetOrdinal("Id")))
                };

                clip.IsDownloaded = File.Exists($@"{_appSetting.CoursePath}\{courseName}\{moduleName}\{clip.Name}.psv");
                list.Add(clip);
            }
            reader.Close();
            return list;
        }

        public static string ModuleHash(string moduleName, string moduleAuthorName)
        {
            string s = moduleName + "|" + moduleAuthorName;
            MD5 md5 = MD5.Create();
            return Convert.ToBase64String(md5.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace('/', '_');
        }

        public List<ClipTranscript> GetTranscriptFromDb(int clipId)
        {
            try
            {
                List<ClipTranscript> list = new List<ClipTranscript>();

                var cmd = _databaseConnection.CreateCommand();
                cmd.CommandText = @"SELECT StartTime, EndTime, Text
                                FROM ClipTranscript
                                WHERE ClipId = @clipId
                                ORDER BY Id ASC";
                cmd.Parameters.Add(new SQLiteParameter("@clipId", clipId));

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ClipTranscript clipTranscript = new ClipTranscript
                    {
                        StartTime = reader.GetInt32(reader.GetOrdinal("StartTime")),
                        EndTime = reader.GetInt32(reader.GetOrdinal("EndTime")),
                        Text = reader.GetString(reader.GetOrdinal("Text"))
                    };
                    list.Add(clipTranscript);
                }

                return list;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new List<ClipTranscript>();
            }
        }

        public static void DecryptAllVideos(string folderPath, Module module, string outputPath)
        {
            try
            {
                // Get all clips of this module from database
                List<Clip> listClips = module.Clips;

                if (listClips.Count > 0)
                {
                    // integer to add 1 if index should start at 1
                    // int startAt1 = Convert.ToInt16(chkStartClipIndexAt1.Checked);
                    int startAt1 = 1;
                    foreach (Clip clip in listClips)
                    {
                        // Get current path of the encrypted video
                        string currentPath = Path.Combine(folderPath, $"{clip.Name}.psv");
                        if (File.Exists(currentPath))
                        {
                            // Create new path with output folder
                            string newPath = Path.Combine(outputPath, $"{(startAt1 + clip.Index):00}. {clip.Title}.mp4");

                            // Init video and get it from iStream
                            var playingFileStream = new VirtualFileStream(currentPath);
                            playingFileStream.Clone(out IStream iStream);

                            string fileName = Path.GetFileName(currentPath);

                            Console.WriteLine("Decrypting : [{0}] {1}.{2}", Path.GetFileName(Path.GetDirectoryName(newPath)), clip.Index , clip.Title);

                            DecryptVideo(iStream, newPath);
                            // Generate transcript file if user ask
                            WriteTranscriptFile(clip, newPath);

                            playingFileStream.Dispose();

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public static void DecryptVideo(IStream currentStream, string newPath)
        {
            try
            {
                currentStream.Stat(out STATSTG stat, 0);
                IntPtr myPtr = (IntPtr)0;
                int streamSize = (int)stat.cbSize;
                byte[] streamInfo = new byte[streamSize];
                currentStream.Read(streamInfo, streamSize, myPtr);
                File.WriteAllBytes(newPath, streamInfo);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void WriteTranscriptFile(Clip clipId, string clipPath)
        {
            // Get all transcript to list
            List<ClipTranscript> clipTranscripts = clipId.Subtitle;

            if (clipTranscripts.Count > 0)
            {
                // Create transcript path with the same name of the clip
                string transcriptPath = Path.Combine(Path.GetDirectoryName(clipPath),
                    Path.GetFileNameWithoutExtension(clipPath) + ".srt");
                if (File.Exists(transcriptPath))
                {
                    File.Delete(transcriptPath);
                }

                using (FileStream transcriptStream = File.OpenWrite(transcriptPath))
                {
                    using (StreamWriter writer = new StreamWriter(transcriptStream))
                    {
                        // Write it to file with stream writer
                        int i = 1;
                        foreach (var clipTranscript in clipTranscripts)
                        {
                            var start = TimeSpan.FromMilliseconds(clipTranscript.StartTime).ToString(@"hh\:mm\:ss\,fff");
                            var end = TimeSpan.FromMilliseconds(clipTranscript.EndTime).ToString(@"hh\:mm\:ss\,fff");
                            writer.WriteLine(i++);
                            writer.WriteLine(start + " --> " + end);
                            writer.WriteLine(clipTranscript.Text);
                            writer.WriteLine();
                        }
                    }
                }
            }
        }

    }
}
