**This is still a work in progress!!**

# PsDecryptCourse
Decrypt video courses using simple Powershell cmdlets

After building the solution, load the module into Powershell:

```Powershell
Import-Module PoSHLib01
```

This houses two cmdlets.

* **Get-Courses** - Cmdlet that will look in the default directory and database file for downloaded courses. It returns a collection of courses.
* **Invoke-Decryption** - This will take a single course object from Get-Courses and dump the videos into a folder of your choice.

Perform:

```Powershell
$courses = Get-Courses
( select a course from $courses.Course grab the ID or title )
$course = $courses | where { $_.Course.Name -eq "498ebdff-3ef5-4bc0-a37a-e8471f17fd8c" }
$course = $courses | where { $_.Course.Title -eq "Advanced Course About Doing Stuff" }
Invoke-Decryption -courseitem $course -outputpath "C:\dumpcourses"
```

```Powershell
$courses = Get-Courses
$courses | % {
  Invoke-Decryption -courseitem $_ -outputpath "C:\dumpcourses"
}
```

**This is still a work in progress!!**
