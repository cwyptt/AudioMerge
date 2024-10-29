@echo off

pyinstaller --onefile --name "AudioMergeMKV" AudioMergeMKV_Release.py --icon="../AudioMergeMKV.ico" --add-binary="../../ffmpeg/bin/ffmpeg.exe;ffmpeg/bin" --add-binary="../python312.dll;." --add-data="../../presets;presets" --hidden-import "tkinter" --noconsole

