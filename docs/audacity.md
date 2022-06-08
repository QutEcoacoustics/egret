# Audacity

[Audacity](https://www.audacityteam.org/)
is "free, open source, cross-platform audio software - 
an easy-to-use, multi-track audio editor and recorder for 
Windows, macOS, GNU/Linux and other operating systems."

Audacity runs on desktops and laptops, and has a point and click graphical interface and keyboard shortcuts.

Egret can use [Audacity Project](https://manual.audacityteam.org/man/audacity_projects.html) files as input and output.

*As input*: Audacity Project files can contain labels in [label tracks](https://manual.audacityteam.org/man/label_tracks.html).
The labels can be used as expectations for tests in a test suite.


*As output*: Results from running tools and comparing tool output to the test expectations can be saved as Audacity Project files.


## Benefits

Audacity may be helpful for [defining test expectations](#usageDefineExpectations) using the graphical interface.

While there are many programs that can visualise audio, 
Audacity has built-in tools to define areas of interest in the audio.
The areas of interest are defined by a start time and an end time, and can include a high and low frequency range as well.
The areas can be defined by clicking and dragging and can be edited in a table layout.
These areas of interest can be saved to Audacity Project files and used by Egret as test expectations.

Audacity may be helpful when [reviewing results](#usageReviewResults) to 
look at the visualisation of the audio and labelled areas of interest.

It can be difficult to create reliable audio recognition tools.
Egret makes it easy to assess the actual output of tools compared to the expected output.
Output formats such as CSV or JSON allow automated processing to assess the results.
Sometimes it is useful to manually assess any problems with the actual output not matching expected output.
Egret results saved as Audacity Project files can be opened in Audacity, along with the audio file.
The Audacity interface provides a way to manually evaluate test results compared to test expectations.


## <a id="usageDefineExpectations"></a> Usage for defining test expectations

Open Audacity  (version 2 or 3) , and 
[import the audio file](https://manual.audacityteam.org/man/importing_audio.html)
that contains the sounds to be recognised. 

Define Labels either in Label Tracks using the 
[label track tools](https://manual.audacityteam.org/man/label_tracks.html) or use the 
[Label Editor](https://manual.audacityteam.org/man/labels_editor.html).

Save your work as an [Audacity Project](https://manual.audacityteam.org/man/file_menu_save_project.html).
Either "Save Project" or "Save Project As...". This will save your tracks as an Audacity Project file.
Depending on the Audacity version, this might:

- create a file with the extension `.aup` and a folder named the same as the Audacity project with the extension `_data` (version 2); or
- create a file with the extension `.aup3` (version 3).

Egret can import both `.aup` and `.aup3` files.

To import the labels, Egret only needs the `.aup` or `.aup3` file, not the directory.
The directory contains the audio imported into Audacity. You may want to keep the directory if you want to change the labels.

Make sure you keep the original audio file you imported into Audacity.
Egret requires this original audio file to run the analysis tools and get results.

The Audacity project file and the audio file must have the same name, and be in the same folder.

The original audio file is found by Egret by finding a file that has the same file "stem" as the Audacity project file.
The "stem" is the filename before to the last dot `.` (the extension, e.g. `.wav`).
Egret finds the original audio file by removing the file extension and comparing the "stem" of the audio file and the Audacity project file.

For example, the Audacity project file `/123/abc.aup` or `/123/abc.aup3` might have an original audio file
at the path `/123/abc.wav` or `/123/abc.mp3`.


## <a id="usageReviewResults"></a> Usage for reviewing results

When running Egret, there is an option to save the results as Audacity Project files using the `--audacity` command line argument.

This will create one Audacity Project file for each combination of test suite, tool, and audio file.
For example, if you have one test suite with two audio files, and two tools, 
then the output results will be stored in four Audacity Projects.

Egret saves Audacity Project results as `.aup` files, which can be opened by Audacity version 2 or 3.

Open an Audacity Project file. It will contain a number of Label Tracks.
I won't have any Audio tracks. The audio file that was used by Egret needs to be imported to show the audio track.

Once the audio file is imported, then the Label Tracks can be used to investigate the results from the analysis tools run using Egret.


## Use different Audacity Projects for defining expectations and reviewing results

We recommend using separate, dedicated Audacity Projects for defining expectations and reviewing results.

It should be possible to open two or more Audacity Project files at the same time using 
[File > New](https://manual.audacityteam.org/man/file_menu.html#new).

As you review the results, you can use any insights to make changes in the other Audacity window that contains the test expectations.


## Frequently Asked Questions

### Why can't Egret read the audio files saved as part of the Audacity Project file?

An Audacity project file may include zero, one, or more audio files.
However, the files are stored in Sun AU format, split into smaller files.

The goal is to be able to load expectations and save results to easily view labels against audio files.
It is tricky to run tools and match test expectations when the original audio is spilt into parts, 
and in a format that some tools have trouble reading.

Egret is a tool runner, and does not try to convert or combine the audio files.

Instead, there is the convention that one Audacity project file (`.aup` or `.aup3`) references one original audio file.
The Audacity project file and the audio file must have the same name, and be in the same folder.
