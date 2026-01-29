@echo off
set b="version"
set version ="1"
REM 获取版本号
for /f "tokens=1,2* delims=:," %%a in (Assets/ActionEditor.Nodes/package.json) do (
    echo %%a| findstr %b% >nul && (
       set version=  %%b
    ) || (
        @REM echo %%a nnn %b%
    )
)


set version=%version: =%
echo on
git subtree split --prefix=Assets/ActionEditor.Nodes.BT --branch upm_bt
git push origin upm_bt:upm_bt
git tag %version% upm_bt
git push origin upm_bt --tags
set cur=%~dp0


pause