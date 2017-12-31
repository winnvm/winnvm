REM Sonar Analysis

MSBuild.exe /t:Clean
SonarQube.Scanner.MSBuild.exe begin /k:"com.seenukarthi:winnvm" /n:"WinNvm" /v:"1.0.1"
MSBuild.exe /t:Rebuild
SonarQube.Scanner.MSBuild.exe end
