# PyAudioMerge.spec
# -*- mode: python ; coding: utf-8 -*-

block_cipher = None

a = Analysis(
    ['PyAudioMerge.py'],
    pathex=[],
    binaries=[('ffmpeg/bin/ffmpeg.exe', 'ffmpeg/bin'), ('resources/python312.dll', '.')],  # Include binaries
    datas=[('presets', 'presets')],  # Include data files
    hiddenimports=['tkinter'],
    hookspath=[],
    hooksconfig={},
    runtime_hooks=[],
    excludes=[],
    noarchive=False,
    optimize=0,
)

pyz = PYZ(a.pure, a.zipped_data, cipher=block_cipher)

exe = EXE(
    pyz,
    a.scripts,
    [],
	exclude_binaries=True,
    name='PyAudioMerge',
    debug=True,
    bootloader_ignore_signals=False,
    strip=False,
    upx=True,
    console=True,
    disable_windowed_traceback=False,
    target_arch=None,
    codesign_identity=None,
    entitlements_file=None,
    icon=['resources\\AudioMerge.ico'],
)

# Add a build command to output the directory instead of a single file
coll = COLLECT(
    exe,
    a.binaries,
    a.zipfiles,
    a.datas,
    strip=False,
    upx=True,
    upx_exclude=[],
    name='PyAudioMerge'  # This name will be the output directory
)
