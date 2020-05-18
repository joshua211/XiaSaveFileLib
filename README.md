# XiaSaveFileLib

Library to save and load savefiles from the game [Amazing Cultivation Simulator](https://store.steampowered.com/app/955900/Amazing_Cultivation_Simulator/)

Requires 
```
Assembly-CSharp.dll 
```
and 
```
Assembly-CSharp-firstpass.dll
```
from the game folder to be placed in the Lib folder.

## How to use

Load a .save file from the /saves folder with
```
SaveFileManager.LoadSaveFileAsync(string path, int head);
```
this returns a SaveFile object, with the actual save file content as a JSON string in SaveFile.FileContent

Use
```
SaveFileManager.SaveFileAsync(SaveFile file, string path);
```
to turn a SaveFile object back to a .save file after editing the JSON content.

# Disclaimer
All rights belong to the creators of the game. No actual source code from the game should be shipped together with this project
