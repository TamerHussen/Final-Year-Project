@echo off
title ML-Agents Automated Setup
echo =============================================
echo   ML-Agents Unity Setup Script (Python 3.10)
echo =============================================

echo.
echo STEP 1: Remove Python 3.11 (Microsoft Store Version)
echo -----------------------------------------------------
powershell -command "Get-AppxPackage *Python* | Remove-AppxPackage" 
echo Python 3.11 removed if it existed.
echo.

echo STEP 2: Download Python 3.10 installer
echo -----------------------------------------------------
set PYTHON_URL=https://www.python.org/ftp/python/3.10.0/python-3.10.0-amd64.exe
set INSTALLER=D:\python310_installer.exe
curl -L %PYTHON_URL% -o %INSTALLER%
echo Download complete.
echo.

echo STEP 3: Install Python 3.10 to D:\Python310
echo -----------------------------------------------------
%INSTALLER% /quiet InstallAllUsers=1 PrependPath=1 TargetDir="D:\Python310"
echo Python 3.10 installed to D:\Python310
echo.

echo STEP 4: Add Python 3.10 to PATH
echo -----------------------------------------------------
setx PATH "%PATH%;D:\Python310;D:\Python310\Scripts"
echo PATH updated.
echo.

echo STEP 5: Create ML-Agents virtual environment
echo -----------------------------------------------------
D:\Python310\python.exe -m venv D:\mlagents_venv
echo Virtual environment created at D:\mlagents_venv
echo.

echo STEP 6: Activate VENV and install ML-Agents + Torch
echo -----------------------------------------------------
call D:\mlagents_venv\Scripts\activate
pip install --upgrade pip
pip install mlagents==0.30.0
pip install torch --upgrade
pip install tensorboard
echo ML-Agents + Torch installed successfully.
echo.

echo STEP 7: Verify installation
echo -----------------------------------------------------
python --version
mlagents-learn --help
echo Verified.
echo.

echo ===============================
echo ML-Agents Setup Complete!
echo ===============================
pause
