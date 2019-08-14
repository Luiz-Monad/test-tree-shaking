
Set-ExecutionPolicy Bypass -Scope Process -Force; iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))

cinst Cmder #1.3.11
# cinst ConEmu #19.6.23.0
cinst 7zip #19.0
# cinst DotNet3.5 #3.5.20160716
cinst DotNet4.0 #4.0.30319.20141222 -- used by msbuild, used by FSAC
# cinst DotNet4.5.2 #4.5.2.20140902
# cinst DotNet4.6.2 #4.6.01055.20170308
cinst dotnetfx #4.7.2.20180712
cinst netfx-4.7.2-devpack #4.7.2.20190225
# cinst microsoft-build-tools #15.0.26320.2 -- used by the FSAC on .fsx files
# cinst microsoft-build-tools --version 14.0.25420.1 --side-by-side
cinst visualstudio2019buildtools #lastest tools
cinst visualstudio2019-workload-vctools #c++ tools, used by node-gyp
cinst dotnetcore-sdk #2.2.200
cinst dotnetcore-windowshosting #2.2.6
cinst fiddler #2.4.9.9
cinst git #2.21.0
cinst ilspy #4.0.1
cinst jdk8 #8.0.112
cinst linqpad #5.05.02
# cinst mono #4.4.2
# cinst msys2 #20160719.1.1
cinst nodejs #11.14.0
cinst paint.net #4.0.6
cinst paket #5.207.0
cinst PowerShell #5.1.14409.20180811
# cinst procexp #16.21.0.20170716
# cinst procmon #3.32
# cinst TcpView #3.05
cinst sysinternals
cinst python #3.6.0
cinst python2 #2.7.16
cinst python3 #3.6.0
cinst tortoisegit #2.8.0.0
cinst tortoisehg #4.0.1
# cinst vcpython27 #9.0.0.30729
cinst vcredist140 #14.21.27702.2
cinst vcredist2008 #9.0.30729.6161
cinst vcredist2010 #10.0.40219.2
cinst vcredist2015 #14.0.24215.20170201
cinst visualfsharptools #4.0
cinst vlc #2.2.4.20161210
# cinst vmmap #3.21 #broken
cinst vscode #1.24.1
cinst fake
cinst paket

install-module pscx

npm install -g yarn

