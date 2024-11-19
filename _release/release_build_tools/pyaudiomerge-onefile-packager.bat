@echo off

pyinstaller --onefile --name "PyAudioMerge" PyAudioMerge_Release.py --icon="../../resources/AudioMerge.ico" --add-binary="../../ffmpeg/bin/ffmpeg.exe;ffmpeg/bin" --add-binary="../../resources/python312.dll;." --add-data="../../presets;presets" --hidden-import "tkinter" --noconsole

