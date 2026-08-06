@echo off
cd /d "%~dp0.."
set b="version"
set version ="1"
set branchName="upm_attribute"

REM Read the package version.
for /f "tokens=1,2* delims=:," %%a in (Assets/ActionAttribute/package.json) do (
    echo %%a| findstr %b% >nul && (
       set version=  %%b
    )
)

set version=%version: =%
echo on
git subtree split --prefix=Assets/ActionAttribute --branch %branchName%
git push origin %branchName%:%branchName%
git tag %branchName%_%version% %branchName%
git push origin %branchName% --tags
pause
