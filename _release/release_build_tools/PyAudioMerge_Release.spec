# -*- mode: python ; coding: utf-8 -*-


a = Analysis(
    ['PyAudioMerge_Release.py'],
    pathex=[],
    binaries=[('../../ffmpeg/bin/ffmpeg.exe', 'ffmpeg/bin'), ('../../resources/python312.dll', '.')],
    datas=[('../../presets', 'presets')],
    hiddenimports=['tkinter'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)
pyz = PYZ(a.pure)

exe = EXE(
    pyz,
    a.scripts,
    a.binaries,
    a.datas,
    [],
    name='PyAudioMerge',
    debug=False,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    upx_exclude=[],
    runtime_tmpdir=None,
    console=False,
    disable_windowed_traceback=False,
    argv_emulation=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=['..\\..\\resources\\AudioMerge.ico'],
)
