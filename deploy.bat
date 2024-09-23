
@echo off

rem H is the destination game folder
rem GAMEDIR is the name of the mod folder (usually the mod name)
rem GAMEDATA is the name of the local GameData
rem VERSIONFILE is the name of the version file, usually the same as GAMEDATA,
rem    but not always

set H=%KSPDIR%
set GAMEDIR=%NAME%
set GAMEDATA="GameData"
set VERSIONFILE=%GAMEDIR%.version

rem Print and pause to confirm the inputs
rem echo Target Dir: %TGTDIR%
rem echo Plugin: "%TGTDIR%%FILENAME%"
rem echo PDB: "%TGTDIR%%NAME%.pdb"
rem pause

rem Proceed with file copying

copy /Y "%TGTDIR%%FILENAME%" "%GAMEDATA%\%GAMEDIR%\Plugins"
copy /Y "%TGTDIR%%NAME%.pdb" "%GAMEDATA%\%GAMEDIR%\Plugins"

copy /Y %VERSIONFILE% %GAMEDATA%\%GAMEDIR%

xcopy /y /s /I %GAMEDATA%\%GAMEDIR% "%H%\GameData\%GAMEDIR%"

rem pause
