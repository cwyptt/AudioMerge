import subprocess
import os
import sys
import json
import tkinter as tk
from tkinter import filedialog, messagebox


class AudioMerge:
    def __init__(self, root):
        self.root = root
        self.root.title("audiomerge (v0.1)")
        self.root.geometry("800x800")
        self.root.resizable(False, False)  # Allow window resizing
        self.audio_tracks = []
        self.volumes = []
        self.includes = []
        self.track_vars = []
        self.volume_vars = []
        self.track_frames = []
        self.track_name_vars = []
        self.filepath = None
        self.preset_selected_label = None
        self.file_selected_label = None
        self.preset_filepath = None
        self.tracks_frame = None
        self.merge_button = None
        self.volume_frame = None
        self.start_time_entry = None
        self.export_folder = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'output')
        self.preset_folder = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'presets')
        self.audio_tracks_file = os.path.join(self.preset_folder, "default_preset.json")

        # Create the default folders if they don't exist
        os.makedirs(self.export_folder, exist_ok=True)
        os.makedirs(self.preset_folder, exist_ok=True)

        self.create_widgets()

    def create_widgets(self):
        tk.Label(self.root, text="Click below to browse for an MKV file").pack(pady=10, anchor='center')

        self.file_selected_label = tk.Label(self.root, text="No file selected")
        self.file_selected_label.pack(anchor='center')

        browse_button = tk.Button(self.root, text="Browse", command=self.browse_file)
        browse_button.pack(pady=10, anchor='center')

        tk.Label(self.root, text="Volume Scale (0% - 200%)").pack(anchor='center')

        self.tracks_frame = tk.Frame(self.root)
        self.tracks_frame.pack(expand=True, fill='both', pady=10)

        self.load_tracks()

        self.merge_button = tk.Button(self.root, text="Merge Audio Files With Video", command=self.process_file,
                                      bg="red", fg="white")
        self.merge_button.pack(pady=10, anchor='center')

        tk.Label(self.root, text="Start Time (hh:mm:ss)").pack(anchor='center')

        self.start_time_entry = tk.Entry(self.root)
        self.start_time_entry.pack(pady=5, anchor='center')

        save_button = tk.Button(self.root, text="Save Preset", command=self.save_preset)
        save_button.pack(pady=10, anchor='center')

        self.preset_selected_label = tk.Label(self.root, text="No preset selected")
        self.preset_selected_label.pack(anchor='center')

        load_button = tk.Button(self.root, text="Load Preset", command=self.load_preset)
        load_button.pack(pady=10, anchor='center')

        export_folder_button = tk.Button(self.root, text="Set Export Folder", command=self.set_export_folder)
        export_folder_button.pack(pady=10, anchor='center')

        preset_folder_button = tk.Button(self.root, text="Set Preset Folder", command=self.set_preset_folder)
        preset_folder_button.pack(pady=10, anchor='center')

    def load_tracks(self):
        self.audio_tracks, self.volumes, self.includes = self.read_audio_tracks(self.audio_tracks_file)

        for widget in self.tracks_frame.winfo_children():
            widget.destroy()

        self.track_vars = []
        self.volume_vars = []
        self.track_name_vars = []
        self.track_frames = []

        colors = ['#FFC0CB', '#FFD700', '#ADFF2F', '#87CEEB', '#FF69B4', '#DDA0DD']  # List of colors to cycle through

        for i in range(len(self.audio_tracks)):
            track_label = tk.Label(self.tracks_frame, text=f"Track {i + 1}", width=10)
            track_label.grid(row=i, column=0, padx=5, pady=2)

            color = colors[i % len(colors)]  # Cycle through the colors

            content_frame = tk.Frame(self.tracks_frame, bd=2, relief='raised', cursor='hand2', bg=color)
            content_frame.grid(row=i, column=1, sticky='ew', padx=5, pady=2)
            content_frame._original_bg = color  # Set the original background color

            track_var = tk.IntVar(value=int(self.includes[i]))
            volume_var = tk.IntVar(value=int(self.volumes[i]))
            track_name_var = tk.StringVar(value=self.audio_tracks[i])

            tk.Checkbutton(content_frame, variable=track_var, bg=color).pack(side='left', padx=5)

            track_name_entry = tk.Entry(content_frame, textvariable=track_name_var, width=25)
            track_name_entry.pack(side='left', padx=5)

            tk.Scale(content_frame, from_=0, to=200, orient='horizontal', variable=volume_var, length=300).pack(
                side='right', padx=5)

            self.track_vars.append(track_var)
            self.volume_vars.append(volume_var)
            self.track_name_vars.append(track_name_var)
            self.track_frames.append(content_frame)

        self.tracks_frame.grid_columnconfigure(1, weight=1)

    @staticmethod
    def read_audio_tracks(audio_tracks_file):
        with open(audio_tracks_file, 'r') as f:
            data = json.load(f)

        tracks = [item['Audio Track'] for item in data]
        volumes = [item['Volume'] for item in data]
        includes = [int(item['Enabled']) if item['Enabled'].isdigit() else 0 for item in data]

        return tracks, volumes, includes

    def browse_file(self):
        filepath = filedialog.askopenfilename(filetypes=[("MKV files", "*.mkv")])
        if filepath:
            self.filepath = filepath
            self.file_selected_label.config(text=f"File Selected: {os.path.basename(self.filepath)}")

            self.merge_button.config(bg="green", text="Merge Audio Files With Video")  # Change button color to green
            messagebox.showinfo("File selected", f"File selected: {self.filepath}")
        else:
            print("No file selected")
            messagebox.showwarning("Warning", "No file selected")
            self.merge_button.config(bg="red", text="Merge Audio Files With Video")  # Change button color to red

    def process_file(self):
        if not self.filepath:
            messagebox.showerror("Error", "No MKV file selected.")
            return

        selected_tracks = [(track, volume.get(), idx) for idx, (track, var, volume) in
                           enumerate(zip(self.audio_tracks, self.track_vars, self.volume_vars)) if var.get() == 1]

        if not selected_tracks:
            messagebox.showerror("Error", "No audio tracks selected for merging.")
            return

        first_2_letters = '_'.join(track[:2].lower() for track, _, _ in selected_tracks)
        tracks, volumes, indices = zip(*selected_tracks)

        self.execute_ffmpeg(self.filepath, tracks, volumes, indices, first_2_letters)

    def execute_ffmpeg(self, input_filepath, tracks, volumes, indices, first_2_letters):
        input_name = os.path.splitext(os.path.basename(input_filepath))[0]
        base_output_filename = f"{input_name}-{first_2_letters}-audio_merged.mp4"

        if self.export_folder:
            base_output_filepath = os.path.join(self.export_folder, base_output_filename)
        else:
            base_output_filepath = base_output_filename

        output_filepath = self.generate_output_filepath(base_output_filepath)

        if getattr(sys, 'frozen', False):
            if hasattr(sys, '_MEIPASS'):
                ffmpeg_path = os.path.join(sys._MEIPASS, "ffmpeg", "bin", "ffmpeg.exe")
            else:
                script_dir = os.path.dirname(os.path.abspath(sys.argv[0]))
                ffmpeg_path = os.path.join(script_dir, "ffmpeg", "bin", "ffmpeg.exe")
        else:
            script_dir = os.path.dirname(os.path.abspath(__file__))
            ffmpeg_path = os.path.join(script_dir, "ffmpeg", "bin", "ffmpeg.exe")

        amerge_inputs = len(tracks)
        volume_filters = [f"[0:a:{idx}]volume={vol / 100.0}[a{idx}]" for idx, vol in zip(indices, volumes)]
        amerge_filter = ';'.join(volume_filters)
        amerge_filter += ';' + ''.join([f"[a{idx}]" for idx in indices])
        amerge_filter += f"amerge=inputs={amerge_inputs}[aout]"

        start_time = self.start_time_entry.get()

        ffmpeg_command = [
            ffmpeg_path,
            "-ss", start_time,
            "-i", input_filepath,
            "-filter_complex", amerge_filter,
            "-map", "0:v",
            "-map", "[aout]",
            "-c:v", "libx264",
            "-crf", "21",
            "-preset", "fast",
            "-ac", "2",
            "-metadata:s:a:0", f"title={', '.join(tracks)}",
            "-async", "1",
            output_filepath
        ]

        try:
            subprocess.run(ffmpeg_command, check=True)
            track_names = ', '.join(tracks)
            messagebox.showinfo("Success", f"Processing complete: {output_filepath} | {track_names}")
        except subprocess.CalledProcessError as e:
            messagebox.showerror("Error", f"An error occurred: {e}")

    @staticmethod
    def generate_output_filepath(base_filepath):
        if not os.path.exists(base_filepath):
            return base_filepath

        base, ext = os.path.splitext(base_filepath)
        i = 1
        new_filepath = f"{base} ({i}){ext}"
        while os.path.exists(new_filepath):
            i += 1
            new_filepath = f"{base} ({i}){ext}"

        return new_filepath

    def save_preset(self):
        preset_file = filedialog.asksaveasfilename(defaultextension=".json", filetypes=[("JSON files", "*.json")],
                                                   initialdir=self.preset_folder)
        if not preset_file:
            return

        data = []
        for track, var, volume in zip(self.audio_tracks, self.track_vars, self.volume_vars):
            include = var.get()
            volume_value = int(volume.get())
            data.append({"Audio Track": track, "Enabled": str(include), "Volume": volume_value})

        with open(preset_file, 'w') as f:
            json.dump(data, f, indent=4)

        messagebox.showinfo("Preset Saved", "Preset saved successfully.")

    def load_preset(self):
        preset_file = filedialog.askopenfilename(filetypes=[("JSON files", "*.json")], initialdir=self.preset_folder)
        if not preset_file:
            return

        self.preset_filepath = preset_file
        self.audio_tracks, self.volumes, self.includes = self.read_audio_tracks(preset_file)
        self.update_gui_with_loaded_preset()
        self.preset_selected_label.config(
            text=f"Preset Selected: {os.path.splitext(os.path.basename(self.preset_filepath))[0]}")
        messagebox.showinfo("Preset Loaded", "Preset loaded successfully.")

    def update_gui_with_loaded_preset(self):
        for track_var, volume_var, include, volume in zip(self.track_vars, self.volume_vars, self.includes,
                                                          self.volumes):
            track_var.set(int(include))
            volume_var.set(int(volume))

        for widget in self.tracks_frame.winfo_children():
            widget.destroy()

        for track, track_var, volume_var in zip(self.audio_tracks, self.track_vars, self.volume_vars):
            frame = tk.Frame(self.tracks_frame)
            frame.pack(fill='x', pady=2, anchor='center')

            tk.Checkbutton(frame, text=track, variable=track_var).pack(side='left', padx=10)

            # Volume Scale
            self.volume_frame = tk.Frame(frame)
            self.volume_frame.pack(padx=10)

            tk.Label(frame, text="Volume:").pack()
            tk.Scale(frame, variable=volume_var, from_=0, to=200, resolution=10, orient='horizontal', label='%').pack(
                anchor='center')

            # Ensure scales are centered within frames
            frame.pack_propagate(False)

    def set_export_folder(self):
        folder = filedialog.askdirectory()
        if folder:
            self.export_folder = folder
            messagebox.showinfo("Export Folder", f"Export folder set to: {self.export_folder}")

    def set_preset_folder(self):
        folder = filedialog.askdirectory()
        if folder:
            self.preset_folder = folder
            messagebox.showinfo("Preset Folder", f"Preset folder set to: {self.preset_folder}")


def main():
    root = tk.Tk()
    app = AudioMerge(root)
    root.mainloop()


if __name__ == "__main__":
    main()
