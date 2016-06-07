@echo off

tools\NuGet.exe restore src\Parsley.sln -source "https://nuget.org/api/v2/" -RequireConsent -o "src\packages"

powershell -NoProfile -ExecutionPolicy Bypass -Command "& 'src\packages\psake.4.5.0\tools\psake.ps1' build.ps1 %*; if ($psake.build_success -eq $false) { write-host "Build Failed!" -fore RED; exit 1 } else { exit 0 }"